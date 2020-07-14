using System;
using System.Data;
using System.Windows.Forms;
using Npgsql;
using Prizmer.PoolServer.DataBase;
using PollingLibraries.LibPorts;
using System.Collections.Generic;




namespace Prizmer.PoolServer
{
    public partial class MetersSearchForm : Form
    {
        public PgStorage storage;
        public MainService msInstance;
        private DataTable metersTable = new DataTable();

        private SearchFormData searchFormData = null;
        private TakenParams[] selectedMeterTakenParamsAll = new TakenParams[0];

        private MainFormParamsStructure msfPrms;

        public delegate void MeterSearchFormEventHandler(object sender, MainFormParamsStructure e);
        public event MeterSearchFormEventHandler pollingStart;

        public MetersSearchForm()
        {
            InitializeComponent();
        }

        

        private void MetersSearchForm_Load(object sender, EventArgs e)
        {
            MetersGrid.AutoGenerateColumns = false;
            MetersGrid.DataSource = metersTable.DefaultView;
            cbReadAllParams.Checked = true;

            dateTimePicker1.Value = DateTime.Now.Date;

            pollingStart += MetersSearchForm_pollingStart;
            msInstance.pollingEnded += Ms_pollingEnded;
       
        }

        private void Ms_pollingEnded(object sender, MyEventArgs e)
        {
            Action action = () => { this.groupBox1.Enabled = true;  };
            this.Invoke(action);

        }

        private void MetersSearchForm_pollingStart(object sender, MainFormParamsStructure e)
        {
            this.groupBox1.Enabled = false;
        }


        private void MetersSearchForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void printPortParams(Guid meterGuid)
        {
            meterInfoTextBox.Text = String.Empty;
            bool is_linked = false;



            TCPIPSettings tcpip = storage.GetTCPIPByMeterGUID(meterGuid);
            if (tcpip.ip_address != null)
            {
                is_linked = true;
                meterInfoTextBox.Text += "IP: " + tcpip.ip_address + Environment.NewLine +
                    "Порт: " + tcpip.ip_port.ToString() + Environment.NewLine +
                    "Таймаут записи: " + tcpip.write_timeout.ToString() + Environment.NewLine +
                    "Таймаут чтения: " + tcpip.read_timeout.ToString() + Environment.NewLine +
                    "Попытки: " + tcpip.attempts.ToString() + Environment.NewLine +
                    "Задержка между отправками: " + tcpip.delay_between_sending.ToString() + Environment.NewLine;

                // дополним структуру
                msfPrms.ip = tcpip.ip_address;
                msfPrms.port = tcpip.ip_port;
                msfPrms.isTcp = true;
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

        string meterParamsStr = String.Empty;
        private void fillParamsListbox(Guid meterGuid)
        {
            List<string> paramsList = new List<string>();

            lbParams.Items.Clear();
            meterParamsStr = String.Empty;

            selectedMeterTakenParamsAll = storage.GetTakenParamByMetersGUID(meterGuid);

            if (searchFormData != null)
                searchFormData.takenParams = selectedMeterTakenParamsAll;

            if (selectedMeterTakenParamsAll.Length > 0)
            {
                foreach (TakenParams taken in storage.GetTakenParamByMetersGUID(meterGuid))
                {
                    string paramCaption = String.Empty;
                    Param param = storage.GetParamByGUID(taken.guid_params);
                    paramCaption += "Имя: " + param.name + "; Тип: ";

                    switch (param.type)
                    {
                        case 0:
                            paramCaption += "текущий";
                            break;
                        case 2:
                            paramCaption += "месячный";
                            break;
                        case 3:
                            paramCaption += "архивный";
                            break;
                        case 1:
                            paramCaption += "суточный";
                            break;
                        case 4:
                            paramCaption += "получасовой";
                            break;
                    }

                    paramCaption += "; Адрес: " + param.param_address.ToString() + "; Канал: " + param.channel.ToString() + ";" + Environment.NewLine;
                    meterParamsStr += paramCaption;
                    paramsList.Add(paramCaption);
                }

                lbParams.Items.AddRange(paramsList.ToArray());
            }
            else
            {
                MessageBox.Show("Прибор не имеет считываемых параметров", "Ошибка");
            }
        }
        
        private void getMeterInfo()
        {
            if (MetersGrid.CurrentRow != null)
            {
                searchFormData = new SearchFormData();

                Guid guidMetersPk = Guid.Parse(metersTable.Rows[MetersGrid.CurrentRow.Index]["guid"].ToString());
                searchFormData.guidMeter = guidMetersPk;

                string meterCaption = metersTable.Rows[MetersGrid.CurrentRow.Index]["name"].ToString();
                this.tbSelectedMeter.Text = meterCaption;

                this.fillParamsListbox(guidMetersPk);
                this.printPortParams(guidMetersPk);

                Guid meterTypeGuid = Guid.Parse(metersTable.Rows[MetersGrid.CurrentRow.Index]["guid_types_meters"].ToString());
                TypeMeter typeMeter = storage.GetMetersTypeByGUID(meterTypeGuid);
                msfPrms.driverName = typeMeter.driver_name;
                msfPrms.driverGuid = typeMeter.guid;
            }
            else
            {
                searchFormData = null;
                selectedMeterTakenParamsAll = new TakenParams[0];
            }
        }

        private void MetersGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            getMeterInfo();
        }
        private void MetersGrid_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                getMeterInfo();
            }
        }

        

