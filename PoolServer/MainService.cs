using System;
using System.Collections.Generic;
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

using Drivers.LibMeter;
using Drivers.PulsarDriver;
using Drivers.ElfApatorDriver;

namespace Prizmer.PoolServer
{
    class MainService
    {
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
            WriteToLog("test");
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
        bool DM_POLL_ARCHIVE = false;

        #endregion

        public bool SO_AUTO_START = false;

        Logger loggerMainService = new Logger();
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

            DMTCP_STATIC_METER_NUMBER = B_DEBUG_MODE_TCP ? DMTCP_STATIC_METER_NUMBER : false;

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

                strTmpVal = ConfigurationManager.AppSettings.GetValues("b_auto_start")[0];
                bool.TryParse(strTmpVal, out SO_AUTO_START);
            }
            catch (Exception ex)
            {
                WriteToLog("Проблемы с разбором файла конфигурации: " + ex.Message);
            }
        }

        private List<Thread> getStartComThreadsList(ComPortSettings[] cps, MainFormParamsStructure prms)
        {
            List<Thread> comPortThreadsList = new List<Thread>();

            //нам не нужны ком порты если отлаживаем tcp
            if (B_DEBUG_MODE_TCP) return comPortThreadsList;


            for (int i = 0; i < cps.Length; i++)
            {
                Meter[] metersbyport = ServerStorageMainService.GetMetersByComportGUID(cps[i].guid);
                //if (metersbyport.Length > 0)
              //  {
                    Thread portThread = new Thread(new ParameterizedThreadStart(this.pollingPortThread));
                    portThread.IsBackground = true;

                    List<object> prmsList = new List<object>();

                    prmsList.Add(cps[i]);
                    prmsList.Add(portThread);
                    prmsList.Add(prms);                  

                    portThread.Start(prmsList);
                    comPortThreadsList.Add(portThread);
             //   }
            }

            return comPortThreadsList;
        }

        private List<Thread> getStartTcpThreadsList(TCPIPSettings[] tcpips, MainFormParamsStructure prms)
        {
            List<Thread> tcpPortThreadsList = new List<Thread>();

            for (int i = 0; i < tcpips.Length; i++)
            {
                if (B_DEBUG_MODE_TCP)
                {
                    if (tcpips[i].ip_address != DMTCP_IP || tcpips[i].ip_port != DMTCP_PORT)
                        continue;
                }
                else if (prms.mode == 1 && prms.isTcp)
                {
                    //WriteToLog("addr: " + tcpips[i].ip_address + "; p: " + tcpips[i].ip_port.ToString());
                    //WriteToLog("addr: " + prms.ip + "; p: " + ((ushort)prms.port).ToString());
                    if (tcpips[i].ip_address != prms.ip || tcpips[i].ip_port != (ushort)prms.port)
                        continue;
                }
                            
                Meter[] metersbyport = ServerStorageMainService.GetMetersByTcpIPGUID(tcpips[i].guid);
                //WriteToLog("mbp: " + metersbyport.Length );
             //   if (metersbyport.Length > 0)
              //  {


                    Thread portThread = new Thread(new ParameterizedThreadStart(this.pollingPortThread));
                    portThread.IsBackground = true;

                    List<object> prmsList = new List<object>();
                    prmsList.Add(tcpips[i]);
                    prmsList.Add(portThread);
                    prmsList.Add(prms);

                    portThread.Start(prmsList);                 
                    tcpPortThreadsList.Add(portThread);
               // }
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
                sayani_kombik.DeleteDumpDirectory();
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

            if (!B_DEBUG_MODE_TCP && mfPrms.mode == 0)
            {
                comPortThreads = this.getStartComThreadsList(cps, mfPrms);
                PortsThreads.AddRange(comPortThreads);
            }

            tcpPortThreads = this.getStartTcpThreadsList(tcpips, mfPrms);
            PortsThreads.AddRange(tcpPortThreads);

            if (tcpPortThreads.Count == 0)
            {
                if (mfPrms.mode == 1 && pollingEnded != null)
                    pollingEnded(this, new MyEventArgs());

            }

            object deleteLogsThreadMethodPrmsObj = null;
            Thread logsEreaserThread = new Thread(new ParameterizedThreadStart(DeleteLogsThreadMethod));
            logsEreaserThread.IsBackground = true;
            logsEreaserThread.Start(deleteLogsThreadMethodPrmsObj);

            //закрываем соединение с БД
            ServerStorageMainService.Close();
        }

        bool _manualStartInProcess = false;
        bool ManualStartInProcess
        {
            get { return _manualStartInProcess; }
            set
            {
                if (value)
                {


                }
                else
                {



                }
            }

        }


        Thread stopServerThread;

        public void StopServerThreadProc(object prms)
        {
            bool doAbort = (bool)prms;
            bStopServer = true;

            MyEventArgs mea = new MyEventArgs();
            if (stoppingStarted != null)
                stoppingStarted(this, mea);


            int periods = 120;


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
                if (frmAnalizator != null && frmAnalizator.deadThreads.Count == PortsThreads.Count)
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

        #region Методы опроса параметров разных типов

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

        private int pollSerialNumber(PollMethodsParams pmPrms)
        {
            if (bStopServer) return 1;

            pmPrms.logger.LogInfo("Чтение серийника открыт");
            string serial_number = String.Empty;
            if (pmPrms.meter.OpenLinkCanal())
            {
                pmPrms.logger.LogInfo("Канал для чтения серийника открыт");
                Meter mDb = pmPrms.metersbyport[pmPrms.MetersCounter];
                string isEqual = "";

                if (pmPrms.meter.ReadSerialNumber(ref serial_number))
                {
                    pmPrms.logger.LogInfo("Серийник прочитан: " + serial_number);

                    if (mDb.factory_number_manual == serial_number)
                        isEqual = "TRUE";
                    else
                        isEqual = "FALSE";

                    pmPrms.ServerStorage.UpdateMeterFactoryNumber(mDb.guid, serial_number, isEqual);
                }
                else
                {
                    pmPrms.logger.LogInfo("Серийник не прочитан...");
                    //ServerStorage.UpdateMeterFactoryNumber(mDb.guid, String.Empty, String.Empty);
                }
            }
            else
            {
                //связь с прибором не установлена
                return 3;
            }

            return 0;
        }

        private int pollCurrent(PollMethodsParams pmPrms, DateTime currentDT)
        {
            if (bStopServer) return 1;

            //чтение текущих параметров, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, 0);

            if (takenparams.Length > 0)
            {
                //читать данные только если прибор ответил
                if (pmPrms.meter.OpenLinkCanal())
                {
                    for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                    {
                        if (bStopServer) return 1;

                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        float curvalue = 0;
                        if (pmPrms.meter.ReadCurrentValues(param.param_address, param.channel, ref curvalue))
                        {
                            Value value = new Value();
                            value.dt = currentDT;
                            value.id_taken_params = takenparams[tpindex].id;
                            value.status = false;
                            value.value = curvalue;

                            pmPrms.ServerStorage.AddCurrentValues(value);
                            pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                        }
                        else
                        {
                            string s_log = String.Format("Текущие: метод драйвера ReadCurrentValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан",
                                param.name, param.param_address, param.channel);
                            pmPrms.logger.LogError(s_log);
                        }
                    }
                }
                else
                {
                    //связь с прибором не установлена
                    return 3;
                }
            }
            else
            {
                //параметры данного типа не считываются
                return 2;
            }

            return 0;
        }
        private int pollDaily(PollMethodsParams pmPrms, DateTime date)
        {
            if (bStopServer) return 1;

            //pmPrms.logger.LogInfo("Polling daily...");

            DateTime curDate = DateTime.Now;
            if (date.Date > curDate.Date) date = curDate.Date;

            DateTime startDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            DateTime endDate = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);


            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, 1);
            if (takenparams.Length > 0)
            {
                string portStr = pmPrms.m_vport.GetName();
                string mAddr = pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString();
                string mName = pmPrms.metersbyport[pmPrms.MetersCounter].name;

                for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                {
                    if (bStopServer) return 1;

                    Value[] lastvalue = pmPrms.ServerStorage.GetExistsDailyValuesDT(takenparams[tpindex], startDate, endDate);
                    if (lastvalue.Length > 0) continue;

                    Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                    if (param.guid == Guid.Empty) continue;

                    if (pmPrms.meter.OpenLinkCanal())
                    {
                        
                        float curvalue = 0;
                        if (pmPrms.meter.ReadDailyValues(date, param.param_address, param.channel, ref curvalue))
                        {
                            Value value = new Value();
                            value.dt = date;
                            value.id_taken_params = takenparams[tpindex].id;
                            value.status = false;
                            value.value = curvalue;

                            pmPrms.ServerStorage.AddDailyValues(value);
                            pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                        }
                        else
                        {
                            string s_log = String.Format("Суточные: метод драйвера ReadDailyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан;",
                                param.name, param.param_address, param.channel);
                            pmPrms.logger.LogWarn(s_log);                      
                            pmPrms.logger.LogInfo("Счетчик " + mName + " порт " + pmPrms.m_vport.ToString() + " адрес " + pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString() + ";");
                        }
                    }
                    else 
                    {
                        string s_log = String.Format("Суточные: не удалось открыть канал связи. Параметр {0} с адресом {1} каналом {2} не прочитан;",
                            param.name, param.param_address, param.channel);
                        pmPrms.logger.LogWarn(s_log);
                        pmPrms.logger.LogInfo("Счетчик " + mName + " порт " + pmPrms.m_vport.ToString() + " адрес " + pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString() + ";");
                    }
                }
            }
            else
            {
                return 2;
            }

            return 0;
        }
        private int pollMonthly(PollMethodsParams pmPrms)
        {
            if (bStopServer) return 1;

            DateTime CurTime = DateTime.Now; 
            DateTime PrevTime = new DateTime(CurTime.Year, CurTime.Month, 1);
            DateTime tmpDate;

            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, 2);
            if (takenparams.Length > 0)
            {
                //pmPrms.logger.LogInfo("Месячные: начало чтения суточных");
                for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                {
                    if (bStopServer){
                        return 1;  
                    }

                    tmpDate = PrevTime;

                    Value[] lastvalue = pmPrms.ServerStorage.GetExistsMonthlyValuesDT(takenparams[tpindex], tmpDate, tmpDate);
                       
                   // string queryExample = "SELECT date, value, status, id_taken_params FROM monthly_values " +
            //"WHERE (id_taken_params = " + takenparams[tpindex].id + ") AND date BETWEEN '" + tmpDate.ToShortDateString() + "' AND '" + tmpDate.ToShortDateString() + "'";
                    //pmPrms.logger.LogInfo("Месячные: запрос в базу на проверку существования: " + queryExample);

                    //если значение в БД уже есть, то не читать его из прибора
                    if (lastvalue.Length > 0) continue;

                    //читать данные только если прибор ответил
                    if (pmPrms.meter.OpenLinkCanal())
                    {

                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        //RecordValueEnergy rve = new RecordValueEnergy();
                            
                        float curvalue = 0;

                        //чтение месячных параметров
                        if (pmPrms.meter.ReadMonthlyValues(tmpDate, param.param_address, param.channel, ref curvalue))
                        {
                            Value value = new Value();
                            value.dt = new DateTime(tmpDate.Year, tmpDate.Month, 1);
                            value.id_taken_params = takenparams[tpindex].id;
                            value.status = false;
                            value.value = curvalue;
                            //pmPrms.logger.LogInfo("Месячные: на дату " + value.dt.ToShortDateString() + " значение " + curvalue);

                            value.value = (float)Math.Round(value.value, 4, MidpointRounding.AwayFromZero);
                            //pmPrms.logger.LogInfo("Месячные: на дату " + value.dt.ToShortDateString() + " значение преобразованное" + value.value);
                            pmPrms.ServerStorage.AddMonthlyValues(value);
                            pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                        }
                        else
                        {
                            string s_log = String.Format("На начало месяца: метод драйвера ReadMonthlyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан. Запрашиваемая дата: {3}",
                                param.name, param.param_address, param.channel, tmpDate.ToString());
                            pmPrms.logger.LogError(s_log);
                            //meter.WriteToLog("текущий параметр не прочитан:" + param.param_address.ToString());   
                        }
                    }
                    else
                    {
                        //meter.WriteToLog("ошибка cвязи с прибором");
                    }                   
                }
            }
            else
            {
                return 2;
            }

            return 0;
        }

        private int pollArchivesOld(PollMethodsParams pmPrms)
        {
            if (bStopServer) return 1;

            DateTime cur_date = DateTime.Now.Date;
            DateTime dt_install = pmPrms.metersbyport[pmPrms.MetersCounter].dt_install.Date;

            //чтение архивных параметров, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, 3);
            if (takenparams.Length > 0)
            {
                if (pmPrms.common_dt_install.Ticks == 0)
                    pmPrms.logger.LogWarn("Дата установки прибора не задана, критично для АРХИВНЫХ ПАРАМЕТРОВ");
                if (pmPrms.common_dt_install > pmPrms.common_dt_cur)
                    pmPrms.logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для АРХИВНЫХ ПАРАМЕТРОВ");

                for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                {
                    if (bStopServer) return 1;

                    //получим все записи в интервале от даты установки (если нет, от начала НЭ) до текущего момента
                    Value[] valueArr = pmPrms.ServerStorage.GetExistsDailyValuesDT(takenparams[tpindex], dt_install, cur_date);

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
                    if (pmPrms.meter.OpenLinkCanal())
                    {
                        float curValue = 0;

                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        for (int i = 0; i <= diff.TotalDays; i++)
                        {
                            int cnt = 0;
                        READAGAIN:
                            if (pmPrms.meter.ReadDailyValues(fromDate, param.param_address, param.channel, ref curValue))
                            {
                                Value value = new Value();
                                value.dt = fromDate;
                                value.id_taken_params = takenparams[tpindex].id;
                                value.status = false;
                                value.value = curValue;
                                pmPrms.ServerStorage.AddDailyValues(value);
                                pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                                //meter.WriteToLog("Арх: записал в базу " + value.value.ToString());
                            }
                            else
                            {
                                string s_log = String.Format("Архивные: попытка {4}, метод драйвера ReadDailyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан. Запрашиваемая дата: {3}",
                                    param.name, param.param_address, param.channel, fromDate.ToString(), cnt);
                                pmPrms.logger.LogError(s_log);

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
            return 0;
        }
        private int pollArchivesNewActual(PollMethodsParams pmPrms)
        {
            if (bStopServer) return 1;

            bool doArchLog = true;

            DateTime cur_date = new DateTime(DateTime.Now.Date.Ticks);

            //если дата установки отсутствует, считаем что счетчик установлен сегодня
            DateTime dt_install = new DateTime();
            dt_install = pmPrms.metersbyport[pmPrms.MetersCounter].dt_install.Date.Ticks == 0 ? new DateTime(0).Date : pmPrms.metersbyport[pmPrms.MetersCounter].dt_install.Date;

            //чтение архивных параметров, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, 3);
            if (takenparams.Length > 0)
            {
                if (pmPrms.common_dt_install.Ticks == 0)
                    pmPrms.logger.LogWarn("Дата установки прибора не задана, критично для АРХИВНЫХ ПАРАМЕТРОВ");
                if (pmPrms.common_dt_install > pmPrms.common_dt_cur)
                    pmPrms.logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для АРХИВНЫХ ПАРАМЕТРОВ");

                for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                {
                    if (doArchLog) pmPrms.logger.LogInfo("Архивные: параметр: " + tpindex.ToString());
                    if (bStopServer) return 1;
                    Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
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

                    if (doArchLog) pmPrms.logger.LogInfo("Архивные: дата начала: " + fromDate.ToString());
                    TimeSpan diff = DateTime.Now.Date - fromDate.Date;
                    if (doArchLog) pmPrms.logger.LogInfo("Архивные: разница в днях между тек. и нач. датами: " + diff.TotalDays.ToString());
                    //читать данные только если прибор ответил
                    if (pmPrms.meter.OpenLinkCanal())
                    {
                        float curValue = 0;

                        DateTime tmpDT = new DateTime(fromDate.Ticks);
                        for (int i = 0; i <= diff.TotalDays; i++)
                        {
                            if (doArchLog) pmPrms.logger.LogInfo(String.Format("Архивные: день: {0}; дата: {1};", i, tmpDT.ToString()));
                            int cnt = 0;
                            //получим все записи в интервале от даты установки (если нет, от начала НЭ) до текущего 
                            Value[] valueArr = pmPrms.ServerStorage.GetExistsDailyValuesDT(takenparams[tpindex], tmpDT, tmpDT);
                            //если в базе найдено суточное показание продолжим
                            if (valueArr.Length > 0)
                            {
                                if (doArchLog) pmPrms.logger.LogInfo(String.Format("Архивные: в базе есть показание на эту дату: {0}; дата: {1};", valueArr[0].value.ToString(), valueArr[0].dt.ToString()));
                                tmpDT = tmpDT.AddDays(1);
                                continue;
                            }


                        READAGAIN:
                            if (pmPrms.meter.ReadDailyValues(tmpDT, param.param_address, param.channel, ref curValue))
                            {
                                Value value = new Value();
                                value.dt = tmpDT;
                                value.id_taken_params = takenparams[tpindex].id;
                                value.status = false;
                                value.value = curValue;
                                value.value = (float)Math.Round(value.value, 2, MidpointRounding.AwayFromZero);
                                pmPrms.ServerStorage.AddDailyValues(value);
                                pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                                //meter.WriteToLog("Арх: записал в базу " + value.value.ToString());
                            }
                            else
                            {
                                string s_log = String.Format("Архивные: попытка {4}, метод драйвера ReadDailyValues вернул false. Параметр {0} с адресом {1} каналом {2} не прочитан. Запрашиваемая дата: {3}",
                                    param.name, param.param_address, param.channel, tmpDT.ToString(), cnt);
                                pmPrms.logger.LogError(s_log);

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
            return 0;
        }

        #region Методы чтения получасовок (старые)
        //TODO: refactor 
        private int pollHalfsSet4M230(PollMethodsParams pmPrms)
        {
            if (bStopServer) return 1;

            const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
            const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов

            //чтение получасовых срезов, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid,
                SLICE_PER_HALF_AN_HOUR_TYPE);

            if (takenparams.Length > 0)
            {
                //читать данные только если прибор ответил
                if (pmPrms.meter.OpenLinkCanal())
                {
                    const bool WRITE_LOG = true;
                    pmPrms.meter.WriteToLog("RSL: 1. Открыт канал для чтения получасовок", WRITE_LOG);

                    if (pmPrms.common_dt_install.Ticks == 0)
                    {
                        pmPrms.logger.LogWarn("Дата установки прибора не задана, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");

                    }

                    if (pmPrms.common_dt_install > pmPrms.common_dt_cur)
                        pmPrms.logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");

                    //дата установки счетчика
                    int daysBack = -1;
                    DateTime dt_install = DateTime.Now.Date.AddDays(daysBack);//= metersbyport[MetersCounter].dt_install;
                    if (dt_install == null || dt_install.Ticks == 0)
                        dt_install = DateTime.Now.Date.AddDays(daysBack);

                    DateTime dt_cur = DateTime.Now;
                    DateTime dt_last_slice_arr_init = new DateTime();

                    //пусть дата начала = дата установки
                    DateTime date_from = dt_install;

                    for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                    {
                        List<RecordPowerSlice> lrps = new List<RecordPowerSlice>();
                        pmPrms.meter.WriteToLog("RSL: 2. Вошли в цикл, итерация" + tpindex.ToString(), WRITE_LOG);

                        if (dt_install > dt_cur)
                        {
                            pmPrms.meter.WriteToLog("RSL: 3. Дата установки не может быть больше текущей: " +
                                dt_install.ToString(), WRITE_LOG);
                            break;
                        }
                        pmPrms.meter.WriteToLog("RSL: 3. Дата установки корректна: " + dt_install.ToString(), WRITE_LOG);

                        if (bStopServer) return 1;

                        //получим последний (по дате) срез из БД
                        Value latestSliceVal = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[tpindex]);

                        if (latestSliceVal.dt.Ticks > 0)
                        {
                            pmPrms.meter.WriteToLog("RSL: 4. В базе найден последний срез от: " + latestSliceVal.dt.ToString(), WRITE_LOG);
                            TimeSpan timeSpan = new TimeSpan(dt_cur.Ticks - latestSliceVal.dt.Ticks);


                            if (timeSpan.TotalMinutes < SLICE_PER_HALF_AN_HOUR_PERIOD)
                            {
                                pmPrms.meter.WriteToLog("RSL: 4.1. Не прошло 30 минут с момента добавления среза, выхожу из цикла", WRITE_LOG);
                                continue;
                            }
                        }
                        else
                        {
                            pmPrms.meter.WriteToLog("RSL: 4. Последний срез в базе НЕ найден", WRITE_LOG);
                            //получим дату последней инициализации массива срезов
                            if (pmPrms.meter.ReadSliceArrInitializationDate(ref dt_last_slice_arr_init))
                            {
                                if (dt_last_slice_arr_init > date_from && dt_last_slice_arr_init < dt_cur)
                                {
                                    pmPrms.meter.WriteToLog("RSL: 5. Принял за начало дату инициализации: " +
                                    dt_last_slice_arr_init.ToString(), WRITE_LOG);
                                    date_from = dt_last_slice_arr_init;
                                }
                            }
                            else
                            {
                                pmPrms.meter.WriteToLog("RSL: 5. Дата инициализации НЕ найдена", WRITE_LOG);
                            }
                        }

                        //прочие проверки
                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        pmPrms.meter.WriteToLog("RSL: 6. Параметру присвоен GUID, параметр:" + param.name, WRITE_LOG);


                        //уточним начальную дату чтения срезов
                        if (latestSliceVal.dt > date_from && latestSliceVal.dt < dt_cur)
                        {
                            date_from = latestSliceVal.dt.AddMinutes(1);
                            pmPrms.meter.WriteToLog("RSL: 7. Принял за начало дату ПОСЛЕДНЕГО СРЕЗА: " +
                            date_from.ToString(), WRITE_LOG);
                        }

                        if (date_from.Ticks == 0)
                        {
                            pmPrms.meter.WriteToLog("RSL: 8. Начальная дата НЕКОРРЕКТНА, срезы прочитаны НЕ будут: " +
                            date_from.ToString());
                        }
                        else
                        {
                            pmPrms.meter.WriteToLog("RSL: 8. ЗА дату начала приняли:" + date_from.ToString(), WRITE_LOG);
                            pmPrms.meter.WriteToLog("        ЗА дату конца приняли:" + dt_cur.ToString(), WRITE_LOG);
                        }

                        //если срезы из указанного диапазона дат прочитаны успешно
                        if (pmPrms.meter.ReadPowerSlice(date_from, dt_cur, ref lrps, SLICE_PER_HALF_AN_HOUR_PERIOD))
                        {
                            pmPrms.meter.WriteToLog("RSL: 9. Данные прочитаны, осталось занести в базу", WRITE_LOG);
                            foreach (RecordPowerSlice rps in lrps)
                            {
                                if (bStopServer) return 1;

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
                                pmPrms.ServerStorage.AddVariousValues(val);
                                pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                            }

                        }
                        pmPrms.meter.WriteToLog("RSL: 10. Данные успешно занесены в БД", WRITE_LOG);
                    }
                }
                else
                {
                    //meter.WriteToLog("Дата, с которой планируется читать срезы мощности не может быть больше текущей даты");
                }
            }

            return 0;
        }
        private int pollHalfsNew(PollMethodsParams pmPrms)
        {
            if (bStopServer) return 1;

            const byte SLICE_TYPE = 4;                         //тип значения в БД (получасовой/часовой)
            const SlicePeriod SLICE_PERIOD = SlicePeriod.HalfAnHour;

            if (pmPrms.meter.OpenLinkCanal())
            {
                /* Цикл организуется для возможности немедленного прекращения выполнения 
                 * блока чтения срезов в случае ошибки*/
                while (true)
                {
                    //чтение 'дескрипторов' считываемых параметров указанного типа
                    TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid,
                        SLICE_TYPE);
                    if (takenparams.Length == 0) break;

                    if (pmPrms.common_dt_install.Ticks == 0)
                        pmPrms.logger.LogWarn("Дата установки прибора не задана, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");
                    if (pmPrms.common_dt_install > pmPrms.common_dt_cur)
                        pmPrms.logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");


                    string msg = String.Format("ПОЛУчасовые срезы: к считыванию подлежит {0} параметров", takenparams.Length);
                    pmPrms.logger.LogInfo(msg);

                    #region Выбор дат, с которых необходимо читать каждый параметр, создание словаря вида 'Дата-Список параметров с этой датой'

                    //дата установки счетчика
                    DateTime dt_install = pmPrms.metersbyport[pmPrms.MetersCounter].dt_install;
                    DateTime dt_cur = DateTime.Now;

                    //пусть дата начала = дата установки
                    DateTime date_from = dt_install;

                    if (dt_install > dt_cur)
                    {
                        msg = String.Format("ПОЛУчасовые срезы: дата установки прибора ({0}) не может быть больше текущей", dt_install.ToString());
                        pmPrms.logger.LogError(msg);
                        break;
                    }

                    if (bStopServer) return 1;

                    //некоторые счетчики хранят дату инициализации архива (начала учета)
                    DateTime dt_last_slice_arr_init = new DateTime();
                    //получим дату последней инициализации массива срезов (если счетчик поддерживает)
                    if (pmPrms.meter.ReadSliceArrInitializationDate(ref dt_last_slice_arr_init))
                    {
                        msg = String.Format("ПОЛУчасовые срезы: определена дата инициализации архива ({0})",
                            dt_last_slice_arr_init.ToString());
                        pmPrms.logger.LogInfo(msg);
                    }

                    //для каждого считываемого параметра определим дату начала и сопоставим дескриптору
                    //считываемого параметра
                    Dictionary<DateTime, List<TakenParams>> dt_param_dict = new Dictionary<DateTime, List<TakenParams>>();
                    for (int i = 0; i < takenparams.Length; i++)
                    {
                        if (bStopServer) return 1;

                        //получим последний (по дате) срез для читаемого параметра i
                        Value latestSliceVal = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[i]);

                        Param p = pmPrms.ServerStorage.GetParamByGUID(takenparams[i].guid_params);
                        if (p.guid == Guid.Empty)
                        {
                            msg = String.Format("ПОЛУчасовые срезы: ошибка считывания GUIDa параметра {0} из {1} считываемых, параметр: {2}",
                                i, takenparams.Length, p.name);
                            pmPrms.logger.LogError(msg);
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
                                pmPrms.logger.LogInfo(msg);
                                continue;
                            }
                        }
                        else
                        {
                            //meter.WriteToLog("RSL: Последний срез в базе НЕ найден", SEL_DATE_REGION_LOGGING);
                            msg = String.Format("ПОЛУчасовые срезы: последний срез в базе не найден");
                            pmPrms.logger.LogInfo(msg);

                            if (dt_last_slice_arr_init > date_from && dt_last_slice_arr_init < dt_cur)
                            {
                                msg = String.Format("ПОЛУчасовые срезы: дата инициализации архивов ({0}) принята за дату начала",
                                    dt_last_slice_arr_init.ToString());
                                pmPrms.logger.LogInfo(msg);

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
                            pmPrms.logger.LogError(msg);
                            continue;
                        }
                        else
                        {
                            msg = String.Format("ПОЛУчасовые срезы: начальная дата ({0})", date_from.ToString());
                            pmPrms.logger.LogInfo(msg);
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
                        pmPrms.logger.LogError(msg);
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
                            Param p = pmPrms.ServerStorage.GetParamByGUID(tp.guid_params);
                            if (p.guid == Guid.Empty)
                            {
                                msg = String.Format("ПОЛУчасовые срезы: ошибка считывания GUIDa одного из параметров");
                                pmPrms.logger.LogError(msg);
                                continue;
                            }

                            sd.AddValueDescriptor(tp.id, p.param_address, p.channel, SLICE_PERIOD);
                        }

                        sliceDescrList.Add(sd);
                    }

                    #endregion

                    #region Отправка дескрипторов счетчику и запись полученных значений в БД

                    //если срезы прочитаны успешно
                    if (pmPrms.meter.ReadPowerSlice(ref sliceDescrList, dt_cur, SLICE_PERIOD))
                    {
                        //meter.WriteToLog("RSL: Данные прочитаны, осталось занести в базу", LOG_SLICES);
                        foreach (SliceDescriptor su in sliceDescrList)
                        {
                            if (bStopServer) return 1;

                            for (uint i = 0; i < su.ValuesCount; i++)
                            {
                                try
                                {
                                    Value val = new Value();
                                    su.GetValueId(i, ref val.id_taken_params);
                                    su.GetValue(i, ref val.value, ref val.status);
                                    val.dt = su.Date;


                                    /*добавим в БД "разное" значение и обновим dt_last_read*/
                                    pmPrms.ServerStorage.AddVariousValues(val);
                                    pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                                }
                                catch (Exception ex)
                                {
                                    msg = String.Format("ПОЛУчасовые срезы: ошибка перегрупировки параметров, срез ({0}) считан не будет; текст исключения: {1}",
                                        i, ex.Message);
                                    pmPrms.logger.LogError(msg);
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
                        pmPrms.logger.LogError(msg);
                    }

                    #endregion

                    break;
                }
            }
            else
            {
                //ошибка Связь неустановлена
            }

            return 0;
        }
        private int pollHalfsForDatesPrevious(PollMethodsParams pmPrms, DateTime dateFrom, DateTime dateTo)
        {
            if (bStopServer) return 1;

            const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
            const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов
            bool successFlag = false;
            bool lFlag = true;

            //чтение получасовых срезов, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, SLICE_PER_HALF_AN_HOUR_TYPE);

            if (takenparams.Length > 0)
            {
                //читать данные только если прибор ответил
                if (pmPrms.meter.OpenLinkCanal())
                {
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: открыт канал для чтения получасовок ПО ДАТАМ (метод pollHalfsForDates)");

                    DateTime dt_cur = DateTime.Now;

                    DateTime date_from = dateFrom;
                    if (dateFrom > dt_cur) dateFrom = new DateTime(dt_cur.Ticks);

                    DateTime date_to = dateTo;
                    if (date_to > dt_cur) date_to = new DateTime(dt_cur.Ticks);

                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: даты с/по " + date_from.ToString() + "/" + date_to.ToString());

                    //определим возможное кол-во срезов за период
                    TimeSpan span = date_to - date_from;
                    int diff_minutes = (int)Math.Ceiling(span.TotalMinutes);
                    int slicesNumber = diff_minutes / SLICE_PER_HALF_AN_HOUR_PERIOD;
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: требуемое кол-во срезов " + slicesNumber + " за время (мин) " + diff_minutes);

                    List<RecordPowerSlice> lrps = new List<RecordPowerSlice>();
                    for (int takenPrmsIndex = 0; takenPrmsIndex < takenparams.Length; takenPrmsIndex++)
                    {
                        if (bStopServer) return 1;

                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: вошли в цикл перебора считываемых параметров, итерация " + (takenPrmsIndex+1).ToString() + " из " + takenparams.Length);

                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[takenPrmsIndex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        Value[] valuesInDB = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[takenPrmsIndex], date_from, date_to);
                        //для поддержки драйвера 230, который выдает значения с заданного момента до настоящщего времени
                        Value[] valuesInDBToCurrentTime = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[takenPrmsIndex], date_from, DateTime.Now);
                        
                        int valInDbCnt = valuesInDB.Count<Value>();
                        int valInDbCntToCurTime = valuesInDBToCurrentTime.Count<Value>();

                        //счетчик уже имеющихся в БД записей
                        int alreadyExistInDbValueCnt = 0;

                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: число значений в базе за даты (" + date_from.ToShortDateString() + "; " +
    date_to.ToShortDateString() + "): " + valInDbCnt);
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: число значений в базе за даты (" + date_from.ToShortDateString() + "; " +
DateTime.Now.ToShortDateString() + "): " + valInDbCntToCurTime);
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: должно быть срезов сейчас: " + slicesNumber);
                 

                        if (valInDbCnt < slicesNumber)
                        {
  
                             //в случае с классическим драйвером M230 - по боку date_to. Он ищет с date_from до последней записи
                             //но в целом такая ситуация устраивает, в базу записывается сразу на несколько дней
                             //когда опрашивается следующая дата, нужное количество записей в БД уже имеется

                            bool res = false;

                            if (lrps.Count == 0)
                            {
                                res = pmPrms.meter.ReadPowerSlice(date_from, date_to, ref lrps, SLICE_PER_HALF_AN_HOUR_PERIOD);
                                if (lFlag) if (res) pmPrms.logger.LogInfo("RSL: 3. Метод ReadPowerSlice завершен, получено " + lrps.Count + " значений");
                            }

                            if (lrps.Count > 0)
                            {
                                if (lFlag) pmPrms.logger.LogInfo("RSL: 4. Данные для параметра " + (takenPrmsIndex + 1) + " из " + takenparams.Length + " уже получены");
                                successFlag = true;
                            }
                            else
                            {
                                if (lFlag) pmPrms.logger.LogInfo("RSL: 4.  Данные для параметра " + (takenPrmsIndex + 1) + " из " + takenparams.Length + " НЕ получены, переход к след. параметру");
                                continue;
                            }

                            //если срезы из указанного диапазона дат прочитаны успешно
                            foreach (RecordPowerSlice rps in lrps)
                            {
                                if (bStopServer) return 1;

                                Value val = new Value();
                                val.dt = rps.date_time;
                                val.id_taken_params = takenparams[takenPrmsIndex].id;
                                val.status = Convert.ToBoolean(rps.status);

                                if (valuesInDBToCurrentTime.Length > 0)
                                {
                                    if (valuesInDBToCurrentTime.Count<Value>((valDb) => { return valDb.dt == val.dt; }) > 0)
                                    {
                                        //if (lFlag) pmPrms.logger.LogInfo("RSL: 4. Получасовка за " + val.dt.ToString() + " уже есть в базе");
                                        alreadyExistInDbValueCnt++;
                                        continue;
                                    }
                                }


                                switch (param.param_address)
                                {
                                    case 0: { val.value = rps.APlus; break; }
                                    case 1: { val.value = rps.AMinus; break; }
                                    case 2: { val.value = rps.RPlus; break; }
                                    case 3: { val.value = rps.RMinus; break; }
                                    default: continue;
                                }

                                pmPrms.ServerStorage.AddVariousValues(val);
                                pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                            }

                            if (lFlag) pmPrms.logger.LogInfo("RSL: 5. Значения успешно занесены в VariousValues, " + alreadyExistInDbValueCnt + " из " + lrps.Count + " пропущены, так как уже существуют");

                        }
                        else
                        {
                            //для данного параметра все получасовки в базе
                            if (lFlag) pmPrms.logger.LogInfo("RSL: 3: для данного параметра собраны все получасовки на текущий момент");
                        }
                    }
                }
                else
                {
                    if (lFlag) pmPrms.logger.LogInfo("RSL: 0: не удалось открыть канал...");
                    return 3;
                }
            }
            else
            {
                return 2;
            }

            return successFlag ? 10 : 0;
        }
        private int pollHalfsForDates(PollMethodsParams pmPrms, DateTime dateFrom, DateTime dateTo)
        {
            if (bStopServer) return 1;

            const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
            const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов
            bool successFlag = false;
            bool lFlag = true;

            //чтение получасовых срезов, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, SLICE_PER_HALF_AN_HOUR_TYPE);

            if (takenparams.Length > 0)
            {
                //читать данные только если прибор ответил
                if (pmPrms.meter.OpenLinkCanal())
                {
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: открыт канал для чтения получасовок ПО ДАТАМ (метод pollHalfsForDates)");

                    DateTime dt_cur = DateTime.Now;

                    DateTime date_from = dateFrom;
                    if (dateFrom > dt_cur) dateFrom = new DateTime(dt_cur.Ticks);

                    DateTime date_to = dateTo;
                    if (date_to > dt_cur) date_to = new DateTime(dt_cur.Ticks);

                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: даты с/по " + date_from.ToString() + "/" + date_to.ToString());



                    //определим дату самого раннего из последних параметров записанных в базу
                    Value earliestValue = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[0]);
                    for (int tpInd = 0; tpInd < takenparams.Length; tpInd++)
                    {
                        Value tmpVal = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[tpInd]);
                        if (tmpVal.dt.Ticks == 0)
                            continue;
                        else if (tmpVal.dt < earliestValue.dt) earliestValue = tmpVal;
                    }
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: самый ранний из последних записанных срезов от " + earliestValue.ToString());


                    //прочитаем срезы от самого раннего до date_to



                    //определим возможное кол-во срезов за период
                    TimeSpan span = date_to - date_from;
                    int diff_minutes = (int)Math.Ceiling(span.TotalMinutes);
                    int slicesNumber = diff_minutes / SLICE_PER_HALF_AN_HOUR_PERIOD;
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: требуемое кол-во срезов " + slicesNumber + " за время (мин) " + diff_minutes);

                    List<RecordPowerSlice> lrps = new List<RecordPowerSlice>();
                    for (int takenPrmsIndex = 0; takenPrmsIndex < takenparams.Length; takenPrmsIndex++)
                    {
                        if (bStopServer) return 1;

                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: вошли в цикл перебора считываемых параметров, итерация " + (takenPrmsIndex + 1).ToString() + " из " + takenparams.Length);

                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[takenPrmsIndex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        Value[] valuesInDB = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[takenPrmsIndex], date_from, date_to);
                        //для поддержки драйвера 230, который выдает значения с заданного момента до времени последнего среза
                        //игнорируя date_to. В драйвере 234го это поправлено.
                        Value[] valuesInDBToCurrentTime = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[takenPrmsIndex], date_from, DateTime.Now);

                        int valInDbCnt = valuesInDB.Count<Value>();
                        int valInDbCntToCurTime = valuesInDBToCurrentTime.Count<Value>();

                        //счетчик уже имеющихся в БД записей
                        int alreadyExistInDbValueCnt = 0;

                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: число значений в базе за даты (" + date_from.ToShortDateString() + "; " +
    date_to.ToShortDateString() + "): " + valInDbCnt);
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: число значений в базе за даты (" + date_from.ToShortDateString() + "; " +
DateTime.Now.ToShortDateString() + "): " + valInDbCntToCurTime);
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: должно быть срезов сейчас: " + slicesNumber);


                        if (valInDbCnt < slicesNumber)
                        {

                            //в случае с классическим драйвером M230 - по боку date_to. Он ищет с date_from до последней записи
                            //но в целом такая ситуация устраивает, в базу записывается сразу на несколько дней
                            //когда опрашивается следующая дата, нужное количество записей в БД уже имеется

                            bool res = false;

                            if (lrps.Count == 0)
                            {
                                res = pmPrms.meter.ReadPowerSlice(date_from, date_to, ref lrps, SLICE_PER_HALF_AN_HOUR_PERIOD);
                                if (lFlag) if (res) pmPrms.logger.LogInfo("RSL: 3. Метод ReadPowerSlice завершен, получено " + lrps.Count + " значений");
                            }

                            if (lrps.Count > 0)
                            {
                                if (lFlag) pmPrms.logger.LogInfo("RSL: 4. Данные для параметра " + (takenPrmsIndex + 1) + " из " + takenparams.Length + " уже получены");
                                successFlag = true;
                            }
                            else
                            {
                                if (lFlag) pmPrms.logger.LogInfo("RSL: 4.  Данные для параметра " + (takenPrmsIndex + 1) + " из " + takenparams.Length + " НЕ получены, переход к след. параметру");
                                continue;
                            }

                            //если срезы из указанного диапазона дат прочитаны успешно
                            foreach (RecordPowerSlice rps in lrps)
                            {
                                if (bStopServer) return 1;

                                Value val = new Value();
                                val.dt = rps.date_time;
                                val.id_taken_params = takenparams[takenPrmsIndex].id;
                                val.status = Convert.ToBoolean(rps.status);

                                if (valuesInDBToCurrentTime.Length > 0)
                                {
                                    if (valuesInDBToCurrentTime.Count<Value>((valDb) => { return valDb.dt == val.dt; }) > 0)
                                    {
                                        //if (lFlag) pmPrms.logger.LogInfo("RSL: 4. Получасовка за " + val.dt.ToString() + " уже есть в базе");
                                        alreadyExistInDbValueCnt++;
                                        continue;
                                    }
                                }


                                switch (param.param_address)
                                {
                                    case 0: { val.value = rps.APlus; break; }
                                    case 1: { val.value = rps.AMinus; break; }
                                    case 2: { val.value = rps.RPlus; break; }
                                    case 3: { val.value = rps.RMinus; break; }
                                    default: continue;
                                }

                                pmPrms.ServerStorage.AddVariousValues(val);
                                pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                            }

                            if (lFlag) pmPrms.logger.LogInfo("RSL: 5. Значения успешно занесены в VariousValues, " + alreadyExistInDbValueCnt + " из " + lrps.Count + " пропущены, так как уже существуют");

                        }
                        else
                        {
                            //для данного параметра все получасовки в базе
                            if (lFlag) pmPrms.logger.LogInfo("RSL: 3: для данного параметра собраны все получасовки на текущий момент");
                        }
                    }
                }
                else
                {
                    if (lFlag) pmPrms.logger.LogInfo("RSL: 0: не удалось открыть канал...");
                    return 3;
                }
            }
            else
            {
                return 2;
            }

            return successFlag ? 10 : 0;
        }
        private int pollHalfsM230New(PollMethodsParams pmPrms)
        {
            DateTime dtCur = DateTime.Now.Date;
            DateTime dtStart = new DateTime(dtCur.Year, dtCur.Month, dtCur.Day, 0, 0, 0);
            DateTime dtEnd = new DateTime(dtCur.Year, dtCur.Month, dtCur.Day, 23, 59, 59);

            return pollHalfsForDates(pmPrms, dtStart, dtEnd);
        }
        //TODO: refactor 
        #endregion

        private int pollHalfsAutomatically(PollMethodsParams pmPrms)
        {
            if (bStopServer) return 1;

            const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
            const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов
            bool successFlag = false;
            bool lFlag = true;

            //чтение получасовых срезов, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, SLICE_PER_HALF_AN_HOUR_TYPE);

            if (takenparams.Length > 0)
            {
                //читать данные только если прибор ответил
                if (pmPrms.meter.OpenLinkCanal())
                {
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: открыт канал для чтения получасовок АВТО (метод pollHalfsAutomatically)");

                    DateTime dt_cur = DateTime.Now;
                    DateTime date_from = new DateTime(dt_cur.Year, dt_cur.Month, dt_cur.Day, 0, 0, 0);
                    DateTime date_to = new DateTime(dt_cur.Year, dt_cur.Month, dt_cur.Day, 23, 31, 0);

                    //определим сколько срезов должно быть на текущий момент
                    TimeSpan span = dt_cur - date_from;
                    int diff_minutes = (int)Math.Ceiling(span.TotalMinutes);
                    int slicesNumber = diff_minutes / SLICE_PER_HALF_AN_HOUR_PERIOD + 1;
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: сейчас должно быть " + slicesNumber + "срезов за время (мин) " + diff_minutes);

                    //определим дату самого раннего из последних параметров записанных в базу за сегодня
                    //а также какое кол-во записей сделано
                    Value earliestValue = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[0]);
                    int leastValueCnt = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[0], date_from, DateTime.Now).Length;

                    for (int tpInd = 0; tpInd < takenparams.Length; tpInd++)
                    {
                        Value tmpVal = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[tpInd]);
                        Value[] valuesInDBToCurrentTime = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[tpInd], date_from, DateTime.Now);

                        if (tmpVal.dt.Ticks == 0)
                            continue;
                        else
                            if (tmpVal.dt < earliestValue.dt)
                            {
                                earliestValue = tmpVal;
                                leastValueCnt = valuesInDBToCurrentTime.Length;
                            }
                    }

                    //!!!
                    if (earliestValue.dt.Ticks > 0)
                        date_from = earliestValue.dt;

                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: самый ранний из последних записанных срезов от " + earliestValue.ToString());
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: за это время записей сделано " + leastValueCnt.ToString());

                    //прочитаем срезы от самого раннего до date_to
                    bool res = false;
                    List<RecordPowerSlice> lrps = new List<RecordPowerSlice>();
                    if (leastValueCnt < slicesNumber)
                    {         
                        res = pmPrms.meter.ReadPowerSlice(date_from, date_to, ref lrps, SLICE_PER_HALF_AN_HOUR_PERIOD);
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки:  метод ReadPowerSlice завершен, получено " + lrps.Count + " значений");
                    }

                    if (!res)
                    {
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: метод ReadPowerSlice вернул 0 значений, выход");
                        return 0;
                    }

                    //если срезы из указанного диапазона дат прочитаны успешно
                    foreach (RecordPowerSlice rps in lrps)
                    {
                        if (bStopServer) return 1;


                        for (int tpInd = 0; tpInd < takenparams.Length; tpInd++)
                        {
                            Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpInd].guid_params);
                            if (param.guid == Guid.Empty) continue;

                            Value val = new Value();
                            val.dt = rps.date_time;
                            val.id_taken_params = takenparams[tpInd].id;
                            val.status = Convert.ToBoolean(rps.status);

                            Value[] valuesInDBToCurrentTime = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[tpInd], date_from, DateTime.Now);

                            if (valuesInDBToCurrentTime.Length > 0)
                            {
                                if (valuesInDBToCurrentTime.Count<Value>((valDb) => { return valDb.dt == val.dt; }) > 0)
                                {
                                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: получасовка за " + val.dt.ToString() + " уже есть в базе");
                                    continue;
                                }
                            }

                            switch (param.param_address)
                            {
                                case 0: { val.value = rps.APlus; break; }
                                case 1: { val.value = rps.AMinus; break; }
                                case 2: { val.value = rps.RPlus; break; }
                                case 3: { val.value = rps.RMinus; break; }
                                default: continue;
                            }

                            pmPrms.ServerStorage.AddVariousValues(val);
                            pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                        }
                    }
                }
                else
                {
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: не удалось открыть канал...");
                    return 3;
                }
            }
            else
            {
                return 2;
            }

            return successFlag ? 10 : 0;
        }
        private int pollHalfsForDate(PollMethodsParams pmPrms, DateTime date)
        {
            if (bStopServer) return 1;

            const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
            const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов
            bool successFlag = false;
            bool lFlag = true;

            //чтение получасовых срезов, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, SLICE_PER_HALF_AN_HOUR_TYPE);

            if (takenparams.Length > 0)
            {
                //читать данные только если прибор ответил
                if (pmPrms.meter.OpenLinkCanal())
                {
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: открыт канал для чтения получасовок за день (метод pollHalfsForDate)");

                    DateTime dt_cur = DateTime.Now;
                    DateTime date_from = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
                    //date_to - захватывает последнюю получасовку, но не более. Если сделать 23:59
                    //23:59 -> (~24:00) diff_minutes ~= 1440 / 30 = 48. Это следствие округления.
                    //если сделать 23:31 -> округления не произойдет, и получасовка 23:30 будет 47.
                    //однако, мы уже посчитали получасовку, коорая 00:00, поэтому 48-я получасовка - 23:30
                    DateTime date_to = new DateTime(date.Year, date.Month, date.Day, 23, 31, 0);

                    if (date_to > dt_cur) date_to = dt_cur;

                    //определим сколько срезов должно быть за период
                    TimeSpan span = date_to - date_from;
                    int diff_minutes = (int)Math.Ceiling(span.TotalMinutes);
                    int slicesNumber = diff_minutes / SLICE_PER_HALF_AN_HOUR_PERIOD + 1;
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: должно быть " + slicesNumber + "срезов за период за время (мин) " + diff_minutes);

                    //определим существующее кол-во записей по параметрам
                    //если хотябы одного параметра не хватает - грузим все получасовки и выборочно
                    //пишем их в базу
                    int leastValueCnt = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[0], date_from, date_to).Length;
                    for (int tpInd = 0; tpInd < takenparams.Length; tpInd++)
                    {
                        Value[] valuesInDBToCurrentTime = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[tpInd], date_from, date_to);

                        if (valuesInDBToCurrentTime.Length == 0)
                            continue;
                        else
                            if (valuesInDBToCurrentTime.Length < leastValueCnt)
                        {
                            leastValueCnt = valuesInDBToCurrentTime.Length;
                        }
                    }

                    if (leastValueCnt == slicesNumber)
                    {
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки:  за данный период УЖЕ получено " + leastValueCnt + " значений, выход");
                        return 10;
                    }
  
                    //прочитаем срезы от самого раннего до date_to
                    bool res = false;
                    List<RecordPowerSlice> lrps = new List<RecordPowerSlice>();
                    if (leastValueCnt < slicesNumber)
                    {
                        res = pmPrms.meter.ReadPowerSlice(date_from, date_to, ref lrps, SLICE_PER_HALF_AN_HOUR_PERIOD);
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки:  метод ReadPowerSlice завершен, получено " + lrps.Count + " значений");
                    }

                    if (!res)
                    {
                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: метод ReadPowerSlice вернул 0 значений, выход");
                        return 0;
                    }

                    //если срезы из указанного диапазона дат прочитаны успешно
                    foreach (RecordPowerSlice rps in lrps)
                    {
                        if (bStopServer) return 1;


                        for (int tpInd = 0; tpInd < takenparams.Length; tpInd++)
                        {
                            Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpInd].guid_params);
                            if (param.guid == Guid.Empty) continue;

                            Value val = new Value();
                            val.dt = rps.date_time;
                            val.id_taken_params = takenparams[tpInd].id;
                            val.status = Convert.ToBoolean(rps.status);

                            Value[] valuesInDBToCurrentTime = pmPrms.ServerStorage.GetExistsVariousValuesDT(takenparams[tpInd], date_from, date_to);

                            if (valuesInDBToCurrentTime.Length > 0)
                            {
                                if (valuesInDBToCurrentTime.Count<Value>((valDb) => { return valDb.dt == val.dt; }) > 0)
                                {
                                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: получасовка за " + val.dt.ToString() + " уже есть в базе");
                                    continue;
                                }
                            }

                            switch (param.param_address)
                            {
                                case 0: { val.value = rps.APlus; break; }
                                case 1: { val.value = rps.AMinus; break; }
                                case 2: { val.value = rps.RPlus; break; }
                                case 3: { val.value = rps.RMinus; break; }
                                default: continue;
                            }

                            pmPrms.ServerStorage.AddVariousValues(val);
                            pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                        }
                    }
                }
                else
                {
                    if (lFlag) pmPrms.logger.LogInfo("Получасовки: не удалось открыть канал...");
                    return 3;
                }
            }
            else
            {
                return 2;
            }

            return successFlag ? 10 : 0;
        }

        private int pollHours(PollMethodsParams pmPrms)
        {
            #region ЧАСОВЫЕ СРЕЗЫ
            if (bStopServer) return 1;

            const bool LOG_SLICES = false;
            const bool LOG_HOURSLICES_ERRORS = true;
            const bool SEL_DATE_REGION_LOGGING = false;

            const byte SLICE_TYPE = 5;                         //тип значения в БД (получасовой/часовой)
            const SlicePeriod SLICE_PERIOD = SlicePeriod.Hour;

            if (pmPrms.meter.OpenLinkCanal())
            {
                string portStr = pmPrms.m_vport.GetName();
                string mAddr = pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString();
                string mName = pmPrms.metersbyport[pmPrms.MetersCounter].name;
                /* Цикл организуется для возможности немедленного прекращения выполнения 
                 * блока чтения срезов в случае ошибки*/
                while (true)
                {
                    if (bStopServer) return 1;

                    //чтение 'дескрипторов' считываемых параметров указанного типа
                    TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid,
                        SLICE_TYPE);
                    if (takenparams.Length == 0) break;

                    #region Выбор дат, с которых необходимо читать каждый параметр, создание словаря вида 'Дата-Список параметров с этой датой'

                    if (pmPrms.common_dt_install.Ticks == 0)
                        pmPrms.logger.LogWarn("Дата установки прибора не задана, критично для ЧАСОВЫХ СРЕЗОВ");
                    if (pmPrms.common_dt_install > pmPrms.common_dt_cur)
                        pmPrms.logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для ЧАСОВЫХ СРЕЗОВ");

                    //дата установки счетчика
                    DateTime dt_install = pmPrms.metersbyport[pmPrms.MetersCounter].dt_install;
                    DateTime dt_cur = DateTime.Now;

                    //пусть дата начала = дата установки
                    DateTime date_from = dt_install;

                    if (dt_install > dt_cur)
                    {
                        break;
                    }

                    if (bStopServer) return 1;

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
                        if (bStopServer) return 1;

                        string paramName = pmPrms.ServerStorage.GetParamByGUID(takenparams[i].guid_params).name;
                        if (bStopServer) return 1;
                        //получим последний (по дате) срез для читаемого параметра i
                        Value latestSliceVal = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[i]);

                        Param p = pmPrms.ServerStorage.GetParamByGUID(takenparams[i].guid_params);
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
                                pmPrms.logger.LogInfo("Часовые срезы: принял за начало дату инициализации архивов - " + dt_last_slice_arr_init.ToString());
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
                            pmPrms.logger.LogError(s_log);

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
                                pmPrms.logger.LogWarn(s_log);
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
                        pmPrms.logger.LogError(s_log);

                        break;
                    }

                    #endregion

                    #region Подготовка дескрипторов параметров для передачи в драйвер

                    //создадим список дескрипторов срезов и заполним его дескрипторами параметров
                    List<SliceDescriptor> sliceDescrList = new List<SliceDescriptor>();

                    foreach (KeyValuePair<DateTime, List<TakenParams>> pair in dt_param_dict)
                    {
                        if (bStopServer) return 1;

                        DateTime tmpDate = pair.Key;
                        List<TakenParams> tmpTpList = pair.Value;

                        SliceDescriptor sd = new SliceDescriptor(tmpDate);

                        foreach (TakenParams tp in tmpTpList)
                        {
                            Param p = pmPrms.ServerStorage.GetParamByGUID(tp.guid_params);
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
                        pmPrms.logger.LogError(s_log);
                        break;
                    }

                    #endregion

                    #region Отправка дескрипторов счетчику и запись полученных значений в БД

                    //если срезы прочитаны успешно
                    if (pmPrms.meter.ReadPowerSlice(ref sliceDescrList, dt_cur, SLICE_PERIOD))
                    {
                        //meter.WriteToLog("RSL: Данные прочитаны, осталось занести в базу", LOG_SLICES);
                        foreach (SliceDescriptor su in sliceDescrList)
                        {
                            if (bStopServer) return 1;

                            for (uint i = 0; i < su.ValuesCount; i++)
                            {
                                try
                                {
                                    Value val = new Value();
                                    su.GetValueId(i, ref val.id_taken_params);
                                    su.GetValue(i, ref val.value, ref val.status);
                                    val.dt = su.Date;

                                    /*добавим в БД "разное" значение и обновим dt_last_read*/
                                    pmPrms.ServerStorage.AddVariousValues(val);
                                    pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
                                }
                                catch (Exception ex)
                                {
                                    string s_log = String.Format("Часовые срезы: ошибка при обработке прочитанного среза. Содержание исключения: {0}", ex.Message);
                                    pmPrms.logger.LogError(s_log);
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
                        try
                        {
                            string s_log = String.Format("Часовые срезы: метод драйвера ReadPowerSlice вернул false. Аргументы: длина списка дескрипторов {0}, " +
                                "дата начала {1}, период чтения {2}", sliceDescrList.Count, dt_cur.ToString(), SLICE_PERIOD.ToString());
                            pmPrms.logger.LogError(s_log);
                        }
                        catch (Exception ex)
                        {
                            string s_log = String.Format("Часовые срезы: метод драйвера ReadPowerSlice вернул false. Исключение: {0}",
                                ex.Message);
                            pmPrms.logger.LogError(s_log);
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
            return 0;
        }




        // Обеспечивает ручное дочитывание
        private int pollDatesRange(MyEventArgs myEventArgs, MainFormParamsStructure mfPrms, PollMethodsParams pmPrms)
        {

            myEventArgs.metersCount = pmPrms.metersbyport.Length;
            myEventArgs.currentCount = (int)pmPrms.MetersCounter;

            string portStr = pmPrms.m_vport.GetName();
            string mAddr = pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString();
            string mName = pmPrms.metersbyport[pmPrms.MetersCounter].name;

            DateTime dtStart = mfPrms.dtStart.Date;
            DateTime dtEnd = mfPrms.dtEnd.Date;
            TimeSpan diff = dtEnd - dtStart;

            pmPrms.logger.LogInfo("Вычитка данных за интервал дат для " + mName);
            pmPrms.logger.LogInfo("Дата начала: " + dtStart.ToShortDateString());
            pmPrms.logger.LogInfo("Дата конца: " + dtEnd.ToShortDateString());
            pmPrms.logger.LogInfo("Прибор: " + mName + " порт " + pmPrms.m_vport.ToString() + " адрес " + pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString());

            //чтение текущих параметров, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, (byte)mfPrms.paramType);
            pmPrms.logger.LogInfo("Число параметров выбранного типа (" + mfPrms.paramType + "): " + takenparams.Length);


            if (takenparams.Length > 0)
            {
                if (bStopServer) return 1;

                DateTime tmpDateTime = new DateTime(dtStart.Ticks);
                int totalD = (int)diff.TotalDays;

                for (int d = 0; d <= totalD; d++)
                {
                    pmPrms.logger.LogInfo("Дата, за которую идет считывание: " + tmpDateTime.ToShortDateString());
                    float curvalue = 0;

                    if (mfPrms.paramType == 0)
                    {
                        //текущий
                        pollCurrent(pmPrms, tmpDateTime);
                    }
                    else if (mfPrms.paramType == 1)
                    {
                        //суточный
                        pollDaily(pmPrms, tmpDateTime);
                    }
                    else if (mfPrms.paramType == 2)
                    {
                        //месячный
                        return 2;
                    }
                    else if (mfPrms.paramType == 3)
                    {
                        //архивный
                        return 2;
                    }
                    else if (mfPrms.paramType == 4)
                    {
                        //получасовой

                        DateTime dt_start_halfs = new DateTime(tmpDateTime.Year, tmpDateTime.Month, tmpDateTime.Day, 0, 0, 0);
                        DateTime dt_end_halfs = new DateTime(tmpDateTime.Year, tmpDateTime.Month, tmpDateTime.Day, 23, 59, 59);

                        pollHalfsForDate(pmPrms, tmpDateTime);
                    }

                    tmpDateTime = tmpDateTime.AddDays(1);
                }
            }

            return 0;
        }

        #endregion

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


            bool POLLING_ACTIVE = mfPrms.mode == 0 ? true : false;

            string portFullName = "";
            if (data.GetType().Name == "ComPortSettings" && !B_DEBUG_MODE_TCP)
            {
                ComPortSettings portsettings = (ComPortSettings)data;
                m_vport = new ComPort(byte.Parse(portsettings.name), (int)portsettings.baudrate, portsettings.data_bits, portsettings.parity, portsettings.stop_bits, portsettings.write_timeout, portsettings.read_timeout, (byte)portsettings.attempts);

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
                if (mfPrms.mode == 1)
                    metersbyport = ServerStorage.GetMetersByTcpIPGUIDAndParams(PortGUID, mfPrms.paramType, mfPrms.driverName);
                else
                    metersbyport = ServerStorage.GetMetersByTcpIPGUID(PortGUID);
            }

            AnalizatorPollThreadInfo apti = new AnalizatorPollThreadInfo(portFullName);
            apti.metersByPort = metersbyport.Length;
            apti.thread = (Thread)prmsList[1];
            mfPrms.frmAnalizator.addThreadToLiveListOrUpdate(apti);

            loggerThread.Initialize(Logger.DIR_LOGS_MAIN, true, portFullName.Replace(':', '_'));
           // WriteToLog("Meters by port length: " + metersbyport.Length);

            //if (m_vport == null) goto CloseThreadPoint;
            if (metersbyport == null || metersbyport.Length == 0)
            {
                loggerThread.LogWarn("Остановка: к порту не привязаны приборы");

                apti.metersByPort = 0;
                apti.commentList.Add("Остановка: к порту не привязаны приборы");
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
            if (mfPrms.mode == 1 && pollingStarted != null)
                pollingStarted(this, myEventArgs);



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
                    //проблема с 230ми
                    //case "m230": meter = new m230(); break;
                    case "pulsar10": meter = new pulsar10(); break;
                    case "pulsar16": meter = new pulsar16(); break;
                    case "tem4": meter = new tem104(); break;
                    case "tem106": meter = new tem106(); break;
                    case "set4tm_03": meter = new set4tm_03(); break;
                    case "spg76212": meter = new spg76212(); break;
                    case "teplouchet1": meter = new teplouchet1(); break;
                    case "m200": meter = new Mercury200(); break;
                    case "opcretranslator": meter = new OpcRetranslator(); break;
                    case "sayani_kombik": meter = new sayani_kombik(); break;
                    case "m230": meter = new m234(); break;
                    case "m234": meter = new m234(); break;
                    case "m230_stable": meter = new m230(); break;
                    case "um40rtu" : meter = new UM_RTU40(); break;
                    case "elf108": meter = new ElfApatorDriver(); break;
                    case "PulsarM": meter = new PulsarDriver(); break;
                    case "pulsar_teplo": meter = new PulsarDriver(); break;
                    case "pulsar_hvs": meter = new PulsarDriver(); break;
                    case "pulsar_gvs": meter = new PulsarDriver(); break;
                }

                if (meter == null) goto NetxMeter;


                /*если соединяться с конечной точкой вначале, то консольная программа rds не сможет с ней соединиться
                 * поэтому создание порта и подключение к нему осуществляется на первой итерации цикла при условии,
                 * что счетчик не саяны. Предполагаются что на одном порту будут висеть только саяны, если будут другие приборы,
                 * создастся порт и саяны не будут читаться снова.
                 * */

                if (m_vport == null && (typemeter.driver_name != "sayani_kombik")) {
                    TCPIPSettings portsettings = (TCPIPSettings)data;
                    m_vport = new TcpipPort(portsettings.ip_address, (int)portsettings.ip_port, portsettings.write_timeout, portsettings.read_timeout, 50);

                    apti.vp = m_vport;
                    mfPrms.frmAnalizator.addThreadToLiveListOrUpdate(apti);
                }
                else if (m_vport == null && (typemeter.driver_name == "sayani_kombik"))
                {
                    m_vport = new ComPort(byte.Parse("250"), 2400, 8, 1, 1, 1, 1, 1);

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
                if (POLLING_ACTIVE && DM_POLL_ADDR)
                {
                    int status = pollSerialNumber(pmPrms);
                    pmPrms.logger.LogInfo("Прочитал серийный номер со статусом: " + status);
                    if (status == 1) goto CloseThreadPoint;
                }


                //***************************************| Значения ТЕКУЩИЕ (0) |***************************************               
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_CURR && pollingParams.b_poll_current)
                {
                    int status = pollCurrent(pmPrms, DateTime.Now);
                    pmPrms.logger.LogInfo("Прочитал ТЕКУЩИЕ со статусом: " + status);
                    if (status == 1) goto CloseThreadPoint;
                }

                //***************************************| На начало СУТОК (1) |***************************************   
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_DAY && pollingParams.b_poll_day)
                {
                    int status = pollDaily(pmPrms, DateTime.Now);
                    pmPrms.logger.LogInfo("Прочитал СУТОЧНЫЕ со статусом: " + status);
                    if (status == 1) goto CloseThreadPoint;
                }

                //***************************************| На начало МЕСЯЦА (2) |***************************************  
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_MONTH && pollingParams.b_poll_month)
                {
                    int status = pollMonthly(pmPrms);
                    pmPrms.logger.LogInfo("Прочитал МЕСЯЧНЫЕ со статусом: " + status);
                    if (status == 1) goto CloseThreadPoint;
                }

                //***************************************| Значения АРХИВНЫЕ (3) |***************************************  
                if (bStopServer) goto CloseThreadPoint;
                if (false && POLLING_ACTIVE && DM_POLL_ARCHIVE && pollingParams.b_poll_archive)
                {
                    pollArchivesOld(pmPrms);
                }
                if (false && POLLING_ACTIVE && DM_POLL_ARCHIVE && pollingParams.b_poll_archive)
                {
                    pollArchivesNewActual(pmPrms);
                }

                //***************************************| Значения ПОЛУЧАСОВЫЕ (4) |***************************************  
                if (bStopServer) goto CloseThreadPoint;
                if (POLLING_ACTIVE && DM_POLL_HALFANHOUR && pollingParams.b_poll_halfanhour)
                {
                    if (typemeter.driver_name == "set4tm_03")
                    {
                        int status = pollHalfsSet4M230(pmPrms);
                        if (status == 1) goto CloseThreadPoint;
                    }
                    else if (typemeter.driver_name == "m230")
                    {
                        //дочитка за вчера
                        DateTime dtCur = DateTime.Now;
                        DateTime dtStartY = new DateTime(dtCur.Year, dtCur.Month, dtCur.Day, 0, 0, 0).AddDays(-1);

                        int status = pollHalfsForDate(pmPrms, dtStartY);
                        if (status == 1) goto CloseThreadPoint;

                        status = pollHalfsAutomatically(pmPrms);
                        if (status == 1) goto CloseThreadPoint;
                    }
                    else
                    {
                        int status = pollHalfsAutomatically(pmPrms);
                        if (status == 1) goto CloseThreadPoint;
                    }
                }

                //***************************************| Значения ЧАСОВЫЕ (5) |*************************************** 
                if (bStopServer) goto CloseThreadPoint;
                if (false && POLLING_ACTIVE && DM_POLL_HOUR && pollingParams.b_poll_hour)
                {
                    int status = pollHours(pmPrms);
                    if (status == 1) goto CloseThreadPoint;
                }
                


                //********************************************************************************************************************* 
                //***************************************| ДОЧИТКА ЗНАЧЕНИЙ |**********************************************************
                //*********************************************************************************************************************
                if (mfPrms.mode == 1 && typemeter.driver_name == mfPrms.driverName)
                {
                    int status = pollDatesRange(myEventArgs, mfPrms, pmPrms);
                    if (status == 1) goto CloseThreadPoint;                   
                }

            NetxMeter:

                if (!DMTCP_STATIC_METER_NUMBER)
                {
                    MetersCounter++;
                    if (mfPrms.mode != 1 && MetersCounter >= metersbyport.Length)
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
                    else if (mfPrms.mode == 1)
                    {
                        // РЕЖИМ РУЧНОЙ ДОЧИТКИ
                        if (meterPolled != null)
                            meterPolled(this, myEventArgs);

                        if (MetersCounter >= metersbyport.Length)
                        {
                            if (pollingEnded != null)
                            {
                                pollingEnded(this, myEventArgs);
                                bStopServer = true;
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

    public class MyEventArgs:EventArgs
    {
        public int metersCount;
        public bool success;
        public int currentCount;
    }
}
