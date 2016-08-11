using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using Prizmer.Ports;
using Prizmer.Meters.iMeters;
using System.Configuration;

using FirebirdSql.Data.FirebirdClient;

namespace Prizmer.Meters
{
    public class DomovoyMeters : CMeter, IMeter
    {
        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            this.m_address = address;
        }

        FbConnection fbConnection = new FbConnection();
        public bool OpenConnection()
        {

            //// Set the ServerType to 1 for connect to the embedded server
            //string connectionString =
            //"User=SYSDBA;" +
            //"Password=masterkey;" +
            //"Database=C:/Users/ikhromov/Desktop/Domovoy_prj/4rmd.gdb;" +
            //"DataSource=localhost;" +
            //"Port=3050;" +
            //"Dialect=3;" +
            //"Charset=NONE;" +
            //"Role=;" +
            //"Connection lifetime=15;" +
            //"Pooling=true;" +
            //"MinPoolSize=0;" +
            //"MaxPoolSize=50;" +
            //"Packet Size=8192;" +
            //"ServerType=0";

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["domovoyDBConnection"].ConnectionString;
                fbConnection = new FbConnection(connectionString);
                // Open connection.
                fbConnection.Open();
                fbConnection.Close();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }
        public bool CloseConnection()
        {
            try
            {
                fbConnection.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool OpenLinkCanal()
        {
            return OpenConnection();
        }

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {

                try
                {
                    fbConnection.Open();

                    // declare command
                    string sqlCmd = 
                    "SELECT FIRST 1 i.VAL_FLOAT FROM INTEGRAL_DEVID i, DEVICE_TYPES dty, DEV_VALUES dv " +
                    "WHERE i.DEV_ID=12 AND i.CHANNEL=0 AND i.VALUE_ID=1 AND (dty.INDEX_TYPE=( " +
                    "SELECT dev.INDEX_TYPE " +
                    "FROM DEVICES dev " +
                    "WHERE dev.PRIMARY_DEVICE_INDEX=i.DEV_ID)) " +
                    "AND (dv.VALUE_ID=i.VALUE_ID) " +
                    "ORDER BY i.VALDATE DESC";

                    FbCommand readCommand = new FbCommand(sqlCmd, fbConnection);
                    //readCommand.Parameters.Add("@iMeterAddress", m_address);
                    //readCommand.Parameters.Add("@iTarif", tarif);
                    //readCommand.Parameters.Add("@iParamAddress", param);

                    FbDataReader myreader = readCommand.ExecuteReader();
                    float tmpVal = 0f;
                    while (myreader.Read())
                    {
                        // myreader[0] reads from the 1st Column
                        tmpVal = Convert.ToSingle(myreader[0]);
                    }
                    myreader.Close(); // we are done with the reader
                    recordValue = tmpVal;
                }
                catch (Exception x)
                {
                    CloseConnection();
                    return false;
                }
                finally
                {
                    CloseConnection();
                }

                return true;
        }

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
        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
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
}
