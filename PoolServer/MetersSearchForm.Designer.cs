namespace Prizmer.PoolServer
{
    partial class MetersSearchForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MetersSearchForm));
            this.label1 = new System.Windows.Forms.Label();
            this.SerialNumBox = new System.Windows.Forms.TextBox();
            this.meterInfoTextBox = new System.Windows.Forms.RichTextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.btnRead = new System.Windows.Forms.Button();
            this.btnCopyPrmsToBuffer = new System.Windows.Forms.Button();
            this.cbReadAllParams = new System.Windows.Forms.CheckBox();
            this.lbParams = new System.Windows.Forms.ListBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.MetersGrid = new Prizmer.PoolServer.MyDataGridView();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.address = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.factory_number_manual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.factory_number_readed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.password = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dt_install = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dt_last_read = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.time_delay_current = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tbSelectedMeter = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.linkDeleteData = new System.Windows.Forms.LinkLabel();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MetersGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(124, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Серийный номер:";
            // 
            // SerialNumBox
            // 
            this.SerialNumBox.Location = new System.Drawing.Point(139, 9);
            this.SerialNumBox.Name = "SerialNumBox";
            this.SerialNumBox.Size = new System.Drawing.Size(219, 22);
            this.SerialNumBox.TabIndex = 2;
            this.SerialNumBox.TextChanged += new System.EventHandler(this.SerialNumBox_TextChanged);
            // 
            // meterInfoTextBox
            // 
            this.meterInfoTextBox.Location = new System.Drawing.Point(548, 348);
            this.meterInfoTextBox.Name = "meterInfoTextBox";
            this.meterInfoTextBox.ReadOnly = true;
            this.meterInfoTextBox.Size = new System.Drawing.Size(327, 251);
            this.meterInfoTextBox.TabIndex = 7;
            this.meterInfoTextBox.Text = "";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.linkDeleteData);
            this.groupBox1.Controls.Add(this.tbSelectedMeter);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.dateTimePicker1);
            this.groupBox1.Controls.Add(this.btnRead);
            this.groupBox1.Controls.Add(this.btnCopyPrmsToBuffer);
            this.groupBox1.Controls.Add(this.cbReadAllParams);
            this.groupBox1.Controls.Add(this.lbParams);
            this.groupBox1.Location = new System.Drawing.Point(14, 357);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(528, 277);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Управление";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker1.Location = new System.Drawing.Point(301, 21);
            this.dateTimePicker1.Margin = new System.Windows.Forms.Padding(4);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(144, 22);
            this.dateTimePicker1.TabIndex = 8;
            this.dateTimePicker1.Value = new System.DateTime(2017, 3, 2, 0, 0, 0, 0);
            // 
            // btnRead
            // 
            this.btnRead.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnRead.BackgroundImage")));
            this.btnRead.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnRead.Location = new System.Drawing.Point(463, 21);
            this.btnRead.Name = "btnRead";
            this.btnRead.Size = new System.Drawing.Size(52, 52);
            this.btnRead.TabIndex = 5;
            this.toolTip1.SetToolTip(this.btnRead, "Считать параметры");
            this.btnRead.UseVisualStyleBackColor = true;
            this.btnRead.Click += new System.EventHandler(this.btnRead_Click);
            // 
            // btnCopyPrmsToBuffer
            // 
            this.btnCopyPrmsToBuffer.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btnCopyPrmsToBuffer.BackgroundImage")));
            this.btnCopyPrmsToBuffer.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.btnCopyPrmsToBuffer.Location = new System.Drawing.Point(496, 240);
            this.btnCopyPrmsToBuffer.Name = "btnCopyPrmsToBuffer";
            this.btnCopyPrmsToBuffer.Size = new System.Drawing.Size(26, 27);
            this.btnCopyPrmsToBuffer.TabIndex = 6;
            this.toolTip1.SetToolTip(this.btnCopyPrmsToBuffer, "Копировать список параметров в буфер обмена");
            this.btnCopyPrmsToBuffer.UseVisualStyleBackColor = true;
            this.btnCopyPrmsToBuffer.Click += new System.EventHandler(this.btnCopyPrmsToBuffer_Click);
            // 
            // cbReadAllParams
            // 
            this.cbReadAllParams.AutoSize = true;
            this.cbReadAllParams.Location = new System.Drawing.Point(9, 52);
            this.cbReadAllParams.Name = "cbReadAllParams";
            this.cbReadAllParams.Size = new System.Drawing.Size(132, 21);
            this.cbReadAllParams.TabIndex = 4;
            this.cbReadAllParams.Text = "Все параметры";
            this.cbReadAllParams.UseVisualStyleBackColor = true;
            this.cbReadAllParams.CheckedChanged += new System.EventHandler(this.cbReadAllParams_CheckedChanged);
            // 
            // lbParams
            // 
            this.lbParams.FormattingEnabled = true;
            this.lbParams.ItemHeight = 16;
            this.lbParams.Location = new System.Drawing.Point(9, 82);
            this.lbParams.Name = "lbParams";
            this.lbParams.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbParams.Size = new System.Drawing.Size(506, 164);
            this.lbParams.TabIndex = 3;
            this.lbParams.SelectedIndexChanged += new System.EventHandler(this.lbParams_SelectedIndexChanged);
            // 
            // MetersGrid
            // 
            this.MetersGrid.AllowUserToAddRows = false;
            this.MetersGrid.AllowUserToDeleteRows = false;
            this.MetersGrid.AllowUserToResizeColumns = false;
            this.MetersGrid.AllowUserToResizeRows = false;
            this.MetersGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            this.MetersGrid.ColumnHeadersHeight = 37;
            this.MetersGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.MetersGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.name,
            this.address,
            this.factory_number_manual,
            this.factory_number_readed,
            this.password,
            this.dt_install,
            this.dt_last_read,
            this.time_delay_current});
            this.MetersGrid.Location = new System.Drawing.Point(12, 42);
            this.MetersGrid.MultiSelect = false;
            this.MetersGrid.Name = "MetersGrid";
            this.MetersGrid.RowHeadersVisible = false;
            this.MetersGrid.RowTemplate.Height = 24;
            this.MetersGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.MetersGrid.Size = new System.Drawing.Size(861, 300);
            this.MetersGrid.TabIndex = 3;
            this.MetersGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.MetersGrid_CellDoubleClick);
            this.MetersGrid.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.MetersGrid_PreviewKeyDown);
            // 
            // name
            // 
            this.name.DataPropertyName = "name";
            this.name.HeaderText = "Имя устройства";
            this.name.Name = "name";
            this.name.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.name.Width = 125;
            // 
            // address
            // 
            this.address.DataPropertyName = "address";
            this.address.HeaderText = "Адрес";
            this.address.Name = "address";
            this.address.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.address.Width = 50;
            // 
            // factory_number_manual
            // 
            this.factory_number_manual.DataPropertyName = "factory_number_manual";
            this.factory_number_manual.HeaderText = "Введённый серийный номер";
            this.factory_number_manual.Name = "factory_number_manual";
            this.factory_number_manual.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.factory_number_manual.Width = 150;
            // 
            // factory_number_readed
            // 
            this.factory_number_readed.DataPropertyName = "factory_number_readed";
            this.factory_number_readed.HeaderText = "Считанный серийный номер";
            this.factory_number_readed.Name = "factory_number_readed";
            this.factory_number_readed.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.factory_number_readed.Width = 150;
            // 
            // password
            // 
            this.password.DataPropertyName = "password";
            this.password.HeaderText = "Пароль";
            this.password.Name = "password";
            this.password.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.password.Width = 80;
            // 
            // dt_install
            // 
            this.dt_install.DataPropertyName = "dt_install";
            this.dt_install.HeaderText = "Дата установки";
            this.dt_install.Name = "dt_install";
            this.dt_install.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // dt_last_read
            // 
            this.dt_last_read.DataPropertyName = "dt_last_read";
            this.dt_last_read.HeaderText = "Дата последнего считывания";
            this.dt_last_read.Name = "dt_last_read";
            this.dt_last_read.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // time_delay_current
            // 
            this.time_delay_current.DataPropertyName = "time_delay_current";
            this.time_delay_current.HeaderText = "Текущая задержка";
            this.time_delay_current.Name = "time_delay_current";
            this.time_delay_current.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // tbSelectedMeter
            // 
            this.tbSelectedMeter.Location = new System.Drawing.Point(75, 21);
            this.tbSelectedMeter.Name = "tbSelectedMeter";
            this.tbSelectedMeter.ReadOnly = true;
            this.tbSelectedMeter.Size = new System.Drawing.Size(210, 22);
            this.tbSelectedMeter.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 17);
            this.label2.TabIndex = 11;
            this.label2.Text = "Выбран:";
            // 
            // linkDeleteData
            // 
            this.linkDeleteData.ActiveLinkColor = System.Drawing.Color.Blue;
            this.linkDeleteData.AutoSize = true;
            this.linkDeleteData.Location = new System.Drawing.Point(350, 52);
            this.linkDeleteData.Name = "linkDeleteData";
            this.linkDeleteData.Size = new System.Drawing.Size(95, 17);
            this.linkDeleteData.TabIndex = 13;
            this.linkDeleteData.TabStop = true;
            this.linkDeleteData.Text = "Очистить БД";
            this.toolTip1.SetToolTip(this.linkDeleteData, "Удалятся значения выбранных парамеров за выбранную дату");
            this.linkDeleteData.Visible = false;
            this.linkDeleteData.VisitedLinkColor = System.Drawing.Color.Blue;
            this.linkDeleteData.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkDeleteData_LinkClicked);
            // 
            // MetersSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(887, 633);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.meterInfoTextBox);
            this.Controls.Add(this.MetersGrid);
            this.Controls.Add(this.SerialNumBox);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MetersSearchForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Поиск счётчиков";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MetersSearchForm_FormClosing);
            this.Load += new System.EventHandler(this.MetersSearchForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MetersGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox SerialNumBox;
        private MyDataGridView MetersGrid;
        private System.Windows.Forms.RichTextBox meterInfoTextBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn address;
        private System.Windows.Forms.DataGridViewTextBoxColumn factory_number_manual;
        private System.Windows.Forms.DataGridViewTextBoxColumn factory_number_readed;
        private System.Windows.Forms.DataGridViewTextBoxColumn password;
        private System.Windows.Forms.DataGridViewTextBoxColumn dt_install;
        private System.Windows.Forms.DataGridViewTextBoxColumn dt_last_read;
        private System.Windows.Forms.DataGridViewTextBoxColumn time_delay_current;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox lbParams;
        private System.Windows.Forms.Button btnRead;
        private System.Windows.Forms.CheckBox cbReadAllParams;
        private System.Windows.Forms.Button btnCopyPrmsToBuffer;
        private System.Windows.Forms.ToolTip toolTip1;
        public System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.LinkLabel linkDeleteData;
        private System.Windows.Forms.TextBox tbSelectedMeter;
        private System.Windows.Forms.Label label2;
    }
}

