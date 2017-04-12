using System;
using System.Collections.Generic;
using System.Data;

using Npgsql;

namespace Prizmer.PoolServer.DataBase
{
    public class PgStorage : DBInterface, IDisposable
    {
        private string m_connection_string;
        private Npgsql.NpgsqlConnection m_pg_con = null;

        public ConnectionState Open(String ConnectionString)
        {
            try
            {
                m_connection_string = ConnectionString;
                m_pg_con = new Npgsql.NpgsqlConnection(ConnectionString);
            }
            catch (Exception ex)
            {

                return ConnectionState.Broken;
            }

            try
            {
                m_pg_con.Open();
            }
            catch (Exception ex)
            {
                return ConnectionState.Broken;
            }

            return m_pg_con.State;
        }

        public void Close()
        {
            if (m_pg_con != null)
            {
                m_pg_con.Close();
                m_pg_con = null;
            }
        }

        public ConnectionState ConnectionStatus()
        {
            return (m_pg_con != null) ? m_pg_con.State : ConnectionState.Closed;
        }

        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        ~PgStorage()
        {
            Dispose();
        }

        private delegate Object ParseData(NpgsqlDataReader dr);

        private int AddRecord(string query)
        {
            NpgsqlCommand command = new NpgsqlCommand(query, m_pg_con);

            try
            {
                return command.ExecuteNonQuery();
            }
            catch
            {
                return -1;
            }
        }

        private List<Object> GetRecordsFromReader(string query, ParseData func_parse_data)
        {
            List<Object> result = new List<Object>();

            NpgsqlCommand command = new NpgsqlCommand(query, m_pg_con);
            NpgsqlDataReader dr = null;

            try
            {
                dr = command.ExecuteReader();

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        result.Add((Object)func_parse_data(dr));
                    }
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                if (dr != null)
                {
                    if (!dr.IsClosed)
                    {
                        dr.Close();
                    }
                }
            }

            return result;
        }

        private Object GetRecordFromReader(string query, ParseData func_parse_data)
        {
            NpgsqlCommand command = new NpgsqlCommand(query, m_pg_con);
            NpgsqlDataReader dr = null;

            try
            {
                dr = command.ExecuteReader();

                if (dr.HasRows)
                {
                    if (dr.Read())
                    {
                        return (Object)func_parse_data(dr);
                    }
                }
            }
            catch
            {

            }
            finally
            {
                if (dr != null)
                {
                    if (!dr.IsClosed)
                    {
                        dr.Close();
                    }
                }
            }

            return null;
        }

        private Object RetrieveMeter(NpgsqlDataReader dr)
        {
            Meter m;
            m.guid = new Guid(Convert.ToString(dr["guid"]));
            m.name = Convert.ToString(dr["name"]);
            m.address = Convert.ToUInt32(dr["address"]);
            m.password = Convert.ToString(dr["password"]);
            m.password_type_hex = Convert.ToBoolean(dr["password_type_hex"]);
            m.factory_number_manual = Convert.ToString(dr["factory_number_manual"]);
            m.factory_number_readed = Convert.ToString(dr["factory_number_readed"]);

            if (Convert.ToString(dr["dt_install"]).Length > 0) m.dt_install = Convert.ToDateTime(dr["dt_install"]);
            else m.dt_install = new DateTime(0);

            if (Convert.ToString(dr["dt_last_read"]).Length > 0) m.dt_last_read = Convert.ToDateTime(dr["dt_last_read"]);
            else m.dt_last_read = new DateTime(0);

            m.guid_types_meters = new Guid(Convert.ToString(dr["guid_types_meters"]));
            m.guid_meters = (Convert.ToString(dr["guid_meters"]) != "") ? new Guid(Convert.ToString(dr["guid_meters"])) : m.guid;
            m.time_delay_current = Convert.ToUInt16(dr["time_delay_current"]);

            return (Object)m;
        }

