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
            this.SerialNumBox = new System.Windows.Forms.TextBox();
            this.MetersGrid = new System.Windows.Forms.DataGridView();
            this.name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.address = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.factory_number_manual = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.factory_number_readed = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.driver_name = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PortButton = new System.Windows.Forms.Button();
            this.readParamsButton = new System.Windows.Forms.Button();
            this.meterInfoTextBox = new System.Windows.Forms.RichTextBox();
            this.IsSearchByIdCheckBox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.MetersGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // SerialNumBox
            // 
            this.SerialNumBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SerialNumBox.Location = new System.Drawing.Point(12, 9);
            this.SerialNumBox.Name = "SerialNumBox";
            this.SerialNumBox.Size = new System.Drawing.Size(177, 22);
            this.SerialNumBox.TabIndex = 2;
            this.SerialNumBox.TextChanged += new System.EventHandler(this.SerialNumBox_TextChanged);
            // 
            // MetersGrid
            // 
            this.MetersGrid.AllowUserToAddRows = false;
            this.MetersGrid.AllowUserToDeleteRows = false;
            this.MetersGrid.AllowUserToResizeColumns = false;
            this.MetersGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MetersGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            this.MetersGrid.ColumnHeadersHeight = 37;
            this.MetersGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.MetersGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.name,
            this.address,
            this.factory_number_manual,
            this.factory_number_readed,
            this.driver_name});
            this.MetersGrid.Location = new System.Drawing.Point(12, 42);
            this.MetersGrid.MultiSelect = false;
            this.MetersGrid.Name = "MetersGrid";
            this.MetersGrid.RowHeadersVisible = false;
            this.MetersGrid.RowTemplate.Height = 24;
            this.MetersGrid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.MetersGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.MetersGrid.Size = new System.Drawing.Size(643, 300);
            this.MetersGrid.TabIndex = 3;
            this.MetersGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.MetersGrid_DataError);
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
            // 
            // factory_number_readed
            // 
            this.factory_number_readed.DataPropertyName = "factory_number_readed";
            this.factory_number_readed.HeaderText = "Считанный серийный номер";
            this.factory_number_readed.Name = "factory_number_readed";
            this.factory_number_readed.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // driver_name
            // 
            this.driver_name.DataPropertyName = "driver_name";
            this.driver_name.HeaderText = "Имя драйвера";
            this.driver_name.Name = "driver_name";
            this.driver_name.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            // 
            // PortButton
            // 
            this.PortButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
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
            this.readParamsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
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
            this.meterInfoTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.meterInfoTextBox.Location = new System.Drawing.Point(12, 348);
            this.meterInfoTextBox.Name = "meterInfoTextBox";
            this.meterInfoTextBox.ReadOnly = true;
            this.meterInfoTextBox.Size = new System.Drawing.Size(643, 112);
            this.meterInfoTextBox.TabIndex = 7;
            this.meterInfoTextBox.Text = "";
            // 
            // IsSearchByIdCheckBox
            // 
            this.IsSearchByIdCheckBox.AutoSize = true;
            this.IsSearchByIdCheckBox.Location = new System.Drawing.Point(195, 10);
            this.IsSearchByIdCheckBox.Name = "IsSearchByIdCheckBox";
            this.IsSearchByIdCheckBox.Size = new System.Drawing.Size(151, 21);
            this.IsSearchByIdCheckBox.TabIndex = 8;
            this.IsSearchByIdCheckBox.Text = "поиск параметров";
            this.IsSearchByIdCheckBox.UseVisualStyleBackColor = true;
            this.IsSearchByIdCheckBox.CheckedChanged += new System.EventHandler(this.IsSearchByIdCheckBox_CheckedChanged);
            // 
            // MetersSearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(667, 468);
            this.Controls.Add(this.IsSearchByIdCheckBox);
            this.Controls.Add(this.meterInfoTextBox);
            this.Controls.Add(this.readParamsButton);
            this.Controls.Add(this.PortButton);
            this.Controls.Add(this.MetersGrid);
            this.Controls.Add(this.SerialNumBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(685, 515);
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
        private System.Windows.Forms.TextBox SerialNumBox;
        private System.Windows.Forms.DataGridView MetersGrid;
        private System.Windows.Forms.Button PortButton;
        private System.Windows.Forms.Button readParamsButton;
        private System.Windows.Forms.RichTextBox meterInfoTextBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn name;
        private System.Windows.Forms.DataGridViewTextBoxColumn address;
        private System.Windows.Forms.DataGridViewTextBoxColumn factory_number_manual;
        private System.Windows.Forms.DataGridViewTextBoxColumn factory_number_readed;
        private System.Windows.Forms.DataGridViewTextBoxColumn driver_name;
        private System.Windows.Forms.CheckBox IsSearchByIdCheckBox;
    }
}

