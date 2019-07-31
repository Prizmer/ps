using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Prizmer.PoolServer.DataBase;
using Drivers.LibMeter;
using PollingLibraries.LibLogger;


namespace Prizmer.PoolServer
{
    public class PollMethods
    {
        MainService _mainService = null;
        public PollMethods(MainService mainService)
        {
            _mainService = mainService;
            mainService.ReqStopServer += MainService_ReqStopServer;
        }

        ~PollMethods()
        {
            if (_mainService != null)
                _mainService.ReqStopServer -= MainService_ReqStopServer;
        }

        private bool bStopServer = false;
        private void MainService_ReqStopServer()
        {
            bStopServer = true;
        }


        //public void RequestStopServer()
        //{
        //    bStopServer = true;
        //}

        #region Методы опроса параметров разных типов

        public PollingResultStatus pollSerialNumber(PollMethodsParams pmPrms, bool readCurrentErrors = false)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

            // pmPrms.logger.LogInfo("Чтение серийника открыт");
            string serial_number_with_err = String.Empty;
            if (pmPrms.meter.OpenLinkCanal())
            {
                // pmPrms.logger.LogInfo("Канал для чтения серийника открыт");
                Meter mDb = pmPrms.metersbyport[pmPrms.MetersCounter];
                string isEqual = "";


                // ввиду отсутствия в интерфейсе метода принимающего массив байт (кодов ошибок)
                // было принято временное решение ввести локальную систему кодов ошибок
                // и накапливать их в логах, как текущие. От решения нужно отказаться, в н.вр.
                // поддерживается только m230
                float localErrCode = 0;
                if (readCurrentErrors)
                    if (pmPrms.meter.ReadCurrentValues(99, 0, ref localErrCode) && localErrCode > 0)
                    {
                        Directory.CreateDirectory(Logger.BaseDirectory);
                        string pathToDir = String.Format(Logger.BaseDirectory + "\\m_errors");
                        Directory.CreateDirectory(pathToDir);
                        string logFileName = DateTime.Now.Date.ToShortDateString().Replace(".", "_") + "_" + pmPrms.m_vport.GetName() + ".txt";
                        string prefix = mDb.address + "_" + mDb.factory_number_manual + "_" + mDb.name;
                        string msg = "";
                        if (localErrCode == 1)
                            msg += "Напряжение батареи менее 2,65В; ";
                        else if (localErrCode == 2)
                            msg += "Напряжение батареи менее 2,2В; ";
                        else if (localErrCode == 4)
                            msg += "Напряжение батареи менее 2,2В; ";
                        else
                            msg += "Неизвестный код ошибки: " + localErrCode + "; ";

                        FileStream fs = new FileStream(pathToDir + "\\" + logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        string resMsg = String.Format("{0}: {1}", prefix, msg);

                        StreamWriter sw = new StreamWriter(fs, Encoding.Default);
                        sw.WriteLine(resMsg);

                        sw.Close();
                        fs.Close();
                    }

                if (pmPrms.meter.ReadSerialNumber(ref serial_number_with_err))
                {
                    // pmPrms.logger.LogInfo("Серийник прочитан: " + serial_number);

                    // посмотрим содержит ли строка ошибки и если да, занесем из в БД
                    string onlySerial = updateMeterErrInDb(mDb, serial_number_with_err);

                    if (mDb.factory_number_manual == onlySerial)
                        isEqual = "TRUE";
                    else
                        isEqual = "FALSE";

                    pmPrms.ServerStorage.UpdateMeterFactoryNumber(mDb.guid, onlySerial, isEqual);
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
                return PollingResultStatus.OPEN_LINK_CHANNEL_FAULT;
            }

            return PollingResultStatus.OK;
        }

        public PollingResultStatus pollCurrent(PollMethodsParams pmPrms, DateTime currentDT)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

            //чтение текущих параметров, подлежащих чтению, относящихся к конкретному прибору
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, 0);

