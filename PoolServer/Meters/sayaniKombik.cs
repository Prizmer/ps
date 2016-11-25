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
using System.Xml.Serialization;

namespace Prizmer.Meters
{

    public struct Params
    {
        /**   4 bytes   **/
        public float[] Q;    //Q1-Q2
        public float[] M;    //M1-M4
        public float[] V;

        /**   2 bytes   **/
        public float[] T;    //T1-T4

        /**   1 byte   **/
        public int[] NC;     //NC1-NC2
        public float VoltsBattery;
    };
    public struct MeterInfo
    {
        public string serialNumber;
    };

    [Serializable]
    public struct DumpMeta
    {
        public List<string> paramList;
    }

    public class sayani_kombik : CMeter, IMeter
    {
        public sayani_kombik()
        {
            CreateEEPROMParamList(ref EEPROMParamList);
            CreateHourRecordParamList(ref HourRecordParamList);


            //параметры исполнения процесса cmd
            // создаем процесс cmd.exe с параметрами "ipconfig /all"
            psiOpt = new ProcessStartInfo(@"cmd.exe");

            psiOpt.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
            // скрываем окно запущенного процесса
            psiOpt.WindowStyle = ProcessWindowStyle.Normal;
            psiOpt.RedirectStandardOutput = true;
            psiOpt.RedirectStandardInput = true;
            psiOpt.RedirectStandardError = true;
            psiOpt.UseShellExecute = false;
            psiOpt.CreateNoWindow = true;

            bool baseReplaceRes = BaseReplace();
          }

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

        //если прибор не ответил один раз, он блокируется на это время
        int blockingTimeMinutes = 1;

        //время ожидания завершения работы утиллиты rds
        const int waitRDSTimeInSec = 60;
        const byte RecordLength = 32;
        const byte bytesFromTheEnd = 32;

        const string DIR_NAME_LIB = "RDS";
        const string DIR_NAME_DUMPS = "Dumps";
        const string DIR_NAME_BATCH = "Batches";
        string directoryBase = "";

        // передаем в конструктор тип класса
        XmlSerializer formatter = new XmlSerializer(typeof(DumpMeta));

        List<MeterParam> EEPROMParamList = null;
        public void CreateEEPROMParamList(ref List<MeterParam> paramsList)
        {
            EEPROMParamList = new List<MeterParam>();

            int k1 = 4 * 1000;

            EEPROMParamList.Add(new MeterParam(0x1B8, 4, "Q1", "ГДж", k1));
            EEPROMParamList.Add(new MeterParam(0x1BC, 4, "Q2", "ГДж", k1));

            EEPROMParamList.Add(new MeterParam(0x1C0, 4, "M1", "Т", k1));
            EEPROMParamList.Add(new MeterParam(0x1C4, 4, "M2", "Т", k1));
            EEPROMParamList.Add(new MeterParam(0x1C8, 4, "M3", "Т", k1));
            EEPROMParamList.Add(new MeterParam(0x1CC, 4, "M4", "Т", k1));

            EEPROMParamList.Add(new MeterParam(0x1D0, 4, "V1", "м3", k1));
            EEPROMParamList.Add(new MeterParam(0x1D4, 4, "V2", "м3", k1));
            EEPROMParamList.Add(new MeterParam(0x1D8, 4, "V3", "м3", k1));
            EEPROMParamList.Add(new MeterParam(0x1DC, 4, "V4", "м3", k1));
        }

