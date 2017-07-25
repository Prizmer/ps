using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;

//классы совместимости с СО
using elfextendedapp;


namespace ElfApatorCommonDriver
{
    /// <summary>
    /// Описывает типовое архивное значение
    /// </summary>
    public struct ArchiveValue
    {
        public int id;
        public DateTime dt;
        public float energy;
        public float volume;
        public int timeOn;
        public int timeErr;
    };

    public class elf108 : CMeter
    {
        public void Init(uint address, string pass, VirtualPort vp)
        {
            this.m_address = address;
            this.m_addr = (byte)(this.m_address & 0x000000ff);
            this.m_vport = vp;
        }

        static string GetStringFromByteArray(byte[] arr)
        {
            string answ_str = "";
            foreach (byte b in arr)
                answ_str += Convert.ToString(b, 16) + " ";
            return answ_str;
        }

        private byte m_addr = 0xFD;

        #region Протокол MBUS

        public struct Record
        {
            public byte DIF;
            public List<byte> DIFEs;

            public byte VIF;
            public List<byte> VIFEs;

            public List<byte> dataBytes;

            public RecordDataType recordType;
        }

        public enum RecordDataType
        {
            NO_DATA = 0,
            INTEGER = 1,
            REAL = 2,
            BCD = 3,
            VARIABLE_LENGTH = 4,
            SELECTION_FOR_READOUT = 5,
            SPECIAL_FUNСTIONS = 6
        }

        //параметры в порядке как они идут в rsp_ud
        public enum Params
        {
            FACTORY_NUMBER = 0,
            DATE = 1,
            ERR_CODE = 2,
            ENERGY = 3, //ГКал
            VOLUME = 4,
            VOLUME_IMP1 = 5,
            VOLUME_IMP2 = 6,
            VOLUME_IMP3 = 7,
            VOLUME_IMP4 = 8,
            VOLUME_FLOW = 9,
            POWER = 10,
            TEMP_INP = 11,
            TEMP_OUTP = 12,
            TIME_ON = 13,
            TIME_ON_ERR = 14
        }

        public int getLengthAndTypeFromDIF(byte DIF, out RecordDataType type)
        {
            int data = DIF & 0x0F; //00001111b
            switch (data)
            {
                case 0:
                    {
                        type = RecordDataType.NO_DATA;
                        return 0;
                    }
                case 1:
                    {
                        type = RecordDataType.INTEGER;
                        return 1;
                    }
                case 2:
                    {
                        type = RecordDataType.INTEGER;
                        return 2;
                    }
                case 3:
                    {
                        type = RecordDataType.INTEGER;
                        return 3;
                    }
                case 4:
                    {
                        type = RecordDataType.INTEGER;
                        return 4;
                    }
                case 5:
                    {
                        WriteToLog("getLengthAndTypeFromDIF: 5, real");
                        type = RecordDataType.REAL;
                        return 4;
                    }
                case 6:
                    {
                        type = RecordDataType.INTEGER;
                        return 6;
                    }
                case 7:
                    {
                        type = RecordDataType.INTEGER;
                        return 8;
                    }
                case 8:
                    {
                        //selection for readout
                        WriteToLog("getLengthAndTypeFromDIF: 8, selection for readout");
                        type = RecordDataType.SELECTION_FOR_READOUT;
                        return 0;
                    }
                case 9:
                    {
                        type = RecordDataType.BCD;
                        return 1;
                    }
                case 10:
                    {
                        type = RecordDataType.BCD;
                        return 2;
                    }
                case 11:
                    {
                        type = RecordDataType.BCD;
                        return 3;
                    }
                case 12:
                    {
                        type = RecordDataType.BCD;
                        return 4;
                    }
                case 13:
                    {
                        WriteToLog("getLengthAndTypeFromDIF: 13, variable length");
                        type = RecordDataType.VARIABLE_LENGTH;
                        return -1;
                    }
                case 14:
                    {
                        type = RecordDataType.BCD;
                        return 6;
                    }
                case 15:
                    {
                        WriteToLog("getLengthAndTypeFromDIF: 15, special functions");
                        type = RecordDataType.SPECIAL_FUNСTIONS;
                        return -1;
                    }
                default:
                    {
                        type = RecordDataType.NO_DATA;
                        return -1;
                    }
            }
        }

