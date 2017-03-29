using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Prizmer.PoolServer
{
    public partial class FormMain : Form
    {
        MainService ms = new MainService();

        public FormMain()
        {
            InitializeComponent();
        }
        
        private void FormMain_Load(object sender, EventArgs e)
        {            
            //groupBox1 settings
            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 4;
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

            ms.pollingStarted += new MainService.MyEventHandler(ms_pollingStarted);
            ms.meterPolled += new MainService.MyEventHandler(ms_meterPolled);
            ms.pollingEnded += new MainService.MyEventHandler(ms_pollingEnded);
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
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

        public void pollStarted()
        {
            progressBar1.Value = 0;
            lblCurCnt.Text = "0";
            lblCnt.Text = "0";
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
        }

        void ms_pollingStarted(object sender, MyEventArgs e)
        {
            this.Invoke(new InvokeDelegate(pollStarted));
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
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            dateTimePicker1.Value = dateTimePicker1.Value.AddDays(-1);
            dateTimePicker2.Value = dateTimePicker2.Value.AddDays(-1);
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
