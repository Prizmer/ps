namespace Prizmer.PoolServer
{
    partial class ScheduleForm
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
            this.PortsListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.PollOnScheduleCheckBox = new System.Windows.Forms.CheckBox();
            this.PollCalendar = new System.Windows.Forms.MonthCalendar();
            this.PollDaysListBox = new System.Windows.Forms.ListBox();
            this.PollAMCheckBox = new System.Windows.Forms.CheckBox();
            this.PollPMCheckBox = new System.Windows.Forms.CheckBox();
            this.ShowTCPIPRadioButton = new System.Windows.Forms.RadioButton();
            this.ShowComRadioButton = new System.Windows.Forms.RadioButton();
            this.AddDayButton = new System.Windows.Forms.Button();
            this.DeleteDayButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // PortsListBox
            // 
            this.PortsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.PortsListBox.FormattingEnabled = true;
            this.PortsListBox.ItemHeight = 16;
            this.PortsListBox.Location = new System.Drawing.Point(12, 28);
            this.PortsListBox.Name = "PortsListBox";
            this.PortsListBox.Size = new System.Drawing.Size(168, 244);
            this.PortsListBox.TabIndex = 0;
            this.PortsListBox.SelectedIndexChanged += new System.EventHandler(this.PortsListBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Список портов:";
            // 
            // PollOnScheduleCheckBox
            // 
            this.PollOnScheduleCheckBox.AutoSize = true;
            this.PollOnScheduleCheckBox.Location = new System.Drawing.Point(186, 5);
            this.PollOnScheduleCheckBox.Name = "PollOnScheduleCheckBox";
            this.PollOnScheduleCheckBox.Size = new System.Drawing.Size(176, 21);
            this.PollOnScheduleCheckBox.TabIndex = 3;
            this.PollOnScheduleCheckBox.Text = "Опрос по расписанию";
            this.PollOnScheduleCheckBox.UseVisualStyleBackColor = true;
            this.PollOnScheduleCheckBox.CheckedChanged += new System.EventHandler(this.PollOnScheduleCheckBox_CheckedChanged);
            // 
            // PollCalendar
            // 
            this.PollCalendar.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PollCalendar.Location = new System.Drawing.Point(374, 5);
            this.PollCalendar.MaxSelectionCount = 365;
            this.PollCalendar.Name = "PollCalendar";
            this.PollCalendar.TabIndex = 6;
            // 
            // PollDaysListBox
            // 
            this.PollDaysListBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.PollDaysListBox.FormattingEnabled = true;
            this.PollDaysListBox.ItemHeight = 16;
            this.PollDaysListBox.Location = new System.Drawing.Point(187, 29);
            this.PollDaysListBox.Name = "PollDaysListBox";
            this.PollDaysListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.PollDaysListBox.Size = new System.Drawing.Size(176, 244);
            this.PollDaysListBox.TabIndex = 4;
            this.PollDaysListBox.SelectedIndexChanged += new System.EventHandler(this.PollDaysListBox_SelectedIndexChanged);
            // 
            // PollAMCheckBox
            // 
            this.PollAMCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PollAMCheckBox.AutoSize = true;
            this.PollAMCheckBox.Checked = true;
            this.PollAMCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.PollAMCheckBox.Location = new System.Drawing.Point(374, 259);
            this.PollAMCheckBox.Name = "PollAMCheckBox";
            this.PollAMCheckBox.Size = new System.Drawing.Size(112, 21);
            this.PollAMCheckBox.TabIndex = 8;
            this.PollAMCheckBox.Text = "Опрос до 12";
            this.PollAMCheckBox.UseVisualStyleBackColor = true;
            this.PollAMCheckBox.CheckedChanged += new System.EventHandler(this.PollAMCheckBox_CheckedChanged);
            // 
            // PollPMCheckBox
            // 
            this.PollPMCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PollPMCheckBox.AutoSize = true;
            this.PollPMCheckBox.Checked = true;
            this.PollPMCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.PollPMCheckBox.Location = new System.Drawing.Point(374, 286);
            this.PollPMCheckBox.Name = "PollPMCheckBox";
            this.PollPMCheckBox.Size = new System.Drawing.Size(135, 21);
            this.PollPMCheckBox.TabIndex = 9;
            this.PollPMCheckBox.Text = "Опрос после 12";
            this.PollPMCheckBox.UseVisualStyleBackColor = true;
            this.PollPMCheckBox.CheckedChanged += new System.EventHandler(this.PollPMCheckBox_CheckedChanged);
            // 
            // ShowTCPIPRadioButton
            // 
            this.ShowTCPIPRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ShowTCPIPRadioButton.AutoSize = true;
            this.ShowTCPIPRadioButton.Checked = true;
            this.ShowTCPIPRadioButton.Location = new System.Drawing.Point(12, 280);
            this.ShowTCPIPRadioButton.Name = "ShowTCPIPRadioButton";
            this.ShowTCPIPRadioButton.Size = new System.Drawing.Size(72, 21);
            this.ShowTCPIPRadioButton.TabIndex = 1;
            this.ShowTCPIPRadioButton.TabStop = true;
            this.ShowTCPIPRadioButton.Text = "TCP/IP";
            this.ShowTCPIPRadioButton.UseVisualStyleBackColor = true;
            this.ShowTCPIPRadioButton.CheckedChanged += new System.EventHandler(this.ShowTCPIPRadioButton_CheckedChanged);
            // 
            // ShowComRadioButton
            // 
            this.ShowComRadioButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ShowComRadioButton.AutoSize = true;
            this.ShowComRadioButton.Location = new System.Drawing.Point(123, 280);
            this.ShowComRadioButton.Name = "ShowComRadioButton";
            this.ShowComRadioButton.Size = new System.Drawing.Size(57, 21);
            this.ShowComRadioButton.TabIndex = 2;
            this.ShowComRadioButton.Text = "Com";
            this.ShowComRadioButton.UseVisualStyleBackColor = true;
            // 
            // AddDayButton
            // 
            this.AddDayButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AddDayButton.Enabled = false;
            this.AddDayButton.Location = new System.Drawing.Point(374, 224);
            this.AddDayButton.Name = "AddDayButton";
            this.AddDayButton.Size = new System.Drawing.Size(215, 29);
            this.AddDayButton.TabIndex = 7;
            this.AddDayButton.Text = "Добавить день";
            this.AddDayButton.UseVisualStyleBackColor = true;
            this.AddDayButton.Click += new System.EventHandler(this.AddDayButton_Click);
            // 
            // DeleteDayButton
            // 
            this.DeleteDayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DeleteDayButton.Enabled = false;
            this.DeleteDayButton.Location = new System.Drawing.Point(187, 280);
            this.DeleteDayButton.Name = "DeleteDayButton";
            this.DeleteDayButton.Size = new System.Drawing.Size(175, 29);
            this.DeleteDayButton.TabIndex = 5;
            this.DeleteDayButton.Text = "Удалить день";
            this.DeleteDayButton.UseVisualStyleBackColor = true;
            this.DeleteDayButton.Click += new System.EventHandler(this.DeleteDayButton_Click);
            // 
            // ScheduleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 320);
            this.Controls.Add(this.DeleteDayButton);
            this.Controls.Add(this.AddDayButton);
            this.Controls.Add(this.ShowComRadioButton);
            this.Controls.Add(this.ShowTCPIPRadioButton);
            this.Controls.Add(this.PollPMCheckBox);
            this.Controls.Add(this.PollAMCheckBox);
            this.Controls.Add(this.PollDaysListBox);
            this.Controls.Add(this.PollCalendar);
            this.Controls.Add(this.PollOnScheduleCheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PortsListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(615, 365);
            this.Name = "ScheduleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Расписание опроса";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ScheduleForm_FormClosing);
            this.Load += new System.EventHandler(this.ScheduleForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox PortsListBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox PollOnScheduleCheckBox;
        private System.Windows.Forms.MonthCalendar PollCalendar;
        private System.Windows.Forms.ListBox PollDaysListBox;
        private System.Windows.Forms.CheckBox PollAMCheckBox;
        private System.Windows.Forms.CheckBox PollPMCheckBox;
        private System.Windows.Forms.RadioButton ShowTCPIPRadioButton;
        private System.Windows.Forms.RadioButton ShowComRadioButton;
        private System.Windows.Forms.Button AddDayButton;
        private System.Windows.Forms.Button DeleteDayButton;
    }
}