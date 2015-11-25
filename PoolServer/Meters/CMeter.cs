using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;

using Prizmer.Ports;
using Prizmer.PoolServer;

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
            mLogger.Initialize(m_vport.GetName(), m_address.ToString(), String.Empty, "meters");

            if (doWrite)
            {
                mLogger.LogInfo(str);
                /*
    StreamWriter sw = null;
    FileStream fs = null;
    try
    {
        fs = new FileStream(@"logs\driversSummary.log", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        sw = new StreamWriter(fs, Encoding.Default);
        if (m_vport == null) sw.WriteLine(DateTime.Now.ToString() + ": Unknown port: adress: " + m_address + ": " + str);
        else sw.WriteLine(DateTime.Now.ToString() + ": " + m_vport.GetName() + ": adress: " + m_address + ": " + str);
        sw.Close();
        fs.Close();
    }
    catch
    {
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
 * */
            }
        }
    }
}
