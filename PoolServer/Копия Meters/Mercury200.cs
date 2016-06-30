using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Prizmer.Meters.iMeters;
using Prizmer.Ports;

namespace Prizmer.Meters
{
    public sealed class Mercury200 : CMeter, IMeter
    {
        private enum TypesValues
        {
            Tarif1AP = 1,
            Tarif2AP = 2,
            Tarif3AP = 3,
            Tarif4AP = 4
        }

        public void Init(uint address, string pass, VirtualPort vp)
        {
            // перевод адреса в формат наладчика
            this.m_address = 0xFA000000 + Convert.ToUInt32(3 + address * 8);
            this.m_vport = vp;

            SetTypesForRead(GetTypesForCategory(CommonCategory.Current));
        }

        /// <summary>
        /// Структура с информацией об единичной считываемой величине  
        /// </summary>
        public struct RecordValue
        {
            /// <summary>
            /// Значение
            /// </summary>
            public double value;
            /// <summary>
            /// Тип
            /// </summary>
            public byte type;
            /// <summary>
            /// Статус (true - значение верно, false - неверно)
            /// </summary>
            public bool fine_state;
        };

        private Dictionary<byte, string> m_dictDataTypes = new Dictionary<byte, string>();
        private List<byte> m_listTypesForRead = new List<byte>();


        /// <summary>
        ///  Структура с информацией о считанных величинах
        /// </summary>
        public struct Values
        {
            /// <summary>
            /// Коллекция с информацией о считанных величинах
            /// </summary>
            public List<RecordValue> listRV;
        }

        // Массивы для подсчета контрольной суммы
        private byte[] srCRCHi = new byte[256] {
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
                0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
                0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
                0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
                0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40
        };

        private byte[] srCRCLo = new byte[256] {
                0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06, 0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD,
                0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09, 0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A,
                0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC, 0x14, 0xD4, 0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
                0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3, 0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4,
                0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A, 0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29,
                0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED, 0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
                0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60, 0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67,
                0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F, 0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68,
                0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E, 0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
                0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71, 0x70, 0xB0, 0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92,
                0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C, 0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B,
                0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B, 0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
                0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42, 0x43, 0x83, 0x41, 0x81, 0x80, 0x40
        };

        private const ushort m_init_crc = 0xFFFF;
        private byte[] m_crc = new byte[2];
        private byte[] m_cmd = new byte[32];

        private DateTime m_dt;
        private byte m_length_cmd = 0;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="address"></param>
        public Mercury200()
        {
            m_dictDataTypes.Add((byte)TypesValues.Tarif1AP, "Тариф 1 А+");
            m_dictDataTypes.Add((byte)TypesValues.Tarif2AP, "Тариф 2 А+");
            m_dictDataTypes.Add((byte)TypesValues.Tarif3AP, "Тариф 3 А+");
            m_dictDataTypes.Add((byte)TypesValues.Tarif4AP, "Тариф 4 А+");

            //  m_log_file_name += this.GetType() + "_" + m_address.ToString();
        }

        public void SetTypesForRead(List<byte> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (m_dictDataTypes.ContainsKey(types[i]))
                {
                    m_listTypesForRead.Add(types[i]);
                }
            }
        }

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            List<byte> listTypes = new List<byte>();

            switch (common_category)
            {
                case CommonCategory.Current:
                case CommonCategory.Monthly:
                    for (byte type = (byte)TypesValues.Tarif1AP; type <= (byte)TypesValues.Tarif4AP; type++)
                    {
                        listTypes.Add(type);
                    }
                    break;
            }