        private bool getRecordValueByParam(Params param, List<Record> records, out float value)
        {
            if (records == null && records.Count == 0)
            {
                WriteToLog("getRecordValueByParam: список записей пуст");
                value = 0f;
                return false;
            }

            if ((int)param >= records.Count)
            {
                WriteToLog("getRecordValueByParam: параметра не существует в списке записей: " + param.ToString());
                value = 0f;
                return false;
            }

            Record record = records[(int)param];
            byte[] data = record.dataBytes.ToArray();
            Array.Reverse(data);
            string hex_str = BitConverter.ToString(data).Replace("-", string.Empty);

            int COEFFICIENT = 1;
            switch (param)
            {
                case Params.FACTORY_NUMBER:
                    {
                        break;
                    }
                case Params.ENERGY:
                    {
                        //коэффициент, согласно документации MBUS, после применения дает значение в KCal
                        COEFFICIENT = 10;
                        //однако, согласно документации elf, требуется представить в GCal
                        COEFFICIENT *= (int)Math.Pow(10, 6);
                        break;
                    }
                case Params.POWER:
                    {
                        COEFFICIENT = 10;
                        break;
                    }
                case Params.VOLUME:
                case Params.VOLUME_FLOW:
                    {
                        COEFFICIENT = 1000;
                        break;
                    }
                case Params.VOLUME_IMP1:
                case Params.VOLUME_IMP2:
                case Params.VOLUME_IMP3:
                case Params.VOLUME_IMP4:
                    {
                        
                        COEFFICIENT = 1000;
                        break;
                    }
                case Params.TEMP_INP:
                case Params.TEMP_OUTP:
                    {
                        COEFFICIENT = 10;
                        break;
                    }
                case Params.TIME_ON:
                case Params.TIME_ON_ERR:
                    {
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            if (!float.TryParse(hex_str, out value))
            {
                value = 0f;

                string mgs = String.Format("Ошибка преобразования параметра {0} во float, исходная строка: {1}", param.ToString(), hex_str);
                WriteToLog(mgs);

                return false;
            }
            else
            {
                value /= COEFFICIENT;
                return true;
            }
        }

        public bool getRecordValueByParam(Params param, out float value)
        {
            List<Record> records = new List<Record>();
            value = 0f;

            if (!GetRecordsList(out records))
            {
                WriteToLog("getRecordValueByParam: can't split records");
                return false;
            }

            float res_val = 0f;
            if (getRecordValueByParam(param, records, out res_val))
            {
                value = res_val;
                return true;
            }
            else
            {
                WriteToLog("getRecordValueByParam: can't getRecordValueByParam: " + Params.FACTORY_NUMBER.ToString());
                return false;
            }
        }

        public bool GetRecordsList(out List<Record> records)
        {
            records = new List<Record>();

            List<byte> answerBytes = new List<byte>();
            if (!SendREQ_UD2(out answerBytes) || answerBytes.Count == 0)
            {
                WriteToLog("ReadSerialNumber: не получены байты ответа");
                return false;
            }
           
            if (!SplitRecords(answerBytes, ref records) || records.Count == 0)
            {
                WriteToLog("ReadSerialNumber: не удалось разделить запись");
                return false;
            }

            return true;
        }

        //возвращает true если установлен extension bit, позволяет опрелелить, есть ли DIFE/VIFE
        private bool hasExtension(byte b)
        {
            byte EXTENSION_BIT_MASK = Convert.ToByte("10000000", 2);
            int extensionBit = (b & EXTENSION_BIT_MASK) >> 7;
            if (extensionBit == 1)
                return true;
            else
                return false;
        }

        public bool SplitRecords(List<byte> recordsBytes, ref List<Record> recordsList)
        {
            recordsList = new List<Record>();
            if (recordsBytes.Count == 0) return false;

            bool doStop = false;
            int index = 0;

            //переберем записи
            while (!doStop)
            {
                Record tmpRec = new Record();
                tmpRec.DIFEs = new List<byte>();
                tmpRec.VIFEs = new List<byte>();
                tmpRec.dataBytes = new List<byte>();

                tmpRec.DIF = recordsBytes[index];

                //определим длину и тип данных
                int dataLength = getLengthAndTypeFromDIF(tmpRec.DIF, out tmpRec.recordType);

                if (hasExtension(tmpRec.DIF))
                {
                    //переход к байту DIFE
                    index++;
                    byte DIFE = recordsBytes[index];
                    tmpRec.DIFEs.Add(DIFE);

                    while (hasExtension(DIFE))
                    {
                        //перейдем к следующему DIFE
                        index++;
                        DIFE = recordsBytes[index];
                        tmpRec.DIFEs.Add(DIFE);
                    }
                }

                //переход к VIF
                index++;
                tmpRec.VIF = recordsBytes[index];

                //проверим на наличие специального VIF, после которого следует ASCII строка
                if (tmpRec.VIF == Convert.ToByte("11111100", 2))
                {
                    index++;
                    int str_length = recordsBytes[index];
                    index += str_length;
                }

                if (hasExtension(tmpRec.VIF))
                {
                    //переход к VIFE
                    index++;
                    byte VIFE = recordsBytes[index];
                    tmpRec.VIFEs.Add(VIFE);

                    while (hasExtension(VIFE))
                    {
                        //перейдем к следующему VIFE
                        index++;
                        VIFE = recordsBytes[index];
                        tmpRec.VIFEs.Add(VIFE);
                    }
                }

                //переход к первому байту данных
                index++;
                int dataCnt = 0;
                while (dataCnt < dataLength)
                {
                    tmpRec.dataBytes.Add(recordsBytes[index]);
                    index++;
                    dataCnt++;
                }

                recordsList.Add(tmpRec);
                if (index >= recordsBytes.Count - 1) doStop = true;
            }

            return true;
        }
        public bool SendREQ_UD2(out List<byte> recordsBytesList)
        {
            recordsBytesList = new List<byte>();

            /*данные проходящие по протоколу m-bus не нужно шифровать, а также не нужно
             применять отрицание для зарезервированных символов*/
            byte cmd = 0x7b;
            byte CS = (byte)(cmd + m_addr);

            byte[] cmdArr = { 0x10, cmd, m_addr, CS, 0x16 };
            byte[] inp = new byte[256];

            try
            {
                //режим, когда незнаем сколько байт нужно принять
                m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, -1);

                WriteToLog(GetStringFromByteArray(inp));

                //if (inp.Length < 6)
                //{
                //    WriteToLog("SendREQ_UD2: Длина корректного ответа не может быть меньше 5 байт: " + answ_str);
                //    return false;
                //}

                int firstAnswerByteIndex = -1;
                int byteCIndex = -1;
                //определим индекс первого байта С
                for (int i = 0; i < inp.Length; i++)
                {
                    int j = i + 3;
                    if (inp[i] == 0x68 && j < inp.Length && inp[j] == 0x68)
                    {
                        firstAnswerByteIndex = i;
                        byteCIndex = ++j;
                    }
                }

                if (firstAnswerByteIndex == -1)
                {
                    WriteToLog("SendREQ_UD2: не определено начало ответа 0x68, firstAnswerByteIndex: " + firstAnswerByteIndex.ToString());
                    return false;
                }

                //определим длину данных ответа
                byte dataLength = inp[firstAnswerByteIndex + 1];
                if (dataLength != inp[firstAnswerByteIndex + 2])
                {
                    WriteToLog("SendREQ_UD2: не определена длина данных L, dataLength");
                    return false;
                }


                byte C = inp[byteCIndex];
                byte A = inp[byteCIndex + 1]; //адрес прибора 
                byte CI = inp[byteCIndex + 2]; //тип ответа, если 72h то с переменной длиной

                if (CI != 0x72)
                {
                    WriteToLog("SendREQ_UD2: счетчик должен ответить сообщением с переменной длиной, CI = 0x72");
                    return false;
                }

                int firstFixedDataHeaderIndex = byteCIndex + 3;
                byte[] factoryNumberBytes = new byte[4];
                Array.Copy(inp, firstFixedDataHeaderIndex, factoryNumberBytes, 0, factoryNumberBytes.Length);
                Array.Reverse(factoryNumberBytes);
                //серийный номер полученный из заголовка может быть изменен, достовернее серийник, полученный из блока записей
                string factoryNumber = BitConverter.ToString(factoryNumberBytes);

                //12 байт - размер заголовка, индекс первого байта первой записи
                int firstRecordByteIndex = firstFixedDataHeaderIndex + 12;

                //байт окончания сообщения
                int lastByteIndex = byteCIndex + dataLength + 1;
                int byteCSIndex = byteCIndex + dataLength;
                if (inp[lastByteIndex] != 0x16)
                {
                    WriteToLog("SendREQ_UD2: не найден байт окончания сообщения 0х16");
                    return false;
                }

                //индекс последнего байта последнегй записи
                int lastRecordByteIndex = lastByteIndex - 2;

                //поместим байты записей в отдельный список
                for (int i = firstRecordByteIndex; i <= lastRecordByteIndex; i++)
                    recordsBytesList.Add(inp[i]);

                return true;
            }
            catch (Exception ex)
            {
                WriteToLog("SendREQ_UD2: " + ex.Message);
                return false;
            }
        }

        public bool GetAllValues(out string res)
        {
            res = "Ошибка";
            List<Record> records = new List<Record>();
            if (!GetRecordsList(out records))
            {
                WriteToLog("GetAllValues: can't split records");
                return false;
            }

            res = "";
            foreach (Params p in Enum.GetValues(typeof(Params)))
            {
                float val = -1f;
                string s = "false;";

                if (getRecordValueByParam(p, records, out val))
                    s = val.ToString();

                res += String.Format("{0}: {1}\n", p.ToString(), s);
            }

            return true;
        }

        #endregion

        #region Протокол PT

        public bool SendPT01_CMD(byte[] outCmdBytes, ref byte[] data_arr, byte[] outCmdDataBytes = null)
        {
            List<byte> resCmdList = new List<byte>();

            bool isThereCmdData = false;
            if (outCmdDataBytes != null)
            {
                foreach (byte b in outCmdBytes)
                    if (b != 0x0)
                    {
                        isThereCmdData = true;
                        break;
                    }
            }

            resCmdList.Add(0x4d);

            byte crcn = CRC8(outCmdBytes, outCmdBytes.Length);

            List<byte> cmdTmpList = new List<byte>();
            cmdTmpList.AddRange(outCmdBytes);
            cmdTmpList.Add(crcn);

            List<byte> encrCmdWCSList = new List<byte>(cmdTmpList.Count);
            EncryptByteArr(cmdTmpList, ref encrCmdWCSList);

            CodeControlBytes(ref encrCmdWCSList);
            resCmdList.AddRange(encrCmdWCSList.ToArray());

            if (isThereCmdData)
            {
                byte crcd = CRC8(outCmdDataBytes, outCmdDataBytes.Length);

                List<byte> cmdDataTmp = new List<byte>();
                cmdDataTmp.AddRange(outCmdDataBytes);
                cmdDataTmp.Add(crcd);

                encrCmdWCSList = new List<byte>(cmdDataTmp.Count);
                EncryptByteArr(cmdDataTmp, ref encrCmdWCSList);
                CodeControlBytes(ref encrCmdWCSList);
                resCmdList.AddRange(encrCmdWCSList.ToArray());
            }

            resCmdList.Add(0x16);

            byte[] resCmd = resCmdList.ToArray();

            //максимальная предполагаемая длина ответа
            const int MAX_ANSWER_LENGTH = 200;
            data_arr = new byte[MAX_ANSWER_LENGTH];

            //если указать -1 в качестве ожидаемой длины ответа, длина ответа будет = длине принятых данных
            if (m_vport.WriteReadData(findPackageSign, resCmd, ref data_arr, resCmd.Length, -1) == 0) return false;

            List<byte> data_arr_list = new List<byte>();
            data_arr_list.AddRange(data_arr);

            //если начало правильное

            //все хорошо, служебная информация, обрабатывать (в этой версии) ничего не надо
            if (data_arr_list[0] == 0x53 && data_arr_list[data_arr_list.Count - 1] == 0x16)
            {
                byte[] usefullDataArr = new byte[data_arr_list.Count - 2];
                Array.Copy(data_arr, 1, usefullDataArr, 0, data_arr_list.Count - 2);
                DecryptByteArr(usefullDataArr, ref usefullDataArr);
                data_arr = usefullDataArr;
                return true;
            }
            else if (data_arr_list[0] == 0x4d && data_arr_list[data_arr_list.Count - 1] == 0x16)
            {
                //длина минимальной команды 6 байт
                //if (data_arr_list.Count < 6)
                //{
                //    data_arr = null;
                //    WriteToLog("SendPT01_CMD: корректный ответ не может быть меньше 6 байт по протоколу РТ");
                //    return false;
                //}

                if (data_arr.Length == resCmd.Length)
                {
                    data_arr = null;
                    WriteToLog("SendPT01_CMD: длина ответа совпадает с длиной команды");
                    return false;
                }

                //теперь необходимо оставить только полезные данные
                /*ответ состоит из:
                 1. Команда полностью с двумя контрольными символами
                 2. Контрольный символ начала ответа
                 3. Команда с новым числом данных и контрольной суммой
                 4. Данные с контрольной суммой
                 5. Контрольный символ завершения*/

                //индекс первого байта команды
                int fi = resCmd.Length + 1;
                //индекс первого байта полезных данных
                int se = fi + outCmdBytes.Length + 1;

                //определим кол-во байт полезных данных в ответе
                byte[] bCountArr = new byte[2];
                if ((fi + 2) > data_arr.Length - 1 || (fi + 4) > data_arr.Length - 1)
                {
                    WriteToLog("SendPT01_CMD: индекс за пределами массива 1");
                    return false;
                }

                Array.Copy(data_arr, fi + 2, bCountArr, 0, 2);
                DecodeControlBytes(bCountArr, ref bCountArr);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(bCountArr);

                DecryptByteArr(bCountArr, ref bCountArr);

                byte[] bCountArr2 = new byte[4];
                Array.Copy(bCountArr, bCountArr2, bCountArr.Length);
                //полезные данные без учета байта crc8
                int answerBytesCount = BitConverter.ToInt32(bCountArr2, 0);
                if (answerBytesCount >= data_arr.Length && answerBytesCount == 0)
                {
                    WriteToLog("SendPT01_CMD: неверно определено кол-во байт данных в ответе");
                    Array.Clear(data_arr, 0, data_arr.Length);
                    return false;
                }

                //+1 - учет байта контрольной суммы
                byte[] final_data_arr = new byte[answerBytesCount + 1];
                try
                {
                    if (se > data_arr.Length - 1)
                    {
                        WriteToLog("SendPT01_CMD: индекс за пределами массива 2");
                        return false;
                    }
                    Array.Copy(data_arr, se, final_data_arr, 0, answerBytesCount + 1);
                    DecodeControlBytes(final_data_arr, ref final_data_arr);
                    DecryptByteArr(final_data_arr, ref final_data_arr);
                    data_arr = final_data_arr;

                }
                catch (Exception ex)
                {
                    WriteToLog("SendPT01_CMD: " + ex.Message);
                    Array.Clear(data_arr, 0, data_arr.Length);
                    return false;
                }

                return true;
            }
            else
            {
                Array.Clear(data_arr, 0, data_arr.Length);
                WriteToLog("SendPT01_CMD: принятые данные некорректны");
                return false;
            }

        }

        public bool isControlByte(byte b)
        {
            byte[] control_bytes = { 0x4d, 0x53, 0x6e, 0x16, 0x10, 0x68 };
            if (Array.IndexOf(control_bytes, b) == -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void CodeControlBytes(ref List<byte> cmdBytesList)
        {
            List<byte> tmpList = new List<byte>();
            tmpList.AddRange(cmdBytesList);
            for (int i = 0; i < tmpList.Count; i++)
            {
                if (isControlByte(tmpList[i]))
                {
                    try
                    {
                        byte new_b = Convert.ToByte(~tmpList[i] & 0x000000FF);
                        byte[] repl = { 0x6e, new_b };
                        tmpList.RemoveAt(i);
                        tmpList.InsertRange(i, repl);
                    }
                    catch (Exception ex)
                    {
                        WriteToLog("CodeControlBytes: " + ex.Message);
                    }
                }
            }

            cmdBytesList = tmpList;
        }

        public void DecodeControlBytes(byte[] inpArr, ref byte[] outpArr)
        {
            List<byte> tmpList = new List<byte>();
            tmpList.AddRange(inpArr);
            for (int i = 0; i < tmpList.Count; i++)
            {
                if (isControlByte(tmpList[i]))
                {
                    try
                    {
                        byte new_b = Convert.ToByte(~tmpList[i + 1] & 0x000000FF);
                        byte[] repl = { new_b };
                        tmpList.RemoveAt(i);
                        tmpList.RemoveAt(i);
                        tmpList.InsertRange(i, repl);
                    }
                    catch (Exception ex)
                    {
                        WriteToLog("DecodeControlBytes: " + ex.Message);
                    }
                }
            }

            outpArr = tmpList.ToArray();
        }

        public void EncryptByteArr(byte[] inpArr, ref byte[] outpArr)
        {
            outpArr = new byte[inpArr.Length];
            for (int i = 0; i < inpArr.Length; i++)
            {
                byte mask = 0x03;
                byte b = inpArr[i];
                int part1 = (b & mask) << 6;
                int part2 = (b >> 2) | part1;
                byte res = (byte)~part2;
                outpArr[i] = res;
            }
        }

        public void EncryptByteArr(List<byte> inpArr, ref List<byte> outpArr)
        {
            outpArr = new List<byte>(inpArr.Count);
            for (int i = 0; i < inpArr.Count; i++)
            {
                byte mask = 0x03;
                byte b = inpArr[i];
                int part1 = (b & mask) << 6;
                int part2 = (b >> 2) | part1;
                byte res = (byte)~part2;
                outpArr.Add(res);
            }
        }

        public void DecryptByteArr(byte[] inpArr, ref byte[] outpArr)
        {
            outpArr = new byte[inpArr.Length];
            for (int i = 0; i < inpArr.Length; i++)
            {
                byte mask = 0xC0;
                byte b = (byte)(~inpArr[i]);
                int part1 = (b & mask) >> 6;
                int part2 = (b << 2) | part1;
                outpArr[i] = (byte)part2;
            }
        }

        #endregion

        #region Новые методы, введенные для совместимости с оболочкой

        bool bLogOutBytes = true;

        public bool ToBcd(int value, ref byte[] byteArr)
        {
            if (value < 0 || value > 99999999)
                return false;

            byte[] ret = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ret[i] = (byte)(value % 10);
                value /= 10;
                ret[i] |= (byte)((value % 10) << 4);
                value /= 10;
            }

            Array.Reverse(ret);
            byteArr = ret;

            return true;
        }

        //используется для вывода в лог
        public string current_secondary_id_str = "серийный номер не определен";
        //выделяет счетчик по серийнику и возвращает признак того что прибор на связи
        public bool SelectBySecondaryId(int factoryNumber)
        {
            current_secondary_id_str = factoryNumber.ToString();

            byte cmd = 0x53;
            byte CI = 0x52;

            byte[] addrArr = null;
            if (!ToBcd(factoryNumber, ref addrArr))
                return false;

            byte CS = (byte)(cmd + m_addr + CI + addrArr[3] + addrArr[2] + addrArr[1] + addrArr[0] + 0xFF + 0xFF + 0xFF + 0xFF);

            byte[] cmdArr = { 0x68, 0x0B, 0x0B, 0x68, cmd, m_addr, CI, addrArr[3], addrArr[2], addrArr[1], addrArr[0], 
                                0xFF, 0xFF , 0xFF,0xFF, CS, 0x16 };
            int firstRecordByteIndex = cmdArr.Length + 4 + 3 + 12;


            byte[] inp = new byte[512];
            try
            {
                int readBytes = m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, -1);
                for (int i = inp.Length - 1; i >= 0; i--)
                    if (inp[i] == 0xE5)
                    {
                        WriteToLog("SelectBySecondaryId: выбран счетчик " + current_secondary_id_str, bLogOutBytes);
                        return true;
                    }

                WriteToLog("SelectBySecondaryId: в ответе не найден байт подтверждения 0xE5 для счетчика " + current_secondary_id_str);
                return false;

            }
            catch (Exception ex)
            {
                WriteToLog("SelectBySecondaryId: " + ex.Message);
                return false;
            }
        }

        //сбрасывает выделение конкретного счптчика
        bool SND_NKE(ref bool confirmed)
        {
            byte cmd = 0x40;
            byte CS = (byte)(cmd + m_addr);

            byte[] cmdArr = { 0x10, cmd, m_addr, CS, 0x16 };
            int firstRecordByteIndex = cmdArr.Length + 4 + 3 + 12;

            byte[] inp = new byte[512];
            try
            {
                int readBytes = m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, -1);
                if (readBytes >= 1 && inp[readBytes - 1] == 0xE5)
                    confirmed = true;
                else
                    confirmed = false;

                WriteToLog("SND_NKE: деселекция", bLogOutBytes);

                return true;
            }
            catch (Exception ex)
            {
                WriteToLog("SND_NKE: " + ex.Message);
                return false;
            }

        }
        public bool UnselectAllMeters()
        {
            bool res = false;
            this.SND_NKE(ref res);

            return res;
        }

