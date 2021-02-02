using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace L2Trace
{
    public class Location
    {
        public int x;
        public int y;
        public int z;
        public int pre_x;
        public int pre_y;
        public int pre_z;
        public Location()
        {
            x = 0;
            y = 0;
            z = 0;
            pre_x = 0;
            pre_y = 0;
            pre_z = 0;
        }
    }

    public class PlayerInfoUpdateMark
    {
        public Boolean name;
        public Boolean srvid;
        public Boolean userid;
        public Boolean clanid;
        public Boolean clanname;
        public Boolean allyname;
        public Boolean location;
        public Boolean distance;
        public Boolean updater;
        public PlayerInfoUpdateMark()
        {
            name = false;
            srvid = false;
            userid = false;
            clanid = false;
            clanname = false;
            allyname = false;
            location = false;
            distance = false;
            updater = false;
        }
    }
    public class PlayerInfo
    {
        public string name;
        public string srvid;
        public UInt32 userid;
        public UInt32 clanid;
        public string clanname;
        public string allyname;
        public Location location;
        public UInt64 distance;
        public String angle;
        public string updater;
        public Boolean isUser;
        public Boolean needUpdateUI;
        public Boolean needUpdateNet;
        public Boolean removeMark;
        public PlayerInfoUpdateMark updateMark;
        public PlayerInfo()
        {
            name = "";
            userid = 0;
            clanid = 0;
            clanname = "";
            allyname = "";
            updater = "";
            location = new Location();
            distance = 0;
            angle = "";
            isUser = false;
            needUpdateUI = false;
            needUpdateNet = false;
            removeMark = false;
            updateMark = new PlayerInfoUpdateMark();
        }
        public void restorePlayerInfoUpdateMark()
        {
            this.updateMark.name = false;
            this.updateMark.srvid = false;
            this.updateMark.userid = false;
            this.updateMark.clanid = false;
            this.updateMark.clanname = false;
            this.updateMark.allyname = false;
            this.updateMark.location = false;
            this.updateMark.distance = false;
            this.updateMark.updater = false;
        }
    }

    public class GameSession
    {
        public string game_server; //= "121.51.218.57";
        public List<uint> _l2key; //= new List<uint> { 0xC8, 0x27, 0x93, 0x01, 0xA1, 0x6C, 0x31, 0x97 };
        public List<uint> _clkey; //= new List<uint> { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public List<uint> _svkey; //= new List<uint> { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        public Int32 srvport; //= 0;
        public Int32 clport; //= 0;
        public UInt32 serverid;
        public Boolean is_first; //= true;
        public DataStream srvstream; //= new DataStream();
        public DataStream clstream;
        public PlayerInfo user;
        public List<PlayerInfo> targets;
        public Dictionary<UInt32, List<string>> claninfo;
        private object sessionlock;
        public L2Trace.AppData app;
        public List<String> whiteList;

        public GameSession(L2Trace.AppData app)
        {
            this.game_server = "121.51.218.57"; //TODO: only support 91
            this._l2key = new List<uint> { 0xC8, 0x27, 0x93, 0x01, 0xA1, 0x6C, 0x31, 0x97 };
            this._clkey = new List<uint> { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            this._svkey = new List<uint> { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            this.srvport = 0;
            this.clport = 0;
            this.serverid = 0;
            this.is_first = true;
            this.srvstream = new DataStream();
            this.clstream = new DataStream();
            this.user = new PlayerInfo();
            this.targets = new List<PlayerInfo>();
            this.claninfo = new Dictionary<UInt32, List<string>>();
            this.sessionlock = new object();
            this.app = app;
            this.whiteList = new List<string>();
            Task updatePlayer = new Task(UpdateUserListTask);
            updatePlayer.Start();
        }

        public void UpdateClanInfo(UInt32 clanid, string clanname, string allyname)
        {
            lock(this.sessionlock)
            {
                if (this.claninfo.ContainsKey(clanid) == false)
                {
                    List<String> sub_info = new List<String>();
                    sub_info.Add(clanname);
                    sub_info.Add(allyname);
                    this.claninfo.Add(clanid, sub_info);
                }
                else
                {
                    if (clanname != "")
                        this.claninfo[clanid][0] = clanname;
                    if (allyname != "")
                        this.claninfo[clanid][1] = allyname;
                }
            }
        }

        public void UpdatePlayerInfo(PlayerInfo info)
        {
            Boolean found = false;
            lock(this.sessionlock)
            {
                if ((info.isUser == true) || (info.userid == this.user.userid))
                {
                    //Self update, Only for location
                    if (info.updateMark.location == true)
                    {
                        this.user.location.pre_x = this.user.location.x;
                        this.user.location.pre_y = this.user.location.y;
                        this.user.location.pre_z = this.user.location.z;
                        this.user.location.x = info.location.x;
                        this.user.location.y = info.location.y;
                        this.user.location.z = info.location.z;
                        //Need to update all distance
                        for (int i = 0; i < this.targets.Count; i++)
                        {
                            this.targets[i].distance = GetPlayerDistance(this.targets[i].location, this.user.location);
                            //this.targets[i].angle = JudgePointPosition(this.targets[i].location, this.user.location);
                            this.targets[i].needUpdateUI = true;
                        }
                    }
                    if (info.updateMark.name == true)
                        this.user.name = info.name;

                    if (info.updateMark.userid == true)
                        this.user.userid = info.userid;
                    
                    if (info.updateMark.clanid == true)
                        this.user.clanid = info.clanid;

                    if (info.updateMark.clanname == true)
                        this.user.clanname = info.clanname;

                    if (info.updateMark.allyname == true)
                        this.user.allyname = info.allyname;

                    if (info.removeMark == true)
                    {
                        for (int i = 0; i < this.targets.Count; i++)
                        {
                            this.targets[i].removeMark = true;
                            this.targets[i].needUpdateUI = true;
                        }
                    }
                }
                else
                {
                    //Console.WriteLine(this.targets.ToString());
                    for (int i = 0; i < this.targets.Count; i++)
                    {
                        if (info.userid == this.targets[i].userid)
                        {
                            if (info.removeMark == true)
                            {
                                this.targets[i].removeMark = true;
                                this.targets[i].needUpdateUI = true;
                            }
                            else
                            {
                                if (info.updateMark.clanid == true)
                                {
                                    this.targets[i].clanid = info.clanid;
                                    this.targets[i].clanname = GetClanNameByClanId(info.clanid);
                                    this.targets[i].allyname = GetAllyNameByClanId(info.clanid);
                                    //Console.WriteLine("MOD: clanname = {0}", info.clanname);
                                    this.targets[i].needUpdateUI = true;
                                    this.targets[i].needUpdateNet = true;
                                }
                                if (info.updateMark.name == true)
                                {
                                    this.targets[i].name = info.name;
                                    this.targets[i].needUpdateUI = true;
                                    this.targets[i].needUpdateNet = true;
                                }
                                if (info.updateMark.srvid == true)
                                {
                                    this.targets[i].srvid = info.srvid;
                                    this.targets[i].needUpdateNet = true;
                                }
                                if (info.updateMark.location == true)
                                {
                                    this.targets[i].location.pre_x = this.targets[i].location.x;
                                    this.targets[i].location.pre_y = this.targets[i].location.y;
                                    this.targets[i].location.pre_z = this.targets[i].location.z;
                                    this.targets[i].location.x = info.location.x;
                                    this.targets[i].location.y = info.location.y;
                                    this.targets[i].location.z = info.location.z;
                                    this.targets[i].distance = GetPlayerDistance(this.targets[i].location, this.user.location);
                                    //this.targets[i].angle = JudgePointPosition(this.targets[i].location, this.user.location);
                                    this.targets[i].needUpdateUI = true;
                                    this.targets[i].needUpdateNet = true;
                                }
                            }
                            found = true;
                            break;
                        }
                    }

                    if ((found == false) && (info.name != "")) //name = "" is mob
                    { 
                        info.needUpdateUI = true;
                        info.needUpdateNet = true;
                        info.clanname = GetClanNameByClanId(info.clanid);
                        info.allyname = GetAllyNameByClanId(info.clanid);
                        info.distance = GetPlayerDistance(info.location, this.user.location);
                        //info.angle = JudgePointPosition(info.location, this.user.location);
                        info.updater = this.user.name;
                        info.restorePlayerInfoUpdateMark();
                        //Console.WriteLine("ADD: clanname = {0}", info.clanname);
                        this.targets.Add(info);
                    }


                }
            }
        }
/*
        public void UpdatePlayerInfo(PlayerInfo info)
        {
            Boolean found = false;
            lock(this.sessionlock)
            {
                if ((info.userid == this.user.userid) || (this.user.userid == 0))
                {
                    //Self update, Only for location
                    if ((info.location.x != 0) || (info.location.y != 0) || (info.location.z != 0))
                    {
                        this.user.location.x = info.location.x;
                        this.user.location.y = info.location.y;
                        this.user.location.z = info.location.z;
                        //Need to update all distance
                        for (int i = 0; i < this.targets.Count; i++)
                        {
                            this.targets[i].distance = GetPlayerDistance(this.targets[i].location, this.user.location);
                            this.targets[i].needUpdate = true;
                        }
                    }
                    if (info.name != "")
                        this.user.name = info.name;

                    if (info.userid != 0)
                        this.user.userid = info.userid;
                }
                else
                {
                    for (int i = 0; i < this.targets.Count; i++)
                    {
                        if (info.userid == this.targets[i].userid)
                        {
                            if (((this.targets[i].clanname == "") || (this.targets[i].allyname == "")) && (info.clanid != 0))
                            {
                                this.targets[i].clanid = info.clanid;
                                this.targets[i].clanname = GetClanNameByClanId(info.clanid);
                                this.targets[i].allyname = GetAllyNameByClanId(info.clanid);
                                //Console.WriteLine("MOD: clanname = {0}", info.clanname);
                                this.targets[i].needUpdate = true;
                            }
                            if ((info.name != this.targets[i].name) && (info.name != ""))
                            {
                                this.targets[i].name = info.name;
                                this.targets[i].needUpdate = true;
                            }
                            if ((info.srvid != this.targets[i].srvid) && (info.srvid != ""))
                            {
                                this.targets[i].srvid = info.srvid;
                                this.targets[i].needUpdate = true;
                            }
                            if ((info.location.x != this.targets[i].location.x) || (info.location.y != this.targets[i].location.y) || (info.location.z != this.targets[i].location.z)
                                && (info.location.x != 0) && (info.location.y != 0) && (info.location.z != 0)  )
                            {
                                this.targets[i].location.x = info.location.x;
                                this.targets[i].location.y = info.location.y;
                                this.targets[i].location.z = info.location.z;
                                this.targets[i].distance = GetPlayerDistance(this.targets[i].location, this.user.location);
                                this.targets[i].needUpdate = true;
                            }
                            found = true;
                            break;
                        }
                    }
                    if ((found == false) && (info.name != "")) //name = "" is mob
                    {
                        info.needUpdate = true;
                        info.clanname = GetClanNameByClanId(info.clanid);
                        info.allyname = GetAllyNameByClanId(info.clanid);
                        info.distance = GetPlayerDistance(info.location, this.user.location);
                        info.updater = this.user.name;
                        //Console.WriteLine("ADD: clanname = {0}", info.clanname);
                        this.targets.Add(info);
                    }
                }
            }
        }
*/
        private string GetClanNameByClanId(UInt32 clanid)
        {
            string ret = "";
            if (this.claninfo.ContainsKey(clanid))
            {
                ret = this.claninfo[clanid][0];
            }
            return ret;
        }

        private UInt64 GetPlayerDistance(Location srcloc, Location dstloc)
        {

            Double dSquareSum;
            dSquareSum = Math.Pow(srcloc.x - dstloc.x, 2) + Math.Pow(srcloc.y - dstloc.y, 2) + Math.Pow(srcloc.z - dstloc.z, 2);
            dSquareSum = Math.Sqrt(dSquareSum);
            //Console.WriteLine("Distance: {0}", (UInt64)dSquareSum);

            return (UInt64)dSquareSum;
        }

        
        public static Location GetFootOfPerpendicular(Location pt, Location begin, Location end)
        {
            Location ret = new Location();
            double dx = begin.x - end.x;
            double dy = begin.y - end.y;
            if (Math.Abs(dx) < 0.00000001 && Math.Abs(dy) < 0.00000001)
            {
                return begin;
            }
            double u = (pt.x - begin.x) * (begin.x - end.x) + (pt.y - begin.y) * (begin.y - end.y);
            u = u / ((dx * dx) + (dy * dy));

            ret.x = (Int32)(begin.x + u * dx);
            ret.y = (Int32)(begin.y + u * dy);

            return ret;
        }


        private String JudgePointPosition(Location srcloc, Location dstloc)
        {
            String nResult = "";
            int nResult1 = 0;
            int nResult2 = 0;
            Location tp;
            Location p0 = new Location();
            Location p1 = new Location();
            Location p2 = new Location();

            double ax = dstloc.x - dstloc.pre_x;
            double ay = dstloc.y - dstloc.pre_y;
            double bx = srcloc.x - dstloc.pre_x;
            double by = srcloc.y - dstloc.pre_y;
            double judge = ax * by - ay * bx;
            if (judge > 0)
            {
                nResult1 = 1;
            }
            else if (judge < 0)
            {
                nResult1 = -1;
            }
            else
            {
                nResult1 = 0;
            }

            p0.x = srcloc.x;
            p0.y = srcloc.y;
            p1.x = dstloc.pre_x;
            p1.y = dstloc.pre_y;
            p2.x = dstloc.x;
            p2.y = dstloc.y;

            tp = GetFootOfPerpendicular(p0, p1, p2);

            ax = p0.x - tp.x;
            ay = p0.y - tp.y;
            bx = p2.x - p0.x;
            by = p2.y - p0.y;
            judge = ax * by - ay * bx;

            if (judge > 0)
            {
                nResult2 = 1;
            }
            else if (judge < 0)
            {
                nResult2 = -1;
            }
            else
            {
                nResult2 = 0;
            }

            if ((nResult1 == 0) || (nResult2 == 0))
            {
                nResult = "";
            }
            else if ((nResult1 == -1) && (nResult2 == -1))
            {
                nResult = "↖";
            }
            else if ((nResult1 == 1) && (nResult2 == 1))
            {
                nResult = "↗";
            }
            else if ((nResult1 == 1) && (nResult2 == -1))
            {
                nResult = "↘";
            }
            else if ((nResult1 == -1) && (nResult2 == 1))
            {
                nResult = "↙";
            }
            else
            {
                nResult = "";
            }

            return nResult;
        }

        private string GetAllyNameByClanId(UInt32 clainid)
        {
            string ret = "";

            if (this.claninfo.ContainsKey(clainid))
            {
                ret = this.claninfo[clainid][1];
            }
        
            return ret;
        }

        private string GetPlayerClanNameByPlayerId(UInt32 playerid)
        {
            string ret = "";

            for (int i = 0; i < this.targets.Count; i++)
            {
                if (playerid == this.targets[i].userid)
                {
                    ret = this.targets[i].clanname;
                    break;
                }
            }
        
            return ret;
        }

        private UInt32 GetPlayerClanIdByPlayerId(UInt32 playerid)
        {
            UInt32 ret = 0;

            for (int i = 0; i < this.targets.Count; i++)
            {
                if (playerid == this.targets[i].userid)
                {
                    ret = this.targets[i].clanid;
                    break;
                }
            }

            return ret;
        }
        
        /*
        public void RemovePlayerInfo(UInt32 id)
        {
            PlayerInfo player = new PlayerInfo();
            lock(this.sessionlock)
            {
                for (int i = this.targets.Count - 1; i >= 0; i--)
                {
                    if ((Int32)id == this.targets[i].userid)
                    {   
                        
                        //player.userid = id;
                        //player.name = this.targets[i].name;
                        //player.removeMark = true;
                        //player.needUpdate = true;
                        this.targets[i].removeMark = true;
                        this.targets[i].needUpdate = true;
                        //this.app.up(player);
                        //this.targets.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        */
        
        public void UpdateUserListTask()
        {
            while (!this.app.Source.IsCancellationRequested)
            {
                //Console.WriteLine("UpdateUserListTask");
                lock(this.sessionlock)
                {
                    for (int i = this.targets.Count - 1; i >= 0; i--)
                    {
                        if (this.targets[i].needUpdateUI == true)
                        {
                            PlayerInfo player = new PlayerInfo();
                            player.name = this.targets[i].name;
                            player.location = this.targets[i].location;
                            player.distance = this.targets[i].distance;
                            player.removeMark = this.targets[i].removeMark;
                            player.clanid = this.targets[i].clanid;
                            player.clanname = this.targets[i].clanname;
                            player.allyname = this.targets[i].allyname;
                            player.updater = this.targets[i].updater;
                            player.angle = this.targets[i].angle;

                            if (this.targets[i].needUpdateNet == true)
                            {
                                this.targets[i].needUpdateNet = false;
                                SendPaketData sendData = new SendPaketData();
                                sendData.player.userid = player.userid;
                                sendData.player.name = player.name;
                                sendData.player.location.x = player.location.x;
                                sendData.player.location.y = player.location.y;
                                sendData.player.location.z = player.location.z;
                                sendData.player.clanid = player.clanid;
                                sendData.player.clanname = player.clanname;
                                sendData.player.allyname = player.allyname;
                                sendData.player.updater = player.updater;
                                sendData.player.srvid = this.serverid.ToString();

                                try 
                                {
                                    app.netQueue.EnQueue(sendData);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }


                            if (player.distance > 6000)
                            {
                                player.removeMark = true;
                            }

                            for (int j = 0;  j < this.whiteList.Count; j++)
                            {
                                if (player.allyname == this.whiteList[j])
                                {
                                    player.removeMark = true;
                                    break;
                                }
                            }

                            this.app.up(player);

                            if (player.removeMark == true)
                            {
                                this.targets.RemoveAt(i);
                            }
                            else
                            {
                                this.targets[i].needUpdateUI = false;
                            }

                        }
                    }
                }
                Thread.Sleep(1000);
            }
            Console.WriteLine(app.Source.IsCancellationRequested);
        }
    }

}
