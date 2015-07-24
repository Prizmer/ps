using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Prizmer.Meters.iMeters;
using Prizmer.Ports;

namespace Prizmer.Meters
{
    public sealed class set4tm_03 : CMeter, IMeter
    {
        private enum TypesValues
        {
            Tarif1AP = 1,
            Tarif1AM = 2,
            Tarif1RP = 3,
            Tarif1RM = 4,
            Tarif2AP = 5,
            Tarif2AM = 6,
            Tarif2RP = 7,
            Tarif2RM = 8,
            Tarif3AP = 9,
            Tarif3AM = 10,
            Tarif3RP = 11,
            Tarif3RM = 12,
            Tarif4AP = 13,
            Tarif4AM = 14,
            Tarif4RP = 15,
            Tarif4RM = 16,
            PowerSliceAP = 17,
            PowerSliceRP = 18,
            PowerSliceAM = 19,
            PowerSliceRM = 20,
            Frequency   = 21,
            UA  = 22,
            UB  = 23,
            UC  = 24,
            IA = 25,
            IB = 26,
            IC = 27,
            COS = 28,
            COSA = 29,
            COSB = 30,
            COSC = 31,
            P = 32,
            PA = 33,
            PB = 34,
            PC = 35,
            S = 36,
            SA = 37,
            SB = 38,
            SC = 39,
            Q = 40,
            QA = 41,
            QB = 42,
            QC = 43
        }

        private enum ValueTypes
        {
            AP = 0,
            AM = 1,
            RP = 2,
            RM = 3,
            P = 4,
            Q = 5,
            I = 6,
            U = 7

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

        private const byte m_energy = 4;
        private ushort m_init_crc = 0xFFFF;
        private ushort m_gear_ratio = 1;
        private byte[] m_crc = new byte[2];
        private byte[] m_cmd = new byte[256];
        private byte[] m_password = new byte[6];
        private bool m_is_opened = false;
        private DateTime m_dt;
        private byte m_length_cmd = 0;
        private const byte min_answer_length = 4;
        private const byte m_period_int_power_slices = 30;
        private const ushort m_max_records_power_slices = 8192;
        private readonly ushort m_depth_storage_power_slices = 0;
        private readonly byte m_size_record_power_slices = 0;

        private const byte TEST_ANSW_SIZE			= 4;
        private const byte OPEN_ANSW_SIZE			= 4;
        private const byte CLOSE_ANSW_SIZE			= 4;
        private const byte WPARAMS_ANSW_SIZE		= 4;
        private const byte RCURTIME_ANSW_SIZE		= 11;
        private const byte RVAREXEC_ANSW_SIZE		= 6;
        private const byte RSERIALNUMBER_ANSW_SIZE  = 10;
        private const byte READ_PARAMS_OF_ENERGY    = 19;
        private const byte RLASTSLICE_ANSW_SIZE	    = 10;
        private const byte RMONTHLY_ANSW_SIZE_BASE  = 3;
        private const byte RDAILY_ANSW_SIZE_BASE	= 3;
        private const byte RCURRENT_ANSW_SIZE		= 19;
        private const byte RSLICE_ANSW_SIZE_MIN		= 3;

        private List<byte> m_types_for_read_expense = new List<byte>();
        private List<byte> m_types_for_read_power_slices = new List<byte>();
        private List<byte> m_types_for_read_power_quality_params = new List<byte>();

        #region Импортированные структуры

        public struct LastPowerSlice
        {
            public ushort addr;
            public byte period;
            public bool reload;
            public DateTime dt;
        };


        public struct RecordParamsEnergy
        {
            public double phase_sum;
            public double phase_1;
            public double phase_2;
            public double phase_3;
            public byte type;
        };

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

        /// <summary>
        /// Структура с иформацией об срезе мощности
        /// </summary>
        public struct IndaySlice
        {
            /// <summary>
            /// Коллекция со значениями
            /// </summary>
            public List<RecordValue> values;
            /// <summary>
            /// Статус значений
            /// </summary>
            public bool not_full;
            /// <summary>
            /// Время среза
            /// </summary>
            public DateTime date_time;
        };

        protected Dictionary<byte, string> m_dictDataTypes = new Dictionary<byte, string>();
        protected List<byte> m_listTypesForRead = new List<byte>();

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="address"></param>
        /// <param name="password"></param>
        /// 
        public set4tm_03()
        {
            m_depth_storage_power_slices = Convert.ToUInt16(m_max_records_power_slices / (60 / m_period_int_power_slices + 1));
            m_size_record_power_slices = (60 / m_period_int_power_slices + 1) * 8;
        }
        
        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            bool password_type_hex = false;
            this.m_address = address;

            

            if (m_password.Length >= pass.Length)
            {
                for (int j = 0; j < pass.Length; j++)
                {
                    if (password_type_hex)
                    {
                        m_password[j] = Convert.ToByte(pass[j]);
                        m_password[j] -= 0x30;
                    }
                    else
                    {
                        m_password[j] = Convert.ToByte(pass[j]);
                    }
                }
            }

            /*
            for (byte t = 1; t <= (byte)TypesValues.QC; t++)
            {
                m_dictDataTypes.Add(t, "");
            }
            */

            m_vport = data_vport;

            //m_log_file_name += this.GetType() + "_" + m_address.ToString();
        }

        public void SetTypesForRead(List<byte> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (m_dictDataTypes.ContainsKey(types[i]))
                {
                    m_listTypesForRead.Add(types[i]);

                    if (types[i] >= (byte)TypesValues.Tarif1AP & types[i] <= (byte)TypesValues.Tarif4RM)
                    {
                        m_types_for_read_expense.Add(types[i]);
                    }

                    if (types[i] >= (byte)TypesValues.PowerSliceAP & types[i] <= (byte)TypesValues.PowerSliceRM)
                    {
                        m_types_for_read_power_slices.Add(types[i]);
                    }

                    if (types[i] >= (byte)TypesValues.Frequency & types[i] <= (byte)TypesValues.QC)
                    {
                        m_types_for_read_power_quality_params.Add(types[i]);
                    }
                }
            }
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