        /// <summary>
        /// Чтение списка текущих значений для драйвера теплоучет
        /// </summary>
        /// <param name="valDict"></param>
        /// <returns></returns>
        public bool ReadCurrentValues(List<int> paramCodes, out List<float> values)
        {
            values = new List<float>();
            List<Record> records = new List<Record>();
            List<byte> answerBytes = new List<byte>();

            if (!SendREQ_UD2(out answerBytes) || answerBytes.Count == 0)
            {
                WriteToLog("ReadCurrentValues: не получены байты ответа");
                return false;
            }

            //вывод в лог "сырых" байт, поступивших со счетчика
            if (bLogOutBytes)
            {
                string answBytesStr = String.Format("ReadCurrentValues, response:\n[{0}];", BitConverter.ToString(answerBytes.ToArray()).Replace("-", " "));
                WriteToLog(answBytesStr);
            }

            if (!SplitRecords(answerBytes, ref records) || records.Count == 0)
            {
                WriteToLog("ReadCurrentValues: не удалось разделить запись");
                return false;
            }

            //вывод в лог байт параметров, выделенных программой из "сырого" ответа
            if (bLogOutBytes)
            {
                string recordsStr = String.Empty;
                foreach (Record tR in records)
                    recordsStr += "[" + BitConverter.ToString(tR.dataBytes.ToArray()).Replace("-", " ") + "], ";

                string answBytesStr = String.Format("ReadCurrentValues, records:\n{0};", recordsStr);
                WriteToLog(answBytesStr);
            }

            foreach (int p in paramCodes)
            {
                float tmpVal = -1f;
                values.Add(tmpVal);

                if (!Enum.IsDefined(typeof(Params), p))
                {
                    WriteToLog("ReadCurrentValues не удалось найти в перечислении paramCodes параметр " + p.ToString());
                    continue;
                }

                Params tmpP = (Params)p;

                //не путать с перегруженным аналогом
                if (!getRecordValueByParam(tmpP, records, out tmpVal))
                {
                    WriteToLog("ReadCurrentValues не удалось выполнить getRecordValueByParam для " + tmpP);
                    continue;
                }

                values[values.Count - 1] = tmpVal;
            }

            return true;
        }


