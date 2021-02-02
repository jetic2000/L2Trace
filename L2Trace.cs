using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Configuration;

namespace L2Trace
{
    public partial class L2Trace : Form
    {
        public delegate void printLogDelegate(string msg);
        public delegate void updatePlayerDelegate(PlayerInfo player);
        SvrPacketHandler packetHander;
        AlilogUpdate netupdater;
        GameCrypt crypt;
        Dictionary<int, int> NetworkComboxDict = new Dictionary<int, int>();
        Boolean isRunning = false;
        Int32 selectedNetworkIndex = -1;
        AppData app;
        private object sessionlock; 
        Dictionary<String, ListView> PlayerListDict = new Dictionary<String, ListView>();


        public class AppData
        {

            public printLogDelegate md;
            public updatePlayerDelegate up;
            public List<GameSession> Sessions;
            public BlockQueue<NetPacketData> capQueue;
            public BlockQueue<SendPaketData> netQueue;
            
            public CancellationTokenSource Source = new CancellationTokenSource();
            public List<String> BlockList;
            public UInt32 gameport;
            public AppData()
            {
                Sessions = new List<GameSession>();
                capQueue = new BlockQueue<NetPacketData>(8000);
                netQueue = new BlockQueue<SendPaketData>(8000);
                BlockList = new List<String>();
                gameport = 7777;
            }
        }

        public L2Trace()
        {
            InitializeComponent();
            this.sessionlock = new object();
            this.app = new AppData();
            this.app.md = new printLogDelegate(printLog);
            this.app.up = new updatePlayerDelegate(updatePlayerList);
            this.packetHander = new SvrPacketHandler(this.app);
            this.netupdater = new AlilogUpdate(this.app);
            this.crypt = new GameCrypt(this.app.md);
            getNetworkAdapters();
            loadBlockList();
        }