            return BitConverter.ToUInt16(m_crc, 0);
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

            return BitConverter.ToUInt16(arrCRC, 0);
        }

        /// <summary>
        /// Отправка команды
        /// </summary>
        /// <param name="cmnd"></param>
        /// <param name="answer"></param>
        /// <param name="cmd_size"></param>
        /// <param name="answ_size"></param>
        /// <returns></returns>
        private bool SendCommand(byte[] cmnd, ref byte[] answer, ushort cmd_size, ushort answ_size)
        {
            bool res = false;

            // формирование команды
            MakeCommand(cmnd, ref cmd_size);
            
            if (m_vport != null)
            {
                if (m_vport.WriteReadData(FindPacketSignature, m_cmd, ref answer, cmd_size, answ_size) == answ_size)
                {
                    //проверка пришедших данных
                    if (FinishAccept(answer, Convert.ToUInt16(answ_size)))
                    {
                        res = true;
                    }
                }
            }

            return res;
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
            m_cmd[m_length_cmd++] = (byte)m_address;

            // Добавление данных в посылку
            for (int i = 0; i < size; i++)
            {
                m_cmd[m_length_cmd++] = cmnd[i];
            }

            // Вычисляем CRC
            CalcCRC(m_cmd, Convert.ToUInt16(size + 1));

            // Добавляем контрольную сумму к команде
            for (int i = 0; i < 2; i++)
            {
                m_cmd[m_length_cmd++] = m_crc[i];
            }

            size += 3;
        }

        /// <summary>
        /// Поиск пакета данных
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public int FindPacketSignature(Queue<byte> queue)
        {
            try
            {
                byte[] array = new byte[queue.Count];
                array = queue.ToArray();

                //WriteToLog("Request: " + BitConverter.ToString(m_cmd));
                //WriteToLog("Response: " + BitConverter.ToString(array));

                int i = 0;
                bool have_echo = true;
                if (array[i] == m_cmd[0])
                {
                    int j = 0;
                    for (j = 0; j < m_length_cmd; j++)
                    {
                        if (array[i + j] != m_cmd[j])
                        {
                            have_echo = false;
                            break;
                        }
                    }
                    if (have_echo)
                    {
                        i += j;
                    }
                }

                return i;

                throw new ApplicationException("Несовпадение байт в пакете");
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// проверка пришедших данных
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool FinishAccept(byte[] answer, ushort size)
        {
            byte[] crc = new byte[2];
            byte[] tmp_buf = new byte[32];

            // проверка длины полученного ответа = 1 байт адреса + min 1 байт поле ответа + CRC(2)
            if (size < min_answer_length)
            {
                return false;
            }

            // проверяем адрес прибора в ответе
            if (answer[0] != m_address)
            {
                return false;
            }

            // проверяем CRC
            CalcCRC(answer, Convert.ToUInt16(size - 2));

            if (m_crc[0] == answer[size - 2] && m_crc[0] == answer[size - 1])
            {
                return true;
            }

            return true;
        }

        /// <summary>
        /// Чтение варианта исполнения счетчика
        /// </summary>
        /// <returns></returns>
        private bool ReadVariantExecute()
        {
            byte[] answer = new byte[RVAREXEC_ANSW_SIZE];
            byte[] command = new byte[] { 0x08, 0x12 };

            if (!SendCommand(command, ref answer, 2, RVAREXEC_ANSW_SIZE))
                return false;

            // определяем передаточное число
            switch (answer[2] & 0xF)
            {
                case 0:
                    m_gear_ratio = 5000;
                    break;
                case 1:
                    m_gear_ratio = 25000;
                    break;
                case 2:
                    m_gear_ratio = 1250;
                    break;
                case 3:
                    m_gear_ratio = 500;
                    break;
                case 4:
                    m_gear_ratio = 1000;
                    break;
                case 5:
                    m_gear_ratio = 250;
                    break;
                default:
                    m_gear_ratio = 1;
                    break;
            }

            return true;
        }

        /// <summary>
        /// открытие канала связи
        /// </summary>
        /// <param name="pwd"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool Open()
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[OPEN_ANSW_SIZE];
            byte[] command = new byte[] { 0x01 };

            //if (!m_is_opened)
            //{
                cmnd[0] = command[0];
                m_password.CopyTo(cmnd, 1);

                if (!SendCommand(cmnd, ref answer, 7, OPEN_ANSW_SIZE))
                    return false;

            //    m_is_opened = true;
            //}

            return true;
        }

        /// <summary>
        /// Чтение даты/времени счетчика
        /// </summary>
        /// <returns></returns>
        private bool ReadDateTime()
        {
            byte[] answer = new byte[RCURTIME_ANSW_SIZE];
            byte[] command = new byte[] { 0x04, 0x00 };

            if (!SendCommand(command, ref answer, 2, RCURTIME_ANSW_SIZE))
                return false;

            // конвертируем время из DEC в HEX
            int seconds = CommonMeters.DEC2HEX(answer[1]);
            int minute = CommonMeters.DEC2HEX(answer[2]);
            int hour = CommonMeters.DEC2HEX(answer[3]);
            int wday = CommonMeters.DEC2HEX(answer[4]);
            int day = CommonMeters.DEC2HEX(answer[5]);
            int month = CommonMeters.DEC2HEX(answer[6]);
            int year = CommonMeters.DEC2HEX(answer[7]) + 2000;

            try
            {
                m_dt = new DateTime(year, month, day, hour, minute, seconds, 0);
            }
            catch
            {
                return false;
            }

            //WriteToLog("DateTime=" + m_dt.ToString());

            return true;
        }

        /// <summary>
        /// Чтение указанного типа параметра
        /// </summary>
        /// <param name="paramType"></param>
        /// <param name="recParams"></param>
        /// <returns></returns>
        private bool ReadParams(byte paramType, ref RecordParamsEnergy recParams)
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[32];
            byte[] command = new byte[] { 0x08, 0x1B, 0x2};
            
            command.CopyTo(cmnd, 0);



            cmnd[3] = paramType;

            //WriteToLog("ReadParams=" +  BitConverter.ToString(m_cmd));

            if (!SendCommand(cmnd, ref answer, 4, READ_PARAMS_OF_ENERGY))
                return false;

            // по сумме фаз
            recParams.phase_sum = Math.Round(BitConverter.ToSingle(answer, 1), 3);

            // фаза 1
            recParams.phase_1 = Math.Round(BitConverter.ToSingle(answer, 5), 3);

            // фаза 2
            recParams.phase_2 = Math.Round(BitConverter.ToSingle(answer, 9), 3);

            // фаза 3
            recParams.phase_3 = Math.Round(BitConverter.ToSingle(answer, 13), 3);

            return true;
        }


