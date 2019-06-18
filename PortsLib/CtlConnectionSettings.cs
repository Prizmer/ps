using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Data;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using System.IO.Ports;
using PollingLibraries.LibLogger;
using PollingLibraries.LibPorts;


namespace PollingLibraries.LibPorts
{
    public partial class CtlConnectionSettings: UserControl
    {

        public event EventHandler<EventArgsSettingsApplied> SettingsApplied = null;

        private Logger logger = new Logger();

        private ComPortSettings _comSettings = new ComPortSettings();
        private TCPIPSettings _tcpSettings = new TCPIPSettings();
        private bool _isTCPSelected = true;

        private VirtualPort _virtualPort = null;

        Timer t1 = new Timer();

        public CtlConnectionSettings()
        {
            logger.Initialize("ctlConnectionSettings", false, "customCtl");
            InitializeComponent();
        }

        private void ctlConnectionSettings_Load(object sender, EventArgs e)
        {
            refreshSerialPortComboBox();
            updateComPortSettingFromConfig(ref this._comSettings);


            t1.Interval = 3000;
            t1.Tick += (object ts, EventArgs te) =>
            {
                lblStatus.Text = "";
                t1.Stop();
            };
            updateStatus("OK");

        }

        private void updateStatus(string msg, bool error = false)
        {
            t1.Stop();
            if (error)
                lblStatus.ForeColor = Color.DarkRed;
            else
                lblStatus.ForeColor = Color.DarkGreen;

            this.lblStatus.Text = msg;
            t1.Start();
        }


        #region Методы COM

        private bool refreshSerialPortComboBox(string prefferedPort = "")
        {
            comboBoxComPorts.Items.Clear();
            int startIndex = 0;
            try
            {
                string[] portNamesArr = SerialPort.GetPortNames();
                comboBoxComPorts.Items.AddRange(portNamesArr);
                if (comboBoxComPorts.Items.Count > 0)
                {
            
                    if (prefferedPort != null && prefferedPort.Length > 0)
                    {
                        string formatedPortName = prefferedPort.ToUpper();
                        for (int i = 0; i < portNamesArr.Length; i++)
                            if (portNamesArr[i] == formatedPortName)
                            {
                                startIndex = i;
                                break;
                            }
                    }
   
                    comboBoxComPorts.SelectedIndex = startIndex;
                    return true;
                }
                else
                {
                    logger.LogInfo("В системе не найдены доступные COM порты");
                    return false;
                }
            }
            catch (Exception ex)
            {

                logger.LogError("Ошибка при обновлении списка доступных COM портов: " + ex.Message);
                return false;
            }
        }
        private void updateComPortSettingFromConfig(ref ComPortSettings comPortSettings)
        {
            try
            {
                comPortSettings.baudrate = uint.Parse(ConfigurationSettings.AppSettings["baudrate"]);
                comPortSettings.data_bits = byte.Parse(ConfigurationSettings.AppSettings["databits"]);
                comPortSettings.parity = byte.Parse(ConfigurationSettings.AppSettings["parity"]);
                comPortSettings.stop_bits = byte.Parse(ConfigurationSettings.AppSettings["stopbits"]);
                comPortSettings.bDtr = bool.Parse(ConfigurationSettings.AppSettings["dtr"]);


                tbComConfig.Text = String.Format("{0} {1}-{2}-{3} DTR: {4}", 
                    comPortSettings.baudrate,
                    comPortSettings.data_bits,
                    comPortSettings.parity,
                    comPortSettings.stop_bits,
                    comPortSettings.bDtr
                );

                //comPortSettings.gsm_on = bool.Parse(ConfigurationSettings.AppSettings["GSM_use"]);
                //comPortSettings.gsm_phone_number = ConfigurationSettings.AppSettings["GSM_phone"].ToString();
                //comPortSettings.gsm_init_string = ConfigurationSettings.AppSettings["GSM_init"].ToString();

                
            }
            catch (Exception ex)
            {
                tbComConfig.Text = "Ошибка";
                logger.LogError("Во время чтения параметров из файла конфигурации произошла ошибка: " + ex.Message.ToString());
            }

        }
        private bool updateComPortSettingFromGUI(ref ComPortSettings comPortSettings)
        {
            if (comboBoxComPorts.SelectedIndex > -1)
            {
                comPortSettings.name = comboBoxComPorts.Items[comboBoxComPorts.SelectedIndex].ToString();
                comPortSettings.name = comPortSettings.name.Replace("COM", "");
            }
            else
            {
                return false;
            }

            comPortSettings.read_timeout = (ushort)numericUpDownComReadTimeout.Value;
            comPortSettings.write_timeout = (ushort)numericUpDownComWriteTimeout.Value;

            comPortSettings.gsm_on = cbUseGSM.Checked;
            comPortSettings.gsm_phone_number = tbGSMPhone.Text;
            comPortSettings.gsm_init_string = tbGSMInit.Text;

            return true;
        }

        #endregion

        #region Методы TCP/IP

        private bool updateTCPSettingFromGUI(ref TCPIPSettings tcpIPSettings)
        {
            tcpIPSettings.ip_address = textBoxIp.Text;
            bool res = ushort.TryParse(textBoxPort.Text, out tcpIPSettings.ip_port);
            if (!res)
            {
                return false;
            }

            tcpIPSettings.read_timeout = (ushort)numericUpDownComReadTimeout.Value;
            tcpIPSettings.write_timeout = (ushort)numericUpDownComWriteTimeout.Value;

            tcpIPSettings.delay_between_sending = 50;
            tcpIPSettings.attempts = 1;

            return true;
        }

        #endregion