            return listTypes;
        }

        public bool OpenLinkCanal()
        {
            return ReadDateTime();
        }

        /// <summary>
        /// Чтение даты/времени счетчика
        /// </summary>
        /// <returns></returns>
        private bool ReadDateTime()
        {
            byte[] answer = new byte[14];
            byte[] command = new byte[] { 0x21 };

            if (!SendCommand(command, ref answer, 1, 14))
                return false;

            // конвертируем время из DEC в HEX
            int seconds = CommonMeters.DEC2HEX(answer[8]);
            int minute = CommonMeters.DEC2HEX(answer[7]);
            int hour = CommonMeters.DEC2HEX(answer[6]);
            int day = CommonMeters.DEC2HEX(answer[9]);
            int month = CommonMeters.DEC2HEX(answer[10]);
            int year = CommonMeters.DEC2HEX(answer[11]) + 2000;

            try
            {
                m_dt = new DateTime(year, month, day, hour, minute, seconds);
            }
            catch
            {
                return false;
            }

            WriteToLog("DateTime=" + m_dt.ToString());

            return true;
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            byte[] answer = new byte[11];
            byte[] command = new byte[] { 0x2F };

            if (!SendCommand(command, ref answer, 1, 11))
                return false;

            try
            {
                byte[] temp = new byte[4] { answer[8], answer[7], answer[6], answer[5] };
                serial_number = BitConverter.ToUInt32(temp, 0).ToString();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool SyncTime(DateTime dt)
        {
            byte[] cmnd = new byte[8];
            byte[] answer = new byte[23];
            byte[] command = new byte[] { 0x02 };

            cmnd[0] = command[0];

            cmnd[1] = CommonMeters.HEX2DEC((byte)dt.DayOfWeek);
            cmnd[2] = CommonMeters.HEX2DEC((byte)dt.Hour);
            cmnd[3] = CommonMeters.HEX2DEC((byte)dt.Minute);
            cmnd[4] = CommonMeters.HEX2DEC((byte)dt.Second);
            cmnd[5] = CommonMeters.HEX2DEC((byte)dt.Day);
            cmnd[6] = CommonMeters.HEX2DEC((byte)dt.Month);
            cmnd[7] = CommonMeters.HEX2DEC((byte)(dt.Year - 2000));

            return SendCommand(cmnd, ref answer, 8, 7);
        }

        public bool ReadCurrentValues(ref Values values)
        {
            values = new Values();
            values.listRV = new List<RecordValue>();
            byte[] command = { 0x27 };
            byte[] answer = new byte[23];
            uint total = 0;

            /*
            CMD = 27h
            ADDR(4)-CMD(1)-CRC(2) ( формат ответа ADDR-CMD-count*4-CRC)
            Count = 4 bytes = 0…99999999h
                                            4-х байтовое значение
                                            потребленной эл.энергии
                                            в десятках Вт.ч
                                            Старшие разряды вперёд.
                                            Возвращает сразу по всем тарифам.
            */

            if (!SendCommand(command, ref answer, 1, 23))
                return false;

            for (int t = 0; t < m_listTypesForRead.Count; t++)
            {
                RecordValue recordValue;
                recordValue.type = m_listTypesForRead[t];
                recordValue.fine_state = true;
                recordValue.value = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (answer[5 + i + (m_listTypesForRead[t] - 1) * 4] == 0xFF)
                    {
                        recordValue.fine_state = false;
                        break;
                    }
                }

                if (recordValue.fine_state)
                {
                    total = Convert.ToUInt32(CommonMeters.DEC2HEX(answer[5 + (m_listTypesForRead[t] - 1) * 4])) * 1000000 +
                            Convert.ToUInt32(CommonMeters.DEC2HEX(answer[6 + (m_listTypesForRead[t] - 1) * 4])) * 10000 +
                            Convert.ToUInt32(CommonMeters.DEC2HEX(answer[7 + (m_listTypesForRead[t] - 1) * 4])) * 100 +
                            Convert.ToUInt32(CommonMeters.DEC2HEX(answer[8 + (m_listTypesForRead[t] - 1) * 4]));

                    recordValue.value = Convert.ToSingle(total) / 100;// т.к. передаётся в десятках ватт
                }

                values.listRV.Add(recordValue);
            }

            return true;
        }

        public bool ReadMonthlyValues(byte month, ushort year, ref Values values)
        {
            values = new Values();
            values.listRV = new List<RecordValue>();
            if ((m_dt.Year < year) || (m_dt.Year == year && m_dt.Month < month))
            {
                WriteToLog("ReadMonthlyValues datetime=" + m_dt.ToString() + "; month=" + month.ToString() + "; year=" + year.ToString());
                return false;
            }

            byte[] command = { 0x32, (byte)(month - 1) };
            byte[] answer = new byte[23];
            ulong total = 0;

            /*
            CMD = 32h
            ADDR-CMD-ii3-CRC ( формат ответа ADDR-CMD-count*4-CRC)
            ii3   = 1 byte  = 0…Bh          Младшая тетрада - месяц  0h…Bh
                                            (0 -январь … Bh - декабрь)
            Count = 4 bytes = 0…99999999h
                                            4-х байтовое значение
                                            потребленной эл.энергии
                                            в десятках Вт.ч
                                            Старшие разряды вперёд.
            */

            if (!SendCommand(command, ref answer, 2, 23))
                return false;

            for (int t = 0; t < m_listTypesForRead.Count; t++)
            {
                RecordValue recordValue;
                recordValue.type = m_listTypesForRead[t];
                recordValue.fine_state = true;
                recordValue.value = 0;

                for (int i = 0; i < 4; i++)
                {
                    if (answer[5 + i + (m_listTypesForRead[t] - 1) * 4] == 0xFF)
                    {
                        recordValue.fine_state = false;
                        break;
                    }
                }

                if (recordValue.fine_state)
                {
                    total = Convert.ToUInt32(CommonMeters.DEC2HEX(answer[5 + (m_listTypesForRead[t] - 1) * 4])) * 1000000 +
                            Convert.ToUInt32(CommonMeters.DEC2HEX(answer[6 + (m_listTypesForRead[t] - 1) * 4])) * 10000 +
                            Convert.ToUInt32(CommonMeters.DEC2HEX(answer[7 + (m_listTypesForRead[t] - 1) * 4])) * 100 +
                            Convert.ToUInt32(CommonMeters.DEC2HEX(answer[8 + (m_listTypesForRead[t] - 1) * 4]));

                    recordValue.value = Convert.ToSingle(total) / 100;// т.к. передаётся в десятках ватт
                }

                values.listRV.Add(recordValue);
            }

            return true;
        }

        public bool ReadDailyValues(byte day, byte month, ushort year, ref Values values)
        {
            return false;
        }


        /// <summary>
        /// расчет контрольной суммы
        /// </summary>
        /// <param name="StrForCRC"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private ushort CalcCRC(byte[] StrForCRC, ushort size)
        {
            ushort crc = UpdateCRC(StrForCRC[0], m_init_crc);

            for (ushort i = 1; i < size; i++)
            {
                crc = UpdateCRC(StrForCRC[i], crc);
            }
            m_crc[0] = Convert.ToByte(crc / 256);
            m_crc[1] = Convert.ToByte(crc % 256);

            UInt16 back = 0;
            back = (UInt16)((back << 8) + m_crc[1]);
            back = (UInt16)((back << 8) + m_crc[0]);
            return back;

            //return BitConverter.ToUInt16(m_crc,0);
        }

        /// <summary>
        /// обновление контрольной суммы
        /// </summary>
        /// <param name="C"></param>
        /// <param name="oldCRC"></param>
        /// <returns></returns>
        private ushort UpdateCRC(byte C, ushort oldCRC)
        {
            byte i = 0;
            byte[] arrCRC = new byte[2];

            arrCRC[1] = Convert.ToByte(oldCRC >> 8);
            arrCRC[0] = Convert.ToByte(oldCRC & 0xFF);

            i = Convert.ToByte(arrCRC[1] ^ C);
            arrCRC[1] = Convert.ToByte(arrCRC[0] ^ srCRCHi[i]);
            arrCRC[0] = srCRCLo[i];

            UInt16 back = 0;
            back = (UInt16)((back << 8) + arrCRC[1]);
            back = (UInt16)((back << 8) + arrCRC[0]);
            return back;

            //return BitConverter.ToUInt16(arrCRC, 0);
        }

        /// <summary>
        /// Отправка команды
        /// </summary>
        /// <param name="cmnd"></param>
        /// <param name="answer"></param>
        /// <param name="cmd_size"></param>
        /// <param name="answ_size"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        private bool SendCommand(byte[] cmnd, ref byte[] answer, ushort cmd_size, ushort answ_size)
        {
            // формирование команды
            MakeCommand(cmnd, ref cmd_size);

            if (m_vport != null)
            {
                //WriteToLog("Cmd size=" + cmd_size.ToString());
                if (m_vport.WriteReadData(FindPacketSignature, m_cmd, ref answer, cmd_size, answ_size) == answ_size)
                {
                    //WriteToLog("Answer size=" + answ_size.ToString());
                    //проверка пришедших данных
                    if (FinishAccept(answer, answ_size))
                    {
                        //WriteToLog("Finish accept");
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Поиск пакета данных
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private int FindPacketSignature(Queue<byte> queue)
        {
            try
            {
                byte[] array = new byte[queue.Count];
                array = queue.ToArray();

                for (int i = 0; i + 6 < queue.Count; i++)
                {
                    if (array[i] == m_cmd[0] &&
                        array[i + 1] == m_cmd[1] &&
                        array[i + 2] == m_cmd[2] &&
                        array[i + 3] == m_cmd[3] &&
                        array[i + 4] == m_cmd[4]
                        )
                    {
                        for (int j = 0; j < m_length_cmd; j++)
                        {
                            if (array[i + j] != m_cmd[j])
                            {
                                return i;
                            }
                        }
                    }
                }

                throw new ApplicationException("Несовпадение байт в пакете");
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// формирование команды
        /// </summary>
        /// <param name="cmnd"></param>
        /// <param name="size"></param>
        private void MakeCommand(byte[] cmnd, ref ushort size)
        {
            m_length_cmd = 0;

            // Добавление сетевого адреса прибора в начало посылки
            m_cmd[3] = Convert.ToByte(m_address & 0xFF);
            m_cmd[2] = Convert.ToByte((m_address >> 8) & 0xFF);
            m_cmd[1] = Convert.ToByte((m_address >> 16) & 0xFF);
            m_cmd[0] = Convert.ToByte((m_address >> 24) & 0xFF);

            m_length_cmd += 4;

            // Добавление данных в посылку
            for (int i = 0; i < size; i++)
            {
                m_cmd[m_length_cmd++] = cmnd[i];
            }

            size += 4;

            // Вычисляем CRC
            CalcCRC(m_cmd, size);

            // Добавляем контрольную сумму к команде
            for (int i = 0; i < 2; i++)
            {
                m_cmd[m_length_cmd++] = m_crc[i];
            }

            size += 2;
        }

        /// <summary>
        /// проверка пришедших данных
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool FinishAccept(byte[] answer, ushort size)
        {
            // проверка адреса
            uint temp_addr = 0;

            temp_addr = (temp_addr << 8) + answer[0];
            temp_addr = (temp_addr << 8) + answer[1];
            temp_addr = (temp_addr << 8) + answer[2];
            temp_addr = (temp_addr << 8) + answer[3];

            if (temp_addr == m_address)
            {
                // проверяем CRC
                CalcCRC(answer, Convert.ToUInt16(size - 2));

                if (m_crc[0] == answer[size - 2] && m_crc[1] == answer[size - 1])
                {
                    return true;
                }
            }

            return false;
        }


        //РЕАЛИЗАЦИЯ МЕТОДОВ ИНТЕРФЕЙСА

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            Values tmpValues = new Values();
            if (!ReadCurrentValues(ref tmpValues))
                return false;

            List<RecordValue> listRv = tmpValues.listRV;
            if (listRv == null || listRv.Count == 0) return false;
            if (listRv.Count > 4) return false;

            int listRvCount = listRv.Count;

            switch (tarif)
            {
                case 1:
                    {
                        if (listRv.Count == 0) return false;
                        recordValue = (float)listRv[0].value;
                        break;
                    }
                case 2:
                    {
                        if (listRv.Count < 2) return false;
                        recordValue = (float)listRv[1].value;
                        break;
                    }
                case 3:
                    {
                        if (listRv.Count < 3) return false;
                        recordValue = (float)listRv[2].value;
                        break;
                    }
                case 4:
                    {
                        if (listRv.Count < 4) return false;
                        recordValue = (float)listRv[3].value;
                        break;
                    }
                case 0:
                    {
                        for (int i = 0; i < listRv.Count; i++)
                        {
                            recordValue += (float)listRv[i].value;
                        }
                        break;
                    }
                default: return false;
            }

            return true;
        }


        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            Values tmpValues = new Values();
            if (!ReadMonthlyValues((byte)dt.Month, (ushort)dt.Year, ref tmpValues))
                return false;

            List<RecordValue> listRv = tmpValues.listRV;
            if (listRv == null || listRv.Count == 0) return false;
            if (listRv.Count > 4) return false;

            int listRvCount = listRv.Count;

            switch (tarif)
            {
                case 1:
                    {
                        if (listRv.Count == 0) return false;
                        recordValue = (float)listRv[0].value;
                        break;
                    }
                case 2:
                    {
                        if (listRv.Count < 2) return false;
                        recordValue = (float)listRv[1].value;
                        break;
                    }
                case 3:
                    {
                        if (listRv.Count < 3) return false;
                        recordValue = (float)listRv[2].value;
                        break;
                    }
                case 4:
                    {
                        if (listRv.Count < 4) return false;
                        recordValue = (float)listRv[3].value;
                        break;
                    }
                case 0:
                    {
                        for (int i = 0; i < listRv.Count; i++)
                        {
                            recordValue += (float)listRv[i].value;
                        }
                        break;
                    }
                default: return false;
            }

            return true;
        }




        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            if (dt.TimeOfDay.Hours < 12 && dt.TimeOfDay.Hours > 3)
            {
                try
                {
                    return ReadCurrentValues(param, tarif, ref recordValue);
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return false;
        }




        public bool ReadHalfAnHourValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }



        public bool ReadHourValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }





        public bool ReadDailyValues(uint recordId, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }

        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<RecordPowerSlice> listRPS, byte period)
        {
            return false;
        }

        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period)
        {
            return false;
        }

        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            return false;
        }
    }


}