        public void loadBlockList()
        {
            try{
                lock(sessionlock)
                {
                    this.app.BlockList.Clear();
                }

                foreach(string str in System.IO.File.ReadAllLines("config.txt", Encoding.GetEncoding("utf-8")))
                {
                    lock(sessionlock)
                    {
                        this.app.BlockList.Add(str);
                        Console.WriteLine(str);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Read Config File Error");
            }
        }

        public void printLog(string msg)
        {
            if (this.logTextBox.InvokeRequired)
            {
                printLogDelegate resetMsgTxtCallBack = printLog;
                this.logTextBox.Invoke(resetMsgTxtCallBack, new object[] {msg});
            }
            else
            {
                this.logTextBox.AppendText(msg);
                this.logTextBox.AppendText(System.Environment.NewLine);
            }
        }

        public void updatePlayerList(PlayerInfo player)
        {
            Font f_blod = new Font(Control.DefaultFont, FontStyle.Bold);
            Font f_regular = new Font(Control.DefaultFont, FontStyle.Regular);

            if (this.tabControlSession.InvokeRequired)
            {
                updatePlayerDelegate resetMsgTxtCallBack = updatePlayerList;
                this.tabControlSession.Invoke(resetMsgTxtCallBack, new object[] {player});
            }
            else
            {
                //Console.WriteLine("{0},{1},{2}", player.name, player.distance, player.removeMark);

                if (this.PlayerListDict.ContainsKey(player.updater) == false)
                {
                    TabPage tp =new TabPage();
                    tp.Text = player.updater;
                    ListView lsv = new ListView();
                    lsv.Location = new Point(0, 0);
                    lsv.View = View.Details;
                    lsv.Dock = DockStyle.Fill;

                    ColumnHeader columnHeaderPlayer = new ColumnHeader();
                    columnHeaderPlayer.Text = "玩家";
                    columnHeaderPlayer.Width = 100;
                    lsv.Columns.Add(columnHeaderPlayer);

                    ColumnHeader columnHeaderDistance = new ColumnHeader();
                    columnHeaderDistance.Text = "距离";
                    columnHeaderDistance.Width = 50;
                    lsv.Columns.Add(columnHeaderDistance);

                    ColumnHeader columnHeaderAngle = new ColumnHeader();
                    columnHeaderAngle.Text = "方向";
                    columnHeaderAngle.Width = 50;
                    lsv.Columns.Add(columnHeaderAngle);

                    ColumnHeader columnHeaderPos = new ColumnHeader();
                    columnHeaderPos.Text = "位置";
                    columnHeaderPos.Width = 150;
                    lsv.Columns.Add(columnHeaderPos);

                    ColumnHeader columnHeaderClan = new ColumnHeader();
                    columnHeaderClan.Text = "血盟";
                    columnHeaderClan.Width = 100;
                    lsv.Columns.Add(columnHeaderClan);

                    ColumnHeader columnHeaderAlly = new ColumnHeader();
                    columnHeaderAlly.Text = "联盟";
                    columnHeaderAlly.Width = 100;
                    lsv.Columns.Add(columnHeaderAlly);

                    tp.Controls.Add(lsv);
                    this.tabControlSession.TabPages.Add(tp);
                    this.PlayerListDict.Add(player.updater, lsv);

                    ListViewItem items = new ListViewItem();
                    items.Text = player.name;
                    items.SubItems.Add(player.distance.ToString());
                    items.SubItems.Add(player.angle.ToString());
                    items.SubItems.Add("(" + player.location.x.ToString() + "," + player.location.y.ToString() + "," + player.location.z.ToString() + ")");
                    items.SubItems.Add(player.clanname.ToString());
                    items.SubItems.Add(player.allyname.ToString());

                    if (checkBlockList(items.Text) == true)
                    {
                        if (items.ForeColor != Color.Red)
                        {
                            items.ForeColor = Color.Red;
                            items.Font = f_blod;
                        }
                        lsv.Items.Insert(0,items);
                    }
                    else
                    {
                        if (items.ForeColor != Color.Black)
                        {
                            items.ForeColor = Color.Black;
                            items.Font = f_regular;
                        }
                        lsv.Items.Add(items);
                    }
                    //lsv.Items.Add(items);
                    
                }
                else
                {
                    ListView lsv = this.PlayerListDict[player.updater];
                    Boolean found = false;
                    int i = 0;

                    for (i = 0; i < lsv.Items.Count; i ++)
                    {
                        if (player.name == lsv.Items[i].Text)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        if (player.removeMark == true)
                        {
                            lsv.Items.RemoveAt(i);
                        }
                        else
                        {
                            lsv.Items[i].SubItems[0].Text = player.name;
                            lsv.Items[i].SubItems[1].Text = player.distance.ToString();
                            lsv.Items[i].SubItems[2].Text = player.angle.ToString();
                            lsv.Items[i].SubItems[3].Text = "(" + player.location.x.ToString() + "," + player.location.y.ToString() + "," + player.location.z.ToString() + ")";
                            lsv.Items[i].SubItems[4].Text = player.clanname.ToString();
                            lsv.Items[i].SubItems[5].Text = player.allyname.ToString();
                            if (checkBlockList(lsv.Items[i].SubItems[0].Text) == true)
                            {
                                if (lsv.Items[i].ForeColor != Color.Red)
                                {
                                    //lsv.Items[i].ForeColor = Color.Red;
                                    //lsv.Items[i].Font = f_blod;
                                    ListViewItem items = new ListViewItem();
                                    items.Text = player.name;
                                    items.SubItems.Add(player.distance.ToString());
                                    items.SubItems.Add(player.angle.ToString());
                                    items.SubItems.Add("(" + player.location.x.ToString() + "," + player.location.y.ToString() + "," + player.location.z.ToString() + ")");
                                    items.SubItems.Add(player.clanname.ToString());
                                    items.SubItems.Add(player.allyname.ToString());
                                    items.ForeColor = Color.Red;
                                    items.Font = f_blod;
                                    lsv.Items.RemoveAt(i);
                                    lsv.Items.Insert(0,items);
                                }
                            }
                            else
                            {
                                if (lsv.Items[i].ForeColor != Color.Black)
                                {
                                    //lsv.Items[i].ForeColor = Color.Black;
                                    //lsv.Items[i].Font = f_regular;
                                    ListViewItem items = new ListViewItem();
                                    items.Text = player.name;
                                    items.SubItems.Add(player.distance.ToString());
                                    items.SubItems.Add(player.angle.ToString());
                                    items.SubItems.Add("(" + player.location.x.ToString() + "," + player.location.y.ToString() + "," + player.location.z.ToString() + ")");
                                    items.SubItems.Add(player.clanname.ToString());
                                    items.SubItems.Add(player.allyname.ToString());
                                    items.ForeColor = Color.Black;
                                    items.Font = f_regular;
                                    lsv.Items.RemoveAt(i);
                                    lsv.Items.Add(items);
                                }
                            }
                        }
                    }
                    else
                    {   
                        if (player.removeMark != true)
                        {
                            ListViewItem items = new ListViewItem();
                            items.Text = player.name;
                            items.SubItems.Add(player.distance.ToString());
                            items.SubItems.Add(player.angle.ToString());
                            items.SubItems.Add("(" + player.location.x.ToString() + "," + player.location.y.ToString() + "," + player.location.z.ToString() + ")");
                            items.SubItems.Add(player.clanname.ToString());
                            items.SubItems.Add(player.allyname.ToString());
                            if (checkBlockList(items.Text) == true)
                            {
                                if (items.ForeColor != Color.Red)
                                {
                                    items.ForeColor = Color.Red;
                                    items.Font = f_blod;
                                }
                                lsv.Items.Insert(0,items);
                            }
                            else
                            {
                                if (items.ForeColor != Color.Black)
                                {
                                    items.ForeColor = Color.Black;
                                    items.Font = f_regular;
                                }
                                lsv.Items.Add(items);
                            }
                            //lsv.Items.Add(items);
                            //lsv.Items.Insert(0,items);
                        }
                    }  
                }
            }
        }

        public void getNetworkAdapters()
        {
            List<NetworkAdapterInfo> adapterlist = new List<NetworkAdapterInfo>();
            adapterlist = this.packetHander.GetNetworkAdapters();
            NetworkComboxDict.Clear();
            netAdapterComboBox.Items.Clear();
            selectedNetworkIndex = -1;

            int j = 0;
            for (int i = 0; i < adapterlist.Count; i++)
            {
                string n = MidStrEx(adapterlist[i].name, "{", "}");
                if (n != "")
                {
                    netAdapterComboBox.Items.Add("[" + i.ToString() + "]:" + "(" + adapterlist[i].description + ")");
                    NetworkComboxDict[j] = i;
                    j++;
                }
                //netAdapterComboBox.Items.Add(adapterlist[i].name);
            }
        }

        public static string MidStrEx(string sourse, string startstr, string endstr)
        {
            string result = string.Empty;
            int startindex, endindex;
            try
            {
                startindex = sourse.IndexOf(startstr);
                if (startindex == -1)
                    return result;
                string tmpstr = sourse.Substring(startindex + startstr.Length);
                endindex = tmpstr.IndexOf(endstr);           
                if (endindex == -1)
                    return result;
                result = tmpstr.Remove(endindex);
            }
            catch (Exception ex)
            {
                //Log.WriteLog("MidStrEx Err:" + ex.Message);
            }
            return result;
        }

        private void buttonRun_Click(object sende, EventArgs e)
        {
            if(selectedNetworkIndex == -1)
            {
                MessageBox.Show("请先选择使用中的网卡");
                return;
            }

            if (isRunning == false)
            {
                netAdapterComboBox.Enabled = false;
                buttonRun.Text = "点击停止";
                isRunning = true;
                this.packetHander.PacketHandlerTaskStart(selectedNetworkIndex);
                //packetHander.PacketHandlerTaskStart(selectedNetworkIndex, this.pd, this.md);
            }
            else
            {
                netAdapterComboBox.Enabled = true;
                buttonRun.Text = "点击开始";
                isRunning = false;
                this.packetHander.PacketHandlerTaskStop(selectedNetworkIndex);
            }


        }

        private void netAdapterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedNetworkIndex = NetworkComboxDict[netAdapterComboBox.SelectedIndex];
            Console.WriteLine("Selected Network Index = {0}", selectedNetworkIndex);
        }

        private void logTextBox_TextChanged(object sender, EventArgs e)
        {
            if (logTextBox.Lines.Length > 301)
            {
                logTextBox.Text = logTextBox.Text.Substring(logTextBox.Lines[0].Length + 1);
            }
        }

        public Boolean checkBlockList(String name)
        {
            Boolean result = false;
            lock(sessionlock)
            {
                result = this.app.BlockList.Contains(name);
            }
            return result;
        }

        private void buttonBlockList_Click(object sender, EventArgs e)
        {
            loadBlockList();
        }
    }
}
