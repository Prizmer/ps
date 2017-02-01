using System;
using System.Collections.Generic;
using System.Text;

using System.Threading;
using System.Reflection;
using System.Collections;

using System.Linq;

using Prizmer.PoolServer.DataBase;
using Prizmer.Meters;
using Prizmer.Meters.iMeters;

using System.Windows.Forms;
using System.Configuration;
using System.IO;


namespace Prizmer.PoolServer
{
    class Logger
    {
        public Logger() { }

        struct SenderInfo
        {
            public SenderInfo(string port, string addr, string dName)
            {
                this.port = port;
                this.addr = addr;
                this.driverName = dName;
            }

            public string port;
            public string addr;
            public string driverName;
        }

        public static volatile bool bRestrict = false;

        static string baseDirectory = "logs";
        string workDirectory = "";
        bool isInitialized = false;

        public static string BaseDirectory
        {
            get { return baseDirectory; }
        }

        SenderInfo si;
        public void Initialize(string port, string addr, string driverName, string workDirName = "")
        {
            if (workDirName != String.Empty)
                workDirectory = baseDirectory + "\\" + workDirName;
            else
                workDirectory = baseDirectory;

            si = new SenderInfo(port, addr, driverName);
            Directory.CreateDirectory(workDirectory);

            isInitialized = true;
        }

        private enum MessageType
        {
            ERROR,
            WARN,
            INFO
        }

        public void LogError(string message)
        {
            this.writeToLog(message, si, MessageType.ERROR);
        }

        public void LogInfo(string message)
        {
            this.writeToLog(message, si, MessageType.INFO);
        }

        public void LogWarn(string message)
        {
            this.writeToLog(message, si, MessageType.WARN);
        }

        private void writeToLoggerLog(string msg)
        {
            if (bRestrict) return;

            StreamWriter sw = null;
            string resMsg = String.Format("{0}: {1}", DateTime.Now.ToString(), msg);
            sw = new StreamWriter(baseDirectory + @"\loggerErr.log", true, Encoding.Default);
            sw.WriteLine(resMsg);
            sw.Close();

            if (fs != null)
            {
                fs.Close();
                fs = null;
            }
        }

