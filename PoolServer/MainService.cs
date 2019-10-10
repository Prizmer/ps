using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using System.IO;

using Prizmer.PoolServer.DataBase;

//временное решение до перевода всех драйверов в DLL
using Prizmer.Meters;
//using Prizmer.Meters.iMeters;
//using Prizmer.Ports;


using PollingLibraries.LibLogger;
using PollingLibraries.LibPorts;

using Drivers;
using Drivers.LibMeter;
using Drivers.PulsarDriver;
using Drivers.ElfApatorDriver;
using Drivers.UMDriver;
using Drivers.Mercury23XDriver;
using Drivers.Karat30XDriver;
using Drivers.KaratDanfosDriver;
using Drivers.Mercury200Driver;


namespace Prizmer.PoolServer
{
    public class MainService
    {
        Helper helper = new Helper();
        Type resultEnumType = typeof(PollingResultStatus);


        public delegate void MyEventHandler(object sender, MyEventArgs e);
        public event MyEventHandler pollingStarted;
        public event MyEventHandler pollingEnded;
        public event MyEventHandler meterPolled;


        public event MyEventHandler stoppingStarted;
        public event MyEventHandler stoppingEnded;

        List<Thread> PortsThreads = new List<Thread>();
        List<TcpipPort> tcpPortsGlobal = new List<TcpipPort>();

        public void WriteToLog(string str, string port = "", string addr = "", string mName = "", bool doWrite = true)
        {
            StreamWriter sw = null;
            string resMsg = String.Format("{0}: {1}", DateTime.Now.ToString(), str);
            Directory.CreateDirectory("logs");
            sw = new StreamWriter(@"logs\commonInfo.txt", true, Encoding.Default);
            sw.WriteLine(resMsg);
            sw.Close();
        }

        string ConnectionString = "Server=localhost;Port=5432;User Id=postgres;Password=1;Database=prizmer;";
        public string GetConnectionString()
        {
            //WriteToLog("test");
            return ConnectionString;
        }

        struct PollingParams
        {
            public bool b_poll_current;
            public bool b_poll_day;
            public bool b_poll_month;
            public bool b_poll_hour;
            public bool b_poll_halfanhour;
            public bool b_poll_archive;

            public TimeSpan ts_current_period;
            
            /* задержка перед началом опроса месячных и суточных в минутах
             * на случай, если часы прибора отставют
             */
            public int daily_monthly_delay_minutes;
        }
        PollingParams pollingParams;

        volatile bool bStopServer = true;

        #region Флаги отладки (DEBUG FLAGS)

        bool B_DEBUG_MODE_TCP = false;
        string DMTCP_IP = "192.168.23.52";
        ushort DMTCP_PORT = 5001;
        string DMTCP_DRIVER_NAME = "m230";
        uint DMTCP_METER_ADDR = 188;
        bool DMTCP_STATIC_METER_NUMBER = false;

        bool DM_POLL_ADDR = true;
        bool DM_POLL_CURR = true;
        bool DM_POLL_DAY = true;
        bool DM_POLL_MONTH = true;
        bool DM_POLL_HOUR = false;
        bool DM_POLL_HALFANHOUR = true;
        bool DM_POLL_ARCHIVE = true;

        #endregion

        public bool SO_AUTO_START = false;
        public bool LOGGER_GLOBAL_RESTRICTION = false;

        Logger loggerMainService = new Logger();
        NameValueCollection ApplicationParamsCollection = null;

        //получает данные из файла конфигурации предварительно проверяя на null
        private bool getSafeAppSettingsValue(string key, ref string value)
        {
            object oTmpVal = ConfigurationManager.AppSettings.GetValues(key);
            if (oTmpVal != null)
            {
                value = ((string[])oTmpVal)[0];
                return true;
            }

            return false;
        }

        public MainService()
        {
            try
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["generalConnection"].ConnectionString;
            }
            catch (Exception ex)
            {
                WriteToLog("Строка подключения к БД generalConnection не найдена в файле конфигурации: " + ex.Message);
            }


            List<string> rowValues = new List<string>();

            pollingParams.b_poll_current = true;

            pollingParams.b_poll_day = true;
            pollingParams.b_poll_month = true;
            pollingParams.b_poll_hour = true;
            pollingParams.b_poll_halfanhour = true;
            pollingParams.b_poll_archive = true;
            pollingParams.ts_current_period = new TimeSpan(DateTime.Now.Ticks);
            pollingParams.daily_monthly_delay_minutes = 0;

            DMTCP_STATIC_METER_NUMBER = B_DEBUG_MODE_TCP ? DMTCP_STATIC_METER_NUMBER : false;

