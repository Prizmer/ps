﻿//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace OldCode
//{
//    public sealed class ComPort2 : VirtualPort, IDisposable
//    {
//        private SerialPort m_Port;
//        private string m_name = "COM1";
//        private int m_baudrate = 9600;
//        private int m_data_bits = 8;
//        private Parity m_parity = Parity.None;
//        private StopBits m_stop_bits = StopBits.One;
//        private ushort m_write_timeout = 100;
//        private ushort m_read_timeout = 100;
//        private byte m_attemts = 0;

//        public string GetName()
//        {
//            return m_name + " ";
//        }

//        public void Close()
//        {
//            //if (sender != null)
//            //    sender.Close();
//        }

//        bool areLogsRestricted = false;
//        public ComPort2(byte number, int baudrate, byte data_bits, byte parity, byte stop_bits, ushort write_timeout, ushort read_timeout, byte attemts)
//        {
//            m_name = "COM" + Convert.ToString(number);
//            m_baudrate = baudrate;
//            m_data_bits = data_bits;
//            m_parity = (Parity)parity;
//            m_stop_bits = (StopBits)stop_bits;
//            m_write_timeout = write_timeout;
//            m_read_timeout = read_timeout;
//            m_attemts = attemts;

//            areLogsRestricted = false;

//            try
//            {
//                m_Port = new SerialPort(m_name, m_baudrate, m_parity, m_data_bits, m_stop_bits);

//                /*ELF: для работы с elf108*/
//                m_Port.DtrEnable = true;
//                m_Port.RtsEnable = true;
//            }
//            catch (Exception ex)
//            {
//#if (DEBUG)
//                WriteToLog("Create " + m_name + ": " + ex.Message);
//#endif
//            }
//        }

//        public void Dispose()
//        {
//            if (m_Port != null)
//            {
//                m_Port.Dispose();
//            }
//        }

//        private bool OpenPort()
//        {
//            try
//            {
//                if (m_Port != null)
//                {
//                    if (!m_Port.IsOpen)
//                    {
//                        m_Port.Open();
//                    }
//                }

//                return m_Port.IsOpen;
//            }
//            catch (Exception ex)
//            {
//#if (DEBUG)
//                WriteToLog("Open " + m_name + ": " + ex.Message);
//#endif
//                return false;
//            }
//        }

//        private void ClosePort()
//        {
//            try
//            {
//                if (m_Port != null)
//                {
//                    if (m_Port.IsOpen)
//                    {
//                        m_Port.Close();
//                        //m_Port = null;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//#if (DEBUG)
//                WriteToLog("Close " + m_name + ": " + ex.Message);
//#endif
//            }
//        }

//        #region Старые версии функций
//        /*public void Write(byte[] m_cmd, int leng)
//        {
//            if (OpenPort())
//            {
//                m_Port.Write(m_cmd, 0, leng);
//            }
//        }

//        public int Read(ref byte[] data)
//        {
//            int reading_size = 0;

//            uint elapsed_time_count = 0;

//            Queue<byte> reading_queue = new Queue<byte>(8192);

//            Thread.Sleep(100);
//            //while (elapsed_time_count < m_read_timeout)
//            {
//                if (m_Port.BytesToRead > 0)
//                {
//                    try
//                    {
//                        byte[] tmp_buff = new byte[m_Port.BytesToRead];
//                        reading_size = m_Port.Read(tmp_buff, 0, tmp_buff.Length);

//                        data = tmp_buff;


//                    }
//                    catch (Exception ex)
//                    {
//                        WriteToLog("ReadData: " + ex.Message);
//                    }

//                    //WriteToLog("Request: " + BitConverter.ToString(out_buffer));
//                }
//                //elapsed_time_count += 100;
//                //Thread.Sleep(100);
//            }

//            ClosePort();

//            return reading_size;
//        }*/

