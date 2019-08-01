using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Threading;

using PollingLibraries.LibPorts;

namespace Drivers.LibMeter
{
    public partial class ctlMeters : UserControl
    {
        private CtlSettings _settings = new CtlSettings();
        private IMeter _meter = null;
        private IMeter2 _meter2 = null;
        private VirtualPort _vp = null;

        public event EventHandler<EventArgsValue> ValueIsReady;
        public event EventHandler<EventArgsValue> HalfsAreReady;


        public ctlMeters()
        {
            InitializeComponent();

            dtpDailyMonthly.Value = DateTime.Now.Date;
            dtpFrom.Value = DateTime.Now.Date.AddDays(-1);
            dtpTo.Value = DateTime.Now.Date;

            HalfsAreReady += (object o, EventArgsValue eav) =>
            {
                string msg = "";

                List<RecordPowerSlice> halfsList = eav.Value;
                msg = $"Получено {halfsList.Count} получасовок";
                appendToLog(msg);

                if (halfsList.Count == 0)
                    return;

                string res = "";
                int cnt = 1;
                foreach (RecordPowerSlice half in halfsList)
                {
                    res += $"{cnt}. {half.date_time}: A+ {half.APlus}, A- {half.AMinus}, R+ {half.RPlus}, R- {half.RMinus}, st={half.status}\n";
                    cnt++;
                }

                appendToLog(res);
            };
        }

