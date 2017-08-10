using System;
using System.Collections.Generic;
using System.Text;

using Prizmer.Meters;
using Prizmer.Meters.iMeters;
using Prizmer.Ports;

using System.Reflection;

/// <summary>
/// Реализует интерфейс базовых функций на основе отражения класса устройства, описанного в библиотеке
/// </summary>
namespace Prizmer.PoolServer
{
    class DriverInterface : CMeter, IMeter
    {
        Object refo = null;
        MethodInfo[] drvmi = null;
        Type drvt = null;

        /// <summary>
        /// Создает объект "Устройство" с реализованным интерфейсом базовых функций
        /// </summary>
        /// <param name="t">Отраженный класс, подгруженный динамически</param>
        public DriverInterface(Type t)
        {
            ConstructorInfo[] ci = t.GetConstructors();

            if (ci.Length != 1)
            {
                throw new Exception("Конструктор \"по умолчанию\" не является единственным");
            }
            else
            {
                //для простоты будем считать, что в подгружаемых классах конструкторы "по-умолчанию"
                refo = ci[0].Invoke(null);
                //получим методы по следующим критериям: 
                drvmi = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                drvt = t;
            }
        }

        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("Init") == 0)
                    {
                        object[] FuncParam = { address, pass, data_vport };
                        m.Invoke(refo, FuncParam);
                        return;
                    }
                }
            }
        }

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("GetTypesForCategory") == 0)
                    {
                        object[] FuncParam = { common_category };
                        return (List<byte>)m.Invoke(refo, FuncParam);
                    }
                }
            }
            return null;
        }

        public bool OpenLinkCanal()
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("OpenLinkCanal") == 0)
                    {
                        return (bool)m.Invoke(refo, null);
                    }
                }
            }
            return false;
        }

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("ReadCurrentValues") == 0)
                    {
                        object[] FuncParam = { param, tarif, recordValue };
                        bool r = (bool)m.Invoke(refo, FuncParam);
                        recordValue = (float)FuncParam[2];
                        return r;
                    }
                }
            }
            return false;
        }

        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("ReadMonthlyValues") == 0)
                    {
                        object[] FuncParam = { dt, param, tarif, recordValue };
                        bool r = (bool)m.Invoke(refo, FuncParam);
                        recordValue = (float)FuncParam[3];
                        return r;
                    }
                }
            }
            return false;
        }

        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("ReadDailyValues") == 0)
                    {
                        object[] FuncParam = { dt, param, tarif, recordValue };
                        bool r = (bool)m.Invoke(refo, FuncParam);
                        recordValue = (float)FuncParam[3];
                        return r;
                    }
                }
            }
            return false;
        }

        public bool SyncTime(DateTime dt)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("SyncTime") == 0)
                    {
                        object[] FuncParam = { dt };
                        return (bool)m.Invoke(refo, FuncParam);
                    }
                }
            }
            return false;
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("ReadSerialNumber") == 0)
                    {
                        object[] FuncParam = { serial_number };
                        bool r = (bool)m.Invoke(refo, FuncParam);
                        serial_number = (string)FuncParam[0];
                        return r;
                    }
                }
            }
            return false;
        }

        public void WriteToLog(string str)
        {
            if (drvt != null)
            {
                foreach (MethodInfo m in drvmi)
                {
                    if (m.Name.CompareTo("WriteToLog") == 0)
                    {
                        object[] FuncParam = { str };
                        m.Invoke(refo, FuncParam);
                        return;
                    }
                }
            }
        }


        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<Meters.iMeters.RecordPowerSlice> listRPS, byte period)
        {
            return false;
        }


        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            return false;
        }


        public bool ReadDailyValues(uint recordId, ushort param, ushort tarif, ref float recordValue)
        {
            return false;
        }


        public bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period)
        {
            return false;
        }
    }
}
