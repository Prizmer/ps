using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Prizmer.Meters.iMeters;
using Prizmer.Ports;

namespace Prizmer.Meters
{

    public sealed class tem104 : CMeter, IMeter
    {
        #region Imported Struct
        protected Dictionary<byte, string> m_dictDataTypes = new Dictionary<byte, string>();
        protected List<byte> m_listTypesForRead = new List<byte>();

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

        #endregion


        private struct Date
        {
            public int day;
            public int month;
            public int year;
        }

        private struct AddressInMemory
        {
            public byte b0;
            public byte b1;
            public byte b2;
            public byte b3;
        }

        private enum TypesRecord : byte
        {
            trHour = 0xF4,
            trDay = 0xF8,
            trMonth = 0xFC
        }

        private enum TypesCommands
        {
            tc2K = 1,
            tc512K = 3
        }

        private const byte m_header_length = 6;
        private const byte m_min_answer_length = 7;
        private const byte CRC_SIZE = 1;
        private const int m_max_types = 57;
        private const int m_sys_int_size = 256;
        private const int m_max_day_records = 384;
        private const int READSERIALNUMBER_CMD_ANSWER_SIZE = m_header_length + 4;// + CRC_SIZE;// CRC в ответе нет
        private const int READVALUES_CMD_ANSWER_SIZE = m_header_length + CRC_SIZE;
        private readonly byte m_begin_packet_signature = 0x55;
        private bool already_read_first_address = false;
        private DateTime date_first_readed;
        private Dictionary<Date, AddressInMemory> dict_address_by_date = new Dictionary<Date, AddressInMemory>();

        private byte[] m_cmd = new byte[71];
        private byte m_length_cmd = 0;
        //private byte m_ident_name_size = 0;
        //private int IDENT_CMD_ANSWER_SIZE = m_min_answer_length;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="address"></param>
        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            this.m_address = address;
            //m_ident_name_size = 7;

            for (byte t = 1; t <= m_max_types; t++)
            {
                m_dictDataTypes.Add(t, "");
            }

            this.m_vport = data_vport;
           // m_log_file_name += this.GetType() + "_" + m_address.ToString();
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
                case CommonCategory.Daily:
                    for (byte type = 0; type <= m_max_types; type++)
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
            //IDENT_CMD_ANSWER_SIZE = m_header_length + m_ident_name_size + CRC_SIZE;

            //byte[] answer = new byte[IDENT_CMD_ANSWER_SIZE];

            //MakeCommand(null, 0, 0, 0);

            //if (!SendCommand(ref answer, IDENT_CMD_ANSWER_SIZE))
            //{
            //    return false;
            //}

            //string name = "";

            //for (int i = 0; i < m_ident_name_size; i++)
            //{
            //    name += Convert.ToChar(answer[m_header_length + i]);
            //}

            //WriteToLog("Ident name: " + name);

            return true;
        }
        
        private byte[] AddressToBytes(AddressInMemory address)
        {
            byte[] bytes = new byte[4];

            bytes[0] = address.b0;
            bytes[1] = address.b1;
            bytes[2] = address.b2;
            bytes[3] = address.b3;

            return bytes;
        }

        private AddressInMemory BytesToAddress(byte[] bytes)
        {
            AddressInMemory address;

            address.b0 = bytes[0];
            address.b1 = bytes[1];
            address.b2 = bytes[2];
            address.b3 = bytes[3];

            return address;
        }

