using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Collections.Specialized;

using PollingLibraries.LibLogger;


namespace PollingLibraries.LibPorts
{
    public delegate int FindPacketSignature(Queue<byte> queue);

    /// <summary>
    /// Структура хранит данные о подключении по последовательному порту
    /// </summary>
    public struct ComPortSettings
    {
        public Guid guid;
        public String name;
        public UInt32 baudrate;
        public Byte data_bits;
        public Byte parity;
        public Byte stop_bits;
        public UInt16 write_timeout;
        public UInt16 read_timeout;
        public UInt16 attempts;
        public UInt16 delay_between_sending;
        public string gsm_phone_number;
        public string gsm_init_string;
        public bool gsm_on;
        public bool bDtr;
    }

    /// <summary>
    /// Структура хранит данные о подключении по tcp/ip
    /// </summary>
    public struct TCPIPSettings
    {
        public Guid guid;
        public String ip_address;
        public UInt16 ip_port;
        public UInt16 write_timeout;
        public UInt16 read_timeout;
        public UInt16 attempts;
        public UInt16 delay_between_sending;
    }

    public interface VirtualPort
    {
        int WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length, uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0);
        string GetName();
        string GetFullName();

        string GetConnectionType();
        bool ReInitialize();
        bool GetLocalEndPoint(ref IPEndPoint localEp);
        bool OpenPort();
        void Close();
        void SetReadTimeout(int timeout = 1200);

        void SetConfigurationManagerAppSettings(NameValueCollection loadedAppSettings);

