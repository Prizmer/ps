using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PollingLibraries.LibLogger
{
    public partial class CtlLogger : UserControl
    {
        public CtlLogger()
        {
            InitializeComponent();
        }

        private void BtnOpenLogsDir_Click(object sender, EventArgs e)
        {
            Logger.OpenLogsFolder();
        }

        private void BtnDeleteAllLogsNow_Click(object sender, EventArgs e)
        {
            Logger.DeleteLogsSimple();
        }

        private void BtnDeleteOldLogsGentle_Click(object sender, EventArgs e)
        {
            Logger.DeleteLogs();
        }
    }
}