        /// <summary>
        /// Чтение вспомогательных параметров
        /// </summary>
        /// <param name="readparam">5 - P, 6 - Q, 7 - U, 8 - I</param>
        /// <param name="tarif">Используется как селектор фазы [0-3] 0 - сумма</param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        public bool ReadAuxilaryParams(ushort readparam, byte tarif, ref float recordValue)
        {
            if (tarif > 3)
            {
                this.WriteToLog("ReadAuxilaryParams: tarif should be less than 3");
                return false;
            }

            byte[] cmnd = new byte[32];
            byte[] answer = new byte[32];
            byte[] command = new byte[] { 0x08, 0x1B, 0x2 };

            command.CopyTo(cmnd, 0);

            byte RWRI = 0x0;
            switch (readparam)
            {
                case 5:
                    {
                        byte modeNumber = 0x0;
                        //мощность P 
                        byte paramNumber = 0x0;
                        byte phaseNumber = (byte)(tarif & 0x01);

                        RWRI = (byte)(modeNumber << 4 | paramNumber << 2 | phaseNumber);
                        cmnd[3] = (byte)RWRI;

                        if (!SendCommand(cmnd, ref answer, 4, READ_PARAMS_OF_ENERGY))
                            return false;

                        recordValue = (float)Math.Round(BitConverter.ToSingle(answer, 1+(tarif*4)), 3);
                        return true;
                    }
                case 6:
                    {
                        byte modeNumber = 0x0;
                        //мощность Q 
                        byte paramNumber = 0x01;
                        byte phaseNumber = (byte)(tarif & 0x01);

                        RWRI = (byte)(modeNumber << 4 | paramNumber << 2 | phaseNumber);
                        cmnd[3] = (byte)RWRI;

                        if (!SendCommand(cmnd, ref answer, 4, READ_PARAMS_OF_ENERGY))
                            return false;

                        recordValue = (float)Math.Round(BitConverter.ToSingle(answer, 1 + (tarif * 4)), 3);
                        return true;
                    }
                case 7:
                    {
                        //Iph
                        byte modeNumber = 0x2;
                        byte paramNumber = 0x0;
                        byte phaseNumber = (byte)(tarif & 0x01);

                        RWRI = (byte)(modeNumber << 4 | paramNumber << 2 | phaseNumber);
                        cmnd[3] = (byte)RWRI;

                        if (!SendCommand(cmnd, ref answer, 4, READ_PARAMS_OF_ENERGY))
                            return false;

                        recordValue = (float)Math.Round(BitConverter.ToSingle(answer, 1 + (tarif * 4)), 3);
                        return true;
                    }
                case 8:
                    {
                        //Uph
                        byte modeNumber = 0x1;
                        byte paramNumber = 0x0;
                        byte phaseNumber = (byte)(tarif & 0x01);

                        RWRI = (byte)(modeNumber << 4 | paramNumber << 2 | phaseNumber);
                        cmnd[3] = (byte)RWRI;

                        if (!SendCommand(cmnd, ref answer, 4, READ_PARAMS_OF_ENERGY))
                            return false;

                        recordValue = (float)Math.Round(BitConverter.ToSingle(answer, 1 + (tarif * 4)), 3);
                        return true;
                    }

                default: return false;
            }
        }

