using System;
using System.Configuration;

using Prizmer.Ports;


using PollingLibraries.LibLogger;

namespace Prizmer.Meters
{
    public class CMeter
    {
        //public VirtualPort m_vport = new ComPort(12, 9600, 8, (byte)System.IO.Ports.Parity.None, (byte)System.IO.Ports.StopBits.One, 500, 500, 0);
        public VirtualPort m_vport = null;
        public uint m_address = 0;

        Logger mLogger = new Logger();

        /// <summary>
        /// Запись в ЛОГ-файл
        /// </summary>
        /// <param name="str"></param>
        public void WriteToLog(string str, bool doWrite = true)
        {
            bool areLogsRestricted = false;
            try
            {
                areLogsRestricted = bool.Parse(ConfigurationManager.AppSettings.GetValues("meterLogsRestricted")[0]);
            }
            catch (Exception ex)
            { }

            if (areLogsRestricted) return;
            mLogger.Initialize(Logger.DIR_LOGS_METERS, false, m_vport.GetName(), m_address.ToString());

            if (doWrite)
            {
                mLogger.LogInfo(str);
            }
        }
    }
}
