namespace PollingLibraries.LibLogger
{
    partial class CtlLogger
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnOpenLogsDir = new System.Windows.Forms.Button();
            this.btnDeleteAllLogsNow = new System.Windows.Forms.Button();
            this.btnDeleteOldLogsGentle = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnDeleteOldLogsGentle);
            this.groupBox1.Controls.Add(this.btnDeleteAllLogsNow);
            this.groupBox1.Controls.Add(this.btnOpenLogsDir);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(176, 81);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Логи";
            // 
            // btnOpenLogsDir
            // 
            this.btnOpenLogsDir.Location = new System.Drawing.Point(9, 22);
            this.btnOpenLogsDir.Name = "btnOpenLogsDir";
            this.btnOpenLogsDir.Size = new System.Drawing.Size(48, 48);
            this.btnOpenLogsDir.TabIndex = 0;
            this.btnOpenLogsDir.Text = "О";
            this.toolTip1.SetToolTip(this.btnOpenLogsDir, "Открыть связаную дирректорию");
            this.btnOpenLogsDir.UseVisualStyleBackColor = true;
            this.btnOpenLogsDir.Click += new System.EventHandler(this.BtnOpenLogsDir_Click);
            // 
            // btnDeleteAllLogsNow
            // 
            this.btnDeleteAllLogsNow.Location = new System.Drawing.Point(63, 22);
            this.btnDeleteAllLogsNow.Name = "btnDeleteAllLogsNow";
            this.btnDeleteAllLogsNow.Size = new System.Drawing.Size(48, 48);
            this.btnDeleteAllLogsNow.TabIndex = 1;
            this.btnDeleteAllLogsNow.Text = "У1";
            this.toolTip1.SetToolTip(this.btnDeleteAllLogsNow, "Удалить все логи грубо");
            this.btnDeleteAllLogsNow.UseVisualStyleBackColor = true;
            this.btnDeleteAllLogsNow.Click += new System.EventHandler(this.BtnDeleteAllLogsNow_Click);
            // 
            // btnDeleteOldLogsGentle
            // 
            this.btnDeleteOldLogsGentle.Location = new System.Drawing.Point(117, 22);
            this.btnDeleteOldLogsGentle.Name = "btnDeleteOldLogsGentle";
            this.btnDeleteOldLogsGentle.Size = new System.Drawing.Size(48, 48);
            this.btnDeleteOldLogsGentle.TabIndex = 2;
            this.btnDeleteOldLogsGentle.Text = "У2";
            this.toolTip1.SetToolTip(this.btnDeleteOldLogsGentle, "Удалить старые логи в соответствии с алгоритмом");
            this.btnDeleteOldLogsGentle.UseVisualStyleBackColor = true;
            this.btnDeleteOldLogsGentle.Click += new System.EventHandler(this.BtnDeleteOldLogsGentle_Click);
            // 
            // CtlLogger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "CtlLogger";
            this.Size = new System.Drawing.Size(182, 87);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button btnDeleteOldLogsGentle;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button btnDeleteAllLogsNow;
        private System.Windows.Forms.Button btnOpenLogsDir;
    }
}