//        /*
//        /// <summary>
//        /// Передает даннные из буфера-на-выход в порт, и принимает данные в буфер-на-вход
//        /// </summary>
//        /// <param name="func"></param>
//        /// <param name="out_buffer">Массив байт передаваемый устройству</param>
//        /// <param name="in_buffer">Массив байт принимаемый от устройства</param>
//        /// <param name="out_length">Определяет длину полезных байт в буфере-на-выход (длину команды)</param>
//        /// <param name="target_in_length">Определяет длину полезных байт в буфере-на-вход (длину ответа)</param>
//        /// <param name="pos_count_data_size"></param>
//        /// <param name="size_data"></param>
//        /// <param name="header_size"></param>
//        /// <returns></returns>
//        public int WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length,
//            uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0)
//        {
//            int reading_size = 0;

//            if (OpenPort())
//            {
//                Queue<byte> reading_queue = new Queue<byte>(8192);

//                try
//                {
//                    //пишем в порт команду, ограниченную out_length
//                    m_Port.Write(out_buffer, 0, out_length);

//                    //ожидаем 100мс
//                    Thread.Sleep(100);
//                    uint elapsed_time_count = 100;

//                    while (elapsed_time_count <= m_read_timeout)
//                    {
//                        if (m_Port.BytesToRead > 0)
//                        {
//                            try
//                            {
//                                byte[] tmp_buff = new byte[m_Port.BytesToRead];
//                                //читаем все данные из входного буфера
//                                int readed_bytes = m_Port.Read(tmp_buff, 0, tmp_buff.Length);
//                                //определяем из в очередь
//                                for (int i = 0; i < readed_bytes; i++)
//                                    reading_queue.Enqueue(tmp_buff[i]);
//                            }
//                            catch (Exception ex)
//                            {
//                                WriteToLog("WriteReadData: " + ex.Message);
//                            }

//                            //WriteToLog("Request: " + BitConverter.ToString(out_buffer));

//                            //TODO: Откуда взялась константа 4, почему 4?
//                            if (reading_queue.Count >= 4)
//                            {

//                                int pos = func(reading_queue);
//                                if (pos >= 0)
//                                {
//                                    //избавимся от лишних данных спереди
//                                    for (int i = 0; i < pos; i++)
//                                    {
//                                        reading_queue.Dequeue();
//                                    }

//                                    //оставшиеся данные преобразуем обратно в массив
//                                    byte[] temp_buffer = new byte[reading_size = reading_queue.Count];
//                                    temp_buffer = reading_queue.ToArray();

//                                    //если длина полезных данных ответа не
//                                    if (target_in_length == 0)
//                                    {
//                                        if (reading_size > pos_count_data_size)
//                                        {
//                                            target_in_length = Convert.ToInt32(temp_buffer[pos_count_data_size] * size_data + header_size);
//                                        }
//                                    }

//                                    if (target_in_length > 0)
//                                    {
//                                        if (reading_size >= target_in_length)
//                                        {
//                                            reading_size = target_in_length;
//                                            for (int i = 0; i < target_in_length && i < in_buffer.Length; i++)
//                                            {
//                                                in_buffer[i] = temp_buffer[i];
//                                            }
//                                            break;
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                        else
//                        {
//                            //reading_size = 0;
//                        }

//                        elapsed_time_count += 100;
//                        Thread.Sleep(100);
//                    }
//                }
//                catch (Exception ex)
//                {
//                    WriteToLog("WriteReadData: " + ex.Message);
//                    return -1;
//                }
//                finally
//                {
//                    reading_queue.Clear();

//                    ClosePort();
//                }
//            }

//            return reading_size;
//        }
//            */
//        #endregion

//        public int WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length,
//            uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0)
//        {
//            int reading_size = 0;

//            if (OpenPort())
//            {
//                //очередь для поддержки делегатов в старых драйверах
//                Queue<byte> reading_queue = new Queue<byte>(8192);
//                List<byte> readBytesList = new List<byte>(8192);

//                try
//                {
//                    //пишем в порт команду, ограниченную out_length
//                    m_Port.Write(out_buffer, 0, out_length);
//                }
//                catch (Exception ex)
//                {
//                    WriteToLog("WriteReadData: Write to port error: " + ex.Message);
//                }


