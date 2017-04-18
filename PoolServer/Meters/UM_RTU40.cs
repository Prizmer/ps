using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using System.Data;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

using Prizmer.Ports;
using Prizmer.Meters.iMeters;


namespace Prizmer.Meters
{

    public sealed class UM_RTU40 : CMeter, IMeter
    {
        //пароль к самой ум
        string password = "00000000";

        const string SEPARATOR = ",";
        const int MESSAGE_TAIL_SIZE_BYTES = 6;

        Dictionary<ushort, string> currCorrelationDict = new Dictionary<ushort, string>();
        Dictionary<ushort, string> dailyCorrelationDict = new Dictionary<ushort, string>();
        Dictionary<ushort, string> monthlyCorrelationDict = new Dictionary<ushort, string>();
        Dictionary<ushort, string> slicesCorrelationDict = new Dictionary<ushort, string>();

        public UM_RTU40()
        {
            dailyCorrelationDict.Add(0, "dA+");
            dailyCorrelationDict.Add(1, "dA-");
            dailyCorrelationDict.Add(2, "dR+");
            dailyCorrelationDict.Add(3, "dR-");

            foreach (ushort k in dailyCorrelationDict.Keys)
                monthlyCorrelationDict.Add(k, dailyCorrelationDict[k].Replace("d", "M"));

            foreach (ushort k in dailyCorrelationDict.Keys)
                currCorrelationDict.Add(k, dailyCorrelationDict[k].Replace("d", ""));

            slicesCorrelationDict.Add(0, "DPAp");
            slicesCorrelationDict.Add(1, "DPAm");
            slicesCorrelationDict.Add(2, "DPRp");
            slicesCorrelationDict.Add(3, "DPRm");

        }

        int meterId = 1;
        bool meterIdParsingResult = false;
        UM_VERSION umVersion = UM_VERSION.UM40;

        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            m_address = address;
            this.m_vport = data_vport;

            meterIdParsingResult = int.TryParse(pass, out meterId);
            //this.password = pass.Length > 0 ? pass : "00000000";



            //очистка временных списков ОЧЕНь ВАЖНО для данного прибора
            listOfDailyValues.Clear();
            listOfMonthlyValues.Clear();
            listOfSliceValues.Clear();
        }

        public struct ValueUM
        {
            public float value;
            public DateTime dt;
            public int address;
            public int channel;
            public string caption;
            public string name;
            public string meterSN;
        }
        public enum MeterModels
        {
            USPD = 0,
            M200 = 1,
            M230 = 3,
            SET4TM = 4,
            M203 = 31,
            M206 = 32,
            MConcentrator = 91,
            PulsarRadio = 93,
            Impulse = 121
        }
        public enum InterfaceModels
        {
            CAN1,
            CAN2,
            CAN3,
            RS485_2,
            RS485_1,
            RS232,
            CONCENTRATOR
        }

        private List<byte> wrapCmd(string cmd, string prms = "")
        {
            List<byte> fullCmdList = new List<byte>();

            string CRCFeedString = this.password + SEPARATOR + cmd + prms;
            byte[] CRCFeedASCIIArr = ASCIIEncoding.ASCII.GetBytes(CRCFeedString);
            fullCmdList.AddRange(CRCFeedASCIIArr);

            byte[] CRCASCIIByteArr = makeCRC(CRCFeedASCIIArr);
            string CRCASCIIStr = Encoding.ASCII.GetString(CRCASCIIByteArr).Replace("-", "");
            fullCmdList.AddRange(CRCASCIIByteArr);

            byte[] stopSignArr = new byte[] { 0x0A, 0x0A };
            string stopSignString = ASCIIEncoding.ASCII.GetString(stopSignArr);
            fullCmdList.AddRange(stopSignArr);

            //для наглядности
            string strASCIICmd = CRCFeedString + CRCASCIIStr + stopSignString;

            return fullCmdList;
        }

        public enum UM_VERSION
        {
            UM31,
            UM40,
            UNKNOWN
        }

        public int softwareVersion = 22;


        #region Служебные

