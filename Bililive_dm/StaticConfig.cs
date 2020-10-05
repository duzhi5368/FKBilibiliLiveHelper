using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bililive_dm
{
    public static class StaticConfig
    {
        public static int MAX_MSG_QUEUE_CAPACITY = 100;
        public static int MAX_MSG_HISTORY_CAPACITY = 10;
        public static int UPDATE_DANMU_THREAD_ELSP = 100;
        public static int TTS_THREAD_ELSP = 100;
        public static int TTS_SPEACH_THREAD_ELSP = 50;
        public static int TTS_DOWNLOAD_FAIL_RETRY = 5;
        public static int TTS_NETFRAMEWORK_SPEECH_SPEED = 5;
        public static int GIFTS_THROTTLE_DURATION = 3;

        public static readonly string ApiBaidu = "https://fanyi.baidu.com/gettts?lan=zh&text=$TTSTEXT&spd=5&source=web";
        public static readonly string ApiYoudao = "http://tts.youdao.com/fanyivoice?word=$TTSTEXT&le=zh&keyfrom=speaker-target";
        public static readonly string ApiBaiduAi = "https://tsn.baidu.com/text2audio?tex=$TTSTEXT&lan=zh&per=$PERSON&spd=$SPEED&pit=$PITCH&cuid=1234567JAVA&ctp=1&tok=$TOKEN";
        public static readonly string ApiGoogle = "https://translate.google.cn/translate_tts";
        public static readonly string ApiBaiduAiAppKey = "4E1BG9lTnlSeIf1NQFlrSq6h"; // thank, ref: https://github.com/Baidu-AIP/speech-demo/blob/master/rest-api-tts/java/src/com/baidu/speech/restapi/ttsdemo/TtsMain.java
        public static readonly string ApiBaiduAiSecretKey = "544ca4657ba8002e3dea3ac2f5fdd241";
        public static readonly string AppDllFileName = Assembly.GetExecutingAssembly().Location;
        public static readonly string AppDllFilePath = (new FileInfo(AppDllFileName)).DirectoryName;
        public static readonly string ConfDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Bilibili直播小助手\Plugins");
        public static readonly string DefaultCacheDir = Path.Combine(ConfDir, "Cache");
        public static readonly string ConfFileName = Path.Combine(ConfDir, "Config.cfg");
        public static readonly string DesktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public static readonly string ErrorLogFilePath = Path.Combine(DesktopDir, @"Bilibili直播小助手错误报告.txt");
        public static readonly string AudioLibraryFileName = Path.Combine(Directory.GetCurrentDirectory(), "NAudio.dll");

        public static string CustomVIP = "老爷";
        public static string CustomGuardLevel0 = "用户";
        public static string CustomGuardLevel1 = "总督";
        public static string CustomGuardLevel2 = "提督";
        public static string CustomGuardLevel3 = "舰长";

        public static bool CatchGlobalError = true;
        public static string ApiBaiduAiAccessToken = string.Empty;
    }
}