            try
            {
                ApplicationParamsCollection = ConfigurationManager.AppSettings;
                string strTmpVal = "";

                if (getSafeAppSettingsValue("b_poll_current", ref strTmpVal))  
                    bool.TryParse(strTmpVal, out pollingParams.b_poll_current);

                if (getSafeAppSettingsValue("ts_current_period", ref strTmpVal))
                    TimeSpan.TryParse(strTmpVal, out pollingParams.ts_current_period);

                if (getSafeAppSettingsValue("b_poll_day", ref strTmpVal))
                    bool.TryParse(strTmpVal, out pollingParams.b_poll_day);

                if (getSafeAppSettingsValue("b_poll_month", ref strTmpVal))
                    bool.TryParse(strTmpVal, out pollingParams.b_poll_month);

                if (getSafeAppSettingsValue("b_poll_hour", ref strTmpVal))
                    bool.TryParse(strTmpVal, out pollingParams.b_poll_hour);

                if (getSafeAppSettingsValue("b_poll_halfanhour", ref strTmpVal))
                    bool.TryParse(strTmpVal, out pollingParams.b_poll_halfanhour);

                if (getSafeAppSettingsValue("b_poll_archive", ref strTmpVal))
                    bool.TryParse(strTmpVal, out pollingParams.b_poll_archive);

                if (getSafeAppSettingsValue("b_auto_start", ref strTmpVal))
                    bool.TryParse(strTmpVal, out SO_AUTO_START);

                if (getSafeAppSettingsValue("b_restrict_logs", ref strTmpVal))
                {
                    if (bool.TryParse(strTmpVal, out LOGGER_GLOBAL_RESTRICTION))
                        Logger.bRestrict = LOGGER_GLOBAL_RESTRICTION;
                }

                if (getSafeAppSettingsValue("daily_monthly_delay_minutes", ref strTmpVal))
                    int.TryParse(strTmpVal, out pollingParams.daily_monthly_delay_minutes);
            }
            catch (Exception ex)
            {
                WriteToLog("Проблемы с разбором файла конфигурации: " + ex.Message);
            }
        }

        private List<Thread> getStartComThreadsList(ComPortSettings[] cps, MainFormParamsStructure prms)
        {
            List<Thread> comPortThreadsList = new List<Thread>();

            //нам не нужны ком порты если дочитываем tcp
            if (prms.mode != OperatingMode.OM_AUTO && prms.isTcp) return comPortThreadsList;
            //нам не нужны ком порты если отлаживаем tcp
            if (B_DEBUG_MODE_TCP) return comPortThreadsList;


            for (int i = 0; i < cps.Length; i++)
            {
                //если дочитка, пропустим все порты кроме выбранного
                if (prms.mode != OperatingMode.OM_AUTO && !prms.isTcp)
                {
                    if (cps[i].name != prms.ip)
                        continue;
                }

                Meter[] metersbyport = ServerStorageMainService.GetMetersByComportGUID(cps[i].guid);

                Thread portThread = new Thread(new ParameterizedThreadStart(this.pollingPortThread));
                portThread.IsBackground = true;

                List<object> prmsList = new List<object>();

                prmsList.Add(cps[i]);
                prmsList.Add(portThread);
                prmsList.Add(prms);                  

                portThread.Start(prmsList);
                comPortThreadsList.Add(portThread);
            }

            return comPortThreadsList;
        }

        private List<Thread> getStartTcpThreadsList(TCPIPSettings[] tcpips, MainFormParamsStructure prms)
        {
            List<Thread> tcpPortThreadsList = new List<Thread>();

            //нам не нужны tcp если дочитываем com
            if (prms.mode != OperatingMode.OM_AUTO && !prms.isTcp) return tcpPortThreadsList;

            for (int i = 0; i < tcpips.Length; i++)
            {
                if (B_DEBUG_MODE_TCP)
                {
                    if (tcpips[i].ip_address != DMTCP_IP || tcpips[i].ip_port != DMTCP_PORT)
                        continue;
                }
                else if (prms.mode != OperatingMode.OM_AUTO && prms.isTcp)
                {
                    //WriteToLog("addr: " + tcpips[i].ip_address + "; p: " + tcpips[i].ip_port.ToString());
                    //WriteToLog("addr: " + prms.ip + "; p: " + ((ushort)prms.port).ToString());
                    if (tcpips[i].ip_address != prms.ip || tcpips[i].ip_port != (ushort)prms.port)
                        continue;
                }
                            
                Meter[] metersbyport = ServerStorageMainService.GetMetersByTcpIPGUID(tcpips[i].guid);

                Thread portThread = new Thread(new ParameterizedThreadStart(this.pollingPortThread));
                portThread.IsBackground = true;

                List<object> prmsList = new List<object>();
                prmsList.Add(tcpips[i]);
                prmsList.Add(portThread);
                prmsList.Add(prms);

                portThread.Start(prmsList);                 
                tcpPortThreadsList.Add(portThread);
 
            }

            return tcpPortThreadsList;
        }