        public void sendAbort() 
        {
            List<byte> cmd = wrapCmd("ABORT");
            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);
       
        }

        public bool readUMSerial(ref string serial_number)
        {
            WriteToLog("readUMSerial start");
            List<byte> cmd = wrapCmd("UM_READ_ID");

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

            WriteToLog("readUMSerial, incoming: " + ASCIIEncoding.ASCII.GetString(incommingData));

            if (incommingData.Length < MESSAGE_TAIL_SIZE_BYTES + 1) return false;
            byte[] cuttedIncommingData = new byte[incommingData.Length - MESSAGE_TAIL_SIZE_BYTES];
            Array.Copy(incommingData, 0, cuttedIncommingData, 0, cuttedIncommingData.Length);

            string answ = ASCIIEncoding.ASCII.GetString(cuttedIncommingData);
            if (!answ.Contains("UM_ID")) return false;

            serial_number = answ.Replace("UM_ID=", "");

            WriteToLog("Serial number readed: " + serial_number);

            return true;
        }

        public bool readUMName(out UM_VERSION um_version)
        {
            um_version = UM_VERSION.UNKNOWN;
            List<byte> cmd = wrapCmd("RDIAGN");

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

            if (incommingData.Length < MESSAGE_TAIL_SIZE_BYTES + 1) return false;
            byte[] cuttedIncommingData = new byte[5];
            Array.Copy(incommingData, 0, cuttedIncommingData, 0, 5);

            string umName = ASCIIEncoding.ASCII.GetString(cuttedIncommingData);

            if (umName == "UM-31") um_version = UM_VERSION.UM31;
            else if (umName == "UM-40") um_version = UM_VERSION.UM40;

            return true;
        }

        public bool readSWVersion(out int version)
        {

            version = 22;

            List<byte> cmd = wrapCmd("GETSWVER");

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

            if (incommingData.Length < MESSAGE_TAIL_SIZE_BYTES + 1) return false;
            byte[] cuttedIncommingData = new byte[incommingData.Length - MESSAGE_TAIL_SIZE_BYTES];
            Array.Copy(incommingData, 0, cuttedIncommingData, 0, cuttedIncommingData.Length);

            string answ = ASCIIEncoding.ASCII.GetString(cuttedIncommingData);
            WriteToLog(answ);

            if (!answ.Contains("SW")) return false;

            return int.TryParse(answ.Replace("SW=", ""), out version);
        }

        #endregion

        #region Таблица приборов

        public struct MetersTableEntry
        {
            public int id;
            public string meterName;
            public int networkAddr;
            public string interfaceType;
            public string passFormat;
            public string pass1;
            public string pass2;
        }

        public bool getMetersTableEntriesNumber(out int cnt)
        {
            int emergencyNumberOfRecords = 4;

            cnt = 0;
            //определим длину таблицы с приборами
            List<byte> cmd = wrapCmd("RTABLLEN");

            int attempts = 0;

            TRY_AGAIN:

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

            if (incommingData.Length < 11)
            {
                if (attempts == 0)
                {
                    //m_vport.Close();
                    attempts++;
                    Thread.Sleep(1000);

                    Thread.Sleep(1000);
                    goto TRY_AGAIN;
                }
                else
                {
                    cnt = emergencyNumberOfRecords;
                    return true;
                }

            } 

            string answ = ASCIIEncoding.ASCII.GetString(incommingData);
            if (!answ.Contains("TABLLEN"))
            {
                return false;
            }

            string countStr = answ.Substring(8, 3);
            if (!int.TryParse(countStr, out cnt)) return false;

            return true;
        }

        private bool parseMetersTableEntry(byte[] entry, out List<string> metersTableEntryList, ref MetersTableEntry metersTableEntry)
        {
            metersTableEntry = new MetersTableEntry();
            metersTableEntryList = new List<string>();

            string str = ASCIIEncoding.ASCII.GetString(entry);
            if (!str.Contains("TABLEX")) return false;

            string[] strItems = str.Replace("TABLEX=","").Split(';');

            WriteToLog(str);

            if (strItems.Length < 5) return false;

            int moveIndex = 0;
            if (softwareVersion < 22) moveIndex = 1;


            metersTableEntry.id = int.Parse(strItems[0]);
            metersTableEntry.meterName = ((MeterModels)int.Parse(strItems[moveIndex + 1])).ToString();
            metersTableEntry.networkAddr = int.Parse(strItems[moveIndex + 2]);
            metersTableEntry.interfaceType = ((InterfaceModels)int.Parse(strItems[moveIndex + 6])).ToString();
            metersTableEntry.pass1 = strItems[moveIndex + 3];
            metersTableEntry.pass2 = strItems[moveIndex + 4];

            metersTableEntryList.Add(metersTableEntry.id.ToString());
            metersTableEntryList.Add(metersTableEntry.meterName);
            metersTableEntryList.Add(metersTableEntry.networkAddr.ToString());
           // metersTableEntryList.Add(metersTableEntry.interfaceType);
          //  metersTableEntryList.Add(metersTableEntry.pass1);
          //  metersTableEntryList.Add(metersTableEntry.pass2);

            return true;
        }

        public bool getMetersTable(ref DataTable metersTable)
        {

            readSWVersion(out softwareVersion);

            int metersCount = 0;
            if (!getMetersTableEntriesNumber(out metersCount)) return false;

            metersTable = new DataTable();
            DataTable metersDt = metersTable;

            DataColumn tmpCol = new DataColumn();
            tmpCol.Caption = "ID";
            tmpCol.ColumnName = "colID";
            metersDt.Columns.Add(tmpCol);

            tmpCol = new DataColumn();
            tmpCol.Caption = "Модель";
            tmpCol.ColumnName = "colMeterModel";
            metersDt.Columns.Add(tmpCol);

            tmpCol = new DataColumn();
            tmpCol.Caption = "Сетевой адрес";
            tmpCol.ColumnName = "colNetwAddr";
            metersDt.Columns.Add(tmpCol);

            //tmpCol = new DataColumn();
            //tmpCol.Caption = "Интерфейс";
            //tmpCol.ColumnName = "colInterfaceName";
            //metersDt.Columns.Add(tmpCol);

            //tmpCol = new DataColumn();
            //tmpCol.Caption = "П1";
            //tmpCol.ColumnName = "colPass1";
            //metersDt.Columns.Add(tmpCol);

            //tmpCol = new DataColumn();
            //tmpCol.Caption = "П2";
            //tmpCol.ColumnName = "colPass2";
            //metersDt.Columns.Add(tmpCol);

            DataRow captionRow = metersDt.NewRow();
            for (int i = 0; i < metersDt.Columns.Count; i++)
                captionRow[i] = metersDt.Columns[i].Caption;
            metersDt.Rows.Add(captionRow);

            //счет с 1го у них
            for (int i = 0; i < metersCount; i++)
            {
                DataRow dRow = metersDt.NewRow();
                List<byte> cmd = wrapCmd("READTABLEX=" + i);
                int attempts = 0;

            TRY_AGAIN:
                byte[] incommingData = new byte[1];
                m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

                string answ = ASCIIEncoding.ASCII.GetString(incommingData);
                if (!answ.Contains("TABLEX")) continue;



                MetersTableEntry mte = new MetersTableEntry();
                List<string> mteList = new List<string>();

                if (!parseMetersTableEntry(incommingData, out mteList, ref mte)) continue;
                if (mteList.Count == 0) continue;

                for (int j = 0; j < mteList.Count; j++)
                    dRow[j] = mteList[j];

                metersDt.Rows.Add(dRow);
            }

            if (metersDt.Rows.Count == 0) return false;


            return true;
        }

        #endregion

        #region Значения

        private bool getDateFromAnswString(string answ, ref DateTime date)
        {
            //получает первую дату из поданой части ответа

            date = new DateTime();

            try
            {
                string pattern = "DT\\s.*<";
                Regex reg = new Regex(pattern);
                string res = reg.Match(answ).Groups[0].Value.Replace("DT\n", "").Replace("<", "");

                CultureInfo provider = CultureInfo.InvariantCulture;
                string dateStr = res.Remove(res.Length - 5);
                string syncrFlagStr = res.Remove(0, res.Length - 4).Remove(2, 2);
                string winterFlagStr = res.Remove(0, res.Length - 1);

                bool areClockSincronized = syncrFlagStr == "02" ? true : false;
                bool isWinterTime = winterFlagStr == "0" ? true : false;

                DateTime dt = DateTime.ParseExact(dateStr, "dd.MM.yy HH:mm:ss", CultureInfo.InvariantCulture);
              
                date = dt;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool getMetersSNFromAnswString(string answ, ref string serialNumberStr)
        {
            string pattern = ";\\d*<";

            try
            {
                Regex reg = new Regex(pattern);
                serialNumberStr = reg.Match(answ).Groups[0].Value.Replace(";", "").Replace("<", "");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool getRecordsDictionary(string answ, ref Dictionary<string, float> records, float coefficient = 1000)
        {
            Dictionary<string, float> tmpDict = new Dictionary<string, float>();

            string startSign = "\n<";
            string endSign = "<";

            int index = answ.IndexOf(startSign);

            while (index != -1)
            {
                int valFirstLetterIndex = index + startSign.Length;

                int secondIndex = answ.IndexOf(endSign, valFirstLetterIndex + 1);
                if (secondIndex == -1) break;

                int valLastLetterIndex = secondIndex - endSign.Length;
                int valStrLength = valLastLetterIndex - valFirstLetterIndex + 1;

                string tmpRecordStr = answ.Substring(valFirstLetterIndex, valStrLength);
                string[] tmpRecordArr = tmpRecordStr.Split('\n');

                string tmpValue = tmpRecordArr[1];
                float resVal = 0;

                if (!float.TryParse(tmpValue,  NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out resVal))
                    if (tmpValue != "?") 
                        return false;

                tmpDict.Add(tmpRecordArr[0], resVal / coefficient);

                index = answ.IndexOf(startSign, secondIndex);
            }

            records = tmpDict;
            return true;

        }

        public bool getDailyValuesForID(int id, DateTime dt, out List<ValueUM> umVals)
        {
            umVals = new List<ValueUM>();

            string cmdStr = "READDAY=" + dt.ToString("yy") + "." + dt.ToString("MM") + "." + dt.ToString("dd") +
                ";" + id;
            List<byte> cmd = wrapCmd(cmdStr);

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);



            string answ = ASCIIEncoding.ASCII.GetString(incommingData);
            WriteToLog("getDailyValuesForID: incomming: " + answ);

            List<string> recordStringsForDates = new List<string>();

            int endIndex = answ.IndexOf("\nEND");
            if (endIndex == -1)
            {
                WriteToLog("getDailyValuesForID: No end tag in incomming");
                return false;
            }
            string tmpMeterSerial = "";
            getMetersSNFromAnswString(answ, ref tmpMeterSerial);


            int indexDt = answ.IndexOf("<DT");
            while (indexDt != -1)
            {
                int tmpIndexDt = answ.IndexOf("<DT", indexDt + 1);
                string tmpVal = "";
                if (tmpIndexDt == -1)
                {
                    tmpVal = answ.Substring(indexDt, endIndex - indexDt + 1);
                }
                else
                {
                    tmpVal = answ.Substring(indexDt, tmpIndexDt - indexDt + 1);
                }

                indexDt = tmpIndexDt;
                recordStringsForDates.Add(tmpVal);
            }

            if (recordStringsForDates.Count > 1)
                WriteToLog("Суточные: на данную дату пришло несколько значений, возможно расходятся часы");
            if (recordStringsForDates.Count == 0)
            {
                WriteToLog("getDailyValuesForID: recordStringsForDates.Count == 0");
                return false;
            }

            DateTime recordDt = new DateTime();
            if (!getDateFromAnswString(recordStringsForDates[0], ref recordDt))
            {
                WriteToLog("getDailyValuesForID: getDateFromAnswString returned false");
                return false;
            }
            string selectedRecordString = recordStringsForDates[0];

            //получим блок TD
            string tdStartSign = "<TD\n";
            int tdIndex = selectedRecordString.IndexOf(tdStartSign);

            int secondIndex = selectedRecordString.IndexOf(tdStartSign, tdIndex + tdStartSign.Length);
            if (secondIndex == -1)
            {
                secondIndex = endIndex;
            }else{
                WriteToLog("Внимание, несколько тегов TD!");
            }

            string tdString = selectedRecordString.Substring(tdIndex, selectedRecordString.Length - tdIndex - 1);

            Dictionary<string, float> recordsDict = new Dictionary<string, float>();
            if (!getRecordsDictionary(tdString, ref recordsDict))
            {
                WriteToLog("getRecordsDictionary returned false");
                return false;
            }

            if (recordsDict.Count != 20)
            {
                WriteToLog("getRecordsDictionary: число записей не равно 20");
                //return false;
            }

            int cnt = 0;
            
            foreach (string s in recordsDict.Keys)
            {
                ValueUM tmpVal = new ValueUM();
                tmpVal.dt = recordDt;
                tmpVal.name = s;
                tmpVal.value = recordsDict[s];
                tmpVal.meterSN = tmpMeterSerial;

                umVals.Add(tmpVal);
                cnt++;
            }

            if (umVals.Count > 0)
            {
                WriteToLog("Successfully readed: " + umVals.Count);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool getDailyValuesForID(int id, out List<ValueUM> umVals)
        {
            WriteToLog("getDailyValuesForID METHOD START");

            umVals = new List<ValueUM>();

            string cmdStr = "READCURR=" + id;
            List<byte> cmd = wrapCmd(cmdStr);

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

            string answ = ASCIIEncoding.ASCII.GetString(incommingData);

            WriteToLog("getDailyValuesForID, answer: " + answ);

            List<string> recordStringsForDates = new List<string>();

            int endIndex = answ.IndexOf("\nEND");
            if (endIndex == -1) return false;

            string tmpMeterSerial = "";
            getMetersSNFromAnswString(answ, ref tmpMeterSerial);


            int indexDt = answ.IndexOf("<DT");
            while (indexDt != -1)
            {
                int tmpIndexDt = answ.IndexOf("<DT", indexDt + 1);
                string tmpVal = "";
                if (tmpIndexDt == -1)
                {
                    tmpVal = answ.Substring(indexDt, endIndex - indexDt + 1);
                }
                else
                {
                    tmpVal = answ.Substring(indexDt, tmpIndexDt - indexDt + 1);
                }

                indexDt = tmpIndexDt;
                recordStringsForDates.Add(tmpVal);
            }

            if (recordStringsForDates.Count > 1)
                WriteToLog("Суточные: на данную дату пришло несколько значений, возможно расходятся часы");


            if (recordStringsForDates.Count == 0)
            {
                WriteToLog("recordStringsForDates == 0");
                return false;
            }

            DateTime recordDt = new DateTime();
            if (!getDateFromAnswString(recordStringsForDates[0], ref recordDt)) return false;

            string selectedRecordString = recordStringsForDates[0];

            //получим блок TD
            string tdStartSign = "<TD\n";
            int tdIndex = selectedRecordString.IndexOf(tdStartSign);

            int secondIndex = selectedRecordString.IndexOf(tdStartSign, tdIndex + tdStartSign.Length);
            if (secondIndex == -1)
            {
                secondIndex = endIndex;
            }
            else
            {
                WriteToLog("Внимание, несколько тегов TD!");
            }



            string tdString = selectedRecordString.Substring(tdIndex, selectedRecordString.Length - tdIndex - 1);
            WriteToLog("TDSTRING: " + tdString);


            Dictionary<string, float> recordsDict = new Dictionary<string, float>();
            if (!getRecordsDictionary(tdString, ref recordsDict))
            {
                WriteToLog("getRecordsDictionary == false");
                return false;
            }
                WriteToLog("recordsDictCnt: " + recordsDict.Count);

           // if (recordsDict.Count != 20) return false;

            int cnt = 0;

            foreach (string s in recordsDict.Keys)
            {
                ValueUM tmpVal = new ValueUM();
                tmpVal.dt = recordDt;
                tmpVal.name = s;
                tmpVal.value = recordsDict[s];
                tmpVal.meterSN = tmpMeterSerial;

                umVals.Add(tmpVal);
                cnt++;
            }

            WriteToLog("umVals.Count == " + umVals.Count);

            if (umVals.Count > 0)
                return true;
            else
                return false;
        }


        public bool getMonthlyValuesForID(int id, DateTime dt, out List<ValueUM> umVals)
        {
            umVals = new List<ValueUM>();

            string cmdStr = "READMONTH="+ dt.ToString("MM") + "." + dt.ToString("yy") +
                ";" + id;
            List<byte> cmd = wrapCmd(cmdStr);

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

            string answ = ASCIIEncoding.ASCII.GetString(incommingData);

            List<string> recordStringsForDates = new List<string>();

            int endIndex = answ.IndexOf("\nEND");
            if (endIndex == -1) return false;

            string tmpMeterSerial = "";
            getMetersSNFromAnswString(answ, ref tmpMeterSerial);

            int indexDt = answ.IndexOf("<DT");
            while (indexDt != -1)
            {
                int tmpIndexDt = answ.IndexOf("<DT", indexDt + 1);
                string tmpVal = "";
                if (tmpIndexDt == -1)
                {
                    tmpVal = answ.Substring(indexDt, endIndex - indexDt + 1);
                }
                else
                {
                    tmpVal = answ.Substring(indexDt, tmpIndexDt - indexDt + 1);
                }

                indexDt = tmpIndexDt;
                recordStringsForDates.Add(tmpVal);
            }

            if (recordStringsForDates.Count > 1)
                WriteToLog("Месячные: на данную дату пришло несколько значений, возможно расходятся часы");
            if (recordStringsForDates.Count == 0) return false;

            DateTime recordDt = new DateTime();
            if (!getDateFromAnswString(recordStringsForDates[0], ref recordDt)) return false;

            string selectedRecordString = recordStringsForDates[0];

            //получим блок TD
            string tdStartSign = "<TD\n";
            int tdIndex = selectedRecordString.IndexOf(tdStartSign);

            int secondIndex = selectedRecordString.IndexOf(tdStartSign, tdIndex + tdStartSign.Length);
            if (secondIndex == -1)
            {
                secondIndex = endIndex;
            }
            else
            {
                WriteToLog("Внимание, несколько тегов TD!");
            }

            string tdString = selectedRecordString.Substring(tdIndex, selectedRecordString.Length - tdIndex - 1);

            Dictionary<string, float> recordsDict = new Dictionary<string, float>();
            if (!getRecordsDictionary(tdString, ref recordsDict)) return false;


            if (recordsDict.Count != 20) return false;

            int cnt = 0;

            foreach (string s in recordsDict.Keys)
            {
                if (cnt == 5) break;
                if (!s.Contains("MA+")) continue;

                ValueUM tmpVal = new ValueUM();
                tmpVal.dt = recordDt;
                tmpVal.name = s;
                tmpVal.value = recordsDict[s];
                tmpVal.meterSN = tmpMeterSerial;

                umVals.Add(tmpVal);
                cnt++;
            }


            //разбор значений

            return true;
        }

        public bool getSlicesValuesForID(int id, DateTime dt_start, DateTime dt_end, out List<ValueUM> umVals)
        {
            umVals = new List<ValueUM>();

            string cmdStr = "READSTATEUTC=" + dt_start.ToString("yy") + "." + dt_start.ToString("MM") + "." + dt_start.ToString("dd") +
                " " + dt_start.ToString("MM") + ":" + dt_start.ToString("MM") + ":" + dt_start.ToString("cc") + " " + Convert.ToInt32(dt_start.IsDaylightSavingTime()).ToString() +
                " " + dt_end.ToString("HH") + "." + dt_end.ToString("mm") + "." + dt_end.ToString("dd") +
                " " + dt_end.ToString("MM") + ":" + dt_end.ToString("MM") + ":" + dt_end.ToString("cc") + " " + Convert.ToInt32(dt_start.IsDaylightSavingTime()).ToString() +
                " ;" + id;
            List<byte> cmd = wrapCmd(cmdStr);

            byte[] incommingData = new byte[1];
            m_vport.WriteReadData(FindPacketSignature, cmd.ToArray(), ref incommingData, cmd.Count, -1);

            string answ = ASCIIEncoding.ASCII.GetString(incommingData);

            //разбор значений

            return true;
        }

        public bool findValueInListByName(string name, List<ValueUM> values, out ValueUM result)
        {
             result = values.Find((x) => {return x.name == name;});

             if (result.name == null) return false;
            //todo
             return true;
        }


        #endregion

        #region CRC Вычисление

        UInt16 crc16_update_poly(UInt16 crc, byte a)
        {
            crc ^= a;
            for (byte i = 0; i < 8; ++i)
            {
                if ((crc & 1) == 1)
                    crc = (UInt16)((crc >> 1) ^ 0xA001);
                else
                    crc = (UInt16)(crc >> 1);
            }
            return crc;
        }

        UInt16 crc16_calc_poly(byte[] buf, int len, UInt16 crc)
        {

            for (int i = 0; i < len; i++)
                crc = crc16_update_poly(crc, buf[i]);

            return crc;
        }

        byte[] makeCRC(byte[] buf)
        {
            UInt16 crc = 0x40BF;
            UInt16 resCrcNumb = crc16_calc_poly(buf, buf.Length, crc);

            char[] CRCCharArr = Convert.ToString(resCrcNumb, 16).ToUpper().ToCharArray();
            int zerosNeeded = 4 - CRCCharArr.Length;

            List<char> charList = new List<char>();
            for (int i = 0; i < zerosNeeded; i++)
                charList.Add('0');
            charList.AddRange(CRCCharArr);

            CRCCharArr = charList.ToArray();

            List<byte> CRCByteList = new List<byte>();
            byte[] CRCASCIICharBytes = Encoding.ASCII.GetBytes(CRCCharArr);
            WriteToLog(BitConverter.ToString(CRCASCIICharBytes));

            CRCByteList.Add(CRCASCIICharBytes[2]);
            CRCByteList.Add(CRCASCIICharBytes[3]);
            CRCByteList.Add(CRCASCIICharBytes[0]);
            CRCByteList.Add(CRCASCIICharBytes[1]);

            return CRCByteList.ToArray();
        }

        #endregion


        #region Реализация интерфейса СО

        private int FindPacketSignature(Queue<byte> queue)
        {
            //определим конец пакета

            if (queue.Count < 2) return 0;

            byte last = queue.Dequeue();
            byte preLast = queue.Dequeue();
            if (last == preLast && preLast == 0x0a) return 1;
            else
                return 0;
        }

        public bool OpenLinkCanal()
        {
            string serial = "";
            for (int i = 0; i < 3; i++)
            {
                if (readUMSerial(ref serial))
                {
                    WriteToLog("OpenLinkCanal true!");
                    return true;
                }
            }

            WriteToLog("OpenLinkCanal false!");
            return false;
        }

        public bool ReadSerialNumber(ref string serial_number)
        {
            serial_number = "";

            List<ValueUM> valList = new List<ValueUM>();
            if (listOfDailyValues.Count > 0) valList = listOfDailyValues;
            else if (listOfMonthlyValues.Count > 0) valList = listOfMonthlyValues;
            else if (!getDailyValuesForID(meterId, DateTime.Now.Date, out valList) || valList.Count == 0) return false;

            serial_number = valList[0].meterSN;
            return true;
        }

        
        List<ValueUM> listOfDailyValues = new List<ValueUM>();
        public bool ReadDailyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            WriteToLog("Begin to read daily: ");
            if (listOfDailyValues == null || listOfDailyValues.Count == 0)
            {
               if (!getDailyValuesForID(meterId, dt, out listOfDailyValues))
               // if (!getDailyValuesForID(meterId, out listOfDailyValues))
                {
                    WriteToLog("getDailyValuesForID returned false ");
                    return false;
                } 
            }

            string paramName = dailyCorrelationDict[param];
           //string paramName = currCorrelationDict[param];
            string fullParamName = paramName + tarif.ToString();
            
            ValueUM val = new ValueUM();
            if (!findValueInListByName(fullParamName, listOfDailyValues, out val))
            {
                WriteToLog("can't findValueInListByName...");
                return false;
            }

            WriteToLog("success in reading " + val.name + " = " + val.value);
            recordValue = val.value;

            return true;
        }

        List<ValueUM> listOfMonthlyValues = new List<ValueUM>();
        public bool ReadMonthlyValues(DateTime dt, ushort param, ushort tarif, ref float recordValue)
        {
            if (listOfMonthlyValues == null || listOfMonthlyValues.Count == 0)
            {
                if (!getMonthlyValuesForID(meterId, dt, out listOfMonthlyValues)) return false;
            }

            string paramName = monthlyCorrelationDict[param];
            string fullParamName = paramName + tarif.ToString();

            ValueUM val = new ValueUM();
            if (!findValueInListByName(fullParamName, listOfMonthlyValues, out val)) return false;

            recordValue = val.value;

            return true;
        }

        List<ValueType> listOfSliceValues = new List<ValueType>();
        public bool ReadPowerSlice(DateTime dt_begin, DateTime dt_end, ref List<RecordPowerSlice> listRPS, byte period)
        {


            return true;
        }

        #endregion

        #region Неиспользуемые методы интерфейса

        public List<byte> GetTypesForCategory(CommonCategory common_category)
        {
            return null;
        }

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
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

        public bool ReadSliceArrInitializationDate(ref DateTime lastInitDt)
        {
            return false;
        }

        public bool SyncTime(DateTime dt)
        {
            return false;
        }

        #endregion


    }
}