        /*
        /// <summary>
        /// Чтение параметров качества энергии
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private void PowerQualityParams(ref Values values)
        {
            RecordParamsEnergy recParams = new RecordParamsEnergy();

            // читаем частоту сети, Гц
            if (m_types_for_read_power_quality_params.Contains((byte)TypesValues.Frequency))
            {
                if (this.ReadParams(0x40, ref recParams))
                {
                    RecordValue iv;
                    iv.fine_state = true;

                    iv.type = (byte)TypesValues.Frequency;
                    iv.value = recParams.phase_sum;

                    values.listRV.Add(iv);

                    //WriteToLog("F = " + recParams.phase_sum.ToString());
                }
            }

            // читаем напряжение, В
            if (m_types_for_read_power_quality_params.Contains((byte)TypesValues.UA) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.UB) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.UC)
                )
            {
                if (this.ReadParams(0x11, ref recParams))
                {
                    RecordValue iv;
                    iv.fine_state = true;

                    iv.type = (byte)TypesValues.UA;
                    iv.value = recParams.phase_1;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.UB;
                    iv.value = recParams.phase_2;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.UC;
                    iv.value = recParams.phase_3;

                    values.listRV.Add(iv);

                    //WriteToLog("Ua = " + recParams.phase_1.ToString() + "; Ub = " + recParams.phase_2.ToString() + "; Uc = " + recParams.phase_3.ToString());
                }
            }

            // читаем ток, А
            if (m_types_for_read_power_quality_params.Contains((byte)TypesValues.IA) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.IB) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.IC)
                )
            {
                if (this.ReadParams(0x21, ref recParams))
                {
                    RecordValue iv;
                    iv.fine_state = true;

                    iv.type = (byte)TypesValues.IA;
                    iv.value = recParams.phase_1;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.IB;
                    iv.value = recParams.phase_2;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.IC;
                    iv.value = recParams.phase_3;

                    values.listRV.Add(iv);

                    //WriteToLog("Ia = " + recParams.phase_1.ToString() + "; Ib = " + recParams.phase_2.ToString() + "; Ic = " + recParams.phase_3.ToString());
                }
            }

            // читаем коэффициенты мощности cos
            if (m_types_for_read_power_quality_params.Contains((byte)TypesValues.COS) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.COSA) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.COSB) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.COSC)
                )
            {
                if (this.ReadParams(0x30, ref recParams))
                {
                    RecordValue iv;
                    iv.fine_state = true;

                    iv.type = (byte)TypesValues.COSA;
                    iv.value = recParams.phase_1;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.COSB;
                    iv.value = recParams.phase_2;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.COSC;
                    iv.value = recParams.phase_3;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.COS;
                    iv.value = recParams.phase_sum;

                    values.listRV.Add(iv);

                    //WriteToLog("cos =" + recParams.phase_sum.ToString() + "; cosA = " + recParams.phase_1.ToString() + "; cosB = " + recParams.phase_2.ToString() + "; cosC = " + recParams.phase_3.ToString());
                }
            }

            // читаем мощность P, Вт
            if (m_types_for_read_power_quality_params.Contains((byte)TypesValues.P) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.PA) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.PB) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.PC)
                )
            {
                if (this.ReadParams(0x0, ref recParams))
                {
                    RecordValue iv;
                    iv.fine_state = true;

                    iv.type = (byte)TypesValues.PA;
                    iv.value = recParams.phase_1;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.PB;
                    iv.value = recParams.phase_2;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.PC;
                    iv.value = recParams.phase_3;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.P;
                    iv.value = recParams.phase_sum;

                    values.listRV.Add(iv);

                    //WriteToLog("PSum =" + recParams.phase_sum.ToString() + "; Pa = " + recParams.phase_1.ToString() + "; Pb = " + recParams.phase_2.ToString() + "; Pc = " + recParams.phase_3.ToString());
                }
            }

            // читаем мощность S, ВА
            if (m_types_for_read_power_quality_params.Contains((byte)TypesValues.S) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.SA) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.SB) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.SC)
                )
            {
                if (this.ReadParams(0x8, ref recParams))
                {
                    RecordValue iv;
                    iv.fine_state = true;

                    iv.type = (byte)TypesValues.SA;
                    iv.value = recParams.phase_1;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.SB;
                    iv.value = recParams.phase_2;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.SC;
                    iv.value = recParams.phase_3;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.S;
                    iv.value = recParams.phase_sum;

                    values.listRV.Add(iv);

                    //WriteToLog("SSum =" + recParams.phase_sum.ToString() + "; Sa = " + recParams.phase_1.ToString() + "; Sb = " + recParams.phase_2.ToString() + "; Sc = " + recParams.phase_3.ToString());
                }
            }

            // читаем мощность Q, Вар
            if (m_types_for_read_power_quality_params.Contains((byte)TypesValues.Q) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.QA) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.QB) |
                m_types_for_read_power_quality_params.Contains((byte)TypesValues.QC)
                )
            {
                if (this.ReadParams(0x4, ref recParams))
                {
                    RecordValue iv;
                    iv.fine_state = true;

                    iv.type = (byte)TypesValues.QA;
                    iv.value = recParams.phase_1;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.QB;
                    iv.value = recParams.phase_2;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.QC;
                    iv.value = recParams.phase_3;

                    values.listRV.Add(iv);

                    iv.type = (byte)TypesValues.Q;
                    iv.value = recParams.phase_sum;

                    values.listRV.Add(iv);

                    //WriteToLog("QSum =" + recParams.phase_sum.ToString() + "; Qa = " + recParams.phase_1.ToString() + "; Qb = " + recParams.phase_2.ToString() + "; Qc = " + recParams.phase_3.ToString());
                }
            }
        }
        */

        public bool ReadPowerSlice(ref List<RecordPowerSlice> listRPS, DateTime dt_begin, byte period)
        {
            try
            {
                // читаем последний срез
                LastPowerSlice lps = new LastPowerSlice();
                if (!ReadCurrentPowerSliceInfo(ref lps))
                {
                    return false;
                }

                // Вычисляем разницу в часах
                TimeSpan span = lps.dt - dt_begin;
                int diff_hours = Convert.ToInt32(span.TotalHours);

                // если разница > max кол-ва хранящихся записей в счётчике, то не вычитываем их из счётчика
                if (diff_hours >= m_depth_storage_power_slices)
                {
                    diff_hours = m_depth_storage_power_slices;
                }

                if (diff_hours > (lps.addr / 24))
                {
                    diff_hours = lps.addr / 24;
                }

                //WriteToLog("differense hours=" + diff_hours.ToString() + "; reload=" + lps.reload.ToString() + "; dt_begin=" + dt_begin.ToString());
                    List<RecordPowerSlice> lRPS = new List<RecordPowerSlice>();

                for (int i = diff_hours; i > 0; i--)
                {
                    int add_minus_val = (lps.dt.Minute == 0) ? 8 : 16;
                    int addr = lps.addr - Convert.ToUInt16(m_size_record_power_slices * i) - (ushort)add_minus_val;
                    ushort address_slice = (addr < 0) ? Convert.ToUInt16(Convert.ToInt32(65536 + addr)) : Convert.ToUInt16(addr);

                    // чтение среза по рассчитанному адресу и при чтении не было ошибок
                    
                    List<IndaySlice> record_slices = new List<IndaySlice>();
                    if (ReadSlice((ushort)address_slice, ref record_slices, period))
                    {
                        foreach (IndaySlice ids in record_slices)
                        {
                            RecordPowerSlice rps = new RecordPowerSlice();
                            rps.APlus = (float)ids.values[0].value;
                            rps.AMinus = (float)ids.values[1].value;
                            rps.RPlus = (float)ids.values[2].value;
                            rps.RMinus = (float)ids.values[3].value;
                            rps.status = Convert.ToByte(!ids.not_full);
                            rps.date_time = ids.date_time;
                            rps.period = 30;
                            lRPS.Add(rps);
                        }
                    }
                    else
                    {
                        WriteToLog("ReadPowerSlice: slice has not been read");
                        break;
                    }
                }

                if (lRPS.Count > 0)
                {
                    listRPS = lRPS;
                    return true;
                }
            }
            catch (Exception ex)
            {
                WriteToLog("ReadPowerSlice: " + ex.Message);
                return false;
            }

            return false;
        }
        