        PgStorage ServerStorageMainService = new PgStorage();
        Analizator frmAnalizator = null;
        public void StartServer(MainFormParamsStructure mfPrms)
        {         
            bStopServer = false;

            frmAnalizator = mfPrms.frmAnalizator;

            #region Блок особых действий
                // sayani_kombik.DeleteDumpDirectory();
            #endregion

            System.Data.ConnectionState conState = ServerStorageMainService.Open(ConnectionString);
            if (conState == System.Data.ConnectionState.Broken)
            {
                MessageBox.Show("Невозможно подключиться к БД, проверьте строку подключения: " + ConnectionString, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }

            //обработка всех неотлавливаемых исключений для логгирования
            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledException_Handler);

            ComPortSettings[] cps = ServerStorageMainService.GetComportSettings();
            TCPIPSettings[] tcpips = ServerStorageMainService.GetTCPIPSettings();

            List<Thread> comPortThreads = new List<Thread>();
            List<Thread> tcpPortThreads = new List<Thread>();
            PortsThreads = new List<Thread>();

            if (!B_DEBUG_MODE_TCP)
            {
                if (mfPrms.mode == OperatingMode.OM_AUTO)
                {
                    comPortThreads = this.getStartComThreadsList(cps, mfPrms);
                    tcpPortThreads = this.getStartTcpThreadsList(tcpips, mfPrms);

                    PortsThreads.AddRange(comPortThreads);
                    PortsThreads.AddRange(tcpPortThreads);
                }
                else
                {
                    //manual operating mode

                    if (mfPrms.isTcp)
                    {
                        tcpPortThreads = this.getStartTcpThreadsList(tcpips, mfPrms);
                        PortsThreads.AddRange(tcpPortThreads);
                    }
                    else
                    {
                        comPortThreads = this.getStartComThreadsList(cps, mfPrms);
                        PortsThreads.AddRange(comPortThreads);
                    }


                }
            }
            else
            {
                //debug пока только для tcp портов
                tcpPortThreads = this.getStartTcpThreadsList(tcpips, mfPrms);
                PortsThreads.AddRange(tcpPortThreads);
            }
            

            if (PortsThreads.Count == 0)
            {
                if (mfPrms.mode != OperatingMode.OM_AUTO && pollingEnded != null)
                    pollingEnded(this, new MyEventArgs());

            }

            object deleteLogsThreadMethodPrmsObj = null;
            Thread logsEreaserThread = new Thread(new ParameterizedThreadStart(DeleteLogsThreadMethod));
            logsEreaserThread.IsBackground = true;
            logsEreaserThread.Start(deleteLogsThreadMethodPrmsObj);

            //закрываем соединение с БД
            ServerStorageMainService.Close();
        }

        Thread stopServerThread;

        public void SetBStopServer()
        {
            bStopServer = true;
            ReqStopServer?.Invoke();
        }
        public event Action ReqStopServer;

