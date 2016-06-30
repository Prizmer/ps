using System;
using System.Collections.Generic;
using Prizmer.Ports;

namespace Prizmer.Meters.iMeters
{
    /// <summary>
    /// Общие категории считываемых данных
    /// </summary>
    public enum CommonCategory
    {
        /// <summary>
        /// Текущие значения
        /// </summary>
        Current = 1,
        /// <summary>
        /// Значения на начало месяца
        /// </summary>
        Monthly = 2,
        /// <summary>
        /// Внутридневные значения
        /// </summary>
        Inday = 3,
        /// <summary>
        /// Значения на начало суток
        /// </summary>
        Daily = 4
    };


    public enum SlicePeriod
    {
        HalfAnHour = 30,
        Hour = 60
    };


    public struct RecordPowerSlice
    {
        public float APlus;
        public float AMinus;
        public float RPlus;
        public float RMinus;
        public byte status;
        public byte period;
        public DateTime date_time;
    };

    /// <summary>
    /// Содержащий набор полей, достаточный для описание среза данных
    /// любого из приборов.
    /// </summary>
    public class SliceUniversal2
    {
        private float[] valuesFloatArr;
        public uint address;
        public uint channel;
        public uint id;
        public bool Status;
        public DateTime Date;
 

        private const int VAL_NUMB = 8;

        public SliceUniversal2()
        {
            valuesFloatArr = new float[VAL_NUMB];
            for (int i = 0; i < VAL_NUMB; i++)
                valuesFloatArr[i] = 0;

            Status = false;
        }

