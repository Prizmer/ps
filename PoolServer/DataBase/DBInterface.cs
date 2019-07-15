using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;

using PollingLibraries.LibPorts;

namespace Prizmer.PoolServer.DataBase
{
    #region Data

    /// <summary>
    /// Структура хранит информацию о типе прибора
    /// </summary>
    public struct TypeMeter
    {
        public Guid guid;
        public String name;
        public String driver_name;
    }

    /// <summary>
    /// Структура хранит информацию о приборе
    /// </summary>
    public struct Meter
    {
        public Guid guid;
        public String name;
        public UInt32 address;
        public String password;
        public Boolean password_type_hex;
        public String factory_number_manual;
        public String factory_number_readed;
        public DateTime dt_install;
        public DateTime dt_last_read;
        public Guid guid_types_meters;
        public Guid guid_meters;
        public UInt16 time_delay_current;
    }

    /// <summary>
    /// Структура хранит данные о параметре и представляет собой слияние таблиц params, names_params и types_params
    /// </summary>
    public struct Param
    {
        public Guid guid;
        public UInt16 param_address;
        public UInt16 channel;
        public Guid guid_types_meters;
        public String name;
        public UInt16 period;
        public Byte type;
    }

    /// <summary>
    /// Структура хранит информацию о записи в таблице taken_params
    /// </summary>
    public struct TakenParams
    {
        public UInt32 id;
        public Guid guid;
        public Guid guid_params;
        public Guid guid_meters;
    }

    /// <summary>
    /// Структура хранит информации об одном значении параметра
    /// </summary>
    public struct Value
    {
        public DateTime dt;
        public double value;
        public Boolean status;
        public UInt32 id_taken_params;
    }

    #endregion

    public interface DBInterface
    {
        /// <summary>
        /// Метод открывает соединение и/или сохраняет строку подключения
        /// </summary>
        /// <param name="ConnectionString"></param>
        ConnectionState Open(String ConnectionString);

        /// <summary>
        /// Метод закрывает соединение
        /// </summary>
        void Close();

        /// <summary>
        /// Метод возвращает текущее состояние соединения
        /// </summary>
        /// <returns></returns>
        ConnectionState ConnectionStatus();

        #region Meters
        /// <summary>
        /// Метод возвращает массив структур Meter, содержащих информацию обо всех приборах в системе
        /// </summary>
        /// <returns></returns>
        Meter[] GetMeters();

        /// <summary>
        /// Метод возвращает структуру Meter, содержащую информацию приборе с заданным guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        Meter GetMeterByGUID(Guid guid);

        /// <summary>
        /// Метод возвращает массив структур Meter, содержащих данные о приборах, ссылки на которые есть в таблице taken_param
        /// </summary>
        /// <returns></returns>
        Meter[] GetMetersHavingTakenParam();

        /// <summary>
        /// Метод возвращает массив структур Meter с данными о приборах, подключенных к данному последовательному порту
        /// </summary>
        /// <param name="guid_comport"></param>
        /// <returns></returns>
        Meter[] GetMetersByComportGUID(Guid guid_comport);

        /// <summary>
        /// Метод возвращает массив структур Meter с данными о приборах, подключенных к данному соединению tcp/ip
        /// </summary>
        /// <param name="guid_comport"></param>
        /// <returns></returns>
        Meter[] GetMetersByTcpIPGUID(Guid guid_tcpip);

        /// <summary>
        /// Метод возвращает массив структур TypeMeter, содержащих информацию обо всех типах приборов
        /// </summary>
        /// <returns></returns>
        TypeMeter[] GetMetersTypes();

        /// <summary>
        /// Метод возвращает структуру TypeMeter, содержащую информацию о типе прибора с заданным guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        TypeMeter GetMetersTypeByGUID(Guid guid);

        /// <summary>
        /// Обновляет время последнего опроса прибора
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        int UpdateMeterLastRead(Guid guid, DateTime dt);

        /// <summary>
        /// Обновляет переменную "Прочитанный со счетчика номер" и переменную "Совпадает ли номер с указанным вручную"
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="factoryNumber">Прочитанный со счетчика номер</param>
        /// <param name="isEqual"></param>
        /// <returns></returns>
        int UpdateMeterFactoryNumber(Guid guid, string factoryNumber, string isEqual);

        //Новая функция для MetersSearchForm
        /// <summary>
        /// Возвращает таблицу с информацией о счётчиках с серийными номерами, содержащими строку
        /// </summary>
        /// <param name="factory_number">Искомая строка</param>
        /// <param name="table">Возвращаемая таблица</param>
        void FindMetersWithSerial(string factory_number, DataTable table);

        #endregion

        #region CommunicationSettings

        /// <summary>
        /// Метод возвращает массив структур ComPortSettings, содержащих данные обо всех соединениях по последовательному порту
        /// </summary>
        /// <returns></returns>
        ComPortSettings[] GetComportSettings();

        /// <summary>
        /// Метод возвращает структуру ComPortSettings, описывающую свойства подключения прибора по последовательному порту
        /// Если данного подключения нет в системе, то поле name структуры должно вернуться пустым
        /// </summary>
        /// <param name="guid_meters"></param>
        /// <returns></returns>
        ComPortSettings GetComportByMeterGUID(Guid guid_meters);