        public void StopServerThreadProc(object prms)
        {
            bool doAbort = (bool)prms;
            SetBStopServer();

            MyEventArgs mea = new MyEventArgs();
            if (stoppingStarted != null)
                stoppingStarted(this, mea);


            int periods = 30;


            if (doAbort)
            {
                for (int i = 0; i < PortsThreads.Count; i++)
                {
                    try
                    {
                        PortsThreads[i].Abort();
                    }
                    catch (Exception ex) { }
                }         

                    Thread.Sleep(2000);

                    if (stoppingEnded != null)
                    {
                        mea.success = true;
                        stoppingEnded(this, mea);
                    }

                    return;
            }

            for (int j = 0; j < periods; j++)
            {

                //bool allThreadsAreDead = true;
                //for (int k = 0; k < PortsThreads.Count; k++)
                //{
                //    int idx = frmAnalizator.deadThreads.FindIndex((item) => {
                //        return item.thread.ManagedThreadId == PortsThreads[k].ManagedThreadId;
                //    });

                //    if (idx > -1)
                //        allThreadsAreDead = false;
                //}

                if (frmAnalizator != null && PortsThreads.Count == frmAnalizator.deadThreads.Count)
                {
                    if (stoppingEnded != null)
                    {
                        mea.success = true;
                        stoppingEnded(this, mea);
                    }

                    return;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }

            if (stoppingEnded != null)
            {
                mea.success = false;
                stoppingEnded(this, mea);
            }

        }

        public void StopServer(bool doAbort = false)
        {
            if (stopServerThread != null) stopServerThread.Abort();

            stopServerThread = new Thread(new ParameterizedThreadStart(StopServerThreadProc));
            stopServerThread.Start((object)doAbort);
        }

        void UnhandledException_Handler(object sender, UnhandledExceptionEventArgs e)
        {
            //TODO: сделать это в лог
            WriteToLog("Глобальное исключение: " + e.ExceptionObject.ToString());
        }

        public void DeleteLogsThreadMethod(object param)
        {
            while (Thread.CurrentThread.IsAlive)
            {
                //Автоудаление логов
                try
                {
                    Logger.DeleteLogs();
                }
                catch (Exception ex)
                {
                    WriteToLog("DeleteLogsThreadMethod: " + ex.Message);
                }

                TimeSpan sleepSpan = new TimeSpan(1, 0, 0);
                Thread.Sleep(sleepSpan);
            }
        }

        // Обеспечивает ручное дочитывание
        private PollingResultStatus pollDatesRange(MyEventArgs myEventArgs, MainFormParamsStructure mfPrms, PollMethodsParams pmPrms)
        {
            PollMethods pollMethods = new PollMethods(this);
         
            myEventArgs.metersCount = pmPrms.metersbyport.Length;
            myEventArgs.currentCount = (int)pmPrms.MetersCounter;

            string portStr = pmPrms.m_vport.GetName();
            string mAddr = pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString();
            string mName = pmPrms.metersbyport[pmPrms.MetersCounter].name;

            DateTime dtStart = mfPrms.dtStart.Date;
            DateTime dtEnd = mfPrms.dtEnd.Date;
            TimeSpan diff = dtEnd - dtStart;

            pmPrms.logger.LogInfo("*** РУЧНАЯ ДОЧИТКА (метод pollDatesRange) ***");
            pmPrms.logger.LogInfo("Вычитка данных за интервал дат для " + mName);
            pmPrms.logger.LogInfo("Дата начала: " + dtStart.ToShortDateString());
            pmPrms.logger.LogInfo("Дата конца: " + dtEnd.ToShortDateString());
            pmPrms.logger.LogInfo("Прибор: " + mName + " порт " + pmPrms.m_vport.GetName() + " адрес " + pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString());

            // чтение текущих параметров, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = new TakenParams[0];
            // если опрашиваем из формы поиска, у нас уже есть набор takenparams и их нужно прочитать все
            if (mfPrms.mode == OperatingMode.OM_MANUAL_SEARCH_FORM)
            {
                takenparams = mfPrms.searchFormData.takenParams;
            }
            else
            {
                takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, (byte)mfPrms.paramType);
            }

            pmPrms.logger.LogInfo("Число параметров выбранного типа (" + mfPrms.paramType + "): " + takenparams.Length);


            if (takenparams.Length > 0)
            {
                if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

                DateTime tmpDateTime = new DateTime(dtStart.Ticks);
                int totalD = (int)diff.TotalDays;

                if (mfPrms.mode == OperatingMode.OM_MANUAL_SEARCH_FORM)
                    pmPrms.logger.LogInfo("Инициатор: форма поиска приборов");
                else
                    pmPrms.logger.LogInfo("Инициатор: главная форма");

                for (int d = 0; d <= totalD; d++)
                {
                    pmPrms.logger.LogInfo("Дата, за которую идет считывание: " + tmpDateTime.ToShortDateString());

                    if (mfPrms.mode == OperatingMode.OM_MANUAL_SEARCH_FORM)
                    {
                        PollingResultStatus[] statusArr = new PollingResultStatus[] { PollingResultStatus.UNDEFINED, PollingResultStatus.UNDEFINED,
                            PollingResultStatus.UNDEFINED, PollingResultStatus.UNDEFINED };
                        string statusStr = String.Empty;

                        // если дочитка с формы поиска, то у считываемых параметров разные типа
                        // вызовем все активные методы
                        if (pollingParams.b_poll_current)
                        {
                            statusArr[0] = pollMethods.pollCurrent(pmPrms, tmpDateTime);
                            statusStr += "Текущие: " + helper.GetEnumKeyAsString(resultEnumType, statusArr[0]) + "; ";
                        }
                  
                        if (pollingParams.b_poll_day)
                        {
                            statusArr[1] = pollMethods.pollDaily(pmPrms, tmpDateTime);
                            statusStr += "Суточные: " + helper.GetEnumKeyAsString(resultEnumType, statusArr[1]) + "; ";
                        }

                        if (pollingParams.b_poll_month)
                        {
                            statusArr[2] = pollMethods.pollMonthly(pmPrms, tmpDateTime);
                            statusStr += "Месячные: " + helper.GetEnumKeyAsString(resultEnumType, statusArr[1]) + "; ";
                        }

                        if (pollingParams.b_poll_halfanhour)
                        {
                            DateTime dt_start_halfs = new DateTime(tmpDateTime.Year, tmpDateTime.Month, tmpDateTime.Day, 0, 0, 0);
                            DateTime dt_end_halfs = new DateTime(tmpDateTime.Year, tmpDateTime.Month, tmpDateTime.Day, 23, 59, 59);
                            statusArr[3] = pollMethods.pollHalfsForDate(pmPrms, tmpDateTime);
                            statusStr += "Получасовые: " + helper.GetEnumKeyAsString(resultEnumType, statusArr[1]) + ";";
                        }

                        pmPrms.logger.LogInfo(statusStr + "\n");
                    }
                    else
                    {
                        // если дочитка с главной формы, мы сами указали тип считываемого параметра
                        // вызываем нужный метод

                        PollingResultStatus prSt = PollingResultStatus.UNDEFINED;


                        if (mfPrms.paramType == 0)
                        {
                            //текущий
                            prSt = pollMethods.pollCurrent(pmPrms, tmpDateTime);
                        }
                        else if (mfPrms.paramType == 1)
                        {
                            //суточный
                            prSt = pollMethods.pollDaily(pmPrms, tmpDateTime);
                        }
                        else if (mfPrms.paramType == 2)
                        {
                            //месячный
                            prSt = pollMethods.pollMonthly(pmPrms, tmpDateTime);
                        }
                        else if (mfPrms.paramType == 3)
                        {
                            //архивный
                            prSt = pollMethods.pollDaily(pmPrms, tmpDateTime, true);
                            //return 2;
                        }
                        else if (mfPrms.paramType == 4)
                        {
                            //получасовой

                            DateTime dt_start_halfs = new DateTime(tmpDateTime.Year, tmpDateTime.Month, tmpDateTime.Day, 0, 0, 0);
                            DateTime dt_end_halfs = new DateTime(tmpDateTime.Year, tmpDateTime.Month, tmpDateTime.Day, 23, 59, 59);

                            prSt = pollMethods.pollHalfsForDate(pmPrms, tmpDateTime);
                        }
                    }

                    tmpDateTime = tmpDateTime.AddDays(1);
                }
            }

            return PollingResultStatus.OK;
        }

