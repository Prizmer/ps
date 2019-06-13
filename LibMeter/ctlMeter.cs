using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using PollingLibraries.LibPorts;

namespace Drivers.LibMeter
{
    public partial class ctlMeters : UserControl
    {
        CtlSettings _settings = new CtlSettings();
        IMeter _meter = null;
        VirtualPort _vp = null;

        public ctlMeters()
        {
            InitializeComponent();

            dtpDailyMonthly.Value = DateTime.Now.Date;
            dtpFrom.Value = DateTime.Now.Date.AddDays(-1);
            dtpTo.Value = DateTime.Now.Date;
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
        public string AddressMeter
        {
            get
            {
                return _settings.AddressMeter;
            }
            set
            {
                _settings.AddressMeter = value;
                tbMeterAddress.Text = _settings.AddressMeter;
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

        public void InitializeMeter(IMeter meter, VirtualPort vp)
        {
            _meter = meter;
            _vp = vp;

            ushort addr = (ushort)_settings.AddressParam;
            ushort tarif = (ushort)_settings.ChannelParam;
            string pass = _settings.PasswordMeter == null ? "" : _settings.PasswordMeter;

            _meter.Init(addr, pass, _vp);

        }

        public event EventHandler<EventArgsValue> ValueIsReady;



        private void appendToLog(string msg)
        {
            rtbLog.Text += "\n" + DateTime.Now.ToShortTimeString() + ": " + msg;
        }


        private bool openChannel()
        {
            string msg = "";
            if (_meter == null || _vp == null)
            {
                msg = "Компонент не проинициализирован _meter, _vp";
                appendToLog(msg);
                return false;
            }

            // на случай если изменились адрес, тариф или пароль
            InitializeMeter(_meter, _vp);

            bool resOpenChannel = _meter.OpenLinkCanal();
            if (!resOpenChannel)
            {
                msg = "Не удалось открыть канал методом OpenLinkCanal";
                appendToLog(msg);
            }

            return true;
        }

        private bool readParam(string prmTypeLbl, out float val)
        {
            val = -1;
            string msg = "";

            if (!openChannel())
                return false;

            ushort addr = (ushort)_settings.AddressParam;
            ushort tarif = (ushort)_settings.ChannelParam;

            switch (prmTypeLbl)
            {
                case "curr":
                    {
                        if (!_meter.ReadCurrentValues(addr, tarif, ref val))
                        {
                            msg = "ReadCurrentValues вернул false";
                            appendToLog(msg);
                            return false;
                        }

                        break;
                    }
                case "day":
                    {
                        if (!_meter.ReadDailyValues(dtpDailyMonthly.Value.Date, addr, tarif, ref val))
                        {
                            msg = "ReadDailyValues вернул false";
                            appendToLog(msg);
                            return false;
                        }

                        break;
                    }
                case "month":
                    {
                        if (!_meter.ReadMonthlyValues(dtpDailyMonthly.Value.Date, addr, tarif, ref val))
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
        
        private bool readInfo(out string info)
        {
            info = "Ошибка";

            if (!openChannel())
                return false;

            bool resSerial = _meter.ReadSerialNumber(ref info);
 
            return resSerial;
        }

        private void btnReadParam_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string prmTypeLbl = btn.Tag.ToString();
            string msg = "";

            float resVal = -1;
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

        private void rtbLog_DoubleClick(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }
    }

    public struct CtlSettings
    {
        public bool EnableHalfs;
        public bool EnableAuxilary;

        public bool EnableCurrent;
        public bool EnableDaily;
        public bool EnableMonthly;

        public string AddressMeter;
        public string PasswordMeter;

        public int AddressParam;
        public int ChannelParam;
    };

    public class EventArgsValue : EventArgs
    {
        private float _value;
        private string _valueStr;
        private string _message;
        private bool _status;
        private bool _isStringVal;

        public dynamic Value
        {
            get
            {
                if (_isStringVal)
                    return _valueStr;
                else
                    return _value;
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
        public bool IsString
        {
            get
            {
                return _isStringVal;
            }
        }

        public EventArgsValue(float value, string message, bool status)
        {
            _isStringVal = false;

            this._valueStr = "";
            this._value = value;
            this._message = message;
            this._status = status;
        }

        public EventArgsValue(string value, string message, bool status)
        {
            _isStringVal = true;

            this._valueStr = value;
            this._value = 0;
            this._message = message;
            this._status = status;
        }

    }
}