        /// <summary>
        /// Чтение среза мощности по указанному адресу
        /// </summary>
        /// <param name="addr_slice"></param>
        /// <param name="record_slice"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        private bool ReadSlice(ushort addr_slice, ref List<IndaySlice> record_slices, byte period)
        {            
            if (period != m_period_int_power_slices)
            {
                return false;
            }

            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RSLICE_ANSW_SIZE_MIN + m_size_record_power_slices];
            byte[] command = new byte[] { 0x06, 0x03 };
            byte[] addr = new byte[2];

            cmnd[0] = command[0];
            cmnd[1] = command[1];
            addr = BitConverter.GetBytes(addr_slice);
            cmnd[2] = addr[1];
            cmnd[3] = addr[0];
            cmnd[4] = m_size_record_power_slices;
            
            bool read_ok = false;

            int p = 0;
            do
            {
                if (SendCommand(cmnd, ref answer, 5, Convert.ToUInt16(RSLICE_ANSW_SIZE_MIN + m_size_record_power_slices)))
                {
                    read_ok = true;
                    break;
                }
            } while (p++ < 3);
            
            //WriteToLog("Request: " + BitConverter.ToString(m_cmd));
            //WriteToLog("ReadSlice: " + BitConverter.ToString(answer));

            if (read_ok)
            {
                try
                {
                    // проверка периода интегрирования
                    if (answer[6] == m_period_int_power_slices)
                    {
                        // время записи среза
                        int hour = CommonMeters.DEC2HEX(answer[1]);
                        int day = CommonMeters.DEC2HEX(answer[2]);
                        int month = CommonMeters.DEC2HEX(answer[3]);
                        int year = CommonMeters.DEC2HEX(answer[4]) + 2000;

                        WriteToLog("ReadSlice address: " + m_address.ToString() + "; datetime: hour=" + hour.ToString() + " day=" + day.ToString() + " month=" + month.ToString() + " year=" + year.ToString());

                        byte[] slices_data = new byte[16];

                        for (int b = 0; b < slices_data.Length; b++)
                        {
                            slices_data[b] = answer[9 + b];
                        }

                        for (int j = 0; j < 60 / m_period_int_power_slices; j++)
                        {
                            IndaySlice rps = new IndaySlice();
                            rps.values = new List<RecordValue>();
                            // Время среза
                            rps.date_time = new DateTime(year, month, day, hour, j * 30, 0);
                            // Неполный срез
                            rps.not_full = (Convert.ToByte(slices_data[0] >> 7) == 1) ? true : false;

                            WriteToLog("ReadSlice: datetime " + rps.date_time.ToString() + "; status " + rps.not_full.ToString());

                            // Разбираем по видам энергии
                            for (int i = 0; i < 4; i++)
                            {
                                byte type = 0;
                                switch (i)
                                {
                                    case 0:
                                        type = (byte)TypesValues.PowerSliceAP;
                                        break;
                                    case 1:
                                        type = (byte)TypesValues.PowerSliceAM;
                                        break;
                                    case 2:
                                        type = (byte)TypesValues.PowerSliceRP;
                                        break;
                                    case 3:
                                        type = (byte)TypesValues.PowerSliceRM;
                                        break;
                                }
                                    RecordValue rv;
                                    rv.type = type;
                                    rv.fine_state = false;
                                    rv.value = 0;

                                    byte[] buff = new byte[2];
                                    buff[0] = slices_data[j * 8 + i * 2 + 1];
                                    buff[1] = Convert.ToByte(slices_data[j * 8 + i * 2 + 0] & 0x7F);

                                    rv.value = Math.Round((Convert.ToSingle(Convert.ToSingle(BitConverter.ToUInt16(buff, 0)) / (2f * (float)m_gear_ratio)) * (60 / m_period_int_power_slices)) / 2f, 4);//n/2a*60/period
                                    rv.fine_state = true;

                                    rps.values.Add(rv);
                            }

                            record_slices.Add(rps);
                        }
                    }
                    else
                    {
                        WriteToLog("Wrong period integr: target = " + m_period_int_power_slices.ToString() + "; from answer=" + answer[6].ToString());
                    }
                }
                catch (Exception ex)
                {
                    WriteToLog("ReadSlice - " + ex.Message);
                    return false;
                }
            }