        private void pollingPortThread(object data)
        {
            VirtualPort m_vport = null;
            Meter[] metersbyport = null;
            MyEventArgs myEventArgs = new MyEventArgs();      
            Logger logger = new Logger();
            Logger loggerThread = new Logger();
            Guid PortGUID = Guid.Empty;
         
            //подключение к БД
            PgStorage ServerStorage = new PgStorage();
            System.Data.ConnectionState conState = ServerStorage.Open(ConnectionString);

            List<object> prmsList = (List<object>)data;
            data = prmsList[0];
            MainFormParamsStructure mfPrms = new MainFormParamsStructure();
            mfPrms = (MainFormParamsStructure)prmsList[prmsList.Count - 1];


            bool POLLING_ACTIVE = mfPrms.mode == OperatingMode.OM_AUTO ? true : false;

            string portFullName = "";
            if (data.GetType().Name == "ComPortSettings" && !B_DEBUG_MODE_TCP)
            {
                ComPortSettings portsettings = (ComPortSettings)data;
                m_vport = new ComPort(portsettings);

                portFullName = m_vport.GetFullName();
                //читаем список приборов, привязанных к порту
                PortGUID = portsettings.guid;
                metersbyport = ServerStorage.GetMetersByComportGUID(PortGUID);
            }
            else if (data.GetType().Name == "TCPIPSettings")
            {
                TCPIPSettings portsettings = (TCPIPSettings)data;
                //m_vport = new Prizmer.Ports.TcpipPort(portsettings.ip_address, (int)portsettings.ip_port, portsettings.write_timeout, portsettings.read_timeout, 50);
                //читаем список приборов, привязанных к порту

                portFullName = portsettings.ip_address + ":" + portsettings.ip_port;
                //здесь мы не создаем порт сразу (это сделано для поддержки RDS, порт создается дальше               
                PortGUID = portsettings.guid;

                if (mfPrms.mode == OperatingMode.OM_MANUAL)
                {
                    // если вручную опрашиваем через главную форму, то получим счетчики с типом указанным типом драйвера
                    // имеющие параметры заданного типа и висящие на выбранном порту
                    metersbyport = ServerStorage.GetMetersByTcpIPGUIDAndParams(PortGUID, mfPrms.paramType, mfPrms.driverGuid);
                }
                else if (mfPrms.mode == OperatingMode.OM_MANUAL_SEARCH_FORM)
                {
                    // если идем из формы поиска, мы уже выбрали конкретный счетчик и знаем его guid
                    Meter m = ServerStorage.GetMeterByGUID(mfPrms.searchFormData.guidMeter);
                    metersbyport = new Meter[]{ m };
                }
                else
                {
                    // если в авто-режиме, получим все счетчики висящие на порту n
                    metersbyport = ServerStorage.GetMetersByTcpIPGUID(PortGUID);
                }

            }

            AnalizatorPollThreadInfo apti = new AnalizatorPollThreadInfo(portFullName);
            apti.metersByPort = metersbyport.Length;
            apti.thread = (Thread)prmsList[1];
            mfPrms.frmAnalizator.addThreadToLiveListOrUpdate(apti);

            loggerThread.Initialize(Logger.DIR_LOGS_MAIN, true, portFullName.Replace(':', '_'));
            //loggerThread.LogInfo("pollingPortThread-conState: " + Enum.GetName(typeof(System.Data.ConnectionState), conState));

            //if (m_vport == null) goto CloseThreadPoint;
            if (metersbyport == null || metersbyport.Length == 0)
            {
                loggerThread.LogWarn("Остановка: к порту не привязаны приборы");
                loggerThread.LogWarn("Остановка: portGUID: " + PortGUID);

                apti.metersByPort = 0;
                apti.commentList.Add("Остановка: к порту не привязаны приборы");
                apti.commentList.Add("Остановка: portGUID: " + PortGUID);
                goto CloseThreadPoint;
            }

            uint MetersCounter = 0;
            if (B_DEBUG_MODE_TCP) { 
                //в режиме отладки найдем # счетчика по его адресу
                for (int i = 0; i < metersbyport.Length; i++)
                    if (metersbyport[i].address == DMTCP_METER_ADDR)
                        MetersCounter = (uint)i;
            }

            myEventArgs.metersCount = metersbyport.Length;
            if (mfPrms.mode != OperatingMode.OM_AUTO && pollingStarted != null)
                pollingStarted(this, myEventArgs);

            PollMethods pollMethods = new PollMethods(this);

            while (!bStopServer)
            {
                //с этим тоже проблемы
                //apti.currentMeterNumber = (int)MetersCounter + 1;
                //mfPrms.frmAnalizator.addThreadToLiveListOrUpdate(apti);

                //здесь надо выбрать - какой драйвер будет использоваться
                TypeMeter typemeter = ServerStorage.GetMetersTypeByGUID(metersbyport[MetersCounter].guid_types_meters);
                IMeter meter = null;

                string tmpDriverName = typemeter.driver_name;

                if (B_DEBUG_MODE_TCP)
                    if (typemeter.driver_name != DMTCP_DRIVER_NAME)
                        tmpDriverName = "";

                switch (tmpDriverName)
                {
                    case "pulsar10": meter = new pulsar10(); break;
                    case "pulsar16": meter = new pulsar16(); break;
                    case "tem4": meter = new tem104(); break;
                    case "tem106": meter = new tem106(); break;
                    case "set4tm_03": meter = new SET4tmDriver(); break;
                    case "set4tm": meter = new SET4tmDriver(); break;
                    case "spg76212": meter = new spg76212(); break;
                    case "teplouchet1": meter = new teplouchet1(); break;
                    case "m200": meter = new Mercury200Driver(); break;
                    case "opcretranslator": meter = new OpcRetranslator(); break;
                    case "sayani_kombik": meter = new sayani_kombik(); break;
                    case "m230": meter = new Mercury23XDriver(); break;
                    case "m234": meter = new Mercury23XDriver(); break;
                    case "m230_stable": meter = new m230(); break;
                    case "um40rtu" : meter = new UMRTU40Driver(); break;
                    case "elf108": meter = new ElfApatorDriver(); break;
                    case "PulsarM": meter = new PulsarDriver(); break;
                    case "pulsar_teplo": meter = new PulsarDriver(); break;
                    case "pulsar_hvs": meter = new PulsarDriver(); break;
                    case "pulsar_gvs": meter = new PulsarDriver(); break;
                    case "karat_23X": meter = new Karat30XDriver(); break;
                    case "karat_danfos": meter = new KaratDanfosDriver(); break;
                }

                if (meter == null) goto NetxMeter;

    
                /*если соединяться с конечной точкой вначале, то консольная программа rds не сможет с ней соединиться
                 * поэтому создание порта и подключение к нему осуществляется на первой итерации цикла при условии,
                 * что счетчик не саяны. Предполагаются что на одном порту будут висеть только саяны, если будут другие приборы,
                 * создастся порт и саяны не будут читаться снова.
                 * */

                if (m_vport == null && (typemeter.driver_name != "sayani_kombik")) {
                    TCPIPSettings portsettings = (TCPIPSettings)data;
                    m_vport = new TcpipPort(portsettings.ip_address, (int)portsettings.ip_port, portsettings.write_timeout, portsettings.read_timeout, 50, ApplicationParamsCollection);

                    apti.vp = m_vport;
                    mfPrms.frmAnalizator.addThreadToLiveListOrUpdate(apti);
                }
                else if (m_vport == null && (typemeter.driver_name == "sayani_kombik"))
                {

                    // рассписание для саян, n дней до окончания месяца, n дней после окончания
                    DateTime dt = DateTime.Now.Date;


                    bool bFollowSchedule = true;


                    string str1 = "";
                    int iDaysAfterMonthEnd = 0;

                    string str2 = "";
                    int iDaysBeforeMonthEnd = 0;

                    if (!getSafeAppSettingsValue("sayaniDaysBeforeMonthEnd", ref str1) || !int.TryParse(str1, out iDaysBeforeMonthEnd))
                        bFollowSchedule = false;
                    if (!getSafeAppSettingsValue("sayaniDaysAfterMonthEnd", ref str2) || !int.TryParse(str2, out iDaysAfterMonthEnd))
                        bFollowSchedule = false;




                    if (iDaysBeforeMonthEnd == 0 && iDaysAfterMonthEnd == 0)
                        bFollowSchedule = false;

                    //WriteToLog(String.Format("sayani schedule: {0}, {1}, {2}, {3}, {4}", str1, iDaysBeforeMonthEnd, str2, iDaysAfterMonthEnd, bFollowSchedule));

                    if (bFollowSchedule)
                    {
                        DateTime dtMonthBeginFrom = new DateTime(dt.Year, dt.Month, 1);
                        DateTime dtMonthBeginTo = dtMonthBeginFrom.AddDays(iDaysAfterMonthEnd);

                        int daysInMonth = DateTime.DaysInMonth(dt.Year, dt.Month);
                        DateTime dtMonthEndTo = new DateTime(dt.Year, dt.Month, daysInMonth);
                        DateTime dtMonthEndFrom = dtMonthEndTo.AddDays(-iDaysBeforeMonthEnd);

                        bool cond1 = (dt < dtMonthBeginFrom) || (dt > dtMonthBeginTo);
                        bool cond2 = (dt < dtMonthEndFrom) || (dt > dtMonthEndTo);

                        //WriteToLog(String.Format("sayani schedule 2: {0}, {1}, {2}, {3}, {4}", dtMonthBeginFrom, dtMonthBeginTo, dtMonthEndFrom, dtMonthEndTo, dt));
                        //WriteToLog(String.Format("sayani schedule 3: {0}, {1}", cond1, cond2));

                        if (cond1 && cond2)
                            goto NetxMeter;
                    }

                    ComPortSettings tmpRdsComSettings = new ComPortSettings();
                    tmpRdsComSettings.name = "COM250";
                    tmpRdsComSettings.baudrate = 9600;
                    tmpRdsComSettings.data_bits = 8;
                    tmpRdsComSettings.parity = 1;
                    tmpRdsComSettings.stop_bits = 1;

                    m_vport = new ComPort(tmpRdsComSettings);

                    apti.vp = m_vport;
                    apti.commentList.Add("Порт для поддержки RDS");
                    mfPrms.frmAnalizator.addThreadToLiveListOrUpdate(apti);
                }

                //возникают проблемы с этим
               // apti.currentMeterName = metersbyport[MetersCounter].name + ": a: " + metersbyport[MetersCounter].address + "; s/n: " + metersbyport[MetersCounter].factory_number_manual;
              //  mfPrms.frmAnalizator.addThreadToLiveListOrUpdate(apti);
 
                meter.Init(metersbyport[MetersCounter].address, metersbyport[MetersCounter].password, m_vport);
                logger.Initialize(Logger.DIR_LOGS_MAIN, false, m_vport.GetName(), typemeter.driver_name, metersbyport[MetersCounter].address.ToString(), metersbyport[MetersCounter].factory_number_manual);
                //  logger.LogInfo(String.Format("[{3}] Meter with id {0} and address {1} initialized. Port: {2}; ", metersbyport[MetersCounter].password, metersbyport[MetersCounter].address, m_vport.GetName(), typemeter.driver_name));

                //выведем в лог общие ошибки если таковые есть
                DateTime common_dt_install = metersbyport[MetersCounter].dt_install;
                DateTime common_dt_cur = DateTime.Now;

                PollMethodsParams pmPrms = new PollMethodsParams();
                pmPrms.m_vport = m_vport;
                pmPrms.logger = logger;
                pmPrms.meter = meter;
                pmPrms.MetersCounter = MetersCounter;
                pmPrms.ServerStorage = ServerStorage;
                pmPrms.metersbyport = metersbyport;
                pmPrms.common_dt_cur = common_dt_cur;
                pmPrms.common_dt_install = common_dt_install;


                //***************************************| Чтение S/N |***************************************    
                if (bStopServer) goto CloseThreadPoint;
                // POLLING_ACTIVE && DM_POLL_ADDR
                if (DM_POLL_ADDR)
                {
                    PollingResultStatus status = PollingResultStatus.UNDEFINED;

                    if (typemeter.driver_name == "m230")
                    {
                        // 230 поддерживают чтение ошибок, как текущих при чтении серийника
                        // решение временное, нужно переделать добавлением метода в интерфейс
                        status = pollMethods.pollSerialNumber(pmPrms, true);
                    }
                    else
                    {
                        status = pollMethods.pollSerialNumber(pmPrms);
                    }
  
                    string statusStr = helper.GetEnumKeyAsString(resultEnumType, status);
                    pmPrms.logger.LogInfo("Прочитал серийный номер со статусом: " + statusStr);
                    if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                }


                //***************************************| Значения ТЕКУЩИЕ (0) |***************************************               
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_CURR && pollingParams.b_poll_current)
                {
                    PollingResultStatus status = pollMethods.pollCurrent(pmPrms, DateTime.Now);
                    string statusStr = helper.GetEnumKeyAsString(resultEnumType, status);
                    pmPrms.logger.LogInfo("Прочитал ТЕКУЩИЕ со статусом: " + statusStr);
                    if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                }

                //***************************************| На начало СУТОК (1) |***************************************   
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_DAY && pollingParams.b_poll_day)
                {
                    PollingResultStatus status = PollingResultStatus.UNDEFINED;
                    bool delayCondition = pollingParams.daily_monthly_delay_minutes > 0 && DateTime.Now.Minute < pollingParams.daily_monthly_delay_minutes;

                    if (!delayCondition)
                        status = pollMethods.pollDaily(pmPrms, DateTime.Now);
                    else
                        status = PollingResultStatus.DELAYED_BY_TIMEOUT;

                    string statusStr = helper.GetEnumKeyAsString(resultEnumType, status);

                    pmPrms.logger.LogInfo("Прочитал СУТОЧНЫЕ со статусом: " + statusStr);
                    if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                }