            if (takenparams.Length > 0)
            {
                //читать данные только если прибор ответил
                if (pmPrms.meter.OpenLinkCanal())
                {
                    for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                    {
                        if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        double curvalue = -1;

                        bool bReadResult = false;
                        if (pmPrms.meter is IMeter2)
                        {
                            IMeter2 meter2 = (IMeter2)pmPrms.meter;
                            bReadResult = meter2.ReadCurrentValues(param.param_address, param.channel, ref curvalue);
                        }
                        else
                        {
                            float tmpFloat = -1f;
                            bReadResult = pmPrms.meter.ReadCurrentValues(param.param_address, param.channel, ref tmpFloat);
                            curvalue = (double)tmpFloat;
                        }

                        if (bReadResult)
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
                    return PollingResultStatus.OPEN_LINK_CHANNEL_FAULT;
                }
            }
            else
            {
                //параметры данного типа не считываются
                return PollingResultStatus.NO_TAKEN_PARAMS;
            }

            return PollingResultStatus.OK;
        }
        public PollingResultStatus pollDaily(PollMethodsParams pmPrms, DateTime date, bool bArchiveType = false)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

            //pmPrms.logger.LogInfo("Polling daily...");

            DateTime curDate = DateTime.Now;
            if (date.Date > curDate.Date) date = curDate.Date;

            DateTime startDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            DateTime endDate = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);

