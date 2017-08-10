using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;

//using Prizmer.Meters.iMeters;
//using Prizmer.Ports;

using Drivers.LibMeter;
using PollingLibraries.LibPorts;

namespace Prizmer.Meters
{
    public sealed class pulsar16 : Drivers.LibMeter.CMeter, Drivers.LibMeter.IMeter
    {
        public enum TypeDataPulsar : byte
        {
            Current = 0,
            Hourly = 1,
            Daily = 2,
            Monthly = 3
        }

        /// <summary>
        /// Структура с информацией об единичной считываемой величине  
        /// </summary>
        public struct RecordValue
        {
            /// <summary>
            /// Значение
            /// </summary>
            public float value;
            /// <summary>
            /// Тип
            /// </summary>
            public byte type;
            /// <summary>
            /// Статус (true - значение верно, false - неверно)
            /// </summary>
            public bool fine_state;
        };

        private byte[] m_cmd;
        private byte m_length_cmd = 0;
        private byte m_max_canals = 16;
        
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="address"></param>
        /// <param Pulsar="password"></param>
        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            this.m_address = address;

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
            byte out_packet_length = 11;

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
            m_cmd[m_length_cmd++] = 0x06;
            m_length_cmd+=6;

            byte[] crc16;// CRC16

            ////
            m_cmd[5] = 0x02;
            m_cmd[6] = 0x40;
            m_cmd[7] = Convert.ToByte(dt.Minute.ToString(),16);
            m_cmd[8] = Convert.ToByte(dt.Second.ToString(),16);
            crc16 = CRC16(m_cmd, m_length_cmd-2);
            m_cmd[9] = crc16[0];
            m_cmd[10] = crc16[1];
            ReadData(m_cmd, out_packet_length);
            ////////
            m_cmd[5] = 0x02;
            m_cmd[6] = 0x42;
            m_cmd[7] = Convert.ToByte(dt.Day.ToString(), 16);
            m_cmd[8] = Convert.ToByte(dt.Hour.ToString(), 16);
            crc16 = CRC16(m_cmd, m_length_cmd - 2);
            m_cmd[9] = crc16[0];
            m_cmd[10] = crc16[1];
            ReadData(m_cmd, out_packet_length);
            ////////
            m_cmd[5] = 0x02;
            m_cmd[6] = 0x44;
            m_cmd[7] = Convert.ToByte((dt.Year - 2000).ToString(), 16);
            m_cmd[8] = Convert.ToByte(dt.Month.ToString(), 16);
            crc16 = CRC16(m_cmd, m_length_cmd - 2);
            m_cmd[9] = crc16[0];
            m_cmd[10] = crc16[1];
            ReadData(m_cmd, out_packet_length);

            return true;
        }

        private byte [] ReadData(byte[] data, int bytes_to_read)
        {
            if (m_vport == null) return new byte[0];
            
            Byte[] in_buffer = new Byte[255];

            if (m_vport.WriteReadData(FindPacketSignature, m_cmd, ref in_buffer, m_length_cmd, bytes_to_read) > 0)
            {
                #if (DEBUG)
                    //WriteToLog("Pulsar16: WriteReadData");
                #endif
                bool find_header = true;

                // длина пакета 
                byte packet_length = 0;

                // проверка заголовка пакета
                for (int i = 0; i < 5; i++)
                {
                    if (m_cmd[i] != in_buffer[i])
                    {
                        find_header = false;

                        //WriteToLog("find_header false, =" + i.ToString() );
                        //string s = "";
                        //for(int j=0;j<bytes_to_read;j++)
                        //{
                        //    s += " [" + in_buffer[j].ToString("x") + "] ";
                        //}
                        //WriteToLog(s);
                    }
                }

                if (find_header)
                {
                    #if (DEBUG)
                        WriteToLog("Pulsar16: find_header");
                    #endif
                    packet_length = (byte)(in_buffer[5] + 8);

                    // проверка CRC
                    byte[] crc16 = CRC16(in_buffer, packet_length - 2);

                    if (in_buffer[packet_length - 2] == crc16[0] && in_buffer[packet_length - 1] == crc16[1])
                    {
                        return in_buffer;

                    }
                }
            }

            return new byte[0];
        }

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            //param=1-16
            param -= 1;
            if (param >= m_max_canals) return false;

            List<RecordValue> listRV = new List<RecordValue>();
            if (!ReadCurrentValues_(ref listRV, 0x200, 10, 1)) return false;
            if (!ReadCurrentValues_(ref listRV, 0x214, 10, 6)) return false;
            if (!ReadCurrentValues_(ref listRV, 0x228, 10, 11)) return false;
            if (!ReadCurrentValues_(ref listRV, 0x23c, 2, 16)) return false;