        #region Свойства компонента

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Включение панели получасовок"), Category("Custom")]
        public bool EnableHalfs
        {
            get
            {
                return _settings.EnableHalfs;
            }
            set
            {
                _settings.EnableHalfs = value;
                panelHalfs.Enabled = _settings.EnableHalfs;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Включение панели дополнительных параметров"), Category("Custom")]
        public bool EnableAuxilary
        {
            get
            {
                return _settings.EnableAuxilary;
            }
            set
            {
                _settings.EnableAuxilary = value;
                gbAuxilary.Enabled = _settings.EnableAuxilary;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Управление чтением текущих"), Category("Custom")]
        public bool EnableCurrent
        {
            get
            {
                return _settings.EnableCurrent;
            }
            set
            {
                _settings.EnableCurrent = value;
                btnReadCurrent.Enabled = _settings.EnableCurrent;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Управление чтением суточных"), Category("Custom")]
        public bool EnableDaily
        {
            get
            {
                return _settings.EnableDaily;
            }
            set
            {
                _settings.EnableDaily = value;
                btnReadDaily.Enabled = _settings.EnableDaily;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Управление чтением месячных"), Category("Custom")]
        public bool EnableMonthly
        {
            get
            {
                return _settings.EnableMonthly;
            }
            set
            {
                _settings.EnableMonthly = value;
                btnReadMonthly.Enabled = _settings.EnableMonthly;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Сетевой адрес прибора"), Category("Custom")]
        public uint AddressMeter
        {
            get
            {
                return _settings.AddressMeter;
            }
            set
            {
                _settings.AddressMeter = value;
                tbMeterAddress.Text = _settings.AddressMeter.ToString();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Пароль к прибору"), Category("Custom")]
        public string PasswordMeter
        {
            get
            {
                return _settings.PasswordMeter;
            }
            set
            {
                _settings.PasswordMeter = value;
                tbMeterPassword.Text = _settings.PasswordMeter;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Адрес запрашиваемого параметра"), Category("Custom")]
        public int AddressParam
        {
            get
            {
                return _settings.AddressParam;
            }
            set
            {
                _settings.AddressParam = value;
                numParamAddr.Value = _settings.AddressParam;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Канал или тариф запрашиваемого параметра"), Category("Custom")]
        public int ChannelParam
        {
            get
            {
                return _settings.ChannelParam;
            }
            set
            {
                _settings.ChannelParam = value;
                numParamTarif.Value = _settings.ChannelParam;
            }
        }

        #endregion

        public void Initialize(IMeter meter, VirtualPort vp)
        {
            _meter = meter;
            _vp = vp;

            string msg = "";
            if (_meter is IMeter2)
            {
                _meter2 = (IMeter2)_meter;
                msg = "Интерфейс IMeter2 поддерживается, показания с точностью double.";
            }
            else
            {
                msg = "Интерфейс IMeter2 НЕ поддерживается, показания с точностью single.";
            }
            appendToLog(msg);

            bool initializationSuccess = InitializeDriver();
        }

        private bool InitializeDriver()
        {
            if (_meter == null || _vp == null)
            {
                appendToLog("Невозможно проиницилизировать драйвер, т.к. компоненту ctlMeters не заданы _meter и _vp. Вызовите сначала метод Initialize");
                return false;
            }

            uint addr = 1;
            if (!uint.TryParse(tbMeterAddress.Text, out addr))
                appendToLog("Не корректный сетевой адрес");

            string pass = tbMeterPassword.Text;

            _meter.Init(addr, pass, _vp);

            return true;
        }
        public IMeter GetInitializedDriver()
        {
            if (InitializeDriver())
                return _meter;
            else
                return null;
        }
        public VirtualPort VirtualPort
        {
            get
            {
                return _vp;
            }
        }


        private bool openChannel()
        {
            string msg = "";

            // на случай если изменились адрес, тариф или пароль
            InitializeDriver();

            bool resOpenChannel = _meter.OpenLinkCanal();
            if (!resOpenChannel)
            {
                msg = "Не удалось открыть канал методом OpenLinkCanal";
                appendToLog(msg);
            }

            return true;
        }

        private bool readParam(string prmTypeLbl, out double val)
        {
            val = -1;
            string msg = "";
            const string msgNewInterfaceAnnotation = "Точность double";

            if (!openChannel())
                return false;

            ushort addr = (ushort)numParamAddr.Value;
            ushort tarif = (ushort)numParamTarif.Value;

            bool bReadResult = false;
            float tmpFloat = -1f;

            switch (prmTypeLbl)
            {
                case "curr":
                    {
                        if (_meter2 != null)
                        {
                            bReadResult = _meter2.ReadCurrentValues(addr, tarif, ref val) ;
                            appendToLog(msgNewInterfaceAnnotation);
                        }
                        else
                        {
                            bReadResult = _meter.ReadCurrentValues(addr, tarif, ref tmpFloat);
                            val = tmpFloat;
                        }

                        if (!bReadResult)
                        {
                            msg = "ReadCurrentValues вернул false";
                            appendToLog(msg);
                            return false;
                        }

                        break;
                    }
                case "day":
                    {
                        if (_meter2 != null)
                        {
                            bReadResult = _meter2.ReadDailyValues(dtpDailyMonthly.Value.Date, addr, tarif, ref val);
                            appendToLog(msgNewInterfaceAnnotation);
                        }
                        else
                        {
                            bReadResult = _meter.ReadDailyValues(dtpDailyMonthly.Value.Date, addr, tarif, ref tmpFloat);
                            val = tmpFloat;
                        }

                        if (!bReadResult)
                        {
                            msg = "ReadDailyValues вернул false";
                            appendToLog(msg);
                            return false;
                        }

                        break;
                    }
                case "month":
                    {
                        if (_meter2 != null)
                        {
                            bReadResult = _meter2.ReadMonthlyValues(dtpDailyMonthly.Value.Date, addr, tarif, ref val);
                            appendToLog(msgNewInterfaceAnnotation);
                        }
                        else
                        {
                            bReadResult = _meter.ReadMonthlyValues(dtpDailyMonthly.Value.Date, addr, tarif, ref tmpFloat);
                            val = tmpFloat;
                        }

                        if (!bReadResult)
                        {
                            msg = "ReadMonthlyValues вернул false";
                            appendToLog(msg);
                            return false;
                        }

                        break;
                    }
                default:
                    {
                        return false;
                    }
            }

            return true;
        }

        private void readParamHalfsAsync()
        {
            string msg = "";

            if (HalfsAreReady == null)
            {
                msg = "К HalfsAreReady не подключены обработчики";
                appendToLog(msg);
                return;
            }

            this.InProcess = true;
            msg = "Старт асинхронного чтения получасовок...";
            appendToLog(msg);

            ThreadStart threadStart = new ThreadStart(() =>
            {
                EventArgsValue eav = new EventArgsValue(new List<RecordPowerSlice>(), msg, false);
                if (!openChannel())
                {
                    msg = "Не удалось открыть канал";
                    Invoke(new Action(() => {
                        this.InProcess = false;
                    }));
                    HalfsAreReady.Invoke(this, eav);
                    return;
                }

                string m = "";
                List<RecordPowerSlice> tmpHalfsList = new List<RecordPowerSlice>();

                DateTime date_to = new DateTime(dtpTo.Value.Year, dtpTo.Value.Month, dtpTo.Value.Day, 23, 31, 0);
                bool res = _meter.ReadPowerSlice(dtpFrom.Value, date_to, ref tmpHalfsList, 30);
                if (!res)
                {
                    //m = "ReadPowerSlice вернул false";
                    m = "ReadPowerSlice вернул false";
                    appendToLog(m);
                }

                eav = new EventArgsValue(tmpHalfsList, m, res);

                Invoke(new Action(() => {
                    this.InProcess = false;
                }));
                HalfsAreReady.Invoke(this, eav);
            });

            Thread evalThread = new Thread(threadStart);
            evalThread.Start();

            return;
        }
        
        private bool readInfo(out string info)
        {
            info = "Ошибка";

            if (!openChannel())
                return false;

            bool resSerial = _meter.ReadSerialNumber(ref info);
 
            return resSerial;
        }


        private bool _inProcess = false;
        private bool InProcess
        {
            get
            {
                return _inProcess;
            }
            set
            {
                _inProcess = value;

                panelDailyMonthly.Enabled = !value;
                panelHalfs.Enabled = !value;
                gbAuxilary.Enabled = !value;
                panelMain.Enabled = !value;

                pbPreloader.Visible = value;
            }
        }

        private void appendToLog(string msg)
        {
            Action a = () =>
            {
                string prfix = rtbLog.Text != "" ? "\n" : "";
                rtbLog.Text += prfix + DateTime.Now.ToString("HH:mm:ss.fff") + ": " + msg;
            };

            this.Invoke(a);

        }

        #region Обработчики контролов

        private void btnReadParam_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string prmTypeLbl = btn.Tag.ToString();
            string msg = "";

            double resVal = -1;
            bool status = readParam(prmTypeLbl, out resVal);

            if (status)
            {
                msg = prmTypeLbl + " = " + resVal + ";";
                appendToLog(msg);
            }
            else
            {
                msg = "Ошибка";
                appendToLog(msg);
            }

            EventArgsValue evArg = new EventArgsValue(resVal, msg, status);
            ValueIsReady?.Invoke(sender, evArg);
        }

        private void btnReadHalfs_Click(object sender, EventArgs e)
        {  
            readParamHalfsAsync();
        }

        private void btnReadInfo_Click(object sender, EventArgs e)
        {
            string msg = "";
            string info = "";
            bool status = readInfo(out info);


            msg = info;
            appendToLog(msg);

            EventArgsValue evArg = new EventArgsValue(info, msg, status);
            ValueIsReady?.Invoke(sender, evArg);
        }

        private void tbMeterPassword_Leave(object sender, EventArgs e)
        {
            if (_meter != null)
                _meter.Init(_settings.AddressMeter, tbMeterPassword.Text, _vp);
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        #endregion
    }

    public struct CtlSettings
    {
        public bool EnableHalfs;
        public bool EnableAuxilary;

        public bool EnableCurrent;
        public bool EnableDaily;
        public bool EnableMonthly;

        public uint AddressMeter;
        public string PasswordMeter;

        public int AddressParam;
        public int ChannelParam;
    };

    public class EventArgsValue : EventArgs
    {
        private double _value;
        private string _valueStr;
        private List<RecordPowerSlice> _valueEnergyHalfsList;
        private string _message;
        private bool _status;
        private ValueTypes _valueType;

        public enum ValueTypes
        {
            FLOAT,
            STRING,
            ENERGY_HALFS
        }

        public dynamic Value
        {
            get
            {
                dynamic v = null;
                switch (_valueType)
                {
                    case ValueTypes.FLOAT:
                        {
                            v = _value;
                            break;
                        }
                    case ValueTypes.STRING:
                        {
                            v = _valueStr;
                            break;
                        }
                    case ValueTypes.ENERGY_HALFS:
                        {
                            v = _valueEnergyHalfsList;
                            break;
                        }
                }

                return v;
            }
        }

        public string Message
        {
            get
            {
                return _message;
            }
        }
        public bool Status
        {
            get
            {
                return _status;
            }
        }

        public EventArgsValue(double value, string message, bool status)
        {
            _valueType = ValueTypes.FLOAT;

            this._valueStr = "";
            this._value = value;
            this._valueEnergyHalfsList = null;
            this._message = message;
            this._status = status;
        }

        public EventArgsValue(string value, string message, bool status)
        {
            _valueType = ValueTypes.STRING;

            this._valueStr = value;
            this._value = 0;
            this._valueEnergyHalfsList = null;
            this._message = message;
            this._status = status;
        }

        public EventArgsValue(List<RecordPowerSlice> values, string message, bool status)
        {
            _valueType = ValueTypes.ENERGY_HALFS;

            this._valueStr = "";
            this._value = 0;
            this._valueEnergyHalfsList = values;
            this._message = message;
            this._status = status;
        }

    }

}