        /// <summary>
        /// Чтение архивных данных
        /// </summary>
        /// <param name="day"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private bool ReadFromFlash512K(byte day, byte month, ushort year, ref Values values)
        {
            try
            {
                DateTime date_target = new DateTime(year, month, day, 0, 0, 0);

                if (Math.Round(DateTime.Now.Subtract(date_target).TotalDays, 0) >= 0)
                {
                    byte[] address_buffer = new byte[4];
                    byte[] answer_bytes = new byte[READVALUES_CMD_ANSWER_SIZE + 4];
                    
                    int ihour = 0;
                    int iday = 0;
                    int imonth = 0;
                    int iyear = 0;

                    Date date;
                    date.day = day;
                    date.month = month;
                    date.year = year;
                    
                    WriteToLog("dict_address_by_date count=" + dict_address_by_date.Count.ToString());
                    
                    // если требуемая дата, ранее была обнаружена, то читаем по обнаруженному адресу
                    if (dict_address_by_date.ContainsKey(date))
                    {
                        address_buffer = AddressToBytes(dict_address_by_date[date]);

                        if (ReadValuesFromAddress(TypesCommands.tc512K, 4, address_buffer, answer_bytes))
                        {
                            ihour = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 0]);
                            iday = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 1]);
                            imonth = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 2]);
                            iyear = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 3]) + 2000;

                            // проверка на совпадение прочитанной даты с искомой
                            if (iday == day && imonth == month && iyear == year)
                            {
                                if (ReadDataFromFlash512KByAddress(address_buffer, ref values))
                                {
                                    WriteToLog("Readed data from early finded address : " + new DateTime(iyear, imonth, iday).ToShortDateString());
                                }
                            }
                        }
                    }
                    // в противном случае - ищем запись по архиву
                    else
                    {
                        // чтение данных из первого адреса в архиве, если ранее не было прочитано
                        if (!already_read_first_address)
                        { 
                            address_buffer[0] = 0;
                            address_buffer[1] = 6;
                            address_buffer[2] = 0;
                            address_buffer[3] = 0;

                            if (ReadValuesFromAddress(TypesCommands.tc512K, 4, address_buffer, answer_bytes))
                            {
                                already_read_first_address = true;

                                ihour = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 0]);
                                iday = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 1]);
                                imonth = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 2]);
                                iyear = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 3]) + 2000;
                                
                                date_first_readed = new DateTime(iyear, imonth, iday);

                                date.day = iday;
                                date.month = imonth;
                                date.year = iyear;

                                if (!dict_address_by_date.ContainsKey(date))
                                {
                                    dict_address_by_date.Add(date, BytesToAddress(address_buffer));
                                }

                                // проверка на совпадение прочитанной даты с искомой
                                if (iday == day && imonth == month && iyear == year)
                                {
                                    if (ReadDataFromFlash512KByAddress(address_buffer, ref values))
                                    {
                                        WriteToLog("Read from first address. Date=" + new DateTime(iyear, imonth, iday).ToShortDateString() + "; Values.Count = " + values.listRV.Count.ToString());
                                    }
                                }
                            }
                        }
                        // поиск записи с нужной датой в архиве ПУ
                        if (already_read_first_address)
                        {
                            TimeSpan diff_date = date_target - date_first_readed;
                            int iexpected_number = (int)diff_date.TotalDays < m_max_day_records ? (int)diff_date.TotalDays : (m_max_day_records - 1);

                            iexpected_number = iexpected_number < 0 ? m_max_day_records - iexpected_number : iexpected_number;

                            WriteToLog("Difference days of target and first records=" + iexpected_number.ToString());

                            byte[] address_expected_bytes = BitConverter.GetBytes(Convert.ToInt32(iexpected_number * m_sys_int_size));

                            // Чтение искомой записи по предполагаемому адресу
                            address_buffer[0] = 0;
                            address_buffer[1] = Convert.ToByte(6 + address_expected_bytes[2]);
                            address_buffer[2] = address_expected_bytes[1];
                            address_buffer[3] = address_expected_bytes[0];
                            
                            if (ReadValuesFromAddress(TypesCommands.tc512K, 4, address_buffer, answer_bytes))
                            {
                                ihour = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 0]);
                                iday = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 1]);
                                imonth = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 2]);
                                iyear = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 3]) + 2000;

                                date.day = iday;
                                date.month = imonth;
                                date.year = iyear;

                                if (!dict_address_by_date.ContainsKey(date))
                                {
                                    dict_address_by_date.Add(date, BytesToAddress(address_buffer));
                                }

                                // проверка на совпадение прочитанной даты с искомой
                                if (iday == day && imonth == month && iyear == year)
                                {
                                    if (ReadDataFromFlash512KByAddress(address_buffer, ref values))
                                    {
                                        WriteToLog("Read expected. Date=" + new DateTime(iyear, imonth, iday).ToShortDateString() + "; Values.Count = " + values.listRV.Count.ToString());
                                    }
                                }
                                else // последовательное чтение всех записей для поиска записи с нужной датой
                                {
                                    bool finded = false;
                                    bool all_records_check = true;

                                    int iforward = 0;
                                    int ibackward = 0;

                                    for (int n = 0; n < m_max_day_records; n++)
                                    {
                                        int number_record_for_read = 0;

                                        if (n % 2 == 0)
                                        {
                                            number_record_for_read = iexpected_number + ++iforward;
                                            //if (number_record_for_read < m_max_day_records)
                                            //{
                                            //    WriteToLog("iforward=" + iforward.ToString());
                                            //}
                                        }
                                        else
                                        {
                                            number_record_for_read = iexpected_number + --ibackward;
                                            //if (number_record_for_read > 0)
                                            //{
                                            //    WriteToLog("ibackward=" + ibackward.ToString());
                                            //}
                                        }

                                        address_expected_bytes = BitConverter.GetBytes(number_record_for_read * m_sys_int_size);

                                        // искомый адрес
                                        address_buffer[0] = 0;
                                        address_buffer[1] = Convert.ToByte(6 + address_expected_bytes[2]);
                                        address_buffer[2] = address_expected_bytes[1];
                                        address_buffer[3] = address_expected_bytes[0];

                                        if (!dict_address_by_date.ContainsValue(BytesToAddress(address_buffer)))
                                        {
                                            //WriteToLog("Address=" + address_buffer[0].ToString("x") + "|" + address_buffer[1].ToString("x") + "|" + address_buffer[2].ToString("x") + "|" + address_buffer[3].ToString("x"));
                                            if (ReadValuesFromAddress(TypesCommands.tc512K, 4, address_buffer, answer_bytes))
                                            {
                                                ihour = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 0]);
                                                iday = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 1]);
                                                imonth = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 2]);
                                                iyear = (int)Meters.CommonMeters.BCDToByte(answer_bytes[m_header_length + 3]) + 2000;

                                                date.day = iday;
                                                date.month = imonth;
                                                date.year = iyear;

                                                //WriteToLog("ReadValuesFromAddress Date=" + iday.ToString() + "." + imonth.ToString() + "." + iyear.ToString());

                                                if (!dict_address_by_date.ContainsKey(date))
                                                {
                                                    dict_address_by_date.Add(date, BytesToAddress(address_buffer));
                                                }

                                                if (iday == day && imonth == month && iyear == year)
                                                {
                                                    finded = true;

                                                    if (ReadDataFromFlash512KByAddress(address_buffer, ref values))
                                                    {
                                                        WriteToLog("Find required. Date=" + new DateTime(iyear, imonth, iday).ToShortDateString() + "; Values.Count = " + values.listRV.Count.ToString());
                                                    }

                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                all_records_check = false;
                                            }
                                        }
                                    }

                                    // при не нахождении записи 
                                    if (finded == false && all_records_check == true)
                                    {
                                        for (byte k = 0; k < m_listTypesForRead.Count; k++)
                                        {
                                            RecordValue rv;
                                            rv.fine_state = false;
                                            rv.type = m_listTypesForRead[k];
                                            rv.value = 0;

                                            values.listRV.Add(rv);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                WriteToLog("ReadFromFlash512K: " + ex.Message);
            }

            return (values.listRV.Count == m_listTypesForRead.Count) ? true : false;
        }
        
        public bool SyncTime(DateTime dt)
        {
            return false;
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            byte[] data = new byte[3];
            byte[] answer = new byte[READSERIALNUMBER_CMD_ANSWER_SIZE];
            byte length_data = 4;

            data[0] = 0;
            data[1] = 0x7C;
            data[2] = length_data;

            MakeCommand(data, (byte)data.Length, 0xF, 0x1);

            if (!SendCommand(ref answer, READSERIALNUMBER_CMD_ANSWER_SIZE, false))
            {
                return false;
            }

            byte[] right_order_bytes = CommonMeters.InverseBytesOrder(answer, m_header_length + 3, length_data);

            serial_number = BitConverter.ToUInt32(right_order_bytes, 0).ToString();

            WriteToLog("Serial Number: " + serial_number);

            return true;
        }

        public bool ReadPowerSlice(ref List<IndaySlice> listRPS, DateTime dt_begin, byte period)
        {
            return false;
        }

        private bool ReadDataFromFlash512KByAddress(byte[] address_buffer, ref Values values)
        {
            byte[] value_sizes = new byte[] { 4, 4, 4, 2, 1, 4 };
            byte[] lengths = new byte[] { 52, 64, 16, 24, 12, 48 };
            byte[] begin_address = new byte[] { 0x38, 0x6C, 0xAC, 0xC8, 0xE0, 0x08 };
            byte number_value = 1;

            for (int i = 0; i < begin_address.Length; i++)
            {
                address_buffer[3] = begin_address[i];

                byte[] answer_bytes = new byte[READVALUES_CMD_ANSWER_SIZE + lengths[i]];

                if (ReadValuesFromAddress(TypesCommands.tc512K, lengths[i], address_buffer, answer_bytes))
                {
                    List<double> temp_values = new List<double>();

                    switch (value_sizes[i])
                    {
                        case 1:
                            for (int n = 0; n < lengths[i] / value_sizes[i]; n++)
                            {
                                byte value = answer_bytes[m_header_length + n * value_sizes[i]];

                                temp_values.Add((double)value / 100f);
                            }
                            break;
                        case 2:
                            for (int n = 0; n < lengths[i] / value_sizes[i]; n++)
                            {
                                byte[] temp_buff = CommonMeters.InverseBytesOrder(answer_bytes, Convert.ToUInt32(m_header_length + n * value_sizes[i] + 1), value_sizes[i]);
                                ushort integer_part = BitConverter.ToUInt16(temp_buff, 0);
                                temp_values.Add((double)integer_part / 100f);
                            }
                            break;
                        case 4:
                            for (int n = 0; n < lengths[i] / value_sizes[i]; n++)
                            {
                                byte[] temp_buff = CommonMeters.InverseBytesOrder(answer_bytes, Convert.ToUInt32(m_header_length + n * value_sizes[i] + 3), value_sizes[i]);
                                temp_values.Add((double)(begin_address[i] == 0x08 ? BitConverter.ToSingle(temp_buff, 0) : BitConverter.ToUInt32(temp_buff, 0)));
                            }
                            break;
                    }

                    if (begin_address[i] == 0x08)
                    {
                        for (byte k = 0; k < temp_values.Count; k++)
                        {
                            int index = values.listRV.FindIndex(x => x.type == (k + 1));
                            if (index >= 0)
                            {
                                RecordValue rv = values.listRV[index];
                                rv.value += temp_values[k];

                                values.listRV[index] = rv;

                                WriteToLog("temp_values type=" + k.ToString() + "; value = " + temp_values[k].ToString());
                            }
                        }
                    }
                    else
                    {
                        for (int j = 0; j < temp_values.Count; j++)
                        {
                            if (m_listTypesForRead.Contains(number_value))
                            {
                                RecordValue rv;
                                rv.fine_state = true;
                                rv.type = number_value;
                                rv.value = temp_values[j];

                                values.listRV.Add(rv);

                                WriteToLog("value type=" + rv.type.ToString() + "; value = " + rv.value.ToString());
                            }

                            number_value++;
                        }
                    }
                }
                else
                {
                    values.listRV.Clear();
                    break;
                }
            }

            WriteToLog("values.listRV.Count=" + values.listRV.Count.ToString() + "; m_listTypesForRead.Count=" + m_listTypesForRead.Count.ToString());

            return (values.listRV.Count == m_listTypesForRead.Count) ? true : false;
        }

        private bool ReadValuesFromAddress(TypesCommands tc, byte length_data, byte[] begin_address, byte[] answer_bytes, int index_begin_address = 0)
        {
            byte[] data = null;

            switch (tc)
            {
                case TypesCommands.tc2K:
                    data = new byte[3];
                    data[0] = 0x2;
                    data[1] = begin_address[index_begin_address];
                    data[2] = length_data;
                    break;
                case TypesCommands.tc512K:
                    data = new byte[5];
                    data[0] = length_data;
                    for (int i = 0; i < 4; i++)
                    {
                        data[1 + i] = begin_address[i];
                    }
                    break;
            }

            MakeCommand(data, (byte)data.Length, 0xF, (byte)tc);

            for (int i = 0; i < 5; i++)
            {
                if (SendCommand(ref answer_bytes, READVALUES_CMD_ANSWER_SIZE + length_data))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Формирование запроса
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length_data"></param>
        /// <param name="type_command"></param>
        /// <param name="command"></param>
        private void MakeCommand(byte[] data, byte length_data, byte type_command, byte command)
        {
            m_length_cmd = 0;

            m_cmd[m_length_cmd++] = m_begin_packet_signature;
            m_cmd[m_length_cmd++] = (byte)this.m_address;
            m_cmd[m_length_cmd++] = (byte)(~m_address);
            m_cmd[m_length_cmd++] = type_command;
            m_cmd[m_length_cmd++] = command;
            m_cmd[m_length_cmd++] = length_data;

            for (int i = 0; i < length_data; i++)
            {
                m_cmd[m_length_cmd++] = data[i];
            }

            m_cmd[m_length_cmd++] = CalcCRC(m_cmd, m_length_cmd);
        }

        /// <summary>
        /// Проверка пришедших данных
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="size"></param>
        /// <param name="verify_crc"></param>
        /// <returns></returns>
        private bool FinishAccept(byte[] answer, ushort size, bool verify_crc = true)
        {
            // проверка длины 
            if (size < m_min_answer_length)
            {
                return false;
            }

            // проверяем сигнатурного байта
            if (answer[0] != (byte)(~m_begin_packet_signature))
            {
                return false;
            }

            // проверка сетевого адреса
            if (answer[1] != m_address)
            {
                return false;
            }

            // проверка инверсного значения сетевого адреса
            if (answer[2] != (byte)(~m_address))
            {
                return false;
            }

            //// проверка типа команды
            if (answer[3] != m_cmd[3])
            {
                return false;
            }

            // проверка команды
            if (answer[4] != m_cmd[4])
            {
                return false;
            }

            if (verify_crc)
            {
                // проверяем CRC
                if (CalcCRC(answer, size) != answer[size - 1])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Вычисление CRC
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private byte CalcCRC(byte[] data, ushort size)
        {
            byte crc = 0;
            for (int i = 0; i < size - 1; i++)
            {
                crc += (byte)data[i];
            }

            return (byte)(~crc);
        }

        /// <summary>
        /// Отправка команды
        /// </summary>
        /// <param name="answer"></param>
        /// <param name="answ_size"></param>
        /// <param name="verify_crc"></param>
        /// <returns></returns>
        private bool SendCommand(ref byte[] answer, int answ_size, bool verify_crc = true)
        {
            bool res = false;

            if (m_vport != null)
            {
                if (m_vport.WriteReadData(FindPacketSignature, m_cmd, ref answer, m_length_cmd, answ_size) == answ_size)
                {
                    //проверка пришедших данных
                    if (FinishAccept(answer, Convert.ToUInt16(answ_size), verify_crc))
                    {
                        res = true;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Поиск пакета
        /// </summary>
        /// <param name="queue"></param>
        /// <returns></returns>
        private int FindPacketSignature(Queue<byte> queue)
        {
            try
            {
                if (queue.Count > 0)
                {
                    byte[] array = new byte[queue.Count];
                    array = queue.ToArray();

                    int i = 0;
                    bool have_echo = true;
                    if (array[i] == (byte)(~m_cmd[0]))
                    {
                        int j = 0;
                        for (j = 0; j < m_length_cmd; j++)
                        {
                            if (i + j < array.Length)
                            {
                                if (array[i + j] != m_cmd[j])
                                {
                                    have_echo = false;
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (have_echo)
                        {
                            i += j;
                        }
                    }

                    return i;
                }

                throw new ApplicationException("Нет байт в пакете. " + queue.Count.ToString());
            }
            catch (Exception ex)
            {
                WriteToLog("FindPacketSignature: " + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Адрес следующей архивной записи данных 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="tr"></param>
        /// <returns></returns>
        private bool GetNextRecordAddress(out byte[] address, TypesRecord tr)
        {
            byte length_data = 4;
            address = new byte[length_data];

            byte[] data = new byte[3];
            byte[] answer = new byte[READVALUES_CMD_ANSWER_SIZE + length_data];

            data[0] = 0;
            data[1] = (byte)tr;
            data[2] = length_data;

            MakeCommand(data, (byte)data.Length, 0xF, 0x1);

            if (!SendCommand(ref answer, READVALUES_CMD_ANSWER_SIZE + length_data))
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                address[i] = answer[m_header_length + i];
            }

            //WriteToLog(BitConverter.ToString(answer));

            return true;
        }


        public bool ReadCurrentValues(ref Values values)
        {
            byte[] value_sizes = new byte[] {4, 4, 4, 2, 1, 4 };
            byte[] lengths = new byte[] { 52, 64, 16, 24, 12, 48 };
            byte[] begin_address = new byte[] { 0x38, 0x6C, 0xAC, 0xC8, 0xE0, 0x08 };
            byte[] answer_bytes;
            byte number_value = 1;

            try
            {
                for (int i = 0; i < lengths.Length & i < begin_address.Length & i < value_sizes.Length; i++)
                {
                    answer_bytes = new byte[READVALUES_CMD_ANSWER_SIZE + lengths[i]];

                    if (ReadValuesFromAddress(TypesCommands.tc2K, lengths[i], begin_address, answer_bytes, i))
                    {
                        List<double> temp_values = new List<double>();

                        switch (value_sizes[i])
                        {
                            case 1:
                                for (int n = 0; n < lengths[i] / value_sizes[i]; n++)
                                {
                                    byte value = answer_bytes[m_header_length + n * value_sizes[i]];
                                        temp_values.Add((double)value / 100f);
                                }
                                break;
                            case 2:
                                for (int n = 0; n < lengths[i] / value_sizes[i]; n++)
                                {
                                    byte[] temp_buff = CommonMeters.InverseBytesOrder(answer_bytes, Convert.ToUInt32(m_header_length + n * value_sizes[i] + 1), value_sizes[i]);
                                    ushort integer_part = BitConverter.ToUInt16(temp_buff, 0);
                                    temp_values.Add((double)integer_part / 100f);
                                }
                                break;
                            case 4:
                                for (int n = 0; n < lengths[i] / value_sizes[i]; n++)
                                {
                                    byte[] temp_buff = CommonMeters.InverseBytesOrder(answer_bytes, Convert.ToUInt32(m_header_length + n * value_sizes[i] + 3), value_sizes[i]);
                                    temp_values.Add((double)(begin_address[i] == 0x08 ? BitConverter.ToSingle(temp_buff, 0) : BitConverter.ToUInt32(temp_buff, 0)));
                                }
                                break;
                        }

                        if (begin_address[i] == 0x08)
                        {
                            for (byte k = 0; k < temp_values.Count; k++)
                            {
                                int index = values.listRV.FindIndex(x => x.type == (k + 1));
                                if (index >= 0)
                                {
                                   // WriteToLog(temp_values[k].ToString());
                                    RecordValue rv = values.listRV[index];
                                    rv.value += temp_values[k];

                                    values.listRV[index] = rv;
                                }
                            }
                        }
                        else
                        {
                            for (int j = 0; j < temp_values.Count; j++)
                            {
                               // WriteToLog(temp_values[j].ToString());
                                if (m_listTypesForRead.Contains(number_value))
                                {
                                    RecordValue rv;
                                    rv.fine_state = true;
                                    rv.type = number_value;
                                    rv.value = temp_values[j];

                                    values.listRV.Add(rv);
                                }
                                number_value++;
                            }
                        }
                    }
                    else
                    {
                        number_value += (byte)(lengths[i] / value_sizes[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToLog("ReadCurrentValues: " + ex.Message);
            }

            return (values.listRV.Count == m_listTypesForRead.Count) ? true : false;
        }

        public bool ReadMonthlyValues(byte month, ushort year, ref Values values)
        {
            return ReadFromFlash512K(1, month, Convert.ToUInt16(year), ref values);
        }

        public bool ReadDailyValues(byte day, byte month, ushort year, ref Values values)
        {
            return ReadFromFlash512K(day, month, Convert.ToUInt16(year), ref values);
        }


        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            List<byte> sellectedType = new List<byte>(1);
            sellectedType.Add((byte)param);
            SetTypesForRead(sellectedType);

            Values vals = new Values();
            vals.listRV = new List<RecordValue>();
            ReadCurrentValues(ref vals);

            if (vals.listRV.Count == 1)
            {
                recordValue = (float)vals.listRV[0].value;
                WriteToLog("Получено ТЕКУЩЕЕ значение:" + recordValue.ToString()); 
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            List<byte> sellectedType = new List<byte>(1);
            sellectedType.Add((byte)param);
            SetTypesForRead(sellectedType);

            Values vals = new Values();
            vals.listRV = new List<RecordValue>();

            ReadMonthlyValues((byte)dt.Month, (ushort)dt.Year, ref vals);

            if (vals.listRV.Count == 1)
            {
                recordValue = (float)vals.listRV[0].value;
                WriteToLog("Получено МЕСЯЧНОЕ значение:" + recordValue.ToString());
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            List<byte> sellectedType = new List<byte>(1);
            sellectedType.Add((byte)param);
            SetTypesForRead(sellectedType);

            Values vals = new Values();
            vals.listRV = new List<RecordValue>();

            ReadDailyValues((byte)dt.Day, (byte)dt.Month, (ushort)dt.Year, ref vals);

            if (vals.listRV.Count == 1)
            {
                recordValue = (float)vals.listRV[0].value;
                WriteToLog("Получено ЕЖЕДНЕВНОЕ значение:" + recordValue.ToString());
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<iMeters.RecordPowerSlice> listRPS, byte period)
        {
            return false;
        }

        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
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
