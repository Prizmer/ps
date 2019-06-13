namespace PollingLibraries.LibPorts
{
    partial class CtlConnectionSettings
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.gbComProp = new System.Windows.Forms.GroupBox();
            this.tbComConfig = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panelCOMGSM = new System.Windows.Forms.Panel();
            this.tbGSMInit = new System.Windows.Forms.TextBox();
            this.tbGSMPhone = new System.Windows.Forms.TextBox();
            this.cbUseGSM = new System.Windows.Forms.CheckBox();
            this.btnApplyConnectionSettings = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.rbCom = new System.Windows.Forms.RadioButton();
            this.rbTcp = new System.Windows.Forms.RadioButton();
            this.label4 = new System.Windows.Forms.Label();
            this.numericUpDownComWriteTimeout = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDownComReadTimeout = new System.Windows.Forms.NumericUpDown();
            this.comboBoxComPorts = new System.Windows.Forms.ComboBox();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.textBoxIp = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.gbComProp.SuspendLayout();
            this.panelCOMGSM.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownComWriteTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownComReadTimeout)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblStatus);
            this.groupBox1.Controls.Add(this.gbComProp);
            this.groupBox1.Controls.Add(this.btnApplyConnectionSettings);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.rbCom);
            this.groupBox1.Controls.Add(this.rbTcp);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.numericUpDownComWriteTimeout);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.numericUpDownComReadTimeout);
            this.groupBox1.Controls.Add(this.comboBoxComPorts);
            this.groupBox1.Controls.Add(this.textBoxPort);
            this.groupBox1.Controls.Add(this.textBoxIp);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(285, 390);
            this.groupBox1.TabIndex = 87;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Подключение";
            // 
            // gbComProp
            // 
            this.gbComProp.Controls.Add(this.tbComConfig);
            this.gbComProp.Controls.Add(this.label2);
            this.gbComProp.Controls.Add(this.panelCOMGSM);
            this.gbComProp.Location = new System.Drawing.Point(15, 87);
            this.gbComProp.Name = "gbComProp";
            this.gbComProp.Size = new System.Drawing.Size(251, 155);
            this.gbComProp.TabIndex = 99;
            this.gbComProp.TabStop = false;
            this.gbComProp.Text = "COM свойства";
            // 
            // tbComConfig
            // 
            this.tbComConfig.Enabled = false;
            this.tbComConfig.Location = new System.Drawing.Point(11, 115);
            this.tbComConfig.Name = "tbComConfig";
            this.tbComConfig.Size = new System.Drawing.Size(227, 22);
            this.tbComConfig.TabIndex = 90;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 95);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(127, 17);
            this.label2.TabIndex = 89;
            this.label2.Text = "Из конфигурации:";
            // 
            // panelCOMGSM
            // 
            this.panelCOMGSM.Controls.Add(this.tbGSMInit);
            this.panelCOMGSM.Controls.Add(this.tbGSMPhone);
            this.panelCOMGSM.Controls.Add(this.cbUseGSM);
            this.panelCOMGSM.Location = new System.Drawing.Point(6, 21);
            this.panelCOMGSM.Name = "panelCOMGSM";
            this.panelCOMGSM.Size = new System.Drawing.Size(242, 62);
            this.panelCOMGSM.TabIndex = 88;
            // 
            // tbGSMInit
            // 
            this.tbGSMInit.Location = new System.Drawing.Point(6, 34);
            this.tbGSMInit.Name = "tbGSMInit";
            this.tbGSMInit.Size = new System.Drawing.Size(227, 22);
            this.tbGSMInit.TabIndex = 90;
            // 
            // tbGSMPhone
            // 
            this.tbGSMPhone.Location = new System.Drawing.Point(75, 6);
            this.tbGSMPhone.Name = "tbGSMPhone";
            this.tbGSMPhone.Size = new System.Drawing.Size(158, 22);
            this.tbGSMPhone.TabIndex = 89;
            // 
            // cbUseGSM
            // 
            this.cbUseGSM.AutoSize = true;
            this.cbUseGSM.Location = new System.Drawing.Point(6, 7);
            this.cbUseGSM.Name = "cbUseGSM";
            this.cbUseGSM.Size = new System.Drawing.Size(61, 21);
            this.cbUseGSM.TabIndex = 88;
            this.cbUseGSM.Text = "GSM";
            this.cbUseGSM.UseVisualStyleBackColor = true;
            this.cbUseGSM.CheckedChanged += new System.EventHandler(this.cbUseGSM_CheckedChanged);
            // 
            // btnApplyConnectionSettings
            // 
            this.btnApplyConnectionSettings.Location = new System.Drawing.Point(15, 319);
            this.btnApplyConnectionSettings.Name = "btnApplyConnectionSettings";
            this.btnApplyConnectionSettings.Size = new System.Drawing.Size(251, 35);
            this.btnApplyConnectionSettings.TabIndex = 98;
            this.btnApplyConnectionSettings.Text = "Применить";
            this.btnApplyConnectionSettings.UseVisualStyleBackColor = true;
            this.btnApplyConnectionSettings.Click += new System.EventHandler(this.btnApplyConnectionSettings_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(212, 288);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(34, 17);
            this.label7.TabIndex = 97;
            this.label7.Text = "(мс)";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 288);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(119, 17);
            this.label6.TabIndex = 96;
            this.label6.Text = "Таймаут чтение:";
            // 
            // rbCom
            // 
            this.rbCom.AutoSize = true;
            this.rbCom.Location = new System.Drawing.Point(15, 56);
            this.rbCom.Name = "rbCom";
            this.rbCom.Size = new System.Drawing.Size(60, 21);
            this.rbCom.TabIndex = 95;
            this.rbCom.TabStop = true;
            this.rbCom.Tag = "com";
            this.rbCom.Text = "COM";
            this.rbCom.UseVisualStyleBackColor = true;
            this.rbCom.CheckedChanged += new System.EventHandler(this.rbTcp_CheckedChanged);
            // 
            // rbTcp
            // 
            this.rbTcp.AutoSize = true;
            this.rbTcp.Location = new System.Drawing.Point(15, 30);
            this.rbTcp.Name = "rbTcp";
            this.rbTcp.Size = new System.Drawing.Size(56, 21);
            this.rbTcp.TabIndex = 94;
            this.rbTcp.TabStop = true;
            this.rbTcp.Tag = "tcp";
            this.rbTcp.Text = "TCP";
            this.rbTcp.UseVisualStyleBackColor = true;
            this.rbTcp.CheckedChanged += new System.EventHandler(this.rbTcp_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 258);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(121, 17);
            this.label4.TabIndex = 93;
            this.label4.Text = "Таймаут запись: ";
            // 
            // numericUpDownComWriteTimeout
            // 
            this.numericUpDownComWriteTimeout.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.numericUpDownComWriteTimeout.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownComWriteTimeout.Location = new System.Drawing.Point(139, 258);
            this.numericUpDownComWriteTimeout.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numericUpDownComWriteTimeout.Maximum = new decimal(new int[] {
            1800,
            0,
            0,
            0});
            this.numericUpDownComWriteTimeout.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDownComWriteTimeout.Name = "numericUpDownComWriteTimeout";
            this.numericUpDownComWriteTimeout.Size = new System.Drawing.Size(61, 18);
            this.numericUpDownComWriteTimeout.TabIndex = 92;
            this.numericUpDownComWriteTimeout.Value = new decimal(new int[] {
            500,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(212, 257);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 17);
            this.label1.TabIndex = 91;
            this.label1.Text = "(мс)";
            // 
            // numericUpDownComReadTimeout
            // 
            this.numericUpDownComReadTimeout.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.numericUpDownComReadTimeout.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownComReadTimeout.Location = new System.Drawing.Point(139, 287);
            this.numericUpDownComReadTimeout.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.numericUpDownComReadTimeout.Maximum = new decimal(new int[] {
            1800,
            0,
            0,
            0});
            this.numericUpDownComReadTimeout.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numericUpDownComReadTimeout.Name = "numericUpDownComReadTimeout";
            this.numericUpDownComReadTimeout.Size = new System.Drawing.Size(61, 18);
            this.numericUpDownComReadTimeout.TabIndex = 90;
            this.numericUpDownComReadTimeout.Value = new decimal(new int[] {
            1200,
            0,
            0,
            0});
            // 
            // comboBoxComPorts
            // 
            this.comboBoxComPorts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.comboBoxComPorts.FormattingEnabled = true;
            this.comboBoxComPorts.Location = new System.Drawing.Point(105, 55);
            this.comboBoxComPorts.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.comboBoxComPorts.Name = "comboBoxComPorts";
            this.comboBoxComPorts.Size = new System.Drawing.Size(161, 24);
            this.comboBoxComPorts.TabIndex = 89;
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(210, 29);
            this.textBoxPort.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(56, 22);
            this.textBoxPort.TabIndex = 88;
            // 
            // textBoxIp
            // 
            this.textBoxIp.Location = new System.Drawing.Point(105, 29);
            this.textBoxIp.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxIp.Name = "textBoxIp";
            this.textBoxIp.Size = new System.Drawing.Size(99, 22);
            this.textBoxIp.TabIndex = 87;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblStatus.Location = new System.Drawing.Point(12, 367);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(28, 17);
            this.lblStatus.TabIndex = 101;
            this.lblStatus.Text = "OK";
            // 
            // CtlConnectionSettings
            // 
            this.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "CtlConnectionSettings";
            this.Size = new System.Drawing.Size(292, 396);
            this.Load += new System.EventHandler(this.ctlConnectionSettings_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbComProp.ResumeLayout(false);
            this.gbComProp.PerformLayout();
            this.panelCOMGSM.ResumeLayout(false);
            this.panelCOMGSM.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownComWriteTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownComReadTimeout)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.GroupBox gbComProp;
        private System.Windows.Forms.TextBox tbComConfig;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Panel panelCOMGSM;
        private System.Windows.Forms.TextBox tbGSMInit;
        private System.Windows.Forms.TextBox tbGSMPhone;
        private System.Windows.Forms.CheckBox cbUseGSM;
        private System.Windows.Forms.Button btnApplyConnectionSettings;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RadioButton rbCom;
        private System.Windows.Forms.RadioButton rbTcp;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown numericUpDownComWriteTimeout;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDownComReadTimeout;
        private System.Windows.Forms.ComboBox comboBoxComPorts;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.TextBox textBoxIp;
    }
}
