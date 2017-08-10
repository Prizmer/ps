using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;


using PollingLibraries.LibPorts;

namespace Prizmer.PoolServer
{
    public struct AnalizatorPollThreadInfo
    {
        public AnalizatorPollThreadInfo(VirtualPort vp)
        {
            this.vp = vp;
            this.thread = null;

            if (vp != null)
                name = vp.GetFullName();
            else
                this.name = "Unknown port";

            creationDate = DateTime.Now;
            stopDate = new DateTime();

            isCycleInterupted = false;
            commentList = new List<string>();

            metersByPort = -1;

            currentMeterName = "";
            currentParamType = "";
            currentMeterNumber = 0;
        }

        public AnalizatorPollThreadInfo(string name, VirtualPort vp = null)
        {
            this.vp = vp;
            this.thread = null;

            if (vp != null)
                this.name = vp.GetFullName();
            else
                this.name = name;

            creationDate = DateTime.Now;
            stopDate = new DateTime();

            isCycleInterupted = false;
            commentList = new List<string>();
            metersByPort = -1;

            currentMeterName = "";
            currentParamType = "";
            currentMeterNumber = 0;
        }

        public VirtualPort vp;

        public Thread thread;
        public string name;
        public DateTime creationDate;
        public DateTime stopDate;
        public bool isCycleInterupted;
        public int metersByPort;

        public List<string> commentList;

        public string currentMeterName;
        public string currentParamType;
        public int currentMeterNumber;

    }

    public partial class Analizator : Form
    {
        object syncAddObject;
        public Analizator()
        {
            InitializeComponent();

            syncAddObject = new object();
        }

        public List<AnalizatorPollThreadInfo> aliveThreads = new List<AnalizatorPollThreadInfo>();
        public List<AnalizatorPollThreadInfo> deadThreads = new List<AnalizatorPollThreadInfo>();




        public void addThreadToLiveListOrUpdate(AnalizatorPollThreadInfo info)
        {
            bool bAddLocked = true;

            while (bAddLocked)
            {
                lock (syncAddObject)
                {

                    if (info.vp == null && info.name.Length == 0) return;

                    int indexDead = deadThreads.FindIndex((t) => { return t.name == info.name; });
                    if (indexDead > -1)
                    {
                        deadThreads.RemoveAt(indexDead);
                        info.commentList.Add("Поток был восстановлен из убитых " + DateTime.Now);
                    }

                    int index = aliveThreads.FindIndex((t) => { return t.name == info.name; });
                    if (index == -1)
                    {
                        aliveThreads.Add(info);
                    }
                    else
                    {
                        aliveThreads.RemoveAt(index);
                        if (index < aliveThreads.Count)
                            aliveThreads.Insert(index, info);
                        else
                            aliveThreads.Add(info);
                    }

                    bAddLocked = false;
                }

                Thread.Sleep(1);
            }
        }

        public void moveThreadToDeadList(AnalizatorPollThreadInfo info)
        {
            bool bDelLocked = true;

            while (bDelLocked)
            {
                lock (syncAddObject)
                {

                    int index = aliveThreads.FindIndex((t) => { return t.name == info.name; });
                    if (index > -1)
                    {
                        aliveThreads.RemoveAt(index);
                    }
                    else
                    {
                        return;//info.commentList.Add("Поток не был добавлен в живые списки!!");
                    }

                    info.isCycleInterupted = true;
                    info.stopDate = DateTime.Now;

                    deadThreads.Add(info);

                    bDelLocked = false;

                    return;
                }
            }
        }

        public void clearThreadLists()
        {
            aliveThreads.Clear();
            deadThreads.Clear();
        }



        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                //обновление списков потоков
                List<string> threadCaptions;
                if (lbActiveThreads.Items.Count != aliveThreads.Count)
                {
                    threadCaptions = new List<string>();
                    foreach (AnalizatorPollThreadInfo apti in aliveThreads)
                        threadCaptions.Add(apti.name);

                    lbActiveThreads.Items.Clear();
                    lbActiveThreads.Items.AddRange(threadCaptions.ToArray());
                }

                if (lbClosedThreads.Items.Count != deadThreads.Count)
                {
                    threadCaptions = new List<string>();
                    foreach (AnalizatorPollThreadInfo apti in deadThreads)
                        threadCaptions.Add(apti.name);

                    lbClosedThreads.Items.Clear();
                    lbClosedThreads.Items.AddRange(threadCaptions.ToArray());
                }


                if (selectedPollThreadInfo.name != null && selectedPollThreadInfo.name.Length > 0)
                {
                    rtbThreadInfo.Clear();
                    rtbThreadInfo.Text += ("Статус потока: " + (selectedPollThreadInfo.isCycleInterupted ? "остановлен" : "работает") + ";\n");
                    if (selectedPollThreadInfo.isCycleInterupted)
                        rtbThreadInfo.Text += ("Дата остановки: " + selectedPollThreadInfo.stopDate.ToString() + ";\n");
                    else
                        rtbThreadInfo.Text += ("Дата запуска: " + selectedPollThreadInfo.creationDate.ToString() + ";\n");

                    string threadObjSt = selectedPollThreadInfo.thread == null ? "null" : (selectedPollThreadInfo.thread.IsAlive ? "alive" : "not alive");
                    rtbThreadInfo.Text += ("Объект потока: " + threadObjSt + ";\n");

                    rtbThreadInfo.Text += ("Кол-во приборов: " + selectedPollThreadInfo.metersByPort + ";\n");

                    if (selectedPollThreadInfo.metersByPort > 0)
                    {
                       // rtbThreadInfo.Text += ("Опрашивается " + selectedPollThreadInfo.currentMeterNumber + " из " + selectedPollThreadInfo.metersByPort + ";\n");
                        // rtbThreadInfo.Text += ("Прибор: " + selectedPollThreadInfo.currentMeterName + ";\n");
                    }

                    foreach (string s in selectedPollThreadInfo.commentList)
                    {
                        rtbThreadInfo.Text += (s + "; \n");
                    }

                    rtbThreadInfo.Tag = selectedPollThreadInfo.name;
                }

            }
            catch (Exception ex)
            {

            }         
        }

        AnalizatorPollThreadInfo selectedPollThreadInfo = new AnalizatorPollThreadInfo();
        private void lbActiveThreads_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbActiveThreads.SelectedIndex != -1)
            {
                lbClosedThreads.SelectedIndex = -1;
                selectedPollThreadInfo = aliveThreads[lbActiveThreads.SelectedIndex];
            }
            else
            {
                selectedPollThreadInfo = new AnalizatorPollThreadInfo();
            }
        }

        private void lbClosedThreads_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbClosedThreads.SelectedIndex != -1)
            {
                lbActiveThreads.SelectedIndex = -1;
                selectedPollThreadInfo = deadThreads[lbClosedThreads.SelectedIndex];
            }
            else
            {
                selectedPollThreadInfo = new AnalizatorPollThreadInfo();
            }
        }

        private void Analizator_FormClosing(object sender, FormClosingEventArgs e)
        {

            this.Hide();
            e.Cancel = true;
        }

        private void Analizator_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

    }
}
