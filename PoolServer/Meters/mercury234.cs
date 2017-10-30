using System;
using System.Collections.Generic;

//using Prizmer.Meters.iMeters;
//using Prizmer.Ports;

using Drivers.LibMeter;
using PollingLibraries.LibPorts;


namespace Prizmer.Meters
{

    class m234 : Drivers.LibMeter.CMeter, Drivers.LibMeter.IMeter
    {
        public struct RecordValueEnergy
        {
            public float APlus;
            public float AMinus;
            public float RPlus;
            public float RMinus;
            public byte type;
        };

        public struct RecordParamsEnergy
        {
            public float phase_sum;
            public float phase_1;
            public float phase_2;
            public float phase_3;
            public byte type;
        };

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

        private byte m_maskaPhase = 1;
        private byte m_maskaEnergy = 1;
        private byte m_maskaTarif = 1;
        private bool m_presenceTarif = false;
        private ushort InitCRC = 0xFFFF;
        private bool m_presenceProfile;
        private ushort m_GearRatio = 1;
        private byte[] m_crc = new byte[2];
        private byte[] m_cmd = new byte[256];
        private byte[] m_password = new byte[6];
        private uint m_version = 0;
        private bool m_is_opened = false;
        private ushort m_diff_slices = 0;

        //private VirtualPort m_vport = null;

        private const byte TEST_ANSW_SIZE = 4;
        private const byte OPEN_ANSW_SIZE = 4;
        private const byte CLOSE_ANSW_SIZE = 4;
        private const byte WPARAMS_ANSW_SIZE = 4;
        private const byte WPHYSADDR_ANSW_SIZE = 4;
        private const byte RCURTIME_ANSW_SIZE = 11;
        private const byte RENERGYARR_ANSW_SIZE = 16;
        private const byte RVERSION_ANSW_SIZE = 6;
        private const byte RVAREXEC_ANSW_SIZE1 = 6;
        private const byte RVAREXEC_ANSW_SIZE2 = 9;
        private const byte RTIME_ANSW_TRANSFER = 9;
        private const byte RSN_ANSW_SIZE = 10;

        private const byte RPOWER_SIZE = 15;
        private const byte RKOEF_SIZE = 15;
        private const byte RVOLTAGE_SIZE = 12;
        private const byte RTOK_SIZE = 12;
        private const byte RANGLE_SIZE = 12;
        private const byte RFREQUENCY_SIZE = 6;

        private const byte RLASTSLICE_ANSW_SIZE = 12;
        private const byte RMEDSLICE1_ANSW_SIZE = 8;
        private const byte RPHYSADDR_ANSW_SIZE = 5;
        private const byte RMONTHLY_ANSW_SIZE = 19;
        private const byte RDAILY_ANSW_SIZE = 19;
        private const byte RCURRENT_ANSW_SIZE = 19;
        private const byte RSLICE_ANSW_SIZE = 18;
        private const byte REVENTTIME_ANSW_SIZE = 10;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="address"></param>
        /// <param name="password"></param>
        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            byte[] password = new byte[pass.Length];

            for (int j = 0; j < password.Length; j++)
            {
                password[j] = Convert.ToByte(pass[j]);
                password[j] -= 0x30;
            }

          
            

            //if (address > 239) this.m_address = address - 239;
            //else this.m_address = address;

            if (address == 0)
            {
                WriteToLog("Init: Не возможно проинициализировать драйвер m230 с адресом 0");
                return;
            } 

            this.m_address = address % 239;
            if (m_address == 0) m_address = 239;

            password.CopyTo(this.m_password, 0);

            m_vport = data_vport;
        }

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            List<byte> listTypes = new List<byte>();

            switch (common_category)
            {
                case CommonCategory.Current:
                case CommonCategory.Monthly:
                case CommonCategory.Daily:
                    for (byte type = 1; type <= 5; type++)
                    {
                        listTypes.Add(type);
                    }
                    break;
                case CommonCategory.Inday:
                    break;
            }

