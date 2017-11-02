using System;
using System.Data;
using System.Windows.Forms;
using Prizmer.PoolServer.DataBase;
using PollingLibraries.LibPorts;

namespace Prizmer.PoolServer
{
    public partial class MetersSearchForm : Form
    {
        public PgStorage storage;
        DataTable metersTable = new DataTable();

        public MetersSearchForm()
        {
            InitializeComponent();
        }

        private void metersTable_RowChanged(object sender, DataRowChangeEventArgs e)
        {
            if (e.Action == DataRowAction.Change)
                e.Row.RejectChanges();
        }

        private void MetersSearchForm_Load(object sender, EventArgs e)
        {
            MetersGrid.AutoGenerateColumns = false;
            MetersGrid.DataSource = metersTable.DefaultView;
            metersTable.RowChanged += metersTable_RowChanged;
        }

        private void PortButton_Click(object sender, EventArgs e)
        {
            if (MetersGrid.CurrentRow != null)
            {
                meterInfoTextBox.Text = String.Empty;
                bool is_linked = false;
                TCPIPSettings tcpip = storage.GetTCPIPByMeterGUID(Guid.Parse(metersTable.Rows[MetersGrid.CurrentRow.Index]["guid"].ToString()));
                if (tcpip.ip_address != null)
                {
                    is_linked = true;
                    meterInfoTextBox.Text += "IP: " + tcpip.ip_address + Environment.NewLine +
                        "Порт: " + tcpip.ip_port.ToString() + Environment.NewLine +
                        "Таймаут записи: " + tcpip.write_timeout.ToString() + Environment.NewLine +
                        "Таймаут чтения: " + tcpip.read_timeout.ToString() + Environment.NewLine +
                        "Попытки: " + tcpip.attempts.ToString() + Environment.NewLine +
                        "Задержка между отправками: " + tcpip.delay_between_sending.ToString() + Environment.NewLine;
                }
                ComPortSettings comport = storage.GetComportByMeterGUID(Guid.Parse(metersTable.Rows[MetersGrid.CurrentRow.Index]["guid"].ToString()));
                if (comport.name != null)
                {
                    is_linked = true;
                    meterInfoTextBox.Text += "Имя: " + comport.name + Environment.NewLine +
                        "Baudrate: " + comport.baudrate.ToString() + Environment.NewLine +
                        "Data bits: " + comport.data_bits.ToString() + Environment.NewLine +
                        "Parity: " + comport.parity.ToString() + Environment.NewLine +
                        "Stop bits: " + comport.stop_bits.ToString() + Environment.NewLine +
                        "Write timeout: " + comport.write_timeout.ToString() + Environment.NewLine +
                        "Read timeout: " + comport.read_timeout.ToString() + Environment.NewLine +
                        "Attempts: " + comport.attempts.ToString() + Environment.NewLine +
                        "Delay between sending: " + comport.delay_between_sending.ToString() + Environment.NewLine;
                }
                if (!is_linked) MessageBox.Show("Прибор не привязан ни к одному порту", "Ошибка");
            }
        }

        private void ReadParamsButton_Click(object sender, EventArgs e)
        {
            if (MetersGrid.CurrentRow != null)
            {
                meterInfoTextBox.Text = String.Empty;
                bool is_linked = false;
                foreach (TakenParams taken in storage.GetTakenParamByMetersGUID(Guid.Parse(metersTable.Rows[MetersGrid.CurrentRow.Index]["guid"].ToString())))
                {
                    is_linked = true;
                    Param param = storage.GetParamByGUID(taken.guid_params);
                    meterInfoTextBox.Text += "Имя: " + param.name + "; Тип: ";
                    switch (param.type)
                    {
                        case 0:
                            meterInfoTextBox.Text += "текущий";
                            break;
                        case 2:
                            meterInfoTextBox.Text += "месячный";
                            break;
                        case 3:
                            meterInfoTextBox.Text += "архивный";
                            break;
                        case 1:
                            meterInfoTextBox.Text += "суточный";
                            break;
                        case 4:
                            meterInfoTextBox.Text += "получасовой";
                            break;
                    }
                    meterInfoTextBox.Text += "; Адрес: " + param.param_address.ToString() + "; Канал: " + param.channel.ToString() + ";" + Environment.NewLine;
                }
                if (!is_linked) MessageBox.Show("Прибор не имеет считываемых параметров", "Ошибка");
            }
        }

        private void MetersSearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            foreach (Form win in Application.OpenForms)
            {
                if (win.Name == "FormMain") win.Focus();
            }
            e.Cancel = true;
        }

        private void SerialNumBox_TextChanged(object sender, EventArgs e)
        {
            storage.FindMeters(SerialNumBox.Text, metersTable, isSearchByIdCheckBox.Checked);
        }

        private void MetersGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MetersGrid.CancelEdit();
            e.Cancel = true;
        }

        private void isSearchByIdCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            storage.FindMeters(SerialNumBox.Text, metersTable, isSearchByIdCheckBox.Checked);
        }
    }
}