        public bool AddValue(uint position, float val)
        {
            if (position < VAL_NUMB)
            {
                valuesFloatArr[position] = val;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetValue(uint position, ref float val)
        {
            if (position < VAL_NUMB)
            {
                val = valuesFloatArr[position];
                return true;
            }
            else
            {
                return false;
            }

        }

        public int FloatValuesCount
        {
            get { return VAL_NUMB; }
        }

    };

    /// <summary>
    /// Содержит группу параметров с одинаковой временной меткой (срез)
    /// </summary>
    public class SliceDescriptor
    {
        List<uint> identificators = new List<uint>();
        List<uint> addresses = new List<uint>();
        List<uint> channels = new List<uint>();

        List<float> values = new List<float>();
        List<bool> statuses = new List<bool>();

        SlicePeriod period = SlicePeriod.HalfAnHour;
        public SlicePeriod Period
        {
            get { return this.period; }
        }

        DateTime date = new DateTime();
        public DateTime Date
        {
            get { return date;}
        }
        public SliceDescriptor(DateTime date){
            this.date = date;
        }

        int value_counter = 0;
        public int ValuesCount
        {
            get {return value_counter;}
        }

        public int AddValueDescriptor(uint id, uint addr, uint channel, SlicePeriod period){
            addresses.Add(addr);
            identificators.Add(id);
            channels.Add(channel);

            values.Add(0);
            statuses.Add(false);

            this.period = period;

            return value_counter++;
        }

        public bool InsertValue(uint index, float val, bool status)
        {
            if (index < value_counter)
            {
                values[(int)index] = val;
                statuses[(int)index] = status;
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<uint> GetAddressList()
        {
            List<uint> tmp = new List<uint>();
            tmp.AddRange(addresses);
            return tmp;
        }

        public bool GetValueDescriptor(uint index, 
            ref uint id, ref uint addr, ref uint channel, ref SlicePeriod period)
        {
            if (index < value_counter)
            {
                id = identificators[(int)index];
                addr = addresses[(int)index];
                channel = channels[(int)index];
                period = this.period;
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public bool GetValueId(uint index, ref uint id)
        {
            if (index < value_counter)
            {
                id = identificators[(int)index];
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetValue(uint index,
            ref float value, ref bool status)
        {
            if (index < value_counter)
            {
                value = values[(int)index];
                status = statuses[(int)index];
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetValueChannel(uint index, ref uint channel)
        {
            if (index < value_counter)
            {
                channel = channels[(int)index];
                return true;
            }
            else
            {
                return false;
            }
        }



    };

    /// <summary>
    /// Описывает типовое архивное значение
    /// </summary>
    public struct ArchiveValue
    {
        public int id;
        public DateTime dt;
        public float energy;
        public float volume;
        public int timeOn;
        public int timeErr;
    };

    /// <summary>
    /// Интерфейс драйвера устройства учёта энергоресурсов
    /// </summary>
    public interface IMeter
    {
        /// <summary>
        /// Инициализирует драйвер
        /// </summary>
        /// <param name="address"></param>
        /// <param name="pass"></param>
        /// <param name="data_vport"></param>
        void Init(uint address, string pass, VirtualPort data_vport);

        /// <summary>
        /// Типы относящиеся к категории
        /// </summary>
        /// <param name="common_category">Категория типов</param>
        /// <returns></returns>
        List<byte> GetTypesForCategory(CommonCategory common_category);

        /// <summary>
        /// Открытие канала связи 
        /// </summary>
        /// <returns></returns>
        bool OpenLinkCanal();

        /// <summary>
        /// Чтение текущих значений
        /// </summary>
        /// <param name="values">Возвращаемые данные</param>
        /// <returns></returns>
        bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue);

        /// <summary>
        /// Чтение значений на начало месяца
        /// </summary>
        /// <param name="month">Месяц</param>
        /// <param name="year">Год</param> 
        /// <param name="values">Возвращаемые данные</param>
        /// <returns></returns>
        bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue);

        /// <summary>
        /// Чтение значений на начало суток
        /// </summary>
        /// <param name="day">День</param>
        /// <param name="month">Месяц</param>
        /// <param name="year">Год</param> 
        /// <param name="values">Возвращаемые данные</param>
        /// <returns></returns>
        bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue);

        /// <summary>
        /// Чтения значений на начало суток для счетчиков, в которых невозможно верно преобразовать
        /// дату в идентификатор
        /// </summary>
        /// <param name="recordId"></param>
        /// <param name="param"></param>
        /// <param name="tarif"></param>
        /// <param name="recordValue"></param>
        /// <returns></returns>
        bool ReadDailyValues(uint recordId, ushort param, ushort tarif, ref float recordValue);


        /// <summary>
        /// Старая версия метода чтения срезов (оставлена для совместимости)
        /// </summary>
        /// <param name="dt_begin">начальная дата</param>
        /// <param name="dt_end">конечная дата</param>
        /// <param name="listRPS"></param>
        /// <param name="period">период инициализации (30, 60 и т.д.)</param>
        /// <returns></returns>
        bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<RecordPowerSlice> listRPS, byte period);

        /// <summary>
        /// Основной метод чтения срезов (значений на определенный момент времени) - начиная 9/3/15
        /// </summary>
        /// <param name="dt_begin">начальная дата</param>
        /// <param name="dt_end">конечная дата</param>
        /// <param name="pAddrList">список, содержащий адреса параметров</param>
        /// <param name="period">определяет тип среза (часовой, полу-)</param>
        /// <param name="sliceUniversalList">список структур типа 'Универсальный срез'</param>
        /// <returns></returns>
        bool ReadPowerSlice(ref List<SliceDescriptor> sliceUniversalList, DateTime dt_end, SlicePeriod period);

        /// <summary>
        /// Возвращает дату инициализации массива срезов
        /// </summary>
        /// <param name="index">№ записи в журнале (0-9)</param>
        /// <param name="latestInitDt"></param>
        /// <returns></returns>
        bool ReadSliceArrInitializationDate(ref DateTime lastInitDt);

        /// <summary>
        /// Синхронизация времени в устройстве
        /// </summary>
        /// <param name="dt">Время для синхронизации</param>
        /// <returns></returns>
        bool SyncTime(DateTime dt);

        /// <summary>
        /// Чтение серийного номера устройства
        /// </summary>
        /// <param name="serial_number">Возвращаемое значение</param>
        /// <returns></returns>
        bool ReadSerialNumber(ref string serial_number);

        /// <summary>
        /// Запись в ЛОГ-файл
        /// </summary>
        /// <param name="str"></param>
        void WriteToLog(string str, bool doWrite = true);

    }
}