        private Object RetrieveMeterType(NpgsqlDataReader dr)
        {
            TypeMeter tm;
            tm.guid = new Guid(Convert.ToString(dr["guid"]));
            tm.name = Convert.ToString(dr["name"]);
            tm.driver_name = Convert.ToString(dr["driver_name"]);

            return (Object)tm;
        }

        private Object RetrieveComPortSettings(NpgsqlDataReader dr)
        {
            ComPortSettings cps;
            cps.guid = new Guid(Convert.ToString(dr["guid"]));
            cps.name = Convert.ToString(dr["name"]);
            cps.baudrate = Convert.ToUInt32(dr["baudrate"]);
            cps.data_bits = Convert.ToByte(dr["data_bits"]);
            cps.parity = Convert.ToByte(dr["parity"]);
            cps.stop_bits = Convert.ToByte(dr["stop_bits"]);
            cps.write_timeout = Convert.ToUInt16(dr["write_timeout"]);
            cps.read_timeout = Convert.ToUInt16(dr["read_timeout"]);
            cps.attempts = Convert.ToUInt16(dr["attempts"]);
            cps.delay_between_sending = Convert.ToUInt16(dr["delay_between_sending"]);

            return (Object)cps;
        }

        private Object RetrieveTcpIPSettings(NpgsqlDataReader dr)
        {
            TCPIPSettings tis;
            tis.guid = new Guid(Convert.ToString(dr["guid"]));
            tis.ip_address = Convert.ToString(dr["ip_address"]);
            tis.ip_port = Convert.ToUInt16(dr["ip_port"]);
            tis.write_timeout = Convert.ToUInt16(dr["write_timeout"]);
            tis.read_timeout = Convert.ToUInt16(dr["read_timeout"]);
            tis.attempts = Convert.ToUInt16(dr["attempts"]);
            tis.delay_between_sending = Convert.ToUInt16(dr["delay_between_sending"]);

            return (Object)tis;
        }

        private Object RetrieveParam(NpgsqlDataReader dr)
        {
            Param pm;
            pm.guid = new Guid(Convert.ToString(dr["guid"]));
            pm.param_address = Convert.ToUInt16(dr["param_address"]);
            pm.channel = Convert.ToUInt16(dr["channel"]);
            pm.guid_types_meters = new Guid(Convert.ToString(dr["guid_types_meters"]));
            pm.name = Convert.ToString(dr["name"]);
            pm.period = (dr["period"] != DBNull.Value) ? Convert.ToUInt16(dr["period"]) : Convert.ToUInt16(0);
            pm.type = Convert.ToByte(dr["type"]);

            return (Object)pm;
        }

        private Object RetrieveTakenParams(NpgsqlDataReader dr)
        {
            TakenParams tp;
            tp.id = Convert.ToUInt32(dr["id"]);
            tp.guid = new Guid(Convert.ToString(dr["guid"]));
            tp.guid_params = new Guid(Convert.ToString(dr["guid_params"]));
            tp.guid_meters = new Guid(Convert.ToString(dr["guid_meters"]));

            return (Object)tp;
        }

        private Object RetrieveValueWithDateTime(NpgsqlDataReader dr)
        {
            Value v;
            string[] date_str = Convert.ToString(dr["date"]).Split(new char[] { ' ' });
            string[] time_str = Convert.ToString(dr["time"]).Split(new char[] { ' ' });
            v.dt = Convert.ToDateTime(date_str[0] + " " + time_str[1]);
            v.value = Convert.ToSingle(dr["value"]);
            v.status = Convert.ToBoolean(dr["status"]);
            v.id_taken_params = Convert.ToUInt32(dr["id_taken_params"]);

            return (Object)v;
        }

        private Object RetrieveValueWithDate(NpgsqlDataReader dr)
        {
            Value v;
            v.dt = Convert.ToDateTime(Convert.ToString(dr["date"]));
            v.value = Convert.ToSingle(dr["value"]);
            v.status = Convert.ToBoolean(dr["status"]);
            v.id_taken_params = Convert.ToUInt32(dr["id_taken_params"]);

            return (Object)v;
        }