        private bool setVirtualPort()
        {
            _virtualPort = null;
            string msg = "";
            try
            {
                if (!this._isTCPSelected)
                {
                    if (!updateComPortSettingFromGUI(ref _comSettings))
                    {
                        msg = "Порт не создан. Некорректные свойства COM.";
                        updateStatus(msg, true);
                        logger.LogError(msg);
                        SettingsApplied?.Invoke(this, new EventArgsSettingsApplied(_virtualPort, msg));
                        return false;
                    }
                    _virtualPort = new ComPort(_comSettings);
                }
                else
                {
                    if (!updateTCPSettingFromGUI(ref _tcpSettings))
                    {
                        msg = "Порт не создан. Некорректные свойства TCP/IP.";
                        updateStatus(msg, true);
                        logger.LogError(msg);
                        SettingsApplied?.Invoke(this, new EventArgsSettingsApplied(_virtualPort, msg));
                        return false;
                    }

                   _virtualPort = new TcpipPort(
                       _tcpSettings.ip_address,
                       _tcpSettings.ip_port, 
                       _tcpSettings.write_timeout, 
                       _tcpSettings.read_timeout, 
                       _tcpSettings.delay_between_sending
                       );
                }


                msg = "Порт успешно создан";
                updateStatus(msg);
                SettingsApplied?.Invoke(this, new EventArgsSettingsApplied(_virtualPort, msg));
                return true;
            }
            catch (Exception ex)
            {
                msg = "Ошибка создания виртуального порта: " + ex.Message;
                logger.LogError(msg);
                SettingsApplied?.Invoke(this, new EventArgsSettingsApplied(_virtualPort, msg));
                return false;
            }
        }
        
        public VirtualPort VirtualPort
        {
            get
            {
                return _virtualPort;
            }
        }

        #region Свойства COM подключения

        public ComPortSettings ComPortSettings
        {
            get { return _comSettings; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("COM порт"), Category("Custom")]
        public string COM
        {
            get {
                return _comSettings.name;
            }
            set
            {
                _comSettings.name = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("GSM"), Category("Custom")]
        public bool GSM
        {
            get
            {
                return _comSettings.gsm_on;
            }
            set
            {
                _comSettings.gsm_on = value;
                if (value)
                {
                    tbGSMPhone.Enabled = value;
                    tbGSMInit.Enabled = value;
                }
                else
                {
                    tbGSMPhone.Enabled = value;
                    tbGSMInit.Enabled = value;
                }
            }
        }



        #endregion

        #region Свойства TCP подключения

        public TCPIPSettings TCPPortSettings
        {
            get { return _tcpSettings; }
        }

        [Description("Выбран TCP"), Category("Custom")]
        public bool IsTCPSelected
        {
            get { return _isTCPSelected; }
            set
            {
                _isTCPSelected = value;
                if (value)
                {
                    rbTcp.Checked = true;

                    comboBoxComPorts.Enabled = !value;
                    gbComProp.Enabled = !value;
                    cbUseGSM.Enabled = !value;

                    textBoxIp.Enabled = value;
                    textBoxPort.Enabled = value;
                }
                else
                {
                    rbCom.Checked = true;

                    comboBoxComPorts.Enabled = !value;
                    gbComProp.Enabled = !value;
                    cbUseGSM.Enabled = !value;

                    textBoxIp.Enabled = value;
                    textBoxPort.Enabled = value;
                }

            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("IP адрес"), Category("Custom")]
        public string IP
        {
            get { return _tcpSettings.ip_address; }
            set {
                _tcpSettings.ip_address = value;
                textBoxIp.Text = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("TCP порт"), Category("Custom")]
        public string TCPPort
        {
            get { return _tcpSettings.ip_port.ToString(); }
            set {
                ushort resPortVal = 0;
                try
                {
                    resPortVal = Convert.ToUInt16(value);
                }
                catch (Exception ex)
                {
                    logger.LogError("Некорректное значение порта: " + ex.Message.ToString());
                }

                _tcpSettings.ip_port = resPortVal;
                textBoxPort.Text = resPortVal.ToString();
            }
        }


        #endregion


        private void rbTcp_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Checked == true && rb.Tag.ToString() == "tcp")
            {
                IsTCPSelected = true;
            }
            else if (rb.Checked == true && rb.Tag.ToString() == "com")
            {
                IsTCPSelected = false;
                updateComPortSettingFromConfig(ref this._comSettings);
            }
        }

        private void btnApplyConnectionSettings_Click(object sender, EventArgs e)
        {
            setVirtualPort();
        }

        private void cbUseGSM_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            GSM = cb.Checked;
        }

        private void btnClosePort_Click(object sender, EventArgs e)
        {
            string msg = "Порт закрыт ";
            if (_virtualPort != null)
            {
                object oVp = _virtualPort.GetPortObject();


                // на будущее для более осознанного закрытия
                if (typeof(SerialPort).IsAssignableFrom(oVp.GetType()))
                {
                    SerialPort p = (SerialPort)oVp;
                    //_virtualPort.Close();
                }
                else if (typeof(TcpipPort).IsAssignableFrom(oVp.GetType()))
                {
                    TcpipPort p = (TcpipPort)oVp;
                    //_virtualPort.Close();
                }


                _virtualPort.Close();

                msg += _virtualPort.GetName();

                updateStatus(msg);
            }
            else
            {
                msg = "Порт не создан";
                logger.LogError(msg);
                updateStatus(msg, true);
            }



        }
    }

    public class EventArgsSettingsApplied : EventArgs
    {
        public VirtualPort VPort { get; private set; }
        public string Message { get; private set; }

        public EventArgsSettingsApplied(VirtualPort virtualPort, string msg)
        {
            VPort = virtualPort;
            Message = msg;
        }

        
    }
}