                //***************************************| На начало МЕСЯЦА (2) |***************************************  
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_MONTH && pollingParams.b_poll_month)
                {
                    PollingResultStatus status = PollingResultStatus.UNDEFINED;
                    bool delayCondition = pollingParams.daily_monthly_delay_minutes > 0 && DateTime.Now.Minute < pollingParams.daily_monthly_delay_minutes;

                    if (!delayCondition)
                        status = pollMethods.pollMonthly(pmPrms);
                    else
                        status = PollingResultStatus.DELAYED_BY_TIMEOUT;

                    string statusStr = helper.GetEnumKeyAsString(resultEnumType, status);
                    pmPrms.logger.LogInfo("Прочитал МЕСЯЧНЫЕ со статусом: " + statusStr);
                    if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                }

                //***************************************| Значения АРХИВНЫЕ (3) |***************************************  

                // Архивные используются только при работе с Elf, не нужно убирать совсем, или выполнить рефакторинг

                if (bStopServer) goto CloseThreadPoint;
                //if (false && POLLING_ACTIVE && DM_POLL_ARCHIVE && pollingParams.b_poll_archive)
                //{
                //    pollArchivesOld(pmPrms);
                //}
                //if (false && POLLING_ACTIVE && DM_POLL_ARCHIVE && pollingParams.b_poll_archive)
                //{
                //    pollArchivesNewActual(pmPrms);
                //}
                if (POLLING_ACTIVE && DM_POLL_ARCHIVE && pollingParams.b_poll_archive)
                {
                    const bool bPollArchiveAsDaily = true;
                    PollingResultStatus status = PollingResultStatus.UNDEFINED;
                    bool delayCondition = pollingParams.daily_monthly_delay_minutes > 0 && DateTime.Now.Minute < pollingParams.daily_monthly_delay_minutes;

                    if (!delayCondition)
                        status = pollMethods.pollDaily(pmPrms, DateTime.Now, bPollArchiveAsDaily);
                    else
                        status = PollingResultStatus.DELAYED_BY_TIMEOUT;

                    string statusStr = helper.GetEnumKeyAsString(resultEnumType, status);

                    pmPrms.logger.LogInfo("Прочитал СУТОЧНЫЕ (бывш. АРХИВНЫЕ) со статусом: " + statusStr);
                    if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                }

                //***************************************| Значения ПОЛУЧАСОВЫЕ (4) |***************************************  
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_HALFANHOUR && pollingParams.b_poll_halfanhour)
                {
                    //if (typemeter.driver_name == "set4tm_03" || typemeter.driver_name == "set4tm")
                    //{
                    //    int status = pollHalfsSet4M230(pmPrms);
                    //    if (status == 1) goto CloseThreadPoint;
                    //}
                    //else 
                    
                    if (typemeter.driver_name == "m230")
                    {
                        //дочитка за вчера
                        DateTime dtCur = DateTime.Now;
                        DateTime dtStartY = new DateTime(dtCur.Year, dtCur.Month, dtCur.Day, 0, 0, 0).AddDays(-1);

                        PollingResultStatus status = pollMethods.pollHalfsForDate(pmPrms, dtStartY);
                        if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;

                        status = pollMethods.pollHalfsAutomatically(pmPrms);
                        if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                    }
                    else
                    {
                        PollingResultStatus status = pollMethods.pollHalfsAutomatically(pmPrms);
                        if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                    }
                }

                //***************************************| Значения ЧАСОВЫЕ (5) |*************************************** 
                if (bStopServer) goto CloseThreadPoint;
                if (false && POLLING_ACTIVE && DM_POLL_HOUR && pollingParams.b_poll_hour)
                {
                    PollingResultStatus status = pollMethods.pollHours(pmPrms);
                    if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                }
                


                //********************************************************************************************************************* 
                //***************************************| ДОЧИТКА ЗНАЧЕНИЙ |**********************************************************
                //*********************************************************************************************************************
                if (mfPrms.mode != OperatingMode.OM_AUTO && typemeter.guid == mfPrms.driverGuid)
                {
                    PollingResultStatus status = pollDatesRange(myEventArgs, mfPrms, pmPrms);
                    if (status == PollingResultStatus.STOP_SERVER_REQUEST) goto CloseThreadPoint;
                }

            NetxMeter:

                if (!DMTCP_STATIC_METER_NUMBER)
                {
                    MetersCounter++;
                    if (mfPrms.mode == OperatingMode.OM_AUTO && MetersCounter >= metersbyport.Length)
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

                        if (metersbyport.Length == 0)
                        {
                            loggerThread.LogError("Остановка: приборы были привязаны к порту, но сейчас их нет");

                            apti.metersByPort = 0;
                            apti.commentList.Add("Остановка: приборы были привязаны к порту, но сейчас их нет");
                            goto CloseThreadPoint;
                        }

                        //if (m_vport.GetConnectionType() == "tcp")
                        //    m_vport.ReInitialize();
                    }
                    else if (mfPrms.mode != OperatingMode.OM_AUTO)
                    {
                        // РЕЖИМ РУЧНОЙ ДОЧИТКИ
                        if (meterPolled != null)
                            meterPolled(this, myEventArgs);

                        if (MetersCounter >= metersbyport.Length)
                        {
                            if (pollingEnded != null)
                            {
                                if (m_vport != null)
                                {
                                    m_vport.Close();
                                }
                                else
                                {
                                    WriteToLog("Все счетчики перебраны, но объект порта так и не был создан. Возможно в машине состояний нет дрйвера "
                                        + typemeter.driver_name);
                                }

                                pollingEnded(this, myEventArgs);
                                SetBStopServer();
                            }

                            break;
                        }
                    }

                    Thread.Sleep(300);
                }
            }

            //закрываем соединение с БД
        CloseThreadPoint:
            if (bStopServer)
            {
                apti.commentList.Add("Остановка: по требованию пользователя");
            }

            mfPrms.frmAnalizator.moveThreadToDeadList(apti);

            ServerStorage.Close();
            if (m_vport != null)
                m_vport.Close();


        }
    }

    public enum PollingResultStatus
    {
        UNDEFINED = -1,
        OK = 0,
        STOP_SERVER_REQUEST = 1,
        NO_TAKEN_PARAMS = 2,
        OPEN_LINK_CHANNEL_FAULT = 3,
        DELAYED_BY_TIMEOUT = 4,

        SEE_DETAILS_IN_CODE_1 = 10
    }

    public class MyEventArgs:EventArgs
    {
        public int metersCount;
        public bool success;
        public int currentCount;
    }

    public struct PollMethodsParams
    {
        public IMeter meter;
        public VirtualPort m_vport;
        public PgStorage ServerStorage;
        public Meter[] metersbyport;
        public uint MetersCounter;
        public Logger logger;
        public DateTime common_dt_install;
        //нужно убрать этот позор
        public DateTime common_dt_cur;
    };


}