            return read_ok;
        }

        /// <summary>
        /// Чтение информации о последнем зафиксированнрм счетчиком среза
        /// </summary>
        /// <param name="lps"></param>
        /// <returns></returns>
        private bool ReadCurrentPowerSliceInfo(ref LastPowerSlice lps)
        {
            byte[] answer = new byte[RLASTSLICE_ANSW_SIZE];
            byte[] command = new byte[] { 0x08, 0x4 };

            // Читаем последний срез мощности для определения адреса физической памяти последней записи массива
            if (!SendCommand(command, ref answer, 2, RLASTSLICE_ANSW_SIZE))
                return false;

            try
            {
                byte[] temp = new byte[2] { answer[7], answer[6] };

                // адрес последней записи
                lps.addr = BitConverter.ToUInt16(temp, 0);

                // признак перезаписи области профиля
                lps.reload = ((answer[1] & 0x80) == 0x80) ? true : false;

                // конвертируем время из DEC в HEX 
                int minute = CommonMeters.DEC2HEX(Convert.ToByte(answer[1] & 0x7F));
                int hour = CommonMeters.DEC2HEX(answer[2]);
                int day = CommonMeters.DEC2HEX(answer[3]);
                int month = CommonMeters.DEC2HEX(answer[4]);
                int year = CommonMeters.DEC2HEX(answer[5]) + 2000;

                //minute = (minute > 15 && minute < 45) ? 30 : 00;

                //WriteToLog("ReadCurrentPowerSliceInfo datetime: minute" + answer[1].ToString("x") + " hour=" + hour.ToString() + " day=" + day.ToString() + " month=" + month.ToString() + " year=" + year.ToString());

                lps.dt = new DateTime(year, month, day, hour, minute, 0);

                //WriteToLog("Last power slice: Reload=" + lps.reload.ToString() +
                //                            "; Address=" + lps.addr.ToString() +
                //                            "; Datetime=" + lps.dt.ToShortDateString() + " " + lps.dt.ToShortTimeString());
            }
            catch (Exception ex)
            {
                WriteToLog("ReadCurrentPowerSliceInfo - " + ex.Message);
                return false;
            }
            return true;
        }

        /*
        public bool ReadCurrentValues(ref Values values)
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RCURRENT_ANSW_SIZE];

            bool[] already_read_tarif = new bool[4] { false, false, false, false };

            for (int t = 0; t < m_types_for_read_expense.Count; t++)
            {
                byte tarif = 0;
                switch (m_types_for_read_expense[t])
                {
                    case (byte)TypesValues.Tarif1AP:
                    case (byte)TypesValues.Tarif1AM:
                    case (byte)TypesValues.Tarif1RP:
                    case (byte)TypesValues.Tarif1RM:
                        tarif = 1;
                        break;
                    case (byte)TypesValues.Tarif2AP:
                    case (byte)TypesValues.Tarif2AM:
                    case (byte)TypesValues.Tarif2RP:
                    case (byte)TypesValues.Tarif2RM:
                        tarif = 2;
                        break;
                    case (byte)TypesValues.Tarif3AP:
                    case (byte)TypesValues.Tarif3AM:
                    case (byte)TypesValues.Tarif3RP:
                    case (byte)TypesValues.Tarif3RM:
                        tarif = 3;
                        break;
                    case (byte)TypesValues.Tarif4AP:
                    case (byte)TypesValues.Tarif4AM:
                    case (byte)TypesValues.Tarif4RP:
                    case (byte)TypesValues.Tarif4RM:
                        tarif = 4;
                        break;
                    default:
                        continue;
                }

                if (already_read_tarif[tarif - 1] == true)
                    continue;

                already_read_tarif[tarif - 1] = true;

                byte[] command = new byte[] { 0x05, 0x0 };

                command.CopyTo(cmnd, 0);
                cmnd[2] = tarif;

                if (!SendCommand(cmnd, ref answer, 3, RCURRENT_ANSW_SIZE))
                    continue;

                for (byte i = 0; i < m_energy; i++)
                {
                    if (m_types_for_read_expense.Contains(Convert.ToByte(tarif + i))) 
                    {
                        RecordValue recordValue;
                        recordValue.fine_state = false;
                        recordValue.value = 0;
                        recordValue.type = Convert.ToByte(tarif + i);

                        byte[] buff = new byte[4];
                        buff[0] = answer[1 + 3 + i * 4];
                        buff[1] = answer[1 + 2 + i * 4];
                        buff[2] = answer[1 + 1 + i * 4];
                        buff[3] = answer[1 + 0 + i * 4];

                        if (buff[0] != 0xFF & buff[1] != 0xFF & buff[2] != 0xFF & buff[3] != 0xFF)
                        {
                            recordValue.fine_state = true;
                            recordValue.value = Convert.ToSingle(BitConverter.ToUInt32(buff, 0)) / (2f * (float)m_gear_ratio);//1000f;//Вт -> кВт
                        }

                        values.listRV.Add(recordValue);
                    }
                }
            }

           // PowerQualityParams(ref values);

            return true;
        }
         * 
         * */

        public bool ReadMonthlyValues(byte month, ushort year, ref Values values)
        {
            if ((m_dt.Year < year) || (m_dt.Year == year && m_dt.Month < month))
            {
                return false;
            }

            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RMONTHLY_ANSW_SIZE_BASE + m_energy * 4];

            bool[] already_read_tarif = new bool[4] { false, false, false, false };

            for (int t = 0; t < m_types_for_read_expense.Count; t++)
            {
                byte tarif = 0;
                switch (m_types_for_read_expense[t])
                {
                    case (byte)TypesValues.Tarif1AP:
                    case (byte)TypesValues.Tarif1AM:
                    case (byte)TypesValues.Tarif1RP:
                    case (byte)TypesValues.Tarif1RM:
                        tarif = 1;
                        break;
                    case (byte)TypesValues.Tarif2AP:
                    case (byte)TypesValues.Tarif2AM:
                    case (byte)TypesValues.Tarif2RP:
                    case (byte)TypesValues.Tarif2RM:
                        tarif = 2;
                        break;
                    case (byte)TypesValues.Tarif3AP:
                    case (byte)TypesValues.Tarif3AM:
                    case (byte)TypesValues.Tarif3RP:
                    case (byte)TypesValues.Tarif3RM:
                        tarif = 3;
                        break;
                    case (byte)TypesValues.Tarif4AP:
                    case (byte)TypesValues.Tarif4AM:
                    case (byte)TypesValues.Tarif4RP:
                    case (byte)TypesValues.Tarif4RM:
                        tarif = 4;
                        break;
                    default:
                        continue;
                }

                if (already_read_tarif[tarif - 1] == true)
                    continue;

                already_read_tarif[tarif - 1] = true;

                byte[] command = new byte[] { 0x0A, 0x83 };

                command.CopyTo(cmnd, 0);
                cmnd[2] = month;
                cmnd[3] = tarif;
                cmnd[4] = 0xF;//A+;A-;R+;R-
                cmnd[5] = 0;

                if (!SendCommand(cmnd, ref answer, 6, RMONTHLY_ANSW_SIZE_BASE + m_energy * 4))
                    continue;

                for (byte i = 0; i < m_energy; i++)
                {
                    if (m_types_for_read_expense.Contains(Convert.ToByte(tarif + i))) // + вид энергии: 0-A+;1-A-;2-R+;3-R-
                    {
                        RecordValue recordValue;
                        recordValue.fine_state = false;
                        recordValue.value = 0;
                        recordValue.type = Convert.ToByte(tarif + i);

                        byte[] buff = new byte[4];
                        buff[0] = answer[1 + 3 + i * 4];
                        buff[1] = answer[1 + 2 + i * 4];
                        buff[2] = answer[1 + 1 + i * 4];
                        buff[3] = answer[1 + 0 + i * 4];

                        if (buff[0] != 0xFF & buff[1] != 0xFF & buff[2] != 0xFF & buff[3] != 0xFF)
                        {
                            recordValue.fine_state = true;
                            recordValue.value = Convert.ToSingle(BitConverter.ToUInt32(buff, 0)) / (2f * (float)m_gear_ratio);//1000f;//Вт -> кВт
                        }

                        values.listRV.Add(recordValue);
                    }
                }
            }

            return true;
        }

        public bool ReadDailyValues(byte day, byte month, ushort year, ref Values values)
        {
            return false;
        }

        /// <summary>
        /// Возвращает дату последней инициализации массива срезов
        /// </summary>
        /// <param name="lastInitDt"></param>
        /// <returns></returns>
        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {/*
            const bool WRITE_LOG = true;
            byte firstRecordIndex = 0;
            byte lastRecordIndex = 9;

            byte[] cmnd = new byte[32];
            byte[] answer = new byte[9];
            byte[] command = new byte[] { 0x04, 0x0A };
            byte status = 0;

            cmnd[0] = command[0];
            cmnd[1] = command[1];

            List<DateTime> initJournal = new List<DateTime>(10);

            for (byte i = firstRecordIndex; i <= lastRecordIndex; i++)
            {
                cmnd[2] = i;

                if (!SendCommand(cmnd, ref answer, 3, 9, ref status))
                    return false;

                int year = (int)BCDToByte(answer[6]);
                int month = (int)BCDToByte(answer[5]);
                int day = (int)BCDToByte(answer[4]);
                int hour = (int)BCDToByte(answer[3]);
                int minute = (int)BCDToByte(answer[2]);

                if (year > 0 && month > 0 && day > 0)
                    year += 2000;
                else
                    continue;

                try
                {
                    DateTime dt = new DateTime(year, month, day, hour, minute, 0);
                    initJournal.Add(dt);
                }
                catch (Exception ex)
                {
                    WriteToLog("ReadSliceArrInitializationDate: запись " + i.ToString() + "некорректна: " + ex.Message);
                    continue;
                }
            }

            if (initJournal.Count == 0)
            {
                WriteToLog("ReadSliceArrInitializationDate: не найдено ни одной записи в журнале инициализации массива");
                return false;
            }

            //переберем записанные даты в поисках наиболее свежей

            DateTime latestDt = initJournal[0];
            byte index = 0;
            for (byte j = 0; j < initJournal.Count; j++)
                if (initJournal[j] > latestDt) { latestDt = initJournal[j]; index = j; }

            WriteToLog("ReadSliceArrInitializationDate: выбрана запись " + index.ToString() + ": " + latestDt.ToString(), WRITE_LOG);
            lastInitDt = latestDt;
          * */
            return false;
        }