            byte paramType = (byte)(bArchiveType == true ? 3 : 1);
            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, paramType);
            if (takenparams.Length > 0)
            {
                string portStr = pmPrms.m_vport.GetName();
                string mAddr = pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString();
                string mName = pmPrms.metersbyport[pmPrms.MetersCounter].name;

                for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                {
                    if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

                    Value[] lastvalue = pmPrms.ServerStorage.GetExistsDailyValuesDT(takenparams[tpindex], startDate, endDate);
                    if (lastvalue.Length > 0) continue;

                    Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                    if (param.guid == Guid.Empty) continue;

                    if (pmPrms.meter.OpenLinkCanal())
                    {
                        double curvalue = -1;

                        bool bReadResult = false;
                        if (pmPrms.meter is IMeter2)
                        {
                            IMeter2 meter2 = (IMeter2)pmPrms.meter;
                            bReadResult = meter2.ReadDailyValues(date, param.param_address, param.channel, ref curvalue);
                        }
                        else
                        {
                            float tmpFloat = -1f;
                            bReadResult = pmPrms.meter.ReadDailyValues(date, param.param_address, param.channel, ref tmpFloat);
                            curvalue = (double)tmpFloat;
                        }

                        if (bReadResult)
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
                            pmPrms.logger.LogInfo("Счетчик " + mName + " порт " + pmPrms.m_vport.GetName() + " адрес " + pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString() + ";");
                        }
                    }
                    else
                    {
                        string s_log = String.Format("Суточные: не удалось открыть канал связи. Параметр {0} с адресом {1} каналом {2} не прочитан;",
                            param.name, param.param_address, param.channel);
                        pmPrms.logger.LogWarn(s_log);
                        pmPrms.logger.LogInfo("Счетчик " + mName + " порт " + pmPrms.m_vport.GetName() + " адрес " + pmPrms.metersbyport[pmPrms.MetersCounter].address.ToString() + ";");
                    }
                }
            }
            else
            {
                return PollingResultStatus.NO_TAKEN_PARAMS;
            }

            return PollingResultStatus.OK;
        }
        public PollingResultStatus pollMonthly(PollMethodsParams pmPrms, object oDate = null)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;


            DateTime dtStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (oDate != null)
            {
                DateTime dt = (DateTime)oDate;
                dtStart = new DateTime(dt.Year, dt.Month, 1);
            }

            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid, 2);
            if (takenparams.Length > 0)
            {

                for (int tpindex = 0; tpindex < takenparams.Length; tpindex++)
                {
                    if (bStopServer)
                    {
                        return PollingResultStatus.STOP_SERVER_REQUEST;
                    }

                    Value[] lastvalue = pmPrms.ServerStorage.GetExistsMonthlyValuesDT(takenparams[tpindex], dtStart, dtStart);

                    //если значение в БД уже есть, то не читать его из прибора
                    if (lastvalue.Length > 0) continue;

                    //читать данные только если прибор ответил
                    if (pmPrms.meter.OpenLinkCanal())
                    {
                        Param param = pmPrms.ServerStorage.GetParamByGUID(takenparams[tpindex].guid_params);
                        if (param.guid == Guid.Empty) continue;

                        double curvalue = -1;

                        bool bReadResult = false;
                        if (pmPrms.meter is IMeter2)
                        {
                            IMeter2 meter2 = (IMeter2)pmPrms.meter;
                            bReadResult = meter2.ReadMonthlyValues(dtStart, param.param_address, param.channel, ref curvalue);
                        }
                        else
                        {
                            float tmpFloat = -1f;
                            bReadResult = pmPrms.meter.ReadMonthlyValues(dtStart, param.param_address, param.channel, ref tmpFloat);
                            curvalue = (double)tmpFloat;
                        }

                        //чтение месячных параметров
                        if (bReadResult)
                        {
                            Value value = new Value();
                            value.dt = dtStart;
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
                                param.name, param.param_address, param.channel, dtStart.ToString());
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
                return PollingResultStatus.NO_TAKEN_PARAMS;
            }

            return PollingResultStatus.OK;
        }

        public PollingResultStatus pollArchivesOld(PollMethodsParams pmPrms)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

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
                    if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

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
            return PollingResultStatus.OK;
        }
        public PollingResultStatus pollArchivesNewActual(PollMethodsParams pmPrms)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

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
                    if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;
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
            return PollingResultStatus.OK;
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
        //private int pollHalfsNew(PollMethodsParams pmPrms)
        //{
        //    if (bStopServer) return 1;

        //    const byte SLICE_TYPE = 4;                         //тип значения в БД (получасовой/часовой)
        //    const SlicePeriod SLICE_PERIOD = SlicePeriod.HalfAnHour;

        //    if (pmPrms.meter.OpenLinkCanal())
        //    {
        //        /* Цикл организуется для возможности немедленного прекращения выполнения 
        //         * блока чтения срезов в случае ошибки*/
        //        while (true)
        //        {
        //            //чтение 'дескрипторов' считываемых параметров указанного типа
        //            TakenParams[] takenparams = pmPrms.ServerStorage.GetTakenParamByMetersGUIDandParamsType(pmPrms.metersbyport[pmPrms.MetersCounter].guid,
        //                SLICE_TYPE);
        //            if (takenparams.Length == 0) break;

        //            if (pmPrms.common_dt_install.Ticks == 0)
        //                pmPrms.logger.LogWarn("Дата установки прибора не задана, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");
        //            if (pmPrms.common_dt_install > pmPrms.common_dt_cur)
        //                pmPrms.logger.LogWarn("Дата установки прибора не может быть больше текущей даты, критично для ПОЛУЧАСОВЫХ СРЕЗОВ");


        //            string msg = String.Format("ПОЛУчасовые срезы: к считыванию подлежит {0} параметров", takenparams.Length);
        //            pmPrms.logger.LogInfo(msg);

        //            #region Выбор дат, с которых необходимо читать каждый параметр, создание словаря вида 'Дата-Список параметров с этой датой'

        //            //дата установки счетчика
        //            DateTime dt_install = pmPrms.metersbyport[pmPrms.MetersCounter].dt_install;
        //            DateTime dt_cur = DateTime.Now;

        //            //пусть дата начала = дата установки
        //            DateTime date_from = dt_install;

        //            if (dt_install > dt_cur)
        //            {
        //                msg = String.Format("ПОЛУчасовые срезы: дата установки прибора ({0}) не может быть больше текущей", dt_install.ToString());
        //                pmPrms.logger.LogError(msg);
        //                break;
        //            }

        //            if (bStopServer) return 1;

        //            //некоторые счетчики хранят дату инициализации архива (начала учета)
        //            DateTime dt_last_slice_arr_init = new DateTime();
        //            //получим дату последней инициализации массива срезов (если счетчик поддерживает)
        //            if (pmPrms.meter.ReadSliceArrInitializationDate(ref dt_last_slice_arr_init))
        //            {
        //                msg = String.Format("ПОЛУчасовые срезы: определена дата инициализации архива ({0})",
        //                    dt_last_slice_arr_init.ToString());
        //                pmPrms.logger.LogInfo(msg);
        //            }

        //            //для каждого считываемого параметра определим дату начала и сопоставим дескриптору
        //            //считываемого параметра
        //            Dictionary<DateTime, List<TakenParams>> dt_param_dict = new Dictionary<DateTime, List<TakenParams>>();
        //            for (int i = 0; i < takenparams.Length; i++)
        //            {
        //                if (bStopServer) return 1;

        //                //получим последний (по дате) срез для читаемого параметра i
        //                Value latestSliceVal = pmPrms.ServerStorage.GetLatestVariousValue(takenparams[i]);

        //                Param p = pmPrms.ServerStorage.GetParamByGUID(takenparams[i].guid_params);
        //                if (p.guid == Guid.Empty)
        //                {
        //                    msg = String.Format("ПОЛУчасовые срезы: ошибка считывания GUIDa параметра {0} из {1} считываемых, параметр: {2}",
        //                        i, takenparams.Length, p.name);
        //                    pmPrms.logger.LogError(msg);
        //                    continue;
        //                }
        //                else
        //                {
        //                    //string msg = String.Format("RSL: Итерация {3}: Определение даты для параметра {0}; адрес {1}; канал {2}", p.name, p.param_address, p.channel, i);
        //                    //meter.WriteToLog(msg, SEL_DATE_REGION_LOGGING);
        //                }

        //                if (latestSliceVal.dt.Ticks > 0)
        //                {
        //                    //meter.WriteToLog("RSL: В базе найден последний срез от: " + latestSliceVal.dt.ToString(), SEL_DATE_REGION_LOGGING);
        //                    TimeSpan timeSpan = new TimeSpan(dt_cur.Ticks - latestSliceVal.dt.Ticks);

        //                    if (timeSpan.TotalMinutes <= (int)SLICE_PERIOD)
        //                    {
        //                        msg = String.Format("ПОЛУчасовые срезы: Не прошло {0} минут с момента добавления среза {1}, перехожу к следующему параметру",
        //                           (int)SLICE_PERIOD, latestSliceVal.dt);
        //                        pmPrms.logger.LogInfo(msg);
        //                        continue;
        //                    }
        //                }
        //                else
        //                {
        //                    //meter.WriteToLog("RSL: Последний срез в базе НЕ найден", SEL_DATE_REGION_LOGGING);
        //                    msg = String.Format("ПОЛУчасовые срезы: последний срез в базе не найден");
        //                    pmPrms.logger.LogInfo(msg);

        //                    if (dt_last_slice_arr_init > date_from && dt_last_slice_arr_init < dt_cur)
        //                    {
        //                        msg = String.Format("ПОЛУчасовые срезы: дата инициализации архивов ({0}) принята за дату начала",
        //                            dt_last_slice_arr_init.ToString());
        //                        pmPrms.logger.LogInfo(msg);

        //                        date_from = dt_last_slice_arr_init;
        //                    }
        //                }

        //                //уточним начальную дату чтения срезов
        //                if (latestSliceVal.dt > date_from && latestSliceVal.dt < dt_cur)
        //                {
        //                    date_from = latestSliceVal.dt.AddMinutes((double)SLICE_PERIOD);
        //                    //meter.WriteToLog("RSL: Принял за начало дату ПОСЛЕДНЕГО СРЕЗА + 1 минута: " + date_from.ToString(), SEL_DATE_REGION_LOGGING);
        //                }

        //                if (date_from.Ticks == 0)
        //                {
        //                    msg = String.Format("ПОЛУчасовые срезы: начальная дата ({0}) НЕКОРРЕКТНА, срезы параметра прочитаны НЕ будут",
        //                        date_from.ToString());
        //                    pmPrms.logger.LogError(msg);
        //                    continue;
        //                }
        //                else
        //                {
        //                    msg = String.Format("ПОЛУчасовые срезы: начальная дата ({0})", date_from.ToString());
        //                    pmPrms.logger.LogInfo(msg);
        //                }

        //                //добавим пару значений в словарь
        //                if (dt_param_dict.ContainsKey(date_from))
        //                {
        //                    List<TakenParams> takenParamsList = null;
        //                    if (!dt_param_dict.TryGetValue(date_from, out takenParamsList))
        //                    {

        //                    }

        //                    dt_param_dict.Remove(date_from);
        //                    takenParamsList.Add(takenparams[i]);
        //                    dt_param_dict.Add(date_from, takenParamsList);
        //                }
        //                else
        //                {
        //                    List<TakenParams> takenParamsList = new List<TakenParams>();
        //                    takenParamsList.Add(takenparams[i]);
        //                    dt_param_dict.Add(date_from, takenParamsList);
        //                }
        //            }

        //            if (dt_param_dict.Count == 0)
        //            {
        //                msg = String.Format("ПОЛУчасовые срезы: cловарь 'Дата-Дескриптор параметра' пуст. Срезы прочитаны не будут");
        //                pmPrms.logger.LogError(msg);
        //                break;
        //            }

        //            #endregion

        //            #region Подготовка дескрипторов параметров для передачи в драйвер

        //            //создадим список дескрипторов срезов и заполним его дескрипторами параметров
        //            List<SliceDescriptor> sliceDescrList = new List<SliceDescriptor>();

        //            foreach (KeyValuePair<DateTime, List<TakenParams>> pair in dt_param_dict)
        //            {
        //                DateTime tmpDate = pair.Key;
        //                List<TakenParams> tmpTpList = pair.Value;

        //                SliceDescriptor sd = new SliceDescriptor(tmpDate);

        //                foreach (TakenParams tp in tmpTpList)
        //                {
        //                    Param p = pmPrms.ServerStorage.GetParamByGUID(tp.guid_params);
        //                    if (p.guid == Guid.Empty)
        //                    {
        //                        msg = String.Format("ПОЛУчасовые срезы: ошибка считывания GUIDa одного из параметров");
        //                        pmPrms.logger.LogError(msg);
        //                        continue;
        //                    }

        //                    sd.AddValueDescriptor(tp.id, p.param_address, p.channel, SLICE_PERIOD);
        //                }

        //                sliceDescrList.Add(sd);
        //            }

        //            #endregion

        //            #region Отправка дескрипторов счетчику и запись полученных значений в БД

        //            //если срезы прочитаны успешно
        //            if (pmPrms.meter.ReadPowerSlice(ref sliceDescrList, dt_cur, SLICE_PERIOD))
        //            {
        //                //meter.WriteToLog("RSL: Данные прочитаны, осталось занести в базу", LOG_SLICES);
        //                foreach (SliceDescriptor su in sliceDescrList)
        //                {
        //                    if (bStopServer) return 1;

        //                    for (uint i = 0; i < su.ValuesCount; i++)
        //                    {
        //                        try
        //                        {
        //                            Value val = new Value();
        //                            su.GetValueId(i, ref val.id_taken_params);
        //                            su.GetValue(i, ref val.value, ref val.status);
        //                            val.dt = su.Date;


        //                            /*добавим в БД "разное" значение и обновим dt_last_read*/
        //                            pmPrms.ServerStorage.AddVariousValues(val);
        //                            pmPrms.ServerStorage.UpdateMeterLastRead(pmPrms.metersbyport[pmPrms.MetersCounter].guid, DateTime.Now);
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            msg = String.Format("ПОЛУчасовые срезы: ошибка перегрупировки параметров, срез ({0}) считан не будет; текст исключения: {1}",
        //                                i, ex.Message);
        //                            pmPrms.logger.LogError(msg);
        //                            continue;
        //                        }
        //                    }
        //                }
        //                //meter.WriteToLog("RSL: Данные успешно занесены в БД", LOG_SLICES);
        //                //meter.WriteToLog("RSL: ---/ конец чтения срезов /---", LOG_SLICES);
        //            }
        //            else
        //            {
        //                msg = String.Format("ПОЛУчасовые срезы: метод драйвера ReadPowerSlice(ref sliceDescrList, dt_cur, SLICE_PERIOD) вернул false, срезы не прочитаны");
        //                pmPrms.logger.LogError(msg);
        //            }

        //            #endregion

        //            break;
        //        }
        //    }
        //    else
        //    {
        //        //ошибка Связь неустановлена
        //    }

        //    return 0;
        //}
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

                        if (lFlag) pmPrms.logger.LogInfo("Получасовки: вошли в цикл перебора считываемых параметров, итерация " + (takenPrmsIndex + 1).ToString() + " из " + takenparams.Length);

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
        private int pollHalfsM230New(PollMethodsParams pmPrms)
        {
            DateTime dtCur = DateTime.Now.Date;
            DateTime dtStart = new DateTime(dtCur.Year, dtCur.Month, dtCur.Day, 0, 0, 0);
            DateTime dtEnd = new DateTime(dtCur.Year, dtCur.Month, dtCur.Day, 23, 59, 59);

            return pollHalfsForDates(pmPrms, dtStart, dtEnd);
        }
        //TODO: refactor 

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
        #endregion

        public PollingResultStatus pollHalfsAutomatically(PollMethodsParams pmPrms)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

            const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
            const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов
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
                        return PollingResultStatus.OK;
                    }

                    //если срезы из указанного диапазона дат прочитаны успешно
                    foreach (RecordPowerSlice rps in lrps)
                    {
                        if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;


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
                    return PollingResultStatus.OPEN_LINK_CHANNEL_FAULT;
                }
            }
            else
            {
                return PollingResultStatus.NO_TAKEN_PARAMS;
            }

            return PollingResultStatus.OK;
        }
        public PollingResultStatus pollHalfsForDate(PollMethodsParams pmPrms, DateTime date)
        {
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

            const byte SLICE_PER_HALF_AN_HOUR_TYPE = 4;                         //тип значения в БД (получасовой)
            const byte SLICE_PER_HALF_AN_HOUR_PERIOD = 30;                      //интервал записи срезов
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
                        return PollingResultStatus.SEE_DETAILS_IN_CODE_1;
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
                        return PollingResultStatus.OK;
                    }

                    //если срезы из указанного диапазона дат прочитаны успешно
                    foreach (RecordPowerSlice rps in lrps)
                    {
                        if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;


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
                    return PollingResultStatus.OPEN_LINK_CHANNEL_FAULT;
                }
            }
            else
            {
                return PollingResultStatus.NO_TAKEN_PARAMS;
            }

            return PollingResultStatus.OK;
        }

        public PollingResultStatus pollHours(PollMethodsParams pmPrms)
        {
            #region ЧАСОВЫЕ СРЕЗЫ
            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;


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
                    if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

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

                    if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

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
                        if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

                        string paramName = pmPrms.ServerStorage.GetParamByGUID(takenparams[i].guid_params).name;
                        if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;
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
                        if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

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
                            if (bStopServer) return PollingResultStatus.STOP_SERVER_REQUEST;

                            for (uint i = 0; i < su.ValuesCount; i++)
                            {
                                try
                                {
                                    Value val = new Value();
                                    su.GetValueId(i, ref val.id_taken_params);
                                    float tmpValFloat = -1f;
                                    su.GetValue(i, ref tmpValFloat, ref val.status);
                                    val.value = tmpValFloat;
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
                return PollingResultStatus.OPEN_LINK_CHANNEL_FAULT;
            }
            #endregion     
            return PollingResultStatus.OK;
        }

        #endregion

        private string updateMeterErrInDb(Meter mDb, string snWithErrors)
        {
            if (!snWithErrors.Contains("#")) return snWithErrors;

            // распарсим ошибки
            string[] errStrArr = snWithErrors.Split('#');
            string errStr = errStrArr[0];

            if (errStr.Length == 0) return errStrArr[1];

            // строка с ошибками 

            return errStrArr[1];
        }
    }
}
