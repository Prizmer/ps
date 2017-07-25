using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.IO.Ports;
using ExcelLibrary.SpreadSheet;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
//using System.Configuration.Assemblies;

using ElfApatorCommonDriver;

namespace elfextendedapp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            this.Text = FORM_TEXT_DEFAULT;

            DeveloperMode = true;
            if (DeveloperMode) this.Height -= groupBox1.Height;

            InProgress = false;
            DemoMode = false;
            InputDataReady = false;

            checkBoxTcp.Checked = false;
            rbDaily.Checked = true;
        }

        //при опросе или тесте связи
        bool bInProcess = false;
        public bool InProgress
        {
            get { return bInProcess; }
            set
            {
                bInProcess = value;

                if (bInProcess)
                {
                    toolStripProgressBar1.Value = 0;

                    comboBoxComPorts.Enabled = false;
                    buttonPoll.Enabled = false;
                    buttonImport.Enabled = false;
                    label1.Enabled = false;
                    buttonExport.Enabled = false;
                    buttonStop.Enabled = true;
                    numericUpDownComReadTimeout.Enabled = false;
                    btnGetMetersTable.Enabled = false;

                    rbDaily.Enabled = false;
                    rbMonthly.Enabled = false;
                    rbHalfs.Enabled = false;


                    this.Text += FORM_TEXT_INPROCESS;
                }
                else
                {
                   // comboBoxComPorts.Enabled = true;
                    btnGetMetersTable.Enabled = true;
                    buttonPoll.Enabled = true;
                   // buttonPing.Enabled = true;
                    buttonImport.Enabled = true;
                    buttonExport.Enabled = true;
                    label1.Enabled = true;
                    buttonStop.Enabled = false;
                  //  numericUpDownComReadTimeout.Enabled = true;
                    dgv1.Enabled = true;


                    rbDaily.Enabled = true;
                    rbMonthly.Enabled = true;
                    rbHalfs.Enabled = true;

                    this.Text = this.Text.Replace(FORM_TEXT_INPROCESS, String.Empty);
                }
            }
        }

        //Демонстрационный режим - отключает сервисные сообщения
        bool bDemoMode = false;
        public bool DemoMode
        {
            get { return bDemoMode; }
            set
            {
                bDemoMode = value;

                if (bDemoMode)
                {
                    this.Text = this.Text.Replace(FORM_TEXT_DEMO_OFF, String.Empty);
                    attempts = 3;
                }
                else
                {
                    //this.Text += FORM_TEXT_DEMO_OFF;
                    attempts = 5;
                }
            }

        }

        ParamTypes selectedParamType;

        bool bInputDataReady = false;
        public bool InputDataReady
        {
            get { return bInputDataReady; }
            set
            {
                bInputDataReady = value;

                if (!bInputDataReady)
                {
                    toolStripProgressBar1.Value = 0;

                    //comboBoxComPorts.Enabled = false;
                    buttonPoll.Enabled = false;
                    buttonImport.Enabled = true;
                    buttonExport.Enabled = false;
                    buttonStop.Enabled = false;

                    //numericUpDownComReadTimeout.Enabled = false;

                }
                else
                {
                    comboBoxComPorts.Enabled = true;
                    buttonPoll.Enabled = true;
                    buttonImport.Enabled = true;
                    buttonExport.Enabled = true;
                    buttonStop.Enabled = false;
                    numericUpDownComReadTimeout.Enabled = true;
                }
            }
        }

        bool pollingByMetersTable = false;

        #region Строковые постоянные 

            const string METER_IS_ONLINE = "ОК";
            const string METER_IS_OFFLINE = "Нет связи";
            const string METER_WAIT = "Ждите";
            const string REPEAT_REQUEST = "Повтор";

            const string FORM_TEXT_DEFAULT = "УСПД - программа опроса v.1.0";
            const string FORM_TEXT_DEMO_OFF = " - демо режим ОТКЛЮЧЕН";
            const string FORM_TEXT_DEV_ON = " - режим разработчика";

            const string FORM_TEXT_INPROCESS = " - в процессе";

        #endregion

        UM_RTU40 Meter = null;
        VirtualPort Vp = null;

        //изначально ни один процесс не выполняется, все остановлены
        volatile bool doStopProcess = false;
        bool bPollOnlyOffline = false;

        //default settings for input *.xls file
        int flatNumberColumnIndex = 0;
        int factoryNumberColumnIndex = 4;
        int firstRowIndex = 2;
        
        //предустановка значений
        int colIPIndex = 6;
        int colPortIndex = 7;
        int colChannelIndex = 3;
        int colMeterNameIndex = 1;
        int colPredValue = 8;


        private bool initMeterDriver(uint mAddr, string mPass, VirtualPort virtPort)
        {
            if (virtPort == null) return false;

            try
            {
                Meter = new UM_RTU40();
                Meter.Init(mAddr, mPass, virtPort);
                return true;
            }
            catch (Exception ex)
            {
                WriteToStatus("Ошибка инициализации драйвера: " + ex.Message);
                return false;
            }
        }

        private bool refreshSerialPortComboBox()
        {
            try
            {
                string[] portNamesArr = SerialPort.GetPortNames();
                comboBoxComPorts.Items.Clear();
                comboBoxComPorts.Items.AddRange(portNamesArr);
                if (comboBoxComPorts.Items.Count > 0)
                {
                    int startIndex = 0;
                    comboBoxComPorts.SelectedIndex = startIndex;
                    return true;
                }
                else
                {
                    WriteToStatus("В системе не найдены доступные COM порты");
                    return false;
                }
            }
            catch (Exception ex)
            {
                WriteToStatus("Ошибка при обновлении списка доступных COM портов: " + ex.Message);
                return false;
            }
        }

        private bool setVirtualSerialPort()
        {
            try
            {
                byte attempts = 1;
                ushort read_timeout = (ushort)numericUpDownComReadTimeout.Value;
                ushort write_timeout = (ushort)numericUpDownComWriteTimeout.Value;

                if (!checkBoxTcp.Checked)
                {
                    SerialPort m_Port = new SerialPort(comboBoxComPorts.Items[comboBoxComPorts.SelectedIndex].ToString());

                    m_Port.BaudRate = int.Parse(ConfigurationSettings.AppSettings["baudrate"]);
                    m_Port.DataBits = int.Parse(ConfigurationSettings.AppSettings["databits"]);
                    m_Port.Parity = (Parity)int.Parse(ConfigurationSettings.AppSettings["parity"]);
                    m_Port.StopBits = (StopBits)int.Parse(ConfigurationSettings.AppSettings["stopbits"]);
                    m_Port.DtrEnable = bool.Parse(ConfigurationSettings.AppSettings["dtr"]);

                    //meters initialized by secondary id (factory n) respond to 0xFD primary addr
                    Vp = new ComPort(m_Port, attempts, read_timeout, write_timeout);
                }
                else
                {
                    Vp = new TcpipPort(textBoxIp.Text, int.Parse(textBoxPort.Text), write_timeout, read_timeout, 0);
                }

                uint mAddr = 0xFD;
                string mPass = "";

                if (!initMeterDriver(mAddr, mPass, Vp)) return false;

                //check vp settings
                if (!checkBoxTcp.Checked)
                {
                    SerialPort tmpSP = Vp.getSerialPortObject();
                    if (!DemoMode)
                    {
                        toolStripStatusLabel2.Text = String.Format("{0}-{1}-{2}-DTR({3})-RTimeout: {4}ms", tmpSP.PortName, tmpSP.BaudRate, tmpSP.Parity, tmpSP.DtrEnable, read_timeout);
                    }
                    else
                    {
                        toolStripStatusLabel2.Text = String.Empty;
                    }                   
                }
                else
                {
                    toolStripStatusLabel2.Text = "TCP mode";
                }
               

                return true;
            }
            catch (Exception ex)
            {
                WriteToStatus("Ошибка создания виртуального порта: " + ex.Message);
                return false;
            }
        }

        private bool setXlsParser()
        {
            try
            {
                flatNumberColumnIndex = int.Parse(ConfigurationSettings.AppSettings["flatColumn"]) - 1;
                factoryNumberColumnIndex = int.Parse(ConfigurationSettings.AppSettings["factoryColumn"]) - 1;
                firstRowIndex = int.Parse(ConfigurationSettings.AppSettings["firstRow"]) - 1;
                //предустановка значений
                colIPIndex = int.Parse(ConfigurationSettings.AppSettings["colIPIndex"]) - 1;
                colPortIndex = int.Parse(ConfigurationSettings.AppSettings["colPortIndex"]) - 1;
                colChannelIndex = int.Parse(ConfigurationSettings.AppSettings["colChannelIndex"]) - 1;
                colMeterNameIndex = int.Parse(ConfigurationSettings.AppSettings["colMeterNameIndex"]) - 1;
                colPredValue = int.Parse(ConfigurationSettings.AppSettings["colPredValue"]) - 1;

                return true;
            }
            catch (Exception ex)
            {
                WriteToStatus("Ошибка разбора блока \"Настройка парсера\" в файле конфигурации: " + ex.Message);
                return false;
            }

        }

        private void WriteToStatus(string str)
        {
            MessageBox.Show(str, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            checkBoxTcp.Checked = true;

            //setting up dialogs
            ofd1.Filter = "Excel files (*.xls) | *.xls";
            sfd1.Filter = ofd1.Filter;
            ofd1.FileName = "FactoryNumbersTable";
            sfd1.FileName = ofd1.FileName;


            if (!checkBoxTcp.Checked)
            {
                if (!refreshSerialPortComboBox()) return;
                if (!setVirtualSerialPort()) return;
            }
            if (!setXlsParser()) return;

            //привязываются здесь, чтобы можно было выше задать значения без вызова обработчиков
            comboBoxComPorts.SelectedIndexChanged += new EventHandler(comboBoxComPorts_SelectedIndexChanged);
            numericUpDownComReadTimeout.ValueChanged +=new EventHandler(numericUpDownComReadTimeout_ValueChanged);
            numericUpDownComWriteTimeout.ValueChanged += new EventHandler(numericUpDownComWriteTimeout_ValueChanged);
            
            meterPinged += new EventHandler(Form1_meterPinged);
            pollingEnd += new EventHandler(Form1_pollingEnd);

        }

        void numericUpDownComWriteTimeout_ValueChanged(object sender, EventArgs e)
        {
            setVirtualSerialPort();
        }

        DataTable dt = new DataTable("meters");
        public string worksheetName = "Лист1";

        //список, хранящий номера параметров в перечислении Params драйвера
        //целесообразно его сделать здесь, так как кол-во считываемых значений зависит от кол-ва колонок
        List<int> paramCodes = null;
        private void createMainTable(ref DataTable dt)
        {
            paramCodes = new List<int>();

            //creating columns for internal data table
            DataColumn column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "№ кв.";
            column.ColumnName = "colFlat";

            column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "S/N";
            column.ColumnName = "colFactory";

            column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "Результат";
            column.ColumnName = "colResult";

            column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "Канал";
            column.ColumnName = "colChannel";

            column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "Счетчик";
            column.ColumnName = "colMeterNumber";

            column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "IP";
            column.ColumnName = "colIp";

            column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "Port";
            column.ColumnName = "colPort";

            column = dt.Columns.Add();
            column.DataType = typeof(string);
            column.Caption = "Значение";
            column.ColumnName = "colVal1";

            DataRow captionRow = dt.NewRow();
            for (int i = 0; i < dt.Columns.Count; i++)
                captionRow[i] = dt.Columns[i].Caption;
            dt.Rows.Add(captionRow);

        }

        private void loadXlsFile()
        {
            try
            {
                doStopProcess = false;
                buttonStop.Enabled = true;

                string fileName = ofd1.FileName;
                Workbook book = Workbook.Load(fileName);

                //auto detection of working mode
                object typeDirectiveVal = "";

                try
                {
                    Row zeroRow = book.Worksheets[0].Cells.GetRow(0);
                    typeDirectiveVal = zeroRow.GetCell(0).Value;
                }
                catch (Exception ex)
                {
                    return;
                }

                dt = new DataTable();
                createMainTable(ref dt);

                int rowsInFile = 0;
                for (int i = 0; i < book.Worksheets.Count; i++)
                    rowsInFile += book.Worksheets[i].Cells.LastRowIndex - firstRowIndex;

                //setting up progress bar
                toolStripProgressBar1.Minimum = 0;
                toolStripProgressBar1.Maximum = rowsInFile;
                toolStripProgressBar1.Step = 1;

                //filling internal data table with *.xls file data according to *.config file
                for (int i = 0; i < 1; i++)
                {
                    Worksheet sheet = book.Worksheets[i];
                    //если пусто в номере квартиры, берем предыдущий
                    string strFlatPrevNumber = "";

                    for (int rowIndex = firstRowIndex; rowIndex <= sheet.Cells.LastRowIndex; rowIndex++)
                    {
                        if (doStopProcess)
                        {
                            buttonStop.Enabled = false;
                            return;
                        }

                        Row row_l = sheet.Cells.GetRow(rowIndex);
                        DataRow dataRow = dt.NewRow();




                        object oFlatNumber = row_l.GetCell(flatNumberColumnIndex).Value;
                        int iFlatNumber = -1;
                        if (oFlatNumber != null)
                        {
                            string tmpStrNumb = oFlatNumber.ToString().Replace("Квартира ", "");
                            strFlatPrevNumber = tmpStrNumb;
                            if (!int.TryParse(tmpStrNumb, out iFlatNumber))
                            {
                                incrProgressBar();
                                continue;
                            }

                            incrProgressBar();
                            dataRow[0] = iFlatNumber;
                        }
                        else
                        {
                            if (!int.TryParse(strFlatPrevNumber, out iFlatNumber))
                            {
                                incrProgressBar();
                                continue;     
                            }

                            incrProgressBar();
                            dataRow[0] = iFlatNumber;
                 
                        }


                        dataRow[1] = row_l.GetCell(factoryNumberColumnIndex).Value;

                        //предустановленные
                        dataRow[3] = row_l.GetCell(colChannelIndex).Value;
                        dataRow[4] = row_l.GetCell(colMeterNameIndex).Value;
                        dataRow[5] = row_l.GetCell(colIPIndex).Value;
                        dataRow[6] = row_l.GetCell(colPortIndex).Value;
                        string strTmpPredVal = row_l.GetCell(colPredValue).Value == null ? "" : row_l.GetCell(colPredValue).Value.ToString().Replace(" ", "");
                        dataRow[7] = strTmpPredVal;

                        dt.Rows.Add(dataRow);
                    }
                }


                dgv1.DataSource = dt;

                toolStripProgressBar1.Value = 0;
                toolStripProgressBar1.Maximum = dt.Rows.Count - 1;
                toolStripStatusLabel1.Text = String.Format("({0}/{1})", toolStripProgressBar1.Value, toolStripProgressBar1.Maximum);

                InputDataReady = true;
                pollingByMetersTable = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Невозможно загрузить таблицу, проверьте что файл не открыт в другой программе", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void buttonImport_Click(object sender, EventArgs e)
        {
            if (ofd1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                loadXlsFile();
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            if (sfd1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //create new xls file
                string file = sfd1.FileName;
                Workbook workbook = new Workbook();
                Worksheet worksheet = new Worksheet(worksheetName);

                //office 2010 will not open file if there is less than 100 cells
                for (int i = 0; i < 100; i++)
                    worksheet.Cells[i, 0] = new Cell("");

                //copying data from data table
                for (int rowIndex = 0; rowIndex < dt.Rows.Count; rowIndex++)
                {
                    for (int colIndex = 0; colIndex < dt.Columns.Count; colIndex++)
                    {
                        worksheet.Cells[rowIndex, colIndex] = new Cell(dt.Rows[rowIndex][colIndex].ToString());
                    }
                }

                workbook.Worksheets.Add(worksheet);
                workbook.Save(file);
            }
        }

        private void incrProgressBar()
        {
            if (toolStripProgressBar1.Value < toolStripProgressBar1.Maximum)
            {
                toolStripProgressBar1.Value += 1;
                toolStripStatusLabel1.Text = String.Format("({0}/{1})", toolStripProgressBar1.Value, toolStripProgressBar1.Maximum);
            }
        }

        public event EventHandler meterPinged;
        void Form1_meterPinged(object sender, EventArgs e)
        {
            incrProgressBar();
        }

        public event EventHandler pollingEnd;
        void Form1_pollingEnd(object sender, EventArgs e)
        {
            InProgress = false;
            doStopProcess = false;
        }

        Thread pingThr = null;
        //Обработчик кнопки "Тест связи"
        private void buttonPing_Click(object sender, EventArgs e)
        {
            InProgress = true;
            doStopProcess = false;

            DeleteLogFiles();

            pingThr = new Thread(pingMeters);
            pingThr.Start((object)dt);
        }

        int attempts = 3;
        private void pingMeters(Object metersDt)
        {
            return;
        }

        struct PollMetersArguments
        {
            public DataTable dt;
            public List<int> incorrectRows;
        }

        enum ParamTypes
        {
            daily,
            monthly,
            halfs
        }

        //Обработчик кнопки "Опрос"
        private void buttonPoll_Click(object sender, EventArgs e)
        {
            InProgress = true;
            doStopProcess = false;

            DeleteLogFiles();

            PollMetersArguments pma = new PollMetersArguments();
            pma.dt = dt;
            pma.incorrectRows = null;

            //pingThr = new Thread(pollMeters);
            //pingThr.Start((object)pma);
            //int metersCount = 0;
            //if (!Meter.getMetersTableEntriesNumber(out metersCount))
            //{
            //    MessageBox.Show("Не удалось определить кол-во записей в таблице счетчиков");
            //    return;
            //}

           //Meter.sendAbort();

            switch (selectedParamType)
            {
                case ParamTypes.daily:
                {
                    doPollDaily(pma);
                    break;
                }
                case ParamTypes.monthly:
                {
                    doPollMonthly(pma);
                    break;
                }
                case ParamTypes.halfs:
                {
                    doPollSlices(pma);
                    break;
                }
            }

            InProgress = false;
        }


        private void doPollParams(string[] capturedParams, ParamTypes paramType, PollMetersArguments pmaInp)
        {
            DataTable dt = metersDataTable.Copy();


            int newColIndex = dt.Columns.Count + 1;

            DataColumn tmpDataCol = dt.Columns.Add();
            tmpDataCol.DataType = typeof(string);
            tmpDataCol.Caption = "S/N";
            dt.Rows[0][newColIndex - 1] = tmpDataCol.Caption;

            int tmpCnt = 0;
            foreach (string prmName in capturedParams)
            {
                tmpDataCol = dt.Columns.Add();
                tmpDataCol.DataType = typeof(string);
                tmpDataCol.Caption = prmName;

                dt.Rows[0][newColIndex + tmpCnt] = tmpDataCol.Caption;
                tmpCnt++;
            }

            Invoke(new MethodInvoker(() => { dgv1.DataSource = dt; dgv1.Refresh(); }));

            for (int i = 1; i < dt.Rows.Count; i++)
            {
                object oID = dt.Rows[i]["colID"];

                if (oID == null) continue;

                int meterId = 0;
                if (!int.TryParse(oID.ToString(), out meterId)) continue;

                Meter.Init(0, meterId.ToString(), Vp);

                List<UM_RTU40.ValueUM> valueList = new List<UM_RTU40.ValueUM>();
                float[] results = new float[capturedParams.Length];
                switch (paramType)
                {
                    case ParamTypes.daily:
                        {
                            for (ushort t = 0; t < 3; t++)
                                if (!checkBox1.Checked) Meter.ReadDailyValues(DateTime.Now.Date, 0, t, ref results[t]);
                                else Meter.ReadDailyValues2(0, t, ref results[t]);

                            break;
                        }
                    case ParamTypes.monthly:
                        {
                            for (ushort t = 0; t < 3; t++)
                                Meter.ReadMonthlyValues(DateTime.Now.Date, 0, t, ref results[t]);

                            break;
                        }
                    case ParamTypes.halfs:
                        {
                            if (!Meter.getDailyValuesForID(meterId, DateTime.Now.Date, out valueList)) continue;
                            break;
                        }
                    default: return;
                }

                string sN = "";
                Meter.ReadSerialNumber(ref sN);

                for (int j = 0; j < results.Length; j++)
                {
                    dt.Rows[i][newColIndex + j] = results[j];
                }

                dt.Rows[i][newColIndex - 1] = sN;



                Invoke(new MethodInvoker(() => { dgv1.DataSource = dt; dgv1.Refresh(); }));
            }

            Vp.ClosePort();

            if (pollingEnd != null)
                pollingEnd.Invoke(this, new EventArgs());
 
            
        }

        private void doPollDaily(PollMetersArguments pmaInp)
        {
            //указаны на странице 62
            string[] capturedParams = { "dA+0", "dA+1", "dA+2", "dA+3", "dA+4" };
            doPollParams(capturedParams, ParamTypes.daily, pmaInp);
        }

        private void doPollMonthly(PollMetersArguments pmaInp)
        {
            //указаны на странице 62
            string[] capturedParams = { "MA+0", "MA+1", "MA+2", "MA+3", "MA+4" };
            doPollParams(capturedParams, ParamTypes.monthly, pmaInp);
        }

        private void doPollSlices(PollMetersArguments pmaInp)
        {
            //указаны на странице 62
            string[] capturedParams = { "dA+0", "dA+1", "dA+2", "dA+3", "dA+4" };
            doPollParams(capturedParams, ParamTypes.halfs, pmaInp);
        }



        //Обработчик клавиши "Стоп"
        private void buttonStop_Click(object sender, EventArgs e)
        {
            doStopProcess = true;

            buttonStop.Enabled = false;
            dgv1.Enabled = false;
        }

        private void comboBoxComPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            setVirtualSerialPort();
        }

        private void numericUpDownComReadTimeout_ValueChanged(object sender, EventArgs e)
        {
            setVirtualSerialPort();
        }

        private void checkBoxPollOffline_CheckedChanged(object sender, EventArgs e)
        {

            if (bPollOnlyOffline)
            {
                buttonPoll.Text = "Читать";
            }
            else
            {
                buttonPoll.Text = "Записать";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (InProgress)
            {
                MessageBox.Show("Остановите опрос перед закрытием программы","Напоминание");
                e.Cancel = true;
                return;
            }

            if (Vp.isOpened())
                Vp.ClosePort();
        }

        private void DeleteLogFiles()
        {
            string curDir = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                FileInfo fi = new FileInfo(curDir + "teplouchetlog.pi");
                if (fi.Exists)
                    fi.Delete();

                fi = new FileInfo(curDir + "metersinfo.pi");
                if (fi.Exists)
                    fi.Delete();

                fi = new FileInfo(curDir + "datainfo.pi");
                if (fi.Exists)
                    fi.Delete();
            }
            catch (Exception ex)
            {
                //
            }
        }
        public void WriteToLog(string str, bool doWrite = true)
        {
            if (doWrite)
            {
                StreamWriter sw = null;
                FileStream fs = null;
                try
                {
                    string curDir = AppDomain.CurrentDomain.BaseDirectory;
                    fs = new FileStream(curDir + "metersinfo.pi", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    sw = new StreamWriter(fs, Encoding.Default);
                    sw.WriteLine(DateTime.Now.ToString() + ": " + str);
                    sw.Close();
                    fs.Close();
                }
                catch
                {
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                        sw = null;
                    }
                    if (fs != null)
                    {
                        fs.Close();
                        fs = null;
                    }
                }
            }
        }
        public void WriteToSeparateLog(string str, bool doWrite = true)
        {
            if (doWrite)
            {
                StreamWriter sw = null;
                FileStream fs = null;
                try
                {
                    string curDir = AppDomain.CurrentDomain.BaseDirectory;
                    fs = new FileStream(curDir + "datainfo.pi", FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    sw = new StreamWriter(fs, Encoding.Default);
                    sw.WriteLine(DateTime.Now.ToString() + ": " + str);
                    sw.Close();
                    fs.Close();
                }
                catch
                {
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                        sw = null;
                    }
                    if (fs != null)
                    {
                        fs.Close();
                        fs = null;
                    }
                }
            }
        }
        #region Панель разработчика

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.A)
                DeveloperMode = !DeveloperMode;
            else if (e.Control && e.Shift && e.KeyCode == Keys.D)
                DemoMode = !DemoMode;
        }

        bool bDeveloperMode = false;
        public bool DeveloperMode
        {
            get { return bDeveloperMode; }
            set
            {
                bDeveloperMode = value;

                if (bDeveloperMode)
                {
                    this.Text += FORM_TEXT_DEV_ON;
                    this.Height = this.Height + groupBox1.Height;
                    groupBox1.Visible = true;

                }
                else
                {
                    this.Text = this.Text.Replace(FORM_TEXT_DEV_ON, String.Empty);
                    groupBox1.Visible = false;
                    this.Height = this.Height - groupBox1.Height;
                }
            }
        }

        private void checkBoxTcp_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb.Checked == false)
            {
                if (!refreshSerialPortComboBox()) return;
                comboBoxComPorts.Enabled = true;
            }
            else
            {
                comboBoxComPorts.Enabled = false;
            }
            if (!setVirtualSerialPort()) return;
        }



        #endregion

        private void pictureBoxLogo_Click(object sender, EventArgs e)
        {
            Process.Start("http://prizmer.ru/");
        }

        DataTable metersDataTable = null;
        private void btnGetMetersTable_Click(object sender, EventArgs e)
        {           
            //string serial = "";
            //Meter.ReadSerialNumber(ref serial);

            metersDataTable = null;

            //на всякий случай)))
           // Meter.sendAbort();
            InProgress = true;

            if (!Meter.getMetersTable(ref metersDataTable))
            {
                InProgress = false;
                Vp.ClosePort();
                MessageBox.Show("Не удалось прочитать таблицу приборов", "Ошибка соединения",  MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                return;
            }

            dt = metersDataTable.Copy();
            dgv1.DataSource = dt;

            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Maximum = dt.Rows.Count - 1;
            toolStripStatusLabel1.Text = String.Format("({0}/{1})", toolStripProgressBar1.Value, toolStripProgressBar1.Maximum);

            InProgress = false;
            InputDataReady = true;
            pollingByMetersTable = true;

            Vp.ClosePort();
        }

        private void rbSelectParamsType_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            switch (rb.Tag.ToString())
            {
                case "daily":
                {
                    selectedParamType = ParamTypes.daily;
                    break;
                }
                case "monthly":
                {
                    selectedParamType = ParamTypes.monthly;
                    break;
                }
                case "halfs":
                {
                    selectedParamType = ParamTypes.halfs;
                    break;
                }                
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            textBoxIp.Text = "172.40.40.8";
            textBoxPort.Text = "4001";
        }
    }
}
