using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using BilibiliDM_PluginFramework;
using BiliDMLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bililive_dm
{
    public sealed class Plugin_Test: DMPlugin
    {
        public Plugin_Test()
        {
            this.PluginDesc = "这是测试插件";
            this.PluginAuth = "FreeKnight";
            this.PluginCont = "duzhi5368@gmail.com";
            this.PluginName = "Debug测试插件";
            this.PluginVer = "v1.0.0";
            this.ReceivedDanmaku += OnReceivedDanmaku;
            this.Connected += OnConnected;
            this.Disconnected += OnDisconnected;
            this.ReceivedRoomCount += OnReceivedRoomCount;
        }


        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            try
            {
                Console.WriteLine("OnReceivedDanmaku");
                this.Log("OnReceivedDanmaku");
                this.AddDM("OnReceivedDanmaku", true);
            }
            catch (Exception){} 
        }

        private void OnReceivedRoomCount(object sender, BilibiliDM_PluginFramework.ReceivedRoomCountArgs e)
        {
            try
            {
                Console.WriteLine("OnReceivedRoomCount");
                this.Log("OnReceivedRoomCount");
                this.AddDM("OnReceivedRoomCount", true);
            }
            catch (Exception) { }
        }

        private void OnDisconnected(object sender, BilibiliDM_PluginFramework.DisconnectEvtArgs e)
        {
            try
            {
                Console.WriteLine("OnDisconnected");
                this.Log("OnDisconnected");
                this.AddDM("OnDisconnected", true);
            }
            catch (Exception) { }
        }

        private void OnConnected(object sender, BilibiliDM_PluginFramework.ConnectedEvtArgs e)
        {
            try
            {
                Console.WriteLine("OnConnected");
                this.Log("OnConnected");
                this.AddDM("OnConnected", true);
            }
            catch (Exception) { }
        }

        public override void Admin()
        {
            base.Admin();
            Console.WriteLine("Admin");
            this.Log("Admin");
        }

        public override void Stop()
        {
            // 不要阻塞
            base.Stop();
            Console.WriteLine("停止");
            this.Log("停止");
        }

        public override void Start()
        {
            // 不要阻塞
            base.Start();
            Console.WriteLine("启动");
            this.Log("启动");
        }

        public override void Inited()
        {
            // 不要阻塞
            base.Inited();
            Console.WriteLine("初始化");
            this.Log("初始化");
        }

        public override void Destory()
        {
            // 不要阻塞
            base.Destory();
            Console.WriteLine("释放");
            this.Log("释放");
        }
    }
}