        StreamWriter sw = null;
        FileStream fs = null;
        private void writeToLog(string message, SenderInfo senderInfo, MessageType messageType)
        {
            if (bRestrict) return; 

            if (!isInitialized)
            {
                writeToLoggerLog("Логгер не проинициализирован");
                return;
            }

            try
            {
                string pathToDir = String.Format(workDirectory + "\\{0}", DateTime.Now.Date.ToShortDateString().Replace(".", "_"));
                Directory.CreateDirectory(pathToDir);
                string logFileName = String.Format("\\{0}_a{1}_{2}_ms.log", senderInfo.port.Trim(), senderInfo.addr.Trim(), senderInfo.driverName.Trim());
                fs = new FileStream(pathToDir + logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                string resMsg = String.Format("{1} [{0}]: {2}", messageType.ToString(), DateTime.Now.ToString(), message);

                sw = new StreamWriter(fs, Encoding.Default);
                sw.WriteLine(resMsg);

                sw.Close();
                fs.Close();
            }
            catch (Exception lEx)
            {
                writeToLoggerLog(lEx.Message);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }

                if (fs != null)
                {
                    fs.Close();
                    fs = null;
                }
            }
        }

        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }

    }

    class MainService
    {

        public void WriteToLog(string str, string port = "", string addr = "", string mName = "", bool doWrite = true)
        {
            //TODO убрать метод, оставлен для поддержки
        }

        //список потоков для опроса приборов - один поток на каждый порт
        List<Thread> PortsThreads = new List<Thread>();

        string ConnectionString = "Server=localhost;Port=5432;User Id=postgres;Password=1;Database=prizmer;";

        struct PollingParams
        {
            public bool b_poll_current;
            public bool b_poll_day;
            public bool b_poll_month;
            public bool b_poll_hour;
            public bool b_poll_halfanhour;
            public bool b_poll_archive;

            public TimeSpan ts_current_period;
        }

        PollingParams pollingParams;

        bool bStopServer = true;

        public MainService()
        {
            //ConnectionString = global::PoolServer.Properties.Settings.Default.ConnectionString;
            ConnectionString = ConfigurationManager.ConnectionStrings["generalConnection"].ConnectionString;

            List<string> rowValues = new List<string>();

            pollingParams.b_poll_current = true;

            pollingParams.b_poll_day = true;
            pollingParams.b_poll_month = true;
            pollingParams.b_poll_hour = true;
            pollingParams.b_poll_halfanhour = true;
            pollingParams.b_poll_archive = true;
            pollingParams.ts_current_period = new TimeSpan(DateTime.Now.Ticks);

            try
            {
                string strTmpVal = ConfigurationManager.AppSettings.GetValues("b_poll_current")[0];
                bool.TryParse(strTmpVal, out pollingParams.b_poll_current);

                strTmpVal = ConfigurationManager.AppSettings.GetValues("ts_current_period")[0];
                TimeSpan.TryParse(strTmpVal, out pollingParams.ts_current_period);

                strTmpVal = ConfigurationManager.AppSettings.GetValues("b_poll_day")[0];
                bool.TryParse(strTmpVal, out pollingParams.b_poll_day);

                strTmpVal = ConfigurationManager.AppSettings.GetValues("b_poll_month")[0];
                bool.TryParse(strTmpVal, out pollingParams.b_poll_month);

                strTmpVal = ConfigurationManager.AppSettings.GetValues("b_poll_hour")[0];
                bool.TryParse(strTmpVal, out pollingParams.b_poll_hour);

                strTmpVal = ConfigurationManager.AppSettings.GetValues("b_poll_halfanhour")[0];
                bool.TryParse(strTmpVal, out pollingParams.b_poll_halfanhour);

                strTmpVal = ConfigurationManager.AppSettings.GetValues("b_poll_archive")[0];
                bool.TryParse(strTmpVal, out pollingParams.b_poll_archive);
            }
            catch (Exception ex)
            {
                WriteToLog("Проблеммы с применением файла конфигурации: " + ex.Message);
            }
        }
        
        public void StartServer()
        {

            PgStorage ServerStorage = new PgStorage();

            #region Блок особых действий
                sayani_kombik.DeleteDumpDirectory();

            #endregion

            //подключение к БД
            System.Data.ConnectionState conState = ServerStorage.Open(ConnectionString);

            if (conState == System.Data.ConnectionState.Broken)
            {
                MessageBox.Show("Невозможно подключиться к БД, проверьте строку подключения: " + ConnectionString, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            //чтение всех COM-портов
            ComPortSettings[] cps = ServerStorage.GetComportSettings();

            //чтение всех TCPIP-портов
            TCPIPSettings[] tcpips = ServerStorage.GetTCPIPSettings();

            bStopServer = false;

            //обработка всех неотлавливаемых исключений для логгирования
            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException_Handler);

            for (int i = 0; i < cps.Length; i++)
            {
                //если к порту привязаны приборы, то создаем для него поток и записываем в пул потоков
                Meter[] metersbyport = ServerStorage.GetMetersByComportGUID(cps[i].guid);
                if (metersbyport.Length > 0)
                {
                    Thread portThread = new Thread(new ParameterizedThreadStart(this.pollingPortThread));
                    portThread.IsBackground = true;
                    portThread.Start(cps[i]);

                    PortsThreads.Add(portThread);
                }
            }

            for (int i = 0; i < tcpips.Length; i++)
            {
                //если к порту привязаны приборы, то создаем для него поток и записываем в пул потоков
                Meter[] metersbyport = ServerStorage.GetMetersByTcpIPGUID(tcpips[i].guid);
                if (metersbyport.Length > 0)
                {
                    Thread portThread = new Thread(new ParameterizedThreadStart(this.pollingPortThread));
                    portThread.IsBackground = true;
                    portThread.Start(tcpips[i]);

                    PortsThreads.Add(portThread);
                }
            }

            object iLogsAreAliveDays = 6;
            //Thread logsEreaserThread = new Thread(new ParameterizedThreadStart(DeleteLogsDirectory));
            //logsEreaserThread.IsBackground = true;
            //logsEreaserThread.Start(iLogsAreAliveDays);

            //закрываем соединение с БД
            ServerStorage.Close();
        }

        //обработчик необработанных исключений
        void UnhandledException_Handler(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString());
        }
        public void StopServer()
        {
            bStopServer = true;
            Thread.Sleep(1000);

            for (int i = 0; i < PortsThreads.Count; i++)
            {
                try
                {
                    PortsThreads[i].Abort();
                }
                catch { }
            }
        }

        public void DeleteLogsDirectory(object param)
        {
            while (Thread.CurrentThread.IsAlive)
            {
                //Автоудаление логов
                try
                {
                    DirectoryInfo di = new DirectoryInfo(Logger.BaseDirectory);
                    if (di.Exists)
                    {
                        TimeSpan ts = DateTime.Now.Date - di.CreationTime.Date;
                        if (ts.TotalDays >= (int)param)
                        {
                            Logger.bRestrict = true;
                            Thread.Sleep(100);
                            Logger.DeleteDirectory(di.FullName);
                            Logger.bRestrict = false;
                        }
                            
                    }
                }
                catch (Exception ex)
                { }

                TimeSpan sleepSpan = new TimeSpan(1, 0, 0);
                Thread.Sleep(sleepSpan);
            }
        }

        private void pollingPortThread(object data)
        {
            Prizmer.Ports.VirtualPort m_vport = null;
            Meter[] metersbyport = null;

            Logger logger = new Logger();

            Guid PortGUID = Guid.Empty;

            //подключение к БД
            PgStorage ServerStorage = new PgStorage();

            System.Data.ConnectionState conState = ServerStorage.Open(ConnectionString);

            if (data.GetType().Name == "ComPortSettings")
            {
                ComPortSettings portsettings = (ComPortSettings)data;
                m_vport = new Prizmer.Ports.ComPort(byte.Parse(portsettings.name), (int)portsettings.baudrate, portsettings.data_bits, portsettings.parity, portsettings.stop_bits, portsettings.write_timeout, portsettings.read_timeout, (byte)portsettings.attempts);
                //читаем список приборов, привязанных к порту
                PortGUID = portsettings.guid;
                metersbyport = ServerStorage.GetMetersByComportGUID(PortGUID);
            }
            else if (data.GetType().Name == "TCPIPSettings")
            {
                TCPIPSettings portsettings = (TCPIPSettings)data;
                //m_vport = new Prizmer.Ports.TcpipPort(portsettings.ip_address, (int)portsettings.ip_port, portsettings.write_timeout, portsettings.read_timeout, 50);
                //читаем список приборов, привязанных к порту
                PortGUID = portsettings.guid;
                metersbyport = ServerStorage.GetMetersByTcpIPGUID(PortGUID);
            }

            //if (m_vport == null) goto CloseThreadPoint;
            if (metersbyport == null) goto CloseThreadPoint;
            if (metersbyport.Length == 0) goto CloseThreadPoint;
                        
            uint MetersCounter = 0;

            Hashtable MetersDriver = new Hashtable();

            while (!bStopServer)
            {
                //здесь надо выбрать - какой драйвер будет использоваться
                TypeMeter typemeter = ServerStorage.GetMetersTypeByGUID(metersbyport[MetersCounter].guid_types_meters);
                IMeter meter = null;

                switch (typemeter.driver_name)
                {
                    case "m230": meter = new m230(); break;
                    case "pulsar10": meter = new pulsar10(); break;
                    case "pulsar16": meter = new pulsar16(); break;
                    case "tem4": meter = new tem104(); break;
                    case "tem106": meter = new tem106(); break;
                    case "set4tm_03": meter = new set4tm_03(); break;
                    case "elf108": meter = new elf108(); break;
                    case "PulsarM": meter = new PulsarM(); break;
                    case "spg76212": meter = new spg76212(); break;
                    case "teplouchet1": meter = new teplouchet1(); break;
                    case "m200": meter = new Mercury200(); break;
                    case "opcretranslator": meter = new OpcRetranslator(); break;
                    case "sayani_kombik": meter = new sayani_kombik(); break;
                }

                if (meter == null)
                {
                    goto NetxMeter;
                }

                /*если соединяться с конечной точкой вначале, то консольная программа rds не сможет с ней соединиться
                 * поэтому создание пора и подключение к нему осуществляется на первой итерации цикла при условии,
                 * что счетчик не саяны. Предполагаются что на одном порту будут висеть только саяны, если будут другие приборы,
                 * создастся порт и саяны не будут читаться снова.
                 * */
                if (m_vport == null && (typemeter.driver_name != "sayani_kombik")) {
                    TCPIPSettings portsettings = (TCPIPSettings)data;
                    m_vport = new Prizmer.Ports.TcpipPort(portsettings.ip_address, (int)portsettings.ip_port, portsettings.write_timeout, portsettings.read_timeout, 50);
                }
                else if (m_vport == null && (typemeter.driver_name == "sayani_kombik"))
                {
                    m_vport = new Prizmer.Ports.ComPort(byte.Parse("250"), 1111, 1, 1, 1, 1, 1, 1);
                }
 

                //инициализация прибора
                meter.Init(metersbyport[MetersCounter].address, metersbyport[MetersCounter].password, m_vport);

                //инициализация логгера
                logger.Initialize(m_vport.GetName(), metersbyport[MetersCounter].address.ToString(), typemeter.driver_name, "main");

                //выведем в лог общие ошибки если таковые есть
                DateTime common_dt_install = metersbyport[MetersCounter].dt_install;
                DateTime common_dt_cur = DateTime.Now;

               // if (!meter.OpenLinkCanal())
                    //logger.LogWarn("Связь с прибором в начале цикла опроса не установлена. Значения могут быть искажены.");


                ////////////////////Блок чтения серийника///////////////////
                if (bStopServer) goto CloseThreadPoint;
                if (true)
                {
                    string serial_number = String.Empty;
                    if (meter.OpenLinkCanal())
                    {
                        Meter mDb = metersbyport[MetersCounter];
                        string isEqual = "";

                        if (meter.ReadSerialNumber(ref serial_number))
                        {
                            if (mDb.factory_number_manual == serial_number)
                                isEqual = "TRUE";
                            else
                                isEqual = "FALSE";

                            ServerStorage.UpdateMeterFactoryNumber(mDb.guid, serial_number, isEqual);
                        }
                        else
                        {
                            //ServerStorage.UpdateMeterFactoryNumber(mDb.guid, String.Empty, String.Empty);
                        }
                    }
                }

                if (bStopServer) goto CloseThreadPoint;
                if (true && pollingParams.b_poll_hour)
                {
                    #region ЧАСОВЫЕ СРЕЗЫ

                    const bool LOG_SLICES = false;
                    const bool LOG_HOURSLICES_ERRORS = true;
                    const bool SEL_DATE_REGION_LOGGING = false;

                    const byte SLICE_TYPE = 5;                         //тип значения в БД (получасовой/часовой)
                    const SlicePeriod SLICE_PERIOD = SlicePeriod.Hour;

                    if (meter.OpenLinkCanal())
                    {
                        string portStr = m_vport.GetName();
                        string mAddr = metersbyport[MetersCounter].address.ToString();
                        string mName = metersbyport[MetersCounter].name;
                        /* Цикл организуется для возможности немедленного прекращения выполнения 
                         * блока чтения срезов в случае ошибки*/
                        while (true)
                        {
                            //чтение 'дескрипторов' считываемых параметров указанного типа
                            TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid,
                                SLICE_TYPE);
                            if (takenparams.Length == 0) break;

                            #region Выбор дат, с которых необходимо читать каждый параметр, создание словаря вида 'Дата-Список параметров с этой датой'

                            if (common_dt_install.Ticks == 0)
                                logger.LogWarn("Дата установки прибора не задана, критично для ЧАСОВЫХ СРЕЗОВ");
                            if (common_dt_install > common_dt_cur)
                                logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для ЧАСОВЫХ СРЕЗОВ");

                            //дата установки счетчика
                            DateTime dt_install = metersbyport[MetersCounter].dt_install;
                            DateTime dt_cur = DateTime.Now;

                            //пусть дата начала = дата установки
                            DateTime date_from = dt_install;

                            if (dt_install > dt_cur)
                            {
                                break;
                            }

                            if (bStopServer) goto CloseThreadPoint;

                            //некоторые счетчики хранят дату инициализации архива (начала учета)
                            DateTime dt_last_slice_arr_init = new DateTime();
                            //получим дату последней инициализации массива срезов (если счетчик поддерживает)
                            //if (!meter.ReadSliceArrInitializationDate(ref dt_last_slice_arr_init))
                               // meter.WriteToLog("RSL: Дата инициализации архивов НЕ найдена", SEL_DATE_REGION_LOGGING);


                            //для каждого считываемого параметра определим дату начала и сопоставим дескриптору
                            //считываемого параметра
                            Dictionary<DateTime, List<TakenParams>> dt_param_dict = new Dictionary<DateTime, List<TakenParams>>();
                            for (int i = 0; i < takenparams.Length; i++)
                            {
                                string paramName = ServerStorage.GetParamByGUID(takenparams[i].guid_params).name;
                                if (bStopServer) goto CloseThreadPoint;

                                //получим последний (по дате) срез для читаемого параметра i
                                Value latestSliceVal = ServerStorage.GetLatestVariousValue(takenparams[i]);

                                Param p = ServerStorage.GetParamByGUID(takenparams[i].guid_params);
                                if (p.guid == Guid.Empty)
                                {
                                    //WriteToLog("RSL: Err2: ошибка считывания GUIDa параметра на итерации " + i, portStr, mAddr, LOG_HOURSLICES_ERRORS);
                                    continue;
                                }
                                else
                                {
                                    string msg = String.Format("RSL: Итерация {3}: Определение даты для параметра {0}; адрес {1}; канал {2}",
                                        p.name, p.param_address, p.channel, i);
                                    //meter.WriteToLog(msg, SEL_DATE_REGION_LOGGING);
                                }

                                if (latestSliceVal.dt.Ticks > 0)
                                {
                                   // meter.WriteToLog("RSL: В базе найден последний срез от: " + latestSliceVal.dt.ToString(), SEL_DATE_REGION_LOGGING);
                                    TimeSpan timeSpan = new TimeSpan(dt_cur.Ticks - latestSliceVal.dt.Ticks);

                                    if (timeSpan.TotalMinutes <= (int)SLICE_PERIOD)
                                    {
                                        //meter.WriteToLog("RSL: - Не прошло period минут с момента добавления среза, перехожу к следующему параметру", SEL_DATE_REGION_LOGGING);
                                        continue;
                                    }
                                }
                                else
                                {
                                    //meter.WriteToLog("RSL: Последний срез в базе НЕ найден", SEL_DATE_REGION_LOGGING);

                                    if (dt_last_slice_arr_init > date_from && dt_last_slice_arr_init < dt_cur)
                                    {
                                        logger.LogInfo("Часовые срезы: принял за начало дату инициализации архивов - " + dt_last_slice_arr_init.ToString());
                                        //meter.WriteToLog("RSL: Принял за начало дату инициализации архивов: " +
                                        //dt_last_slice_arr_init.ToString(), SEL_DATE_REGION_LOGGING);
                                        date_from = dt_last_slice_arr_init;
                                    }
                                }

                                //уточним начальную дату чтения срезов
                                if (latestSliceVal.dt > date_from && latestSliceVal.dt < dt_cur)
                                {
                                    date_from = latestSliceVal.dt.AddMinutes((double)SLICE_PERIOD);
                                    //meter.WriteToLog("RSL: Принял за начало дату ПОСЛЕДНЕГО СРЕЗА + 1 минута: " + date_from.ToString(), SEL_DATE_REGION_LOGGING);
                                }

                                if (date_from.Ticks == 0)
                                {
                                    if (dt_install.Ticks > 0)
                                        date_from = dt_install.Date;
                                    else
                                        date_from = dt_cur.Date.AddDays(-1);

                                    string s_log = String.Format("Часовые срезы: начальная дата '{0}' некорректна, параметр '{1}' прочитан не будет", 
                                        date_from.ToString(), paramName);
                                    logger.LogError(s_log);

                                    /*
                                    WriteToLog("RSL: Err3: Начальная дата НЕКОРРЕКТНА, срезы параметра прочитаны НЕ будут: " +
                                    date_from.ToString(), portStr, mAddr, LOG_HOURSLICES_ERRORS);
                                    */
                                    continue;
                                }
                                else
                                {
                                    //meter.WriteToLog("RSL: ЗА дату начала приняли:" + date_from.ToString(), SEL_DATE_REGION_LOGGING);
                                }

                                //добавим пару значений в словарь
                                if (dt_param_dict.ContainsKey(date_from))
                                {
                                    List<TakenParams> takenParamsList = new List<TakenParams>();
                                    

                                    if (!dt_param_dict.TryGetValue(date_from, out takenParamsList))
                                    {
                                        string s_log = String.Format("Часовые срезы: ключ {0} присутствует в словаре dt_param_dict, но TryGetValue возвращает false",
                                            date_from.ToString());
                                        logger.LogWarn(s_log);
                                    }

                                    dt_param_dict.Remove(date_from);
                                    takenParamsList.Add(takenparams[i]);
                                    dt_param_dict.Add(date_from, takenParamsList);
                                }
                                else
                                {
                                    List<TakenParams> takenParamsList = new List<TakenParams>();
                                    takenParamsList.Add(takenparams[i]);
                                    dt_param_dict.Add(date_from, takenParamsList);
                                }
                            }

                            if (dt_param_dict.Count == 0)
                            {
                                //WriteToLog("RSL: Err4: Словарь 'Дата-Дескриптор параметра' пуст. Срезы считаны не будут.", portStr, mAddr, LOG_HOURSLICES_ERRORS);

                                string s_log = String.Format("Часовые срезы: cловарь 'Дата-Дескриптор параметра' пуст. Часовые срезы прибора считаны НЕ будут");
                                logger.LogError(s_log);
                                
                                break;
                            }

                            #endregion

                            #region Подготовка дескрипторов параметров для передачи в драйвер

                            //создадим список дескрипторов срезов и заполним его дескрипторами параметров
                            List<SliceDescriptor> sliceDescrList = new List<SliceDescriptor>();

                            foreach (KeyValuePair<DateTime, List<TakenParams>> pair in dt_param_dict)
                            {
                                DateTime tmpDate = pair.Key;
                                List<TakenParams> tmpTpList = pair.Value;

                                SliceDescriptor sd = new SliceDescriptor(tmpDate);

                                foreach (TakenParams tp in tmpTpList)
                                {
                                    Param p = ServerStorage.GetParamByGUID(tp.guid_params);
                                    if (p.guid == Guid.Empty)
                                    {
                                      //  WriteToLog("RSL: Err: ошибка чтения GUIDa одного из параметров", portStr, mAddr,
                                      //  LOG_HOURSLICES_ERRORS);
                                        continue;
                                    }

                                    sd.AddValueDescriptor(tp.id, p.param_address, p.channel, SLICE_PERIOD);
                                }

                                sliceDescrList.Add(sd);
                            }

                            if (sliceDescrList.Count == 0)
                            {
                                string s_log = String.Format("Часовые срезы: список дескрипторов 'sliceDescrList' пуст. Часовые срезы прибора считаны НЕ будут");
                                logger.LogError(s_log);
                                break;
                            }

                            #endregion

                            #region Отправка дескрипторов счетчику и запись полученных значений в БД

                            //если срезы прочитаны успешно
                            if (meter.ReadPowerSlice(ref sliceDescrList, dt_cur, SLICE_PERIOD))
                            {
                                //meter.WriteToLog("RSL: Данные прочитаны, осталось занести в базу", LOG_SLICES);
                                foreach (SliceDescriptor su in sliceDescrList)
                                {
                                    if (bStopServer) goto CloseThreadPoint;

                                    for (uint i = 0; i < su.ValuesCount; i++)
                                    {
                                        try
                                        {
                                            Value val = new Value();
                                            su.GetValueId(i, ref val.id_taken_params);
                                            su.GetValue(i, ref val.value, ref val.status);
                                            val.dt = su.Date;

                                            /*добавим в БД "разное" значение и обновим dt_last_read*/
                                            ServerStorage.AddVariousValues(val);
                                            ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);
                                        }
                                        catch (Exception ex)
                                        {
                                            string s_log = String.Format("Часовые срезы: ошибка при обработке прочитанного среза. Содержание исключения: {0}", ex.Message);
                                            logger.LogError(s_log);
                                            //WriteToLog("RSL: Err6: Ошибка перегрупировки параметров 1: " + ex.Message + " срез " + i + " считан не будет.",
                                            //     portStr, mAddr, LOG_HOURSLICES_ERRORS);
                                            continue;
                                        }
                                    }
                                }
                                //meter.WriteToLog("RSL: Данные успешно занесены в БД", LOG_SLICES);
                                //meter.WriteToLog("RSL: ---/ конец чтения срезов /---", LOG_SLICES);
                            }
                            else
                            {
                                try{
                                    string s_log = String.Format("Часовые срезы: метод драйвера ReadPowerSlice вернул false. Аргументы: длина списка дескрипторов {0}, " +
                                        "дата начала {1}, период чтения {2}", sliceDescrList.Count, dt_cur.ToString(), SLICE_PERIOD.ToString());
                                    logger.LogError(s_log);
                                }catch (Exception ex){
                                    string s_log = String.Format("Часовые срезы: метод драйвера ReadPowerSlice вернул false. Исключение: {0}",
                                        ex.Message);
                                    logger.LogError(s_log);
                                }
                                //WriteToLog("RSL: Err7: драйвер не может прочитать срезы", portStr, mAddr, LOG_HOURSLICES_ERRORS);
                                //meter.WriteToLog("RSL: ---/ конец чтения срезов /---", LOG_SLICES);
                            }

                            #endregion

                            break;
                        }
                    }
                    else
                    {
                        //ошибка Связь неустановлена
                    }
                    #endregion
                }

                if (bStopServer) goto CloseThreadPoint;
                if (pollingParams.b_poll_halfanhour)
                {
                    if (typemeter.driver_name == "set4tm_03" || typemeter.driver_name == "m230")
                    {
                        #region ПОЛУЧАСОВЫЕ СРЕЗЫ (СТАРАЯ ВЕРСИЯ)

                        const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
                        const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов

                        //чтение получасовых срезов, подлежащих чтению, относящихся к конкретному прибору
                        TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid,
                            SLICE_PER_HALF_AN_HOUR_TYPE);

                        if (takenparams.Length > 0)
                        {
                            //читать данные только если прибор ответил
                            if (meter.OpenLinkCanal())
                            {
                                const bool WRITE_LOG = true;
                                meter.WriteToLog("RSL: 1. Открыт канал для чтения получасовок", WRITE_LOG);

                                if (common_dt_install.Ticks == 0)
                                {
                                    logger.LogWarn("Дата установки прибора не задана, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");
                                    
                                }

                                if (common_dt_install > common_dt_cur)
                                    logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");

                                //дата установки счетчика
                                DateTime dt_install = metersbyport[MetersCounter].dt_install;
                                if (dt_install == null || dt_install.Ticks == 0)
                                    dt_install = DateTime.Now.Date.AddDays(-2);

                                DateTime dt_cur = DateTime.Now;
                                DateTime dt_last_slice_arr_init = new DateTime();

                                //пусть дата начала = дата установки
                                DateTime date_from = dt_install;

                                for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                                {
                                    List<RecordPowerSlice> lrps = new List<RecordPowerSlice>();
                                    meter.WriteToLog("RSL: 2. Вошли в цикл, итерация" + tpindex.ToString(), WRITE_LOG);

                                    if (dt_install > dt_cur)
                                    {
                                        meter.WriteToLog("RSL: 3. Дата установки не может быть больше текущей: " +
                                            dt_install.ToString(), WRITE_LOG);
                                        break;
                                    }
                                    meter.WriteToLog("RSL: 3. Дата установки корректна: " + dt_install.ToString(), WRITE_LOG);

                                    if (bStopServer) goto CloseThreadPoint;

                                    //получим последний (по дате) срез из БД
                                    Value latestSliceVal = ServerStorage.GetLatestVariousValue(takenparams[tpindex]);

                                    if (latestSliceVal.dt.Ticks > 0)
                                    {
                                        meter.WriteToLog("RSL: 4. В базе найден последний срез от: " + latestSliceVal.dt.ToString(), WRITE_LOG);
                                        TimeSpan timeSpan = new TimeSpan(dt_cur.Ticks - latestSliceVal.dt.Ticks);


                                        if (timeSpan.TotalMinutes < SLICE_PER_HALF_AN_HOUR_PERIOD)
                                        {
                                            meter.WriteToLog("RSL: 4.1. Не прошло 30 минут с момента добавления среза, выхожу из цикла", WRITE_LOG);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        meter.WriteToLog("RSL: 4. Последний срез в базе НЕ найден", WRITE_LOG);
                                        //получим дату последней инициализации массива срезов
                                        if (meter.ReadSliceArrInitializationDate(ref dt_last_slice_arr_init))
                                        {
                                            if (dt_last_slice_arr_init > date_from && dt_last_slice_arr_init < dt_cur)
                                            {
                                                meter.WriteToLog("RSL: 5. Принял за начало дату инициализации: " +
                                                dt_last_slice_arr_init.ToString(), WRITE_LOG);
                                                date_from = dt_last_slice_arr_init;
                                            }
                                        }
                                        else
                                        {
                                            meter.WriteToLog("RSL: 5. Дата инициализации НЕ найдена", WRITE_LOG);
                                        }
                                    }

                                    //прочие проверки
                                    Param param = ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                                    if (param.guid == Guid.Empty) continue;

                                    meter.WriteToLog("RSL: 6. Параметру присвоен GUID, параметр:" + param.name, WRITE_LOG);


                                    //уточним начальную дату чтения срезов
                                    if (latestSliceVal.dt > date_from && latestSliceVal.dt < dt_cur)
                                    {
                                        date_from = latestSliceVal.dt.AddMinutes(1);
                                        meter.WriteToLog("RSL: 7. Принял за начало дату ПОСЛЕДНЕГО СРЕЗА: " +
                                        date_from.ToString(), WRITE_LOG);
                                    }

                                    if (date_from.Ticks == 0)
                                    {
                                        meter.WriteToLog("RSL: 8. Начальная дата НЕКОРРЕКТНА, срезы прочитаны НЕ будут: " +
                                        date_from.ToString());
                                    }
                                    else
                                    {
                                        meter.WriteToLog("RSL: 8. ЗА дату начала приняли:" + date_from.ToString(), WRITE_LOG);
                                        meter.WriteToLog("        ЗА дату конца приняли:" + dt_cur.ToString(), WRITE_LOG);
                                    }

                                    //если срезы из указанного диапазона дат прочитаны успешно
                                    if (meter.ReadPowerSlice(date_from, dt_cur, ref lrps, SLICE_PER_HALF_AN_HOUR_PERIOD))
                                    {
                                        meter.WriteToLog("RSL: 9. Данные прочитаны, осталось занести в базу", WRITE_LOG);
                                        foreach (RecordPowerSlice rps in lrps)
                                        {
                                            if (bStopServer) goto CloseThreadPoint;

                                            Value val = new Value();
                                            val.dt = rps.date_time;
                                            val.id_taken_params = takenparams[tpindex].id;
                                            val.status = Convert.ToBoolean(rps.status);

                                            switch (param.param_address)
                                            {
                                                case 0: { val.value = rps.APlus; break; }
                                                case 1: { val.value = rps.AMinus; break; }
                                                case 2: { val.value = rps.RPlus; break; }
                                                case 3: { val.value = rps.RMinus; break; }
                                                default:
                                                    {
                                                        continue;
                                                        //meter.WriteToLog("Значения среза по каналу {0}", param.channel);
                                                    }
                                            }

                                            /*добавим в БД "настраивоемое" значение и обновим dt_last_read*/
                                            ServerStorage.AddVariousValues(val);
                                            ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);
                                        }

                                    }
                                    meter.WriteToLog("RSL: 10. Данные успешно занесены в БД", WRITE_LOG);
                                }
                            }
                            else
                            {
                                //meter.WriteToLog("Дата, с которой планируется читать срезы мощности не может быть больше текущей даты");
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        #region ПОЛУЧАСОВЫЕ СРЕЗЫ

                        const byte SLICE_TYPE = 4;                         //тип значения в БД (получасовой/часовой)
                        const SlicePeriod SLICE_PERIOD = SlicePeriod.HalfAnHour;

                        if (meter.OpenLinkCanal())
                        {
                            /* Цикл организуется для возможности немедленного прекращения выполнения 
                             * блока чтения срезов в случае ошибки*/
                            while (true)
                            {
                                //чтение 'дескрипторов' считываемых параметров указанного типа
                                TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid,
                                    SLICE_TYPE);
                                if (takenparams.Length == 0) break;

                                if (common_dt_install.Ticks == 0)
                                    logger.LogWarn("Дата установки прибора не задана, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");
                                if (common_dt_install > common_dt_cur)
                                    logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");


                                string msg = String.Format("ПОЛУчасовые срезы: к считыванию подлежит {0} параметров", takenparams.Length);
                                logger.LogInfo(msg);

                                #region Выбор дат, с которых необходимо читать каждый параметр, создание словаря вида 'Дата-Список параметров с этой датой'

                                //дата установки счетчика
                                DateTime dt_install = metersbyport[MetersCounter].dt_install;
                                DateTime dt_cur = DateTime.Now;

                                //пусть дата начала = дата установки
                                DateTime date_from = dt_install;

                                if (dt_install > dt_cur)
                                {
                                    msg = String.Format("ПОЛУчасовые срезы: дата установки прибора ({0}) не может быть больше текущей", dt_install.ToString());
                                    logger.LogError(msg);
                                    break;
                                }

                                if (bStopServer) goto CloseThreadPoint;

                                //некоторые счетчики хранят дату инициализации архива (начала учета)
                                DateTime dt_last_slice_arr_init = new DateTime();
                                //получим дату последней инициализации массива срезов (если счетчик поддерживает)
                                if (meter.ReadSliceArrInitializationDate(ref dt_last_slice_arr_init))
                                {
                                    msg = String.Format("ПОЛУчасовые срезы: определена дата инициализации архива ({0})",
                                        dt_last_slice_arr_init.ToString());
                                    logger.LogInfo(msg);
                                }

                                //для каждого считываемого параметра определим дату начала и сопоставим дескриптору
                                //считываемого параметра
                                Dictionary<DateTime, List<TakenParams>> dt_param_dict = new Dictionary<DateTime, List<TakenParams>>();
                                for (int i = 0; i < takenparams.Length; i++)
                                {
                                    if (bStopServer) goto CloseThreadPoint;

                                    //получим последний (по дате) срез для читаемого параметра i
                                    Value latestSliceVal = ServerStorage.GetLatestVariousValue(takenparams[i]);

                                    Param p = ServerStorage.GetParamByGUID(takenparams[i].guid_params);
                                    if (p.guid == Guid.Empty)
                                    {
                                        msg = String.Format("ПОЛУчасовые срезы: ошибка считывания GUIDa параметра {0} из {1} считываемых, параметр: {2}",
                                            i, takenparams.Length, p.name);
                                        logger.LogError(msg);
                                        continue;
                                    }
                                    else
                                    {
                                        //string msg = String.Format("RSL: Итерация {3}: Определение даты для параметра {0}; адрес {1}; канал {2}", p.name, p.param_address, p.channel, i);
                                        //meter.WriteToLog(msg, SEL_DATE_REGION_LOGGING);
                                    }

                                    if (latestSliceVal.dt.Ticks > 0)
                                    {
                                        //meter.WriteToLog("RSL: В базе найден последний срез от: " + latestSliceVal.dt.ToString(), SEL_DATE_REGION_LOGGING);
                                        TimeSpan timeSpan = new TimeSpan(dt_cur.Ticks - latestSliceVal.dt.Ticks);

                                        if (timeSpan.TotalMinutes <= (int)SLICE_PERIOD)
                                        {
                                            msg = String.Format("ПОЛУчасовые срезы: Не прошло {0} минут с момента добавления среза {1}, перехожу к следующему параметру",
                                               (int)SLICE_PERIOD, latestSliceVal.dt);
                                            logger.LogInfo(msg);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        //meter.WriteToLog("RSL: Последний срез в базе НЕ найден", SEL_DATE_REGION_LOGGING);
                                        msg = String.Format("ПОЛУчасовые срезы: последний срез в базе не найден");
                                        logger.LogInfo(msg);

                                        if (dt_last_slice_arr_init > date_from && dt_last_slice_arr_init < dt_cur)
                                        {
                                            msg = String.Format("ПОЛУчасовые срезы: дата инициализации архивов ({0}) принята за дату начала",
                                                dt_last_slice_arr_init.ToString());
                                            logger.LogInfo(msg);

                                            date_from = dt_last_slice_arr_init;
                                        }
                                    }

                                    //уточним начальную дату чтения срезов
                                    if (latestSliceVal.dt > date_from && latestSliceVal.dt < dt_cur)
                                    {
                                        date_from = latestSliceVal.dt.AddMinutes((double)SLICE_PERIOD);
                                        //meter.WriteToLog("RSL: Принял за начало дату ПОСЛЕДНЕГО СРЕЗА + 1 минута: " + date_from.ToString(), SEL_DATE_REGION_LOGGING);
                                    }

                                    if (date_from.Ticks == 0)
                                    {
                                        msg = String.Format("ПОЛУчасовые срезы: начальная дата ({0}) НЕКОРРЕКТНА, срезы параметра прочитаны НЕ будут",
                                            date_from.ToString());
                                        logger.LogError(msg);
                                        continue;
                                    }
                                    else
                                    {
                                        msg = String.Format("ПОЛУчасовые срезы: начальная дата ({0})", date_from.ToString());
                                        logger.LogInfo(msg);
                                    }

                                    //добавим пару значений в словарь
                                    if (dt_param_dict.ContainsKey(date_from))
                                    {
                                        List<TakenParams> takenParamsList = null;
                                        if (!dt_param_dict.TryGetValue(date_from, out takenParamsList))
                                        {

                                        }

                                        dt_param_dict.Remove(date_from);
                                        takenParamsList.Add(takenparams[i]);
                                        dt_param_dict.Add(date_from, takenParamsList);
                                    }
                                    else
                                    {
                                        List<TakenParams> takenParamsList = new List<TakenParams>();
                                        takenParamsList.Add(takenparams[i]);
                                        dt_param_dict.Add(date_from, takenParamsList);
                                    }
                                }

                                if (dt_param_dict.Count == 0)
                                {
                                    msg = String.Format("ПОЛУчасовые срезы: cловарь 'Дата-Дескриптор параметра' пуст. Срезы прочитаны не будут");
                                    logger.LogError(msg);
                                    break;
                                }

                                #endregion

                                #region Подготовка дескрипторов параметров для передачи в драйвер

                                //создадим список дескрипторов срезов и заполним его дескрипторами параметров
                                List<SliceDescriptor> sliceDescrList = new List<SliceDescriptor>();

                                foreach (KeyValuePair<DateTime, List<TakenParams>> pair in dt_param_dict)
                                {
                                    DateTime tmpDate = pair.Key;
                                    List<TakenParams> tmpTpList = pair.Value;

                                    SliceDescriptor sd = new SliceDescriptor(tmpDate);

                                    foreach (TakenParams tp in tmpTpList)
                                    {
                                        Param p = ServerStorage.GetParamByGUID(tp.guid_params);
                                        if (p.guid == Guid.Empty)
                                        {
                                            msg = String.Format("ПОЛУчасовые срезы: ошибка считывания GUIDa одного из параметров");
                                            logger.LogError(msg);
                                            continue;
                                        }

                                        sd.AddValueDescriptor(tp.id, p.param_address, p.channel, SLICE_PERIOD);
                                    }

                                    sliceDescrList.Add(sd);
                                }

                                #endregion

                                #region Отправка дескрипторов счетчику и запись полученных значений в БД

                                //если срезы прочитаны успешно
                                if (meter.ReadPowerSlice(ref sliceDescrList, dt_cur, SLICE_PERIOD))
                                {
                                    //meter.WriteToLog("RSL: Данные прочитаны, осталось занести в базу", LOG_SLICES);
                                    foreach (SliceDescriptor su in sliceDescrList)
                                    {
                                        if (bStopServer) goto CloseThreadPoint;

                                        for (uint i = 0; i < su.ValuesCount; i++)
                                        {
                                            try
                                            {
                                                Value val = new Value();
                                                su.GetValueId(i, ref val.id_taken_params);
                                                su.GetValue(i, ref val.value, ref val.status);
                                                val.dt = su.Date;


                                                /*добавим в БД "разное" значение и обновим dt_last_read*/
                                                ServerStorage.AddVariousValues(val);
                                                ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);
                                            }
                                            catch (Exception ex)
                                            {
                                                msg = String.Format("ПОЛУчасовые срезы: ошибка перегрупировки параметров, срез ({0}) считан не будет; текст исключения: {1}",
                                                    i, ex.Message);
                                                logger.LogError(msg);
                                                continue;
                                            }
                                        }
                                    }
                                    //meter.WriteToLog("RSL: Данные успешно занесены в БД", LOG_SLICES);
                                    //meter.WriteToLog("RSL: ---/ конец чтения срезов /---", LOG_SLICES);
                                }
                                else
                                {
                                    msg = String.Format("ПОЛУчасовые срезы: метод драйвера ReadPowerSlice(ref sliceDescrList, dt_cur, SLICE_PERIOD) вернул false, срезы не прочитаны");
                                    logger.LogError(msg);
                                }

                                #endregion

                                break;
                            }
                        }
                        else
                        {
                            //ошибка Связь неустановлена
                        }
                        #endregion
                    }

                    if (bStopServer) goto CloseThreadPoint;
                }

                
                ////////////////////ТЕКУЩИЕ///////////////////
                if (bStopServer) goto CloseThreadPoint;
                if (true && pollingParams.b_poll_current)
                {
                    #region ТЕКУЩИЕ ЗНАЧЕНИЯ
                    //чтение текущих параметров, подлежащих чтению, относящихся к конкретному прибору
                    TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid, 0);

                    if (takenparams.Length > 0)
                    {
                        //читать данные только если прибор ответил
                        if (meter.OpenLinkCanal())
                        {
                            for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                            {
                                if (bStopServer) goto CloseThreadPoint;

                                Param param = ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                                if (param.guid == Guid.Empty) continue;


                                //RecordValueEnergy rve = new RecordValueEnergy();

                                float curvalue = 0;

                                //чтение текущих параметров
                                if (meter.ReadCurrentValues(param.param_address, param.channel, ref curvalue))
                                {
                                    Value value = new Value();
                                    value.dt = DateTime.Now;
                                    value.id_taken_params = takenparams[tpindex].id;
                                    value.status = false;
                                    value.value = curvalue;
                                    ServerStorage.AddCurrentValues(value);
                                    ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);
                                }
                                else
                                {
                                    string s_log = String.Format("Текущие: метод драйвера ReadCurrentValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан",
                                        param.name, param.param_address, param.channel);
                                    logger.LogError(s_log);
                                    //meter.WriteToLog("текущий параметр не прочитан:" + param.param_address.ToString());
                                }
                            }
                        }
                        else
                        {
                            //meter.WriteToLog("Текущие: ошибка cвязи с прибором ");
                        }
                    }
                    #endregion
                }
                
                ////////////////////на начало СУТОК///////////////////
                if (bStopServer) goto CloseThreadPoint;
                if (true && pollingParams.b_poll_day)
                {
                    #region НА НАЧАЛО СУТОК
                    DateTime CurTime = DateTime.Now; CurTime.AddHours(-1);
                    DateTime PrevTime = CurTime;

                    const bool LOG_DAILY = false;
                    const bool LOG_DAILY_ERRORS = true;

                    //чтение текущих параметров, подлежащих чтению, относящихся к конкретному прибору
                    TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid, 1);
                    if (takenparams.Length > 0)
                    {
                        string portStr = m_vport.GetName();
                        string mAddr = metersbyport[MetersCounter].address.ToString();
                        string mName = metersbyport[MetersCounter].name;

                        for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                        {
                            if (bStopServer) goto CloseThreadPoint;

                            Value[] lastvalue = ServerStorage.GetExistsDailyValuesDT(takenparams[tpindex], PrevTime, CurTime);
                            //если значение в БД уже есть, то не читать его из прибора
                            if (lastvalue.Length > 0) continue;
                            //WriteToLog(mName + " - готов прочитать " + takenparams.Length.ToString() + " СУТОЧНЫХ параметров", portStr, mAddr, LOG_DAILY);
                            //читать данные только если прибор ответил
                           if (meter.OpenLinkCanal())
                            {
                                WriteToLog("Канал для " + mName + " порт " + m_vport.ToString() + " адрес " + metersbyport[MetersCounter].address.ToString() + " открыт");

                                Param param = ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                                if (param.guid == Guid.Empty) continue;

                                //RecordValueEnergy rve = new RecordValueEnergy();

                                float curvalue = 0;
                              // WriteToLog(mName + " - СУТОЧНЫЕ: читаю параметр (" +
                                 //  tpindex.ToString() + "): " + param.name, portStr, mAddr, LOG_DAILY);

                                //чтение суточных параметров
                                if (meter.ReadDailyValues(DateTime.Now, param.param_address, param.channel, ref curvalue))
                                {
                                    Value value = new Value();
                                    value.dt = DateTime.Now;
                                    value.id_taken_params = takenparams[tpindex].id;
                                    value.status = false;
                                    value.value = curvalue;
                                    ServerStorage.AddDailyValues(value);
                                    ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);

                               //     WriteToLog("Addr: " + metersbyport[MetersCounter].address.ToString() + "; параметр (" +
                                    //    tpindex.ToString() + ") записан в базу", portStr, mAddr, LOG_DAILY);
                                }
                                else
                                {
                                    string s_log = String.Format("Суточные: метод драйвера ReadDailyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан",
                                        param.name, param.param_address, param.channel);
                                    logger.LogWarn(s_log);
                                  // WriteToLog("Addr: " + metersbyport[MetersCounter].address.ToString() + "; параметр (" + tpindex.ToString() + ") не записан", portStr, mAddr, LOG_DAILY_ERRORS);
                                }
                            }
                            else
                            {
                              //  WriteToLog(mName + " порт " + m_vport.ToString() + " адрес " + metersbyport[MetersCounter].address.ToString() + " невозможно открыть канал связи", portStr, mAddr, LOG_DAILY_ERRORS);
                            }
                        }

                    }
                #endregion
                }

                ////////////////////на начало МЕСЯЦА///////////////////
                if (bStopServer) goto CloseThreadPoint;
                if (true && pollingParams.b_poll_month)
                {
                    #region НА НАЧАЛО МЕСЯЦА
                    DateTime CurTime = DateTime.Now; 
                    DateTime PrevTime = new DateTime(CurTime.Year, CurTime.Month, 1);
                    DateTime tmpDate;


                    //чтение месячных параметров, подлежащих чтению, относящихся к конкретному прибору
                    TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid, 2);

                    if (takenparams.Length > 0)
                    {
                            for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                            {
                                if (bStopServer) goto CloseThreadPoint;

                                //организация вычитки за 2 предыдущих месяца
                                for (int m = 2; m >= 0; m--)
                                {
                                    tmpDate = PrevTime.AddMonths(-m);

                                    Value[] lastvalue = ServerStorage.GetExistsMonthlyValuesDT(takenparams[tpindex], tmpDate, tmpDate);
                                    //если значение в БД уже есть, то не читать его из прибора
                                    if (lastvalue.Length > 0) continue;

                                    //читать данные только если прибор ответил
                                    if (meter.OpenLinkCanal())
                                    {

                                        Param param = ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                                        if (param.guid == Guid.Empty) continue;

                                        //RecordValueEnergy rve = new RecordValueEnergy();

                                        float curvalue = 0;

                                        //чтение месячных параметров
                                        if (meter.ReadMonthlyValues(tmpDate, param.param_address, param.channel, ref curvalue))
                                        {
                                            Value value = new Value();
                                            value.dt = new DateTime(tmpDate.Year, tmpDate.Month, 1);
                                            value.id_taken_params = takenparams[tpindex].id;
                                            value.status = false;
                                            value.value = curvalue;
                                            value.value = (float)Math.Round(value.value, 4, MidpointRounding.AwayFromZero);
                                            ServerStorage.AddMonthlyValues(value);
                                            ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);
                                        }
                                        else
                                        {
                                            string s_log = String.Format("На начало месяца: метод драйвера ReadMonthlyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан. Запрашиваемая дата: {3}",
                                                param.name, param.param_address, param.channel, tmpDate.ToString());
                                            logger.LogError(s_log);
                                            //meter.WriteToLog("текущий параметр не прочитан:" + param.param_address.ToString());
                                        }
                                    }
                                    else
                                    {
                                        //meter.WriteToLog("ошибка cвязи с прибором");
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                ////////////////////АРХИВЫ///////////////////
                if (bStopServer) goto CloseThreadPoint;
                if (false && pollingParams.b_poll_archive)
                {
                    #region АРХИВНЫЕ ДАННЫЕ СТАРЫЙ МЕТОД

                    DateTime cur_date = DateTime.Now.Date;
                    DateTime dt_install = metersbyport[MetersCounter].dt_install.Date;

                    //чтение архивных параметров, подлежащих чтению, относящихся к конкретному прибору
                    TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid, 3);
                    if (takenparams.Length > 0)
                    {
                        if (common_dt_install.Ticks == 0)
                            logger.LogWarn("Дата установки прибора не задана, критично для АРХИВНЫХ ПАРАМЕТРОВ");
                        if (common_dt_install > common_dt_cur)
                            logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для АРХИВНЫХ ПАРАМЕТРОВ");

                        for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                        {
                            if (bStopServer) goto CloseThreadPoint;

                            //получим все записи в интервале от даты установки (если нет, от начала НЭ) до текущего момента
                            Value[] valueArr = ServerStorage.GetExistsDailyValuesDT(takenparams[tpindex], dt_install, cur_date);

                            //пусть по умолчанию, читаются данные за двое предыдущих суток
                            DateTime fromDate = DateTime.Now.Date.AddDays(-2);

                            //если задано dt_install, то используем его
                            if (dt_install.Date != new DateTime(0).Date)
                                fromDate = dt_install.Date;

                            //если в базе найдено суточное показание и оно не за сегодня, прибавим день и примем дату за начальную
                            if (valueArr.Length > 0)
                            {
                                fromDate = valueArr[valueArr.Length - 1].dt.Date;
                                if (fromDate.Date == cur_date.Date)
                                    continue;
                                else
                                    fromDate.AddDays(1);
                            }

                            TimeSpan diff = cur_date.Date - fromDate.Date;

                            //читать данные только если прибор ответил
                            if (meter.OpenLinkCanal())
                            {
                                float curValue = 0;

                                Param param = ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                                if (param.guid == Guid.Empty) continue;

                                for (int i = 0; i <= diff.TotalDays; i++)
                                {
                                    int cnt = 0;
                                READAGAIN:
                                    if (meter.ReadDailyValues(fromDate, param.param_address, param.channel, ref curValue))
                                    {
                                        Value value = new Value();
                                        value.dt = fromDate;
                                        value.id_taken_params = takenparams[tpindex].id;
                                        value.status = false;
                                        value.value = curValue;
                                        ServerStorage.AddDailyValues(value);
                                        ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);
                                        //meter.WriteToLog("Арх: записал в базу " + value.value.ToString());
                                    }
                                    else
                                    {
                                        string s_log = String.Format("Архивные: попытка {4}, метод драйвера ReadDailyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан. Запрашиваемая дата: {3}",
                                            param.name, param.param_address, param.channel, fromDate.ToString(), cnt);
                                        logger.LogError(s_log);

                                        if (cnt < 2)
                                        {
                                            cnt++;
                                            goto READAGAIN;
                                        }
                                    }

                                    fromDate = fromDate.AddDays(1);
                                }
                            }
                            else
                            {
                                //meter.WriteToLog("ошибка cвязи с прибором");
                            }

                        }

                    }
                    #endregion
                }

                ////////////////////АРХИВЫ_НОВЫЙ_АКТУАЛЬНЫЙ_АЛГ///////////////////
                if (bStopServer) goto CloseThreadPoint;
                if (true && pollingParams.b_poll_archive)
                {
                    #region АРХИВНЫЕ ДАННЫЕ НОВ
                    bool doArchLog = true;

                    DateTime cur_date = new DateTime(DateTime.Now.Date.Ticks);
                  
                    //если дата установки отсутствует, считаем что счетчик установлен сегодня
                    DateTime dt_install = new DateTime();
                    dt_install = metersbyport[MetersCounter].dt_install.Date.Ticks == 0 ? new DateTime(0).Date : metersbyport[MetersCounter].dt_install.Date;

                    //чтение архивных параметров, подлежащих чтению, относящихся к конкретному прибору
                    TakenParams[] takenparams = ServerStorage.GetTakenParamByMetersGUIDandParamsType(metersbyport[MetersCounter].guid, 3);
                    if (takenparams.Length > 0)
                    {
                        if (common_dt_install.Ticks == 0)
                            logger.LogWarn("Дата установки прибора не задана, критично для АРХИВНЫХ ПАРАМЕТРОВ");
                        if (common_dt_install > common_dt_cur)
                            logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для АРХИВНЫХ ПАРАМЕТРОВ");

                        for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                        {
                            if (doArchLog) logger.LogInfo("Архивные: параметр: " + tpindex.ToString()); 
                            if (bStopServer) goto CloseThreadPoint;
                            Param param = ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                            if (param.guid == Guid.Empty) continue;

                            //пусть по умолчанию, читаются данные за двое предыдущих суток
                            DateTime fromDate = DateTime.Now.Date.AddDays(-2);

                            //если задана реальная dt_install, то используем ее
                            if (dt_install.Date != new DateTime(0).Date)
                            {          
                                TimeSpan ts1 = DateTime.Now.Date - dt_install.Date;
                                if (ts1.Days < 31) fromDate = dt_install.Date;
                                else fromDate = DateTime.Now.Date.AddDays(-31);
                            }

                            if (doArchLog) logger.LogInfo("Архивные: дата начала: " + fromDate.ToString()); 
                            TimeSpan diff = DateTime.Now.Date - fromDate.Date;
                            if (doArchLog) logger.LogInfo("Архивные: разница в днях между тек. и нач. датами: " + diff.TotalDays.ToString()); 
                            //читать данные только если прибор ответил
                            if (meter.OpenLinkCanal())
                            {
                                float curValue = 0;

                                DateTime tmpDT = new DateTime(fromDate.Ticks);
                                for (int i = 0; i <= diff.TotalDays; i++)
                                {
                                    if (doArchLog) logger.LogInfo(String.Format("Архивные: день: {0}; дата: {1};", i, tmpDT.ToString())); 
                                    int cnt = 0;
                                    //получим все записи в интервале от даты установки (если нет, от начала НЭ) до текущего 
                                    Value[] valueArr = ServerStorage.GetExistsDailyValuesDT(takenparams[tpindex], tmpDT, tmpDT);
                                    //если в базе найдено суточное показание продолжим
                                    if (valueArr.Length > 0)
                                    {
                                        if (doArchLog) logger.LogInfo(String.Format("Архивные: в базе есть показание на эту дату: {0}; дата: {1};", valueArr[0].value.ToString(), valueArr[0].dt.ToString()));
                                        tmpDT = tmpDT.AddDays(1);
                                        continue;
                                    }
                                        

                                READAGAIN:
                                    if (meter.ReadDailyValues(tmpDT, param.param_address, param.channel, ref curValue))
                                    {
                                        Value value = new Value();
                                        value.dt = tmpDT;
                                        value.id_taken_params = takenparams[tpindex].id;
                                        value.status = false;
                                        value.value = curValue;
                                        value.value = (float)Math.Round(value.value, 2, MidpointRounding.AwayFromZero);
                                        ServerStorage.AddDailyValues(value);
                                        ServerStorage.UpdateMeterLastRead(metersbyport[MetersCounter].guid, DateTime.Now);
                                        //meter.WriteToLog("Арх: записал в базу " + value.value.ToString());
                                    }
                                    else
                                    {
                                        string s_log = String.Format("Архивные: попытка {4}, метод драйвера ReadDailyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан. Запрашиваемая дата: {3}",
                                            param.name, param.param_address, param.channel, tmpDT.ToString(), cnt);
                                        logger.LogError(s_log);

                                        if (cnt < 2)
                                        {
                                            cnt++;
                                            goto READAGAIN;
                                        }
                                    }

                                    tmpDT = tmpDT.AddDays(1);
                                }
                            }
                        }
                    }
                    #endregion
                }

            NetxMeter:

                MetersCounter++;
                if (MetersCounter >= metersbyport.Length)
                {
                    MetersCounter = 0;
                    //перечитать список приборов - вдруг что-то добавили или убрали
                    if (data.GetType().Name == "ComPortSettings")
                    {
                        metersbyport = ServerStorage.GetMetersByComportGUID(PortGUID);
                    }
                    else if (data.GetType().Name == "TCPIPSettings")
                    {
                        metersbyport = ServerStorage.GetMetersByTcpIPGUID(PortGUID);
                    }

                    //if (m_vport.GetConnectionType() == "tcp")
                    //    m_vport.ReInitialize();
                }

                Thread.Sleep(1000);
            }

            //закрываем соединение с БД
            CloseThreadPoint:
            ServerStorage.Close();
        }
    }
}
