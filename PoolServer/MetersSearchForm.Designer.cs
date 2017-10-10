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
            this.label1 = new System.Windows.Forms.Label();
            this.SerialNumBox = new System.Windows.Forms.TextBox();
            this.MetersGrid = new System.Windows.Forms.DataGridView();
            this.PortButton = new System.Windows.Forms.Button();
            this.readParamsButton = new System.Windows.Forms.Button();
            this.meterInfoTextBox = new System.Windows.Forms.RichTextBox();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.address = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.factory_number_manual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.factory_number_readed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.password = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dt_install = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dt_last_read = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.time_delay_current = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            this.SerialNumBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.SerialNumBox_KeyPress);
            // 
            // MetersGrid
            // 
            this.MetersGrid.AllowUserToAddRows = false;
            this.MetersGrid.AllowUserToDeleteRows = false;
            this.MetersGrid.AllowUserToResizeColumns = false;
            this.MetersGrid.AllowUserToResizeRows = false;
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
            this.MetersGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.MetersGrid.Location = new System.Drawing.Point(12, 42);
            this.MetersGrid.MultiSelect = false;
            this.MetersGrid.Name = "MetersGrid";
            this.MetersGrid.ReadOnly = true;
            this.MetersGrid.RowHeadersVisible = false;
            this.MetersGrid.RowTemplate.Height = 24;
            this.MetersGrid.RowTemplate.ReadOnly = true;
            this.MetersGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.MetersGrid.Size = new System.Drawing.Size(643, 300);
            this.MetersGrid.TabIndex = 3;
            // 
            // PortButton
            // 
            this.PortButton.Location = new System.Drawing.Point(364, 5);
            this.PortButton.Name = "PortButton";
            this.PortButton.Size = new System.Drawing.Size(76, 31);
            this.PortButton.TabIndex = 5;
            this.PortButton.Text = "Порт";
            this.PortButton.UseVisualStyleBackColor = true;
            this.PortButton.Click += new System.EventHandler(this.PortButton_Click);
            // 
            // readParamsButton
            // 
            this.readParamsButton.Location = new System.Drawing.Point(446, 5);
            this.readParamsButton.Name = "readParamsButton";
            this.readParamsButton.Size = new System.Drawing.Size(209, 31);
            this.readParamsButton.TabIndex = 6;
            this.readParamsButton.Text = "Считываемые параметры";
            this.readParamsButton.UseVisualStyleBackColor = true;
            this.readParamsButton.Click += new System.EventHandler(this.ReadParamsButton_Click);
            // 
            // meterInfoTextBox
            // 
            this.meterInfoTextBox.Location = new System.Drawing.Point(12, 348);
            this.meterInfoTextBox.Name = "meterInfoTextBox";
            this.meterInfoTextBox.ReadOnly = true;
            this.meterInfoTextBox.Size = new System.Drawing.Size(643, 112);
            this.meterInfoTextBox.TabIndex = 7;
            this.meterInfoTextBox.Text = "";
            // 
            // name
            // 
            this.name.DataPropertyName = "name";
            this.name.HeaderText = "Имя устройства";
            this.name.Name = "name";
            this.name.ReadOnly = true;
            this.name.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.name.Width = 125;
            // 
            // address
            // 
            this.address.DataPropertyName = "address";
            this.address.HeaderText = "Адрес";
            this.address.Name = "address";
            this.address.ReadOnly = true;
            this.address.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.address.Width = 50;
            // 
            // factory_number_manual
            // 
            this.factory_number_manual.DataPropertyName = "factory_number_manual";
            this.factory_number_manual.HeaderText = "Введённый серийный номер";
            this.factory_number_manual.Name = "factory_number_manual";
            this.factory_number_manual.ReadOnly = true;
            this.factory_number_manual.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.factory_number_manual.Width = 150;
            // 
            // factory_number_readed
            // 
            this.factory_number_readed.DataPropertyName = "factory_number_readed";
            this.factory_number_readed.HeaderText = "Считанный серийный номер";
            this.factory_number_readed.Name = "factory_number_readed";
            this.factory_number_readed.ReadOnly = true;
            this.factory_number_readed.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.factory_number_readed.Width = 150;
            // 
            // password
            // 
            this.password.DataPropertyName = "password";
            this.password.HeaderText = "Пароль";
            this.password.Name = "password";
            this.password.ReadOnly = true;
            this.password.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.password.Width = 80;
            // 
            // dt_install
            // 
            this.dt_install.DataPropertyName = "dt_install";
            this.dt_install.HeaderText = "Дата установки";
            this.dt_install.Name = "dt_install";
            this.dt_install.ReadOnly = true;
            this.dt_install.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // dt_last_read
            // 
            this.dt_last_read.DataPropertyName = "dt_last_read";
            this.dt_last_read.HeaderText = "Дата последнего считывания";
            this.dt_last_read.Name = "dt_last_read";
            this.dt_last_read.ReadOnly = true;
            this.dt_last_read.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // time_delay_current
            // 
            this.time_delay_current.DataPropertyName = "time_delay_current";
            this.time_delay_current.HeaderText = "Текущая задержка";
            this.time_delay_current.Name = "time_delay_current";
            this.time_delay_current.ReadOnly = true;
            this.time_delay_current.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // MetersSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(667, 468);
            this.Controls.Add(this.meterInfoTextBox);
            this.Controls.Add(this.readParamsButton);
            this.Controls.Add(this.PortButton);
            this.Controls.Add(this.MetersGrid);
            this.Controls.Add(this.SerialNumBox);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MetersSearchForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Поиск счётчиков";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MetersSearchForm_FormClosing);
            this.Load += new System.EventHandler(this.MetersSearchForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.MetersGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox SerialNumBox;
        private System.Windows.Forms.DataGridView MetersGrid;
        private System.Windows.Forms.Button PortButton;
        private System.Windows.Forms.Button readParamsButton;
        private System.Windows.Forms.RichTextBox meterInfoTextBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn address;
        private System.Windows.Forms.DataGridViewTextBoxColumn factory_number_manual;
        private System.Windows.Forms.DataGridViewTextBoxColumn factory_number_readed;
        private System.Windows.Forms.DataGridViewTextBoxColumn password;
        private System.Windows.Forms.DataGridViewTextBoxColumn dt_install;
        private System.Windows.Forms.DataGridViewTextBoxColumn dt_last_read;
        private System.Windows.Forms.DataGridViewTextBoxColumn time_delay_current;
    }
}