        #endregion

        /// <summary>
        /// Открытие канала связи (отправкой SND_NKE)
        /// </summary>
        /// <returns></returns>
        public bool OpenLinkCanal()
        {
            //SND_NKE
            byte cmd = 0x40;
            byte CS = (byte)(cmd + m_addr);

            byte[] cmdArr = { 0x10, cmd, m_addr, CS, 0x16 };
            byte[] inp = new byte[256];

            try
            {
                //режим, когда незнаем сколько байт нужно принять
                m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, -1);

                if (inp == null || inp.Length == 0)
                {
                    WriteToLog("Не получен ответ при чтении серийного номера");
                    return false;
                }

                if (inp[inp.Length - 1] == 0xE5)
                {
                    return true;
                }
                else
                {
                    WriteToLog("В ответе SND_NKE не найден подтверждающий байт 0xE5");
                    return false;
                }


            }
            catch (Exception ex)
            {
                WriteToLog("Ошибка при чтении серийного номера: " + ex.Message);
                return false;
            }
        }


        //Метод для совместимости с оболочкой. Также задает сетевой номер счетчика
        public bool OpenLinkCanal(byte meterLocalNumber)
        {
            //SND_NKE
            this.m_address = (uint)meterLocalNumber;
            this.m_addr = meterLocalNumber;

            byte cmd = 0x40;
            byte CS = (byte)(cmd + m_addr);

            byte[] cmdArr = { 0x10, cmd, m_addr, CS, 0x16 };
            byte[] inp = new byte[256];

            try
            {
                //режим, когда незнаем сколько байт нужно принять
                m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, -1);

                if (inp == null || inp.Length == 0)
                {
                    WriteToLog("Не получен ответ при чтении серийного номера");
                    return false;
                }

                if (inp[inp.Length - 1] == 0xE5)
                {
                    return true;
                }
                else
                {
                    string infoStr = String.Format("В ответе SND_NKE не найден подтверждающий байт 0xE5.\nПолучены байты: [0];", BitConverter.ToString(inp, 0).Replace('-', ' '));
                    WriteToLog(infoStr);
                    return false;
                }


            }
            catch (Exception ex)
            {
                WriteToLog("Ошибка при чтении серийного номера: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Чтение серийного номера устройства (из RSP_UD, а не из заголовка)
        /// </summary>
        /// <param name="serial_number">Возвращаемое значение</param>
        /// <returns></returns>
        public bool ReadSerialNumber(ref string serial_number)
        {
            List<Record> records = new List<Record>();
            if (!GetRecordsList(out records))
            {
                WriteToLog("ReadSerialNumber: can't split records");
                return false;
            }

            float res_val = 0f;
            if (getRecordValueByParam(Params.FACTORY_NUMBER, records, out res_val))
            {
                serial_number = ((int)res_val).ToString();
                return true;
            }
            else
            {
                WriteToLog("ReadSerialNumber: can't getRecordValueByParam: " + Params.FACTORY_NUMBER.ToString());
                return false;
            }
        }

        /// <summary>
        /// Чтение текущих значений
        /// </summary>
        /// <param name="values">Возвращаемые данные</param>
        /// <returns></returns>
        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            List<Record> records = new List<Record>();
            if (!GetRecordsList(out records))
            {
                WriteToLog("ReadCurrentValues: can't get records list");
                return false;
            }

            switch (param)
            {
                case 1:
                    {
                        return getRecordValueByParam(Params.ENERGY, records, out recordValue);
                    }
                case 2:
                    {
                        return getRecordValueByParam(Params.VOLUME, records, out recordValue);
                    }
                case 3:
                    {
                        return getRecordValueByParam(Params.TIME_ON, records, out recordValue);
                    }
                case 4:
                    {
                        return getRecordValueByParam(Params.TIME_ON_ERR, records, out recordValue);
                    }
                case 5:
                    {
                        Params tmp_param = Params.TEMP_INP;
                        switch (tarif)
                        {
                            case 1: { tmp_param = Params.TEMP_OUTP; break; }
                            default:
                                {
                                    tmp_param = Params.TEMP_INP;
                                    break;
                                }
                        }

                        return getRecordValueByParam(tmp_param, records, out recordValue);
                    }
                case 6:
                    {
                        Params tmp_param = Params.VOLUME_IMP1;
                        switch (tarif)
                        {
                            case 2: { tmp_param = Params.VOLUME_IMP2; break; }
                            case 3: { tmp_param = Params.VOLUME_IMP3; break; }
                            case 4: { tmp_param = Params.VOLUME_IMP4; break; }
                            default:
                                {
                                    tmp_param = Params.VOLUME_IMP1;
                                    break;
                                }
                        }

                        return getRecordValueByParam(tmp_param, records, out recordValue);
                    }
            
                default:
                    {
                        WriteToLog("ReadCurrentValues: для параметра " + param.ToString() + " нет обработчика");
                        return false;
                    }
            }
        }

        #region Расчет контрольной суммы
        // CRC-8 for Dallas iButton products from Maxim/Dallas AP Note 27
        readonly byte[] crc8Table = new byte[]
        {
            0x00, 0x5E, 0xBC, 0xE2, 0x61, 0x3F, 0xDD, 0x83,
            0xC2, 0x9C, 0x7E, 0x20, 0xA3, 0xFD, 0x1F, 0x41,
            0x9D, 0xC3, 0x21, 0x7F, 0xFC, 0xA2, 0x40, 0x1E,
            0x5F, 0x01, 0xE3, 0xBD, 0x3E, 0x60, 0x82, 0xDC,
            0x23, 0x7D, 0x9F, 0xC1, 0x42, 0x1C, 0xFE, 0xA0,
            0xE1, 0xBF, 0x5D, 0x03, 0x80, 0xDE, 0x3C, 0x62,
            0xBE, 0xE0, 0x02, 0x5C, 0xDF, 0x81, 0x63, 0x3D,
            0x7C, 0x22, 0xC0, 0x9E, 0x1D, 0x43, 0xA1, 0xFF,
            0x46, 0x18, 0xFA, 0xA4, 0x27, 0x79, 0x9B, 0xC5,
            0x84, 0xDA, 0x38, 0x66, 0xE5, 0xBB, 0x59, 0x07,
            0xDB, 0x85, 0x67, 0x39, 0xBA, 0xE4, 0x06, 0x58,
            0x19, 0x47, 0xA5, 0xFB, 0x78, 0x26, 0xC4, 0x9A,
            0x65, 0x3B, 0xD9, 0x87, 0x04, 0x5A, 0xB8, 0xE6,
            0xA7, 0xF9, 0x1B, 0x45, 0xC6, 0x98, 0x7A, 0x24,
            0xF8, 0xA6, 0x44, 0x1A, 0x99, 0xC7, 0x25, 0x7B,
            0x3A, 0x64, 0x86, 0xD8, 0x5B, 0x05, 0xE7, 0xB9,
            0x8C, 0xD2, 0x30, 0x6E, 0xED, 0xB3, 0x51, 0x0F,
            0x4E, 0x10, 0xF2, 0xAC, 0x2F, 0x71, 0x93, 0xCD,
            0x11, 0x4F, 0xAD, 0xF3, 0x70, 0x2E, 0xCC, 0x92,
            0xD3, 0x8D, 0x6F, 0x31, 0xB2, 0xEC, 0x0E, 0x50,
            0xAF, 0xF1, 0x13, 0x4D, 0xCE, 0x90, 0x72, 0x2C,
            0x6D, 0x33, 0xD1, 0x8F, 0x0C, 0x52, 0xB0, 0xEE,
            0x32, 0x6C, 0x8E, 0xD0, 0x53, 0x0D, 0xEF, 0xB1,
            0xF0, 0xAE, 0x4C, 0x12, 0x91, 0xCF, 0x2D, 0x73,
            0xCA, 0x94, 0x76, 0x28, 0xAB, 0xF5, 0x17, 0x49,
            0x08, 0x56, 0xB4, 0xEA, 0x69, 0x37, 0xD5, 0x8B,
            0x57, 0x09, 0xEB, 0xB5, 0x36, 0x68, 0x8A, 0xD4,
            0x95, 0xCB, 0x29, 0x77, 0xF4, 0xAA, 0x48, 0x16,
            0xE9, 0xB7, 0x55, 0x0B, 0x88, 0xD6, 0x34, 0x6A,
            0x2B, 0x75, 0x97, 0xC9, 0x4A, 0x14, 0xF6, 0xA8,
            0x74, 0x2A, 0xC8, 0x96, 0x15, 0x4B, 0xA9, 0xF7,
            0xB6, 0xE8, 0x0A, 0x54, 0xD7, 0x89, 0x6B, 0x35
        };

        public byte CRC8(byte[] bytes, int len)
        {
            byte crc = 0;
            for (var i = 0; i < len; i++)
                crc = crc8Table[crc ^ bytes[i]];

            //byte[] crcArr = new byte[1];
            // crcArr[0] = crc;
            //MessageBox.Show(BitConverter.ToString(crcArr));
            return crc;
        }

        #endregion


        public bool ChangeImpulseInputDefaultValue(int inputId, int inputValue)
        {
            byte[] cmd = { m_addr, 0x32, 0x0a, 0x00 };
            byte[] inputValueBytes = BitConverter.GetBytes(inputValue);
            byte[] inputValues5BytesArr = new byte[5];
            for (int i = 0; i < inputValueBytes.Length; i++)
                inputValues5BytesArr[i] = inputValueBytes[i];
            byte[] cmd_data = { (byte)inputId, 0x0, 0x0, 0x0, 0x0, inputValues5BytesArr[0], inputValues5BytesArr[1], inputValues5BytesArr[2], inputValues5BytesArr[3], inputValues5BytesArr[4] };

            byte[] data_arr = new byte[1];
            if (!SendPT01_CMD(cmd, ref data_arr, cmd_data)) return false;


            byte crc_check = CRC8(data_arr, data_arr.Length);
            if (crc_check != 0x0)
            {
                WriteToLog("ChangeImpulseInputDefaultValue: данные приняты неверно");
                return false;
            }

            return true;
        }

        public bool ChangeImpulseInputsValPrice(int inp1, int inp2)
        {
            byte[] cmd = { m_addr, 0x2b, 0x35, 0x00 };
            byte[] input1ValueBytes = BitConverter.GetBytes(inp1);
            byte[] input2ValueBytes = BitConverter.GetBytes(inp2);
            byte[] i1_4BArr = new byte[4];
            byte[] i2_4BArr = new byte[4];

            for (int i = 0; i < input1ValueBytes.Length; i++)
                i1_4BArr[i] = input1ValueBytes[i];
            for (int i = 0; i < input2ValueBytes.Length; i++)
                i2_4BArr[i] = input2ValueBytes[i];

            byte[] cmd_data = { 
                0x01, 0x0, 0x0, 0x01,
                0x00, 0x0, 0x0, 0x01,
                0x00, 0x0, 0x0, 0x01,
                0x00, 0x0, 0x0, 0x01,
                0x00, 0x0, 0x0, 0x00,
                i1_4BArr[0], i1_4BArr[1], i1_4BArr[2], i1_4BArr[3], 
                i2_4BArr[0], i2_4BArr[1], i2_4BArr[2], i2_4BArr[3],
                                0x00, 0x0, 0x0, 0x01,
                                                0x00, 0x0, 0x0, 0x01,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0
           };

            byte[] data_arr = new byte[1];
            if (!SendPT01_CMD(cmd, ref data_arr, cmd_data)) return false;

            byte crc_check = CRC8(data_arr, data_arr.Length);
            if (crc_check != 0x0)
            {
                WriteToLog("ReadLastArchiveVal: данные приняты неверно");
                return false;
            }

            return true;
        }


        public bool ReadArchiveLastVal(ref ArchiveValue archVal)
        {
            byte[] cmd = { m_addr, 0x2e, 0x02, 0x00 };
            byte[] cmd_data = { 0x02, 0x01 };

            byte[] data_arr = null;

            if (!SendPT01_CMD(cmd, ref data_arr, cmd_data)) return false;

            byte crc_check = CRC8(data_arr, data_arr.Length);
            if (crc_check != 0x0)
            {
                WriteToLog("ReadLastArchiveVal: данные приняты неверно");
                return false;
            }

            try
            {
                WriteToLog("data_arr.length = " + data_arr.Length.ToString());
                ArchiveValueParser avp = new ArchiveValueParser(data_arr);
                WriteToLog("avp.toStr = " + avp.ToString());

                avp.GetArchiveValue(ref archVal);
            }
            catch (Exception ex)
            {
                WriteToLog("ReadArchiveLastVal: " + ex.Message);
                return false;
            }

            return true;
        }

        public bool ReadArchiveValById(uint id, ref ArchiveValue archVal)
        {
            byte[] cmd = { m_addr, 0x2f, 0x05, 0x00 };
            byte[] cmd_data = new byte[0x05];

            //преобразуем целочисленные id в посл.байт от младшему к старшему
            byte[] id_bytes = BitConverter.GetBytes(id);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(id_bytes);

            //сформируем команду, где первый байт 0х02 - на начало суток (С2)
            cmd_data[0] = 0x02;
            Array.Copy(id_bytes, 0, cmd_data, 1, id_bytes.Length);


            byte[] data_arr = null;

            if (!SendPT01_CMD(cmd, ref data_arr, cmd_data)) return false;

            byte crc_check = CRC8(data_arr, data_arr.Length);
            if (crc_check != 0x0)
            {
                WriteToLog("ReadArchiveValById: check sum error");
                return false;
            }

            try
            {
                ArchiveValueParser avp = new ArchiveValueParser(data_arr);
                return avp.GetArchiveValue(ref archVal);
            }
            catch(Exception ex)
            {
                WriteToLog("ReadArchiveValById: " + ex.Message);
                return false;
            }
        }

        public bool ReadArchiveValCountId(ref int valuesCount, ref int lastValId)
        {
            byte[] cmd = { m_addr, 0x2e, 0x02, 0x00 };
            byte[] cmd_data = { 0x02, 0x00 };

            byte last_id_index = 0;
            byte[] last_id_bytes_arr = new byte[2];

            byte val_count_index = 2;
            byte[] val_count_bytes_arr = new byte[4];

            byte[] data_arr = new byte[24];
            if (!SendPT01_CMD(cmd, ref data_arr, cmd_data)) return false;

            byte crc_check = CRC8(data_arr, data_arr.Length);
            if (crc_check != 0x0)
            {
                WriteToLog("ReadArchiveValCountId: данные приняты неверно");
                return false;
            }

            Array.Copy(data_arr, last_id_index, last_id_bytes_arr, 0, last_id_bytes_arr.Length);
            Array.Copy(data_arr, val_count_index, val_count_bytes_arr, 0, val_count_bytes_arr.Length);

            /*Во всех запросах кроме запросов на получение архивов используется формат данных LSB-MSB*/
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(last_id_bytes_arr);
                Array.Reverse(val_count_bytes_arr);
            }

            try
            {
                /*TODO: проверить правильность преобразования больших чисел, уяснить разницу
                 между преобразованием в инт и уинт*/
                lastValId = (int)BitConverter.ToUInt32(val_count_bytes_arr, 0);
                valuesCount = (int)BitConverter.ToUInt16(last_id_bytes_arr, 0);

            }
            catch (Exception ex)
            {
                WriteToLog("ReadLastArchiveVal: ошибка при разборе ответа - " + ex.Message);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Преобразует дату в идентификатор архивной записи и возвращает значение в соответствии с 
        /// указанным param. Правильное преобразование не гарантируется. 
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="param"></param>
        /// <param name="tarif"></param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            ArchiveValue resArchVal = new ArchiveValue();
            ArchiveValue lastArchiveVal = new ArchiveValue();
            lastArchiveVal.id = -1;

            int recCount = -1, lastRecId = -1;

            if (ReadArchiveValCountId(ref recCount, ref lastRecId))
            {
                WriteToLog("case1: ReadArchiveValCountId");
                if (ReadArchiveValById((uint)lastRecId, ref lastArchiveVal))
                    goto SUCCESS;
            }


            if (ReadArchiveLastVal(ref lastArchiveVal))
            {
                WriteToLog("case2: ReadArchiveLastVal");
                goto SUCCESS;
            }

            if (dt.Date == DateTime.Now.Date && DateTime.Now.Hour >= 16)
            {
                if (ReadCurrentValues(param, tarif, ref recordValue))
                    return true;
            }


            return false;


        SUCCESS:
            int lastid = lastArchiveVal.id;
            if (lastid == -1) return false;

            DateTime lastRecDt = lastArchiveVal.dt;

            if (dt.Date > lastRecDt.Date)
            {
                WriteToLog("ReadDailyValues: на указанную дату записей не обнаружено: " + dt.ToShortDateString());
                return false;
            }

            WriteToLog("ReadDailyValues: lastRecDt: " + lastRecDt.ToShortDateString());
            WriteToLog("ReadDailyValues: lastId: " + lastid.ToString());
            WriteToLog("ReadDailyValues: requiredDt: " + dt.ToShortDateString());

            //преобразуем dt в id
            TimeSpan ts = lastRecDt.Date - dt.Date;
            if (ts.TotalDays == 0)
            {
                resArchVal = lastArchiveVal;
            }
            else
            {
                uint resRecId = (uint)(lastid - ts.TotalDays);
                WriteToLog("ReadDailyValues: requiredId: " + resRecId.ToString());
                try
                {
                    if (!ReadArchiveValById(resRecId, ref resArchVal))
                    {
                        string str = String.Format("ReadDailyValues: запись от числа {0} c id {1} не найдена",
                            dt.ToShortDateString(), resRecId.ToString());
                        WriteToLog(str);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    WriteToLog("ReadDailyValues: ReadArchiveValById ex: " + ex.Message);
                }
            }

            switch (param)
            {
                case 1: 
                    {
                        recordValue = resArchVal.energy;
                        break; 
                    }
                case 2:
                    {
                        recordValue = resArchVal.volume;
                        break;
                    }
                case 3:
                    {
                        recordValue = resArchVal.timeOn;
                        break;
                    }
                case 4:
                    {
                        recordValue = resArchVal.timeErr;
                        break;
                    }
                default :
                    {
                        WriteToLog("ReadDailyValues: для параметра " + param.ToString() + " нет обработчика");
                        return false;
                    }

            }

            return true;
        }

        /// <summary>
        /// Возвращает значение архивной записи в соответствии с указанным param.
        /// </summary>
        /// <param name="recordId">Идентификатор записи</param>
        /// <param name="param">Определяет тип параметра: энергия, объем и т.д.</param>
        /// <param name="tarif">не используется</param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        public bool ReadDailyValues(uint recordId, ushort param, ushort tarif, ref float recordValue)
        {
            ArchiveValue resArchVal = new ArchiveValue();
            if (!ReadArchiveValById(recordId, ref resArchVal))
            {
                WriteToLog("ReadDailyValues не удалось прочитать запись с id = " + recordId.ToString());
                return false;
            }

            switch (param)
            {
                case 0:
                    {
                        recordValue = resArchVal.energy;
                        break;
                    }
                case 1:
                    {
                        recordValue = resArchVal.volume;
                        break;
                    }
                case 2:
                    {
                        recordValue = resArchVal.timeOn;
                        break;
                    }
                case 3:
                    {
                        recordValue = resArchVal.timeErr;
                        break;
                    }
                default:
                    {
                        WriteToLog("ReadDailyValues: для параметра " + param.ToString() + " нет обработчика");
                        return false;
                    }
            }

            return true;          
        }

        #region Неиспользуемые методы интерфейса

        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            return false;
        }
        public bool SyncTime(DateTime dt)
        {
            return false;
        }
        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }
        int findPackageSign(Queue<byte> queue)
        {
            return 0;
        }