        /// <summary>
        /// Метод возвращает массив структур TCPIPSettings, содержащих данные обо всех соединениях по tcp/ip
        /// </summary>
        /// <returns></returns>
        TCPIPSettings[] GetTCPIPSettings();

        /// <summary>
        /// Метод возвращает структуру TCPIPSettings, описывающую свойства подключения прибора по tcp/ip
        /// Если данного подключения нет в системе, то поле ip_address структуры должно вернуться пустым
        /// </summary>
        /// <param name="guid_meters"></param>
        /// <returns></returns>
        TCPIPSettings GetTCPIPByMeterGUID(Guid guid_meters);

        #endregion

        #region Params

        /// <summary>
        /// Метод возвращает структуру с описанием параметра, с заданным GUID
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        Param GetParamByGUID(Guid guid);

        /// <summary>
        /// Метод возвращает массив структур с описанием параметров, относящихся к типу прибора с заданным guid
        /// </summary>
        /// <param name="guid_types_meters"></param>
        /// <returns></returns>
        Param[] GetParamByTypeMetersGUID(Guid guid_types_meters);

        /// <summary>
        /// Метод возвращает массив структур с описанием параметров заданного типа types_params, относящихся к прибору с заданным guid,
        /// ссылка на который присутствует в таблице taken_params
        /// </summary>
        /// <param name="guid_meters"></param>
        /// <param name="types_param"></param>
        /// <returns></returns>
        Param[] GetParamByTakenParam(Guid guid_meters, Byte types_params);

        /// <summary>
        /// Метод возвращает массив структур таблицы taken_param, указывающих на параметры заданного типа types_params, относящихся к прибору с заданным guid,
        /// ссылка на который присутствует в таблице taken_params
        /// </summary>
        /// <param name="guid_meters"></param>
        /// <param name="types_param"></param>
        /// <returns></returns>
        TakenParams[] GetTakenParamByMetersGUIDandParamsType(Guid guid_meters, Byte types_params);

        /// <summary>
        /// Метод возвращает массив структур таблицы taken_param с описанием параметров, относящихся к прибору с заданным guid,
        /// ссылка на который присутствует в таблице taken_params
        /// </summary>
        /// <param name="guid_meters"></param>
        /// <returns></returns>
        TakenParams[] GetTakenParamByMetersGUID(Guid guid_meters);

        /// <summary>
        /// Метод возвращает массив структур таблицы taken_param
        /// </summary>
        /// <returns></returns>
        TakenParams[] GetTakenParams();

        /// <summary>
        /// Метод возвращает запись из таблицы taken_param c заданным id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TakenParams GetTakenParamByID(UInt32 id);

        /// <summary>
        /// Метод возвращает запись из таблицы taken_param c заданным guid
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        TakenParams GetTakenParamByGUID(Guid guid);

        #endregion

        #region Values

        /// <summary>
        /// Метод добавляет запись в таблицу current_values_archive, заменяет данные в current_values
        /// </summary>
        /// <param name="value"></param>
        int AddCurrentValues(Value value);

        /// <summary>
        /// Метод добавляет запись в таблицу daily_values
        /// </summary>
        /// <param name="value"></param>
        int AddDailyValues(Value value);

        /// <summary>
        /// Метод добавляет запись в таблицу monthly_values
        /// </summary>
        /// <param name="value"></param>
        int AddMonthlyValues(Value value);

        /// <summary>
        /// Метод добавляет запись в таблицу various_values
        /// </summary>
        /// <param name="value"></param>
        int AddVariousValues(Value value);

        /// <summary>
        /// Метод возвращает записи значения параметра из таблицы daily_values в заданном диапазоне времени
        /// </summary>
        /// <param name="BeginDT"></param>
        /// <param name="EndDT"></param>
        /// <returns></returns>
        Value[] GetExistsDailyValuesDT(TakenParams taken_params, DateTime BeginDT, DateTime EndDT);

        /// <summary>
        /// Метод возвращает записи значения параметра из таблицы monthly_values в заданном диапазоне времени
        /// </summary>
        /// <param name="BeginDT"></param>
        /// <param name="EndDT"></param>
        /// <returns></returns>
        Value[] GetExistsMonthlyValuesDT(TakenParams taken_params, DateTime BeginDT, DateTime EndDT);

        /// <summary>
        /// Метод возвращает записи значения параметра из таблицы various_values в заданном диапазоне времени
        /// </summary>
        /// <param name="BeginDT"></param>
        /// <param name="EndDT"></param>
        /// <returns></returns>
        Value[] GetExistsVariousValuesDT(TakenParams taken_params, DateTime BeginDT, DateTime EndDT);

        #endregion

        #region Для дочитки
            List<string[]> GetDriverNames();
            List<string> GetPortsAvailiableByDriverGuid(int paramType, string driverGuid, bool isTcp);
            Meter[] GetMetersByTcpIPGUIDAndParams(Guid guid_tcpip, int paramType, Guid driverGuid);
        #endregion
    }
}

