using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using Prizmer.Ports;
using Prizmer.Meters.iMeters;

namespace Prizmer.Meters
{
    public class spg76212 : CMeter, IMeter
    {
        public void Init(uint address, string pass, VirtualPort vp)
        {
            this.m_address = address;
            this.m_vport = vp;
            this.vp = vp;


            //для однозачного парсинга float значений
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
  
        }

        //адрес счетчика
        //private byte m_address;
        private byte host_address = 0x08;
        
        VirtualPort vp;

        byte DLE = 0x10;
        byte SOH = 0x01;
        byte ISI = 0x1f;
        byte STX = 0x02;
        byte ETX = 0x03;
        byte HT = 0x09;
        byte FF = 0x0c;

        public struct ParamInfo
        {
            public string caption;
            public string units;
            public int channelNumber;
            public int paramNumber;
            public float val;
        }

        ////////////////////////////////////////////////////////
        //Функция вычисляет и возвращает циклический код для
        //последовательности из len байтов, указанной *msg.
        //Используется порождающий полином:
        //(X в степени 16)+(X в степени 12)+(X в степени 5)+1.
        //Полиному соответствует битовая маска 0x1021.
        //
        public byte[] CRC16bytes(byte[] msg)
        {
            int crc = 0;
            for (int i = 0; i < msg.Length; i++)
            {
                crc = crc ^ (int)msg[i] << 8;
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0) crc = (crc << 1) ^ 0x1021;
                    else crc <<= 1;
                }
            }

            byte[] resBytes = BitConverter.GetBytes((ushort)crc);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(resBytes);

