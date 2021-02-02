using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Aliyun.Api.LOG;
using Aliyun.Api.LOG.Common.Communication;
using Aliyun.Api.LOG.Common.Utilities;
using Aliyun.Api.LOG.Data;
using Aliyun.Api.LOG.Request;
using Aliyun.Api.LOG.Response;

namespace L2Trace
{
    public class AlilogUpdate
    {
        public L2Trace.AppData app;
        L2Trace.printLogDelegate md;

        public AlilogUpdate(L2Trace.AppData app)
        {
            this.app = app;
            this.md = app.md;
            Task netUpdater = new Task(HandleSendData);
            netUpdater.Start();
        }

        public void AlilogUpdatePrint(string msg)
        {
            this.md.Invoke(msg);
        }

        public void HandleSendData()
        {
            UInt32 limit = 0;
            while (!this.app.Source.IsCancellationRequested)
            {
                List<SendPaketData> sendData = new List<SendPaketData>();
                while (app.netQueue.Count > 0)
                {
                    var gameData = (SendPaketData)app.netQueue.DeQueue();
                    sendData.Add(gameData);
                    limit += 1;

                    if (limit > 3000)
                    {
                        limit =  0;
                        break;
                    }
                }
                // for(int i = 0; i < sendData.Count; i++)
                // {
                //     Console.WriteLine(sendData[i].player.name);
                // }
                if (sendData.Count > 0)
                    UpdateAlilog(sendData);
                Thread.Sleep(2000);
            }
            Console.WriteLine(this.app.Source.IsCancellationRequested);
        }

        private void UpdateAlilog(List<SendPaketData> data)
        {
            // select you endpoint https://help.aliyun.com/document_detail/29008.html
            String endpoint = "cn-shanghai.log.aliyuncs.com",
                accesskeyId = "", //Add Your owen
                accessKey = "",
                project = "",
                logstore = "";
            LogClient client = new LogClient(endpoint, accesskeyId, accessKey);
            //init http connection timeout
            client.ConnectionTimeout = client.ReadWriteTimeout = 10000;
            //list logstores
            // foreach (String l in client.ListLogstores(new ListLogstoresRequest(project)).Logstores)
            // {
            //     Console.WriteLine(l);
            // }
            //put logs
            PutLogsRequest putLogsReqError = new PutLogsRequest();
            putLogsReqError.Project = project;
            putLogsReqError.Topic = "";
            putLogsReqError.Source = "";
            putLogsReqError.Logstore = logstore;
            putLogsReqError.LogItems = new List<LogItem>();
            for (int i = 0; i < data.Count; i++)
            {
                LogItem logItem = new LogItem();
                LogContent logContent_name = new LogContent();
                logContent_name.Key = "name";
                logContent_name.Value = data[i].player.name;
                logItem.Contents.Add(logContent_name);

                //Console.WriteLine("Update User: {0}", data[i].player.name);

                LogContent logContent_userid = new LogContent();
                logContent_userid.Key = "userid";
                logContent_userid.Value = data[i].player.userid.ToString();
                logItem.Contents.Add(logContent_userid);

                LogContent logContent_clanid = new LogContent();
                logContent_clanid.Key = "clanid";
                logContent_clanid.Value = data[i].player.clanid.ToString();
                logItem.Contents.Add(logContent_clanid);

                LogContent logContent_clanname = new LogContent();
                logContent_clanname.Key = "clanname";
                logContent_clanname.Value = data[i].player.clanname.ToString();
                logItem.Contents.Add(logContent_clanname);

                LogContent logContent_allyname = new LogContent();
                logContent_allyname.Key = "allyname";
                logContent_allyname.Value = data[i].player.allyname.ToString();
                logItem.Contents.Add(logContent_allyname);

                LogContent logContent_srvid = new LogContent();
                logContent_srvid.Key = "srvid";
                logContent_srvid.Value = data[i].player.srvid.ToString();
                logItem.Contents.Add(logContent_srvid);

                LogContent logContent_updater = new LogContent();
                logContent_updater.Key = "updater";
                logContent_updater.Value = data[i].player.updater.ToString();
                logItem.Contents.Add(logContent_updater);

                LogContent logContent_loc = new LogContent();
                logContent_loc.Key = "location";
                logContent_loc.Value = "(" + data[i].player.location.x.ToString() + "," + data[i].player.location.y.ToString() + "," + data[i].player.location.z.ToString() + ")";
                logItem.Contents.Add(logContent_loc);

                logItem.Time = DateUtils.TimeSpan();
                putLogsReqError.LogItems.Add(logItem);

                AlilogUpdatePrint("更新=>" + data[i].player.name);
                //Console.WriteLine("Ali Update: {0} by {1}", data[i].player.name, data[i].player.updater.ToString());
            }

            if (putLogsReqError.LogItems.Count > 0)
            {
                try
                {
                    PutLogsResponse putLogRespError = client.PutLogs(putLogsReqError);
                }
                catch
                {
                    Console.WriteLine("Ali log put Error");
                }
            }
            /*
            UInt32 maxtry = 0;
            while (true)
            {
                try
                {
                    if (maxtry > 1)
                    {
                        Console.WriteLine("Ali log put Error, Reach Maxtry");
                        break;
                    }
                    PutLogsResponse putLogRespError = client.PutLogs(putLogsReqError);
                    break;
                }
                catch
                {
                    maxtry += 1;
                    Thread.Sleep(1000)
                    Console.WriteLine("Ali log put Error");
                }
            }
            */
        }
    }
}
