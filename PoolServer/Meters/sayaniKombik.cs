using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;


using Prizmer.Meters.iMeters;
using Prizmer.Meters;
using Prizmer.Ports;


using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

namespace Prizmer.Meters
{
    public class sayani_kombik : CMeter, IMeter
    {
        ~sayani_kombik()
        {
            StopFlag = true;
        }

        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            this.m_address = address;
            
            int tmp = 0;
            if (pass != "" && int.TryParse(pass, out tmp) && tmp > 0)
                readDailyTimeoutInDays = tmp;
            else
                readDailyTimeoutInDays = 3;

            directoryBase = AppDomain.CurrentDomain.BaseDirectory;
   
        }

        int readDailyTimeoutInDays = 3;
        bool StopFlag = false;

        //время ожидания завершения работы утиллиты rds
        const int waitRDSTimeInSec = 12;
        const byte RecordLength = 32;
        const byte bytesFromTheEnd = 32;

        const string DIR_NAME_LIB = "RDS";
        const string DIR_NAME_DUMPS = "Dumps";
        const string DIR_NAME_BATCH = "Batches";
        string directoryBase = "";


        public struct Params
        {
            /**   2 bytes   **/
            public float[] Q;    //Q1-Q2
            public float[] T;    //T1-T4
            public float[] M;    //M1-M4
            public float V5;

            /**   1 byte   **/
            public float[] P;    //P1-P4 


            public int[] NC;     //NC1-NC2
            public int TimeMinutes1;
            public int VoltsBattery;
            public int TimeMinutes2;
            public int Reserved;
        };
        public struct MeterInfo
        {
            public string serialNumber;
        };

        #region Низкоуровневый разбор дампа

        private int GiveMeNextValue(FileStream fs, int valBytesCount)
        {
            try
            {
                int fsPosition = (int)fs.Position;
                byte[] buffer = new byte[fs.Length];
                int bytesRead = fs.Read(buffer, fsPosition, valBytesCount);

                byte[] tmpInt16Buffer = new byte[valBytesCount];
                Array.Copy(buffer, fsPosition, tmpInt16Buffer, 0, valBytesCount);
                //Array.Reverse(tmpInt16Buffer);
                int val = -2;
                if (valBytesCount == 1)
                    return (int)tmpInt16Buffer[0];
                else if (valBytesCount == 2)
                    val = BitConverter.ToInt16(tmpInt16Buffer, 0);

                return val;
            }
            catch (Exception ex)
            {
                return -2;
            }
        }
        private bool GetParamValues(FileStream fs, int bytesFromTheEnd, ref Params prms, ref string strRepresentation)
        {
            strRepresentation = "";

            if (fs != null)
            {
                int lastFileByteIndex = (int)(fs.Length);
                long lastRecordFirstIndex = lastFileByteIndex - bytesFromTheEnd - RecordLength;
                fs.Seek((int)lastRecordFirstIndex, SeekOrigin.Begin);

                prms = new Params();

                prms.Q = new float[2];
                for (int i = 0; i < 2; i++)
                {
                    prms.Q[i] = (float)GiveMeNextValue(fs, 2);
                    strRepresentation += String.Format("Q{0}: {1}{2}; ", i + 1, prms.Q[i], "");
                }

                prms.T = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    prms.T[i] = (float)GiveMeNextValue(fs, 2);
                    strRepresentation += String.Format("T{0}: {1}{2}; ", i + 1, prms.T[i], "");
                }

                prms.M = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    prms.M[i] = (float)GiveMeNextValue(fs, 2);
                    strRepresentation += String.Format("M{0}: {1}{2}; ", i + 1, prms.M[i], "");
                }


                prms.V5 = (float)GiveMeNextValue(fs, 2);
                strRepresentation += String.Format("V{0}: {1}{2}; ", 5, prms.V5, "");

