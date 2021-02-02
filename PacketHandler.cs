using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.Npcap;
using PacketDotNet;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace L2Trace
{
    public class NetPacketData
    {
        public UInt32 seq;
        public Int32 sport;
        public Int32 dport;
        public Boolean isSYN;
        public Boolean isFIN;
        public byte[] data;

        public NetPacketData()
        {
            isSYN = false;
            isSYN = false;
            seq = 0;
            sport = 0;
            dport = 0;
        }
    }

    public class GamePaketData
    {
        //public string data;
        public Int32 cmd;
        public Int32 excmd;
        public byte[] decypt_data;

        public GamePaketData()
        {
            cmd = -1;
            excmd = -1;
        }
    }

    public class SendPaketData
    {
        //public string data;
        public PlayerInfo player;
        public string data;

        public SendPaketData()
        {
            player = new PlayerInfo();
        }
    }

    public class NetworkAdapterInfo
    {
        public string name;
        public string description;
        public ICaptureDevice device;

        public NetworkAdapterInfo()
        {
            this.name = "";
            this.description = "";
            this.device = null;
        }
    }

    public class DataStream
    {
        public UInt32 next_seq;
        public UInt32 seq;
        public Boolean key_ready;
        public Boolean first_packet;
        public RingBuff data;
        public List<NetPacketData> unhandle_data;

        public DataStream()
        {
            this.next_seq = 0;
            this.seq = 0;
            this.key_ready = false;
            this.first_packet = true;
            this.data = new RingBuff(1024 * 1024 * 16);
            this.unhandle_data = new List<NetPacketData>();
        }
    }

    public class SvrPacketHandler
    {
        public GameSession session;
        public L2Trace.AppData app;
        public List<NetworkAdapterInfo> adapters = new List<NetworkAdapterInfo>();
        public GameCrypt gc;

        public SvrPacketHandler(L2Trace.AppData app)
        {
            this.app = app;
            this.gc = new GameCrypt(this.app.md);
        }

        private void sv_pledgeinfo(GamePaketData pkt, GameSession session)
        {
            UInt32 clanid = (UInt32)((pkt.decypt_data[7]) | (pkt.decypt_data[8] << 8) | (pkt.decypt_data[9] << 16) | (pkt.decypt_data[10] << 24));
            //Console.WriteLine("User ClanId = {0}", clanid);

            UInt32 i = 0;
            UInt32 j = 0;
            string s_clan = "";
            string s_ally = "";
            byte byte1;
            byte byte2;
            List<byte> unicodeClan = new List<byte>();
            List<byte> unicodeAlly = new List<byte>();
            while (true)
            {
                byte1 = (byte)(pkt.decypt_data[11 + i]);
                byte2 = (byte)(pkt.decypt_data[11 + i + 1]);
                i += 2;
                if ((byte1 == 0x00) && (byte2 == 0x00))
                {
                    break;
                }
                else
                {
                    unicodeClan.Add(byte1);
                    unicodeClan.Add(byte2);
                }
            }

            while (true)
            {
                byte1 = (byte)(pkt.decypt_data[11 + j + i]);
                byte2 = (byte)(pkt.decypt_data[11 + j + i + 1]);
                j += 2;
                if ((byte1 == 0x00) && (byte2 == 0x00))
                {

                    break;
                }
                else
                {
                    unicodeAlly.Add(byte1);
                    unicodeAlly.Add(byte2);
                }
            }

            if (unicodeClan.Count > 0)
            {
                byte[] ClanName = unicodeClan.ToArray();
                s_clan = Encoding.Unicode.GetString(ClanName);
                //Console.WriteLine("User ClanName:{0}", Encoding.Unicode.GetString(ClanName));
            }

            if (unicodeAlly.Count > 0)
            {
                byte[] AllyName = unicodeAlly.ToArray();
                s_ally = Encoding.Unicode.GetString(AllyName);
                //Console.WriteLine("User AllyName:{0}", Encoding.Unicode.GetString(AllyName));
            }

            session.UpdateClanInfo(clanid, s_clan, s_ally);
        }

        private static void sv_deleteobject(GamePaketData pkt, GameSession session)
        {
            UInt32 userid = 0;
            PlayerInfo player = new PlayerInfo();
            userid = (UInt32)((pkt.decypt_data[3]) | (pkt.decypt_data[4] << 8) | (pkt.decypt_data[5] << 16) | (pkt.decypt_data[6] << 24));
            player.userid = userid;
            player.removeMark = true;
            session.UpdatePlayerInfo(player);
            //session.RemovePlayerInfo(userid);
        }

        private static void sv_movetopawn(GamePaketData pkt, GameSession session)
        {
            UInt32 userid = 0;
            PlayerInfo player = new PlayerInfo();
            //Int32 i = 0;
            userid = (UInt32)((pkt.decypt_data[3]) | (pkt.decypt_data[4] << 8) | (pkt.decypt_data[5] << 16) | (pkt.decypt_data[6] << 24));
            Int32 dx = (pkt.decypt_data[27] | pkt.decypt_data[28]<<8 | pkt.decypt_data[29]<<16 | pkt.decypt_data[30]<<24);
            Int32 dy = (pkt.decypt_data[31] | pkt.decypt_data[32]<<8 | pkt.decypt_data[33]<<16 | pkt.decypt_data[34]<<24);
            Int32 dz = (pkt.decypt_data[35] | pkt.decypt_data[36]<<8 | pkt.decypt_data[37]<<16 | pkt.decypt_data[38]<<24);

            player.location.x = dx;
            player.location.y = dy;
            player.location.z = dz;
            player.userid = userid;
            player.updateMark.location = true;
            session.UpdatePlayerInfo(player);
        }

        private static void sv_characterselectedpacket(GamePaketData pkt, GameSession session)
        {
            List<byte> unicodeChars = new List<byte>();
            UInt32 length = (UInt32)pkt.decypt_data.Length;
            UInt32 i = 3; //Reset
            while (i < length)
            {
                if ((pkt.decypt_data[i + 2] == 0x00) && (pkt.decypt_data[i + 3] == 0x00))
                {
                    break;
                }
                else
                {
                    unicodeChars.Add(pkt.decypt_data[i]);
                    unicodeChars.Add(pkt.decypt_data[i + 1]);
                }
                i += 2;
            }
            byte[] PlayerName = unicodeChars.ToArray();

            PlayerInfo player = new PlayerInfo();
            player.updateMark.userid = true;
            player.updateMark.name = true;
            player.userid = 0;
            player.name = Encoding.Unicode.GetString(PlayerName);
            player.isUser = true;
            player.removeMark = true;
            session.UpdatePlayerInfo(player);
            //Console.WriteLine("sv_characterselectedpacket");
            //Reset user for new char selected
        }

        private static void sv_userinfo(GamePaketData pkt, GameSession session)
        {
            UInt32 loc_pos;
            if (session.user.userid == 0)
            {
                PlayerInfo player = new PlayerInfo();
                UInt32 length = (UInt32)pkt.decypt_data.Length;
                //UInt32 username_length = 0;
                UInt32 userid = (UInt32)((pkt.decypt_data[3]) | (pkt.decypt_data[4] << 8) | (pkt.decypt_data[5] << 16) | (pkt.decypt_data[6] << 24));
                UInt32 username_length = (UInt32)((pkt.decypt_data[23]) | (pkt.decypt_data[24] << 8));
                List<byte> unicodeChars = new List<byte>();
                UInt32 i = 25;
                for (int j = 0; j < username_length; j++)
                {
                    unicodeChars.Add(pkt.decypt_data[i]);
                    unicodeChars.Add(pkt.decypt_data[i+1]);
                    i += 2;
                }
                byte[] PlayerName = unicodeChars.ToArray();
                if (length > 278) // just ensure for the first pkg TODO:
                {
                    loc_pos = length - 274;
                    Int32 dz = (pkt.decypt_data[loc_pos+8] | pkt.decypt_data[loc_pos+9]<<8 | pkt.decypt_data[loc_pos+10]<<16 | pkt.decypt_data[loc_pos+11]<<24);
                    Int32 dy = (pkt.decypt_data[loc_pos+4] | pkt.decypt_data[loc_pos+5]<<8 | pkt.decypt_data[loc_pos+6]<<16 | pkt.decypt_data[loc_pos+7]<<24);
                    Int32 dx = (pkt.decypt_data[loc_pos] | pkt.decypt_data[loc_pos+1]<<8 | pkt.decypt_data[loc_pos+2]<<16 | pkt.decypt_data[loc_pos+3]<<24);
                    player.location.x = dx;
                    player.location.y = dy;
                    player.location.z = dz;
                }
                player.userid = userid;
                //player.srvid = session.serverid.ToString();
                player.name = Encoding.Unicode.GetString(PlayerName);
                player.updateMark.userid = true;
                player.updateMark.name = true;
                player.updateMark.location = true;
                player.isUser = true;
                //session.UpdateUser(player);
                session.UpdatePlayerInfo(player);
                //Console.WriteLine("Get UserInfo: {0}, {1}", player.userid, player.name);
                //session.UpdatePlayerInfo(player);
            }
        }

        private void sv_teleporttolocationpacket(GamePaketData pkt, GameSession session)
        {
            //SendPaketData sendData = new SendPaketData();
            PlayerInfo player = new PlayerInfo();

            //byte [] d = gameDataPacket.decypt_data;
            //Int32 dx = (d[7] | d[8]<<8 | d[9]<<16 | d[10]<<24);

            Int32 dz = (pkt.decypt_data[15] | pkt.decypt_data[16]<<8 | pkt.decypt_data[17]<<16 | pkt.decypt_data[18]<<24);
            Int32 dy = (pkt.decypt_data[11] | pkt.decypt_data[12]<<8 | pkt.decypt_data[13]<<16 | pkt.decypt_data[14]<<24);
            Int32 dx = (pkt.decypt_data[7] | pkt.decypt_data[8]<<8 | pkt.decypt_data[9]<<16 | pkt.decypt_data[10]<<24);
            UInt32 userid = (UInt32)((pkt.decypt_data[3] | pkt.decypt_data[4]<<8 | pkt.decypt_data[5]<<16 | pkt.decypt_data[6]<<24));

            player.location.x = dx;
            player.location.y = dy;
            player.location.z = dz;
            player.userid = userid;
            player.updateMark.location = true;
            player.removeMark = true;
            //if (userid == session.user.userid)
            session.UpdatePlayerInfo(player); //self update only TODO:
            //session.RemovePlayerInfoAll(userid);
        }
        private void sv_stopmove(GamePaketData pkt, GameSession session)
        {
            //SendPaketData sendData = new SendPaketData();
            PlayerInfo player = new PlayerInfo();
            //Int32 i = 0;

            //byte [] d = gameDataPacket.decypt_data;
            //Int32 dx = (d[7] | d[8]<<8 | d[9]<<16 | d[10]<<24);

            Int32 dz = (pkt.decypt_data[15] | pkt.decypt_data[16]<<8 | pkt.decypt_data[17]<<16 | pkt.decypt_data[18]<<24);
            Int32 dy = (pkt.decypt_data[11] | pkt.decypt_data[12]<<8 | pkt.decypt_data[13]<<16 | pkt.decypt_data[14]<<24);
            Int32 dx = (pkt.decypt_data[7] | pkt.decypt_data[8]<<8 | pkt.decypt_data[9]<<16 | pkt.decypt_data[10]<<24);
            UInt32 userid = (UInt32)((pkt.decypt_data[3] | pkt.decypt_data[4]<<8 | pkt.decypt_data[5]<<16 | pkt.decypt_data[6]<<24));

            player.location.x = dx;
            player.location.y = dy;
            player.location.z = dz;
            player.userid = userid;
            player.updateMark.location = true;
            session.UpdatePlayerInfo(player);
        }

        private void sv_movetolocation(GamePaketData pkt, GameSession session)
        {
            //SendPaketData sendData = new SendPaketData();
            PlayerInfo player = new PlayerInfo();
            //Int32 i = 0;

            //byte [] d = gameDataPacket.decypt_data;
            //Int32 dx = (d[7] | d[8]<<8 | d[9]<<16 | d[10]<<24);

            Int32 dz = (pkt.decypt_data[15] | pkt.decypt_data[16]<<8 | pkt.decypt_data[17]<<16 | pkt.decypt_data[18]<<24);
            Int32 dy = (pkt.decypt_data[11] | pkt.decypt_data[12]<<8 | pkt.decypt_data[13]<<16 | pkt.decypt_data[14]<<24);
            Int32 dx = (pkt.decypt_data[7] | pkt.decypt_data[8]<<8 | pkt.decypt_data[9]<<16 | pkt.decypt_data[10]<<24);
            UInt32 userid = (UInt32)((pkt.decypt_data[3] | pkt.decypt_data[4]<<8 | pkt.decypt_data[5]<<16 | pkt.decypt_data[6]<<24));

            player.location.x = dx;
            player.location.y = dy;
            player.location.z = dz;
            player.userid = userid;
            player.updateMark.location = true;
            session.UpdatePlayerInfo(player);
        }

        private void sv_exuserinfo(GamePaketData pkt, GameSession session)
        {
            //TODO: Still a lot of work to done, only workaround now
            UInt32 userid = 0;
            UInt32 clanid = 0;
            Int32 i;
            UInt32 length;
            Boolean found = false;
            userid = (UInt32)((pkt.decypt_data[7]) | (pkt.decypt_data[8] << 8) | (pkt.decypt_data[9] << 16) | (pkt.decypt_data[10] << 24));
            length = (UInt32)pkt.decypt_data.Length;
            i = 0; //Reset
            while (i < length)
            {
                if ((pkt.decypt_data[i] == 0x10) && (pkt.decypt_data[i + 1] == 0x60))
                {
                    found = true;
                    break;
                }
                i += 1;
            }

            if (found == true)
            {
                clanid = (UInt32)((pkt.decypt_data[i - 2]) | (pkt.decypt_data[i - 1] << 8) | (pkt.decypt_data[i] << 16) | (pkt.decypt_data[i + 1] << 24));

            }
            //Console.WriteLine("User ClanId = {0}", clanid);

            Array.Reverse(pkt.decypt_data);
            List<byte> unicodeChars = new List<byte>();
            length = (UInt32)pkt.decypt_data.Length;
            i = 1; //Reset
            while (i < length)
            {
                if ((pkt.decypt_data[i + 2] == 0x00) && (pkt.decypt_data[i + 3] == 0x00))
                {
                    break;
                }
                else
                {
                    unicodeChars.Add(pkt.decypt_data[i]);
                    unicodeChars.Add(pkt.decypt_data[i + 1]);
                }
                i += 2;
            }

            i += 6;
            Int32 dz = (pkt.decypt_data[i + 3] | pkt.decypt_data[i + 2] << 8 | pkt.decypt_data[i + 1] << 16 | pkt.decypt_data[i] << 24);
            i += 4;
            Int32 dy = (pkt.decypt_data[i + 3] | pkt.decypt_data[i + 2] << 8 | pkt.decypt_data[i + 1] << 16 | pkt.decypt_data[i] << 24);
            i += 4;
            Int32 dx = (pkt.decypt_data[i + 3] | pkt.decypt_data[i + 2] << 8 | pkt.decypt_data[i + 1] << 16 | pkt.decypt_data[i] << 24);

            byte[] PlayerName = unicodeChars.ToArray();
            Array.Reverse(PlayerName);
            PlayerInfo player = new PlayerInfo();
            player.location.x = dx;
            player.location.y = dy;
            player.location.z = dz;
            player.name = Encoding.Unicode.GetString(PlayerName);
            player.clanid = clanid;
            player.userid = userid;

            player.updateMark.userid = true;
            player.updateMark.name = true;
            player.updateMark.clanid = true;
            player.updateMark.name = true;
            player.updateMark.location = true;
            session.UpdatePlayerInfo(player);
        }

        private void clGetGameDataSync(GamePaketData gameDataPacket, GameSession session)
        {

        }

        private void srvGetGameDataSync(GamePaketData gameDataPacket, GameSession session)
        {
            //Console.WriteLine(BitConverter.ToString(gameDataPacket.decypt_data, 0).Replace("-", string.Empty).ToLower());  
            gameDataPacket.cmd = gameDataPacket.decypt_data[2];
            Console.WriteLine("getGameData()->cmd : 0x{0:x}", gameDataPacket.cmd );
            Console.WriteLine(BitConverter.ToString(gameDataPacket.decypt_data, 0).Replace("-", string.Empty).ToLower());
            if (gameDataPacket.cmd == 0xFE)
            {
                gameDataPacket.excmd = (UInt16)((gameDataPacket.decypt_data[3]) | (gameDataPacket.decypt_data[4] << 8));
                Console.WriteLine("getGameData()->excmd : 0x{0:x}", gameDataPacket.excmd );
                if (gameDataPacket.excmd == 0x254)
                {
                    sv_exuserinfo(gameDataPacket, session);
                }
            }

            if (gameDataPacket.cmd == 0x72)
            {
                sv_movetopawn(gameDataPacket, session);
            }

            if (gameDataPacket.cmd == 0x8)
            {
                sv_deleteobject(gameDataPacket, session);
            }

            if (gameDataPacket.cmd == 0x2F)
            {
                sv_movetolocation(gameDataPacket, session);
                //byte[] d = gameDataPacket.decypt_data;
                //Int32 dx = (d[7] | d[8] << 8 | d[9] << 16 | d[10] << 24);
                //Console.WriteLine("dx = {0}", dx);
                //Console.WriteLine(BitConverter.ToString(gameDataPacket.decypt_data, 0).Replace("-", string.Empty).ToLower());
            }

            if (gameDataPacket.cmd == 0x89)
            {
                sv_pledgeinfo(gameDataPacket, session);
            }

            if (gameDataPacket.cmd == 0x32)
            {
                sv_userinfo(gameDataPacket, session);
            }

            if (gameDataPacket.cmd == 0x47)
            {
                sv_stopmove(gameDataPacket, session);
            }

            if (gameDataPacket.cmd == 0x22)
            {
                sv_teleporttolocationpacket(gameDataPacket, session);
            }

            if (gameDataPacket.cmd == 0xB)
            {
                sv_characterselectedpacket(gameDataPacket, session);
            }
        }

        private void getTcpData()
        {
            while (!this.app.Source.IsCancellationRequested)
            {

                var netdata = (NetPacketData)this.app.capQueue.DeQueue();
                //Console.WriteLine(BitConverter.ToString(netdata.data, 0).Replace("-", string.Empty).ToLower());

                Boolean found = false;
                GameSession gameSession = null;

                if (app.Sessions.Count != 0)
                {
                    for (int i = 0; i < this.app.Sessions.Count; i++)
                    {
                        if ((netdata.dport == this.app.Sessions[i].clport) || (netdata.sport == this.app.Sessions[i].clport))
                        {
                            found = true;
                            gameSession = this.app.Sessions[i];
                            break;
                        }
                    }
                }

                if (found == false)
                {
                    //First Package for new session shall always come from client
                    if (netdata.dport == this.app.gameport)
                    {
                        gameSession = new GameSession(this.app);
                        gameSession.clport = netdata.sport;
                        gameSession.srvport = netdata.dport;
                        app.Sessions.Add(gameSession);
                        this.app.md("[提示]: 发现新的客户端通讯");
                    }
                    else
                    {
                        //this.app.md("[错误]: 出现第一个包未从客户端发出");
                        continue;
                    }
                }

                handle_tcpData(gameSession, netdata);
            }
            Console.WriteLine(app.Source.IsCancellationRequested);
        }


        private void handle_tcpData(GameSession session, NetPacketData tcpPacket)
        {
            //DataStream srv_stream = session.srvstream;
            //DataStream cl_stream = session.clstream;
            DataStream stream;
            UInt32 limit = (UInt32)Math.Pow(2, 31);
            UInt32 round = (UInt32)Math.Pow(2, 32);

            // if (tcpPacket.data.Length == 0)
            //     return;

            

            if (tcpPacket.sport == this.app.gameport) //srv pkt
            {
                stream = session.srvstream;
            }
            else
            {
                stream = session.clstream;
            }

            Console.WriteLine("this_seq:{0}, next_seq:{1}", tcpPacket.seq, stream.next_seq);
            if ((tcpPacket.isSYN) || (tcpPacket.isFIN))
            {
                stream.seq = (UInt32)tcpPacket.seq;
                stream.next_seq = (UInt32)(tcpPacket.seq + 1);
                return;
            }
            else if(tcpPacket.data.Length != 0)
            {
                stream.unhandle_data.Add(tcpPacket);
                //Console.WriteLine("{0}", stream.unhandle_data.Count);
                for (int i = stream.unhandle_data.Count - 1; i >= 0; i--)
                {
                    UInt32 this_seq = stream.unhandle_data[i].seq;
                    UInt32 this_len = (UInt32)(stream.unhandle_data[i].data.Length);
                    UInt32 this_next_seq = (UInt32)(this_seq + this_len);
                    UInt32 pre_next_seq = stream.next_seq;
                    UInt32 pre_seq = stream.seq;

                    if (this_seq == pre_next_seq)
                    {
                        stream.data.WriteBuff(stream.unhandle_data[i].data);
                        stream.unhandle_data.RemoveAt(i);
                        stream.next_seq = this_next_seq;
                        stream.seq = (UInt32)this_seq;
                    }
                    else if ((Int32)(this_seq - pre_next_seq) < 0)
                    {
                        Console.WriteLine("Received seq earlier");
                        if ((Int32)(this_next_seq - pre_next_seq) <= 0)
                        {
                            Console.WriteLine("Received seq earlier, abandoned");
                            stream.unhandle_data.RemoveAt(i);
                        }
                        else
                        {
                            UInt32 count = 0;
                            UInt32 start = this_seq;
                            Console.WriteLine("Received seq earlier, part needed");
                            while(true)
                            {
                                if (start == pre_next_seq)
                                {
                                    break;
                                }
                                else
                                {
                                    start = start + 1;
                                    count = count + 1;
                                }
                            }
                            Console.WriteLine("Received seq earlier, part needed, count = {0}, len = {1}", count, this_len);

                            byte[] tmp = new byte[this_len - count];
                            int k = 0;
                            for (int j = (Int32)(count); j < (Int32)(this_len); j++)
                            {
                                tmp[k] = (stream.unhandle_data[i].data)[j];
                                k++;
                            }

                            stream.data.WriteBuff(tmp);
                            stream.unhandle_data.RemoveAt(i);
                            stream.next_seq = (UInt32)(this_seq + this_len);
                            stream.seq = (UInt32)this_seq;
                        }
                    }

                }
            }
            //Console.WriteLine("stream.seq = {0}", stream.seq);
            if (tcpPacket.sport == this.app.gameport) 
            {
                while (stream.data.GetBufferUsedSize() > 3)
                {
                    //Decrypt Srv Packet Here
                    GamePaketData gamepkt = new GamePaketData();
                    if (this.gc.srvDecryptPacket(session, ref gamepkt))
                    {
                        break;
                    }
                    else
                    {
                        srvGetGameDataSync(gamepkt, session);
                    }
                }
            }
            else
            {
                while (stream.data.GetBufferUsedSize() > 3)
                {
                    //Decrypt Cl Packet Here
                    GamePaketData gamepkt = new GamePaketData();
                    if (this.gc.clDecryptPacket(session, ref gamepkt))
                    {
                        break;
                    }
                    else
                    {
                        clGetGameDataSync(gamepkt, session);
                    }
                }
            }
        }

        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            // var time = e.Packet.Timeval.Date;
            // var len = e.Packet.Data.Length;
            // Console.WriteLine("{0}:{1}:{2},{3} Len={4}",
            //     time.Hour, time.Minute, time.Second, time.Millisecond, len);
            // Console.WriteLine(e.Packet.ToString());
            Packet p = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
            var tcpPacket = p.Extract<PacketDotNet.TcpPacket>();
            var ipPacket = (PacketDotNet.IPPacket)tcpPacket.ParentPacket;
            System.Net.IPAddress srcIp = ipPacket.SourceAddress;
            System.Net.IPAddress dstIp = ipPacket.DestinationAddress;
            int srcPort = tcpPacket.SourcePort;
            int dstPort = tcpPacket.DestinationPort;
            //Console.WriteLine("{0}:{1} -> {2}:{3}", srcIp, srcPort, dstIp, dstPort);

            NetPacketData netdata = new NetPacketData();
            if (tcpPacket != null)
            {
                if (tcpPacket.PayloadData.Length != 0)
                {
                    netdata.seq = tcpPacket.SequenceNumber;
                    netdata.dport = dstPort;
                    netdata.sport = srcPort;
                    netdata.isSYN = tcpPacket.Synchronize;
                    netdata.isFIN = tcpPacket.Finished;
                    Console.WriteLine("SYN: {0}, FIN: {1}", netdata.isSYN, netdata.isFIN);
                    netdata.data = new byte[tcpPacket.PayloadData.Length];
                    for (int i = 0; i < tcpPacket.PayloadData.Length; i++)
                    {
                        netdata.data[i] = tcpPacket.PayloadData[i];
                    }

                    try 
                    {
                        app.capQueue.EnQueue(netdata);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    
                }
                else
                {
                    netdata.seq = tcpPacket.SequenceNumber;
                    netdata.dport = dstPort;
                    netdata.sport = srcPort;
                    netdata.data = new byte[0];
                    netdata.isSYN = tcpPacket.Synchronize;
                    netdata.isFIN = tcpPacket.Finished;
                    Console.WriteLine("SYN: {0}, FIN: {1}", netdata.isSYN, netdata.isFIN);

                    try 
                    {
                        app.capQueue.EnQueue(netdata);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        public List<NetworkAdapterInfo> GetNetworkAdapters()
        {

            string ver = SharpPcap.Version.VersionString;
            Console.WriteLine("SharpPcap {0}", ver);
            adapters.Clear();

            //使用系统函数去匹配网络适配器
            Dictionary<String, String> networkDescription = new Dictionary<String, String>();
            NetworkInterface[] sys_adapters = NetworkInterface.GetAllNetworkInterfaces();//获取本地计算机上网络接口的对象
            //Console.WriteLine("适配器个数：" + adapters.Length);
            //Console.WriteLine();
            foreach (NetworkInterface sys_adapter in  sys_adapters)
            {

                networkDescription["\\Device\\NPF_" + sys_adapter.Id] = sys_adapter.Description;
            }

            var devices = CaptureDeviceList.Instance;

            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
            }
            else
            {
                //int i = 0;
                foreach (var dev in devices)
                {
                    /* Description */
                    NetworkAdapterInfo adapter = new NetworkAdapterInfo();
                    adapter.name = dev.Name;
                    if(networkDescription.ContainsKey(dev.Name))
                        adapter.description = networkDescription[dev.Name];
                    else
                        adapter.description = dev.Description;
                    adapter.device = dev;
                    adapters.Add(adapter);
                    //Console.WriteLine("{0}) {1} {2}", i, dev.Name, dev.Description);
                    //i++;
                }
            }
            return adapters;
        }

        //public void PacketHandlerTaskStart(Int32 AdapterIndex, L2Trace.drawPersonDelegate pd, L2Trace.printLogDelegate md)
        public void PacketHandlerTaskStart(Int32 AdapterIndex)
        {
            //Create APP to hold all data
            //PlayerDisplay playerdisplay = new PlayerDisplay(this.app, pd);
            //AlilogUpdate netupdater = new AlilogUpdate(this.app, md);
            //PlayerDisplay playerdisplay = new PlayerDisplay(this.app);
            //AlilogUpdate netupdater = new AlilogUpdate(this.app);

            Task packteHandlerTask = new Task(getTcpData);
            packteHandlerTask.Start();

            ICaptureDevice device = adapters[AdapterIndex].device;
            // Register our handler function to the 'packet arrival' event
            device.OnPacketArrival +=
                new PacketArrivalEventHandler(device_OnPacketArrival);

            // Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            if (device is NpcapDevice)
            {
                var nPcap = device as NpcapDevice;
                nPcap.Open(SharpPcap.Npcap.OpenFlags.DataTransferUdp | SharpPcap.Npcap.OpenFlags.NoCaptureLocal, readTimeoutMilliseconds);
            }
            else if (device is LibPcapLiveDevice)
            {
                var livePcapDevice = device as LibPcapLiveDevice;
                livePcapDevice.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
            }
            else
            {
                throw new InvalidOperationException("unknown device type of " + device.GetType().ToString());
            }

            device.Filter = "tcp and port 7777";

            //Console.WriteLine();
            //Console.WriteLine("-- Listening on {0} {1}, 按'ESC'停止...",
            //    device.Name, device.Description);

            // Start the capturing process
            device.StartCapture();
            Console.WriteLine("-- Capture started.");

        }

        public void PacketHandlerTaskStop(Int32 AdapterIndex)
        {
            ICaptureDevice device = adapters[AdapterIndex].device;
            device.StopCapture();
            app.Source.Cancel();
            Console.WriteLine("-- Capture stopped.");
        }
    }
}
