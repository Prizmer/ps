using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Prizmer.PoolServer.DataBase;

namespace Prizmer.PoolServer
{
    public partial class FormMain : Form
    {
        MainService ms = new MainService();

        public FormMain()
        {
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

             dateTimePicker1.Value = DateTime.Now;
            dateTimePicker2.Value = DateTime.Now;
            MainFormParamsStructure prms = new MainFormParamsStructure();
            prms.mode = 0;
            if (cbServerStarted.Checked)
            {
                ms.StartServer(prms);
                groupBox1.Enabled = false;
            }
            else
            {
                groupBox1.Enabled = true;
            }

           // ms.pollingStarted += new MainService.MyEventHandler(ms_pollingStarted);
          //  ms.meterPolled += new MainService.MyEventHandler(ms_meterPolled);
          //  ms.pollingEnded += new MainService.MyEventHandler(ms_pollingEnded);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            storage.Close();
            ms.StopServer();
        }

        private void cbServerStarted_CheckedChanged(object sender, EventArgs e)
        {
            if (cbServerStarted.Checked)
            {
                MainFormParamsStructure prms = new MainFormParamsStructure();
                prms.mode = 0;

                ms.StartServer(prms);
                groupBox1.Enabled = false;
                this.Height = 137; 
            }
            else
            {
                ms.StopServer();
                groupBox1.Enabled = true;
                this.Height = 384;
            }
        }

        private void btnStartReading_Click(object sender, EventArgs e)
        {
            MainFormParamsStructure prms = new MainFormParamsStructure();

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

                ms.StartServer(prms);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
            MessageBox.Show("Опрос завершен","Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            progressBar1.Value = 0;
            lblCnt.Text = "";
            lblCurCnt.Text = "";
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
    }
}