        List<MeterParam> HourRecordParamList = null;
        public void CreateHourRecordParamList(ref List<MeterParam> paramsList)
        {
            HourRecordParamList = new List<MeterParam>();

            HourRecordParamList.Add(new MeterParam(0, 2, "Q1", "ГДж", 4*1000));
            HourRecordParamList.Add(new MeterParam(2, 2, "Q2", "ГДж", 4*1000));

            HourRecordParamList.Add(new MeterParam(4, 2, "T1", "C", -56, 256));
            HourRecordParamList.Add(new MeterParam(6, 2, "T2", "C", -56, 256));
            HourRecordParamList.Add(new MeterParam(8, 2, "T3", "C", -56, 256));
            HourRecordParamList.Add(new MeterParam(10, 2, "T4", "C", -56, 256));

            HourRecordParamList.Add(new MeterParam(12, 2, "М1", "Л", 4 * 1000));
            HourRecordParamList.Add(new MeterParam(14, 2, "М2", "Л", 4 * 1000));
            HourRecordParamList.Add(new MeterParam(16, 2, "М3", "Л", 4 * 1000));
            HourRecordParamList.Add(new MeterParam(18, 2, "М4", "Л", 4 * 1000));

            HourRecordParamList.Add(new MeterParam(20, 2, "V5", "м3", 4));

            HourRecordParamList.Add(new MeterParam(22, 1, "P1", "0.1*атм", 4));
            HourRecordParamList.Add(new MeterParam(23, 1, "P2", "0.1*атм", 4));
            HourRecordParamList.Add(new MeterParam(24, 1, "P3", "0.1*атм", 4));
            HourRecordParamList.Add(new MeterParam(25, 1, "P4", "0.1*атм", 4));

            HourRecordParamList.Add(new MeterParam(26, 1, "НС1"));
            HourRecordParamList.Add(new MeterParam(27, 1, "НС2"));

            HourRecordParamList.Add(new MeterParam(28, 1, "Tвыч1", "мин"));
            HourRecordParamList.Add(new MeterParam(29, 1, "Uбат", "В", 100));
            HourRecordParamList.Add(new MeterParam(30, 1, "Tвыч2", "мин"));


        }

        //информация о консольном процессе
        ProcessStartInfo psiOpt = null;
        Process procCommand = null;

        public event EventHandler<EventArgs> BatchFileExecutionStartEvent;
        public event EventHandler<EventArgs> BatchFileExecutionEndEvent;
        public event EventHandler<EventArgs> BatchFileTickEvent;

        //останавливает цикл опроса принудительно
        private bool StopFlag = false;

        #region Низкоуровневый разбор дампа

        private string GetNSDescription(int NC)
        {
            string sNCDescription = "";
            if (((NC>> 0) & 0x01) == 0x01) sNCDescription += "Ошибка термодатчика 1;";
            if (((NC>> 1) & 0x01) == 0x01) sNCDescription += "Ошибка термодатчика 2;";
            if (((NC>> 2) & 0x01) == 0x01) sNCDescription += "T1<T2 или T1-T2 меньше порога;";
            if (((NC>> 3) & 0x01) == 0x01) sNCDescription += "Т2<Tх;";
            if (((NC>> 4) & 0x01) == 0x01) sNCDescription += "dQ1<0;";
            if (((NC>> 5) & 0x01) == 0x01) sNCDescription += "нет внешнего питания;";
            if (((NC>> 6) & 0x01) == 0x01) sNCDescription += "проводилась коррекция времени;";
            if (((NC>> 7) & 0x01) == 0x01) sNCDescription += "изменялось содержимое EEPROM;";

            if (sNCDescription == "") sNCDescription = "OK";

            return sNCDescription;
        }

        private float RoundFloat(float a)
        {
            return (float)Math.Round(a, 3, MidpointRounding.AwayFromZero);
        }