        #endregion

    }

    public class ArchiveValueParser : elf108
    {
        #region Объявления

        byte id_index = 0;
        byte[] id_bytes_arr = new byte[4];

        byte date_index = 4;
        byte[] date_bytes_arr = new byte[5];

        byte energy_index = 9;
        byte[] energy_bytes_arr = new byte[4];

        byte vol_index = 13;
        byte[] vol_bytes_arr = new byte[4];

        byte time_on_index = 17;
        byte[] time_on_bytes_arr = new byte[4];

        byte time_err_index = 21;
        byte[] time_err_bytes_arr = new byte[4];

        #endregion

        byte crc_check = 0xFF;
        bool isOk = false;

        ArchiveValue archVal;

        public ArchiveValueParser(byte[] d_array)
        {
            WriteToLog("ArchiveValueParser - constructor start");
            crc_check = CRC8(d_array, d_array.Length);
            if (crc_check == 0x0)
            {

                try
                {
                    Array.Copy(d_array, id_index, id_bytes_arr, 0, id_bytes_arr.Length);
                    Array.Copy(d_array, date_index, date_bytes_arr, 0, date_bytes_arr.Length);
                    Array.Copy(d_array, energy_index, energy_bytes_arr, 0, energy_bytes_arr.Length);
                    Array.Copy(d_array, vol_index, vol_bytes_arr, 0, vol_bytes_arr.Length);
                    Array.Copy(d_array, time_on_index, time_on_bytes_arr, 0, time_on_bytes_arr.Length);
                    Array.Copy(d_array, time_err_index, time_err_bytes_arr, 0, time_err_bytes_arr.Length);

                    /*Во всех запросах кроме запросов на получение архивов используется формат данных LSB-MSB*/
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(id_bytes_arr);
                        Array.Reverse(energy_bytes_arr);
                        Array.Reverse(vol_bytes_arr);
                        Array.Reverse(time_on_bytes_arr);
                        Array.Reverse(time_err_bytes_arr);
                    }
                    //id записи
                    archVal.id = BitConverter.ToInt32(id_bytes_arr, 0);

                    //разбор даты [15 [14 05] 01 02] 01-02-15 14:05
                    string hexValue = date_bytes_arr[0].ToString("X");
                    int year = Convert.ToByte(hexValue, 16) + 2000;
                    hexValue = date_bytes_arr[1].ToString("X");
                    int hours = Convert.ToByte(hexValue, 16);
                    hexValue = date_bytes_arr[2].ToString("X");
                    int minutes = Convert.ToByte(hexValue, 16);
                    hexValue = date_bytes_arr[3].ToString("X");
                    int day = Convert.ToByte(hexValue, 16);
                    hexValue = date_bytes_arr[4].ToString("X");
                    int month = Convert.ToByte(hexValue, 16);
                    archVal.dt = new DateTime(year, month, day, hours, minutes, 0);

                    //разбор ресурсов
                    archVal.energy = ((float)BitConverter.ToUInt32(energy_bytes_arr, 0) / 1000);
                    archVal.volume = ((float)BitConverter.ToUInt32(vol_bytes_arr, 0) / 1000);

                    //разбор времени работы и вр.работы с ошибкой соответственно
                    archVal.timeOn = BitConverter.ToInt32(time_on_bytes_arr, 0);
                    archVal.timeErr = BitConverter.ToInt32(time_err_bytes_arr, 0);

                    isOk = true;
                }
                catch (Exception ex)
                {
                    isOk = false;
                    WriteToLog("ArchiveValueReader: Ошибка разбора значений");
                }
            }
            else
            {
                WriteToLog("ArchiveValueReader: Ошибка подсчета контрольной суммы");
            }
        }

        public override string ToString()
        {
            if (isOk)
                return String.Format("id: {0}, datetime: {1}, energy(Gcal): {2}, volume(m3): {3}; " +
                    "timeOn: {4}, timeOnErr: {5}", archVal.id, archVal.dt.ToString(), archVal.energy, archVal.volume,
                    archVal.timeOn, archVal.timeErr);
            else
                return "Запись не существует";
        }

        public bool GetArchiveValue(ref ArchiveValue archVal)
        {
            if (isOk)
            {
                archVal = this.archVal;
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