                prms.P = new float[4];
                for (int i = 0; i < 4; i++)
                {
                    prms.P[i] = (float)GiveMeNextValue(fs, 1);
                    strRepresentation += String.Format("P{0}: {1}{2}; ", i + 1, prms.P[i], "");
                }
                prms.NC = new int[2];
                for (int i = 0; i < 2; i++)
                {
                    prms.NC[i] = GiveMeNextValue(fs, 1);

                    string sNCDescription = "";
                    if (((prms.NC[i] >> 0) & 0x01) == 0x01) sNCDescription += "Ошибка термодатчика 1;";
                    if (((prms.NC[i] >> 1) & 0x01) == 0x01) sNCDescription += "Ошибка термодатчика 2;";
                    if (((prms.NC[i] >> 2) & 0x01) == 0x01) sNCDescription += "T1<T2 или T1-T2 меньше порога;";
                    if (((prms.NC[i] >> 3) & 0x01) == 0x01) sNCDescription += "Т2<Tх;";
                    if (((prms.NC[i] >> 4) & 0x01) == 0x01) sNCDescription += "dQ1<0;";
                    if (((prms.NC[i] >> 5) & 0x01) == 0x01) sNCDescription += "нет внешнего питания;";
                    if (((prms.NC[i] >> 6) & 0x01) == 0x01) sNCDescription += "проводилась коррекция времени;";
                    if (((prms.NC[i] >> 7) & 0x01) == 0x01) sNCDescription += "изменялось содержимое EEPROM;";

                    if (sNCDescription == "") sNCDescription = "OK";

                    strRepresentation += String.Format("Вычислитель {0}: {1} [{2}];\n", i + 1, prms.NC[i], sNCDescription);
                }

                prms.TimeMinutes1 = GiveMeNextValue(fs, 1);
                prms.VoltsBattery = GiveMeNextValue(fs, 1);
                prms.TimeMinutes2 = GiveMeNextValue(fs, 1);
                prms.Reserved = GiveMeNextValue(fs, 1);

                strRepresentation += String.Format("Time{0}: {1}{2}; ", 1, prms.TimeMinutes1, "");
                strRepresentation += String.Format("Time{0}: {1}{2}; ", 2, prms.TimeMinutes2, "");
                strRepresentation += String.Format("Battery{0}: {1}{2}; ", "", prms.VoltsBattery, " Volts");
                strRepresentation += String.Format("Reserved{0}: {1}{2}; ", "", prms.Reserved, "");

