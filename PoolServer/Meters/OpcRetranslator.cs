using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using Prizmer.Ports;
using Prizmer.Meters.iMeters;

namespace Prizmer.Meters
{
    class OpcRetranslator : CMeter, IMeter
    {
        private string sPrgId = "";
        private string sItemTag = "";


        public void Init(uint address, string pass, VirtualPort data_vport)
        {
            try
            {
                string[] p = pass.Split('#');
                this.sPrgId = p[0];
                this.sItemTag = p[1];
            }
            catch (Exception ex)
            {
                sPrgId = "Logika.DA.2";
                sItemTag = "SPbus.SPG762.p1.300(Potr)";
            }


            this.m_vport = data_vport;
        }

        private bool Connect()
        {
            if (sPrgId == "" || sItemTag == "")
                return false;

            byte[] bHeader = new byte[4];
            char cCmd = 'C';
            byte cmd = (byte)cCmd;
            byte dataSign = 0x44;
            byte endSign = 0x16;

            bHeader[0] = 0x68;
            bHeader[3] = 0x68;

            Encoding enc = Encoding.ASCII;
            byte[] prgIdAsciiBytes = enc.GetBytes(sPrgId);
            byte prgIdLength = (byte)prgIdAsciiBytes.Length;
            byte[] ipAddrArr = { 127, 0, 0, 1 };

            List<byte> dataList = new List<byte>();
            dataList.Add(cmd);
            dataList.Add(dataSign);
            dataList.AddRange(ipAddrArr);
            dataList.Add(dataSign);
            dataList.Add(prgIdLength);
            dataList.AddRange(prgIdAsciiBytes);

            bHeader[1] = (byte)dataList.Count;
            bHeader[2] = (byte)dataList.Count;

            byte cs = 0x0;
            for (int i = 0; i < dataList.Count; i++)
                cs += dataList[i];

            List<byte> resultCmdList = new List<byte>();
            resultCmdList.AddRange(bHeader);
            resultCmdList.AddRange(dataList);
            resultCmdList.Add(cs);
            resultCmdList.Add(endSign);

            //режим, когда незнаем сколько байт нужно принять
            byte[] inp = new byte[1];
            byte[] cmdArr = resultCmdList.ToArray();
            m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, -1);

            if (inp.Length == 1 && inp[0] == 0xE5)
                return true;
            else
                return false;
        }

        public bool OpenLinkCanal()
        {
            return true;
        }

        private bool GetValue(byte[] msgBytes, out float value)
        {
            value = 0f;

            if (msgBytes.Length < 9)
                return false;

            Encoding enc = Encoding.ASCII;

            if (msgBytes[0] == msgBytes[3] && msgBytes[1] == msgBytes[2])
            {
                int dataLength = msgBytes[1];
                if (msgBytes[4 + dataLength + 1] == 0x16)
                {
                    int cmdIndex = 4;
                    char cmd = (char)msgBytes[cmdIndex];
                    switch (cmd)
                    {
                        case 'R':
                            {
                                //вернулись запрошенные данные

                                //качество
                                int stringQualityLengthIndex = cmdIndex + 2;
                                int stringQualityLength = msgBytes[stringQualityLengthIndex];
                                string stringQuality = enc.GetString(msgBytes, stringQualityLengthIndex + 1, stringQualityLength);


                                int iQuality = 0;
                                bool res1 = int.TryParse(stringQuality, out iQuality);
                                if (!res1) return res1;

                               // if ((iQuality >> 6) != 0x03)
                                   // return false;

                                //значение
                                int stringValueLengthIndex = stringQualityLengthIndex + stringQualityLength + 2;
                                int stringValueLength = msgBytes[stringValueLengthIndex];
                                string stringValue = enc.GetString(msgBytes, stringValueLengthIndex + 1, stringValueLength);

                                float fTmpVal = 0f;
                                bool res2 = float.TryParse(stringValue, out fTmpVal);
                                if (!res2) return res2;
                                value = fTmpVal;

                                return true;
                            }
                        case 'E':
                            {
                                //ввывести сообщение об ошибке
                                return false;
                            }
                        default:
                            {
                                return false;
                            }
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public bool ReadCurrentValues(ushort param, ushort tarif, ref float recordValue)
        {
            if (!Connect()) return false;

            byte[] bHeader = new byte[4];
            char cCmd = 'R';
            byte cmd = (byte)cCmd;
            byte dataSign = 0x44;
            byte endSign = 0x16;

            bHeader[0] = 0x68;
            bHeader[3] = 0x68;


            Encoding enc = Encoding.ASCII;
            byte[] itemTagAsciiBytes = enc.GetBytes(sItemTag);
            byte tagLength = (byte)itemTagAsciiBytes.Length;

            List<byte> dataList = new List<byte>();
            dataList.Add(cmd);
            dataList.Add(dataSign);
            dataList.Add(tagLength);
            dataList.AddRange(itemTagAsciiBytes);

            bHeader[1] = (byte)dataList.Count;
            bHeader[2] = (byte)dataList.Count;

            byte cs = 0x0;
            for (int i = 0; i < dataList.Count; i++)
                cs += dataList[i];

            List<byte> resultCmdList = new List<byte>();
            resultCmdList.AddRange(bHeader);
            resultCmdList.AddRange(dataList);
            resultCmdList.Add(cs);
            resultCmdList.Add(endSign);

            byte[] inp = new byte[1];
            byte[] cmdArr = resultCmdList.ToArray();
            m_vport.WriteReadData(findPackageSign, cmdArr, ref inp, cmdArr.Length, -1);


            return GetValue(inp, out recordValue);
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
