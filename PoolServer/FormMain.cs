using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Prizmer.PoolServer.DataBase;
using System.Diagnostics;

using PollingLibraries.LibLogger;

namespace Prizmer.PoolServer
{
    public partial class FormMain : Form
    {
        MainService ms = new MainService();
        Analizator frmAnalizator = new Analizator();
        MetersSearchForm DevSearchForm = new MetersSearchForm();

        public FormMain()
        {
            frmAnalizator = new Analizator();
            InitializeComponent();
        }

        PgStorage storage = new PgStorage();

        private string selectedDriverGuid = "";
        private string selectedDriverName = "";
        List<string[]> driversInfoList = new List<string[]>();

        string connectionStr = "";
        private void FormMain_Load(object sender, EventArgs e)
        {
            //SANDBOX
           // string at_cmd_hang = "ATH0\r";
           // byte[] cmdHang = ASCIIEncoding.ASCII.GetBytes(at_cmd_hang);
            

            const string SO_VERSION = "v. 0.10.4";
            this.Text += " - " + SO_VERSION;

            try
            {
                connectionStr = ms.GetConnectionString();

                //groupBox1 settings
                ConnectionState conState = storage.Open(connectionStr);

                if (conState != ConnectionState.Open)
                    MessageBox.Show("Не удалось установить соединение с БД", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);

                driversInfoList = storage.GetDriverNames();

                List<string> driver_names = new List<string>();
                foreach (string[] drInfo in driversInfoList)
                    driver_names.Add(drInfo[1] + " (" + drInfo[2] + ")");

                if (driver_names.Count > 0)
                {
                    comboBox1.Items.Clear();
                    comboBox1.Items.AddRange(driver_names.ToArray());
                    comboBox1.SelectedIndex = 0;
                }
                else
                {
                    comboBox1.SelectedIndex = -1;
                }

                comboBox2.SelectedIndex = 4;

                comboBox3Upd();
                if (comboBox3.Items.Count > 0)
                    comboBox3.SelectedIndex = 0;

                pbPreloader.Hide();

                dateTimePicker1.Value = DateTime.Now;
                dateTimePicker2.Value = DateTime.Now;
                MainFormParamsStructure prms = new MainFormParamsStructure();
                prms.frmAnalizator = this.frmAnalizator;
                prms.mode = OperatingMode.OM_AUTO;

                ms.pollingStarted += new MainService.MyEventHandler(ms_pollingStarted);
                ms.meterPolled += new MainService.MyEventHandler(ms_meterPolled);
                ms.pollingEnded += new MainService.MyEventHandler(ms_pollingEnded);

                ms.stoppingStarted += new MainService.MyEventHandler(ms_threadClosingStart);
                ms.stoppingEnded += new MainService.MyEventHandler(ms_threadClosingEnd);

                cbServerStarted.Checked = ms.SO_AUTO_START;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //делимся экземпляром storage с дочерней формой
            DevSearchForm.storage = this.storage;
        }

        bool _bThreadsAreClosing = false;
        bool ThreadsAreClosing
        {
            get { return _bThreadsAreClosing; }
            set
            {
                _bThreadsAreClosing = value;
                if (value)
                {
                    tsLabel1.Text = "Дождитесь закрытия портов... (макс 2 мин.)";
                    groupBox1.Enabled = false;
                    pbPreloader.Show();

                    cbServerStarted.Enabled = false;
                }
                else
                {
                    tsLabel1.Text = "Режим: полностью остановлен";
                    groupBox1.Enabled = true;
                    pbPreloader.Hide();

                    cbServerStarted.Enabled = true;
                }
            }
        }


        bool bReadyToExit = false;
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!bReadyToExit)
            {
                e.Cancel = true;

                if (!ThreadsAreClosing)
                {
                    if (cbServerStarted.Checked)
                    {
                        cbServerStarted.Checked = false;
                    }
                    else
                    {
                        ms.StopServer();
                    }
                }
                else
                {
                    ms.StopServer(true);
                   // bReadyToExit = true;
                   // Application.Exit();
                }
            }
            else
            {
                storage.Close();  
            }
        }

        private void cbServerStarted_CheckedChanged(object sender, EventArgs e)
        {
            if (cbServerStarted.Checked)
            {
                MainFormParamsStructure prms = new MainFormParamsStructure();
                prms.frmAnalizator = this.frmAnalizator;
                prms.mode = 0;

                groupBox1.Enabled = false;

                Rectangle screenRectangle = RectangleToScreen(this.ClientRectangle);
                int titleHeight = screenRectangle.Top - this.Top;

                this.Height = titleHeight + cbServerStarted.Location.Y + cbServerStarted.Height + statusStrip1.Height + 5;

                tsLabel1.Text = "Режим: автоматический опрос";

                ms.StartServer(prms);
            }
            else
            {
                groupBox1.Enabled = true;
                this.Height = 412;

                tsLabel1.Text = "Дождитесь закрытия портов...";

                ms.StopServer();
            }
        }