//                Thread.Sleep(100);
//                uint elapsed_time_count = 100;

//                while (elapsed_time_count <= m_read_timeout)
//                {
//                    if (m_Port.BytesToRead > 0)
//                    {
//                        try
//                        {
//                            byte[] tmp_buff = new byte[m_Port.BytesToRead];
//                            int readed_bytes = m_Port.Read(tmp_buff, 0, tmp_buff.Length);

//                            readBytesList.AddRange(tmp_buff);
//                        }
//                        catch (Exception ex)
//                        {
//                            WriteToLog("WriteReadData: Read from port error: " + ex.Message);
//                        }
//                    }

//                    elapsed_time_count += 100;
//                    Thread.Sleep(100);
//                }


//                /*TODO: Откуда взялась константа 4, почему 4?*/
//                if (readBytesList.Count > 0)
//                {
//                    /*попытаемся определить начало полезных данных в буфере-на-вход
//                        при помощи связанного делегата*/
//                    for (int i = 0; i < readBytesList.Count; i++)
//                        reading_queue.Enqueue(readBytesList[i]);

//                    int pos = func(reading_queue);
//                    if (pos >= 0)
//                    {
//                        //избавимся от лишних данных спереди
//                        for (int i = 0; i < pos; i++)
//                        {
//                            reading_queue.Dequeue();
//                        }

//                        //оставшиеся данные преобразуем обратно в массив
//                        byte[] temp_buffer = new byte[reading_size = reading_queue.Count];

//                        //WriteToLog("reading_queue.Count: " + reading_size.ToString());

//                        temp_buffer = reading_queue.ToArray();
//                        //WriteToLog(BitConverter.ToString(temp_buffer));

//                        //если длина полезных данных ответа определена как 0, произведем расчет по необязательнм параметрам
//                        if (target_in_length == 0)
//                        {
//                            if (reading_size > pos_count_data_size)
//                                target_in_length = Convert.ToInt32(temp_buffer[pos_count_data_size] * size_data + header_size);
//                        }

//                        if (target_in_length == -1)
//                        {
//                            target_in_length = reading_queue.Count;
//                            reading_size = target_in_length;
//                            in_buffer = new byte[reading_size];

//                            for (int i = 0; i < in_buffer.Length; i++)
//                                in_buffer[i] = temp_buffer[i];

//                            ClosePort();
//                            return reading_size;
//                        }

//                        if (target_in_length > 0 && reading_size >= target_in_length)
//                        {
//                            reading_size = target_in_length;
//                            for (int i = 0; i < target_in_length && i < in_buffer.Length; i++)
//                            {
//                                in_buffer[i] = temp_buffer[i];
//                            }
//                        }
//                    }
//                }
//            }
//            else
//            {
//                WriteToLog("Open port Error");
//            }

//            ClosePort();
//            return reading_size;
//        }

//        public void WriteToLog(string str)
//        {
//            if (areLogsRestricted) return;

//            try
//            {
//                using (StreamWriter sw = new StreamWriter(@"logs\com_ports.log", true, Encoding.Default))
//                {
//                    sw.WriteLine(DateTime.Now.ToString() + ": " + GetName() + ": " + str);
//                }
//            }
//            catch
//            {
//            }
//        }


//        public string GetConnectionType()
//        {
//            return "com";
//        }

//        public bool ReInitialize()
//        {
//            return false;
//        }

//        public bool GetLocalEndPoint(ref IPEndPoint localEp)
//        {
//            localEp = null;
//            return false;
//        }


//        public void SetReadTimeout(int timeout = 1200)
//        {
//            return;
//        }


//        public object GetPortObject()
//        {
//            return null;
//        }


//        public string GetFullName()
//        {
//            return "COM_NOT_IMPLEMENTED";
//        }

//        public void SetConfigurationManagerAppSettings(NameValueCollection loadedAppSettings)
//        {
//            return;
//        }
//    }


//}
