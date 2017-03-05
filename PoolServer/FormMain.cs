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
            //ms.StartServer();
            //groupBox1 settings
            comboBox1.SelectedIndex = 0;
                            MainFormParamsStructure prms = new MainFormParamsStructure();
                prms.mode = 0;
                if (cbServerStarted.Checked) ms.StartServer(prms);

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
            }
            else
            {
                ms.StopServer();
                groupBox1.Enabled = true;
            }
        }

        private void btnStartReading_Click(object sender, EventArgs e)
        {
            MainFormParamsStructure prms = new MainFormParamsStructure();
            prms.driverName = comboBox1.SelectedItem.ToString();
            prms.dtStart = dateTimePicker1.Value;
            prms.dtEnd = dateTimePicker2.Value;
            prms.ip = tbAddress.Text;
            prms.port = int.Parse(tbPort.Text);
            prms.mode = 1;
            prms.isTcp = rbTCP.Checked;

            ms.pollingStarted += new MainService.MyEventHandler(ms_pollingStarted);
            ms.meterPolled += new MainService.MyEventHandler(ms_meterPolled);
            ms.pollingEnded += new MainService.MyEventHandler(ms_pollingEnded);
       
            ms.StartServer(prms);
        }

        void ms_pollingEnded(object sender, MyEventArgs e)
        {
            ms.StopServer();
            MessageBox.Show("Polling ended!");
        }

        void ms_meterPolled(object sender, MyEventArgs e)
        {
            progressBar1.Value += 1;
            progressBar1.Maximum = e.metersCount +1;
        }

        void ms_pollingStarted(object sender, MyEventArgs e)
        {
            progressBar1.Value = 0;
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
            ms.StopServer();
            progressBar1.Value = 0;
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
    }
}