            if (listRV.Count == m_max_canals)
            {
                recordValue = listRV[param].value;
                return true;
            }

            return false;
        }

        private bool ReadCurrentValues_(ref List<RecordValue> values, uint startaddr, byte count, byte begin_type = 1)
        {
            m_length_cmd = 0;
            
            byte out_packet_length = 11;
            int bytes_to_read = -1;
            
            // адрес
            byte[] adr = new byte[4];
            Int2BCD((int)m_address, adr);

            m_cmd = new byte[out_packet_length];

            // формируем команду 
            // адрес
            for (int i = 0; i < adr.Length; i++)
            {
                m_cmd[m_length_cmd++] = adr[i];
            }
            // номер функции
            m_cmd[m_length_cmd++] = 0x03;
            // адрес
            m_cmd[m_length_cmd++] = (byte)(startaddr>>8);
            m_cmd[m_length_cmd++] = (byte)(startaddr&0xff);
            // число регистров
            m_cmd[m_length_cmd++] = 0x00;
            m_cmd[m_length_cmd++] = count;
            //m_cmd[m_length_cmd++] = 0x14;
            
            // CRC16
            byte[] crc16 = CRC16(m_cmd, m_length_cmd);
            for (int i = 0; i < crc16.Length; i++)
            {
                m_cmd[m_length_cmd++] = crc16[i];
            }

            // байт для чтения
            bytes_to_read = 4 + 1 + 1 + 2*count + 2;

            //WriteToLog("Current m_length_cmd=" + m_length_cmd.ToString());

            //WriteToLog("Current bytes_to_read=" + bytes_to_read.ToString());

            Byte[] in_buffer = ReadData(m_cmd, bytes_to_read);


            if (in_buffer.Length == 0) return false;

            for (int i = 0; i < count/2; i++)
            {
                RecordValue recordValue;
                recordValue.type = Convert.ToByte(begin_type + i);
                if(true)
                {
                    byte[] valueArray = new byte[4];
                    Array.Copy(in_buffer, 6 + i * 4, valueArray, 0, 4);

                    recordValue.fine_state = true;
                    recordValue.value = 0;
                    recordValue.value = (valueArray[3] << 16 | valueArray[2] << 24 | valueArray[0] << 8 | valueArray[1]);
                    recordValue.fine_state = true;
                    values.Add(recordValue);
                }
            }

            return true;
        }

        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            return ReadArchive(TypeDataPulsar.Monthly, new DateTime(dt.Year, dt.Month, 1), param, ref recordValue);
        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            return ReadArchive(TypeDataPulsar.Daily, new DateTime(dt.Year, dt.Month, dt.Day), param, ref recordValue);
        }

        public bool ReadHourlyValues(DateTime dt, ushort param, ref float values)
        {
            return ReadArchive(TypeDataPulsar.Hourly, new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0), param, ref values);
        }

        private bool ReadArchive(TypeDataPulsar type, DateTime date, ushort channel, ref float values)
        {
            channel--;//=0-15
            m_length_cmd = 0;
            Byte[] in_buffer = new Byte[255];

            ///////чтение количества записей
            m_cmd = new byte[11];

            // адрес
            byte[] adr = new byte[4];
            Int2BCD((int)m_address, adr);

            for (int i = 0; i < adr.Length; i++)
            {
                m_cmd[m_length_cmd++] = adr[i];
            }

            // номер функции
            m_cmd[m_length_cmd++] = 0x03;
            // адрес
            switch (type)
            {
                case TypeDataPulsar.Hourly:
                    m_cmd[m_length_cmd++] = 0x02;
                    m_cmd[m_length_cmd++] = 0x4c;
                    break;
                case TypeDataPulsar.Daily:
                    m_cmd[m_length_cmd++] = 0x02;
                    m_cmd[m_length_cmd++] = 0x4e;
                    break;
                case TypeDataPulsar.Monthly:
                    m_cmd[m_length_cmd++] = 0x02;
                    m_cmd[m_length_cmd++] = 0x50;
                    break;
                default: return false;
            }
            // число регистров
            m_cmd[m_length_cmd++] = 0x00;
            m_cmd[m_length_cmd++] = 0x01;

            // CRC16
            byte[] crc16 = CRC16(m_cmd, m_length_cmd);
            for (int i = 0; i < crc16.Length; i++)
            {
                m_cmd[m_length_cmd++] = crc16[i];
            }

            in_buffer = ReadData(m_cmd, 10);

            if (in_buffer.Length == 0) return false;

            int currentPos = in_buffer[6] << 8 | in_buffer[7];//позиция указателя в кольцевом архиве

            ///////чтение архива
            DateTime curdt = DateTime.Now;
            TimeSpan intervaldt = curdt - date;

            int maxLengArchive = 0;
            uint startaddress = 0;

            switch (type)
            {
                case TypeDataPulsar.Hourly:
                    maxLengArchive = 816;
                    startaddress = 160;
                    //если давность запроса превышает размер архива, то завершить опрос
                    if ((int)intervaldt.TotalHours > maxLengArchive) return false;
                    //если давность запроса превышает текущую позицию указателя, то переместить вычислить новый указатель в кольцевом буфере
                    if ((int)intervaldt.TotalHours * 4 > currentPos) currentPos = maxLengArchive - (int)((int)intervaldt.TotalHours * 4 - currentPos);
                    else currentPos = currentPos - (int)intervaldt.TotalHours * 4;
                    break;
                case TypeDataPulsar.Daily:
                    maxLengArchive = 180;
                    startaddress = 52384;
                    //если давность запроса превышает размер архива, то завершить опрос
                    if ((int)intervaldt.TotalDays > maxLengArchive) return false;
                    //если давность запроса превышает текущую позицию указателя, то переместить вычислить новый указатель в кольцевом буфере
                    if ((int)intervaldt.TotalDays * 4 > currentPos) currentPos = maxLengArchive - (int)((int)intervaldt.TotalDays * 4 - currentPos);
                    else currentPos = currentPos - (int)intervaldt.TotalDays * 4;
                    break;
                case TypeDataPulsar.Monthly:
                    maxLengArchive = 24;
                    startaddress = 63904;
                    //если давность запроса превышает размер архива, то завершить опрос
                    if ((int)intervaldt.TotalDays * 30 > maxLengArchive) return false;
                    //если давность запроса превышает текущую позицию указателя, то переместить вычислить новый указатель в кольцевом буфере
                    if ((int)intervaldt.TotalDays * 30 * 4 > currentPos) currentPos = maxLengArchive - (int)((int)intervaldt.TotalDays * 30 * 4 - currentPos);
                    else currentPos = currentPos - (int)intervaldt.TotalDays * 30 * 4;
                    break;
            }



            m_cmd[4] = 0x65;//код функции

            //for (int channel = 0; channel < 16; channel++)
            {
                uint startaddr = (uint)(startaddress + 4 * maxLengArchive * channel + currentPos);

                //адрес
                m_cmd[5] = (byte)(startaddr >> 8);
                m_cmd[6] = (byte)(startaddr & 0xff);
                //количество байт
                m_cmd[7] = 0x00;
                m_cmd[8] = 0x02;
                //crc16
                crc16 = CRC16(m_cmd, 9);
                m_cmd[9] = crc16[0];
                m_cmd[10] = crc16[1];

                in_buffer = ReadData(m_cmd, 12);

                if (in_buffer.Length == 0) return false;

                //

                byte[] valueArray = new byte[4];
                Array.Copy(in_buffer, 6, valueArray, 0, 4);                

                RecordValue recordValue;
                recordValue.type = (byte)(channel + 1);
                recordValue.fine_state = true;
                recordValue.value = (valueArray[3] << 24 | valueArray[2] << 16 | valueArray[1] << 8 | valueArray[0]);

                recordValue.fine_state = true;

                values = recordValue.value;
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

        private int BCD2Int(byte[] buf)
        {
            int idx = 0;
            int val = 0;
            unchecked
            {
                do
                {
                    val *= 100;
                    val += ((buf[idx] >> 4) * 10) + (buf[idx] % 16);
                    idx++;
                } while (idx < buf.Length);

            }

            return val;
        }

        private int BCD2Int2(byte[] buf)
        {
            int idx = 0;
            int val = 0;
            unchecked
            {
                do
                {
                    val *= 100;
                    val += ((buf[idx] >> 4) * 10) + (buf[idx] % 16);
                    idx++;
                } while (idx < buf.Length);

            }

            return val;
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
                        //возможно я тут и не верно понял - что должно происходить
                        //у меня - проверяется корректность всего пакета
                        byte[] crc16 = CRC16(array, array.Length);
                        if ((crc16[0] == 0) && (crc16[1] == 0)) return 0;
                        else return -1;
                        
                    }
                }

                throw new ApplicationException("Несовпадение байт в пакете");
            }
            catch
            {
                return -1;
            }
        }


        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<RecordPowerSlice> listRPS, byte period)
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


