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
    public class elf108 : CMeter, IMeter
    {
        public void Init(uint address, string pass, VirtualPort vp)
        {
            this.m_address = address;
            this.m_addr = (byte)(this.m_address & 0x000000ff);
            this.m_vport = vp;

        }

        private byte m_addr = 0x0;

        //private SerialPort m_port;

        const int REQ_UD2_HEADER_SIZE = 24;
        const int REQ_UD2_DATA_SIZE = 97;
        const int REQ_UD2_ANSW_SIZE = 123;

        #region Константы для разобра ответа REQ_UD2

        const byte FACTORY_NUMBER_INDEX = 0;
        const byte FACTORY_NUMBER_CMD = 2;
        const byte FACTORY_NUMBER_SIZE = 6;

        const byte DATE_CMD = 2;
        const byte DATE_INDEX = 6;
        const byte DATE_SIZE = 4;

        const byte ENERGY_INDEX = 14;
        const byte ENERGY_SIZE = 14;
        const byte ENERGY_CMD = 8;

        const byte ERROR_CODE_INDEX = 10;
        const byte ERROR_CODE_SIZE = 4;
        const byte ERROR_CODE_CMD = 3;

        /**/
        const byte VOLUME_INDEX = 28;
        const byte VOLUME_SIZE = 6;
        const byte VOLUME_CMD = 2;

        const byte TEMP_INP_INDEX = 71;
        const byte TEMP_INP_SIZE = 4;
        const byte TEMP_INP_CMD = 2;

        const byte TEMP_OUTP_INDEX = 75;
        const byte TEMP_OUTP_SIZE = 4;
        const byte TEMP_OUTP_CMD = 2;

        const byte TIME_ON_INDEX = 79;
        const byte TIME_ON_SIZE = 6;
        const byte TIME_ON_CMD = 2;

        #endregion


        bool SendREQ_UD2(ref byte[] data_arr)
        {
            /*данные проходящие по протоколу m-bus не нужно шифровать, а также не нужно
             применять отрицание для зарезервированных символов*/
            byte cmd = 0x7b;
            byte CS = (byte)(cmd + m_addr);

            byte[] cmdArr = { 0x10, cmd, m_addr, CS, 0x16 };
            byte[] inp = new byte[REQ_UD2_ANSW_SIZE];

            try
            {
                byte[] data = new byte[97];
                m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, inp.Length);
                if (inp[0] == 0x10 && inp[inp.Length - 1] == 0x16)
                {
                    Array.Copy(inp, REQ_UD2_HEADER_SIZE, data, 0, REQ_UD2_DATA_SIZE);
                    //WriteToLog(BitConverter.ToString(data));
                    data_arr = data;
                    return true;
                }
                else
                {
                    WriteToLog("SendREQ_UD2: принятые данные некорректны");
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteToLog("SendREQ_UD2: " + ex.Message);
                return false;
            }
        }

        int findPackageSign(Queue<byte> queue)
        {
            return 0;
        }

        bool SendPT01_CMD(byte[] outCmdBytes, ref byte[] data_arr, byte[] outCmdDataBytes = null)
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
            byte[] cmdTmp = new byte[outCmdBytes.Length + 1];
            Array.Copy(outCmdBytes, cmdTmp, outCmdBytes.Length);
            cmdTmp[cmdTmp.Length - 1] = crcn;
            byte[] encrCmdWCS = new byte[cmdTmp.Length];
            EncryptByteArr(cmdTmp, ref encrCmdWCS);
            CodeControlBytes(encrCmdWCS, ref encrCmdWCS);
            resCmdList.AddRange(encrCmdWCS);

            if (isThereCmdData)
            {
                byte crcd = CRC8(outCmdDataBytes, outCmdDataBytes.Length);
                cmdTmp = new byte[outCmdDataBytes.Length + 1];
                Array.Copy(outCmdDataBytes, cmdTmp, outCmdDataBytes.Length);
                cmdTmp[cmdTmp.Length - 1] = crcd;

                byte[] encrCmdDataWCS = new byte[cmdTmp.Length];
                EncryptByteArr(cmdTmp, ref encrCmdDataWCS);
                CodeControlBytes(encrCmdDataWCS, ref encrCmdDataWCS);
                resCmdList.AddRange(encrCmdDataWCS);
            }

            resCmdList.Add(0x16);

            byte[] resCmd = resCmdList.ToArray();

            //максимальная предполагаемая длина ответа
            const int MAX_ANSWER_LENGTH = 200;
            data_arr = new byte[MAX_ANSWER_LENGTH];

            //если указать -1 в качестве ожидаемой длины ответа, длина ответа будет = длине принятых данных
            if (m_vport.WriteReadData(findPackageSign, resCmd, ref data_arr, resCmd.Length, -1) == 0) return false;
            //if (!sport_manager.WriteReadData(resCmd, ref data_arr)) return false;

            List<byte> data_arr_list = new List<byte>();
            data_arr_list.AddRange(data_arr);

            //если начало правильное
            if (data_arr_list[0] == 0x4d && data_arr_list[data_arr_list.Count - 1] == 0x16)
            {
                //длина минимальной команды 6 байт
                if (data_arr_list.Count < 6)
                {
                    data_arr = null;
                    WriteToLog("SendPT01_CMD: корректный ответ не может быть меньше 6 байт по протоколу РТ");
                    return false;
                }

                Array.Copy(data_arr_list.ToArray(), 0, data_arr, 0, data_arr_list.Count);

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

        bool isControlByte(byte b)
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

        void CodeControlBytes(byte[] inpArr, ref byte[] outpArr)
        {
            List<byte> tmpList = new List<byte>();
            tmpList.AddRange(inpArr);
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

            outpArr = tmpList.ToArray();
        }

        void DecodeControlBytes(byte[] inpArr, ref byte[] outpArr)
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

        void EncryptByteArr(byte[] inpArr, ref byte[] outpArr)
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

        void DecryptByteArr(byte[] inpArr, ref byte[] outpArr)
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

        /// <summary>
        /// Чтение серийного номера устройства
        /// </summary>
        /// <param name="serial_number">Возвращаемое значение</param>
        /// <returns></returns>
        public bool ReadSerialNumber(ref string serial_number)
        {
            byte[] data = null;
            if (SendREQ_UD2(ref data))
            {
                try
                {
                    byte[] serialNumbBytes = new byte[FACTORY_NUMBER_SIZE];
                    Array.Copy(data, FACTORY_NUMBER_INDEX, serialNumbBytes, 0, serialNumbBytes.Length);
                    Array.Reverse(serialNumbBytes, FACTORY_NUMBER_CMD, serialNumbBytes.Length - FACTORY_NUMBER_CMD);
                    serial_number = BitConverter.ToString(serialNumbBytes, FACTORY_NUMBER_CMD).Replace("-", string.Empty);

                    string outp_str = "Factory number: " + serial_number;
                    WriteToLog(outp_str);

                    return true;
                }
                catch (Exception ex)
                {
                    WriteToLog("ReadSerialNumber: err: " + ex.Message);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool OpenLinkCanal()
        {
            string sn = "";
            if (ReadSerialNumber(ref sn))
            {
                return true;
            }
            else
            {
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
            try
            {
                switch (param)
                {
                    case 1: return ReadCurrentEnergy(tarif, ref recordValue);
                    case 2: return ReadCurrentVolume(tarif, ref recordValue);
                    case 3: return ReadTimeOn(tarif, ref recordValue);
                    case 4: return ReadErrorCode(ref recordValue);
                    case 5: return ReadCurrentTemperature(tarif, ref recordValue);

                    default:
                        {
                            WriteToLog("ReadCurrentValues: для параметра " + param.ToString() + " нет обработчика");
                            return false;
                        }
                }
            }
            catch (Exception ex)
            {
                WriteToLog("ReadCurrentValues: exception: " + ex.Message, true);
                return false;
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

        bool ReadCurrentEnergy(ushort tarif, ref float value)
        {
            byte[] data = null;
            if (SendREQ_UD2(ref data))
            {
                /*энергия записана в 6ти кодебайтах в hex-dec*/
                byte[] energyBytes = new byte[ENERGY_SIZE];
                Array.Copy(data, ENERGY_INDEX, energyBytes, 0, ENERGY_SIZE);
                Array.Reverse(energyBytes, ENERGY_CMD, energyBytes.Length - ENERGY_CMD);

                string hex_str = BitConverter.ToString(energyBytes, ENERGY_CMD).Replace("-", string.Empty);
                const int ENERGY_COEFFICIENT = 10000;
                float temp_val = (float)Convert.ToDouble(hex_str) / ENERGY_COEFFICIENT;

                /*TODO: проверить ковертацию float в double*/
                string outp_str = "Energy GCal: " + temp_val.ToString();
                WriteToLog(outp_str);
                value = temp_val;

                return true;
            }
            else
            {
                return false;
            }
        }

        bool ReadCurrentVolume(ushort tarif, ref float value)
        {
            byte[] data = null;
            if (SendREQ_UD2(ref data))
            {
                /*объем записан в 4х байтах в hex-dec*/
                byte[] volumeBytes = new byte[VOLUME_SIZE];
                Array.Copy(data, VOLUME_INDEX, volumeBytes, 0, VOLUME_SIZE);
                Array.Reverse(volumeBytes, VOLUME_CMD, volumeBytes.Length - VOLUME_CMD);

                string hex_str = BitConverter.ToString(volumeBytes, VOLUME_CMD).Replace("-", string.Empty);

                const int VOLUME_COEFFICIENT = 1000;
                float temp_val = (float)Convert.ToDouble(hex_str) / VOLUME_COEFFICIENT;


                string outp_str = "Volume m3: " + temp_val.ToString();
                WriteToLog(outp_str);
                value = temp_val;

                return true;
            }
            else
            {
                return false;
            }
        }

        bool ReadCurrentTemperature(ushort tarif, ref float value)
        {
            byte[] data = null;
            if (SendREQ_UD2(ref data))
            {

                byte temp_index = 0;
                byte temp_size = 0;
                byte temp_cmd = 0;

                switch (tarif)
                {
                    case 0:
                        {
                            temp_index = TEMP_INP_INDEX;
                            temp_cmd = TEMP_INP_CMD;
                            temp_size = TEMP_INP_SIZE;
                            break;
                        }
                    case 1:
                        {
                            temp_index = TEMP_OUTP_INDEX;
                            temp_cmd = TEMP_OUTP_CMD;
                            temp_size = TEMP_OUTP_SIZE;
                            break;
                        }
                    default:
                        {
                            WriteToLog("Некорректное значение tarif");
                            return false;
                        }
                }

                /*температура записана в 2х байтах в hex-dec*/
                byte[] temperatureBytes = new byte[temp_size];
                Array.Copy(data, temp_index, temperatureBytes, 0, temp_size);
                Array.Reverse(temperatureBytes, temp_cmd, temperatureBytes.Length - temp_cmd);

                string hex_str = BitConverter.ToString(temperatureBytes, temp_cmd).Replace("-", string.Empty);

                const int TEMP_COEFFICIENT = 10;
                float temp_val = (float)Convert.ToDouble(hex_str) / TEMP_COEFFICIENT;

                string outp_str = "Temp " + tarif.ToString() + " (m3): " + temp_val.ToString();
                WriteToLog(outp_str);
                value = temp_val;

                return true;
            }
            else
            {
                return false;
            }
        }

        bool ReadTimeOn(ushort tarif, ref float value)
        {
            byte[] data = null;
            if (SendREQ_UD2(ref data))
            {
                /*время работы записано в 4х байтах в hex-dec*/
                byte[] timeonBytes = new byte[TIME_ON_SIZE];
                Array.Copy(data, TIME_ON_INDEX, timeonBytes, 0, TIME_ON_SIZE);
                Array.Reverse(timeonBytes, TIME_ON_CMD, timeonBytes.Length - TIME_ON_CMD);

                string hex_str = BitConverter.ToString(timeonBytes, TIME_ON_CMD).Replace("-", string.Empty);

                const int TIMEON_COEFFICIENT = 1;
                float temp_val = (float)Convert.ToDouble(hex_str) / TIMEON_COEFFICIENT;


                string outp_str = "TimeOn (h): " + temp_val.ToString();
                WriteToLog(outp_str);
                value = temp_val;

                return true;
            }
            else
            {
                return false;
            }
        }

        bool ReadArchiveLastVal(ref ArchiveValue archVal)
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
                ArchiveValueParser avp = new ArchiveValueParser(data_arr);
                avp.GetArchiveValue(ref archVal);
            }
            catch (Exception ex)
            {
                WriteToLog("ReadArchiveLastVal: " + ex.Message);
                return false;
            }

            return true;
        }

        bool ReadArchiveValById(uint id, ref ArchiveValue archVal)
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

        bool ReadArchiveValCountId(ref int valuesCount, ref int lastValId)
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

        bool ReadErrorCode(ref float errCode)
        {
            byte[] data = null;
            if (SendREQ_UD2(ref data))
            {
                byte[] errcodeBytes = new byte[ERROR_CODE_SIZE];
                Array.Copy(data, ERROR_CODE_INDEX, errcodeBytes, 0, ERROR_CODE_SIZE);

                float temp_val = Convert.ToSingle(errcodeBytes[ERROR_CODE_CMD]);

                string outp_str = "ErrorCode: " + temp_val.ToString();
                WriteToLog(outp_str);
                errCode = temp_val;
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            throw new NotImplementedException();
        }

        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
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
            int records = 0, lastid = 0;

            try
            {
                if (!ReadArchiveValCountId(ref records, ref lastid)) return false;
            }
            catch (Exception ex)
            {
                //WriteToLog("ReadDailyValues->Records count/last id: " + records + "; " + lastid + "; ex");
                WriteToLog("ReadDailyValues->ReadArchiveValCountId ex: " + ex.Message);
            }

            ArchiveValue lastArchiveVal = new ArchiveValue();

            try
            {
                if (!ReadArchiveLastVal(ref lastArchiveVal)) return false;
            }
            catch (Exception ex)
            {
                WriteToLog("ReadDailyValues->ReadArchiveLastVal ex: " + ex.Message);
            }


            DateTime lastRecDt = lastArchiveVal.dt;

            if (dt > lastRecDt)
            {
                WriteToLog("ReadDailyValues: на указанную дату записей не обнаружено: " + dt.ToShortDateString());
                return false;
            }

            WriteToLog("ReadDailyValues: lastRecDt: " + lastRecDt.ToShortDateString());
            WriteToLog("ReadDailyValues: lastId: " + lastid.ToString());
            WriteToLog("ReadDailyValues: requiredDt: " + dt.ToShortDateString());

            //преобразуем dt в id
            TimeSpan ts = lastRecDt - dt;
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

        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<iMeters.RecordPowerSlice> listRPS, byte period)
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

        class ArchiveValueParser : elf108
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

        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period)
        {
            return false;
        }
    }




}