                return true;
            }

            return false;
        }

        private bool GetMeterInfo(FileStream fs, ref MeterInfo mInfo)
        {
            int meterInfoFirstByte = 0x0123;
            mInfo = new MeterInfo();
            fs.Seek(meterInfoFirstByte, SeekOrigin.Begin);

            try
            {
                int fsPosition = (int)fs.Position;
                byte[] buffer = new byte[fs.Length];

                int bytesRead = fs.Read(buffer, fsPosition, 3);

                byte[] tmpSerialBuffer1 = new byte[1];
                byte[] tmpSerialBuffer2 = new byte[2];
                Array.Copy(buffer, fsPosition + 2, tmpSerialBuffer1, 0, 1);
                Array.Copy(buffer, fsPosition, tmpSerialBuffer2, 0, 2);
                Int16 serial2 = BitConverter.ToInt16(tmpSerialBuffer2, 0);

                mInfo.serialNumber = BitConverter.ToString(tmpSerialBuffer1) + "-" + serial2.ToString();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        #endregion

        private bool DoLibraryExists()
        {
            string libDir = directoryBase + DIR_NAME_LIB;
            string libFn = libDir + "\\" + "rdslib.exe";
            if (!Directory.Exists(libDir) || !File.Exists(libFn))
                return false;

            return true;
        }

        private bool CreateBatchConnectionList(List<FileInfo> fileInfoList, ref List<BatchConnection> batchConnectionList)
        {
            batchConnectionList = new List<BatchConnection>();


            for (int i = 0; i < fileInfoList.Count; i++)
            {
                try
                {
                    FileStream fs = new FileStream(fileInfoList[i].FullName, FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
                    BatchConnection bConn = new BatchConnection(sr.ReadToEnd(), fileInfoList[i].FullName);

                    batchConnectionList.Add(bConn);
                    sr.Close();
                    fs.Close();
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            if (batchConnectionList.Count == 0)
                return false;


            return true;
        }

        private bool ExecuteBatchConnection(BatchConnection batchConn)
        {
            string tmpCmd = batchConn.Command;

            if (!batchConn.ExistsRDS)
                return false;

            // создаем процесс cmd.exe с параметрами "ipconfig /all"
            ProcessStartInfo psiOpt = new ProcessStartInfo(@"cmd.exe");

            psiOpt.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // скрываем окно запущенного процесса
            psiOpt.WindowStyle = ProcessWindowStyle.Hidden;
            psiOpt.RedirectStandardOutput = true;
            psiOpt.RedirectStandardInput = true;
            psiOpt.UseShellExecute = false;
            psiOpt.CreateNoWindow = true;
            // запускаем процесс
            Process procCommand = Process.Start(psiOpt);
            procCommand.StandardInput.WriteLine(tmpCmd);


            for (int t = 0; t < waitRDSTimeInSec; t++)
            {
                if (StopFlag)
                    return false;

                Thread.Sleep(1000);
            }

            try
            {
                procCommand.CloseMainWindow();
            }
            catch (Exception ex)
            { }

            if (File.Exists(batchConn.FileNameDump))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ParseDumpFile(string fileName, ref MeterInfo mi, ref Params prms, bool deleteAfterParse = false)
        {
            mi = new MeterInfo();
            prms = new Params();
            string strValues = "";

            if (!File.Exists(fileName))
                return false;


            FileStream dumpFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            bool tmpRes = false;
            if (GetMeterInfo(dumpFileStream, ref mi))
                if (GetParamValues(dumpFileStream, bytesFromTheEnd, ref prms, ref strValues))
                    tmpRes = true;

            dumpFileStream.Close();

            if (tmpRes && deleteAfterParse)
                DeleteDumpFileAndLogs(fileName);

            return tmpRes;
        }

        private bool GetParamValueFromParams(Params prms, ushort param, ushort tarif, out float recordValue)
        {
            recordValue = -1;

            switch (param)
            {
                case 0:
                    {
                        //тепловая энергия
                        float k = 1;
                        if (tarif > 0 && tarif < 3)
                        {
                            recordValue = prms.Q[tarif - 1] * k;
                            return true;
                        }
                        return false;
                    }
                case 1:
                    {
                        //температура
                        float k = 1;
                        if (tarif > 0 && tarif < 5)
                        {
                            recordValue = prms.T[tarif - 1] * k;
                            return true;
                        }
                        return false;
                    }
                case 2:
                    {
                        //масса
                        float k = 1;
                        if (tarif > 0 && tarif < 5)
                        {
                            recordValue = prms.M[tarif - 1] * k;
                            return true;
                        }
                        return false;
                    }
            }

            return false;
        }

        public bool LatestDumpFileName(string directoryPath, string serialNumberDec, out string fileName, out DateTime dt, 
            string pattern = "*.dat")
        {
            fileName = "";
            dt = new DateTime().Date;

            if (!Directory.Exists(directoryPath))
                return false;

            string[] fileNames = Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);
            if (fileNames.Length == 0)
                return false;

            List<DateTime> dateList = new List<DateTime>();
            for (int i = 0; i < fileNames.Length; i++)
            {
                FileInfo tmpFileInfo = new FileInfo(fileNames[i]);
                string tmpFileName = tmpFileInfo.Name;
                string[] splittedFn = tmpFileName.Split('_');
                if (splittedFn.Length == 0) continue;

                CultureInfo provider = CultureInfo.InvariantCulture;
                DateTime tmpDt = new DateTime();
                if (!DateTime.TryParseExact(splittedFn[0], BatchConnection.DATE_FMT, provider, DateTimeStyles.None, out tmpDt))
                    continue;

                tmpDt = tmpDt.Date;

                dateList.Add(tmpDt);
            }

            List<DateTime> orderedList = new List<DateTime>();
            orderedList = dateList.OrderBy(x => x.Date).ToList();

            DateTime latestDate = dateList[dateList.Count - 1];
            dt = latestDate;
            int latestDateIndex = dateList.FindIndex((x) => { return x.Ticks == latestDate.Ticks; });
            fileName = fileNames[latestDateIndex];

            return true;
        }


        private bool DeleteDumpFileAndLogs(string dumpFileName)
        {

            File.Delete(dumpFileName);

            try
            {
                //разберемся с файлом лога
                string logDir = Path.GetDirectoryName(dumpFileName);
                string logFName = Path.GetFileNameWithoutExtension(dumpFileName);
                string logFullFileName = logDir + "\\" + logFName + ".log";

                if (File.Exists(logFullFileName))
                    File.Delete(logFullFileName);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        #region Методы интерфейса

        public bool OpenLinkCanal()
        {
            return DoLibraryExists();
        }

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            recordValue = -1;

            //находится ли в каталоге утилита rdslib.exe
            if (!DoLibraryExists())
                return false;

            //попытаемся найти BAT файл
            string curBatchDirectory = directoryBase + DIR_NAME_LIB + "\\" + DIR_NAME_BATCH;
            string curBatchFilename = curBatchDirectory + "\\" + m_address + ".bat";

            if (!Directory.Exists(curBatchDirectory) || !File.Exists(curBatchFilename))
                return false;

            string curDumpDir = directoryBase + DIR_NAME_LIB + "\\" + DIR_NAME_DUMPS;
            if (!Directory.Exists(curDumpDir))
                Directory.CreateDirectory(curDumpDir);
            
            //все готово к генерации дампа
            FileInfo batchFileInfo = new FileInfo(curBatchFilename);
            List<FileInfo> batchFIList = new List<FileInfo>();
            batchFIList.Add(batchFileInfo);

            List<BatchConnection> batchConnList = new List<BatchConnection>();
            if (!CreateBatchConnectionList(batchFIList, ref batchConnList))
                return false;

            if (!ExecuteBatchConnection(batchConnList[0]))
                return false;

            MeterInfo tmpMi = new MeterInfo();
            Params tmpPrms = new Params();

            if (!ParseDumpFile(batchConnList[0].FileNameDump, ref tmpMi, ref tmpPrms, true))
                return false;

            if (!GetParamValueFromParams(tmpPrms, param, tarif, out recordValue))
                return false;

            return true;
        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            recordValue = -1;

            MeterInfo tmpMi = new MeterInfo();
            Params tmpPrms = new Params();

            //находится ли в каталоге утилита rdslib.exe
            if (!DoLibraryExists())
                return false;

            //проверим есть ли дамп и старше ли он чем readDailyTimeoutInDays
            string curDumpDir = directoryBase + DIR_NAME_LIB + "\\" + DIR_NAME_DUMPS;
            if (!Directory.Exists(curDumpDir))
                Directory.CreateDirectory(curDumpDir);

            string latestDumpFileName = "";
            DateTime latestDumpDate = new DateTime();
            if (LatestDumpFileName(curDumpDir, m_address.ToString(), out latestDumpFileName, out latestDumpDate))
            {
                if (File.Exists(latestDumpFileName))
                {
                    DateTime dateCur = DateTime.Now.Date;
                    TimeSpan ts = dateCur - latestDumpDate;

                    if (ts.TotalDays < readDailyTimeoutInDays)
                    {
                        return false;
                    }
                    else
                    {
                        DeleteDumpFileAndLogs(latestDumpFileName);
                    }
                }
            }

            //ниже нельзя просто вызвать чтение суточных, т.к. суточные удаляют дамп сразу 
            //после разбора, поэтому дублируем

            //попытаемся найти BAT файл
            string curBatchDirectory = directoryBase + DIR_NAME_LIB + "\\" + DIR_NAME_BATCH;
            string curBatchFilename = curBatchDirectory + "\\" + m_address + ".bat";

            if (!Directory.Exists(curBatchDirectory) || !File.Exists(curBatchFilename))
                return false;

            //все готово к генерации нового дампа
            FileInfo batchFileInfo = new FileInfo(curBatchFilename);
            List<FileInfo> batchFIList = new List<FileInfo>();
            batchFIList.Add(batchFileInfo);

            List<BatchConnection> batchConnList = new List<BatchConnection>();
            if (!CreateBatchConnectionList(batchFIList, ref batchConnList))
                return false;

            if (!ExecuteBatchConnection(batchConnList[0]))
                return false;

            bool DELETE_DUMPS_AFTER_PARSING = false;
            if (!ParseDumpFile(batchConnList[0].FileNameDump, ref tmpMi, ref tmpPrms, DELETE_DUMPS_AFTER_PARSING))
                return false;

            if (!GetParamValueFromParams(tmpPrms, param, tarif, out recordValue))
                return false;

            return true;
        }

        public void WriteToLog(string str, bool doWrite = true)
        {
            return;
        }
        
        #endregion

        #region Unused methods

        int findPackageSign(Queue<byte> queue)
        {
            return 0;
        }
        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            throw new NotImplementedException();
        }
        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
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
        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period)
        {
            return false;
        }
        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            return false;
        }
        public bool SyncTime(DateTime dt)
        {
            return false;
        }
        public bool ReadSerialNumber(ref string serial_number)
        {
            return false;
        }

        #endregion

    }

    public class BatchConnection
    {
        public BatchConnection(string batchContent, string batchFileName = "")
        {
            _batchContentString = batchContent;
            _batchContentStringOrig = batchContent;

            _batchFileName = batchFileName;

            UpdateWithActualData();
        }

        public static string DATE_FMT = "dd-MM-yyyy";

        string _batchFileName;
        public bool HasSourceFile
        {
            get
            {
                if (File.Exists(_batchFileName))
                    return true;
                else
                    return false;
            }
        }

        public string SourceFileName
        {
            get
            {
                if (HasSourceFile)
                {
                    return _batchFileName;
                }
                else
                {
                    return "";
                }
            }
        }

        public string SourceName
        {
            get
            {
                if (HasSourceFile)
                {
                    FileInfo fi = new FileInfo(_batchFileName);
                    return fi.Name;
                }
                else
                {
                    return "";
                }
            }
        }


        string _batchContentStringOrig;
        public string CommandOriginal
        {
            get { return _batchContentStringOrig; }
        }

        string _batchContentString;
        public string Command
        {
            get
            {
                return _batchContentString;
            }
            set
            {
                _batchContentString = value;
            }
        }

        public void Restore()
        {
            _batchContentString = _batchContentStringOrig;
        }

        public string FileNameRDSLib
        {
            get
            {
                if (_batchContentString.Length == 0) return "";
                try
                {
                    return Regex.Match(_batchContentString, "^[^ ]*").Groups[0].Value.Replace("\"", "");
                }
                catch (Exception ex)
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    _batchContentString = Regex.Replace(_batchContentString, "^[^ ]*", "\"" + value + "\"");
                }
                catch (Exception ex)
                {
                    return;
                }

            }
        }
        public string FileNameDB
        {
            get
            {
                try
                {
                    string oOption = Regex.Match(_batchContentString, "-o\"[^ ]*").Groups[0].Value;
                    return Regex.Match(oOption, "\\w:\\\\[^\"]*").Groups[0].Value;
                }
                catch (Exception ex)
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    string oOption = Regex.Match(_batchContentString, "-o\"[^ ]*").Groups[0].Value;
                    oOption = Regex.Replace(oOption, "\\w:\\\\[^\"]*", value);
                    _batchContentString = Regex.Replace(_batchContentString, "-o\"[^ ]*", oOption);
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }
        public string FileNameDump
        {
            get
            {
                if (_batchContentString.Length == 0) return "";
                try
                {
                    return Regex.Match(_batchContentString, "-f\"[^ ]*").Groups[0].Value.Replace("-f", "").Replace("\"", "");
                }
                catch (Exception ex)
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    _batchContentString = Regex.Replace(_batchContentString, "-f\"[^ ]*", "-f\"" + value + "\"");
                }
                catch (Exception ex)
                {
                    return;
                }

            }
        }



        public string FileNameLog
        {
            get
            {
                if (_batchContentString.Length == 0) return "";
                try
                {
                    string tmpLogVal = Regex.Match(_batchContentString, ">\"? ?[^ ?]*").Groups[0].Value.Replace("\"", "");
                    return Regex.Replace(tmpLogVal, ">\"? ?", "");
                }
                catch (Exception ex)
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    _batchContentString = Regex.Replace(_batchContentString, ">\"? ?[^ ?]*", ">\"" + value + "\"");
                }
                catch (Exception ex)
                {
                    return;
                }

            }
        }

        public string SerialNumber
        {
            get
            {
                if (_batchContentString.Length == 0) return "";
                try
                {
                    return Regex.Match(_batchContentString, "-a[^ ]*").Groups[0].Value.Replace("-a", String.Empty);
                }
                catch (Exception ex)
                {
                    return "";
                }
            }
        }
        public string SerialNumberDec2Bytes
        {
            get
            {
                string sn = SerialNumber;
                //отсавляем только последние 4 цифры
                sn = sn.Remove(0, sn.Length - 4);
                int res = Convert.ToInt16(sn, 16);

                sn = res.ToString();

                return sn;
            }

        }

        public bool ExistsRDS
        {
            get
            {
                if (File.Exists(FileNameRDSLib))
                    return true;
                else
                    return false;
            }
        }
        public bool ExistsDump
        {
            get
            {
                if (File.Exists(FileNameDump))
                    return true;
                else
                    return false;
            }
        }

        static string _libFolderName = "";
        static string _dumpFolderName = "";
        static string _logFolderName = "";

        public static string FolderNameLib
        {
            get
            {
                return _libFolderName.Length == 0 ? "RDS" : _libFolderName;
            }
            set
            {
                _libFolderName = value;
            }
        }
        public static string FolderNameDump
        {
            get
            {
                return _dumpFolderName.Length == 0 ? "Dumps" : _dumpFolderName;
            }
            set
            {
                _dumpFolderName = value;
            }
        }
        public static string FolderNameLog
        {
            get
            {
                return _logFolderName.Length == 0 ? FolderNameDump : _logFolderName;
            }
            set
            {
                _logFolderName = value;
            }
        }

        public static byte[] ToByteArray(String HexString)
        {
            if (HexString.Length % 2 != 0)
                HexString = "0" + HexString;

            int NumberChars = HexString.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(HexString.Substring(i, 2), 16);
            }
            return bytes;
        }

        public string GenerateFileName(string postfix, bool serialInHex = true)
        {
            string sn = SerialNumber;
            if (!serialInHex)
                sn = SerialNumberDec2Bytes;

            return DateTime.Now.Date.ToString(BatchConnection.DATE_FMT) + "_" + sn + postfix;
        }

        public void UpdateWithActualData()
        {
            Restore();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory; ;
            FileNameRDSLib = baseDirectory + FolderNameLib + "\\rdslib.exe";
            FileNameDB = baseDirectory + FolderNameLib + "\\4rmd.gdb";

            string dumpsFolder = baseDirectory + FolderNameLib + "\\" + FolderNameDump;
            Directory.CreateDirectory(dumpsFolder);
            FileNameDump = dumpsFolder + "\\" + this.GenerateFileName(".dat", false);

            if (FolderNameLog.Length == 0) FolderNameLog = FolderNameDump;
            string logsFolder = baseDirectory + FolderNameLib + "\\" + FolderNameLog;
            Directory.CreateDirectory(logsFolder);
            FileNameLog = logsFolder + "\\" + this.GenerateFileName(".log", false);

            this._batchContentString = Regex.Match(_batchContentString, ".*log\"?").Groups[0].Value;
        }
    }
}