            return resBytes;
        }

        private bool sendMessage(byte[] messageBodyArr, byte FNC, ref List<byte> answerDataList)
        {
            List<byte> messageList = new List<byte>();

            //заголовок запроса 
            messageList.Add((byte)m_address);
            messageList.Add(host_address);
            messageList.Add(DLE);
            messageList.Add(ISI);
            messageList.Add(FNC);
            //возможные DLE & DATAHEAD

            //тело запроса
            messageList.Add(DLE);
            messageList.Add(STX);
            for (int i = 0; i < messageBodyArr.Length; i++)
                messageList.Add(messageBodyArr[i]);
            messageList.Add(DLE);
            messageList.Add(ETX);

           //CRC16
            messageList.AddRange(CRC16bytes(messageList.ToArray()));

            //добавим неучитываемые при расчете CRC16 байты в начало сообщения
            messageList.Insert(0, SOH);
            messageList.Insert(0, DLE);

            //отправим команду счетчику
            byte[] answerArray = new byte[1024];
            byte[] msgArray = messageList.ToArray();
            int answCount = vp.WriteReadData(findPackageSign, msgArray, ref answerArray, msgArray.Length, -1);


           // WriteToLog(BitConverter.ToString(answerArray));

            if (answCount < 2)
            {
                WriteToLog("sendMessage: в ответ пришло " + answCount.ToString() + " символов");
                return false;

            }

            //выберем из полученного ответа данные

            int STXindex = -1, ETXindex = -1;
            for (int i = 1; i < answerArray.Length; i++)
            {
                if (answerArray[i] == STX && answerArray[i-1] == DLE)
                    STXindex = i;
                else if (answerArray[i] == ETX && answerArray[i-1] == DLE)
                         ETXindex = i;
            }

            if (STXindex < 1 || ETXindex < 1)
            {
                WriteToLog("sendMessage: невозможно определить индексы STX и ETX ответа");
                return false;
            }

            try
            {
                int answDLength = (ETXindex - 2) - (STXindex + 1);
                byte[] answerDataArr = new byte[answDLength];
                Array.Copy(answerArray, STXindex + 1, answerDataArr, 0, answDLength);
                answerDataList = new List<byte>();
                answerDataList.AddRange(answerDataArr);
            }
            catch (Exception ex)
            {
                WriteToLog("sendMessage: ошибка копирования полезных данных ответа в новый массив");
                return false;
            }

            return true;
        }

        public bool readArchivesStructure(ushort paramNumber, ref List<ParamInfo> archStructure)
        {
            byte FNC = 0x19;

            byte[] paramBytes = stringToBytes(paramNumber.ToString());

            List<byte> messageBodyList = new List<byte>();
            messageBodyList.Add(HT);
            messageBodyList.AddRange(stringToBytes("0"));
            messageBodyList.Add(HT);
            messageBodyList.AddRange(paramBytes);
            messageBodyList.Add(FF);
            byte[] messageBodyArr = messageBodyList.ToArray();
            List<byte> answDataList = new List<byte>();
            if (!sendMessage(messageBodyArr, FNC, ref answDataList))
            {
                WriteToLog("readArchivesStructure: ошибка отправки сообщения");
                return false;
            }

            if (answDataList.Count < 2)
            {
                WriteToLog("readArchivesStructure: кол-во полученых данных меньше " +
                "минимально допустимого числа для данного запроса");
                return false;
            }

            List<byte[]> blocks = new List<byte[]>();
            splitInfoBlocks(answDataList.ToArray(), ref blocks);

            List<ParamInfo> pil = new List<ParamInfo>();
            bool exceptionFlag = false;
            int paramNumb = 4;
            for (int i = 0; i < blocks.Count; i++)
            {
                List<byte>[] values = new List<byte>[paramNumb];
                getValueBytesFromInfoBlock(blocks[i], paramNumb, ref values);

                ParamInfo pi = new ParamInfo();
                try
                {
                    pi.caption = bytesToString(values[0].ToArray());
                    pi.units = bytesToString(values[1].ToArray());
                    pi.channelNumber = int.Parse(bytesToString(values[2].ToArray()));
                    pi.paramNumber = int.Parse(bytesToString(values[3].ToArray()));
                }
                catch (Exception ex)
                {
                    exceptionFlag = true;
                    WriteToLog("readArchivesStructure: ошибка преобразование байт в соответствующие единицы");
                }

                pil.Add(pi);
            }

            if (!exceptionFlag)
            {
                archStructure = pil;
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Разделяет массив информационных байт на блоки
        /// </summary>
        /// <param name="infoBlocksBytes">Массив информационных байт начиная с первого реального байта, заканчивая последним</param>
        /// <param name="blockList"></param>
        /// <returns></returns>
        private bool splitInfoBlocks(byte[] infoBlocksBytes, ref List<byte[]> blockList)
        {
            //определим кол-во параметров
            List<int> paramIndexes = new List<int>();
            int lastInformationByteIndex = infoBlocksBytes.Length - 1;

            //WriteToLog(BitConverter.ToString(infoBlocksBytes));

            for (int i = 1; i < infoBlocksBytes.Length; i++)
            {
                if (infoBlocksBytes[i] == HT && infoBlocksBytes[i - 1] == FF)
                    paramIndexes.Add(i);
            }

            blockList = new List<byte[]>();
            for (int i = 0; i < paramIndexes.Count; i++)
            {
                int blockStartInd = paramIndexes[i];
                int blockEndInd;
                if (i < paramIndexes.Count - 1)
                    blockEndInd = paramIndexes[i + 1];
                else
                    blockEndInd = lastInformationByteIndex;

                //учет байта 0c вконце каждого инф параметра
                blockEndInd--;

                int curBlockLength = blockEndInd - blockStartInd;
                byte[] curBlockArr = new byte[curBlockLength];
                Array.Copy(infoBlocksBytes, blockStartInd, curBlockArr, 0, curBlockLength);

                blockList.Add(curBlockArr);
            }

            return true;
        }

        /// <summary>
        /// Распределяет байты параметров одного информационного блока по спискам
        /// </summary>
        /// <param name="informationBlockBytes">Массив байт блока информации начиная с HT(0x09), заканчивая последним байтом блока</param>
        /// <param name="valuesInBlock">Кол-во параметров в блоке информации</param>
        /// <param name="values">-</param>
        /// <returns>-</returns>
        private bool getValueBytesFromInfoBlock(byte[] informationBlockBytes, int valuesInBlock, ref List<byte>[] values)
        {
            List<byte>[] valListsArr = new List<byte>[valuesInBlock];
            for (int i = 0; i < valListsArr.Length; i++)
                valListsArr[i] = new List<byte>();

            int valInBlockCnt = 0;
            for (int i = 0; i < informationBlockBytes.Length; i++)
            {
                if (informationBlockBytes[i] != HT)
                {
                    valListsArr[valInBlockCnt].Add(informationBlockBytes[i]);
                }
                else
                {
                    if (i != 0) valInBlockCnt++;
                    continue;
                }
            }

            values = valListsArr;

            return true;
        }


        private string bytesToString(byte[] arr)
        {
            Encoding enc = Encoding.GetEncoding(866);
            return enc.GetString(arr);
        }

        private byte[] stringToBytes(string str)
        {
            Encoding enc = Encoding.GetEncoding(866);
            return enc.GetBytes(str);
        }

        private int findPackageSign(Queue<byte> queue)
        {
            return 0;
        }

        private void pilLogWriter(List<ParamInfo> pil)
        {
            for (int i = 0; i < pil.Count; i++)
            {
            
                WriteToLog(String.Format("Caption: {0}; Units: {1}; Channel: {2}; Param: {3}; Value: {4}", 
                    pil[i].caption, pil[i].units, pil[i].channelNumber, pil[i].paramNumber, pil[i].val));

            }
        }

        //чтение временного среза
        byte FNC = 0x18;

        public struct ArchiveRow
        {
            public List<ParamInfo> Params;
            public DateTime DateRequested;
            public DateTime DateNearest;
            public DateTime DateNearestInThePast;
        }

        public bool ReadArchiveRow(ushort paramNumber, DateTime dt, List<ParamInfo> archStructure, ref ArchiveRow archRow)
        {
            //номер параметра, соответствующий часовым архивам
         //   ushort paramNumber = 65530;

            if (archStructure == null)
            {
                WriteToLog("ReadHourArchiveRow: не определена структура архива");
                return false;
            }

            int paramsInArchive = archStructure.Count;
            List<byte> msgBodyList = new List<byte>();

            //формируем указатель один
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes("0"));
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes(paramNumber.ToString()));
            msgBodyList.Add(FF);
    
            //формируем указатель два состоящий из даты
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes(dt.Day.ToString()));
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes(dt.Month.ToString()));
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes(dt.Year.ToString()));
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes(dt.Hour.ToString()));
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes("0"));
            msgBodyList.Add(HT);
            msgBodyList.AddRange(stringToBytes("0"));
            msgBodyList.Add(FF);

            byte[] messageBodyArr = msgBodyList.ToArray();
            List<byte> answDataList = new List<byte>();
            if (!sendMessage(messageBodyArr, FNC, ref answDataList))
            {
                WriteToLog("ReadHourArchive: ошибка отправки сообщения");
                return false;
            }

            if (answDataList.Count < 2)
            {
                WriteToLog("ReadHourArchive: кол-во полученых данных меньше " +
                "минимально допустимого числа для данного запроса");
                return false;
            }

            List<byte[]> blocks = new List<byte[]>();
            splitInfoBlocks(answDataList.ToArray(), ref blocks);

            //если количество блоков ответ за вычетом 3х блоков даты не равно кол-ву параметров
            if ((blocks.Count - 3) != archStructure.Count)
            {
                WriteToLog("ReadHourArchive: кол-во распознаных блоков не соответствует " +
                    "кол-ву параметров структуры архива");
                return false;
            }

            int paramNumbInDate = 6;
            int paramNumbInVal = 1;

            //разберемся с датами (это первые 3 блока)
            DateTime[] dtArr = new DateTime[3];
            for (int i = 0; i < dtArr.Length; i++)
            {
                try
                {
                    byte[] curBlock = blocks[i];
                    List<byte>[] values = new List<byte>[paramNumbInDate];
                    getValueBytesFromInfoBlock(curBlock, values.Length, ref values);
                    int day = int.Parse(bytesToString(values[0].ToArray()));
                    int month = int.Parse(bytesToString(values[1].ToArray()));
                    int year = int.Parse(bytesToString(values[2].ToArray()))+2000;
                    int hour = int.Parse(bytesToString(values[3].ToArray()));
                    int minutes = int.Parse(bytesToString(values[4].ToArray()));
                    int seconds = int.Parse(bytesToString(values[5].ToArray()));

                    dtArr[i] = new DateTime(year, month, day, hour, minutes, seconds);
                    
                }
                catch (Exception ex)
                {
                    WriteToLog("ReadHourArchive: ошибка разбора дат в ответе");
                    return false;
                }
            }

            int paramCnt = 0;
            //разберемся с блоками, содержащими значения
            for (int i = 3; i < blocks.Count; i++)
            {
                try
                {
                    byte[] curBlock = blocks[i];
                    List<byte>[] values = new List<byte>[paramNumbInVal];
                    getValueBytesFromInfoBlock(curBlock, values.Length, ref values);
                    float val = float.Parse(bytesToString(values[0].ToArray()));
                    ParamInfo pi = archStructure[paramCnt];
                    pi.val = val;
                    archStructure[paramCnt] = pi;

                    paramCnt++;

                }
                catch (Exception ex)
                {
                    WriteToLog("ReadHourArchive: ошибка разбора значений в ответе");
                    return false;
                }
            }

            try
            {
                ArchiveRow archiveRow = new ArchiveRow();
                archiveRow.Params = new List<ParamInfo>();
                archiveRow.Params.AddRange(archStructure);
                archiveRow.DateRequested = dtArr[0];
                archiveRow.DateNearest = dtArr[1];
                archiveRow.DateNearestInThePast = dtArr[2];
                archRow = archiveRow;

                return true;
            }
            catch (Exception ex)
            {
                WriteToLog("ReadHourArchive: ошибка разбора значений в ответе 2");
                return false;
            }

        }

        public bool ReadHourArchives()
        {
            /*
            DateTime dt = DateTime.Now.Date;
            dt.AddHours(10);
            ArchiveRow arch = new ArchiveRow();
            WriteToLog("Читаю за час: " + dt.ToString());
            if (ReadHourArchiveRow(dt, ref arch))
                pilLogWriter(g_pil);
             * */

            return false;
        }

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            return null;
        }

        public bool OpenLinkCanal()
        {
            return true;
        }

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }

        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            ushort paramNumber = 65532;
            //определим структуру архива
            List<ParamInfo> archStructure = null;
            if (!readArchivesStructure(paramNumber, ref archStructure))
            {
                WriteToLog("ReadPowerSlice: невозможно прочитать структуру архива");
                return false;
            }

            //пусть дата начала - дата которая передана вместе с описанием среза
            DateTime dt_begin = new DateTime(dt.Ticks);

            ArchiveRow arch = new ArchiveRow();

            if (!ReadArchiveRow(paramNumber, dt_begin.AddYears(-2000), archStructure, ref arch))
            {
                string msg = String.Format("ReadPowerSlice: ошибка получения дат: ук2 {0}, ук3 {1}, ук4 {2}",
                dt_begin.ToString(), arch.DateNearest.ToString(), arch.DateNearestInThePast.ToString());
                WriteToLog(msg);
                return false;
           }

            DateTime dt_nearest = arch.DateNearest;

            if (dt_nearest < dt_begin)
            {
                if (!ReadArchiveRow(paramNumber, dt_begin.AddYears(-2000), archStructure, ref arch))
                {
                    string msg = String.Format("ReadPowerSlice: ошибка получения дат: ук2 {0}, ук3 {1}, ук4 {2}",
                    dt_begin.ToString(), arch.DateNearest.ToString(), arch.DateNearestInThePast.ToString());
                    WriteToLog(msg);
                    return false;
                }
            }
            else if (dt_nearest > dt_begin)
            {
                if (!ReadArchiveRow(paramNumber, dt_nearest, archStructure, ref arch))
                {
                    string msg = String.Format("ReadPowerSlice: ошибка получения дат: ук2 {0}, ук3 {1}, ук4 {2}",
                    dt_begin.ToString(), arch.DateNearest.ToString(), arch.DateNearestInThePast.ToString());
                    WriteToLog(msg);
                    return false;
                }
            }

            try
            {
                recordValue = arch.Params[param].val;
            }
            catch (Exception ex)
            {
                WriteToLog("ReadDailyValues: параметр " + param.ToString() + " отсутствует в массиве значений");
            }


            return true;
        }

        public bool ReadDailyValues(uint recordId, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }

        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<RecordPowerSlice> listRPS, byte period)
        {
            return false;
        }

        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            return false;
        }

        public bool SyncTime(DateTime dt)
        {
            return false;
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            return false;
        }


        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceDesriptorList, DateTime dt_end, SlicePeriod period)
        {
            //определим внутренний адрес параметра в зависимости от периода
            ushort paramNumber;
            switch (period)
            {
                case SlicePeriod.Hour: 
                    {
                        //номер параметра, соответствующий часовым архивам
                        paramNumber = 65530;
                        break;
                    }
                default:
                    {
                        WriteToLog("ReadPowerSlice: заданный период среза не поддерживается ");
                        return false;
                    }
            }

            //определим структуру архива
            List<ParamInfo> archStructure = null;                      
            if (!readArchivesStructure(paramNumber, ref archStructure))
            {
                WriteToLog("ReadPowerSlice: невозможно прочитать структуру архива");
                return false;
            }

            //лист дескрипторов со значениями
            List<SliceDescriptor> sdList = new List<SliceDescriptor>();
            foreach (SliceDescriptor sd in sliceDesriptorList)
            {
                //пусть дата начала - дата которая передана вместе с описанием среза
                DateTime dt_begin = new DateTime(sd.Date.Ticks);

                //определим дату, с которой будем читать (прочитаем )
                DateTime start_dt = new DateTime();
                ArchiveRow arch = new ArchiveRow();
                if (ReadArchiveRow(paramNumber, dt_begin.AddYears(-2000), archStructure, ref arch))
                {
                    DateTime dt_nearest = arch.DateNearest;

                    if (dt_nearest == dt_begin)
                        start_dt = dt_begin;
                    else if (dt_nearest < dt_begin)
                        start_dt = dt_begin.AddMinutes((double)period);
                    else
                        start_dt = dt_nearest;
                }
                else
                {
                    string msg = String.Format("ReadPowerSlice: ошибка получения дат: ук2 {0}, ук3 {1}, ук4 {2}", 
                        dt_begin.ToString(), arch.DateNearest.ToString(), arch.DateNearestInThePast.ToString());
                    WriteToLog(msg);
                    return false;
                }

                DateTime tempDt = new DateTime(start_dt.Ticks);
                while (tempDt <= dt_end)
                {
                    SliceDescriptor localSD = new SliceDescriptor(tempDt);
                    ArchiveRow archR = new ArchiveRow();
                    if (ReadArchiveRow(paramNumber, tempDt.AddYears(-2000), archStructure, ref archR))
                    {
                        List<uint> addresses = sd.GetAddressList();
                        for (int i = 0; i < addresses.Count; i++)
                        {
                            uint addr = addresses[i];
                            ParamInfo pi = archR.Params[(int)addr];

                            uint ivalid = 0, ivalchannel = 0;
                            sd.GetValueId((uint)i, ref ivalid);
                            sd.GetValueChannel((uint)i, ref ivalchannel);
                            localSD.AddValueDescriptor(ivalid, addr, ivalchannel, sd.Period);
                            if (!localSD.InsertValue((uint)i, pi.val, true))
                            {
                                WriteToLog("ReadPowerSlice: неудалось вставить значение " + i);
                                continue;
                            }
                        }
                        sdList.Add(localSD);
                    }
                    else
                    {
                        string msg = String.Format("ReadPowerSlice: Строка архива за дату {0} не прочитана", tempDt);
                        WriteToLog(msg);
                    }

                    tempDt = tempDt.AddMinutes((double)period);
                }
            }

            if (sdList.Count > 0)
            {
                sliceDesriptorList = sdList;
                return true;
            }
            else
            {
                WriteToLog("В итоговый список не было добавлено элементов");
                return false;
            }
        }
    }
}
