﻿namespace Prizmer.PoolServer
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBox3 = new System.Windows.Forms.ComboBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.lblCnt = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.tbPort = new System.Windows.Forms.TextBox();
            this.lblCurCnt = new System.Windows.Forms.Label();
            this.tbAddress = new System.Windows.Forms.TextBox();
            this.rbTCP = new System.Windows.Forms.RadioButton();
            this.rbCom = new System.Windows.Forms.RadioButton();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.label3 = new System.Windows.Forms.Label();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.btnEndReading = new System.Windows.Forms.Button();
            this.btnStartReading = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.cbServerStarted = new System.Windows.Forms.CheckBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ctxMenuAnalizator = new System.Windows.Forms.ToolStripMenuItem();
            this.конфигураторToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.ctxMenuShowLogsDir = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuDeleteLogs = new System.Windows.Forms.ToolStripMenuItem();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pbPreloader = new System.Windows.Forms.PictureBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tsLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.DeviceSearchMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbPreloader)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboBox3);
            this.groupBox1.Controls.Add(this.linkLabel1);
            this.groupBox1.Controls.Add(this.comboBox2);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.lblCnt);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.tbPort);
            this.groupBox1.Controls.Add(this.lblCurCnt);
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
            this.groupBox1.Location = new System.Drawing.Point(0, 139);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.groupBox1.Size = new System.Drawing.Size(365, 292);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Дочитка показаний";
            // 
            // comboBox3
            // 
            this.comboBox3.FormattingEnabled = true;
            this.comboBox3.Location = new System.Drawing.Point(109, 192);
            this.comboBox3.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox3.Name = "comboBox3";
            this.comboBox3.Size = new System.Drawing.Size(241, 24);
            this.comboBox3.TabIndex = 17;
            this.comboBox3.SelectedIndexChanged += new System.EventHandler(this.comboBox3_SelectedIndexChanged);
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(147, 39);
            this.linkLabel1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(49, 17);
            this.linkLabel1.TabIndex = 16;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Ранее";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // comboBox2
            // 
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Items.AddRange(new object[] {
            "Текущий",
            "Суточный",
            "Месячный",
            "Архивный",
            "Получасовой"});
            this.comboBox2.Location = new System.Drawing.Point(207, 113);
            this.comboBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(144, 24);
            this.comboBox2.TabIndex = 15;
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 117);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 17);
            this.label4.TabIndex = 14;
            this.label4.Text = "Параметр:";
            // 
            // lblCnt
            // 
            this.lblCnt.AutoSize = true;
            this.lblCnt.BackColor = System.Drawing.Color.Transparent;
            this.lblCnt.Location = new System.Drawing.Point(145, 260);
            this.lblCnt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCnt.Name = "lblCnt";
            this.lblCnt.Size = new System.Drawing.Size(16, 17);
            this.lblCnt.TabIndex = 13;
            this.lblCnt.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Location = new System.Drawing.Point(73, 260);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(23, 17);
            this.label5.TabIndex = 4;
            this.label5.Text = "из";
            // 
            // tbPort
            // 
            this.tbPort.Location = new System.Drawing.Point(267, 166);
            this.tbPort.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbPort.Name = "tbPort";
            this.tbPort.Size = new System.Drawing.Size(84, 22);
            this.tbPort.TabIndex = 12;
            this.tbPort.Text = "5001";
            // 
            // lblCurCnt
            // 
            this.lblCurCnt.AutoSize = true;
            this.lblCurCnt.BackColor = System.Drawing.Color.Transparent;
            this.lblCurCnt.Location = new System.Drawing.Point(17, 260);
            this.lblCurCnt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCurCnt.Name = "lblCurCnt";
            this.lblCurCnt.Size = new System.Drawing.Size(16, 17);
            this.lblCurCnt.TabIndex = 3;
            this.lblCurCnt.Text = "0";
            // 
            // tbAddress
            // 
            this.tbAddress.Location = new System.Drawing.Point(109, 166);
            this.tbAddress.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tbAddress.Name = "tbAddress";
            this.tbAddress.Size = new System.Drawing.Size(148, 22);
            this.tbAddress.TabIndex = 11;
            this.tbAddress.Text = "192.168.23.52";
            // 
            // rbTCP
            // 
            this.rbTCP.AutoSize = true;
            this.rbTCP.Checked = true;
            this.rbTCP.Location = new System.Drawing.Point(20, 193);
            this.rbTCP.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.rbTCP.Name = "rbTCP";
            this.rbTCP.Size = new System.Drawing.Size(56, 21);
            this.rbTCP.TabIndex = 10;
            this.rbTCP.TabStop = true;
            this.rbTCP.Tag = "tcp";
            this.rbTCP.Text = "TCP";
            this.rbTCP.UseVisualStyleBackColor = true;
            this.rbTCP.CheckedChanged += new System.EventHandler(this.rbCom_CheckedChanged);
            // 
            // rbCom
            // 
            this.rbCom.AutoSize = true;
            this.rbCom.Location = new System.Drawing.Point(20, 170);
            this.rbCom.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.rbCom.Name = "rbCom";
            this.rbCom.Size = new System.Drawing.Size(60, 21);
            this.rbCom.TabIndex = 9;
            this.rbCom.Tag = "com";
            this.rbCom.Text = "COM";
            this.rbCom.UseVisualStyleBackColor = true;
            this.rbCom.CheckedChanged += new System.EventHandler(this.rbCom_CheckedChanged);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(20, 228);
            this.progressBar1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(205, 28);
            this.progressBar1.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 82);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 17);
            this.label3.TabIndex = 7;
            this.label3.Text = "Драйвер:";
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker2.Location = new System.Drawing.Point(207, 53);
            this.dateTimePicker2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(144, 22);
            this.dateTimePicker2.TabIndex = 6;
            this.dateTimePicker2.Value = new System.DateTime(2017, 3, 2, 0, 0, 0, 0);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 53);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Конец:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 23);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "Начало:";
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker1.Location = new System.Drawing.Point(207, 23);
            this.dateTimePicker1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(144, 22);
            this.dateTimePicker1.TabIndex = 3;
            this.dateTimePicker1.Value = new System.DateTime(2017, 3, 2, 0, 0, 0, 0);
            // 
            // btnEndReading
            // 
            this.btnEndReading.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEndReading.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEndReading.Location = new System.Drawing.Point(319, 226);
            this.btnEndReading.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnEndReading.Name = "btnEndReading";
            this.btnEndReading.Size = new System.Drawing.Size(33, 30);
            this.btnEndReading.TabIndex = 2;
            this.btnEndReading.Text = "X";
            this.btnEndReading.UseVisualStyleBackColor = true;
            this.btnEndReading.Click += new System.EventHandler(this.btnEndReading_Click);
            // 
            // btnStartReading
            // 
            this.btnStartReading.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStartReading.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStartReading.Location = new System.Drawing.Point(237, 226);
            this.btnStartReading.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnStartReading.Name = "btnStartReading";
            this.btnStartReading.Size = new System.Drawing.Size(83, 30);
            this.btnStartReading.TabIndex = 1;
            this.btnStartReading.Text = "Старт";
            this.btnStartReading.UseVisualStyleBackColor = true;
            this.btnStartReading.Click += new System.EventHandler(this.btnStartReading_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "m230"});
            this.comboBox1.Location = new System.Drawing.Point(207, 82);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(144, 24);
            this.comboBox1.TabIndex = 0;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // cbServerStarted
            // 
            this.cbServerStarted.AutoSize = true;
            this.cbServerStarted.Location = new System.Drawing.Point(15, 111);
            this.cbServerStarted.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cbServerStarted.Name = "cbServerStarted";
            this.cbServerStarted.Size = new System.Drawing.Size(139, 21);
            this.cbServerStarted.TabIndex = 2;
            this.cbServerStarted.Text = "Сервер запущен";
            this.cbServerStarted.UseVisualStyleBackColor = true;
            this.cbServerStarted.CheckedChanged += new System.EventHandler(this.cbServerStarted_CheckedChanged);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxMenuAnalizator,
            this.конфигураторToolStripMenuItem,
            this.DeviceSearchMenuItem,
            this.toolStripMenuItem1,
            this.ctxMenuShowLogsDir,
            this.ctxMenuDeleteLogs});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(256, 168);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening_1);
            // 
            // ctxMenuAnalizator
            // 
            this.ctxMenuAnalizator.Image = ((System.Drawing.Image)(resources.GetObject("ctxMenuAnalizator.Image")));
            this.ctxMenuAnalizator.Name = "ctxMenuAnalizator";
            this.ctxMenuAnalizator.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
            this.ctxMenuAnalizator.Size = new System.Drawing.Size(255, 26);
            this.ctxMenuAnalizator.Text = "Анализатор";
            this.ctxMenuAnalizator.Click += new System.EventHandler(this.ctxMenuAnalizator_Click);
            // 
            // конфигураторToolStripMenuItem
            // 
            this.конфигураторToolStripMenuItem.Name = "конфигураторToolStripMenuItem";
            this.конфигураторToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.K)));
            this.конфигураторToolStripMenuItem.Size = new System.Drawing.Size(255, 26);
            this.конфигураторToolStripMenuItem.Text = "Конфигуратор";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(252, 6);
            // 
            // ctxMenuShowLogsDir
            // 
            this.ctxMenuShowLogsDir.Name = "ctxMenuShowLogsDir";
            this.ctxMenuShowLogsDir.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.L)));
            this.ctxMenuShowLogsDir.Size = new System.Drawing.Size(255, 26);
            this.ctxMenuShowLogsDir.Text = "Открыть логи";
            this.ctxMenuShowLogsDir.Click += new System.EventHandler(this.ctxMenuShowLogsDir_Click);
            // 
            // ctxMenuDeleteLogs
            // 
            this.ctxMenuDeleteLogs.Name = "ctxMenuDeleteLogs";
            this.ctxMenuDeleteLogs.Size = new System.Drawing.Size(255, 26);
            this.ctxMenuDeleteLogs.Text = "Стереть логи";
            this.ctxMenuDeleteLogs.Click += new System.EventHandler(this.ctxMenuDeleteLogs_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.ContextMenuStrip = this.contextMenuStrip1;
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pictureBox1.Image = global::PoolServer.Properties.Resources.logo2;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(369, 101);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // pbPreloader
            // 
            this.pbPreloader.Image = ((System.Drawing.Image)(resources.GetObject("pbPreloader.Image")));
            this.pbPreloader.Location = new System.Drawing.Point(333, 105);
            this.pbPreloader.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.pbPreloader.Name = "pbPreloader";
            this.pbPreloader.Size = new System.Drawing.Size(32, 30);
            this.pbPreloader.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbPreloader.TabIndex = 18;
            this.pbPreloader.TabStop = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 447);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 19, 0);
            this.statusStrip1.Size = new System.Drawing.Size(369, 22);
            this.statusStrip1.TabIndex = 19;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tsLabel1
            // 
            this.tsLabel1.Name = "tsLabel1";
            this.tsLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // DeviceSearchMenuItem
            // 
            this.DeviceSearchMenuItem.Name = "DeviceSearchMenuItem";
            this.DeviceSearchMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
            this.DeviceSearchMenuItem.Size = new System.Drawing.Size(255, 26);
            this.DeviceSearchMenuItem.Text = "Поиск приборов";
            this.DeviceSearchMenuItem.Click += new System.EventHandler(this.DeviceSearchMenuItem_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(369, 469);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.pbPreloader);
            this.Controls.Add(this.cbServerStarted);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.ShowIcon = false;
            this.Text = "ПИ - Сервер опроса";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbPreloader)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
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
        private System.Windows.Forms.Label lblCnt;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblCurCnt;
        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.ComboBox comboBox3;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ctxMenuAnalizator;
        private System.Windows.Forms.PictureBox pbPreloader;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tsLabel1;
        private System.Windows.Forms.ToolStripMenuItem конфигураторToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem ctxMenuShowLogsDir;
        private System.Windows.Forms.ToolStripMenuItem ctxMenuDeleteLogs;
        private System.Windows.Forms.ToolStripMenuItem DeviceSearchMenuItem;
    }
}

