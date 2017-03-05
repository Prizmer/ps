namespace Prizmer.PoolServer
{
    partial class FormMain
    {
        /// <summary>
        /// Требуется переменная конструктора.
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
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.btnStartReading = new System.Windows.Forms.Button();
            this.btnEndReading = new System.Windows.Forms.Button();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.cbServerStarted = new System.Windows.Forms.CheckBox();
            this.rbCom = new System.Windows.Forms.RadioButton();
            this.rbTCP = new System.Windows.Forms.RadioButton();
            this.tbAddress = new System.Windows.Forms.TextBox();
            this.tbPort = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox1.Image = global::PoolServer.Properties.Resources.logo2;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(286, 82);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tbPort);
            this.groupBox1.Controls.Add(this.tbAddress);
            this.groupBox1.Controls.Add(this.rbTCP);
            this.groupBox1.Controls.Add(this.rbCom);
            this.groupBox1.Controls.Add(this.progressBar1);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.dateTimePicker2);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.dateTimePicker1);
            this.groupBox1.Controls.Add(this.btnEndReading);
            this.groupBox1.Controls.Add(this.btnStartReading);
            this.groupBox1.Controls.Add(this.comboBox1);
            this.groupBox1.Location = new System.Drawing.Point(0, 110);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 177);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Дочитка показаний";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "m230"});
            this.comboBox1.Location = new System.Drawing.Point(155, 67);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(109, 21);
            this.comboBox1.TabIndex = 0;
            // 
            // btnStartReading
            // 
            this.btnStartReading.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartReading.Location = new System.Drawing.Point(178, 138);
            this.btnStartReading.Name = "btnStartReading";
            this.btnStartReading.Size = new System.Drawing.Size(62, 24);
            this.btnStartReading.TabIndex = 1;
            this.btnStartReading.Text = "Старт";
            this.btnStartReading.UseVisualStyleBackColor = true;
            this.btnStartReading.Click += new System.EventHandler(this.btnStartReading_Click);
            // 
            // btnEndReading
            // 
            this.btnEndReading.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEndReading.Location = new System.Drawing.Point(239, 138);
            this.btnEndReading.Name = "btnEndReading";
            this.btnEndReading.Size = new System.Drawing.Size(25, 24);
            this.btnEndReading.TabIndex = 2;
            this.btnEndReading.Text = "X";
            this.btnEndReading.UseVisualStyleBackColor = true;
            this.btnEndReading.Click += new System.EventHandler(this.btnEndReading_Click);
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker1.Location = new System.Drawing.Point(155, 19);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(109, 20);
            this.dateTimePicker1.TabIndex = 3;
            this.dateTimePicker1.Value = new System.DateTime(2017, 3, 2, 0, 0, 0, 0);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Начало:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Конец:";
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker2.Location = new System.Drawing.Point(155, 43);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(109, 20);
            this.dateTimePicker2.TabIndex = 6;
            this.dateTimePicker2.Value = new System.DateTime(2017, 3, 2, 0, 0, 0, 0);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 67);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Драйвер:";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(15, 139);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(154, 23);
            this.progressBar1.TabIndex = 8;
            // 
            // cbServerStarted
            // 
            this.cbServerStarted.AutoSize = true;
            this.cbServerStarted.Checked = true;
            this.cbServerStarted.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbServerStarted.Location = new System.Drawing.Point(15, 87);
            this.cbServerStarted.Name = "cbServerStarted";
            this.cbServerStarted.Size = new System.Drawing.Size(110, 17);
            this.cbServerStarted.TabIndex = 2;
            this.cbServerStarted.Text = "Сервер запущен";
            this.cbServerStarted.UseVisualStyleBackColor = true;
            this.cbServerStarted.CheckedChanged += new System.EventHandler(this.cbServerStarted_CheckedChanged);
            // 
            // rbCom
            // 
            this.rbCom.AutoSize = true;
            this.rbCom.Location = new System.Drawing.Point(15, 92);
            this.rbCom.Name = "rbCom";
            this.rbCom.Size = new System.Drawing.Size(49, 17);
            this.rbCom.TabIndex = 9;
            this.rbCom.Tag = "com";
            this.rbCom.Text = "COM";
            this.rbCom.UseVisualStyleBackColor = true;
            this.rbCom.CheckedChanged += new System.EventHandler(this.rbCom_CheckedChanged);
            // 
            // rbTCP
            // 
            this.rbTCP.AutoSize = true;
            this.rbTCP.Checked = true;
            this.rbTCP.Location = new System.Drawing.Point(15, 111);
            this.rbTCP.Name = "rbTCP";
            this.rbTCP.Size = new System.Drawing.Size(46, 17);
            this.rbTCP.TabIndex = 10;
            this.rbTCP.TabStop = true;
            this.rbTCP.Tag = "tcp";
            this.rbTCP.Text = "TCP";
            this.rbTCP.UseVisualStyleBackColor = true;
            this.rbTCP.CheckedChanged += new System.EventHandler(this.rbCom_CheckedChanged);
            // 
            // tbAddress
            // 
            this.tbAddress.Location = new System.Drawing.Point(82, 99);
            this.tbAddress.Name = "tbAddress";
            this.tbAddress.Size = new System.Drawing.Size(112, 20);
            this.tbAddress.TabIndex = 11;
            this.tbAddress.Text = "192.168.23.62";
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(200, 99);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(64, 20);
            this.tbPort.TabIndex = 12;
            this.tbPort.Text = "3001";
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(286, 106);
            this.Controls.Add(this.cbServerStarted);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.ShowIcon = false;
            this.Text = "ПИ - Сервер опроса";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker dateTimePicker2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.Button btnEndReading;
        private System.Windows.Forms.Button btnStartReading;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.CheckBox cbServerStarted;
        private System.Windows.Forms.TextBox tbPort;
        private System.Windows.Forms.TextBox tbAddress;
        private System.Windows.Forms.RadioButton rbTCP;
        private System.Windows.Forms.RadioButton rbCom;
    }
}

