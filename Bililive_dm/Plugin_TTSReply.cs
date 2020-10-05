using BilibiliDM_PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Bililive_dm
{
    public partial class Plugin_TTSReply : DMPlugin
    {
        public static bool IsNAudioReady = false;
        public static bool IsEnabled = false;
        public static GiftDebouncer debouncer = new GiftDebouncer();


        public Window mainWnd;
        public AppSettings gSettings;
        public List<TTSEntry> gTTSList;
        public MethodInfo Text_ReciveGift;
        public MethodInfo EngineType;
        public MethodInfo SpeechPerson;

        public Plugin_TTSReply() {
            this.PluginDesc = "自动回复和语音应答插件";
            this.PluginAuth = "FreeKnight";
            this.PluginCont = "duzhi5368@gmail.com";
            this.PluginName = "语音应答插件";
            this.PluginVer = "v1.0.0";
            this.ReceivedDanmaku += OnReceivedDanmaku;
            this.Connected += OnConnected;
            this.Disconnected += OnDisconnected;
            this.ReceivedRoomCount += OnReceivedRoomCount;
            AppDomain.CurrentDomain.UnhandledException += GlobalErrorHandler;
            debouncer.GiftDebouncedEvent += GiftDebouncedEvent;
        }

        private async void GiftDebouncedEvent(object sender, UserGift e)
        {
            var constructedArgs = new ReceivedDanmakuArgs();
            constructedArgs.Danmaku = new DanmakuModel();
            constructedArgs.Danmaku.GiftName = e.Gift;
            constructedArgs.Danmaku.GiftCount = e.Qty;
            constructedArgs.Danmaku.UserName = e.User;
            constructedArgs.Danmaku.UserID = e.UserId;
            constructedArgs.Danmaku.MsgType = MsgTypeEnum.GiftSend;
            await GiftRoute(null, constructedArgs);
        }

        public async Task GiftRoute(object sender, ReceivedDanmakuArgs e)
        {
            await TTSPlayer.UnifiedPlay(ProcessGift(e), Int32.Parse(EngineType.Invoke(gSettings, new object[] { }).ToString()),
                Int32.Parse(SpeechPerson.Invoke(gSettings, new object[] { }).ToString()), gTTSList);
            //await TTSPlayer.PlayVoiceReply(e.Danmaku);
        }

        public static string ProcessGift(DanmakuModel e, string template)
        {
            var rawDanmaku = Preprocess(template, e);
            return rawDanmaku
                .Replace("$GIFT", e.GiftName)
                .Replace("$COUNT", e.GiftCount.ToString())
                .Replace("$USER", e.UserName);
        }
        public string ProcessGift(ReceivedDanmakuArgs e)
        {
            return ProcessGift(e.Danmaku, Text_ReciveGift.Invoke(gSettings, new object[] { }).ToString());
        }

        public static string Preprocess(string format, DanmakuModel e)
        {
            string guardText;
            switch (e.UserGuardLevel)
            {
                default: guardText = StaticConfig.CustomGuardLevel0; break;
                case 1: guardText = StaticConfig.CustomGuardLevel1; break;
                case 2: guardText = StaticConfig.CustomGuardLevel2; break;
                case 3: guardText = StaticConfig.CustomGuardLevel3; break;
            }
            format = format
                .Replace("$$", "$GUARD$")
                .Replace("$!", "$VIP$");
            if (e.isVIP) format = format.Replace("$VIP", StaticConfig.CustomVIP);
            else format = format.Replace("$VIP", "");
            return format.Replace("$GUARD", guardText);
        }

        public static string Preprocess(string format, ReceivedDanmakuArgs e) => Preprocess(format, e.Danmaku);

        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            try
            {
                Console.WriteLine("OnReceivedDanmaku");
                this.Log("OnReceivedDanmaku");
                this.AddDM("OnReceivedDanmaku", true);
            }
            catch (Exception) { }
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
            /*
            mainWnd = System.Windows.Application.Current.MainWindow;
            Type type = mainWnd.GetType();
            FieldInfo info = type.GetField("gSettings");
            gSettings = () => { }
            info.SetValue(mainWnd, gSettings);
            gSettings = (AppSettings)(type.GetField("gSettings",BindingFlags.Instance).GetValue(mainWnd));
            Text_ReciveGift = gSettings.GetType().GetMethod("Text_ReciveGift", BindingFlags.Instance | BindingFlags.Public);
            gTTSList = (List<TTSEntry>)type.GetField("gTTSList", BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic).GetValue(mainWnd);
            EngineType = gSettings.GetType().GetMethod("EngineType", BindingFlags.Instance | BindingFlags.Public);
            SpeechPerson = gSettings.GetType().GetMethod("SpeechPerson", BindingFlags.Instance | BindingFlags.Public);
            */
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
