using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;


namespace PollServerCfg
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        ServerParams sp = new ServerParams();
        string defaultPath = Directory.GetCurrentDirectory() + "\\PoolServer.exe";
        // передаем в конструктор тип класса
        XmlSerializer formatter = new XmlSerializer(typeof(ServerParams));


        System.Configuration.Configuration config;
        private void Form1_Load(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();
            if (!File.Exists(defaultPath)){
                fd.FileName = defaultPath;

                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    defaultPath = fd.FileName;
                }
                else
                {
                    this.Close();
                }
            }


            // получим конфигурационный файл СО
            config = ConfigurationManager.OpenExeConfiguration(fd.FileName) as Configuration;
            
            //настройки подключения
            ConnectionStringsSection conStrSection = config.ConnectionStrings as ConnectionStringsSection;
            ConnectionStringSettings connection = conStrSection.ConnectionStrings[0];
            richTextBox1.Text = connection.ConnectionString;

            ConfigurationSectionGroup pollSettingsGroup = config.GetSectionGroup("pollSettingsGroup");

            // Display each KeyValueConfigurationElement.
            //NameValueCollection sectionSettings = config.GetSection("doPollSection") as NameValueCollection;


            string gr = "";
            /*
            foreach ( ConfigurationSection c in config.AppSettings.ToString)
            {
                gr += "Group Name: " + c.SectionInformation.SectionName + "; ";
            }*/
            MessageBox.Show(config.AppSettings.SectionInformation.SectionName.ToString());

            // десериализация
            using (FileStream fs = new FileStream(defaultPath, FileMode.Open))
            {
                sp = (ServerParams)formatter.Deserialize(fs);
            }

            TimeSpan parsedTime = TimeSpan.Parse(sp.period);
            dateTimePicker1.Value = DateTime.Now.Date + parsedTime;

            for (int i = 0; i < sp.typeNames.Count; i++)
                checkedListBox1.Items.Add(sp.typeNames[i], sp.typeEnabled[i]);

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            //MessageBox.Show(e.NewValue.ToString());
            if (e.NewValue.ToString() == "Checked")
            {
                sp.typeEnabled[e.Index] = true;
            }
            else
            {
                sp.typeEnabled[e.Index] = false;
            }

            sp.period = dateTimePicker1.Value.TimeOfDay.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // получаем поток, куда будем записывать сериализованный объект
            using (FileStream fs = new FileStream(defaultPath, FileMode.Create))
            {
                formatter.Serialize(fs, sp);
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {

        }
    }

    [Serializable]
    public class ServerParams
    {
        public List<string> typeNames = new List<string>();
        public List<bool> typeEnabled = new List<bool>();
        public string period;
    }
}