            return listTypes;
        }

        /// <summary>
        /// перевод из DEC в HEX
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private byte dec2hex(byte value)
        {
            return Convert.ToByte((value >> 4) * 10 + (value & 0xF));
        }

        /// <summary>
        /// перевод из HEX в DEC
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private byte hex2dec(byte value)
        {
            return Convert.ToByte(((value / 10) << 4) + (value % 10));
        }

        /// <summary>
        /// расчет контрольной суммы
        /// </summary>
        /// <param name="StrForCRC"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private ushort CalcCRC(ref byte[] StrForCRC, ushort size)
        {
            ushort crc = UpdateCRC(StrForCRC[0], InitCRC);

            for (ushort i = 1; i < size; i++)
            {
                crc = UpdateCRC(StrForCRC[i], crc);
            }
            this.m_crc[0] = Convert.ToByte(crc / 256);
            this.m_crc[1] = Convert.ToByte(crc % 256);

            return BitConverter.ToUInt16(this.m_crc, 0);
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
        /// открытие канала связи
        /// </summary>
        /// <returns></returns>
        public bool OpenLinkCanal()
        {
            bool res = true;

            // Тест канала связи
            if (Test() == false)
            {
                return false;
            }

            // открытие канала связи
            if (this.Open() == false)
                return false;

            // читаем версию счетчика
            if (this.ReadVersionMeter() == false)
                return false;

            // читаем вариант исполнения счетчика
            if (this.ReadVariantExecute() == false)
                return false;

            return res;
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
        private bool SendCommand(byte[] cmnd, ref byte[] answer, ushort cmd_size, ushort answ_size, ref byte status)
        {
            bool res = false;

            // формирование команды
            this.MakeCommand(cmnd, ref cmd_size);

            if (this.m_vport != null)
            {
                //this.m_vport.Write(this.m_cmd, cmd_size);

                //ushort size = Convert.ToUInt16(this.m_vport.Read(ref answer));//DelayMs(this.m_DirectTimeoutAnswer);

                int size = this.m_vport.WriteReadData(FindPacketSignature, this.m_cmd, ref answer, cmd_size, answ_size);

                if (size == answ_size)
                {
                    //проверка пришедших данных
                    if (!this.FinishAccept(answer, Convert.ToUInt16(size)))
                        return false;

                    // пришедшие данные корректны
                    res = true;
                }
                else if (size == 4)
                {
                    status = answer[1];
                    res = this.CheckStatusByte(answer[1]);
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
            Array.Clear(this.m_cmd, 0, this.m_cmd.Length);

            // Добавление сетевого адреса прибора в начало посылки
            this.m_cmd[0] = (byte)this.m_address;

            // Добавление данных в посылку
            for (int i = 0; i < size; i++)
                this.m_cmd[1 + i] = cmnd[i];

            // Вычисляем CRC
            this.CalcCRC(ref this.m_cmd, Convert.ToUInt16(size + 1));

            // Добавляем контрольную сумму к команде
            for (int j = 0; j < 2; j++)
                this.m_cmd[size + 1 + j] = this.m_crc[j];

            size += 3;
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
            if (size < 4)
            {
                return false;
            }

            // проверяем адрес прибора в ответе
            if (answer[0] != this.m_address)
            {
                return false;
            }

            // проверяем CRC
            this.CalcCRC(ref answer, Convert.ToUInt16(size - 2));

            for (int i = 0; i < 2; i++)
                crc[i] = answer[size - 2 + i];

            if (crc.Equals(this.m_crc))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// проверка статуса ответа
        /// </summary>
        /// <param name="status_byte"></param>
        /// <returns></returns>
        private bool CheckStatusByte(byte status_byte)
        {
            status_byte = 0xF;
            if (status_byte == 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// тест канала связи
        /// </summary>
        /// <returns></returns>
        private bool Test()
        {
            byte[] command = new byte[1] { 0x0 };
            byte[] answer = new byte[19];
            byte status = 0;

            if (!SendCommand(command, ref answer, 1, 4, ref status))
                return false;

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
            byte[] command = new byte[] { 0x01, 0x01 };
            byte status = 0;

            if (!m_is_opened)
            {
                command.CopyTo(cmnd, 0);
                this.m_password.CopyTo(cmnd, 2);

                if (!this.SendCommand(cmnd, ref answer, 8, OPEN_ANSW_SIZE, ref status))
                    return false;

                m_is_opened = true;
            }

            return true;
        }

        /// <summary>
        /// Чтение версии счетчика
        /// </summary>
        /// <returns></returns>
        private bool ReadVersionMeter()
        {
            byte[] answer = new byte[RVERSION_ANSW_SIZE];
            byte[] command = new byte[] { 0x08, 0x03 };
            byte status = 0;

            if (!this.SendCommand(command, ref answer, 2, RVERSION_ANSW_SIZE, ref status))
                return false;

            this.m_version = Convert.ToUInt32(Convert.ToUInt32(answer[1] * 10000) + Convert.ToUInt32(answer[2] * 100) + Convert.ToUInt32(answer[3]));

            return true;
        }

        /// <summary>
        /// Чтение варианта исполнения счетчика
        /// </summary>
        /// <returns></returns>
        private bool ReadVariantExecute()
        {
            byte[] answer = new byte[RVAREXEC_ANSW_SIZE2];
            byte[] command = new byte[] { 0x08, 0x12 };
            byte status = 0;

            if (this.m_version <= 20100)
            {
                if (!this.SendCommand(command, ref answer, 2, RVAREXEC_ANSW_SIZE1, ref status))
                    return false;
            }
            else if (this.m_version >= 20100)
            {
                if (!this.SendCommand(command, ref answer, 2, RVAREXEC_ANSW_SIZE2, ref status))
                    return false;
            }

            // определяем, ведет ли счетчик профиль мощности
            if (((answer[2] >> 5) & 0x1) == 1)
                this.m_presenceProfile = true;
            else
                this.m_presenceProfile = false;

            // определяем наличие тарификатора в счетчике
            this.m_maskaTarif = 0;
            if (((answer[3] >> 6) & 0x1) == 1)
            {
                this.m_presenceTarif = true;
                this.m_maskaTarif = 0x1F;
            }

            // кол-во фаз
            if (((answer[2] >> 4) & 0x1) == 0)
                this.m_maskaPhase = 0x07;

            // число направлений учитываемых счетчиком
            if (((answer[2] >> 7) & 0x1) == 1)
                this.m_maskaEnergy = 0x05;

            // тип учитываемой энергии
            if (((answer[3] >> 4) & 0x1) == 1)
                this.m_maskaEnergy = 0x03;

            // определяем передаточное число
            switch (answer[2] & 0xF)
            {
                case 0:
                    this.m_GearRatio = 5000;
                    break;
                case 1:
                    this.m_GearRatio = 25000;
                    break;
                case 2:
                    this.m_GearRatio = 1250;
                    break;
                case 3:
                    this.m_GearRatio = 500;
                    break;
                case 4:
                    this.m_GearRatio = 1000;
                    break;
                case 5:
                    this.m_GearRatio = 250;
                    break;
                default:
                    this.m_GearRatio = 1;
                    break;
            }

            return true;
        }

        /// <summary>
        /// Чтение даты/времени счетчика
        /// </summary>
        /// <param name="date_time"></param>
        /// <returns></returns>
        private bool ReadDateTime(out DateTime date_time)
        {
            byte[] answer = new byte[RCURTIME_ANSW_SIZE];
            byte[] command = new byte[] { 0x04, 0x00 };
            byte status = 0;

            date_time = new DateTime();

            if (!SendCommand(command, ref answer, 2, RCURTIME_ANSW_SIZE, ref status))
                return false;

            // конвертируем время из DEC в HEX
            byte second = this.dec2hex(answer[1]);
            byte minute = this.dec2hex(answer[2]);
            byte hour = this.dec2hex(answer[3]);
            byte wday = this.dec2hex(answer[4]);
            byte day = this.dec2hex(answer[5]);
            byte month = this.dec2hex(answer[6]);
            byte year = this.dec2hex(answer[7]);

            date_time = new DateTime(year, month, day, hour, minute, second, 0);

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
            byte[] command = new byte[] { 0x08, 0x16 };
            byte answ_size = 0;
            byte status = 0;
            ushort temp_word = 0;
            uint tmpValue = 0;
            int asign = 1;

            switch (paramType)
            {
                case 0x0:
                case 0x4:
                case 0x8:
                    answ_size = RPOWER_SIZE;
                    break;
                case 0x11:
                    answ_size = RVOLTAGE_SIZE;
                    break;
                case 0x21:
                    answ_size = RTOK_SIZE;
                    break;
                case 0x30:
                    answ_size = RKOEF_SIZE;
                    break;
                case 0x40:
                    answ_size = RFREQUENCY_SIZE;
                    break;
                case 0x51:
                    answ_size = RANGLE_SIZE;
                    break;
                default:
                    return false;
            }

            command.CopyTo(cmnd, 0);

            cmnd[2] = paramType;

            if (!SendCommand(cmnd, ref answer, 3, answ_size, ref status))
                return false;

            switch (answ_size)
            {
                case 6:
                    recParams.phase_1 = 0;
                    recParams.phase_2 = 0;
                    recParams.phase_3 = 0;
                    temp_word = BitConverter.ToUInt16(answer, 2);
                    if (temp_word == 0xFFFF)
                        temp_word = 0;
                    recParams.phase_sum = Convert.ToUInt32(temp_word);
                    break;
                case 12:
                    // фаза 1
                    temp_word = BitConverter.ToUInt16(answer, 2);
                    if (temp_word == 0xFFFF)
                        temp_word = 0;
                    recParams.phase_1 = Convert.ToSingle(temp_word);

                    // фаза 2
                    temp_word = BitConverter.ToUInt16(answer, 5);
                    if (temp_word == 0xFFFF)
                        temp_word = 0;
                    recParams.phase_2 = Convert.ToSingle(temp_word);

                    // фаза 3
                    temp_word = BitConverter.ToUInt16(answer, 8);
                    if (temp_word == 0xFFFF)
                        temp_word = 0;
                    recParams.phase_3 = Convert.ToSingle(temp_word);

                    break;
                case 15:
                    // по сумме фаз
                    asign = (1 + (-2 * ((answer[1] >> 7) & 1)));
                    tmpValue = Convert.ToUInt32(answer[1] & 0x3F);
                    //int asign = (1 + (-2 * ((answer[1] >> 6) & 1)));
                    tmpValue <<= 16;
                    temp_word = BitConverter.ToUInt16(answer, 2);
                    tmpValue += temp_word;
                    recParams.phase_sum = Convert.ToSingle(tmpValue * asign);

                    // фаза 1
                    asign = (1 + (-2 * ((answer[4] >> 7) & 1)));
                    tmpValue = Convert.ToUInt32(answer[4] & 0x3F);
                    tmpValue <<= 16;
                    temp_word = BitConverter.ToUInt16(answer, 5);
                    tmpValue += temp_word;
                    recParams.phase_1 = Convert.ToSingle(tmpValue * asign);

                    // фаза 2
                    asign = (1 + (-2 * ((answer[7] >> 7) & 1)));
                    tmpValue = Convert.ToUInt32(answer[7] & 0x3F);
                    tmpValue <<= 16;
                    temp_word = BitConverter.ToUInt16(answer, 8);
                    tmpValue += temp_word;
                    recParams.phase_2 = Convert.ToSingle(tmpValue * asign);

                    // фаза 3
                    asign = (1 + (-2 * ((answer[10] >> 7) & 1)));
                    tmpValue = Convert.ToUInt32(answer[10] & 0x3F);
                    tmpValue <<= 16;
                    temp_word = BitConverter.ToUInt16(answer, 11);
                    tmpValue += temp_word;
                    recParams.phase_3 = Convert.ToSingle(tmpValue * asign);
                    break;
                default:
                    break;
            }

            return true;
        }

        /// <summary>
        /// Чтение параметров качества энергии
        /// </summary>
        /// <param name="listRecordsParamEnergy"></param>
        /// <returns></returns>
        public void PowerQualityParams(out List<RecordParamsEnergy> listRecordsParamEnergy)
        {
            listRecordsParamEnergy = new List<RecordParamsEnergy>();

            RecordParamsEnergy recParams = new RecordParamsEnergy();

            // читаем напряжение
            if (this.ReadParams(0x11, ref recParams))
            {
                recParams.phase_1 /= 100f;
                recParams.phase_2 /= 100f;
                recParams.phase_3 /= 100f;
                recParams.type = 1;
                listRecordsParamEnergy.Add(recParams);
            }

            // читаем ток
            if (this.ReadParams(0x21, ref recParams))
            {
                recParams.phase_1 /= 1000f;
                recParams.phase_2 /= 1000f;
                recParams.phase_3 /= 1000f;
                recParams.type = 2;
                listRecordsParamEnergy.Add(recParams);
            }

            // читаем частоту сети
            if (this.ReadParams(0x40, ref recParams))
            {
                recParams.phase_sum /= 100f;
                recParams.type = 3;
                listRecordsParamEnergy.Add(recParams);
            }

            // угол между фазными напряжениями
            if (this.ReadParams(0x51, ref recParams))
            {
                recParams.phase_1 /= 100f;
                recParams.phase_2 /= 100f;
                recParams.phase_3 /= 100f;
                recParams.type = 4;
                listRecordsParamEnergy.Add(recParams);
            }

            // читаем коэффициенты мощности
            if (this.ReadParams(0x30, ref recParams))
            {
                recParams.phase_sum /= 1000f;
                recParams.phase_1 /= 1000f;
                recParams.phase_2 /= 1000f;
                recParams.phase_3 /= 1000f;
                recParams.type = 5;
                listRecordsParamEnergy.Add(recParams);
            }

            // читаем мощность P
            if (this.ReadParams(0x0, ref recParams))
            {
                recParams.phase_sum /= (100f * 1000f);
                recParams.phase_1 /= (100f * 1000f);
                recParams.phase_2 /= (100f * 1000f);
                recParams.phase_3 /= (100f * 1000f);
                recParams.type = 6;
                listRecordsParamEnergy.Add(recParams);
            }

            // читаем мощность S
            if (this.ReadParams(0x8, ref recParams))
            {
                recParams.phase_sum /= (100f * 1000f);
                recParams.phase_1 /= (100f * 1000f);
                recParams.phase_2 /= (100f * 1000f);
                recParams.phase_3 /= (100f * 1000f);
                recParams.type = 7;
                listRecordsParamEnergy.Add(recParams);
            }

            // читаем мощность Q
            if (this.ReadParams(0x4, ref recParams))
            {
                recParams.phase_sum /= (100f * 1000f);
                recParams.phase_1 /= (100f * 1000f);
                recParams.phase_2 /= (100f * 1000f);
                recParams.phase_3 /= (100f * 1000f);
                recParams.type = 8;
                listRecordsParamEnergy.Add(recParams);
            }
        }

        /// <summary>
        /// Синхронизация времени
        /// </summary>
        /// <param name="date_system"></param>
        /// <returns></returns>
        public bool SynchronizeClock(DateTime date_system)
        {
            DateTime date_counter;
            int delta = 0;

            // читаем дату/время счетчика
            if (!this.ReadDateTime(out date_counter))
                return false;

            // синхронизация не требуется
            if ((date_counter.Minute == date_system.Minute) && (date_counter.Second == date_system.Second))
                return true;

            // вычисляем рассинхронизацию
            delta = (date_system.Minute * 60 + date_system.Second) - (date_counter.Minute * 60 + date_counter.Second);

            // проверяем время на предмет перехода суток если до окончания суток осталось меньше 5 мин - синхронизацию не производим
            // тк есть вероятность при вычислении установочного времени перейти на след сутки + и еще время передачи команды
            // синхронизация попала на переход суток - отказ в выполнении задания
            if (date_counter.Day != date_system.Day)
                return true;

            // мягкая коррекция времени может производится в пределах +- 4 мин.
            if (delta > 240)
                delta = 239;
            if (delta < -240)
                delta = -239;

            byte second = 0;
            byte minute = Convert.ToByte(date_counter.Minute);
            minute += Convert.ToByte(delta / 60);
            byte hour = Convert.ToByte(date_counter.Hour);

            if (minute >= 60)
            {
                hour++;
                minute -= 60;
            }
            else if (minute <= 0)
            {
                hour--;
                minute += 60;
            }

            // конвертируем время из HEX в DEC
            second = hex2dec(second);
            minute = hex2dec(minute);
            hour = hex2dec(hour);

            //проводим синхронизацию
            return this.SoftCorrectionTime(second, minute, hour);

        }

        /// <summary>
        /// Мягкая коррекция времени
        /// </summary>
        /// <param name="second"></param>
        /// <param name="minute"></param>
        /// <param name="hour"></param>
        /// <returns></returns>
        private bool SoftCorrectionTime(byte second, byte minute, byte hour)
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[WPARAMS_ANSW_SIZE];
            byte[] command = new byte[] { 0x03, 0x0D };
            byte status = 0;

            command.CopyTo(cmnd, 0);

            //добавляем время
            cmnd[2] = second;
            cmnd[3] = minute;
            cmnd[4] = hour;

            if (!SendCommand(cmnd, ref answer, 5, WPARAMS_ANSW_SIZE, ref status))
                return false;

            return true;
        }

        /// <summary>
        /// Чтение текущих показаний по указанному тарифу
        /// </summary>
        /// <param name="tarif">0-сумма;1-1й;2-2й;3-3й;4-4й</param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        private bool ReadCurrentMeterageToTarif(ushort readparam, byte tarif, ref float recordValue)
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RCURRENT_ANSW_SIZE];
            byte[] command = new byte[] { 0x05, 0x0 };
            byte status = 0;

            command.CopyTo(cmnd, 0);
            cmnd[2] = tarif;

            if (!SendCommand(cmnd, ref answer, 3, RCURRENT_ANSW_SIZE, ref status))
                return false;

            ////////

            if (readparam > 4) return false;

            uint temp_value = 0;
            ushort temp_word = 0;
            temp_value = BitConverter.ToUInt16(answer, 1 + readparam * 4);
            temp_value <<= 16;
            temp_word = BitConverter.ToUInt16(answer, 3 + readparam * 4);
            temp_value += temp_word;

            // проверка 
            if (temp_value == 0xFFFFFFFF)
                temp_value = 0;

            switch (readparam)
            {
                case 0:
                    recordValue = Convert.ToSingle(temp_value);
                    break;
                case 1:
                    recordValue = Convert.ToSingle(temp_value);
                    break;
                case 2:
                    recordValue = Convert.ToSingle(temp_value);
                    break;
                case 3:
                    recordValue = Convert.ToSingle(temp_value);
                    break;
                case 4:
                    recordValue = Convert.ToSingle(temp_value);
                    break;
                default:
                    break;
            }
            recordValue /= 1000.0f;
            ///////////

            /*for(int i = 0; i < 4; i++)
            {
                uint temp_value = 0;
                ushort temp_word = 0;
                temp_value = BitConverter.ToUInt16(answer,1+i*4);
                temp_value <<= 16;
                temp_word =  BitConverter.ToUInt16(answer,3+i*4);
                temp_value += temp_word;

                // проверка 
                if(temp_value == 0xFFFFFFFF)
                    temp_value = 0;

                switch(i)
                {
                    case 0:
                        recordValue.APlus = Convert.ToSingle(temp_value);
                        break;
                    case 1:
                        recordValue.AMinus = Convert.ToSingle(temp_value);
                        break;
                    case 2:
                        recordValue.RPlus = Convert.ToSingle(temp_value);
                        break;
                    case 3:
                        recordValue.RMinus = Convert.ToSingle(temp_value);
                        break;
                    default:
                        break;
                }
            }*/

            return true;
        }

        /// <summary>
        /// Чтение показаний на начало суток по указанному тарифу и указанным суткам
        /// </summary>
        /// <param name="dailyArray"></param>
        /// <param name="tarif">0-сумма;1-1й;2-2й;3-3й;4-4й</param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        private bool ReadDailyMeterageToTarif(ushort readparam, byte tarif, ref float recordValue)
        {
            // Адреса памяти счетчика для чтения накопленной энергии по первому тарифу
            ushort[] AddrMemoryBeforeVer_2_1_0 = new ushort[2] { 0xAE0, 0xB80 };
            ushort[] AddrMemoryAfterVer_2_1_0 = new ushort[2] { 0x6A6, 0x6FB };
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RDAILY_ANSW_SIZE];
            byte[] command = new byte[] { 0x06, 0x02 };
            byte[] addr = new byte[2];
            ushort tar = 0;
            byte param = 0x10;
            byte status = 0;

            // в зависимости от версии счётчика
            if (this.m_version <= 20100)
            {
                tar = Convert.ToUInt16(AddrMemoryBeforeVer_2_1_0[0] + Convert.ToUInt16(tarif * 0x20));
            }
            else if (this.m_version >= 20100)
            {
                tar = Convert.ToUInt16(AddrMemoryAfterVer_2_1_0[0] + Convert.ToUInt16(tarif * 0x11));
            }
            // номер команды
            command.CopyTo(cmnd, 0);

            // переворачиваем байты в адресе
            addr = BitConverter.GetBytes(tar);
            cmnd[2] = addr[1];
            cmnd[3] = addr[0];

            // параметр
            cmnd[4] = param;

            if (!SendCommand(cmnd, ref answer, 5, RDAILY_ANSW_SIZE, ref status))
            {
                return false;
            }

            ////////

            if (readparam > 4) return false;

            uint temp_value = 0;
            ushort temp_word = 0;
            temp_value = BitConverter.ToUInt16(answer, 1 + readparam * 4);
            temp_value <<= 16;
            temp_word = BitConverter.ToUInt16(answer, 3 + readparam * 4);
            temp_value += (temp_word);

            // проверка 
            if (temp_value == 0xFFFFFFFF)
                temp_value = 0;

            // значение
            //float value = (Convert.ToSingle(temp_value) / (2 * this.m_GearRatio)) * 1000;//*1000 т.к. храним в Ватах
            float value = (Convert.ToSingle(temp_value) / (2 * this.m_GearRatio)) * 1000;

            switch (readparam)
            {
                case 0:
                    recordValue = value;
                    break;
                case 1:
                    recordValue = value;
                    break;
                case 2:
                    recordValue = value;
                    break;
                case 3:
                    recordValue = value;
                    break;
                case 4:
                    recordValue = value;
                    break;
                default:
                    break;
            }
            recordValue /= 1000.0f;
            ///////////

            /*

            for (int i = 0; i < 4; i++)
	        {
		        uint temp_value = 0;
		        ushort temp_word = 0;
		        temp_value = BitConverter.ToUInt16(answer,1+i*4);
                temp_value <<= 16;
                temp_word = BitConverter.ToUInt16(answer,3+i*4);
                temp_value += (temp_word);

                // проверка 
                if(temp_value == 0xFFFFFFFF)
                    temp_value = 0;

                // значение
                float value = (Convert.ToSingle(temp_value) / (2 * this.m_GearRatio))*1000;//*1000 т.к. храним в Ватах

                // заролняем структуру для возврата
                switch(i)
                {
                    case 0:
                        recordValue.APlus = value;
                        break;
                    case 1:
                        recordValue.AMinus = value;
                        break;
                    case 2:
                        recordValue.RPlus = value;
                        break;
                    case 3:
                        recordValue.RMinus = value;
                        break;
                    default:
                        break;
                }
	        }*/

            return true;
        }

        /// <summary>
        /// Чтение показаний на начало месяца по указанному тарифу и указанному месяцу
        /// </summary>
        /// <param name="tarif">0-сумма;1-1й;2-2й;3-3й;4-4й</param>
        /// <param name="month"></param>
        /// <param name="recordValue"></param>
        /// <returns></returns>        
        private bool ReadMonthlyMeterageToTarif(ushort readparam, byte tarif, byte month, ref float recordValue)
        {
            // Адреса памяти счетчика для чтения накопленной энергии по сумме тарифу
            ushort[] AddrMemoryBeforeVer_2_1_0 = new ushort[12] { 0x360, 0x400, 0x4A0, 0x540, 0x5E0, 0x680, 0x720, 0x7C0, 0x860, 0x900, 0x9A0, 0xA40 };
            ushort[] AddrMemoryAfterVer_2_1_0 = new ushort[12] { 0x2AA, 0x2FF, 0x354, 0x3A9, 0x3FE, 0x453, 0x4A8, 0x4FD, 0x552, 0x5A7, 0x5FC, 0x651 };
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RMONTHLY_ANSW_SIZE];
            byte[] command = new byte[] { 0x06, 0x02 };
            ushort tar = 0;
            byte param = 0x10;
            byte status = 0;

            // номер команды
            command.CopyTo(cmnd, 0);

            // в зависимости от версии счётчика
            if (this.m_version < 210)
                tar = Convert.ToUInt16(AddrMemoryBeforeVer_2_1_0[month - 1] + Convert.ToUInt16(tarif * 0x20));
            else
                tar = Convert.ToUInt16(AddrMemoryAfterVer_2_1_0[month - 1] + Convert.ToUInt16(tarif * 0x11));

            // номер команды
            command.CopyTo(cmnd, 0);

            // переворачиваем байты в адресе
            //cmnd[3] = Convert.ToByte(tar << 8);
            //cmnd[2] = Convert.ToByte(tar >> 8);
            cmnd[3] = (byte)(tar & 0xff);
            cmnd[2] = (byte)(tar >> 8);

            // параметр
            cmnd[4] = param;

            if (!SendCommand(cmnd, ref answer, 5, RMONTHLY_ANSW_SIZE, ref status))
            {
                return false;
            }

            ////////

            if (readparam > 4) return false;

            uint temp_value = 0;
            ushort temp_word = 0;
            temp_value = BitConverter.ToUInt16(answer, 1 + readparam * 4);
            temp_value <<= 16;
            temp_word = BitConverter.ToUInt16(answer, 3 + readparam * 4);
            temp_value += (temp_word);

            // проверка 
            if (temp_value == 0xFFFFFFFF)
                temp_value = 0;

            // значение
            float value = (Convert.ToSingle(temp_value) / (2 * this.m_GearRatio)) * 1000;//*1000 т.к. храним в Ватах

            switch (readparam)
            {
                case 0:
                    recordValue = value;
                    break;
                case 1:
                    recordValue = value;
                    break;
                case 2:
                    recordValue = value;
                    break;
                case 3:
                    recordValue = value;
                    break;
                case 4:
                    recordValue = value;
                    break;
                default:
                    break;
            }
            recordValue /= 1000.0f;
            ///////////

            /*for (int i = 0; i < 4; i++)
	        {
	            uint temp_value = 0;
	            ushort temp_word = 0;
	            temp_value = BitConverter.ToUInt16(answer,1+i*4);
                temp_value <<= 16;
                temp_word = BitConverter.ToUInt16(answer,3+i*4);
                temp_value += (temp_word);

                // проверка 
                if(temp_value == 0xFFFFFFFF)
                    temp_value = 0;

                // значение
                float value = (Convert.ToSingle(temp_value) / (2*this.m_GearRatio))*1000;//*1000 т.к. храним в Ватах

                // заролняем структуру для возврата       
                switch(i)
                {
                    case 0:
                        recordValue.APlus = value;
                        break;
                    case 1:
                        recordValue.AMinus = value;
                        break;
                    case 2:
                        recordValue.RPlus = value;
                        break;
                    case 3:
                        recordValue.RMinus = value;
                        break;
                    default:
                        break;
                }
	        }*/

            return true;
        }

        /// <summary>
        /// Чтение текущих показаний
        /// </summary>
        /// <param name="tarif">0 - по сумме тарифов, 1 - по 1му тарифу, и т.д.</param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            if ((tarif == 0) | (((this.m_maskaTarif >> tarif) & 0x1) == 1))
            {
                if (param >= 0 && param <= 4)
                {
                    bool r = this.ReadCurrentMeterageToTarif(param, (byte)tarif, ref recordValue);
                    recordValue = (float)Math.Round(recordValue, 2, MidpointRounding.AwayFromZero);
                    return r;
                }
                else if (param >= 5)
                {
                    bool r2 = this.ReadAuxilaryParams(param, (byte)tarif, ref recordValue);
                    recordValue = (float)Math.Round(recordValue, 2, MidpointRounding.AwayFromZero);
                    return r2;
                }
            }

            return false;
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

            const byte AUXILARY_PARAMS_11_ANSWER_BYTES = 6;

            byte[] cmnd = new byte[32];
            byte[] answer = new byte[AUXILARY_PARAMS_11_ANSWER_BYTES];
            byte[] command = new byte[] { 0x08, 0x11 };
            byte status = 0;

            command.CopyTo(cmnd, 0);

            byte BWRI = 0x0;
            switch (readparam)
            {
                case 5:
                    {
                        byte axParamNumber = 0x0;
                        //мощность P 
                        byte powerNumber = 0x0;
                        byte phaseNumber = (byte)(tarif & 0x01);

                        BWRI = (byte)(axParamNumber << 4 | powerNumber << 2 | phaseNumber);
                        cmnd[2] = (byte)BWRI;

                        if (!SendCommand(cmnd, ref answer, 3, AUXILARY_PARAMS_11_ANSWER_BYTES, ref status))
                            return false;

                        byte mask = 0x3F;
                        byte tempFirstBype = (byte)(answer[1] & mask);

                        byte[] tempArr = { 0x0, tempFirstBype, answer[3], answer[2] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempArr);

                        float value = BitConverter.ToUInt32(tempArr, 0);
                        recordValue = value / 100f;

                        return true;
                    }
                case 6:
                    {
                        byte axParamNumber = 0x0;
                        //мощность Q 
                        byte powerNumber = 0x01;
                        byte phaseNumber = (byte)(tarif & 0x01);

                        BWRI = (byte)(axParamNumber << 4 | powerNumber << 2 | phaseNumber);
                        cmnd[2] = (byte)BWRI;

                        if (!SendCommand(cmnd, ref answer, 3, AUXILARY_PARAMS_11_ANSWER_BYTES, ref status))
                            return false;

                        byte mask = 0x3F;
                        byte tempFirstBype = (byte)(answer[1] & mask);

                        byte[] tempArr = { 0x0, tempFirstBype, answer[3], answer[2] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempArr);

                        float value = BitConverter.ToUInt32(tempArr, 0);
                        recordValue = value / 100f;

                        return true;
                    }
                case 7:
                    {
                        //токи
                        byte axParamNumber = 0x02;
                        byte phaseNumber = (byte)(tarif & 0x0F);
                        if (phaseNumber == 0) return false;

                        BWRI = (byte)(axParamNumber << 4 | phaseNumber);
                        cmnd[2] = (byte)BWRI;

                        if (!SendCommand(cmnd, ref answer, 3, AUXILARY_PARAMS_11_ANSWER_BYTES, ref status))
                            return false;

                        byte[] tempArr = { 0x0, answer[1], answer[3], answer[2] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempArr);

                        float value = BitConverter.ToUInt32(tempArr, 0);
                        recordValue = value / 1000f;

                        return true;

                    }
                case 8:
                    {
                        //напряжения
                        byte axParamNumber = 0x01;
                        byte phaseNumber = (byte)(tarif & 0x0F);
                        if (phaseNumber == 0) return false;

                        BWRI = (byte)(axParamNumber << 4 | phaseNumber);
                        cmnd[2] = (byte)BWRI;

                        if (!SendCommand(cmnd, ref answer, 3, AUXILARY_PARAMS_11_ANSWER_BYTES, ref status))
                            return false;

                        byte[] tempArr = { 0x0, answer[1], answer[3], answer[2] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempArr);

                        float value = BitConverter.ToUInt32(tempArr, 0);
                        recordValue = value / 100f;

                        return true;

                    }

                default: return false;
            }
        }

        /// <summary>
        /// Чтение показаний на начало текущих суток
        /// </summary>
        /// <param name="date_time"></param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            if ((tarif == 0) | (((this.m_maskaTarif >> tarif) & 0x1) == 1))
            {
                bool r = this.ReadDailyMeterageToTarif(param, (byte)tarif, ref recordValue);
                recordValue = (float)Math.Round(recordValue, 2, MidpointRounding.AwayFromZero);
                return r;
            }


            return false;
        }

        /// <summary>
        /// Чтение показаний на начало месяца
        /// </summary>
        /// <param name="tarif"></param>
        /// <param name="month"></param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            if ((tarif == 0) | (((this.m_maskaTarif >> tarif) & 0x1) == 1))
            {
                return this.ReadMonthlyMeterageToTarif(param, (byte)tarif, (byte)dt.Month, ref recordValue);
            }

            return false;
        }

        /// <summary>
        /// Чтение информации о последнем зафиксированнрм счетчиком среза
        /// </summary>
        /// <param name="lps"></param>
        /// <returns></returns>
        public bool ReadLastSlice(ref LastPowerSlice lps)
        {
            byte[] answer = new byte[RLASTSLICE_ANSW_SIZE];
            byte[] command = new byte[] { 0x08, 0x13 };
            byte status = 0;

            // Читаем последний срез мощности для определения адреса физической памяти последней записи массива
            if (!SendCommand(command, ref answer, 2, RLASTSLICE_ANSW_SIZE, ref status))
                return false;

            // Длительность периода интенрирования
            lps.len = answer[9];

            // конвертируем время из DEC в HEX 
            lps.minute = dec2hex(answer[5]);
            lps.hour = dec2hex(answer[4]);
            lps.day = dec2hex(answer[6]);
            lps.month = dec2hex(answer[7]);
            lps.year = dec2hex(answer[8]) + 2000;

            if (lps.minute > 15 && lps.minute < 45)
                lps.minute = 30;
            if (lps.minute >= 45 && lps.minute <= 59)
                lps.minute = 60;

            // адрес последней записи
            lps.addr = BitConverter.ToUInt16(answer, 1);

            // признак перезаписи области профиля
            if ((answer[3] & 0x1) == 1)
                lps.reload = true;
            else
                lps.reload = false;

            // рассчёт адреса хранения самого раннего среза
            if (lps.reload)
                m_diff_slices = 4096;
            else
                m_diff_slices = Convert.ToUInt16(lps.addr / (ushort)0x10);

            return true;
        }

        // Чтение среза мощности по указанному адресу
        bool ReadSlice(ushort addr_slice, ref RecordPowerSlice record_slice, byte period, bool doReload = false)
        {
            byte[] cmnd = new byte[32];
            byte[] answer = new byte[RSLICE_ANSW_SIZE];

            //83 - c учетом 17 бита адреса, 03 - когда в формировании адреса участвует лишь 16 бит
            //предполагаю, что это связано с переполнением массива
            //TODO: уточнить как понять нужно ли использовать 17бит или нет
            byte stateByte = 0x03;
            if (doReload) stateByte = 0x83;
            byte[] command = new byte[] { 0x06, stateByte };

            byte[] addr = new byte[2];
            ushort temp_value = 0;
            float value = 0;
            byte status = 0;

            cmnd[0] = command[0];
            cmnd[1] = command[1];
            addr = BitConverter.GetBytes(addr_slice);
            cmnd[2] = addr[0];
            cmnd[3] = addr[1];
            cmnd[4] = 0x0F;
            this.Open();
            if (!SendCommand(cmnd, ref answer, 5, RSLICE_ANSW_SIZE, ref status))
            {
                if (status == 0x2)
                    record_slice.status = 0xFE;
                return false;
            }

            // длительность периода интегрирования
            record_slice.period = answer[7];

            // время записи среза
            int hour = dec2hex(answer[2]);
            int minute = dec2hex(answer[3]);
            int day = dec2hex(answer[4]);
            int month = dec2hex(answer[5]);
            int year = dec2hex(answer[6]) + 2000;

            try
            {
                record_slice.date_time = new DateTime(year, month, day, hour, minute, 0);
            }
            catch
            {
                return false;
            }

            // делаем даты кратными 00 и 30
            if (record_slice.date_time.Minute > 15 && record_slice.date_time.Minute < 30)
                record_slice.date_time = record_slice.date_time.AddMinutes(30 - record_slice.date_time.Minute);
            else if (record_slice.date_time.Minute > 30 && record_slice.date_time.Minute < 45)
            {
                record_slice.date_time = record_slice.date_time.AddMinutes(30 - record_slice.date_time.Minute);
            }
            else if (record_slice.date_time.Minute >= 45 && record_slice.date_time.Minute <= 59)
            {
                record_slice.date_time = record_slice.date_time.AddMinutes(60 - record_slice.date_time.Minute);
            }
            else if (record_slice.date_time.Minute > 0 && record_slice.date_time.Minute <= 15)
                record_slice.date_time = record_slice.date_time.AddMinutes(-1 * record_slice.date_time.Minute);

            // Статус среза
            if (((answer[1] >> 0x1) & 1) == 0)
                record_slice.status = 0;
            else
                record_slice.status = 0xFE;

            // Разбираем по видам энергии
            for (int i = 0; i <= 3; i++)
            {
                temp_value = BitConverter.ToUInt16(answer, 8 + i * 2);
                if (temp_value == 0xFFFF)
                    temp_value = 0;

                //string s = Convert.ToString(this.m_address) + ": Ratio: " + Convert.ToString(m_GearRatio) + "; tempValue=" + Convert.ToString(temp_value);
                //WriteToLog(s);

                value = Convert.ToSingle(((float)temp_value * (60 / period)) / ((float)m_GearRatio * 2 * 2));//*2 добавлено 24.03 в кВт

                switch (i)
                {
                    case 0: // активная прямая
                        record_slice.APlus = value;
                        break;
                    case 1: // активная обратная
                        record_slice.AMinus = value;
                        break;
                    case 2:  // реактивная прямая
                        record_slice.RPlus = value;
                        break;
                    case 3: // реактивная обратная
                        record_slice.RMinus = value;
                        break;
                }
            }

            return true;
        }


        /// <summary>
        /// BCD в байт - в дальнейшем перенести в модуль служебных функций
        /// </summary>
        /// <param name="bcds"></param>
        /// <returns></returns>
        byte BCDToByte(byte bcds)
        {
            byte result = 0;

            result = Convert.ToByte(10 * (byte)(bcds >> 4));
            result += Convert.ToByte(bcds & 0xF);

            return result;
        }

        /// <summary>
        /// Возвращает дату последней инициализации массива срезов
        /// </summary>
        /// <param name="lastInitDt"></param>
        /// <returns></returns>
        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            /*
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
            return true;
             */
            return false;
        }


        public bool ReadPowerSliceForM230AndOlder(DateTime dt_begin, DateTime dt_end, ref List<RecordPowerSlice> listRPS, byte period)
        {
            ushort addr_before = 0;
            ushort addr_after = 0;
            ushort diff = 0;
            byte[] tmp_buf = new byte[9];
            byte[] buf = new byte[2];
            LastPowerSlice lps = new LastPowerSlice();
            RecordPowerSlice record_slice = new RecordPowerSlice();
            DateTime dt_lastslice;

            // проверка: данный вариант исполнения счетчика не поддерживает учет срезов
            if (!m_presenceProfile)
            {
                return false;
            }

            // читаем последний срез
            if (!ReadLastSlice(ref lps))
            {
                return false;
            }

            try
            {
                // Время последнего среза из счётчика
                dt_lastslice = new DateTime(lps.year, lps.month, lps.day, lps.hour, lps.minute, 0);
            }
            catch
            {
                return false;
            }

            if (dt_begin >= dt_lastslice)
                return false;

            // Вычисляем разницу в минутах
            TimeSpan span = dt_lastslice - dt_begin;
            TimeSpan span2 = dt_lastslice - dt_end;

            int diff_minutes = Convert.ToInt32(span.TotalMinutes);
            int diff_minutes2 = Convert.ToInt32(span2.TotalMinutes);


            // если разница > max кол-ва хранящихся записей в счётчике, то не вычитываем их из счётчика
            while (diff_minutes >= (4096 * period))
            {
                dt_begin = dt_begin.AddMinutes(period);
                span = dt_lastslice - dt_begin;
                diff_minutes = span.Minutes;
            }

            ushort diff2 = 0;
            try
            {
                //Вычисляем разницу в срезах
                diff = Convert.ToUInt16(diff_minutes / period);
                diff2 = Convert.ToUInt16((diff_minutes2 / period) + 1);
            }
            catch (Exception ex)
            {
                WriteToLog("ReadPowerSlice: " + ex.ToString());
                WriteToLog("ReadPowerSlice: diff_minutes2, period: " + diff_minutes2 + ", " + period);
                WriteToLog("ReadPowerSlice: dt_begin, dt_end: " + dt_begin + ", " + dt_end);
                return false;
            }



            ushort address_slice = diff;

            // Увеличиваем время на 30 минут
            //dt_begin = dt_begin.AddMinutes(30);
            // Уменьшаем адрес среза
            //address_slice--;

            for (ushort i = diff2; i <= diff; i++)
            {
                // меняем байты в слове местами
                addr_before = Convert.ToUInt16(((lps.addr & 0xff) << 8) | ((lps.addr & 0xFF00) >> 8));
                // делаем смещение
                addr_before -= Convert.ToUInt16(address_slice * 0x10);
                // возвращаем байты на прежнее положение
                addr_after = Convert.ToUInt16(((addr_before & 0xff) << 8) | ((addr_before & 0xFF00) >> 8));

                // чтение среза по рассчитанному адресу
                bool res_read_slice = ReadSlice(addr_after, ref record_slice, period);
                //  this.Open();
                //  bool res_read_slice = ReadSlice(0x10f0, ref record_slice, period);

                // Если при чтении не было ошибок
                if (res_read_slice)
                {
                    // проверка на то, что прочитанный срез старый
                    if (dt_begin > record_slice.date_time)
                        record_slice.status = 0xFE;
                    else
                        listRPS.Add(record_slice);

                    /*
                    else if (dt_begin == record_slice.date_time)
                    {
                        listRPS.Add(record_slice);
                    }
                    */
                }

                if (address_slice > 0)
                {
                    // Увеличиваем время на 30 минут
                    dt_begin = dt_begin.AddMinutes(period);

                    // Уменьшаем адрес среза
                    address_slice--;
                }

            }

            return true;
        }


        /// <summary>
        /// Чтение срезов мощности за период времени
        /// </summary>
        /// <param name="dt_begin"></param>
        /// <param name="dt_end"></param>
        /// <param name="listRPS">Выходной лист срезов мощности</param>
        /// <param name="period">Время через которые записаны срезы (устанавливается при инициализации срезов мощности)</param>
        /// <returns></returns>
        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<RecordPowerSlice> listRPS, byte period)
        {
            //
            if (this.m_version < 90000)
            {
                WriteToLog("ReadPowerSlice: выполняю метод для 230х, версия " + this.m_version);
                return this.ReadPowerSliceForM230AndOlder(dt_begin, dt_end, ref listRPS, period);
            }

            WriteToLog("ReadPowerSlice: выполняю метод для 233+, версия " + this.m_version);
            ushort addr_before = 0;
            ushort addr_after = 0;
            ushort diff = 0;
            byte[] tmp_buf = new byte[9];
            byte[] buf = new byte[2];
            LastPowerSlice lps = new LastPowerSlice();
            RecordPowerSlice record_slice = new RecordPowerSlice();
            DateTime dt_lastslice;

            //TODO: здесь непонятно, но так делать нельзя
            //при запросе диапазона дат 00.30 - 2.30 -> выдается 1.00 - 3.00
            //все дело в том, что 234 данной версии воспринимает первую получасовку нового дня 24.01.2017 00:00 
            //как 23.01.2017 00:00, т.е. 23.01.2017 24:00
            bool moveDates = false;//this.m_version == 90000 ? true : false;
            if (moveDates)
            {
                dt_begin = dt_begin.AddMinutes(30);
                dt_end = dt_end.AddMinutes(30);
            }

            // проверка: данный вариант исполнения счетчика не поддерживает учет срезов
            if (!m_presenceProfile)
            {
                return false;
            }

            // читаем последний срез
            if (!ReadLastSlice(ref lps))
            {
                return false;
            }

            try
            {
                // Время последнего среза из счётчика
                dt_lastslice = new DateTime(lps.year, lps.month, lps.day, lps.hour, lps.minute, 0);
                if (dt_end > dt_lastslice) dt_end = dt_lastslice;
            }
            catch
            {
                return false;
            }

            if (dt_begin >= dt_lastslice)
                return false;

            // вычисляем разницу в минутах между последним срезом и запрашиваемым
            TimeSpan span = dt_lastslice - dt_begin;
            TimeSpan span2 = dt_lastslice - dt_end;
            int diff_minutes = Convert.ToInt32(span.TotalMinutes);
            int diff_minutes2 = Convert.ToInt32(span2.TotalMinutes);

            // если разница > max кол-ва хранящихся записей в счётчике, то не вычитываем их из счётчика ??
            while (diff_minutes >= (4096 * period))
            {
                dt_begin = dt_begin.AddMinutes(period);
                span = dt_lastslice - dt_begin;
                diff_minutes = span.Minutes;
            }

            //Вычисляем разницу в срезах
            diff = Convert.ToUInt16(diff_minutes / period);
            ushort diff2 = Convert.ToUInt16(diff_minutes2 / period);



            //кол-во срезов за требуемый промежуток времени
            ushort address_slice = diff;

            // Увеличиваем время на 30 минут
            //dt_begin = dt_begin.AddMinutes(30);
            // Уменьшаем адрес среза
            //address_slice--;

            for (ushort i = diff2; i <= diff; i++)
            {
                // меняем байты в слове адреса последнего среза местами
                addr_before = Convert.ToUInt16(((lps.addr & 0xff) << 8) | ((lps.addr & 0xFF00) >> 8));
                //получаем кол-во байт, которые занимают n записей за требуемый промежуток времени
                int tmpDif = address_slice * 0x10;
                //получаем реальный адрес последней записи (сдвинутый) пример
                //17 6e * 10h -> 76 e0
                int tmpAddr = addr_before * 0x10;
                //получаем реальный адрес получасовки на требуемую дату начала чтения
                addr_before = (ushort)(tmpAddr - tmpDif);
                // возвращаем байты на прежнее положение
                addr_after = Convert.ToUInt16(((addr_before & 0xff) << 8) | ((addr_before & 0xFF00) >> 8));

                bool reload = !lps.reload;
                bool secondChance = false;

            SECOND_CHANCE:

                bool res_read_slice = ReadSlice(addr_after, ref record_slice, period, reload);

                // Если при чтении не было ошибок
                if (res_read_slice)
                {
                    // проверка на то, что прочитанный срез старый
                    if (record_slice.date_time < dt_begin || record_slice.date_time > dt_end)
                    {
                        if (!secondChance)
                        {
                            secondChance = true;
                            reload = !reload;

                            goto SECOND_CHANCE;
                        }
                    }
                    else
                    {
                        if (moveDates)
                        {
                            if (record_slice.date_time.Ticks == record_slice.date_time.Date.Ticks)
                                record_slice.date_time = record_slice.date_time.Date.AddDays(-1);
                        }
                        listRPS.Add(record_slice);
                    }
                }
                else
                {
                    if (!secondChance)
                    {
                        secondChance = true;
                        reload = !reload;

                        goto SECOND_CHANCE;
                    }
                }

                if (address_slice > 0)
                {
                    // Увеличиваем время на 30 минут
                    dt_begin = dt_begin.AddMinutes(period);

                    // Уменьшаем адрес среза
                    address_slice--;
                }

            }

            return true;
        }

        private int FindPacketSignature(Queue<byte> queue)
        {
            return 0;
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            byte[] answer = new byte[RSN_ANSW_SIZE];
            byte[] command = new byte[] { 0x08, 0x00 };
            byte status = 0;



            if (!SendCommand(command, ref answer, 2, RSN_ANSW_SIZE, ref status))
                return false;

            try
            {
                byte[] serialBytesArr = new byte[4];
                serial_number = String.Empty;
   
                serialBytesArr[0] = answer[1];
                serialBytesArr[1] = answer[2];
                serialBytesArr[2] = answer[3];
                serialBytesArr[3] = answer[4];
           

                /*
                serialBytesArr[0] = 0x10;
                serialBytesArr[1] = 0x53;
                serialBytesArr[2] = 0x5A;
                serialBytesArr[3] = 0x0;
                 */

                for (int i = 0; i < 4; i++)
                {
                    /*если число 0x04 то в десятичном отображении префиксный 0 будет опущен, 
                     * добавим его самостоятельно */
                    if (serialBytesArr[i] < 10)
                        serial_number += "0";
                   
                    serial_number += serialBytesArr[i].ToString();
                }

                return true;

            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool SyncTime(DateTime dt)
        {
            return false;
        }

        public bool ReadDailyValues(uint recordId, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }

        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period)
        {
            return false;
        }
    }
}