        public bool FillParamsStructure(FileStream fsDump, out Params structParams, out string strRepresentation)
        {
            structParams = new Params();
            strRepresentation = "";
            byte[] sourceBytes = null;

            if (fsDump != null)
            {
                try
                {
                    //СНАЧАЛА ВСЕ ПАРАМЕТРЫ ИЗ EEPROM

                    //Q
                    structParams.Q = new float[2];
                    MeterParam Q1 = EEPROMParamList.Find((x) => { return x.PName == "Q1"; });
                    fsDump.Seek(Q1.PIndex, SeekOrigin.Begin);
                    Q1.PValue = GiveMeNextValue(fsDump, Q1.PLength, ref sourceBytes);
                    Q1.PValue /= Q1.Coefficient;
                    structParams.Q[0] = RoundFloat(Q1.PValue);

                    MeterParam Q2 = EEPROMParamList.Find((x) => { return x.PName == "Q2"; });
                    Q2.PValue = GiveMeNextValue(fsDump, Q2.PLength, ref sourceBytes);
                    Q2.PValue /= Q2.Coefficient;
                    structParams.Q[1] = RoundFloat(Q2.PValue);

                    for (int i = 0; i < structParams.Q.Length; i++)
                        strRepresentation += String.Format("Q{0}: {1}{2}; ", i + 1, structParams.Q[i], " " + Q1.PUnit);

                    //M
                    structParams.M = new float[4];
                    MeterParam M1 = EEPROMParamList.Find((x) => { return x.PName == "M1"; });
                    M1.PValue = GiveMeNextValue(fsDump, M1.PLength, ref sourceBytes);
                    M1.PValue /= M1.Coefficient;
                    structParams.M[0] = RoundFloat(M1.PValue);

                    MeterParam M2 = EEPROMParamList.Find((x) => { return x.PName == "M2"; });
                    M2.PValue = GiveMeNextValue(fsDump, M2.PLength, ref sourceBytes);
                    M2.PValue /= M2.Coefficient;
                    structParams.M[1] = RoundFloat(M2.PValue);

                    MeterParam M3 = EEPROMParamList.Find((x) => { return x.PName == "M3"; });
                    M3.PValue = GiveMeNextValue(fsDump, M3.PLength, ref sourceBytes);
                    M3.PValue /= M3.Coefficient;
                    structParams.M[2] = RoundFloat(M3.PValue);

                    MeterParam M4 = EEPROMParamList.Find((x) => { return x.PName == "M4"; });
                    M4.PValue = GiveMeNextValue(fsDump, M4.PLength, ref sourceBytes);
                    M4.PValue /= M4.Coefficient;
                    structParams.M[3] = RoundFloat(M4.PValue);

                    for (int i = 0; i < structParams.M.Length; i++)
                        strRepresentation += String.Format("M{0}: {1}{2}; ", i + 1, structParams.M[i], " " + M1.PUnit);

                    //V
                    structParams.V = new float[4];
                    MeterParam V1 = EEPROMParamList.Find((x) => { return x.PName == "V1"; });
                    V1.PValue = GiveMeNextValue(fsDump, V1.PLength, ref sourceBytes);
                    V1.PValue /= V1.Coefficient;
                    structParams.V[0] = RoundFloat(V1.PValue);

                    MeterParam V2 = EEPROMParamList.Find((x) => { return x.PName == "V2"; });
                    V2.PValue = GiveMeNextValue(fsDump, V2.PLength, ref sourceBytes);
                    V2.PValue /= V2.Coefficient;
                    structParams.V[1] = RoundFloat(V2.PValue);

                    MeterParam V3 = EEPROMParamList.Find((x) => { return x.PName == "V3"; });
                    V3.PValue = GiveMeNextValue(fsDump, V3.PLength, ref sourceBytes);
                    V3.PValue /= V3.Coefficient;
                    structParams.V[2] = RoundFloat(V3.PValue);

                    MeterParam V4 = EEPROMParamList.Find((x) => { return x.PName == "V4"; });
                    V4.PValue = GiveMeNextValue(fsDump, V4.PLength, ref sourceBytes);
                    V4.PValue /= V4.Coefficient;
                    structParams.V[3] = RoundFloat(V4.PValue);

                    for (int i = 0; i < structParams.V.Length; i++)
                        strRepresentation += String.Format("V{0}: {1}{2}; ", i + 1, structParams.V[i], " " + V1.PUnit);

                }
                catch (Exception ex)
                {
                    fsDump.Close();
                    return false;
                }

                try
                {
                    //ТЕПЕРЬ ПЕРЕМЕСТИМСЯ К КРАЙНЕЙ ЧАСОВОЙ ЗАПИСИ 

                    //Внимание! В часовых записях, хранится только приращение величины за последний час (кроме T и служебных параметров)
                    int offsetFromTheEnd = ((int)bytesFromTheEnd + RecordLength) * -1;
                    fsDump.Seek(offsetFromTheEnd, SeekOrigin.End);

                    //T
                    structParams.T = new float[4];
                    MeterParam T1 = HourRecordParamList.Find((x) => { return x.PName == "T1"; });
                    fsDump.Seek(T1.PIndex, SeekOrigin.Current);
                    GiveMeNextValue(fsDump, T1.PLength, ref sourceBytes);
                    if (sourceBytes[1] > 0)
                        T1.PValue = sourceBytes[1] + T1.Coefficient;
                    T1.PValue += (float)sourceBytes[0] / T1.Coefficient2;
                    structParams.T[0] = RoundFloat(T1.PValue);

                    MeterParam T2 = HourRecordParamList.Find((x) => { return x.PName == "T2"; });
                    GiveMeNextValue(fsDump, T2.PLength, ref sourceBytes);
                    if (sourceBytes[1] > 0)
                        T2.PValue = sourceBytes[1] + T2.Coefficient;
                    T2.PValue += (float)sourceBytes[0] / T2.Coefficient2;
                    structParams.T[1] = RoundFloat(T2.PValue);

                    MeterParam T3 = HourRecordParamList.Find((x) => { return x.PName == "T3"; });
                    GiveMeNextValue(fsDump, T3.PLength, ref sourceBytes);
                    if (sourceBytes[1] > 0)
                        T3.PValue = sourceBytes[1] + T3.Coefficient;
                    T3.PValue += (float)sourceBytes[0] / T3.Coefficient2;
                    structParams.T[2] = RoundFloat(T3.PValue);

                    MeterParam T4 = HourRecordParamList.Find((x) => { return x.PName == "T4"; });
                    GiveMeNextValue(fsDump, T4.PLength, ref sourceBytes);
                    if (sourceBytes[1] > 0)
                        T4.PValue = sourceBytes[1] + T4.Coefficient;
                    T4.PValue += (float)sourceBytes[0] / T4.Coefficient2;
                    structParams.T[3] = RoundFloat(T4.PValue);

                    for (int i = 0; i < structParams.T.Length; i++)
                        strRepresentation += String.Format("T{0}: {1}{2}; ", i + 1, structParams.T[i], " " + T1.PUnit);


                    //ETC
                    fsDump.Seek(offsetFromTheEnd, SeekOrigin.End);

                    structParams.NC = new int[2];
                    MeterParam NC1 = HourRecordParamList.Find((x) => { return x.PName == "НС1"; });
                    fsDump.Seek(NC1.PIndex, SeekOrigin.Current);
                    NC1.PValue = GiveMeNextValue(fsDump, NC1.PLength, ref sourceBytes);
                    structParams.NC[0] = (int)NC1.PValue;

                    MeterParam NC2 = HourRecordParamList.Find((x) => { return x.PName == "НС2"; });
                    NC2.PValue = GiveMeNextValue(fsDump, NC2.PLength, ref sourceBytes);
                    NC2.PValue /= NC2.Coefficient;
                    structParams.VoltsBattery = NC2.PValue;


                    MeterParam Ubat = HourRecordParamList.Find((x) => { return x.PName == "Uбат"; });
                    fsDump.Seek(offsetFromTheEnd, SeekOrigin.End);
                    fsDump.Seek(Ubat.PIndex, SeekOrigin.Current);
                    Ubat.PValue = GiveMeNextValue(fsDump, Ubat.PLength, ref sourceBytes);
                    Ubat.PValue /= Ubat.Coefficient;
                    structParams.VoltsBattery = Ubat.PValue;

                    for (int i = 0; i < structParams.NC.Length; i++)
                        strRepresentation += String.Format("Вычислитель {0}: {1} [{2}];\n", i + 1,
                            structParams.NC[i], GetNSDescription(structParams.NC[i]));

                    strRepresentation += String.Format("U батареи: {1}{2}; ", "", structParams.VoltsBattery, " " + Ubat.PUnit);
                }
                catch (Exception ex)
                {
                    fsDump.Close();
                    return false;
                }

                fsDump.Close();
                return true;
            }

            return false;
        }

