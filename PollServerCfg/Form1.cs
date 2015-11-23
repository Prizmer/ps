using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;

using System.Collections;
using System.Collections.Specialized;
using System.Configuration;

using System.Diagnostics;


namespace PollServerCfg
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());

        string PathToPollServerCfg = "";
        string PathToPollServer = "";

        System.Configuration.Configuration config;
        AppSettingsSection appSettingsSection;
        ConnectionStringsSection conStrSection;
        ConnectionStringSettings connection;

        private void Form1_Load(object sender, EventArgs e)
        {
            //default paths 
            PathToPollServerCfg = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + 
                @"\PoolServer\bin\Debug\PoolServer.exe.config";
            PathToPollServer = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName +
                @"\PoolServer\bin\Debug\PoolServer.exe";

            OpenFileDialog fd = new OpenFileDialog();
            if (!File.Exists(PathToPollServerCfg))
            {
                fd.FileName = PathToPollServerCfg;

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FileInfo fi = new FileInfo(fd.FileName);
                    PathToPollServerCfg = fd.FileName;
                    PathToPollServer = fi.FullName + @"\PoolServer.exe";
                }
                else
                {
                    this.Close();
                }
            }


            // получим конфигурационный файл СО
            ExeConfigurationFileMap exfm = new ExeConfigurationFileMap();
            exfm.ExeConfigFilename = PathToPollServerCfg;
            config = ConfigurationManager.OpenMappedExeConfiguration(exfm, ConfigurationUserLevel.None);

            //настройки подключения
            conStrSection = config.ConnectionStrings as ConnectionStringsSection;
            connection = conStrSection.ConnectionStrings[0];
            richTextBox1.Text = connection.ConnectionString;

            //настройки приложения
            appSettingsSection = (config.GetSection("appSettings") as AppSettingsSection);

            //инициализация графических элементов значениями
            foreach (string setting in appSettingsSection.Settings.AllKeys)
            {
                if (setting.IndexOf("b_poll") != -1)
                {
                    string b_poll_param_type = appSettingsSection.Settings[setting].Value;
                    bool val = false;                        
                    if (!bool.TryParse(b_poll_param_type, out val)) continue;

                    checkedListBox1.Items.Add(setting, val);
                }
                else if (setting.IndexOf("ts") != -1)
                {
                    string ts_period = appSettingsSection.Settings[setting].Value;

                    TimeSpan val = new TimeSpan(DateTime.Now.Ticks);                        
                    if (!TimeSpan.TryParse(ts_period, out val)) continue;

                    dateTimePicker1.Value = DateTime.Now.Date + val;
                }
            }
        }


        private void applySettings()
        {
            int b_poll_items_cnt = 0;
            foreach (string setting in appSettingsSection.Settings.AllKeys)
            {
                if (setting.IndexOf("b_poll") != -1)
                {
                    bool cb_value = checkedListBox1.GetItemChecked(b_poll_items_cnt);

                    appSettingsSection.Settings[setting].Value = cb_value.ToString();
                    b_poll_items_cnt++;
                }
                else if (setting.IndexOf("ts") != -1)
                {
                    TimeSpan val = new TimeSpan(dateTimePicker1.Value.Ticks);
                    appSettingsSection.Settings[setting].Value = val.ToString(@"hh\:mm\:ss");
                }

            }

            connection.ConnectionString = richTextBox1.Text;
            config.Save(ConfigurationSaveMode.Modified);

            //Force a reload of the changed section. This makes the new values available for reading.
            //ConfigurationManager.RefreshSection(sectionName);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            applySettings();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            applySettings();
            if (File.Exists(PathToPollServer))
            {
                Process.Start(PathToPollServer);
                Application.Exit();
            }
            else
            {
                MessageBox.Show("Не найден исполняемый файл сервера опроса PoolServer.exe",  "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }


}
