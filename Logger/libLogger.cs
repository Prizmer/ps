﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace PollingLibraries.LibLogger
{
    public class Logger
    {
        static string baseDirectory = "logs";
        static string executionDir = "";

        static Thread threadDeleteLogs = null;
        static object _lockerDeleteLogs = new object();
        public static event EventHandler<EventArgs> LogsDeleted;

        public Logger() {
            executionDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        static string getFullBaseDirectory()
        {
            return executionDir + "\\" + baseDirectory;
        }

        struct SenderInfo
        {
            public SenderInfo(string port, string addr, string dName, string metersSerial = "")
            {
                this.port = port;
                this.addr = addr;
                this.driverName = dName;
                this.metersSerial = metersSerial;
            }

            public string port;
            public string addr;
            public string driverName;
            public string metersSerial;
        }

        string[] titlesToPrintArr;

        public static volatile bool bRestrict = false;

        public const string DIR_LOGS_MAIN = "main";
        public const string DIR_LOGS_METERS = "meters";
        public const string DIR_LOGS_PORTS = "ports";
        public const string FNAM_LOGGER_LOG = "loggerErr.log";

        public const int DAYS_TO_STORE_LOGS = 5;

        string workDirectory = "";
        bool isInitialized = false;

        bool byThread = false;


        public static string BaseDirectory
        {
            get {
                return getFullBaseDirectory();
            }
        }

        public void Initialize(string workDirName = "", bool byThread = false, params string[] titlesToPrintArr)
        {
            if (workDirName != String.Empty)
                workDirectory = getFullBaseDirectory() + "\\" + workDirName;
            else
                workDirectory = getFullBaseDirectory();

            this.titlesToPrintArr = titlesToPrintArr;
            Directory.CreateDirectory(workDirectory);

            this.byThread = byThread;

            if (titlesToPrintArr.Length > 0)
                isInitialized = true;
            else
                isInitialized = false;
        }

        private enum MessageType
        {
            ERROR,
            WARN,
            INFO
        }

        public void LogError(string message)
        {
            this.writeToLog(message, MessageType.ERROR);
        }

        public void LogInfo(string message)
        {
            this.writeToLog(message, MessageType.INFO);
        }

        public void LogWarn(string message)
        {
            this.writeToLog(message, MessageType.WARN);
        }

        private void writeToLoggerLog(string msg)
        {
            if (bRestrict) return;

            StreamWriter sw = null;
            string resMsg = String.Format("{0}: {1}", DateTime.Now.ToString(), msg);
            sw = new StreamWriter(getFullBaseDirectory() + @"\" + DateTime.Now.Date.ToShortDateString().Replace(".", "_") + "_" + FNAM_LOGGER_LOG, true, Encoding.Default);
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
        private void writeToLog(string message, MessageType messageType)
        {
            if (bRestrict) return;

            if (!isInitialized)
            {
                writeToLoggerLog("Логгер не проинициализирован, попытка записать " + message);
                return;
            }

            try
            {
                string pathToDir = String.Format(workDirectory + "\\{0}", DateTime.Now.Date.ToShortDateString().Replace(".", "_"));
                Directory.CreateDirectory(pathToDir);

                string logFileName = "\\";
                for (int i = 0; i < titlesToPrintArr.Length; i++)
                    logFileName += titlesToPrintArr[i] + "_";

                if (!byThread)
                    logFileName += "info.log";
                else
                    logFileName += "common_info.log";

                logFileName.Replace(':', '_');

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

        public static void DeleteLogs()
        {
            threadDeleteLogs = new Thread(deleteLogs);
            threadDeleteLogs.Start();
        }


        private static bool isDirReadyForDeletion(DirectoryInfo dirInfo)
        {
            DateTime dirWasCreatedAtDate = dirInfo.CreationTime.Date;
            TimeSpan ts = DateTime.Now.Date - dirWasCreatedAtDate;

            if (ts.TotalDays >= DAYS_TO_STORE_LOGS)
                return true;
            else
                return false;
        }

        private static void deleteLogs()
        {
            lock(_lockerDeleteLogs)
            {
                string[] logDirs = Directory.GetDirectories(getFullBaseDirectory());

                //удалим папки, которые старше N дней
                foreach (string logsSubDirName in logDirs)
                {
                    string[] dateDirs = Directory.GetDirectories(logsSubDirName);
                    DirectoryInfo logsSubDirInfo = new DirectoryInfo(logsSubDirName);

                    foreach (string dateDirName in dateDirs)
                    {
                        DirectoryInfo dateDirInfo = new DirectoryInfo(dateDirName);
                        if (isDirReadyForDeletion(dateDirInfo))
                        { 
                            try
                            {
                                dateDirInfo.Delete(true);
                            }
                            catch (Exception ex)
                            {
                                // на случай, если файл открыт в блокноте например
                            }
                        }
                    }

                    if (logsSubDirInfo.GetFiles().Length == 0)
                    {
                        try
                        {
                            logsSubDirInfo.Delete();
                        }
                        catch (Exception ex)
                        {
                            //
                        }
                    }

                }

                //удалим все файлы базовой дирректории, которые старше N дней
                string[] logFiles = Directory.GetFiles(getFullBaseDirectory());
                foreach (string logFileName in logFiles)
                {
                    FileInfo fInfo = new FileInfo(logFileName);
                    DateTime fileWasCreatedAtDate = fInfo.CreationTime.Date;
                    TimeSpan ts = DateTime.Now.Date - fileWasCreatedAtDate;
                    if (ts.TotalDays > DAYS_TO_STORE_LOGS)
                    {
                        try
                        {
                            fInfo.Delete();
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }

                LogsDeleted?.Invoke(new object(), new EventArgs());
            }
        }

        public static void DeleteLogsSimple()
        {
            string[] logDirs = Directory.GetDirectories(getFullBaseDirectory());      

            foreach (string logsSubDirName in logDirs)
            {
                try
                {
                    Directory.Delete(logsSubDirName, true);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public static void OpenLogsFolder()
        {
            string exeLocation = @Logger.BaseDirectory;
            Process.Start("explorer.exe", exeLocation);
        }
    }
}