        private int GiveMeNextValue(FileStream fs, int valBytesCount, ref byte[] sourceBytes)
        {      
            try
            {
                int fsPosition = (int)fs.Position;
                byte[] buffer = new byte[fs.Length];
                int bytesRead = fs.Read(buffer, fsPosition, valBytesCount);

                byte[] tmpIntBuffer = new byte[valBytesCount];
                Array.Copy(buffer, fsPosition, tmpIntBuffer, 0, valBytesCount);
                //Array.Reverse(tmpIntBuffer);

                sourceBytes = new byte[tmpIntBuffer.Length];
                Array.Copy(tmpIntBuffer, sourceBytes, tmpIntBuffer.Length);
  
                int val = -2;
                if (valBytesCount == 1)
                    return (int)tmpIntBuffer[0];
                else if (valBytesCount == 2)
                    val = BitConverter.ToInt16(tmpIntBuffer, 0);
                else if (valBytesCount == 4)
                    val = BitConverter.ToInt32(tmpIntBuffer, 0);

                return val;
            }
            catch (Exception ex)
            {
                return -2;
            }
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

        public bool IsDatFileAvailable(string fileName)
        {
            FileStream testFs;
            if (File.Exists(fileName))
            {
                try
                {
                    testFs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    MeterInfo testMi = new MeterInfo();
                    bool res = GetMeterInfo(testFs, ref testMi);
                    testFs.Close();
                    
                    return res;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void SetStopFlag()
        {
            StopFlag = true;
        }


        string tmpLogString = "";


        public bool ExecuteBatchConnection(BatchConnection batchConn)
        {
            StopFlag = false;

            WriteToRDSLog(batchConn.FileNameLog, "Начат процесс чтения");

            tmpLogString = "";
            if (BatchFileExecutionStartEvent != null)
                BatchFileExecutionStartEvent.Invoke(null, new EventArgs());

            if (!batchConn.ExistsRDS)
                return false;

            // запускаем процесс
            psiOpt.FileName = "\"" + batchConn.FileNameRDSLib + "\"";         
            psiOpt.Arguments = batchConn.ArgumentsForRDS;

            procCommand = Process.Start(psiOpt);
            procCommand.EnableRaisingEvents = true;

            //procCommand.Exited += new EventHandler(procCommand_Exited);
            procCommand.OutputDataReceived += new DataReceivedEventHandler(procCommand_OutputStreamDataReceived);
            procCommand.ErrorDataReceived += new DataReceivedEventHandler(procCommand_OutputStreamDataReceived);

            procCommand.BeginOutputReadLine();
            procCommand.BeginErrorReadLine();

            bool tmpRes = false;
            for (int t = 0; t < waitRDSTimeInSec; t++)
            {
                if (StopFlag)
                    break;

                if (procCommand.HasExited)
                {
                    if (IsDatFileAvailable(batchConn.FileNameDump))
                        tmpRes = true;
                    break;
                }

                Thread.Sleep(1000);

                if (BatchFileTickEvent != null)
                    BatchFileTickEvent.Invoke(null, new EventArgs());
            }

            if (!tmpRes)
            {
                WriteToRDSLog(batchConn.FileNameLog, "Не удалось прочитать данные");
            }

            if (BatchFileExecutionEndEvent != null)
                BatchFileExecutionEndEvent.Invoke(null, new EventArgs());

            if (tmpLogString.Length > 0)
                WriteToRDSLog(batchConn.FileNameLog, tmpLogString);

            try
            {
                procCommand.Close();
            }
            catch (Exception ex)
            {}

            return tmpRes;
        }


        void procCommand_OutputStreamDataReceived(object sender, DataReceivedEventArgs e)
        {
            tmpLogString += e.Data + Environment.NewLine;
        }

        private void WriteToRDSLog(string logFileName, string msg)
        {
            if (msg.Length > 0)
            {
                FileStream fs = new FileStream(logFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(msg + Environment.NewLine);
                sw.Flush();
                sw.Close();
            }
        }

        private bool ParseDumpFile(string fileName, ref MeterInfo mi, ref Params prms, bool deleteAfterParse = false)
        {
            mi = new MeterInfo();
            prms = new Params();
            string strValues = "";

            if (!File.Exists(fileName))
                return false;


            try
            {
                FileStream dumpFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                bool tmpRes = false;
                if (GetMeterInfo(dumpFileStream, ref mi))
                    if (FillParamsStructure(dumpFileStream, out prms, out strValues))
                        tmpRes = true;

                dumpFileStream.Close();

                if (tmpRes && deleteAfterParse)
                    DeleteDumpFileAndLogs(fileName);


                return tmpRes;
            }
            catch (Exception ex)
            {
                return false;
            }
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

        public bool LatestByDateFileInfo(string directoryPath, string serialNumberDec, ref FileInfo fileInfo, string pattern = "*.dat")
        {
            fileInfo = null;

            if (!Directory.Exists(directoryPath))
                return false;

            string[] fileNames = Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly);
            if (fileNames.Length == 0)
                return false;

            FileInfo resFileInfo = null;

            for (int i = 0; i < fileNames.Length; i++)
            {
                if (!fileNames[i].Contains(serialNumberDec)) continue;
                FileInfo tmpFI = new FileInfo(fileNames[i]);

                if (resFileInfo == null || (resFileInfo.LastWriteTime.Date < tmpFI.LastWriteTime.Date))
                    resFileInfo = tmpFI;
            }

            fileInfo = resFileInfo;

            if (resFileInfo != null)
                return true;
            else
                return false;
        }

        public void ReplaceExtensionInFileName(string fullFileName, string newExtenstion, ref string newFullFileName)
        {
            //разберемся с файлом лога
            string logDir = Path.GetDirectoryName(fullFileName);
            string logFName = Path.GetFileNameWithoutExtension(fullFileName);
            string logFullFileName = logDir + "\\" + logFName + newExtenstion;

            newFullFileName = logFullFileName;
        }

        public static bool DeleteDumpDirectory()
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "RDS\\Dumps";
                Directory.Delete(path, true);
                Directory.CreateDirectory(path);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool BaseReplace()
        {
            try
            {
                string newBasePath = directoryBase + "RDS\\Backup\\4rmd.gdb";
                string oldBasePath = directoryBase + "RDS\\4rmd.gdb";
                File.Delete(oldBasePath);
                File.Copy(newBasePath, oldBasePath);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool DeleteDumpFileAndLogs(string dumpFileName)
        {
            try
            {
                File.Delete(dumpFileName);

                //разберемся с файлом лога
                string logFullFileName = "";
                ReplaceExtensionInFileName(dumpFileName, ".log", ref logFullFileName);

                if (File.Exists(logFullFileName))
                    File.Delete(logFullFileName);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        #region Управление метаданными

        public bool SetDumpMeta(string dumpFileName, DumpMeta dumpMeta)
        {
            string metaFileName = "";
            ReplaceExtensionInFileName(dumpFileName, ".xml", ref metaFileName);
            
            FileStream metaFileStream = null;
            try
            {
                metaFileStream = new FileStream(metaFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                formatter.Serialize(metaFileStream, dumpMeta);
                metaFileStream.Close();
                return true;
            }
            catch (Exception ex)
            {
                if (metaFileStream != null)
                    metaFileStream.Close();
                return false;
            }
        }
        public bool GetDumpMeta(string dumpFileName, ref DumpMeta dumpMeta)
        {
            string metaFileName = "";
            ReplaceExtensionInFileName(dumpFileName, ".xml", ref metaFileName);

            FileStream metaFileStream = null;
            try
            {
                metaFileStream = new FileStream(metaFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                dumpMeta = (DumpMeta)formatter.Deserialize(metaFileStream);
                metaFileStream.Close();
                return true;
            }
            catch (Exception ex)
            {
                if (metaFileStream != null)
                    metaFileStream.Close();

                return false;
            }
        }
        public bool DeleteDumpMeta(string dumpFileName)
        {
            try
            {
                File.Delete(dumpFileName);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        string metaPairSeparator = ";";
        public bool DumpMetaParamsExist(string dumpFileName, int param, int tarif)
        {
            DumpMeta dm = new DumpMeta();
            if (GetDumpMeta(dumpFileName, ref dm) && dm.paramList != null && dm.paramList.Count > 0)
            {
                for (int i = 0; i < dm.paramList.Count; i++)
                {
                    string tmp = dm.paramList[i];
                    if (tmp == param + metaPairSeparator + tarif)
                        return true;
                }          
            }

            return false;
        }
        public bool DumpMetaAppendParams(string dumpFileName, int param, int tarif)
        {
            DumpMeta dm = new DumpMeta();
            if (GetDumpMeta(dumpFileName, ref dm) && dm.paramList != null && dm.paramList.Count > 0)
            {
                for (int i = 0; i < dm.paramList.Count; i++)
                {
                    string tmp = dm.paramList[i];
                    if (tmp == param + metaPairSeparator + tarif)
                        return true;
                }

                dm.paramList.Add(param + metaPairSeparator + tarif);
                return SetDumpMeta(dumpFileName, dm);
            }
            else
            {
                dm.paramList = new List<string>();
                dm.paramList.Add(param + metaPairSeparator + tarif);

                return SetDumpMeta(dumpFileName, dm);
            }

            return false;
        }

        #endregion

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

            DeleteDumpFileAndLogs(batchConnList[0].FileNameDump);

            return true;
        }


        /// <summary>
        /// Определяет по коду завершения лога, было ли предыдущее чтение успешным (0). Если нет, то возвращает истину и прибор чтение
        /// игнорируется на время blockingTimeMinutes. Это нужно для того, чтобы при отдельном опросе параметров, на 1 неотвечающий прибор не уходило
        /// много времени. К примеру, чтобы проверить доступность прибора требуется 10 секунд. Если с него считывается 6 параметров по отдельности,
        /// на его опрос уйдет минута. Используя блокировку, мы позволяем другим приборам занять это время.
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="serialNumberDec"></param>
        /// <param name="strTerminationCode"></param>
        /// <returns></returns>
        private bool IsReadingBlocked(int blockingTimeMinutes, string directoryPath, string serialNumberDec, 
            ref string strTerminationCode)
        {
            string latestLogFileName = "";
            DateTime latestLogDate = new DateTime();
            FileInfo latestLogFileInfo = null;
            strTerminationCode = "";

            if (LatestByDateFileInfo(directoryPath, serialNumberDec, ref latestLogFileInfo, "*.log"))
            {
                latestLogDate = latestLogFileInfo.LastWriteTime.Date;
                latestLogFileName = latestLogFileInfo.FullName;

                string logContentString = "";
                FileInfo logFileInfo = new FileInfo(latestLogFileName);

                try
                {
                    FileStream fs = new FileStream(latestLogFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    StreamReader sr = new StreamReader(fs);
                    logContentString = sr.ReadToEnd();
                    sr.Close();
                }
                catch (Exception ex)
                {
                    //при проблемах с открытием лога не стоит блокировать
                    return false;
                }


                try
                {
                    Match m = Regex.Match(logContentString, "terminating with res = \\d*");
                    string s = m.Groups[m.Groups.Count - 1].ToString();
                    s = s.Replace("terminating with res = ", "");
                    strTerminationCode = s;

                    //если в прошлый раз все было хорошо, не станем блокировать
                    if (s == "0") return false;

                    latestLogDate = logFileInfo.LastWriteTime;
                    TimeSpan ts = DateTime.Now - latestLogDate;
                    //если в последний раз был код отличный от нуля и прошло меньше времени, чем время блокировки, заблокируем
                    if (ts.TotalMinutes < blockingTimeMinutes)
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    //при проблемах с разбором лога не стоит блокировать
                    return false;
                }
            }
            else
            {
                //лог не найден, не нужно блокировать
                return false;
            }

        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            recordValue = -1;
            bool DELETE_DUMPS_AFTER_PARSING = false;

            MeterInfo tmpMi = new MeterInfo();
            Params tmpPrms = new Params();

            //находится ли в каталоге утилита rdslib.exe
            if (!DoLibraryExists())
                return false;

            //проверим есть ли дамп и старше ли он чем readDailyTimeoutInDays
            string curDumpDir = directoryBase + DIR_NAME_LIB + "\\" + DIR_NAME_DUMPS;
            if (!Directory.Exists(curDumpDir))
                Directory.CreateDirectory(curDumpDir);

            string LogTerminationCode = "";
            if (IsReadingBlocked(blockingTimeMinutes, curDumpDir, m_address.ToString(), ref LogTerminationCode))
                return false;

            string latestDumpFileName = "";
            DateTime latestDumpDate = new DateTime();
            FileInfo latestDumpFileInfo = null;

            if (LatestByDateFileInfo(curDumpDir, m_address.ToString(), ref latestDumpFileInfo, "*.dat"))
            {
                latestDumpFileName = latestDumpFileInfo.FullName;
                latestDumpDate = latestDumpFileInfo.LastWriteTime.Date;

                if (File.Exists(latestDumpFileName))
                { 
                    DateTime dateCur = DateTime.Now.Date;
                    TimeSpan ts = dateCur - latestDumpDate;

                    //если мы дошли до сюда, то искомого параметра нет в таблице "суточные" (см. mainservice)
                    //чтобы не дергать счетчик, введено ограничение - мы реально запрашиваем дамп со 
                    //только раз в readDailyTimeoutInDays суток, поэтому:
                    //
                    //если прошло меньше чем readDailyTimeoutInDays дней
                    if (ts.TotalDays < readDailyTimeoutInDays)
                    {
                        //прочитаем недостающий параметр из уже существующего дампа
                        //чтобы не дергать счетчик
                        if (ParseDumpFile(latestDumpFileName, ref tmpMi, ref tmpPrms, DELETE_DUMPS_AFTER_PARSING))
                            if (GetParamValueFromParams(tmpPrms, param, tarif, out recordValue))
                                return true;
                    }
                }
            }

            //если мы дошли до сюда, то, либо подошло время обновить дамп, либо проблемы с разбором существующего
            //если прошло менее N дней, но искомого параметра еще нет
            DeleteDumpFileAndLogs(latestDumpFileName);

            //ниже нельзя просто вызвать чтение суточных, т.к. суточные удаляют дамп сразу 
            //после разбора, поэтому дублируем

            //попытаемся найти BAT файл
            string curBatchDirectory = directoryBase + DIR_NAME_LIB + "\\" + DIR_NAME_BATCH;
            string curBatchFilename = curBatchDirectory + "\\" + m_address + ".bat";

            if (!Directory.Exists(curBatchDirectory) || !File.Exists(curBatchFilename))
                return false;

            //ВСЕ ГОТОВО К РЕАЛЬНОМУ ЧТЕНИЮ И ГЕНЕРАЦИИ ДАМПА

            FileInfo batchFileInfo = new FileInfo(curBatchFilename);
            List<FileInfo> batchFIList = new List<FileInfo>();
            batchFIList.Add(batchFileInfo);

            List<BatchConnection> batchConnList = new List<BatchConnection>();
            if (!CreateBatchConnectionList(batchFIList, ref batchConnList))
                return false;

            if (!ExecuteBatchConnection(batchConnList[0]))
                return false;

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

    public class MeterParam
    {
        public MeterParam(int index, int length, string name, string unit = "", float k = 1, float k2 = 1)
        {
            _index = index;
            _length = length;
            _k = k;
            _k2 = k2;

            _name = name;
            _unit = unit;
        }

        int _index;
        int _length;
        public int PIndex
        {
            get { return _index; }
        }

        public int PLength
        {
            get { return _length; }
        }

        string _name;
        public string PName
        {
            get { return _name; }
        }

        string _unit;
        public string PUnit
        {
            get { return _unit; }
        }

        float _k;
        public float Coefficient
        {
            get { return _k;}
            set
            {
                _k = value;
            }
        }
        float _k2;
        public float Coefficient2
        {
            get { return _k2; }
            set
            {
                _k2 = value;
            }
        }

        float _value = 0;
        public float PValue
        {
            get { return _value; }
            set
            {
                _value = value;
            }
        }     
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

        public string ArgumentsForRDS
        {
            get
            {
                string withoutRdsExe = _batchContentString.Replace("\"" +FileNameRDSLib + "\"","");
                string str = Regex.Replace(withoutRdsExe, " ? \\d> ?\".*", "");
                return str;
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
