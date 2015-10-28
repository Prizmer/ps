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
            ms.StartServer();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            ms.StopServer();
        }
    }
}