        public Meter[] GetMeters()
        {
            string query = "SELECT guid, name, address, password, password_type_hex, factory_number_manual, factory_number_readed, is_factory_numbers_equal, dt_install, dt_last_read, guid_types_meters, guid_meters, time_delay_current FROM meters";

            List<Object> list = GetRecordsFromReader(query, RetrieveMeter);

            Meter[] result = new Meter[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Meter)list[i];
            }

            list.Clear();

            return result;
        }

        public Meter GetMeterByGUID(Guid guid)
        {
            string query = "SELECT guid, name, address, password, password_type_hex, factory_number_manual, factory_number_readed, is_factory_numbers_equal, dt_install, dt_last_read guid_types_meters, guid_meters, time_delay_current FROM meters" +
                            " WHERE guid = '" + guid.ToString() + "'";

            Object o = GetRecordFromReader(query, RetrieveMeter);

            return (o != null) ? (Meter)o : new Meter();
        }

        public TypeMeter[] GetMetersTypes()
        {
            string query = "SELECT guid, name, driver_name FROM types_meters";

            List<Object> list = GetRecordsFromReader(query, RetrieveMeterType);

            TypeMeter[] result = new TypeMeter[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (TypeMeter)list[i];
            }

            list.Clear();

            return result;
        }

        public TypeMeter GetMetersTypeByGUID(Guid guid)
        {
            string query = "SELECT guid, name, driver_name FROM types_meters WHERE guid = '" + guid.ToString() + "'";

            Object o = GetRecordFromReader(query, RetrieveMeterType);

            return (o != null) ? (TypeMeter)o : new TypeMeter();
        }

        public Meter[] GetMetersHavingTakenParam()
        {
            string query = "SELECT guid, name, address, password, password_type_hex, factory_number_manual, factory_number_readed, is_factory_numbers_equal, dt_install, dt_last_read, guid_types_meters, guid_meters, time_delay_current FROM meters " +
                            "GROUP BY guid HAVING (SELECT COUNT(*) FROM taken_params WHERE taken_params.guid_meters = meters.guid) > 0";

            List<Object> list = GetRecordsFromReader(query, RetrieveMeter);

            Meter[] result = new Meter[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Meter)list[i];
            }

            list.Clear();

            return result;
        }

        public Meter[] GetMetersByComportGUID(Guid guid_comport)
        {
            List<Meter> tempList = new List<Meter>();

            string query = "SELECT meters.guid, name, address, password, password_type_hex, factory_number_manual, factory_number_readed, is_factory_numbers_equal, dt_install, dt_last_read, guid_types_meters, meters.guid_meters, time_delay_current FROM meters " +
                            "JOIN link_meters_comport_settings ON link_meters_comport_settings.guid_comport_settings = '" + guid_comport.ToString() + "' " +
                            "WHERE meters.guid = link_meters_comport_settings.guid_meters";

            List<Object> list = GetRecordsFromReader(query, RetrieveMeter);

            Meter[] result = new Meter[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Meter)list[i];
            }

            list.Clear();

            return result;
        }

        public Meter[] GetMetersByTcpIPGUID(Guid guid_tcpip)
        {
            string query = "SELECT meters.guid, name, address, password, password_type_hex, factory_number_manual, factory_number_readed, is_factory_numbers_equal, dt_install, dt_last_read, guid_types_meters, meters.guid_meters, time_delay_current FROM meters " +
                            "JOIN link_meters_tcpip_settings ON link_meters_tcpip_settings.guid_tcpip_settings ='" + guid_tcpip.ToString() + "' " +
                            "WHERE meters.guid = link_meters_tcpip_settings.guid_meters";

            List<Object> list = GetRecordsFromReader(query, RetrieveMeter);

            Meter[] result = new Meter[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Meter)list[i];
            }

            list.Clear();

            return result;
        }

        #region Команды для режима дочики и интерфейса формы

        public Meter[] GetMetersByTcpIPGUIDAndParams(Guid guid_tcpip, int paramType, string driverName)
        {
           string query = @"SELECT DISTINCT ON (factory_number_manual) *
                    FROM 
                      public.meters, 
                      public.tcpip_settings, 
                      public.link_meters_tcpip_settings, 
                      public.types_params, 
                      public.params, 
                      public.types_meters, 
                      public.taken_params
                    WHERE 
                      meters.guid_types_meters = types_meters.guid AND
                      link_meters_tcpip_settings.guid_meters = meters.guid AND
                      link_meters_tcpip_settings.guid_tcpip_settings = tcpip_settings.guid AND
                      params.guid_types_params = types_params.guid AND
                      params.guid_types_meters = types_meters.guid AND
                      taken_params.guid_params = params.guid AND
                      taken_params.guid_meters = meters.guid AND
                      tcpip_settings.guid = '" + guid_tcpip.ToString() + @"' AND
                      types_params.type = "+ paramType + @" AND 
                      types_meters.driver_name = '" + driverName + "';";

            List<Object> list = GetRecordsFromReader(query, RetrieveMeter);

            Meter[] result = new Meter[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Meter)list[i];
            }

            list.Clear();

            return result;
        }

        public List<string> GetDriverNames()
        {
            string query = @"SELECT driver_name FROM types_meters";

            List<string> result = new List<string>();

            NpgsqlCommand command = new NpgsqlCommand(query, m_pg_con);
            NpgsqlDataReader dr = null;

            try
            {
                dr = command.ExecuteReader();

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        string tmp = Convert.ToString(dr["driver_name"]);
                        result.Add(tmp);
                    }
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                if (dr != null)
                {
                    if (!dr.IsClosed)
                    {
                        dr.Close();
                    }
                }
            }

            return result;
        }

        public List<string> GetPortsAvailiableByDriverParamType(int paramType, string driverName)
        {
            string query = @"SELECT DISTINCT ON (tcpip_settings.ip_address)
              tcpip_settings.ip_address, 
              tcpip_settings.ip_port
            FROM 
              public.tcpip_settings, 
              public.meters, 
              public.taken_params, 
              public.params, 
              public.types_params, 
              public.types_meters, 
              public.link_meters_tcpip_settings
            WHERE 
              meters.guid_types_meters = types_meters.guid AND
              taken_params.guid_meters = meters.guid AND
              taken_params.guid_params = params.guid AND
              params.guid_types_params = types_params.guid AND
              link_meters_tcpip_settings.guid_meters = meters.guid AND
              link_meters_tcpip_settings.guid_tcpip_settings = tcpip_settings.guid AND
              types_params.type = " + paramType + @" AND 
              types_meters.driver_name = '" + driverName + "';";


            List<string> result = new List<string>();

            NpgsqlCommand command = new NpgsqlCommand(query, m_pg_con);
            NpgsqlDataReader dr = null;

            try
            {
                dr = command.ExecuteReader();

                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        string ip = Convert.ToString(dr["ip_address"]);
                        string port = Convert.ToString(dr["ip_port"]);
                        string summary = ip + ":" + port;
                        result.Add(summary);
                    }
                }
            }
            catch (Exception e)
            {

            }
            finally
            {
                if (dr != null)
                {
                    if (!dr.IsClosed)
                    {
                        dr.Close();
                    }
                }
            }

            return result;
        }
    
        #endregion

        public int UpdateMeterLastRead(Guid guid, DateTime dt)
        {
            string query = "UPDATE meters SET " +
                            "dt_last_read = '" + dt.ToString() + "'" +
                            " WHERE guid = '" + guid.ToString() + "'";

            return AddRecord(query);
        }

        public int UpdateMeterFactoryNumber(Guid guid, string factoryNumber, string isEqual)
        {
            string query = "UPDATE meters SET " +
                            "factory_number_readed = '" + factoryNumber + "', is_factory_numbers_equal = '" + isEqual + "'" +
                            " WHERE guid = '" + guid.ToString() + "'";

            return AddRecord(query);
        }

        public ComPortSettings[] GetComportSettings()
        {
            string query = "SELECT guid, name, baudrate, data_bits, parity, stop_bits, write_timeout,read_timeout, attempts, delay_between_sending FROM comport_settings";

            List<Object> list = GetRecordsFromReader(query, RetrieveComPortSettings);

            ComPortSettings[] result = new ComPortSettings[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (ComPortSettings)list[i];
            }

            list.Clear();

            return result;
        }

        public TCPIPSettings[] GetTCPIPSettings()
        {
            string query = "SELECT guid, ip_address, ip_port, write_timeout, read_timeout, attempts, delay_between_sending FROM tcpip_settings";

            List<Object> list = GetRecordsFromReader(query, RetrieveTcpIPSettings);

            TCPIPSettings[] result = new TCPIPSettings[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (TCPIPSettings)list[i];
            }

            list.Clear();

            return result;
        }

        public ComPortSettings GetComportByMeterGUID(Guid guid_meters)
        {
            string query = "SELECT comport_settings.guid, comport_settings.name, baudrate, data_bits, parity, stop_bits, write_timeout,read_timeout, attempts, delay_between_sending FROM comport_settings " +
                            "JOIN link_meters_comport_settings ON link_meters_comport_settings.guid_meters = '" + guid_meters.ToString() + "' " +
                            "WHERE comport_settings.guid = link_meters_comport_settings.guid_comport_settings";

            Object o = GetRecordFromReader(query, RetrieveComPortSettings);

            return (o != null) ? (ComPortSettings)o : new ComPortSettings();
        }

        public TCPIPSettings GetTCPIPByMeterGUID(Guid guid_meters)
        {
            string query = "SELECT tcpip_settings.guid, ip_address, ip_port, write_timeout, read_timeout, attempts, delay_between_sending FROM tcpip_settings " +
                            "JOIN link_meters_tcpip_settings ON link_meters_tcpip_settings.guid_meters = '" + guid_meters.ToString() + "' " +
                            "WHERE tcpip_settings.guid = link_meters_tcpip_settings.guid_tcpip_settings";

            Object o = GetRecordFromReader(query, RetrieveTcpIPSettings);

            return (o != null) ? (TCPIPSettings)o : new TCPIPSettings();
        }

        public Param GetParamByGUID(Guid guid)
        {
            string query = "SELECT params.guid, params.param_address, params.channel, params.guid_types_meters, names_params.name, types_params.period, types_params.type FROM params " +
                            "JOIN names_params ON names_params.guid = params.guid_names_params " +
                            "JOIN types_params ON types_params.guid = params.guid_types_params " +
                            "WHERE params.guid = '" + guid.ToString() + "'";

            Object o = GetRecordFromReader(query, RetrieveParam);

            return (o != null) ? (Param)o : new Param();

        }

        public Param[] GetParamByTypeMetersGUID(Guid guid_types_meters)
        {
            string query = "SELECT params.guid, params.param_address, params.channel, params.guid_types_meters, names_params.name, types_params.period, types_params.type FROM params " +
                            "JOIN names_params ON names_params.guid = params.guid_names_params " +
                            "JOIN types_params ON types_params.guid = params.guid_types_params " +
                            "WHERE params.guid_types_meters = '" + guid_types_meters.ToString() + "'";

            List<Object> list = GetRecordsFromReader(query, RetrieveParam);

            Param[] result = new Param[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Param)list[i];
            }

            list.Clear();

            return result;
        }

        public Param[] GetParamByTakenParam(Guid guid_meters, Byte types_params)
        {
            string query = "SELECT params.guid, params.param_address, params.channel, params.guid_types_meters, names_params.name, types_params.period, types_params.type FROM params " +
                            "JOIN names_params ON names_params.guid = params.guid_names_params " +
                            "JOIN types_params ON types_params.guid = params.guid_types_params " +
                            "JOIN taken_params ON taken_params.guid_meters = '" + guid_meters.ToString() + "' " +
                            "WHERE params.guid = taken_params.guid_params AND types_params.type = " + types_params.ToString();

            List<Object> list = GetRecordsFromReader(query, RetrieveParam);

            Param[] result = new Param[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Param)list[i];
            }

            list.Clear();

            return result;
        }

        public TakenParams[] GetTakenParamByMetersGUIDandParamsType(Guid guid_meters, Byte types_params)
        {
            string query = "SELECT id, guid, guid_params, guid_meters FROM taken_params " +
                            "WHERE guid_meters = '" + guid_meters.ToString() + "' AND guid_params IN (SELECT guid FROM params WHERE guid_types_params IN (SELECT guid FROM types_params WHERE type = " + types_params.ToString() + "))";

            List<Object> list = GetRecordsFromReader(query, RetrieveTakenParams);

            TakenParams[] result = new TakenParams[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (TakenParams)list[i];
            }

            list.Clear();

            return result;
        }

        public TakenParams[] GetTakenParamByMetersGUID(Guid guid_meters)
        {
            string query = "SELECT id, guid, guid_params, guid_meters FROM taken_params " +
                            "WHERE guid_meters = '" + guid_meters.ToString() + "'";

            List<Object> list = GetRecordsFromReader(query, RetrieveTakenParams);

            TakenParams[] result = new TakenParams[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (TakenParams)list[i];
            }

            list.Clear();

            return result;
        }

        public TakenParams[] GetTakenParams()
        {
            string query = "SELECT id, guid, guid_params, guid_meters FROM taken_params";

            List<Object> list = GetRecordsFromReader(query, RetrieveTakenParams);

            TakenParams[] result = new TakenParams[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (TakenParams)list[i];
            }

            list.Clear();

            return result;
        }

        public TakenParams GetTakenParamByID(UInt32 id)
        {
            string query = "SELECT id, guid, guid_params, guid_meters FROM taken_params WHERE id = " + id.ToString();

            Object o = GetRecordFromReader(query, RetrieveTakenParams);

            return (o != null) ? (TakenParams)o : new TakenParams();
        }

        public TakenParams GetTakenParamByGUID(Guid guid)
        {
            string query = "SELECT id, guid, guid_params, guid_meters FROM taken_params WHERE guid = '" + guid.ToString() + "'"; ;

            Object o = GetRecordFromReader(query, RetrieveTakenParams);

            return (o != null) ? (TakenParams)o : new TakenParams();
        }

        public int AddCurrentValues(Value value)
        {
            string query = "SELECT date, time, value, status, id_taken_params FROM current_values " +
                            " WHERE id_taken_params = " + value.id_taken_params;

            List<Object> list = GetRecordsFromReader(query, RetrieveValueWithDateTime);
            if (list.Count == 0)
            {
                query = "INSERT INTO current_values (date, time, value, status, id_taken_params) " +
                            "VALUES (" +
                            "'" + value.dt.ToShortDateString() + "', " +
                            "'" + value.dt.ToShortTimeString() + "', " +
                            value.value.ToString().Replace(',', '.') + ", " +
                            value.status.ToString() + ", " +
                            value.id_taken_params.ToString() +
                            ")";
            }
            else
            {
                query = "UPDATE current_values SET " +
                                "date = '" + value.dt.ToShortDateString() + "', " +
                                "time = '" + value.dt.ToShortTimeString() + "', " +
                                "value = " + value.value.ToString().Replace(',', '.') + ", " +
                                "status = " + value.status.ToString() + ", " +
                                "id_taken_params = " + value.id_taken_params.ToString() +
                                " WHERE id_taken_params = " + value.id_taken_params;
            }


            /*
            query = "INSERT INTO current_values_archive (date, time, value, status, id_taken_params) " +
                            "VALUES (" +
                            "'" + value.dt.ToShortDateString() + "', " +
                            "'" + value.dt.ToShortTimeString() + "', " +
                            value.value.ToString().Replace(',', '.') + ", " +
                            value.status.ToString() + ", " +
                            value.id_taken_params.ToString() +
                            ")";
             * *

            return AddRecord(query);;
             */
            return AddRecord(query);
        }

        public int AddDailyValues(Value value)
        {
            string query = "INSERT INTO daily_values (date, value, status, id_taken_params) " +
                "VALUES (" +
                "'" + value.dt.ToShortDateString() + "', " +
                value.value.ToString().Replace(',', '.') + ", " +
                value.status.ToString() + ", " +
                value.id_taken_params.ToString() +
                ")";

            return AddRecord(query);
        }

        public int AddMonthlyValues(Value value)
        {
            string query = "INSERT INTO monthly_values (date, value, status, id_taken_params) " +
                            "VALUES (" +
                            "'" + value.dt.ToShortDateString() + "', " +
                            value.value.ToString().Replace(',', '.') + ", " +
                            value.status.ToString() + ", " +
                            value.id_taken_params.ToString() +
                            ")";

            return AddRecord(query);
        }

        public int AddVariousValues(Value value)
        {
            string query = "INSERT INTO various_values (date, time, value, status, id_taken_params) " +
                "VALUES (" +
                "'" + value.dt.ToShortDateString() + "', " +
                "'" + value.dt.ToShortTimeString() + "', " +
                value.value.ToString().Replace(',', '.') + ", " +
                value.status.ToString() + ", " +
                value.id_taken_params.ToString() +
                ")";

            return AddRecord(query);
        }

        public Value[] GetExistsDailyValuesDT(TakenParams taken_params, DateTime BeginDT, DateTime EndDT)
        {
            string query = "SELECT date, value, status, id_taken_params FROM daily_values " +
                            "WHERE (id_taken_params = " + taken_params.id + ") AND date BETWEEN '" + BeginDT.ToShortDateString() + "' AND '" + EndDT.ToShortDateString() + "'";

            List<Object> list = GetRecordsFromReader(query, RetrieveValueWithDate);

            Value[] result = new Value[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Value)list[i];
            }

            list.Clear();

            return result;
        }

        public Value[] GetExistsMonthlyValuesDT(TakenParams taken_params, DateTime BeginDT, DateTime EndDT)
        {
            string query = "SELECT date, value, status, id_taken_params FROM monthly_values " +
                            "WHERE (id_taken_params = " + taken_params.id + ") AND date BETWEEN '" + BeginDT.ToShortDateString() + "' AND '" + EndDT.ToShortDateString() + "'";

            List<Object> list = GetRecordsFromReader(query, RetrieveValueWithDate);

            Value[] result = new Value[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Value)list[i];
            }

            list.Clear();

            return result;
        }

        public Value[] GetExistsVariousValuesDT(TakenParams taken_params, DateTime BeginDT, DateTime EndDT)
        {
            string query = "SELECT id, date, time, value, status, id_taken_params FROM various_values " +
                            "WHERE (id_taken_params = " + taken_params.id + ") AND date BETWEEN '" + BeginDT.ToShortDateString() + "' AND '" + EndDT.ToShortDateString() + "' AND time BETWEEN '" + BeginDT.ToShortTimeString() + "' AND '" + EndDT.ToShortTimeString() + "'";

            List<Object> list = GetRecordsFromReader(query, RetrieveValueWithDateTime);

            Value[] result = new Value[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (Value)list[i];
            }

            list.Clear();

            return result;
        }

        /// <summary>
        /// Получает наиболее новое (по дате) "настраиваемое" значение
        /// </summary>
        /// <param name="taken_params"></param>
        /// <returns></returns>
        public Value GetLatestVariousValue(TakenParams taken_params)
        {
            Value tempVal = new Value();
            string query = "SELECT * FROM various_values WHERE id_taken_params=" + taken_params.id.ToString() + " ORDER BY date DESC,time DESC LIMIT 1";

            List<Object> list = GetRecordsFromReader(query, RetrieveValueWithDateTime);

            if (list.Count == 1)
                tempVal = (Value)list[0];

            return tempVal;
        }

    }
}