#region Реализация методов интерфейса

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RCURRENT_ANSW_SIZE];
            byte[] command;

            if (param >= 0 && param <= 3)
            {
                //чтение массивов энергии от сброса
                command = new byte[] { 0x05, 0x0};
                command.CopyTo(cmnd, 0);

                if (tarif >= 0 && tarif <= 8)
                {
                    cmnd[2] = (byte)tarif;

                    if (SendCommand(cmnd, ref answer, 3, RCURRENT_ANSW_SIZE))
                    {
                        for (byte i = 0; i < m_energy; i++)
                        {
                            if (i != param) continue;

                            byte[] buff = new byte[4];
                            buff[0] = answer[1 + 3 + i * 4];
                            buff[1] = answer[1 + 2 + i * 4];
                            buff[2] = answer[1 + 1 + i * 4];
                            buff[3] = answer[1 + 0 + i * 4];

                            if (buff[0] != 0xFF & buff[1] != 0xFF & buff[2] != 0xFF & buff[3] != 0xFF)
                            {
                                recordValue = Convert.ToSingle(BitConverter.ToUInt32(buff, 0)) / (2f * (float)m_gear_ratio);//1000f;//Вт -> кВт
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    this.WriteToLog("ReadCurrentValues: неподдерживаемое значение tarif");
                    return false;
                }
            }
            else if (param >= 5 && param <= 8)
            {
                return ReadAuxilaryParams(param, (byte)tarif, ref recordValue);
            }
            else
            {
                this.WriteToLog("ReadCurrentValues: неподдерживаемое значение param");
                return false;
            }





            return false;  
        }

        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            if ((m_dt.Year < dt.Year) || (m_dt.Year == dt.Year && m_dt.Month < dt.Month))
                return false;

            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RMONTHLY_ANSW_SIZE_BASE + m_energy * 4];
            byte[] command;

            if (param >= 0 && param <= 3)
            {
                //чтение массивов энергии на начало текущего месяца
                command = new byte[] { 0x0A, 0x83 };
                command.CopyTo(cmnd, 0);

                if (tarif >= 0 && tarif <= 8)
                {
                    cmnd[2] = (byte)dt.Month;
                    cmnd[3] = (byte)tarif;
                    cmnd[4] = 0xF; //маскировочный байт для получения только 00001111 = A+,A-,R+,R-
                    cmnd[5] = 0;  //резерв        

                    if (SendCommand(cmnd, ref answer, 6, RCURRENT_ANSW_SIZE))
                    {
                        for (byte i = 0; i < m_energy; i++)
                        {
                            if (i != param) continue;

                            byte[] buff = new byte[4];
                            buff[0] = answer[1 + 3 + i * 4];
                            buff[1] = answer[1 + 2 + i * 4];
                            buff[2] = answer[1 + 1 + i * 4];
                            buff[3] = answer[1 + 0 + i * 4];

                            if (buff[0] != 0xFF & buff[1] != 0xFF & buff[2] != 0xFF & buff[3] != 0xFF)
                            {
                                recordValue = Convert.ToSingle(BitConverter.ToUInt32(buff, 0)) / (2f * (float)m_gear_ratio);//1000f;//Вт -> кВт
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    this.WriteToLog("ReadCurrentValues: неподдерживаемое значение tarif");
                    return false;
                }
            }
            else
            {
                this.WriteToLog("ReadCurrentValues: неподдерживаемое значение param");
                return false;
            }

            return false;
        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            //значение dt не требуется при запроса 0x84 
            if ((m_dt.Year < dt.Year) || (m_dt.Year == dt.Year && m_dt.Month < dt.Month))
                return false;

            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RMONTHLY_ANSW_SIZE_BASE + m_energy * 4];
            byte[] command;

            if (param >= 0 && param <= 3)
            {
                //чтение массивов энергии на начало текущего месяца
                command = new byte[] { 0x0A, 0x84 };
                command.CopyTo(cmnd, 0);

                if (tarif >= 0 && tarif <= 8)
                {
                    cmnd[2] = 0;//при запрсе 84 не актуален 
                    cmnd[3] = (byte)tarif;
                    cmnd[4] = 0xF; //маскировочный байт для получения только 00001111 = A+,A-,R+,R-
                    cmnd[5] = 0;  //резерв        

                    if (SendCommand(cmnd, ref answer, 6, RCURRENT_ANSW_SIZE))
                    {
                        for (byte i = 0; i < m_energy; i++)
                        {
                            if (i != param) continue;

                            byte[] buff = new byte[4];
                            buff[0] = answer[1 + 3 + i * 4];
                            buff[1] = answer[1 + 2 + i * 4];
                            buff[2] = answer[1 + 1 + i * 4];
                            buff[3] = answer[1 + 0 + i * 4];

                            if (buff[0] != 0xFF & buff[1] != 0xFF & buff[2] != 0xFF & buff[3] != 0xFF)
                            {
                                recordValue = Convert.ToSingle(BitConverter.ToUInt32(buff, 0)) / (2f * (float)m_gear_ratio);//1000f;//Вт -> кВт
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    this.WriteToLog("ReadCurrentValues: неподдерживаемое значение tarif");
                    return false;
                }
            }
            else
            {
                this.WriteToLog("ReadCurrentValues: неподдерживаемое значение param");
                return false;
            }

            return false;
        }

        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<iMeters.RecordPowerSlice> listRPS, byte period)
        {
            if (ReadPowerSlice(ref listRPS, dt_begin, 30))
                return true;
            else            
                return false;
        }


        public bool SyncTime(DateTime dt)
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[WPARAMS_ANSW_SIZE];
            byte[] command = new byte[] { 0x03, 0x0D };

            command.CopyTo(cmnd, 0);

            cmnd[2] = (byte)dt.Second;
            cmnd[3] = (byte)dt.Minute;
            cmnd[4] = (byte)dt.Hour;

            return SendCommand(cmnd, ref answer, 5, WPARAMS_ANSW_SIZE);
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            byte[] answer = new byte[RSERIALNUMBER_ANSW_SIZE];
            byte[] command = new byte[] { 0x08, 0x0 };

            if (!SendCommand(command, ref answer, 2, RSERIALNUMBER_ANSW_SIZE))
                return false;

            byte[] temp = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                temp[i] = answer[4 - i];
            }

            serial_number = BitConverter.ToUInt32(temp, 0).ToString();

            return true;
        }

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            List<byte> listTypes = new List<byte>();

            switch (common_category)
            {
                case CommonCategory.Current:
                    for (byte type = (byte)TypesValues.Tarif1AP; type <= (byte)TypesValues.Tarif4RM; type++)
                    {
                        listTypes.Add(type);
                    }
                    for (byte type = (byte)TypesValues.Frequency; type <= (byte)TypesValues.QC; type++)
                    {
                        listTypes.Add(type);
                    }
                    break;
                case CommonCategory.Monthly:
                    for (byte type = (byte)TypesValues.Tarif1AP; type <= (byte)TypesValues.Tarif4RM; type++)
                    {
                        listTypes.Add(type);
                    }
                    break;
                case CommonCategory.Inday:
                    for (byte type = (byte)TypesValues.PowerSliceAP; type <= (byte)TypesValues.PowerSliceRM; type++)
                    {
                        listTypes.Add(type);
                    }
                    break;
            }

            return listTypes;
        }

        public bool OpenLinkCanal()
        {
            if (!Open())
                return false;

            if (!ReadVariantExecute())
                return false;

            if (!ReadDateTime())
                return false;

            return true;
        }

#endregion



        public bool ReadDailyValues(uint recordId, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }


        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period)
        {
            return false;
        }
    }

    public static class CommonMeters
    {
        /// <summary>
        /// перевод из DEC в HEX
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte DEC2HEX(byte value)
        {
            return Convert.ToByte((value >> 4) * 10 + (value & 0xF));
        }

        /// <summary>
        /// перевод из HEX в DEC
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte HEX2DEC(byte value)
        {
            return Convert.ToByte(((value / 10) << 4) + (value % 10));
        }

        /// <summary>
        /// Изменение порядка байт
        /// </summary>
        /// <param name="source"></param>
        /// <param name="start_pos"></param>
        /// <param name="bytes_count"></param>
        public static byte[] InverseBytesOrder(byte[] source, uint start_pos, int bytes_count)
        {
            byte[] right_order_bytes = new byte[bytes_count];

            for (int i = 0; i < bytes_count; i++)
            {
                right_order_bytes[i] = source[start_pos - i];
            }

            return right_order_bytes;
        }

        /// <summary>
        /// В двоично-десятичный формат
        /// </summary>
        public static byte[] IntToBCD(int input)
        {
            byte[] bcd = new byte[] 
            { 
                (byte)(input>> 8), 
                (byte)(input& 0x00FF) 
            };

            return bcd;
        }

        /// <summary>
        /// В двоично-десятичный формат
        /// </summary>
        public static byte[] ByteToBCD(int input)
        {
            byte[] bcd = new byte[] 
            { 
                (byte)(input>> 8), 
                (byte)(input& 0x00FF) 
            };

            return bcd;
        }

        /// <summary>
        /// BCD в байт
        /// </summary>
        /// <param name="bcds"></param>
        /// <returns></returns>
        public static byte BCDToByte(byte bcds)
        {
            byte result = 0;

            result = Convert.ToByte(10 * (byte)(bcds >> 4));
            result += Convert.ToByte(bcds & 0xF);

            return result;
        }

    }
}
