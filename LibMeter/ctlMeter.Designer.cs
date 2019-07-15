namespace Drivers.LibMeter
{
    partial class ctlMeters
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

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ctlMeters));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pbPreloader = new System.Windows.Forms.PictureBox();
            this.panelMain = new System.Windows.Forms.Panel();
            this.label7 = new System.Windows.Forms.Label();
            this.numParamTarif = new System.Windows.Forms.NumericUpDown();
            this.btnReadInfo = new System.Windows.Forms.Button();
            this.btnReadCurrent = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.numParamAddr = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.tbMeterAddress = new System.Windows.Forms.TextBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.gbAuxilary = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbMeterPassword = new System.Windows.Forms.TextBox();
            this.panelDailyMonthly = new System.Windows.Forms.Panel();
            this.btnReadMonthly = new System.Windows.Forms.Button();
            this.btnReadDaily = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.dtpDailyMonthly = new System.Windows.Forms.DateTimePicker();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.panelHalfs = new System.Windows.Forms.Panel();
            this.dtpTo = new System.Windows.Forms.DateTimePicker();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.dtpFrom = new System.Windows.Forms.DateTimePicker();
            this.cbAltHalfsMethod = new System.Windows.Forms.CheckBox();
            this.btnReadHalfs = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbPreloader)).BeginInit();
            this.panelMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numParamTarif)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numParamAddr)).BeginInit();
            this.gbAuxilary.SuspendLayout();
            this.panelDailyMonthly.SuspendLayout();
            this.panelHalfs.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.pbPreloader);
            this.groupBox1.Controls.Add(this.panelMain);
            this.groupBox1.Controls.Add(this.btnClearLog);
            this.groupBox1.Controls.Add(this.gbAuxilary);
            this.groupBox1.Controls.Add(this.panelDailyMonthly);
            this.groupBox1.Controls.Add(this.rtbLog);
            this.groupBox1.Controls.Add(this.panelHalfs);
            this.groupBox1.Location = new System.Drawing.Point(3, 2);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.groupBox1.Size = new System.Drawing.Size(916, 255);
            this.groupBox1.TabIndex = 50;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "(3) Интерфейс драйвера";
            // 
            // pbPreloader
            // 
            this.pbPreloader.ErrorImage = null;
            this.pbPreloader.Image = ((System.Drawing.Image)(resources.GetObject("pbPreloader.Image")));
            this.pbPreloader.Location = new System.Drawing.Point(654, 124);
            this.pbPreloader.Margin = new System.Windows.Forms.Padding(4);
            this.pbPreloader.Name = "pbPreloader";
            this.pbPreloader.Size = new System.Drawing.Size(126, 10);
            this.pbPreloader.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbPreloader.TabIndex = 82;
            this.pbPreloader.TabStop = false;
            this.pbPreloader.Visible = false;
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.label7);
            this.panelMain.Controls.Add(this.numParamTarif);
            this.panelMain.Controls.Add(this.btnReadInfo);
            this.panelMain.Controls.Add(this.btnReadCurrent);
            this.panelMain.Controls.Add(this.label6);
            this.panelMain.Controls.Add(this.numParamAddr);
            this.panelMain.Controls.Add(this.label2);
            this.panelMain.Controls.Add(this.tbMeterAddress);
            this.panelMain.Location = new System.Drawing.Point(6, 19);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(480, 53);
            this.panelMain.TabIndex = 81;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(251, 7);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(52, 17);
            this.label7.TabIndex = 69;
            this.label7.Text = "Тариф";
            // 
            // numParamTarif
            // 
            this.numParamTarif.Location = new System.Drawing.Point(254, 27);
            this.numParamTarif.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numParamTarif.Maximum = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this.numParamTarif.Name = "numParamTarif";
            this.numParamTarif.Size = new System.Drawing.Size(63, 22);
            this.numParamTarif.TabIndex = 68;
            this.numParamTarif.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            // 
            // btnReadInfo
            // 
            this.btnReadInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReadInfo.Location = new System.Drawing.Point(398, 20);
            this.btnReadInfo.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReadInfo.Name = "btnReadInfo";
            this.btnReadInfo.Size = new System.Drawing.Size(72, 28);
            this.btnReadInfo.TabIndex = 67;
            this.btnReadInfo.Text = "Инфо";
            this.toolTip1.SetToolTip(this.btnReadInfo, "Информация о приборе");
            this.btnReadInfo.UseVisualStyleBackColor = true;
            this.btnReadInfo.Click += new System.EventHandler(this.btnReadInfo_Click);
            // 
            // btnReadCurrent
            // 
            this.btnReadCurrent.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReadCurrent.Location = new System.Drawing.Point(326, 20);
            this.btnReadCurrent.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReadCurrent.Name = "btnReadCurrent";
            this.btnReadCurrent.Size = new System.Drawing.Size(66, 28);
            this.btnReadCurrent.TabIndex = 66;
            this.btnReadCurrent.Tag = "curr";
            this.btnReadCurrent.Text = "Т";
            this.toolTip1.SetToolTip(this.btnReadCurrent, "Текущие значения");
            this.btnReadCurrent.UseVisualStyleBackColor = true;
            this.btnReadCurrent.Click += new System.EventHandler(this.btnReadParam_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(170, 7);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 17);
            this.label6.TabIndex = 65;
            this.label6.Text = "Адрес";
            // 
            // numParamAddr
            // 
            this.numParamAddr.Location = new System.Drawing.Point(173, 26);
            this.numParamAddr.Margin = new System.Windows.Forms.Padding(4);
            this.numParamAddr.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numParamAddr.Name = "numParamAddr";
            this.numParamAddr.Size = new System.Drawing.Size(74, 22);
            this.numParamAddr.TabIndex = 55;
            this.numParamAddr.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 17);
            this.label2.TabIndex = 51;
            this.label2.Text = "Сетевой номер";
            // 
            // tbMeterAddress
            // 
            this.tbMeterAddress.Location = new System.Drawing.Point(5, 26);
            this.tbMeterAddress.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbMeterAddress.Name = "tbMeterAddress";
            this.tbMeterAddress.Size = new System.Drawing.Size(149, 22);
            this.tbMeterAddress.TabIndex = 50;
            this.tbMeterAddress.Text = "248";
            // 
            // btnClearLog
            // 
            this.btnClearLog.Location = new System.Drawing.Point(885, 19);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(20, 28);
            this.btnClearLog.TabIndex = 80;
            this.btnClearLog.Text = "x";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // gbAuxilary
            // 
            this.gbAuxilary.Controls.Add(this.label3);
            this.gbAuxilary.Controls.Add(this.tbMeterPassword);
            this.gbAuxilary.Location = new System.Drawing.Point(10, 166);
            this.gbAuxilary.Name = "gbAuxilary";
            this.gbAuxilary.Size = new System.Drawing.Size(466, 79);
            this.gbAuxilary.TabIndex = 79;
            this.gbAuxilary.TabStop = false;
            this.gbAuxilary.Text = "Дополнительно";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 17);
            this.label3.TabIndex = 52;
            this.label3.Text = "Пароль";
            // 
            // tbMeterPassword
            // 
            this.tbMeterPassword.Location = new System.Drawing.Point(14, 41);
            this.tbMeterPassword.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.tbMeterPassword.Name = "tbMeterPassword";
            this.tbMeterPassword.Size = new System.Drawing.Size(136, 22);
            this.tbMeterPassword.TabIndex = 51;
            this.tbMeterPassword.Text = "111111";
            this.tbMeterPassword.Leave += new System.EventHandler(this.tbMeterPassword_Leave);
            // 
            // panelDailyMonthly
            // 
            this.panelDailyMonthly.Controls.Add(this.btnReadMonthly);
            this.panelDailyMonthly.Controls.Add(this.btnReadDaily);
            this.panelDailyMonthly.Controls.Add(this.label1);
            this.panelDailyMonthly.Controls.Add(this.dtpDailyMonthly);
            this.panelDailyMonthly.Location = new System.Drawing.Point(6, 69);
            this.panelDailyMonthly.Name = "panelDailyMonthly";
            this.panelDailyMonthly.Size = new System.Drawing.Size(167, 91);
            this.panelDailyMonthly.TabIndex = 78;
            // 
            // btnReadMonthly
            // 
            this.btnReadMonthly.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReadMonthly.Location = new System.Drawing.Point(81, 55);
            this.btnReadMonthly.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReadMonthly.Name = "btnReadMonthly";
            this.btnReadMonthly.Size = new System.Drawing.Size(73, 28);
            this.btnReadMonthly.TabIndex = 79;
            this.btnReadMonthly.Tag = "month";
            this.btnReadMonthly.Text = "M";
            this.toolTip1.SetToolTip(this.btnReadMonthly, "На начало месяца");
            this.btnReadMonthly.UseVisualStyleBackColor = true;
            this.btnReadMonthly.Click += new System.EventHandler(this.btnReadParam_Click);
            // 
            // btnReadDaily
            // 
            this.btnReadDaily.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReadDaily.Location = new System.Drawing.Point(5, 55);
            this.btnReadDaily.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReadDaily.Name = "btnReadDaily";
            this.btnReadDaily.Size = new System.Drawing.Size(72, 28);
            this.btnReadDaily.TabIndex = 78;
            this.btnReadDaily.Tag = "day";
            this.btnReadDaily.Text = "С";
            this.toolTip1.SetToolTip(this.btnReadDaily, "На начало суток");
            this.btnReadDaily.UseVisualStyleBackColor = true;
            this.btnReadDaily.Click += new System.EventHandler(this.btnReadParam_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(145, 17);
            this.label1.TabIndex = 77;
            this.label1.Text = "Суточные/Месячные";
            // 
            // dtpDailyMonthly
            // 
            this.dtpDailyMonthly.Location = new System.Drawing.Point(5, 27);
            this.dtpDailyMonthly.Margin = new System.Windows.Forms.Padding(4);
            this.dtpDailyMonthly.Name = "dtpDailyMonthly";
            this.dtpDailyMonthly.Size = new System.Drawing.Size(149, 22);
            this.dtpDailyMonthly.TabIndex = 76;
            this.dtpDailyMonthly.Value = new System.DateTime(2017, 10, 30, 0, 0, 0, 0);
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(492, 14);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(418, 231);
            this.rtbLog.TabIndex = 77;
            this.rtbLog.Text = "";
            // 
            // panelHalfs
            // 
            this.panelHalfs.Controls.Add(this.dtpTo);
            this.panelHalfs.Controls.Add(this.label9);
            this.panelHalfs.Controls.Add(this.label8);
            this.panelHalfs.Controls.Add(this.dtpFrom);
            this.panelHalfs.Controls.Add(this.cbAltHalfsMethod);
            this.panelHalfs.Controls.Add(this.btnReadHalfs);
            this.panelHalfs.Location = new System.Drawing.Point(172, 69);
            this.panelHalfs.Name = "panelHalfs";
            this.panelHalfs.Size = new System.Drawing.Size(314, 91);
            this.panelHalfs.TabIndex = 76;
            // 
            // dtpTo
            // 
            this.dtpTo.Location = new System.Drawing.Point(160, 27);
            this.dtpTo.Margin = new System.Windows.Forms.Padding(4);
            this.dtpTo.Name = "dtpTo";
            this.dtpTo.Size = new System.Drawing.Size(144, 22);
            this.dtpTo.TabIndex = 77;
            this.dtpTo.Value = new System.DateTime(2017, 10, 30, 23, 30, 0, 0);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(157, 6);
            this.label9.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(24, 17);
            this.label9.TabIndex = 76;
            this.label9.Text = "по";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(4, 6);
            this.label8.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(108, 17);
            this.label8.TabIndex = 75;
            this.label8.Text = "Получасовые с";
            // 
            // dtpFrom
            // 
            this.dtpFrom.Location = new System.Drawing.Point(7, 27);
            this.dtpFrom.Margin = new System.Windows.Forms.Padding(4);
            this.dtpFrom.Name = "dtpFrom";
            this.dtpFrom.Size = new System.Drawing.Size(144, 22);
            this.dtpFrom.TabIndex = 74;
            this.dtpFrom.Value = new System.DateTime(2017, 10, 30, 0, 0, 0, 0);
            // 
            // cbAltHalfsMethod
            // 
            this.cbAltHalfsMethod.AutoSize = true;
            this.cbAltHalfsMethod.Enabled = false;
            this.cbAltHalfsMethod.Location = new System.Drawing.Point(7, 62);
            this.cbAltHalfsMethod.Margin = new System.Windows.Forms.Padding(4);
            this.cbAltHalfsMethod.Name = "cbAltHalfsMethod";
            this.cbAltHalfsMethod.Size = new System.Drawing.Size(124, 21);
            this.cbAltHalfsMethod.TabIndex = 73;
            this.cbAltHalfsMethod.Text = "Старый метод";
            this.cbAltHalfsMethod.UseVisualStyleBackColor = true;
            this.cbAltHalfsMethod.Visible = false;
            // 
            // btnReadHalfs
            // 
            this.btnReadHalfs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReadHalfs.Location = new System.Drawing.Point(232, 55);
            this.btnReadHalfs.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.btnReadHalfs.Name = "btnReadHalfs";
            this.btnReadHalfs.Size = new System.Drawing.Size(72, 28);
            this.btnReadHalfs.TabIndex = 72;
            this.btnReadHalfs.Text = "ПЧ";
            this.toolTip1.SetToolTip(this.btnReadHalfs, "Чтение получасовых значений");
            this.btnReadHalfs.UseVisualStyleBackColor = true;
            this.btnReadHalfs.Click += new System.EventHandler(this.btnReadHalfs_Click);
            // 
            // ctlMeters
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "ctlMeters";
            this.Size = new System.Drawing.Size(922, 268);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbPreloader)).EndInit();
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numParamTarif)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numParamAddr)).EndInit();
            this.gbAuxilary.ResumeLayout(false);
            this.gbAuxilary.PerformLayout();
            this.panelDailyMonthly.ResumeLayout(false);
            this.panelDailyMonthly.PerformLayout();
            this.panelHalfs.ResumeLayout(false);
            this.panelHalfs.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Panel panelHalfs;
        private System.Windows.Forms.DateTimePicker dtpTo;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.DateTimePicker dtpFrom;
        private System.Windows.Forms.CheckBox cbAltHalfsMethod;
        private System.Windows.Forms.Button btnReadHalfs;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Panel panelDailyMonthly;
        private System.Windows.Forms.Button btnReadMonthly;
        private System.Windows.Forms.Button btnReadDaily;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtpDailyMonthly;
        private System.Windows.Forms.GroupBox gbAuxilary;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbMeterPassword;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numParamTarif;
        private System.Windows.Forms.Button btnReadInfo;
        private System.Windows.Forms.Button btnReadCurrent;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numParamAddr;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbMeterAddress;
        private System.Windows.Forms.PictureBox pbPreloader;
    }
}