        private void startReading()
        {
            MainFormParamsStructure prms = new MainFormParamsStructure();
            prms.frmAnalizator = this.frmAnalizator;

            string prmsIp = "";
            int prmsPort = -1;

            if (cbEachPort.Checked)
            {
                comboBox3.SelectedIndex = this.currentPortIndex;
                this.fillTbAddressAndTbPort();
            }


            prmsIp = _isTcpMode ? tbAddress.Text : tbAddress.Text.Replace("COM", "");
            prmsPort = int.Parse(tbPort.Text);
 

            try
            {
                prms.driverGuid = Guid.Parse(this.selectedDriverGuid);
                prms.driverName = this.selectedDriverName;
                prms.dtStart = dateTimePicker1.Value;
                prms.dtEnd = dateTimePicker2.Value;
                prms.isTcp = _isTcpMode;
                prms.ip = prmsIp;
                prms.port = prmsPort;
                prms.mode = OperatingMode.OM_MANUAL;
                prms.paramType = comboBox2.SelectedIndex;

                ManualStartInProcess = true;
                ms.StartServer(prms);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private int currentPortIndex = 0;
        private void btnStartReading_Click(object sender, EventArgs e)
        {
            this.startReading();
        }


        bool _manualStartInProcess = false;
        bool ManualStartInProcess
        {
            get { return _manualStartInProcess; }
            set
            {
                cbServerStarted.Enabled = !value;
                dateTimePicker1.Enabled = !value;
                dateTimePicker2.Enabled = !value;
                comboBox1.Enabled = !value;
                comboBox2.Enabled = !value;
                comboBox3.Enabled = !value;

                tbAddress.Enabled = !value;
                tbPort.Enabled = !value;
                rbTCP.Enabled = !value;
                linkLabel1.Enabled = !value;

                if (value == true)
                {
                    btnStartReading.Enabled = !value;

                    btnEndReading.Enabled = value;
                    pbPreloader.Show();

                }
                else
                {
                    btnStartReading.Enabled = !value;

                    btnEndReading.Enabled = value;
                    pbPreloader.Hide();
                }
            }

        }

        public delegate void InvokeDelegate();
        public delegate void InvokeDelegatePrms(object sender, MyEventArgs e);

        public void pollStarted(object sender, MyEventArgs e)
        {
            progressBar1.Value = 0;
            lblCurCnt.Text = "0";
            lblCnt.Text = e.metersCount.ToString();
        }

        public void meterPolled(object sender, MyEventArgs e)
        {
            progressBar1.Maximum = e.metersCount;
            if (progressBar1.Value < progressBar1.Maximum)
                progressBar1.Value += 1;
            lblCurCnt.Text = progressBar1.Value.ToString();
            lblCnt.Text = e.metersCount.ToString();
        }

        public void pollEnded()
        {

            ms.StopServer();
            //pbPreloader.Hide();
            //ManualStartInProcess = false;
            //progressBar1.Value = 0;
            //lblCnt.Text = "";
            //lblCurCnt.Text = "";


            //if (cbEachPort.Checked)
            //{
            //    if (this.currentPortIndex < this.comboBox3.Items.Count - 1)
            //    {
            //        this.currentPortIndex++;
            //        this.startReading();
            //    }
            //    else
            //    {
            //        this.currentPortIndex = 0;
            //        MessageBox.Show("Опрос по всем портам завершен!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    }
            //} else
            //{
            //    MessageBox.Show("Опрос завершен", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}

        }

        public void threadClosingStart(object sender, MyEventArgs e)
        {
            ThreadsAreClosing = true;
        }

        public void threadClosingEnd(object sender, MyEventArgs e)
        {
            ThreadsAreClosing = false;

            bReadyToExit = false;
            
            if (e.success)
            {
                tsLabel1.Text = "Режим: полностью остановлен";
                ManualStartInProcess = false;


                pbPreloader.Hide();
                ManualStartInProcess = false;
                progressBar1.Value = 0;
                lblCnt.Text = "";
                lblCurCnt.Text = "";
                if (cbEachPort.Checked)
                {
                    if (this.currentPortIndex < this.comboBox3.Items.Count - 1)
                    {
                        this.currentPortIndex++;
                        frmAnalizator = new Analizator();
                        this.startReading();
                    }
                    else
                    {
                        this.currentPortIndex = 0;
                        MessageBox.Show("Опрос по всем портам завершен!", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Опрос завершен", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                tsLabel1.Text = "Не удалось закрыть все порты, перезапустите";
            }

            if (!e.success)
            {
                ms.StopServer(true);
                return;
            }

            bReadyToExit = true;
        }

        void ms_pollingStarted(object sender, MyEventArgs e)
        {
            this.Invoke(new InvokeDelegatePrms(pollStarted), sender, e);
        }

        void ms_meterPolled(object sender, MyEventArgs e)
        {
            this.Invoke(new InvokeDelegatePrms(meterPolled), sender, e);
        }

        void ms_pollingEnded(object sender, MyEventArgs e)
        {
            this.Invoke(new InvokeDelegate(pollEnded));
        }

        void ms_threadClosingStart(object sender, MyEventArgs e)
        {
            this.Invoke(new InvokeDelegatePrms(threadClosingStart), sender, e);
        }

        void ms_threadClosingEnd(object sender, MyEventArgs e)
        {
            this.Invoke(new InvokeDelegatePrms(threadClosingEnd), sender, e);
        }

        private bool _isTcpMode = true;
        private void rbCom_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Tag.ToString() == "com")
            {
                tbPort.Enabled = false;
                tbAddress.Enabled = false;
                _isTcpMode = false;
            }
            else
            {
                tbPort.Enabled = true;
                tbAddress.Enabled = true;
                _isTcpMode = true;
            }

            comboBox3Upd();
            //comboBox3.SelectedIndex = comboBox3.Items.Count - 1;
        }

        private void btnEndReading_Click(object sender, EventArgs e)
        {
            this.cbEachPort.Checked = false;
            ms.StopServer(false);
            progressBar1.Value = 0;
            lblCnt.Text = "";
            lblCurCnt.Text = "";

            ManualStartInProcess = false;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedIndex == 2)
            {
                cb.SelectedIndex = 0;
                MessageBox.Show("Месячный временно не поддерживаются");
            }

            comboBox3Upd();
            //comboBox3.SelectedIndex = comboBox3.Items.Count - 1; 
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            dateTimePicker1.Value = dateTimePicker1.Value.AddDays(-1);
            dateTimePicker2.Value = dateTimePicker2.Value.AddDays(-1);
        }

        private void comboBox3Upd()
        {
            int selectedDriverIndex = this.comboBox1.SelectedIndex;
            if (selectedDriverIndex > -1 && driversInfoList.Count > 0)
            {
                selectedDriverGuid = driversInfoList[selectedDriverIndex][0];
                selectedDriverName = driversInfoList[selectedDriverIndex][2];
                comboBox3.Items.Clear();
                List<string> availiablePorts = storage.GetPortsAvailiableByDriverGuid(comboBox2.SelectedIndex, selectedDriverGuid, _isTcpMode);
                comboBox3.Items.AddRange(availiablePorts.ToArray());

                if (comboBox3.Items.Count > 0)
                    comboBox3.SelectedIndex = 0;
            }
            else
            {
                comboBox3.Items.Clear();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox3Upd();
        }


        private string[] getAddressAndPortFromString(string str)
        {
            string[] res = new string[2];
            if (_isTcpMode)
            {
                res[0] = str.Split(':')[0];
                res[1] = str.Split(':')[1];
            }
            else
            {
                res[0] = str;
                res[1] = "0";
            }

            return res;
        }

        private void fillTbAddressAndTbPort()
        {
            if (comboBox3.Text.Length > 0)
            {
                string[] res = getAddressAndPortFromString(comboBox3.Text);

                tbAddress.Text = res[0];
                tbPort.Text = res[1];
            } else
            {
                tbAddress.Text = "";
                tbPort.Text = "";
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            fillTbAddressAndTbPort();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void contextMenuStrip1_Opening_1(object sender, CancelEventArgs e)
        {

        }

        private void ctxMenuAnalizator_Click(object sender, EventArgs e)
        {
            this.frmAnalizator.Show();
            this.frmAnalizator.Focus();
        }

        private void ctxMenuShowLogsDir_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", @Logger.BaseDirectory);
        }

        private void ctxMenuDeleteLogs_Click(object sender, EventArgs e)
        {
            Logger.DeleteLogs();
        }

        private void timerLogsDeletion_Tick(object sender, EventArgs e)
        {

        }

        private void DeviceSearchMenuItem_Click(object sender, EventArgs e)
        {
            this.DevSearchForm.Show();
            this.DevSearchForm.Focus();
        }

        private void cbEachPort_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cbSender = (CheckBox)sender;
            if (cbSender.Checked)
            {
                this.comboBox3.Enabled = false;
                if (this.comboBox3.Items.Count > 0)
                {
                    this.currentPortIndex = this.comboBox3.SelectedIndex;
                    //this.comboBox3.SelectedIndex = this.currentPortIndex;
                }
            } else
            {
                this.comboBox3.Enabled = true;
            }
        }
    }

    public enum OperatingMode
    {
        OM_AUTO = 0,
        OM_MANUAL = 1
    }

    public struct MainFormParamsStructure
    {
        public OperatingMode mode;
        public DateTime dtStart;
        public DateTime dtEnd;
        public Guid driverGuid;
        public string driverName;
        public string ip;
        public int port;
        public bool isTcp;
        public int paramType;

        public Analizator frmAnalizator;
    }
}
