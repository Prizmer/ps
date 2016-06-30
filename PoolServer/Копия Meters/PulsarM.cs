using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Prizmer.Meters.iMeters;
using Prizmer.Ports;

namespace Prizmer.Meters
{
    #region Заимствованные структуры
    public enum TypeDataPulsar : byte
    {
        Current = 0,
        Hourly = 1,
        Daily = 2,
        Monthly = 3
    }

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

    #endregion

    public sealed class PulsarM : CMeter, IMeter
    {
        Dictionary<byte, string> m_dictDataTypes = new Dictionary<byte, string>();
        List<byte> m_listTypesForRead = new List<byte>();

        private byte[] m_cmd;
        private byte m_length_cmd = 0;
        private byte m_max_canals = 16;

        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            m_address = address;
            this.m_vport = data_vport;


            for (byte t = 1; t <= m_max_canals; t++)
            {
                m_dictDataTypes.Add(t, "");
            }
            //m_log_file_name += this.GetType() + "_" + m_address.ToString();
        }

        private void SetTypesForRead(List<byte> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (m_dictDataTypes.ContainsKey(types[i]))
                {
                    m_listTypesForRead.Add(types[i]);
                }
            }
        }

        private void ClearTypesForRead()
        {
            m_listTypesForRead.Clear();
        }

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            List<byte> listTypes = new List<byte>();

            switch (common_category)
            {
                case CommonCategory.Current:
                case CommonCategory.Monthly:
                case CommonCategory.Daily:
                    for (byte type = 1; type <= m_max_canals; type++)
                    {
                        listTypes.Add(type);
                    }
                    break;
                case CommonCategory.Inday:
                    break;
            }

            return listTypes;
        }

        public bool OpenLinkCanal()
        {
            return true;
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            //serial_number = Convert.ToString(m_address);
            return false;
        }

        public bool SyncTime(DateTime dt)
        {
            m_length_cmd = 0;
            byte out_packet_length = 16;
            int bytes_to_read = 14;
            Byte[] in_buffer = new Byte[255];

            m_cmd = new byte[out_packet_length];

            // формируем команду 
            // адрес
            byte[] adr = new byte[4];
            Int2BCD((int)m_address, adr);
            for (int i = 0; i < adr.Length; i++)
            {
                m_cmd[m_length_cmd++] = adr[i];
            }
            // номер функции
            m_cmd[m_length_cmd++] = 5;
            // общая длина пакета
            m_cmd[m_length_cmd++] = out_packet_length;
            // параметры /*Время*/
            m_cmd[m_length_cmd++] = Convert.ToByte(dt.Year - 2000);
            m_cmd[m_length_cmd++] = Convert.ToByte(dt.Month);
            m_cmd[m_length_cmd++] = Convert.ToByte(dt.Day);
            m_cmd[m_length_cmd++] = Convert.ToByte(dt.Hour);
            m_cmd[m_length_cmd++] = Convert.ToByte(dt.Minute);
            m_cmd[m_length_cmd++] = Convert.ToByte(dt.Second);
            // ID
            ushort rnd = 0x50b9;
            byte[] random = BitConverter.GetBytes(rnd);
            for (int i = 0; i < random.Length; i++)
            {
                m_cmd[m_length_cmd++] = random[i];
            }
            // CRC16
            byte[] crc16 = CRC16(m_cmd, m_length_cmd);
            for (int i = 0; i < crc16.Length; i++)
            {
                m_cmd[m_length_cmd++] = crc16[i];
            }

            if (m_vport != null)
            {
                if (m_vport.WriteReadData(FindPacketSignature, m_cmd, ref in_buffer, m_length_cmd, bytes_to_read) > 0)
                {
                    bool find_header = true;

                    // длина пакета 
                    byte packet_length = 0;

                    // проверка заголовка пакета
                    for (int i = 0; i < 5; i++)
                    {
                        if (m_cmd[i] != in_buffer[i])
                        {
                            find_header = false;
                        }
                    }

                    if (find_header)
                    {
                        packet_length = in_buffer[5];
                        // проверка CRC
                        crc16 = CRC16(in_buffer, packet_length - 2);

                        if (in_buffer[packet_length - 2] == crc16[0] && in_buffer[packet_length - 1] == crc16[1])
                        {
                            // проверка ID
                            if (m_cmd[out_packet_length - 4] == in_buffer[packet_length - 4] && m_cmd[out_packet_length - 3] == in_buffer[packet_length - 3])
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool ReadCurrentValues(ref Values values)
        {
            m_length_cmd = 0;
            Byte[] in_buffer = new Byte[255];

            ushort rnd = 0x50b9;
            byte func = 0;

            uint channel_mask = 0;
            byte out_packet_length = 0;
            int bytes_to_read = -1;

            func = 1;
            out_packet_length = 14;

            // битовая маска каналов
            for (int i = 0; i < m_listTypesForRead.Count; i++)
            {
                uint tmp = 1;
                tmp = tmp << (m_listTypesForRead[i] - 1);
                channel_mask |= tmp;
            }

            // адрес
            byte[] adr = new byte[4];
            Int2BCD((int)m_address, adr);

            byte[] parameter = BitConverter.GetBytes(channel_mask);
            byte[] random = BitConverter.GetBytes(rnd);

            m_cmd = new byte[out_packet_length];

            // формируем команду 
            // адрес
            for (int i = 0; i < adr.Length; i++)
            {
                m_cmd[m_length_cmd++] = adr[i];
            }
            // номер функции
            m_cmd[m_length_cmd++] = func;
            // общая длина пакета
            m_cmd[m_length_cmd++] = out_packet_length;
            // параметры
            for (int i = 0; i < parameter.Length; i++)
            {
                m_cmd[m_length_cmd++] = parameter[i];
            }

            // ID
            for (int i = 0; i < random.Length; i++)
            {
                m_cmd[m_length_cmd++] = random[i];
            }

            // CRC16
            byte[] crc16 = CRC16(m_cmd, m_length_cmd);
            for (int i = 0; i < crc16.Length; i++)
            {
                m_cmd[m_length_cmd++] = crc16[i];
            }

            // байт для чтения
            bytes_to_read = 10 + 8 * m_listTypesForRead.Count;

            //WriteToLog("m_length_cmd=" + m_length_cmd.ToString());

            //WriteToLog("bytes_to_read=" + bytes_to_read.ToString());

            if (m_vport.WriteReadData(FindPacketSignature, m_cmd, ref in_buffer, m_length_cmd, bytes_to_read) > 0)
            {
                //WriteToLog("WriteReadData");
                bool find_header = true;

                // длина пакета 
                byte packet_length = 0;

                // проверка заголовка пакета
                for (int i = 0; i < 5; i++)
                {
                    if (m_cmd[i] != in_buffer[i])
                    {
                        find_header = false;
                    }
                }

                if (find_header)
                {
                    //WriteToLog("find_header");
                    packet_length = in_buffer[5];

                    // проверка CRC
                    crc16 = CRC16(in_buffer, packet_length - 2);

                    if (in_buffer[packet_length - 2] == crc16[0] && in_buffer[packet_length - 1] == crc16[1])
                    {
                        // проверка ID
                        if (m_cmd[out_packet_length - 4] == in_buffer[packet_length - 4] && m_cmd[out_packet_length - 3] == in_buffer[packet_length - 3])
                        {
                            for (int i = 0; i < m_listTypesForRead.Count; i++)
                            {
                                RecordValue recordValue;
                                recordValue.type = m_listTypesForRead[i];
                                recordValue.fine_state = true;
                                recordValue.value = 0;
                                recordValue.value = Math.Round(BitConverter.ToDouble(in_buffer, 6 + i * 8), 4);
                                recordValue.fine_state = true;
                                 values.listRV = new List<RecordValue>();
                                values.listRV.Add(recordValue);
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool ReadMonthlyValues(byte month, ushort year, ref Values values)
        {
            return ReadArchive(TypeDataPulsar.Monthly, new DateTime(year, month, 1), ref values);
        }

        public bool ReadDailyValues(byte day, byte month, ushort year, ref Values values)
        {
            return ReadArchive(TypeDataPulsar.Daily, new DateTime(year, month, day), ref values);
        }

        public bool ReadPowerSlice(ref List<IndaySlice> listRPS, DateTime dt_begin, byte period)
        {
            return false;
        }

        private bool ReadArchive(TypeDataPulsar type, DateTime date,  ref Values values)
        {
            values.listRV = new List<RecordValue>();
            m_length_cmd = 0;
            Byte[] in_buffer = new Byte[255];

            ushort rnd = 0x50b9;
            byte func = 0;

            byte out_packet_length = 0;
            int bytes_to_read = -1;

            func = 6;
            out_packet_length = 28;

            // адрес
            byte[] adr = new byte[4];
            Int2BCD((int)m_address, adr);

            m_cmd = new byte[out_packet_length];

            for (int t = 0; t < m_listTypesForRead.Count; t++)
            {
                m_length_cmd = 0;

                // битовая маска каналов
                uint channel_mask = 0;
                uint tmp = 1;
                tmp = tmp << (m_listTypesForRead[t] - 1);
                channel_mask |= tmp;

                byte[] parameter = BitConverter.GetBytes(channel_mask);
                byte[] random = BitConverter.GetBytes(rnd);

                // формируем команду 
                // адрес
                for (int i = 0; i < adr.Length; i++)
                {
                    m_cmd[m_length_cmd++] = adr[i];
                }
                // номер функции
                m_cmd[m_length_cmd++] = func;
                // общая длина пакета
                m_cmd[m_length_cmd++] = out_packet_length;
                // параметры
                for (int i = 0; i < parameter.Length; i++)
                {
                    m_cmd[m_length_cmd++] = parameter[i];
                }

                bytes_to_read = 10 + 2 + 2 + 6 + 4 * 1;

                // тип архива
                m_cmd[m_length_cmd++] = Convert.ToByte(type);
                m_cmd[m_length_cmd++] = 0;
                // дата - начало
                m_cmd[m_length_cmd++] = Convert.ToByte(date.Year - 2000);
                m_cmd[m_length_cmd++] = Convert.ToByte(date.Month);
                m_cmd[m_length_cmd++] = Convert.ToByte(date.Day);
                m_cmd[m_length_cmd++] = 0;
                m_cmd[m_length_cmd++] = 0;
                m_cmd[m_length_cmd++] = 0;
                // дата - конец
                m_cmd[m_length_cmd++] = Convert.ToByte(date.Year - 2000); 
                m_cmd[m_length_cmd++] = Convert.ToByte(date.Month);
                m_cmd[m_length_cmd++] = Convert.ToByte(date.Day);
                m_cmd[m_length_cmd++] = 0;
                m_cmd[m_length_cmd++] = 0;
                m_cmd[m_length_cmd++] = 0;

                // ID
                for (int i = 0; i < random.Length; i++)
                {
                    m_cmd[m_length_cmd++] = random[i];
                }

                // CRC16
                byte[] crc16 = CRC16(m_cmd, m_length_cmd);
                for (int i = 0; i < crc16.Length; i++)
                {
                    m_cmd[m_length_cmd++] = crc16[i];
                }

                if (m_vport.WriteReadData(FindPacketSignature, m_cmd, ref in_buffer, m_length_cmd, bytes_to_read) > 0)
                {
                    bool find_header = true;

                    // длина пакета 
                    byte packet_length = 0;

                    // проверка заголовка пакета
                    for (int i = 0; i < 5; i++)
                    {
                        if (m_cmd[i] != in_buffer[i])
                        {
                            find_header = false;
                        }
                    }

                    if (find_header)
                    {
                        packet_length = in_buffer[5];

                        // проверка CRC
                        crc16 = CRC16(in_buffer, packet_length - 2);

                        if (in_buffer[packet_length - 2] == crc16[0] && in_buffer[packet_length - 1] == crc16[1])
                        {
                            // проверка ID
                            if (m_cmd[out_packet_length - 4] == in_buffer[packet_length - 4] && m_cmd[out_packet_length - 3] == in_buffer[packet_length - 3])
                            {
                                RecordValue recordValue;
                                recordValue.type = m_listTypesForRead[t];
                                recordValue.fine_state = true;
                                recordValue.value = 0;

                                int data_elements_count = (packet_length - 10 - 4 - 6) / 4;

                                //WriteToLog("type archive=" + type.ToString() + "; data_elements_count=" + data_elements_count.ToString() + "; Date=" + date.ToShortDateString());

                                if (data_elements_count > 0)
                                {
                                    int iyear = in_buffer[6 + 4] + 2000;
                                    int imon = in_buffer[6 + 4 + 1];
                                    int iday = in_buffer[6 + 4 + 2];

                                    //WriteToLog("dt_begin=" + date.ToString() + "; idate=" + iday.ToString() + "." + imon.ToString() + "." + iyear.ToString());

                                    if (date.Year == iyear && date.Month == imon && date.Day == iday)
                                    {
                                        data_elements_count = 1;

                                        for (int d = 0; d < data_elements_count; d++)
                                        {
                                            byte[] temp_buff = new byte[4];
                                            for (int b = 0; b < 4; b++)
                                            {
                                                temp_buff[b] = in_buffer[6 + 4 + 6 + d * 4 + b];
                                            }

                                            if ((temp_buff[0] == 0xF0 || temp_buff[0] == 0xF1) && temp_buff[1] == 0xFF && temp_buff[2] == 0xFF && temp_buff[3] == 0xFF)
                                            {
                                                recordValue.fine_state = true;
                                                recordValue.value = 0;
                                            }
                                            else
                                            {
                                                recordValue.fine_state = true;
                                                recordValue.value = Math.Round(BitConverter.ToSingle(temp_buff, 0), 4);
                                            }

                                            //WriteToLog("value=" + recordValue.value.ToString());

                                            values.listRV.Add(recordValue);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// CRC16 
        /// </summary>
        /// <param name="Arr"></param>
        /// <returns></returns>
        private byte[] CRC16(byte[] Arr, int length)
        {
            byte[] CRC = new byte[2];
            UInt16 B = 0xFFFF;
            int j = 0;
            int i;
            byte b;
            bool f;

            unchecked
            {
                do
                {
                    i = 0;
                    b = Arr[j];
                    B = (UInt16)(B ^ (UInt16)b);
                    do
                    {
                        f = (((B) & (1)) == 1);
                        B = (UInt16)(B / 2);
                        if (f)
                        {
                            B = (UInt16)(B ^ (0xA001));
                        }
                        i++;
                    } while (i < 8);
                    j++;
                } while (j < length);
                CRC[0] = (byte)(B);
                CRC[1] = (byte)(B >> 8);
            }
            return CRC;
        }

        /// <summary>
        /// Конвертация Int в BCD
        /// </summary>
        /// <param name="val"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        private int Int2BCD(int val, byte[] buf)
        {
            int idx = buf.Length;
            unchecked
            {
                do
                {
                    idx--;
                    buf[idx] = (byte)((val % 10) | (((val % 100) / 10) << 4));
                    val /= 100;
                } while (val != 0);

                while (idx > 0)
                {
                    idx--;
                    buf[idx] = 0;
                }
            }

            return idx;
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

                // адрес
                byte[] adr = new byte[4];
                Int2BCD((int)m_address, adr);

                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i] == adr[0] &&
                        array[i + 1] == adr[1] &&
                        array[i + 2] == adr[2] &&
                        array[i + 3] == adr[3]
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

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {

            List<byte> types = new List<byte>();
            types.Add((byte)(param & 0x000000FF));
            ClearTypesForRead();
            SetTypesForRead(types);
            Values vals = new Values();
            if (ReadCurrentValues(ref vals))
            {
                if (vals.listRV.Count == 1)
                {
                    RecordValue rv = vals.listRV[vals.listRV.Count - 1];
                    recordValue = (float)rv.value;
                    return true;
                }
            }
            WriteToLog("ReadCurrentValues: невозможно считать текущее значение для параметра " + param.ToString());
            return false;

            /*
            SetTypesForRead(GetTypesForCategory(CommonCategory.Current));
            Values vals = new Values();
            vals.listRV = new List<RecordValue>();
            if (ReadCurrentValues(ref vals))
            {

                return true;
            }

            return false;*/
        }

        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            List<byte> types = new List<byte>();
            types.Add((byte)(param & 0x000000FF));
            ClearTypesForRead();
            SetTypesForRead(types);
            Values vals = new Values();
            if (ReadMonthlyValues((byte)dt.Month, (ushort)dt.Year, ref vals))
            {
                if (vals.listRV.Count == 1)
                {
                    RecordValue rv = vals.listRV[vals.listRV.Count - 1];
                    recordValue = (float)rv.value;
                    return true;
                }
            }

            WriteToLog("ReadMonthlyValues: невозможно считать текущее значение для параметра " + param.ToString());
            return false;
        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            List<byte> types = new List<byte>();
            types.Add((byte)(param & 0x000000FF));
            ClearTypesForRead();
            SetTypesForRead(types);
            Values vals = new Values();
            if (ReadDailyValues((byte)dt.Day, (byte)dt.Month, (ushort)dt.Year, ref vals))
            {
                if (vals.listRV.Count == 1)
                {
                    RecordValue rv = vals.listRV[vals.listRV.Count - 1];
                    recordValue = (float)rv.value;
                    return true;
                }
            }

            WriteToLog("ReadMonthlyValues: невозможно считать текущее значение для параметра " + param.ToString());
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

        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            return false;
        }

        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period)
        {
            return false;
        }
    }
}