        private void SerialNumBox_TextChanged(object sender, EventArgs e)
        {
            storage.FindMetersWithSerial(SerialNumBox.Text, metersTable);
        }

        private void btnCopyPrmsToBuffer_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(meterParamsStr);
        }

        private void lbParams_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox s = (ListBox)sender;

            if (s.Items.Count == 0) return;
            if (!cbReadAllParams.Checked)
            {
                // получим индексы выбранных элементов
                List<int> selectedItemIndexes = new List<int>();
                foreach (object o in s.SelectedItems)
                    selectedItemIndexes.Add(s.Items.IndexOf(o));

                List<TakenParams> tmpTakenPrmsList = new List<TakenParams>();
                foreach (int i in selectedItemIndexes)
                {
                    tmpTakenPrmsList.Add(selectedMeterTakenParamsAll[i]);
                }

                this.searchFormData.takenParams = tmpTakenPrmsList.ToArray();
            }
            else
            {
                this.searchFormData.takenParams = selectedMeterTakenParamsAll;
            }
        }

        private void cbReadAllParams_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.Checked)
            {
                lbParams.ClearSelected();
                lbParams.SelectionMode = SelectionMode.None;
            }
            else
            {
                lbParams.SelectionMode = SelectionMode.MultiExtended;
            }
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            // нужно заполнить структуру msfPrms
            msfPrms.dtStart = this.dateTimePicker1.Value.Date;
            msfPrms.dtEnd = msfPrms.dtStart;

            if (pollingStart != null && searchFormData != null && searchFormData.takenParams.Length > 0)
            {
                msfPrms.searchFormData = searchFormData; ;
                pollingStart.Invoke(this, msfPrms);
            }
            else
            {
                MessageBox.Show("Прибор или параметры для чтения не выбраны");
            }
        }

        private void linkDeleteData_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }
    }

    public class SearchFormData
    {
        public Guid guidMeter;
        public TakenParams[] takenParams = new TakenParams[0];
    };

    public class MyDataGridView : DataGridView
    {
        /**
         * DataGridView при нажатии на enter переводит фокус и выделение 
         * на следующую строку. Воизбежании - я перегрузил методы и поменял
         * тип компонента DataGridView на MyDataGridView в MetersSearchFormDesigner.cs
         */
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                int col = this.CurrentCell.ColumnIndex;
                int row = this.CurrentCell.RowIndex;
                this.CurrentCell = this[col, row];
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                int col = this.CurrentCell.ColumnIndex;
                int row = this.CurrentCell.RowIndex;
                this.CurrentCell = this[col, row];
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

    }
}