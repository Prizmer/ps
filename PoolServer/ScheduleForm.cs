using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Prizmer.PoolServer.DataBase;
using PollingLibraries.LibPorts;

namespace Prizmer.PoolServer
{
    public partial class ScheduleForm : Form
    {
        private List<DateTime> DaysList = new List<DateTime>();
        private List<TCPIPSettings> TCPIPSettingsList = new List<TCPIPSettings>();
        private List<ComPortSettings> ComPortSettingsList = new List<ComPortSettings>();
        private PortSchedule portSchedule;

        public PgStorage storage;

        public ScheduleForm()
        {
            InitializeComponent();
        }

        private void ScheduleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            foreach (Form win in Application.OpenForms)
            {
                if (win.Name == "FormMain") win.Focus();
            }
            e.Cancel = true;
        }

        private void UpdatePortsList()
        {
            PortsListBox.Items.Clear();
            PollDaysListBox.Items.Clear();
            if (ShowTCPIPRadioButton.Checked)
            {
                foreach (TCPIPSettings TCPIPsettings in TCPIPSettingsList)
                {
                    PortsListBox.Items.Add(TCPIPsettings.ip_address + ':' + TCPIPsettings.ip_port.ToString());
                }
            }
            else
            {
                foreach (ComPortSettings ComPortsettings in ComPortSettingsList)
                {
                    PortsListBox.Items.Add("Com"+ComPortsettings.name);
                }
            }
            if (PortsListBox.Items.Count > 0)
                PortsListBox.SelectedIndex = 0;
        }

        private void ShowTCPIPRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            AddDayButton.Enabled = false;
            UpdatePortsList();
        }

        private void AddDayButton_Click(object sender, EventArgs e)
        {
            DateTime j = new DateTime();
            for (int i = 0; i < Math.Ceiling(PollCalendar.SelectionEnd.Subtract(PollCalendar.SelectionStart).TotalDays); i++)
            {
                j = PollCalendar.SelectionStart.AddDays(i);
                DaysList.Add(j);
            }
            DaysList.Sort((a, b) => a.Ticks.CompareTo(b.Ticks));
            portSchedule.days = DaysList.ToArray();
            PollDaysListBox.Items.Clear();
            foreach (DateTime dt in DaysList)
            {
                PollDaysListBox.Items.Add(dt.ToShortDateString());
            }
            PollDaysListBox.SelectedIndex = DaysList.IndexOf(j);
            storage.ChangePortSchedule(portSchedule, ShowTCPIPRadioButton.Checked);
        }

        private void PortsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PortsListBox.SelectedIndex > -1)
            {
                PollDaysListBox.Items.Clear();
                DaysList.Clear();
                if (ShowTCPIPRadioButton.Checked)
                    portSchedule = storage.GetPortScheduleByGUID(TCPIPSettingsList[PortsListBox.SelectedIndex].guid, true);
                else
                    portSchedule = storage.GetPortScheduleByGUID(ComPortSettingsList[PortsListBox.SelectedIndex].guid, false);
                if (portSchedule.guid != Guid.Empty)
                {
                    DaysList = portSchedule.days.ToList();
                    foreach (DateTime dt in DaysList)
                    {
                        PollDaysListBox.Items.Add(dt.ToShortDateString());
                    }
                }
                else
                {
                    if (ShowTCPIPRadioButton.Checked)
                        portSchedule = storage.CreatePortScheduleForGUID(TCPIPSettingsList[PortsListBox.SelectedIndex].guid, true, true, false, true);
                    else
                        portSchedule = storage.CreatePortScheduleForGUID(ComPortSettingsList[PortsListBox.SelectedIndex].guid, true, true, false, false);
                }
                PollAMCheckBox.Checked = portSchedule.PollAM;
                PollPMCheckBox.Checked = portSchedule.PollPM;
                PollOnScheduleCheckBox.Checked = portSchedule.UseSchedule;
                if (PollDaysListBox.Items.Count > 0)
                    PollDaysListBox.SelectedIndex = PollDaysListBox.Items.Count - 1;
                AddDayButton.Enabled = true;
            }
            else
                AddDayButton.Enabled = false;
        }

        private void PollDaysListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (PollDaysListBox.SelectedIndex > -1)
            {
                DeleteDayButton.Enabled = true;
                PollCalendar.SelectionStart = portSchedule.days[PollDaysListBox.SelectedIndex];
                PollCalendar.SelectionEnd = PollCalendar.SelectionStart;
            }
            else
                DeleteDayButton.Enabled = false;
        }

        private void DeleteDayButton_Click(object sender, EventArgs e)
        {
            int s = PollDaysListBox.SelectedIndex;
            List<int> indices = new List<int>();
            foreach (int i in PollDaysListBox.SelectedIndices)
            {
                indices.Add(i);
            }
            indices.Sort((a, b) => b.CompareTo(a));
            foreach (int i in indices)
            {
                DaysList.RemoveAt(i);
                PollDaysListBox.Items.RemoveAt(i);
            }
            portSchedule.days = DaysList.ToArray();
            storage.ChangePortSchedule(portSchedule, ShowTCPIPRadioButton.Checked);
            PollDaysListBox.SelectedIndices.Clear();
            if (s >= PollDaysListBox.Items.Count)
                s = PollDaysListBox.Items.Count - 1;
            PollDaysListBox.SelectedIndex = s;
        }

        private void PollAMCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            portSchedule.PollAM = PollAMCheckBox.Checked;
            storage.ChangePortSchedule(portSchedule, ShowTCPIPRadioButton.Checked);
        }

        private void PollPMCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            portSchedule.PollPM = PollPMCheckBox.Checked;
            storage.ChangePortSchedule(portSchedule, ShowTCPIPRadioButton.Checked);
        }

        private void PollOnScheduleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            portSchedule.UseSchedule = PollOnScheduleCheckBox.Checked;
            storage.ChangePortSchedule(portSchedule, ShowTCPIPRadioButton.Checked);
        }

        private void ScheduleForm_Load(object sender, EventArgs e)
        {
            TCPIPSettingsList = storage.GetTCPIPSettings().ToList();
            TCPIPSettingsList.Sort((a, b) => (a.ip_address.CompareTo(b.ip_address) * 10) + a.ip_port.CompareTo(b.ip_port)); //сортировка по возрастанию IP и порта для удобного ориентирования
            ComPortSettingsList = storage.GetComportSettings().ToList();
            UpdatePortsList();
        }
    }
}
