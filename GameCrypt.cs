using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace L2Trace
{
    public class GameCrypt
    {
        //public static Dictionary<UInt32, List<string>> ClanInfo = new Dictionary<UInt32, List<string>>();
        L2Trace.printLogDelegate md;
        public GameCrypt(L2Trace.printLogDelegate md)
        {
            this.md = md;
        }
        
        public void GameCryptPrint(string msg)
        {
            this.md.Invoke(msg);   
        }

        public Boolean clDecryptPacket(GameSession session, ref GamePaketData gamepkt)
        {
            return true;
        }

        public Boolean srvDecryptPacket(GameSession session, ref GamePaketData gamepkt)
        {
            Utility util = new Utility();
            DataStream stream = session.srvstream;
            //Console.WriteLine(BitConverter.ToString(tcpPacket.PayloadData, 0).Replace("-", string.Empty).ToLower());
            UInt32 pkgSize = util.ReadStream16(stream, false);
            //Console.WriteLine("[S]: Packet Size: {0} : {1}",pkgSize, stream.data.GetBufferUsedSize());

            if (pkgSize > stream.data.GetBufferUsedSize())
                return true;

            byte[] pktData = util.ReadStreamArray(stream, pkgSize, true);

            byte cmd;
            UInt32 tsize = (UInt32)(pktData[0] | pktData[1] << 8);
            cmd = pktData[2];
            //Console.WriteLine("[S]: tsize Size: {0}", tsize);
            //Console.WriteLine("pkgsize: {0}, cmd : 0x{1:x}", pkgSize, cmd);

            //Console.WriteLine(BitConverter.ToString(pktData, 0).Replace("-", string.Empty).ToLower());            

            //byte cmd = ReadStreamByte(stream);

            if (stream.key_ready == false)
            {
                cmd = pktData[2];
                //pos = 0
                //Console.WriteLine("pkgsize: {0}, cmd : 0x{1:x}", pkgSize, cmd);
                //detect init key
                if (cmd == 0x2e && pkgSize == 28)
                {
                    GameCryptPrint("[信息]: 找到游戏包解码Key");
                    //Console.WriteLine("[S]: Key Pkg found");
                }
                else
                {
                    Console.WriteLine("[S]: No key found in Pkg");
                    return true;
                }

                byte proto = pktData[3]; //proto pos = 1

                for (int i = 0; i < 8; i++)
                {
                    session._svkey[i] = pktData[4 + i];
                }

                for (int i = 0; i < 8; i++)
                {
                    session._svkey[8 + i] = session._l2key[i];
                }

                for (int i = 0; i < 16; i++)
                {
                    session._clkey[i] = session._svkey[i];
                }
                //pos = 9
                //ReadStream32(stream); //Unknown pos = 13                
                UInt32 srvid = (UInt32)(pktData[16] | pktData[17] << 8 | pktData[18] << 16 | pktData[19] << 24);
                GameCryptPrint("登录服务器:" + srvid.ToString());
                session.serverid = srvid;
                //Console.WriteLine("Server ID = {0}", srvid);
                stream.key_ready = true;
                return true;
            }
            else
            {
                //byte [] pktdata = ReadStreamArray(stream, (UInt32)pkgSize - 2);
                byte[] decrpytdata = new byte[pkgSize];
                byte c1 = 0;
                byte c2 = 0;
                decrpytdata[0] = pktData[0];
                decrpytdata[1] = pktData[1]; //pkgSize
                                             //cmd = pktData[2];

                for (int i = 0; i < decrpytdata.Length - 2; i++)
                {
                    //c2 = pktdata[i];
                    c2 = pktData[i + 2];
                    decrpytdata[i + 2] = (byte)(c2 ^ session._svkey[i & 15] ^ c1);
                    c1 = c2;
                }

                UInt32 tmp_key;
                tmp_key = session._svkey[8] & 0xFF;
                tmp_key |= ((session._svkey[9] << 0x8) & 0xFF00);
                tmp_key |= ((session._svkey[10] << 0x10) & 0xFF0000);
                tmp_key |= ((session._svkey[11] << 0x18) & 0xFF000000);

                tmp_key = (UInt32)(tmp_key + pkgSize - 2);

                session._svkey[8] = tmp_key & 0xFF;
                session._svkey[9] = ((tmp_key >> 0x8) & 0xFF);
                session._svkey[10] = ((tmp_key >> 0x10) & 0xFF);
                session._svkey[11] = ((tmp_key >> 0x18) & 0xFF);

                //GamePaketData gamepkt = new GamePaketData();
                gamepkt.decypt_data = decrpytdata;
                //getGameDataSync(gamepkt);
                //dataQueue.EnQueue(gamepkt);
                return false;
            }
            
        }
    }
}