        object GetPortObject();
    }

    public class PortSettings
    {
        public static string selectedLocalIp = "";

        public static void ShowFormSelectLocalIp()
        {
            Form formSelectLocalIp = new Form()
            {
                Height = 178,
                Width = 230,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                Text = "Выберите локальный адрес"
            };

            ListBox listBoxLocalIps = new ListBox()
            {
                Height = 96,
                Width = 198,
                Top = 12,
                Left = 12
            };

            Button btnApply = new Button()
            {
                Height = 23,
                Width = 75,
                Top = 113,
                Left = 135,
                Text = "Применить",
                DialogResult = DialogResult.OK,
                Enabled = false
            };


            formSelectLocalIp.Load += delegate (object sender, EventArgs e)
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        listBoxLocalIps.Items.Add(ip.ToString());
                    }
}

                if (listBoxLocalIps.Items.Count > 0)
                {
                    btnApply.Enabled = true;
                }
            };

            btnApply.Click += delegate (object sender, EventArgs e)
            {
                if (listBoxLocalIps.SelectedIndex != -1)
                {
                    selectedLocalIp = listBoxLocalIps.SelectedItem.ToString();
                }

                MessageBox.Show(selectedLocalIp);
            };

            formSelectLocalIp.Controls.Add(listBoxLocalIps);
            formSelectLocalIp.Controls.Add(btnApply);
        }
    }

    public sealed class TcpipPort : VirtualPort
    {
        private string m_address = "127.0.0.1";
        private int m_port = 80;
        private ushort m_write_timeout = 100;
        private ushort m_read_timeout = 100;
        private int m_delay_between_sending = 100;

        Logger tcpLogger;

        NameValueCollection loadedAppSettings = new NameValueCollection();

        bool areLogsRestricted = false;
        public TcpipPort(string address, int port, ushort write_timeout, ushort read_timeout, int delay_between_sending, NameValueCollection loadedAppSettings = null)
        {
            tcpLogger = new Logger();

            m_address = address;
            m_port = port;
            m_write_timeout = write_timeout;
            m_read_timeout = read_timeout;
            m_delay_between_sending = delay_between_sending;

            tcpLogger.Initialize(Logger.DIR_LOGS_PORTS, false, GetName());
            areLogsRestricted = false;

            if (loadedAppSettings != null)
                this.loadedAppSettings = loadedAppSettings;

            ReInitialize();
        }

        ~TcpipPort()
        {
            if (sender != null)
                sender.Close();
        }

        public string GetName()
        {
            return "tcp" + m_address + "_" + m_port;
        }

        public void Close()
        {
            if (sender != null)
                sender.Close();
        }

        void VirtualPort.SetConfigurationManagerAppSettings(NameValueCollection loadedAppSettings)
        {

        }

        IPAddress ipLocalAddr = null;
        IPEndPoint ipLocalEndpoint = null;
        IPEndPoint remoteEndPoint = null;
        Socket sender = null;

        DateTime dtCreated = DateTime.Now;

        public bool ReInitialize()
        {
            try
            {
                if (sender != null) sender.Close();

                byte[] ipAddrLocalArr = { 192, 168, 0, 1 };
                ipLocalAddr = new IPAddress(ipAddrLocalArr);

                // bool bRes = GetLocalEndPointIp(ref ipLocalAddr);
                bool bRes = GetLocalEndPointIpByRemote(m_address, ref ipLocalAddr);

                ipLocalEndpoint = new IPEndPoint(ipLocalAddr, GetFreeTcpPort());
                remoteEndPoint = new IPEndPoint(IPAddress.Parse(m_address), (int)m_port);

                sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                LingerOption lingOpt = new LingerOption(true, 1);
                sender.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingOpt);

                sender.ReceiveTimeout = 800;
                sender.SendTimeout = 400;

                try
                {
                    //WriteToLog("IpLocalEndp: " + ipLocalEndpoint.ToString() + ";  Remote: " + remoteEndPoint.ToString() );

                    sender.Bind(ipLocalEndpoint);

                    //old version of connection 
                    //sender.Connect(remoteEndPoint);

                    // Connect using a timeout (5 seconds)
                    IAsyncResult result = sender.BeginConnect(remoteEndPoint, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(2000, true);

                    if (sender.Connected)
                    {
                        dtCreated = DateTime.Now;
                        return true;
                    }
                    else
                    {
                        WriteToLog("ReInitialize: не удалось установить соединение между " + 
                            ipLocalEndpoint.ToString() + " и " + remoteEndPoint.ToString()) ;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    WriteToLog("ReInitialize: не удалось установить соединение: " + ex.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteToLog("ReInitialize: TCP порт НЕ ре-инициализирован по причине: " + ex.Message);
                return false;
            }
        }

        bool GetTCPPortLiveMinutes(out int timeout)
        {
            timeout = 60;

            string tmpValStr = "";
            try
            {
                tmpValStr = loadedAppSettings.GetValues("tcpPortLiveMinutes")[0];
            }
            catch (Exception ex)
            {
                return false;
            }

            bool parsingResult = false;
            if (tmpValStr.Length > 0)
                parsingResult = int.TryParse(tmpValStr, out timeout);

            if (parsingResult)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        int GetFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
        bool IsTcpPortFree(int tcpPort)
        {
            bool isAvailable = true;

            // Evaluate current system tcp connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == tcpPort)
                {
                    isAvailable = false;
                    break;
                }
            }

            return isAvailable;
        }

        string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().StartsWith("192.168"))
                        return ip.ToString();
                }
            }

            return "192.168.0.1";
        }
        bool GetLocalEndPointIp(ref IPAddress localEndpointIp)
        {
            string strIpConfig = "";
            try
            {
                object tmp = loadedAppSettings.GetValues("localEndPointIp");
                if (tmp != null)
                    strIpConfig = ((string[])tmp)[0];
                //WriteToLog("GetLocalEndPointIp: IP прочитанный из конфигурации: " + strIpConfig);
            }
            catch (Exception ex)
            {
                WriteToLog("GetLocalEndPointIp: " + ex.ToString());
            }

            bool parsingResult = false;
            if (strIpConfig.Length > 0)
                parsingResult = IPAddress.TryParse(strIpConfig, out localEndpointIp);

            if (parsingResult)
            {
                return true;
            }
            else
            {
                strIpConfig = GetLocalIPAddress();
                return parsingResult = IPAddress.TryParse(strIpConfig, out localEndpointIp);
            }
        }

        bool GetLocalEndPointIpByRemote(string remoteAddrWithoutPort, ref IPAddress localEndpointIp)
        {
            string resultLocalIp = "";

            var host = Dns.GetHostEntry(Dns.GetHostName());
            string[] remoteIpGroups = remoteAddrWithoutPort.Split('.');
            string ipStart = "";
            string infoAvailiablePorts = "";
            
            foreach (var ip in host.AddressList)
            {       
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    infoAvailiablePorts += ip.ToString() + "; ";
                    if (resultLocalIp == "")
                    {
                        // возьмем сеть и подсеть у целевого адреса и выберем доступный
                        if (remoteIpGroups.Length > 1)
                            ipStart = remoteIpGroups[0] + "." + remoteIpGroups[1];

                        if (ip.ToString().StartsWith(ipStart))
                            resultLocalIp = ip.ToString();
                    }
                }
            }

            // если не удалось выбрать ip из доступных в системе
            if (resultLocalIp == "")
            {
                WriteToLog("GetLocalEndPointIpByRemote: не удалось выбрать ip из доступных в системе...");
                WriteToLog("GetLocalEndPointIpByRemote: искали по " + ipStart + "; доступные порты: " + infoAvailiablePorts + "; целевой адрес: " + remoteAddrWithoutPort);
                // попробуем взять из конфига
                try
                {
                    object tmp = loadedAppSettings.GetValues("localEndPointIp");
                    if (tmp != null)
                        resultLocalIp = ((string[])tmp)[0];
                    //WriteToLog("GetLocalEndPointIp: IP прочитанный из конфигурации: " + strIpConfig);
                }
                catch (Exception ex)
                {
                    WriteToLog("GetLocalEndPointIpByRemote: " + ex.ToString());
                }
            }

            bool parsingResult = false;
            if (resultLocalIp.Length > 0)
                parsingResult = IPAddress.TryParse(resultLocalIp, out localEndpointIp);

            if (parsingResult)
            {
                return true;
            }
            else
            {
                resultLocalIp = GetLocalIPAddress();
                return parsingResult = IPAddress.TryParse(resultLocalIp, out localEndpointIp);
            }
        }

        public bool ManageUpWithReceivedBytes(List<byte> readBytesList,
            FindPacketSignature func, int target_in_length,
            out byte[] outDataArr, out int outReadingSize,
            uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0)
        {
            outDataArr = new byte[1];
            outDataArr[0] = 0x0;
            outReadingSize = 0;

            //очередь для поддержки делегатов в старых драйверах
            Queue<byte> reading_queue = new Queue<byte>(8192);

            int reading_size = 0;

            if (readBytesList.Count > 0)
            {
                /*попытаемся определить начало полезных данных в буфере-на-вход
                    при помощи связанного делегата*/
                for (int i = 0; i < readBytesList.Count; i++)
                    reading_queue.Enqueue(readBytesList[i]);

                int pos = 0;
                if (pos == 0)
                {
                    //избавимся от лишних данных спереди
                    //for (int i = 0; i < pos; i++)
                    //    reading_queue.Dequeue();

                    //оставшиеся данные преобразуем обратно в массив
                    reading_size = reading_queue.Count;

                    byte[] temp_buffer = new byte[reading_size];
                    temp_buffer = reading_queue.ToArray();
                    //WriteToLog("ManageUpWithReceivedBytes: received=" + BitConverter.ToString(temp_buffer));

                    //WriteToLog("ManageUpWithReceivedBytes: targetInLength=" + target_in_length);
                    //WriteToLog("ManageUpWithReceivedBytes: readingSize=" + reading_size);

                    //если длина полезных данных ответа определена как 0, произведем расчет по необязательнм параметрам
                    if (target_in_length == 0)
                    {
                        if (reading_size > pos_count_data_size)
                            target_in_length = Convert.ToInt32(temp_buffer[pos_count_data_size] * size_data + header_size);

                        outReadingSize = reading_size;
                        return true;
                    }

                    if (target_in_length == -1)
                    {
                        outDataArr = new byte[reading_queue.Count];
                        // outDataArr = new byte[reading_size];

                        for (int i = 0; i < outDataArr.Length; i++)
                            outDataArr[i] = temp_buffer[i];

                        outReadingSize = outDataArr.Length;
                        return true;
                    }

                    if (target_in_length > 0 && reading_size >= target_in_length)
                    {
                        outDataArr = new byte[target_in_length];
                        reading_size = target_in_length;
                        for (int i = 0; i < target_in_length && i < outDataArr.Length; i++)
                            outDataArr[i] = temp_buffer[i];

                        outReadingSize = reading_size;

                        WriteToLog("To driver: " + BitConverter.ToString(outDataArr));

                        return true;
                    }
                }
            }

            return false;
        }

        public int WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length, uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0)
        {
            //инициализация на случай ошибочного выхода
            in_buffer = new byte[1];
            in_buffer[0] = 0x0;

            int readingSize = 0;
            List<byte> readBytesList = new List<byte>(8192);

            TimeSpan ts = DateTime.Now - dtCreated;
            int tcpAliveMinutes = 60;
            GetTCPPortLiveMinutes(out tcpAliveMinutes);
            if (ts.TotalMinutes >= tcpAliveMinutes)
            {
                //погружаемся в сон на 5 минут, чтобы "дать отдохнуть" принимающим устройствам
                //WriteToLog("WriteReadData: погружаемся в сон на 5 минут, чтобы дать отдохнуть принимающим устройствам");

                if (sender != null && sender.Connected) sender.Close();
                Thread.Sleep(1000 * 60 * 5);
                dtCreated = DateTime.Now;

                ReInitialize();
                // WriteToLog("WriteReadData: открыт новый сокет после сна: " + sender.LocalEndPoint.ToString());
            }

            try
            {
                //2 попытки соединения или чтения данных
                for (int i = 0; i < 2; i++)
                {
                    WriteToLog("Attempt: " + i);
                    readBytesList.Clear();
                    if (sender.Connected)
                    {
                        // Send the data through the socket.
                        sender.Send(out_buffer, 0, out_length, SocketFlags.None);

                        WriteToLog("<< Written data: " + BitConverter.ToString(out_buffer) + "\n");


                        Thread.Sleep(10);
                        uint elapsed_time_count = 100;

                        while (elapsed_time_count <= m_read_timeout)
                        {
                            if (sender.Available > 0)
                            {
                                try
                                {
                                    byte[] tmp_buff = new byte[sender.Available];
                                    int readed_bytes = sender.Receive(tmp_buff);
                                    WriteToLog(">> received_" + elapsed_time_count + "ms ("+ readed_bytes + " bytes): " + BitConverter.ToString(tmp_buff));
                                    readBytesList.AddRange(tmp_buff);
                                }
                                catch (Exception ex)
                                {
                                    WriteToLog("WriteReadData: Reading from port error: " + ex.Message);
                                }

                                Queue<byte> tmpQ = new Queue<byte>();
                                for (int j = readBytesList.Count - 1; j >= 0; j--)
                                    tmpQ.Enqueue(readBytesList[j]);

                                int packageSign = func(tmpQ);
                                if (packageSign == 1)
                                {
                                    WriteToLog("WriteReadData: break on attempt attempt " + i + ", with packageSign; result=" + packageSign);
                                    break;
                                }
 
                            }

                            elapsed_time_count += 100;
                            Thread.Sleep(100);
                        }

                        string tmpResStr = BitConverter.ToString(readBytesList.ToArray());
                        WriteToLog(">> Totaly received: " + tmpResStr + "\n");
                        //  if (tmpResStr.Length < 4)
                        // WriteToLog("received data: " + tmpResStr);

                        bool bManageRes = false;
                        try
                        {
                            bManageRes = ManageUpWithReceivedBytes(
                                readBytesList,
                                func,
                                target_in_length,
                                out in_buffer,
                                out readingSize,
                                pos_count_data_size,
                                size_data,
                                header_size
                            );
                        }
                        catch (Exception ex)
                        {
                            WriteToLog("WriteReadData: ManageUpWithReceivedBytes execution error: " + ex.Message);
                            return -1;
                        }


                        if (bManageRes)
                            break;
                        else
                            Thread.Sleep(100);
                    }
                    else
                    {
                        if (i == 0)
                            ReInitialize();
                        else
                            WriteToLog("WriteReadData: 2 попытки приема/передачи безуспешны");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToLog("WriteReadData: " + ex.Message);
                return -1;
            }

            return readingSize;
        }

        public void WriteToLog(string str)
        {
            tcpLogger.LogWarn(str);
        }

        public string GetConnectionType()
        {
            return "tcp";
        }

        public bool GetLocalEndPoint(ref IPEndPoint localEp)
        {
            return false;
        }


        public void SetReadTimeout(int timeout = 1200)
        {
            // this.readTimeout = timeout;
        }


        public object GetPortObject()
        {
            return sender;
        }

        public string GetFullName()
        {
            if (sender != null)
                return m_address.ToString() + ":" + m_port;
            else
                return "";
        }

        public bool OpenPort()
        {
            return true;
        }
    }


    public class ComPort : VirtualPort, IDisposable
    {
        SerialPort serialPort;
        int readTimeout = 600;
        int writeTimeout = 600;
        int attemts = 1;
        ComPortSettings _cps = new ComPortSettings();
        Logger comLogger;

        public ComPort(ComPortSettings cps)
        {
            _cps = cps;

            comLogger = new Logger();
            comLogger.Initialize(Logger.DIR_LOGS_PORTS, false, "COM" + cps.name);

            comLogger.LogInfo("This is test msg");

            comLogger.LogInfo("_cps: " + _cps.gsm_on);

            //если не передано настроек, создадим рандомный порт
            if (cps.name == "")
            {
                serialPort = new SerialPort("COM250");
                serialPort.BaudRate = 2400;
                serialPort.Parity = Parity.Odd;
                serialPort.DataBits = 8;
                serialPort.StopBits =StopBits.One;

                readTimeout = 400;
                writeTimeout = 400;
                attemts = 1;
                return;
            }
            

            serialPort = new SerialPort("COM" + cps.name);

            serialPort.BaudRate = (int)cps.baudrate;
            serialPort.Parity = (Parity)cps.parity;

            serialPort.DataBits = cps.data_bits;
            serialPort.StopBits = (StopBits)cps.stop_bits;

            serialPort.DtrEnable = cps.bDtr;

            readTimeout = cps.read_timeout;
            writeTimeout = cps.write_timeout;
            attemts = cps.attempts;

            idleThread = new Thread(idleThreadHandler);
            idleThread.IsBackground = true;
            idleThread.Start();
        }

        public ComPort(SerialPort sp, byte attempts, ushort read_timeout, ushort write_timeout)
        {
            comLogger = new Logger();
            comLogger.Initialize(Logger.DIR_LOGS_PORTS, false, "COM" + sp.PortName);

            serialPort = new SerialPort(sp.PortName, sp.BaudRate, sp.Parity, sp.DataBits, sp.StopBits);
            serialPort.DtrEnable = sp.DtrEnable;

            readTimeout = read_timeout;
            writeTimeout = write_timeout;

            //serialPort.ReadTimeout = readTimeout;
            //serialPort.WriteTimeout = writeTimeout;

            serialPort.RtsEnable = true;
            serialPort.Handshake = Handshake.None;
        }

        ~ComPort()
        {
            Close();
        }


        private int FindATSignature(Queue<byte> queue)
        {
            byte[] qArr = queue.ToArray();
            for (int i = 0; i < qArr.Length; i++)
            {
                if (qArr[i] == 0x4f && i < qArr.Length - 1 && qArr[i + 1] == 0x4b)
                    return 1;
            }

            return 0;
        }

        private int FindATDisconnectSignature(Queue<byte> queue)
        {
            byte[] qArr = queue.ToArray();
            Array.Reverse(qArr);
            string answ = ASCIIEncoding.ASCII.GetString(qArr);
            comLogger.LogInfo("FindATDisconnectSignature: answ: " + answ);
            if (answ.IndexOf("OK") != -1)
            {
                return 1;
            }

            //for (int i = 0; i < qArr.Length; i++)
            //{
            //    if (qArr[i] == 0x4f && i < qArr.Length - 1 && qArr[i + 1] == 0x4b)
            //        return 1;
            //}

            return 0;
        }

        private bool ConnectToAt()
        {
            string at_cmd_connect = "ATD" + _cps.gsm_phone_number + "\r";
            byte[] cmdConnect = ASCIIEncoding.ASCII.GetBytes(at_cmd_connect);
            byte[] inAtBuffer = new byte[1];

            comLogger.LogInfo("WriteReadData, gsm connect: " + BitConverter.ToString(cmdConnect));
            readTimeout = 30000;
            int _rTimeout = readTimeout;
            WriteReadData(FindATSignature, cmdConnect, ref inAtBuffer, cmdConnect.Length, -1);
            readTimeout = _rTimeout;
            string answ = ASCIIEncoding.ASCII.GetString(inAtBuffer);
            comLogger.LogInfo("WriteReadData, gsm connect answ: " + answ);

            if (answ.IndexOf("CONNECT") != -1)
            {
                return true;

            }
            else
            {
                return false;
            }
        }

        private bool DisconnectFromAt()
        {
            isTryingToPerformATDisconnect = true;

            comLogger.LogInfo("WriteReadData, gsm disconnect +++: старт");
            byte[] at_cmd_plus = new byte[] { 0x2b, 0x2b, 0x2b };
            string at_cmd_hang = "ATH0\r";
            byte[] cmdHang= ASCIIEncoding.ASCII.GetBytes(at_cmd_hang);
            byte[] inAtBuffer = new byte[1];

            WriteReadData(FindATDisconnectSignature, at_cmd_plus, ref inAtBuffer, at_cmd_plus.Length, -1);
            string answ = ASCIIEncoding.ASCII.GetString(inAtBuffer);
            comLogger.LogInfo("WriteReadData, gsm disconnect: реакция на команду " + BitConverter.ToString(at_cmd_plus) + ": " + answ);
            if (answ.IndexOf("OK") == -1)
            {
               comLogger.LogError("WriteReadData, gsm disconnect: выход, ответ неверный");
                isTryingToPerformATDisconnect = false;
               // return false;
            }

            isTryingToPerformATDisconnect = true;
            comLogger.LogInfo("WriteReadData, gsm disconnect hang: старт" + BitConverter.ToString(cmdHang));
            WriteReadData(FindATDisconnectSignature, cmdHang, ref inAtBuffer, cmdHang.Length, -1);
            answ = ASCIIEncoding.ASCII.GetString(inAtBuffer);
            comLogger.LogInfo("WriteReadData, gsm disconnect: реакция на команду " + BitConverter.ToString(cmdHang) + ": " + answ);
            if (answ.IndexOf("OK") == -1)
            {
                comLogger.LogError("WriteReadData, gsm disconnect: выход, ответ неверный");
                isConnectedToAt = false;
                isTryingToPerformATDisconnect = false;
                return false;
            }

            isTryingToPerformATDisconnect = false;

            return true;
        }


        bool isTryingToPerformATConnect = false;
        bool isTryingToPerformATDisconnect = false;
        bool dataWillBeTransfered = false;
        bool isConnectedToAt = false;

        Thread idleThread;
        private volatile bool isIdle = false;
        private volatile int secondsToDisconect = 10;
        private volatile int idleThreadHandlerCnt = 0;
        private volatile bool idleThreadOnGo = true;

        public void idleThreadHandler()
        {
            idleThreadHandlerCnt = 0;
            while (idleThreadOnGo)
            {
                Thread.Sleep(1000);

                if (isIdle)
                {
                    idleThreadHandlerCnt++;
                    if (idleThreadHandlerCnt == secondsToDisconect)
                    {
                        comLogger.LogInfo("idleThreadHandler: disconnect attempt start");

                        DisconnectFromAt();
                        idleThreadHandlerCnt = 0;

                        isIdle = false;
                        isConnectedToAt = false;
                        isTryingToPerformATConnect = false;
                        isTryingToPerformATDisconnect = false;
                    }
                }

            }
        }


        public int WriteReadData(FindPacketSignature func, byte[] out_buffer, ref byte[] in_buffer, int out_length, int target_in_length, uint pos_count_data_size = 0, uint size_data = 0, uint header_size = 0)
        {
            isIdle = false;
            if (!OpenPort()) return 0;
     
            if (_cps.gsm_on && !isConnectedToAt && !isTryingToPerformATConnect && !isTryingToPerformATDisconnect)
            {
                isTryingToPerformATConnect = true;
                isConnectedToAt = ConnectToAt();
                isTryingToPerformATConnect = false;

                if (!isConnectedToAt)
                {
                    comLogger.LogError("WriteReadData, gsm connect: не удалось подключиться к модему");
                    isIdle = true;
                    return 0;
                }
                else
                {
                    isIdle = false;
                }
            }


            string infStrCustom = String.Format("Custom: RTimeout: {0}; WTimeout: {1};", readTimeout, writeTimeout);
            string infStrPort = String.Format("PortInfo: NAME: {0}; Parity: {1}; DTR: {2}; STOP: {3}; BITES: {4}; RTS: {5};", 
                serialPort.PortName.ToString(), serialPort.Parity, serialPort.DtrEnable.ToString(), serialPort.StopBits, serialPort.DataBits, serialPort.RtsEnable.ToString());
            string infStrArg = String.Format("WriteReadDataArg: out_length (properCMDLength): {0};", out_length);
            WriteToLog("START with:" + Environment.NewLine + infStrCustom + Environment.NewLine + infStrPort + 
                Environment.NewLine + infStrArg);


            serialPort.Write(out_buffer, 0, out_buffer.Length);
            WriteToLog("<< Written data: " + BitConverter.ToString(out_buffer) + "\n");

            //Thread.Sleep(10);
            //if (serialPort.BytesToRead == 0)
            //    Thread.Sleep(writeTimeout);

            int elapsedTime = 0;

            List<byte> inBufferList = new List<byte>();

            while (elapsedTime < readTimeout)
            {
                if (serialPort.BytesToRead > 0)
                {
                    try
                    {
                        byte[] tmp_buff = new byte[serialPort.BytesToRead];
                        serialPort.Read(tmp_buff, 0, tmp_buff.Length);

                        if (tmp_buff.Length > 0)
                            inBufferList.AddRange(tmp_buff);

                        WriteToLog(">> received_" + elapsedTime + "ms (" + tmp_buff.Length + " bytes): " + BitConverter.ToString(inBufferList.ToArray()));

                    } catch (Exception ex)
                    {
                        WriteToLog("WriteReadData: Reading from port error: " + ex.Message);
                    }

                    Queue<byte> tmpQ = new Queue<byte>();
                    for (int j = inBufferList.Count - 1; j >= 0; j--)
                        tmpQ.Enqueue(inBufferList[j]);

                    int packageSign = func(tmpQ);
                    if (packageSign == 1)
                    {
                        in_buffer = inBufferList.ToArray();
                        WriteToLog("WriteReadData: break with packageSign; result=" + packageSign);
                        break;                      
                    }
                }

                in_buffer = inBufferList.ToArray();


                Thread.Sleep(100);
                elapsedTime += 100;
            }


            string tmpResStr = BitConverter.ToString(in_buffer);
            WriteToLog(">> Totaly received: " + tmpResStr + "\n");

            if (in_buffer.Length == 0)
                WriteToLog("RESULT: no bytes received with custom reading timeout: " + readTimeout);

            idleThreadHandlerCnt = 0;
            isIdle = true;

            return in_buffer == null ? 0 : in_buffer.Length;
        }

        public string GetName()
        {
            if (serialPort != null)
                return serialPort.PortName;
            else
                return "";
        }
        public bool isOpened()
        {
            if (serialPort != null && serialPort.IsOpen)
                return true;
            else
                return false;
        }
        public SerialPort getSerialPortObject()
        {
            return serialPort;
        }

        public bool OpenPort()
        {
            if (serialPort != null)
            {
  

                if (!serialPort.IsOpen)
                {
                    try
                    {
                        serialPort.Open();
                        idleThreadOnGo = true;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }

                    try
                    { 
                    serialPort.DiscardOutBuffer();
                    serialPort.DiscardInBuffer();
                    }
                    catch (Exception ex)
                    {

                    }

                    if (serialPort.IsOpen)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    idleThreadOnGo = true;
                    try
                    {
                        serialPort.DiscardOutBuffer();
                        serialPort.DiscardInBuffer();

                    }catch (Exception ex)
                    {

                    }

                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            idleThreadOnGo = false;
            serialPort.Close();
        }

        public void WriteToLog(string str)
        {
            comLogger.LogWarn(str);
        }

        public string GetConnectionType()
        {
            return "com";
        }

        public bool ReInitialize()
        {
            return false;
        }

        public bool GetLocalEndPoint(ref IPEndPoint localEp)
        {
            localEp = null;
            return false;
        }

        public void Close()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                if (_cps.gsm_on)
                {
                    idleThreadOnGo = false;
                    DisconnectFromAt();
                    Thread.Sleep(1000);
                }

                idleThreadOnGo = false;
                serialPort.Close();
            }
        }


        public void SetReadTimeout(int timeout = 1200)
        {
            this.readTimeout = timeout;
        }

        public object GetPortObject()
        {
            return serialPort;
        }


        public string GetFullName()
        {
            if (serialPort != null)
                return serialPort.PortName;
            else
                return "";
        }

        public void SetConfigurationManagerAppSettings(NameValueCollection loadedAppSettings)
        {
            return;
        }
    }
   
}
