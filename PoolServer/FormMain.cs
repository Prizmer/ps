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

namespace Prizmer.PoolServer
{
    public partial class FormMain : Form
    {
        MainService ms = new MainService();
        Analizator frmAnalizator = new Analizator();
        
        public FormMain()
        {
            frmAnalizator = new Analizator();
            InitializeComponent();
        }

        PgStorage storage = new PgStorage();
        string connectionStr = "";
        private void FormMain_Load(object sender, EventArgs e)
        {
            try
            {
                connectionStr = ms.GetConnectionString();

                //groupBox1 settings
                ConnectionState conState = storage.Open(connectionStr);
                List<string> driver_names = storage.GetDriverNames();

                if (driver_names.Count > 0)
                {
                    comboBox1.Items.Clear();
                    comboBox1.Items.AddRange(driver_names.ToArray());
                    comboBox1.SelectedItem = "m230";
                }
                else
                {
                    comboBox1.SelectedIndex = 0;
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
                prms.mode = 0;

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

        private void btnStartReading_Click(object sender, EventArgs e)
        {
            MainFormParamsStructure prms = new MainFormParamsStructure();
            prms.frmAnalizator = this.frmAnalizator;

            try
            {
                prms.driverName = comboBox1.Text;
                prms.dtStart = dateTimePicker1.Value;
                prms.dtEnd = dateTimePicker2.Value;
                prms.ip = tbAddress.Text;
                prms.port = int.Parse(tbPort.Text);
                prms.mode = 1;
                prms.isTcp = rbTCP.Checked;
                prms.paramType = comboBox2.SelectedIndex;

                ManualStartInProcess = true;
                ms.StartServer(prms);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

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
            pbPreloader.Hide();
            MessageBox.Show("Опрос завершен","Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            progressBar1.Value = 0;
            lblCnt.Text = "";
            lblCurCnt.Text = "";

            ManualStartInProcess = false;
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
 
        private void rbCom_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            if (rb.Tag.ToString() == "com")
            {
                tbPort.Enabled = false;
                tbPort.Clear();
            }
            else
            {
                tbPort.Enabled = true;
            }
        }

        private void btnEndReading_Click(object sender, EventArgs e)
        {
            ms.StopServer(false);
            progressBar1.Value = 0;
            lblCnt.Text = "";
            lblCurCnt.Text = "";

            ManualStartInProcess = false;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            if (cb.SelectedIndex == 2 || cb.SelectedIndex == 3)
            {
                cb.SelectedIndex = 0;
                MessageBox.Show("Архивный и месячный временно не поддерживаются");
            }

            comboBox3Upd();
            comboBox3.SelectedIndex = comboBox3.Items.Count - 1; 
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            dateTimePicker1.Value = dateTimePicker1.Value.AddDays(-1);
            dateTimePicker2.Value = dateTimePicker2.Value.AddDays(-1);
        }

        void comboBox3Upd()
        {
            List<string> availiablePorts = storage.GetPortsAvailiableByDriverParamType(comboBox2.SelectedIndex, comboBox1.Text);
            comboBox3.Items.Clear();
            comboBox3.Items.AddRange(availiablePorts.ToArray());
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox3Upd();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.Text.Length > 0)
            {
                tbAddress.Text = comboBox3.Text.Split(':')[0];
                tbPort.Text = comboBox3.Text.Split(':')[1];
            }
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
    }

    public struct MainFormParamsStructure
    {
        public int mode;
        public DateTime dtStart;
        public DateTime dtEnd;
        public string driverName;
        public string ip;
        public int port;
        public bool isTcp;
        public int paramType;

        public Analizator frmAnalizator;
    }
}
