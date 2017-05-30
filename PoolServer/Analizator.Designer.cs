namespace Prizmer.PoolServer
{
    partial class Analizator
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Analizator));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbClosedThreads = new System.Windows.Forms.ListBox();
            this.lbActiveThreads = new System.Windows.Forms.ListBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnCaptureData = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.rtbControlData = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.rbPortStatusClosed = new System.Windows.Forms.RadioButton();
            this.rbPortStatusOpened = new System.Windows.Forms.RadioButton();
            this.tbPortName = new System.Windows.Forms.TextBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.toolTipThread = new System.Windows.Forms.ToolTip(this.components);
            this.rtbThreadInfo = new System.Windows.Forms.RichTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbClosedThreads);
            this.groupBox1.Controls.Add(this.lbActiveThreads);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(423, 397);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Потоки (активные/завершенные)";
            // 
            // lbClosedThreads
            // 
            this.lbClosedThreads.FormattingEnabled = true;
            this.lbClosedThreads.Location = new System.Drawing.Point(211, 19);
            this.lbClosedThreads.Name = "lbClosedThreads";
            this.lbClosedThreads.Size = new System.Drawing.Size(199, 368);
            this.lbClosedThreads.TabIndex = 2;
            this.lbClosedThreads.SelectedIndexChanged += new System.EventHandler(this.lbClosedThreads_SelectedIndexChanged);
            // 
            // lbActiveThreads
            // 
            this.lbActiveThreads.FormattingEnabled = true;
            this.lbActiveThreads.Location = new System.Drawing.Point(6, 19);
            this.lbActiveThreads.Name = "lbActiveThreads";
            this.lbActiveThreads.Size = new System.Drawing.Size(199, 368);
            this.lbActiveThreads.TabIndex = 1;
            this.lbActiveThreads.SelectedIndexChanged += new System.EventHandler(this.lbActiveThreads_SelectedIndexChanged);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 419);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1092, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnCaptureData);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.rtbControlData);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.rbPortStatusClosed);
            this.groupBox2.Controls.Add(this.rbPortStatusOpened);
            this.groupBox2.Controls.Add(this.tbPortName);
            this.groupBox2.Location = new System.Drawing.Point(755, 19);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(332, 390);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Состояние порта потока";
            // 
            // btnCaptureData
            // 
            this.btnCaptureData.Location = new System.Drawing.Point(70, 80);
            this.btnCaptureData.Name = "btnCaptureData";
            this.btnCaptureData.Size = new System.Drawing.Size(75, 23);
            this.btnCaptureData.TabIndex = 7;
            this.btnCaptureData.Text = "Фиксация";
            this.btnCaptureData.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 85);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Контроль:";
            // 
            // rtbControlData
            // 
            this.rtbControlData.Location = new System.Drawing.Point(11, 109);
            this.rtbControlData.Name = "rtbControlData";
            this.rtbControlData.ReadOnly = true;
            this.rtbControlData.Size = new System.Drawing.Size(312, 271);
            this.rtbControlData.TabIndex = 5;
            this.rtbControlData.Text = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(181, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Принято (байт):";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 54);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(92, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Передано (байт):";
            // 
            // rbPortStatusClosed
            // 
            this.rbPortStatusClosed.AutoSize = true;
            this.rbPortStatusClosed.Enabled = false;
            this.rbPortStatusClosed.Location = new System.Drawing.Point(78, 22);
            this.rbPortStatusClosed.Name = "rbPortStatusClosed";
            this.rbPortStatusClosed.Size = new System.Drawing.Size(63, 17);
            this.rbPortStatusClosed.TabIndex = 2;
            this.rbPortStatusClosed.TabStop = true;
            this.rbPortStatusClosed.Text = "Закрыт";
            this.rbPortStatusClosed.UseVisualStyleBackColor = true;
            // 
            // rbPortStatusOpened
            // 
            this.rbPortStatusOpened.AutoSize = true;
            this.rbPortStatusOpened.Enabled = false;
            this.rbPortStatusOpened.Location = new System.Drawing.Point(9, 22);
            this.rbPortStatusOpened.Name = "rbPortStatusOpened";
            this.rbPortStatusOpened.Size = new System.Drawing.Size(63, 17);
            this.rbPortStatusOpened.TabIndex = 1;
            this.rbPortStatusOpened.TabStop = true;
            this.rbPortStatusOpened.Text = "Открыт";
            this.rbPortStatusOpened.UseVisualStyleBackColor = true;
            // 
            // tbPortName
            // 
            this.tbPortName.Location = new System.Drawing.Point(147, 19);
            this.tbPortName.Name = "tbPortName";
            this.tbPortName.ReadOnly = true;
            this.tbPortName.Size = new System.Drawing.Size(176, 20);
            this.tbPortName.TabIndex = 0;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 500;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // rtbThreadInfo
            // 
            this.rtbThreadInfo.Location = new System.Drawing.Point(441, 31);
            this.rtbThreadInfo.Name = "rtbThreadInfo";
            this.rtbThreadInfo.ReadOnly = true;
            this.rtbThreadInfo.Size = new System.Drawing.Size(308, 378);
            this.rtbThreadInfo.TabIndex = 6;
            this.rtbThreadInfo.Text = "";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(441, 15);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "О потоке:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(616, 2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 8;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Analizator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1092, 441);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.rtbThreadInfo);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.Name = "Analizator";
            this.Text = "Анализатор";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Analizator_FormClosing);
            this.Load += new System.EventHandler(this.Analizator_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox lbClosedThreads;
        private System.Windows.Forms.ListBox lbActiveThreads;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnCaptureData;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox rtbControlData;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rbPortStatusClosed;
        private System.Windows.Forms.RadioButton rbPortStatusOpened;
        private System.Windows.Forms.TextBox tbPortName;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolTip toolTipThread;
        private System.Windows.Forms.RichTextBox rtbThreadInfo;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
    }
}