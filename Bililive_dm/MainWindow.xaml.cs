using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Deployment.Application;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Serialization;
using BilibiliDM_PluginFramework;
using BiliDMLib;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.Extensions;
using System.Net.Http;

using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = System.Windows.Controls.ContextMenu;
using DataGrid = System.Windows.Controls.DataGrid;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Controls.Primitives;

namespace Bililive_dm
{
    using static WINAPI.USER32;

    /// <summary>
    ///     MainWindow.xaml
    /// </summary>
    public partial class MainWindow : StyledWindow
    {
        
        private readonly Queue<DanmakuModel> gDanmakuQueue = new Queue<DanmakuModel>();
        private readonly ObservableCollection<string> gMessageQueue = new ObservableCollection<string>();
        private readonly DanmakuLoader gDanmakuLoader = new DanmakuLoader();
        private readonly StaticModel gStatic = new StaticModel();
        private readonly ObservableCollection<GiftRank> gRanking = new ObservableCollection<GiftRank>();
        private readonly ObservableCollection<SessionItem> gSessionItems = new ObservableCollection<SessionItem>();

        private IDanmakuWindow gFulloverlay;                            // 弹幕层对象
        private MainOverlay gOverlay;                                   // 弹幕层对象
        private Thread gProcDanmakuThread;                              // 弹幕处理线程
        private Thread gReleaseMemoryThread;                            // 内存释放线程
        private Thread gTTSThread;                                      // 语音播报线程
        private StoreModel gDanmuSettings;                              // 设定存储对象
        private DispatcherTimer gOneSecondTimer;                        // 秒定时器
        private User gUser;                                             // 用户对象
        private Collection<ResourceDictionary> gSkinDictionary;         // 皮肤列表
        private int gSendMsgHistoryIndex = 0;                           // 发送语言历史索引
        private List<string> gSendMsgHistoryList = new List<string>();  // 发送语言历史列表
        private List<TTSEntry> gTTSList = new List<TTSEntry>();         // 语音播报列表
        public AppSettings gSettings;                                   // 聊天对象设定

        public Queue<DanmakuModel> DanmakuQueue() { return gDanmakuQueue; }
        public StaticModel GetStaticModel() { return gStatic; }
        public MainOverlay GetMainOverlay() { return gOverlay; }
        public bool IsDebugMode { get; private set; }                   // 是否是DEBUG模式
        private bool isFullOverlayEnabled;
        private bool isOverlayEnabled = true;
        private bool isSaveLogEnabled = true;
        private bool isNAudioReady = false;                             // 是否已经安装了NAudio.dll

        public MainWindow()
        {
            InitializeComponent();

            // 初始化检查.net
            BaseFunc.Get45or451FromRegistry();
            if (!BaseFunc.IsNet461()){
                MessageBox.Show(this,Properties.Resources.MainWindow_MainWindow_NetError);
            }

            // 初始化皮肤列表
            gSkinDictionary = Resources.MergedDictionaries;
            gSkinDictionary.Add(new ResourceDictionary());

            // 确定当前是否是测试版
            var cmd_args = Environment.GetCommandLineArgs();
            IsDebugMode = cmd_args.Contains("-d") || cmd_args.Contains("--debug");

            // 读取加载
            try
            {
                this.RoomId.Text = Properties.Settings.Default.roomId.ToString();
            }
            catch
            {
                this.RoomId.Text = "";
            }

            // 更新标题
            UpdateTitle();

            // 事件注册
            Closed += OnMainWindowClosed;
            Loaded += OnMainWindowLoaded;
            gDanmakuLoader.Disconnected += OnLoaderDisconnected;
            gDanmakuLoader.ReceivedDanmaku += OnReceivedDanmaku;
            gDanmakuLoader.ReceivedRoomCount += OnReceivedRoomCount;
            gDanmakuLoader.LogMessage += OnReciveLogMessage;
            Log.Loaded += (sender, args) =>
            {
                var sc = Log.Template.FindName("LogScroll", Log) as ScrollViewer;
                sc?.ScrollToEnd();
            };

            // 每秒提高一次层级
            gOneSecondTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, OnOneSecondTimer, Dispatcher);
            gOneSecondTimer.Start();

            // UI数据初始化
            DataGrid2.ItemsSource = gSessionItems;
            Log.DataContext = gMessageQueue;
            PluginGrid.ItemsSource = App.Plugins;
            StaticPanel.DataContext = gStatic;
            for (var i = 0; i < 100; i++)
            {
                gMessageQueue.Add("");
            }

            // 初始日志
            if (!BaseFunc.IsNet461())
            {
                ErrorLogging(Properties.Resources.MainWindow_MainWindow_NetError);
            }
            Logging(Properties.Resources.MainWindow_MainWindow_公告1, false);
            Logging(Properties.Resources.MainWindow_MainWindow_公告2, false);
            Logging(Properties.Resources.MainWindow_MainWindow_公告3, false);
            Logging(Properties.Resources.MainWindow_MainWindow_公告4, false);
            Logging(Properties.Resources.MainWindow_MainWindow_公告5, false);
            if (IsDebugMode)
            {
                Logging(Properties.Resources.MainWindow_MainWindow_当前为Debug模式, false);
            }

            // 初始化语音组
            InitiateTTSEnvironment();

            // 初始化插件组
            Plugins_Init();
        }

        ~MainWindow()
        {
            if (gFulloverlay != null)
            {
                gFulloverlay.Dispose();
                gFulloverlay = null;
            }
        }

        #region =========================== 功能函数 ===========================
        private void StartMemoryClearThread()
        {
            // 初始化定时内存清理线程
            gReleaseMemoryThread = new Thread(() =>
            {
                while (true)
                {
                    Utils.ReleaseMemory(true);
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                }
            })
            { IsBackground = true };
            gReleaseMemoryThread.Start();
        }

        private void StartProcDanmuThread() {
            // 弹幕线程
            gProcDanmakuThread = new Thread(() =>
            {
                while (true)
                {
                    lock (gDanmakuQueue)
                    {
                        var count = 0;
                        if (gDanmakuQueue.Any())
                        {
                            count = (int)Math.Ceiling(gDanmakuQueue.Count / 30.0);
                        }

                        for (var i = 0; i < count; i++)
                        {
                            if (gDanmakuQueue.Any())
                            {
                                var danmaku = gDanmakuQueue.Dequeue();
                                ProcDanmaku(danmaku);
                                if (danmaku.MsgType == MsgTypeEnum.Comment)
                                {
                                    lock (gStatic)
                                    {
                                        gStatic.DanmakuCountShow += 1;
                                        gStatic.AddUser(danmaku.UserName);
                                    }
                                }
                            }
                        }
                    }

                    Thread.Sleep(StaticConfig.UPDATE_DANMU_THREAD_ELSP);
                }
            })
            { IsBackground = true };
            gProcDanmakuThread.Start();
        }

        private void CreateTSThread()
        {
            gTTSThread = new Thread(() =>
            {
                Logging("播放器已启动", false);
                while (gSettings.IsUseTTS)
                {
                    if (gTTSList.Count != 0)
                    {
                        var fileName = gTTSList[0].Filename;
                        try
                        {
                            TTSPlayer.PlayTTS(fileName, gSettings.TTSVolume, gSettings.IsUseTTS, true, gTTSList[0].DoNotDelete);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogging($"无法读取/播放文件 {Path.GetFileName(fileName)}, 放弃: {ex.Message}");
                        }
                        if (gTTSList.Count > 0) 
                            gTTSList.RemoveAt(0);
                    }
                    try { Thread.Sleep(StaticConfig.TTS_THREAD_ELSP); } catch { }
                }
                Logging("播放器已停止", false);
                gSettings.IsUseTTS = true;
            })
            { IsBackground = true };
        }

        private void StopTTS() {
            Logging("准备关闭播放器", false);
            var frame = new DispatcherFrame();
            var tmpThread = new Thread(() => {
                Logging("正在等待播放器停止", false);
                gSettings.IsUseTTS = false;
                while (this.gTTSThread.IsAlive)
                {
                    Thread.Sleep(100);
                }
                frame.Continue = false;
            });
            tmpThread.Start();
            Dispatcher.PushFrame(frame);
        }

        private void UpdateTitle() {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                Title += Properties.Resources.MainWindow_MainWindow____版本号__ + ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            else
            {

                Title += Properties.Resources.MainWindow_MainWindow____标准版本_;
#if !DEBUG
                if(!(Debugger.IsAttached || offline_mode))
                {
                    MessageBox.Show(Application.Current.MainWindow, Properties.Resources.MainWindow_MainWindow_你的打开方式不正确);
                    this.Close();
                }
#endif
            }
            if (IsDebugMode)
            {
                Title += Properties.Resources.MainWindow_MainWindow_____Debug模式_;
            }
            Title += Properties.Resources.MainWindow_MainWindow____编译时间__ + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void LoadConfig() {
            // 弹幕配置
            try
            {
                var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User |
                                                            IsolatedStorageScope.Domain |
                                                            IsolatedStorageScope.Assembly, null, null);
                var settingsreader =
                    new XmlSerializer(typeof(StoreModel));
                var reader = new StreamReader(new IsolatedStorageFileStream(
                    "settings.xml", FileMode.Open, isoStore));
                gDanmuSettings = (StoreModel)settingsreader.Deserialize(reader);
                reader.Close();
            }
            catch (Exception)
            {
                gDanmuSettings = new StoreModel();
            }
            gDanmuSettings.SaveConfig();
            gDanmuSettings.toStatic();

            // 聊天配置
            if (!Directory.Exists(StaticConfig.ConfDir))
            {
                Directory.CreateDirectory(StaticConfig.ConfDir);
            }
            gSettings = new AppSettings(StaticConfig.ConfFileName);
            try
            {
                gSettings.LoadConfig();
            }
            catch (Exception) {}
            gSettings.SaveConfig();
            
            (this.FindName("ShowLevelBox") as CheckBox).IsChecked = gSettings.LogLevel;
            (this.FindName("ShowEnterBox") as CheckBox).IsChecked = gSettings.LogEnter;
            (this.FindName("ShowMedalBox") as CheckBox).IsChecked = gSettings.LogMedal;
            (this.FindName("ShowTitleBox") as CheckBox).IsChecked = gSettings.LogTitle;
            (this.FindName("ShowFollowBox") as CheckBox).IsChecked = gSettings.LogFollow;
            (this.FindName("ShowGiftsBox") as CheckBox).IsChecked = gSettings.ShowGifts;
            (this.FindName("ShowExternBox") as CheckBox).IsChecked = gSettings.LogExternInfo;
            (this.FindName("LevelShieldCheckBox") as CheckBox).IsChecked = gSettings.EnableShieldLevel;
            (this.FindName("LevelShieldTextBox") as TextBox).Text = gSettings.ShieldLevel.ToString();
            (this.FindName("UseTTS") as CheckBox).IsChecked = gSettings.IsUseTTS;
            (this.FindName("EngineType_CB") as ComboBox).SelectedIndex = gSettings.EngineType;
            (this.FindName("SpeechPerson_CB") as ComboBox).SelectedIndex = gSettings.SpeechPerson;
            (this.FindName("TTSVolumeSlider") as Slider).Value = gSettings.TTSVolume;


            (this.FindName("TB_Use_Sound_UserEnterRoom") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserEnterRoom;
            (this.FindName("TB_Use_Text_UserEnterRoom_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Text_UserEnterRoom_Reply;
            (this.FindName("TB_Use_Sound_UserEnterRoom_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserEnterRoom_Reply;
            (this.FindName("TB_Use_Sound_UserFollow") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserFollow;
            (this.FindName("TB_Use_Text_UserFollow_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Text_UserFollow_Reply;
            (this.FindName("TB_Use_Sound_UserFollow_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserFollow_Reply;
            (this.FindName("TB_Use_Sound_UserShare") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserShare;
            (this.FindName("TB_Use_Text_UserShare_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Text_UserShare_Reply;
            (this.FindName("TB_Use_Sound_UserShare_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserShare_Reply;
            (this.FindName("TB_Use_Sound_UserSpecialFollow") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserSpecialFollow;
            (this.FindName("TB_Use_Text_UserSpecialFollow_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Text_UserSpecialFollow_Reply;
            (this.FindName("TB_Use_Sound_UserSpecialFollow_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserSpecialFollow_Reply;
            (this.FindName("TB_Use_Sound_UserSuperChat") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserSuperChat;
            (this.FindName("TB_Use_Sound_UserChat") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_UserChat;
            (this.FindName("TB_Use_Sound_ReciveGift") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_ReciveGift;
            (this.FindName("TB_Use_Text_ReciveGift_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Text_ReciveGift_Reply;
            (this.FindName("TB_Use_Sound_ReciveGift_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_ReciveGift_Reply;
            (this.FindName("TB_Use_Sound_GuardBuy") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_GuardBuy;
            (this.FindName("TB_Use_Text_GuardBuy_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Text_GuardBuy_Reply;
            (this.FindName("TB_Use_Sound_GuardBuy_Reply") as ToggleButton).IsChecked = gSettings.Is_Use_Sound_GuardBuy_Reply;
        }

        private void SetWindowAffinity()
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            SetWindowDisplayAffinity(wndHelper.Handle, Store.DisplayAffinity ? WINAPI.USER32.WindowDisplayAffinity.ExcludeFromCapture : 0);
        }

        private void CreateFakeComment()
        {
            var ran = new Random();
            var n = ran.Next(100);
            if (n > 98)
            {
                AddDMText(Properties.Resources.MainWindow_connbtn_Click_彈幕姬本身, Properties.Resources.MainWindow_Test_OnClick_這不是個測試, false);
            }
            else
            {
                AddDMText(Properties.Resources.MainWindow_connbtn_Click_彈幕姬本身, Properties.Resources.MainWindow_Test_OnClick_這是一個測試, false);
            }

            foreach (var dmPlugin in App.Plugins.Where(dmPlugin => dmPlugin.Status))
            {
                new Thread(() =>
                {
                    try
                    {
                        var m = new ReceivedDanmakuArgs
                        {
                            Danmaku = new DanmakuModel
                                {
                                    CommentText = Properties.Resources.MainWindow_Test_OnClick_插件彈幕測試,
                                    UserName = "Bilibili直播小助手",
                                    MsgType = MsgTypeEnum.Comment
                                }
                        };
                        dmPlugin.MainReceivedDanMaku(m);
                    }
                    catch (Exception ex)
                    {
                        Utils.PluginExceptionHandler(ex, dmPlugin);
                    }
                }).Start();
            }
        }

        private void AddText2TextList(string text)
        {
            while (gSendMsgHistoryList.Count >= StaticConfig.MAX_MSG_HISTORY_CAPACITY)
                gSendMsgHistoryList.RemoveAt(0);
            gSendMsgHistoryList.Add(text);
        }

        private void InitiateTTSEnvironment() {
            if (!Directory.Exists(StaticConfig.ConfDir))
            {
                Directory.CreateDirectory(StaticConfig.ConfDir);
            }
            if (!Directory.Exists(StaticConfig.DefaultCacheDir))
            {
                Directory.CreateDirectory(StaticConfig.DefaultCacheDir);
            }
            try
            {
                if (!File.Exists(StaticConfig.AudioLibraryFileName) || !VerifyLibraryIntegrity())
                {
                    var writer = new FileStream(StaticConfig.AudioLibraryFileName, FileMode.OpenOrCreate);
                    writer.Write(Properties.Resources.NAudio, 0, Properties.Resources.NAudio.Length);
                    writer.Close();
                }
                if (!isNAudioReady)
                {
                    Assembly.LoadFrom(StaticConfig.AudioLibraryFileName);
                    isNAudioReady = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"错误！\n\n无法正常处理支持库: {ex.Message}\n\n请尝试重新启动Bilibili直播小助手，重置配置文件或检查文件读写权限");
                throw ex;
            }
        }

        private bool VerifyLibraryIntegrity()
        {
            if (!File.Exists(StaticConfig.AudioLibraryFileName)) 
                return false;
            using (var stream = File.OpenRead(StaticConfig.AudioLibraryFileName))
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                if (data.SequenceEqual(Properties.Resources.NAudio))
                {
                    return true;
                }
                return false;
            }
        }

        #endregion

        #region =========================== 插件函数 ===========================
        private void Plugin_Enable(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            var contextMenu = (ContextMenu)menuItem.Parent;

            var item = (DataGrid)contextMenu.PlacementTarget;
            if (item.SelectedCells.Count == 0) return;
            var plugin = item.SelectedCells[0].Item as DMPlugin;
            if (plugin == null) return;

            try
            {
                if (!plugin.Status) plugin.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Properties.Resources.MainWindow_Plugin_Enable_插件報錯, plugin.PluginName, plugin.PluginAuth, plugin.PluginCont));
                try
                {
                    using (var outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                    {
                        outfile.WriteLine(Properties.Resources.MainWindow_Plugin_Enable_請有空發給聯繫方式__0__謝謝, plugin.PluginCont);
                        outfile.WriteLine(DateTime.Now + " " + plugin.PluginName + " " + plugin.PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void Plugin_Disable(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;

            var contextMenu = (ContextMenu)menuItem.Parent;

            var item = (DataGrid)contextMenu.PlacementTarget;
            if (item.SelectedCells.Count == 0) return;
            var plugin = item.SelectedCells[0].Item as DMPlugin;
            if (plugin == null) return;

            try
            {
                if (plugin.Status) plugin.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Properties.Resources.MainWindow_Plugin_Disable_插件報錯2, plugin.PluginName, plugin.PluginAuth, plugin.PluginCont));
                try
                {
                    using (var outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                    {
                        outfile.WriteLine(Properties.Resources.MainWindow_Plugin_Enable_請有空發給聯繫方式__0__謝謝, plugin.PluginCont);
                        outfile.WriteLine(DateTime.Now + " " + plugin.PluginName + " " + plugin.PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void Plugin_admin(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)menuItem.Parent;
            var item = (DataGrid)contextMenu.PlacementTarget;
            if (item.SelectedCells.Count == 0)
                return;
            var plugin = item.SelectedCells[0].Item as DMPlugin;
            if (plugin == null)
                return;

            try
            {
                plugin.Admin();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Properties.Resources.MainWindow_Plugin_Disable_插件報錯2, plugin.PluginName, plugin.PluginAuth, plugin.PluginCont));
                try
                {
                    using (var outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                    {
                        outfile.WriteLine(DateTime.Now + " " + string.Format(Properties.Resources.MainWindow_Plugin_Enable_請有空發給聯繫方式__0__謝謝, plugin.PluginCont));
                        outfile.WriteLine(plugin.PluginName + " " + plugin.PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void Plugins_Init()
        {
            // 默认启动的插件
            App.Plugins.Add(new Plugin_UserUUID());
            //App.Plugins.Add(new Plugin_TTSReply());

            try
            {
                Directory.CreateDirectory(StaticConfig.ConfDir);
            }
            catch (Exception)
            {
                return;
            }
            var files = Directory.GetFiles(StaticConfig.ConfDir);
            Stopwatch sw = new Stopwatch();
            foreach (var file in files)
            {
                if (IsDebugMode)
                {
                    Logging("加载插件文件：" + file, false);
                }
                try
                {
                    var dll = Assembly.LoadFrom(file);

                    if (IsDebugMode)
                    {
                        Logging("Assembly.FullName == " + dll.FullName, false);
                        Logging("Assembly.GetExportedTypes == " +
                                string.Join(",", dll.GetExportedTypes().Select(x => x.FullName).ToArray()), false);
                    }

                    foreach (var exportedType in dll.GetExportedTypes())
                    {
                        if (exportedType.BaseType == typeof(DMPlugin))
                        {
                            if (IsDebugMode)
                            {
                                sw.Restart();
                            }
                            var plugin = (DMPlugin)Activator.CreateInstance(exportedType);
                            if (IsDebugMode)
                            {
                                sw.Stop();
                                Logging(
                                    $"插件{exportedType.FullName}({plugin.PluginName})加载完毕，用时{sw.ElapsedMilliseconds}ms", false);
                            }
                            App.Plugins.Add(plugin);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (IsDebugMode)
                    {
                        ErrorLogging("加载出错：" + ex.ToString());
                    }
                }
            }

            foreach (var plugin in App.Plugins)
            {
                try
                {
                    plugin.Inited();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Properties.Resources.MainWindow_Plugin_Disable_插件報錯2, plugin.PluginName, plugin.PluginAuth, plugin.PluginCont));
                    try
                    {

                        using (var outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                        {
                            outfile.WriteLine(DateTime.Now + " " + string.Format(Properties.Resources.MainWindow_Plugin_Enable_請有空發給聯繫方式__0__謝謝, plugin.PluginCont));
                            outfile.WriteLine(plugin.PluginName + " " + plugin.PluginVer);
                            outfile.Write(ex.ToString());
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

            }

        }

        #endregion

        #region =========================== 回调函数 ===========================
        private void OnReciveLogMessage(object sender, LogMessageArgs e)
        {
            Logging(e.message, false);
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();


            Full.IsChecked = isFullOverlayEnabled;
            SideBar.IsChecked = isOverlayEnabled;
            SaveLog.IsChecked = isSaveLogEnabled;

            OptionDialog.LayoutRoot.DataContext = gDanmuSettings;
            gDanmuSettings.PropertyChanged += (o, args) => { SetWindowAffinity(); };
            SetWindowAffinity();

            // 内存清理线程
            StartMemoryClearThread();
            // 弹幕处理线程
            StartProcDanmuThread();
            // 语言播报线程
            CreateTSThread();
        }

        private void OnMainWindowClosed(object sender, EventArgs e)
        {
            StopTTS();

            foreach (var dmPlugin in App.Plugins)
            {
                try
                {
                    dmPlugin.Destory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Properties.Resources.MainWindow_MainWindow_Closed_插件錯誤, dmPlugin.PluginName, dmPlugin.PluginAuth, dmPlugin.PluginCont));
                    try
                    {
                        using (var outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                        {
                            outfile.WriteLine(Properties.Resources.MainWindow_MainWindow_Closed_报错, dmPlugin.PluginCont);
                            outfile.Write(ex.ToString());
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private void OnOneSecondTimer(object sender, EventArgs eventArgs)
        {
            if (gFulloverlay != null)
            {
                gFulloverlay.ForceTopmost();
            }
            if (gOverlay != null)
            {
                gOverlay.Topmost = false;
                gOverlay.Topmost = true;
            }
        }

        private void OnReceivedRoomCount(object sender, ReceivedRoomCountArgs e)
        {
            if (CheckAccess())
            {
                if (IsDebugMode)
                {
                    Logging(string.Format(Properties.Resources.MainWindow_b_ReceivedRoomCount_, e.UserCount));
                }
                OnlineBlock.Text = e.UserCount + "";
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => { OnlineBlock.Text = e.UserCount + ""; }));
            }
            foreach (var dmPlugin in App.Plugins)
            {
                if (dmPlugin.Status)
                    new Thread(() =>
                    {
                        try
                        {
                            dmPlugin.MainReceivedRoomCount(e);
                        }
                        catch (Exception ex)
                        {
                            Utils.PluginExceptionHandler(ex, dmPlugin);
                        }
                    }).Start();
            }
        }

        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (e.Danmaku.MsgType == MsgTypeEnum.Comment)
            {
                lock (gStatic)
                {
                    gStatic.DanmakuCountRaw += 1;
                }
            }

            lock (gDanmakuQueue)
            {
                var danmakuModel = e.Danmaku;
                gDanmakuQueue.Enqueue(danmakuModel);
            }

            foreach (var dmPlugin in App.Plugins)
            {
                if (dmPlugin.Status)
                    new Thread(() =>
                    {
                        try
                        {
                            dmPlugin.MainReceivedDanMaku(e);
                        }
                        catch (Exception ex)
                        {
                            Utils.PluginExceptionHandler(ex, dmPlugin);
                        }
                    }).Start();
            }
        }

        private void OnLoaderDisconnected(object sender, DisconnectEvtArgs args)
        {
            foreach (var dmPlugin in App.Plugins)
            {
                new Thread(() =>
                {
                    try
                    {
                        dmPlugin.MainDisconnected();
                    }
                    catch (Exception ex)
                    {
                        Utils.PluginExceptionHandler(ex, dmPlugin);
                    }
                }).Start();
            }

            ErrorLogging(string.Format(Properties.Resources.MainWindow_b_Disconnected_連接被斷開__开发者信息_0_, args.Error));
            AddDMText(Properties.Resources.MainWindow_connbtn_Click_彈幕姬本身, Properties.Resources.MainWindow_b_Disconnected_連接被斷開, true);

            if (CheckAccess())
            {
                if (AutoReconnect.IsChecked == true && args.Error != null)
                {
                    ErrorLogging(Properties.Resources.MainWindow_b_Disconnected_正在自动重连___);
                    AddDMText(Properties.Resources.MainWindow_connbtn_Click_彈幕姬本身, Properties.Resources.MainWindow_b_Disconnected_正在自动重连___, true);
                    OnEnterRoomBtnClick(null, null);
                }
                else
                {
                    ConnBtn.IsEnabled = true;
                }
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (AutoReconnect.IsChecked == true && args.Error != null)
                    {
                        ErrorLogging(Properties.Resources.MainWindow_b_Disconnected_正在自动重连___);
                        AddDMText(Properties.Resources.MainWindow_connbtn_Click_彈幕姬本身, Properties.Resources.MainWindow_b_Disconnected_正在自动重连___, true);
                        OnEnterRoomBtnClick(null, null);
                    }
                    else
                    {
                        ConnBtn.IsEnabled = true;
                    }
                }));
            }

            // 释放TTS
            if (isNAudioReady) {
                gTTSList.Clear();
            }
        }

        private void OnStoryboardCompleted(object sender, EventArgs e)
        {
            var s = sender as ClockGroup;
            if (s == null) return;
            var c = Storyboard.GetTarget(s.Children[2].Timeline) as DanmakuTextControl;
            if (c != null)
            {
                gOverlay.LayoutRoot.Children.Remove(c);
            }
        }

        #endregion

        #region =========================== Overlay ===========================
        private void OpenFullOverlay()
        {
            var win8Version = new Version(6, 2, 9200);
            var isWin8OrLater = Environment.OSVersion.Platform == PlatformID.Win32NT
                                && Environment.OSVersion.Version >= win8Version;
            if (isWin8OrLater && Store.WtfEngineEnabled)
                gFulloverlay = new WtfDanmakuWindow();
            else
                gFulloverlay = new WpfDanmakuOverlay();

            gDanmuSettings.PropertyChanged += gFulloverlay.OnPropertyChanged;
            gFulloverlay.Show();
        }

        private void OpenOverlay()
        {
            gOverlay = new MainOverlay();
            gOverlay.Deactivated += OnOverlayDeactivated;
            gOverlay.SourceInitialized += delegate
            {
                var hWnd = new WindowInteropHelper(gOverlay).Handle;
                var exStyles = GetExtendedWindowStyles(hWnd);
                SetExtendedWindowStyles(hWnd, exStyles | WINAPI.USER32.ExtendedWindowStyles.Transparent);

            };
            gOverlay.Background = Brushes.Transparent;
            gOverlay.ShowInTaskbar = false;
            gOverlay.Topmost = true;
            gOverlay.Top = SystemParameters.WorkArea.Top + Store.MainOverlayXoffset;
            gOverlay.Left = SystemParameters.WorkArea.Right - Store.MainOverlayWidth + Store.MainOverlayYoffset;
            gOverlay.Height = SystemParameters.WorkArea.Height;
            gOverlay.Width = Store.MainOverlayWidth;
            gDanmuSettings.PropertyChanged += gOverlay.OnPropertyChanged;
        }

        private void OnOverlayDeactivated(object sender, EventArgs e)
        {
            if (sender is MainOverlay)
            {
                (sender as MainOverlay).Topmost = true;
            }
        }

        #endregion

        #region =========================== 核心函数 ===========================
        public void ProcDanmaku(DanmakuModel danmakuModel)
        {
            try
            {
                switch (danmakuModel.MsgType)
                {
                case MsgTypeEnum.Comment:
                    {
                        OnDanmaku_Comment(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.SuperChat:
                    {
                        OnDanmaku_SuperChat(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.GiftTop:
                    {
                        OnDanmaku_GiftTop(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.GiftSend:
                    {
                        OnDanmaku_GiftSend(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.GuardBuy:
                    {
                        OnDanmaku_GuardBuy(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.Welcome:
                    {
                        OnDanmaku_Welcome(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.WelcomeGuard:
                    {
                        OnDanmaku_WelcomeGuard(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.Interact:
                    {
                        OnDanmaku_Interact(danmakuModel);
                    }
                    break;
                case MsgTypeEnum.LiveStart:
                case MsgTypeEnum.LiveEnd:
                case MsgTypeEnum.Unknown:
                    {
                        if (!gSettings.LogExternInfo)
                        {
                            break;
                        }
                        OnDanmaku_ExternInfo(danmakuModel);
                    }
                    break;
                    default:
                        break;
                }
            }
            catch (Exception ex) {
                MessageBox.Show("遇到不明错误：日志已经保存在桌面，请有空发送给 duzhi5368@gmail.com");
                try
                {
                    using (StreamWriter outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                    {
                        outfile.WriteLine("请有空发送给 duzhi5368@gmail.com 謝謝");
                        outfile.WriteLine(DateTime.Now + "");
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void ErrorLogging(string text)
        {
            Logging("【错误】: "+ text, false);
        }

        public void Logging(string text, bool bAddTime = true)
        {
            if (Log.Dispatcher.CheckAccess())
            {
                // 添加到消息队列
                lock (gMessageQueue){
                    if (gMessageQueue.Count >= StaticConfig.MAX_MSG_QUEUE_CAPACITY)
                    {
                        gMessageQueue.RemoveAt(0);
                    }
                    if (bAddTime){
                        gMessageQueue.Add(DateTime.Now.ToString("T") + " : " + text);
                    }
                    else{
                        gMessageQueue.Add(text);
                    }
                }

                // 保存日志
                if (isSaveLogEnabled){
                    try{
                        // TODO:这里的性能开销应该可以降低一些。
                        var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        path = Path.Combine(path, "Bilibili直播小助手");
                        if (!Directory.Exists(path)){
                            Directory.CreateDirectory(path);
                        }
                        using (var outfile = new StreamWriter(Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd") + ".txt"), true)){
                            outfile.WriteLine(DateTime.Now.ToString("T") + " : " + text);
                        }
                    }catch (Exception){ }
                }
            }
            else
            {
                // 其他线程则进行申请
                Log.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Logging(text, bAddTime)));
            }
        }

        public void AddDMText(string user, string text, bool warn = false, bool foreceenablefullscreen = false, int? keeptime = null)
        {
            if (warn)
            {
                return;
            }
            if (!isOverlayEnabled && !isFullOverlayEnabled) 
                return;

            if (Dispatcher.CheckAccess())
            {
                if (SideBar.IsChecked == true)
                {
                    var c = new DanmakuTextControl(keeptime ?? 0);

                    c.UserName.Text = user;
                    if (warn)
                    {
                        c.UserName.Foreground = Brushes.Red;
                    }
                    c.Text.Text = text;
                    c.ChangeHeight();
                    var sb = (Storyboard)c.Resources["Storyboard1"];
                    //Storyboard.SetTarget(sb,c);
                    sb.Completed += OnStoryboardCompleted;
                    gOverlay.LayoutRoot.Children.Add(c);
                }
                if (Full.IsChecked == true && (!warn || foreceenablefullscreen))
                {
                    gFulloverlay.AddDanmaku(DanmakuType.Scrolling, text, 0xFFFFFFFF);
                }
            }
            else
            {
                Log.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => AddDMText(user, text, warn, foreceenablefullscreen, keeptime)));
            }
        }

        public void OnDanmaku_Comment(DanmakuModel danmakuModel) {

            int UserLevel = danmakuModel.RawDataJToken["info"][4][0].ToObject<int>();
            if ((!gSettings.EnableShieldLevel || UserLevel >= gSettings.ShieldLevel) &&
                (!danmakuModel.RawDataJToken["info"][0][9].ToObject<bool>()))
            {
                int UserMedalLevel = 0;
                string UserMedalName = null;
                string UserTitle = danmakuModel.RawDataJToken["info"][5].HasValues ? danmakuModel.RawDataJToken["info"][5][1].ToString() : null;
                if (UserTitle != null)
                {
                    if (BaseAPI.Titles.ContainsKey(UserTitle))
                    {
                        UserTitle = BaseAPI.Titles[UserTitle];
                    }
                    else
                    {
                        try
                        {
                            BaseAPI.UpdateTitles();
                        }
                        catch { }
                        if (BaseAPI.Titles.ContainsKey(UserTitle))
                        {
                            UserTitle = BaseAPI.Titles[UserTitle];
                        }
                        else
                        {
                            UserTitle = null;
                        }
                    }
                }
                if (danmakuModel.RawDataJToken["info"][3].HasValues)
                {
                    UserMedalLevel = danmakuModel.RawDataJToken["info"][3][0].ToObject<int>();
                    UserMedalName = danmakuModel.RawDataJToken["info"][3][1].ToString();
                }

                string prefix = $"{(danmakuModel.isAdmin ? "[管]" : "")}" +
                    $"{(danmakuModel.UserGuardLevel == 3 ? "[舰]" : danmakuModel.UserGuardLevel == 2 ? "[提]" : danmakuModel.UserGuardLevel == 1 ? "[总]" : null)}" +
                    $"{(danmakuModel.isVIP ? "[爷]" : "")}" +
                    $"{(gSettings.LogMedal && !string.IsNullOrEmpty(UserMedalName) ? $"{{{UserMedalName},{UserMedalLevel}}}" : null)}" +
                    $"{(gSettings.LogTitle && !string.IsNullOrEmpty(UserTitle) ? $"[{UserTitle}]" : "")}" +
                    $"{(gSettings.LogLevel ? $"(UL {UserLevel})" : "")}{danmakuModel.UserName}";

                Logging($"收到弹幕:{prefix} 说: {danmakuModel.CommentText}");
                AddDMText(prefix, danmakuModel.CommentText, false, false, null);
            }
        }


        public void OnDanmaku_ExternInfo(DanmakuModel danmakuModel) {
            switch (danmakuModel.RawDataJToken["cmd"].ToString())
            {
                case "ROOM_SILENT_ON":
                    {
                        string type = danmakuModel.RawDataJToken["data"]["type"].ToString();
                        int endTimeStamp = danmakuModel.RawDataJToken["data"]["second"].ToObject<int>();
                        string toLog = $"主播开启了房间禁言.类型:{(type == "member" ? "全体用户" : type == "medal" ? "粉丝勋章" : "用户等级")};{(type != "member" ? $"等级:{danmakuModel.RawDataJToken["data"]["level"]};" : "")}时间:{(endTimeStamp == -1 ? "直到下播" : $"直到{new DateTime(1970, 1, 1).AddSeconds(endTimeStamp).ToLocalTime():yyyy-MM-dd HH:mm:ss}")}";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null);
                        break;
                    }
                case "ROOM_SILENT_OFF":
                    {
                        string toLog = "主播取消了房间禁言";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null);
                        break;
                    }
                case "ROOM_BLOCK_MSG":
                    {
                        string toLog = $"用户 {danmakuModel.RawDataJToken["uname"]}[{danmakuModel.RawDataJToken["uid"]}] 已被房管禁言";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null);
                        break;
                    }
                case "WARNING":
                    {
                        string toLog = $"直播间被警告:{danmakuModel.RawDataJToken["msg"]}";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null);
                        break;
                    }
                case "CUT_OFF":
                    {
                        string toLog = "当前直播间被直播管理员切断";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null);
                        break;
                    }
                case "ROOM_LOCK":
                    {
                        string toLog = "当前直播间被直播管理员关闭";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null);
                        break;
                    }
                case "LIVE":
                    {
                        string toLog = "主播已开播";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null);
                        break;
                    }
                case "PREPARING":
                    {
                        string toLog = "主播已下播";
                        Logging($"系统通知: {toLog}");
                        AddDMText("系统通知", toLog, true, false, null); ;
                        break;
                    }
                default:
                    {
                        //string toLog = danmakuModel.RawDataJToken["cmd"].ToString();
                        //Logging($"未知消息: {toLog}");
                        //AddDMText("未知消息", toLog, true, false, null); ;
                        break;
                    }
            }
        }
        public void OnDanmaku_Interact(DanmakuModel danmakuModel) {
            {
                string r = Properties.Resources.InteractType_TextFormat;
                string text;
                bool bShow = true;
                switch (danmakuModel.InteractType)
                {
                    case InteractTypeEnum.Enter:
                        bShow = gSettings.LogEnter;
                        text = Properties.Resources.InteractType_Text1;
                        break;
                    case InteractTypeEnum.Follow:
                        bShow = gSettings.LogFollow;
                        text = Properties.Resources.InteractType_Text2;
                        break;
                    case InteractTypeEnum.Share:
                        text = Properties.Resources.InteractType_Text3;
                        break;
                    case InteractTypeEnum.SpecialFollow:
                        bShow = gSettings.LogFollow;
                        text = Properties.Resources.InteractType_Text4;
                        break;
                    case InteractTypeEnum.MutualFollow:
                        bShow = gSettings.LogFollow;
                        text = Properties.Resources.InteractType_Text5;
                        break;
                    default:
                        text = Properties.Resources.InteractType_Unknown;
                        break;
                }

                var logtext = string.Format(r, danmakuModel.UserName, text);

                if (bShow)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Logging(logtext);
                        AddDMText(danmakuModel.UserName, text, true);
                    }));
                }
            }
        }
        public void OnDanmaku_WelcomeGuard(DanmakuModel danmakuModel) {
            string guard_text = string.Empty;
            switch (danmakuModel.UserGuardLevel)
            {
                case 1:
                    guard_text = Properties.Resources.MainWindow_ProcDanmaku_总督;
                    break;
                case 2:
                    guard_text = Properties.Resources.MainWindow_ProcDanmaku_提督;
                    break;
                case 3:
                    guard_text = Properties.Resources.MainWindow_ProcDanmaku_舰长;
                    break;
            }
            Logging(
                string.Format(Properties.Resources.MainWindow_ProcDanmaku_欢迎_0____1__2_, guard_text, danmakuModel.UserName, Properties.Resources.MainWindow_ProcDanmaku__进入直播间));
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDMText(string.Format(Properties.Resources.MainWindow_ProcDanmaku_欢迎_0_, guard_text), danmakuModel.UserName + Properties.Resources.MainWindow_ProcDanmaku__进入直播间, true);
            }));
        }
        public void OnDanmaku_GuardBuy(DanmakuModel danmakuModel) {
            Logging(string.Format(Properties.Resources.MainWindow_ProcDanmaku_上船__0__购买了__1__x__2_, danmakuModel.UserName, danmakuModel.GiftName, danmakuModel.GiftCount));
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDMText(Properties.Resources.MainWindow_ProcDanmaku_上船,
                        string.Format(Properties.Resources.MainWindow_ProcDanmaku__0__购买了__1__x__2_, danmakuModel.UserName, danmakuModel.GiftName, danmakuModel.GiftCount), true);
            }));
        }
        public void OnDanmaku_Welcome(DanmakuModel danmakuModel) {
            Logging(string.Format(Properties.Resources.MainWindow_ProcDanmaku_欢迎老爷_0____1__进入直播间, (danmakuModel.isAdmin ? Properties.Resources.MainWindow_ProcDanmaku_和管理 : ""), danmakuModel.UserName));
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddDMText(
                        string.Format(Properties.Resources.MainWindow_ProcDanmaku_欢迎老爷_0_, (danmakuModel.isAdmin ? Properties.Resources.MainWindow_ProcDanmaku_和管理 : "")),
                        danmakuModel.UserName + Properties.Resources.MainWindow_ProcDanmaku__进入直播间, true);
            }));
        }
        public void OnDanmaku_GiftSend(DanmakuModel danmakuModel) {
            lock (gSessionItems)
            {
                var query = gSessionItems.Where(p => p.UserName == danmakuModel.UserName && p.Item == danmakuModel.GiftName).ToArray();
                if (query.Any())
                {
                    Dispatcher.BeginInvoke(
                        new Action(() => query.First().num += Convert.ToDecimal(danmakuModel.GiftCount)));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lock (gSessionItems)
                        {
                            gSessionItems.Add(
                                new SessionItem
                                {
                                    Item = danmakuModel.GiftName,
                                    UserName = danmakuModel.UserName,
                                    num = Convert.ToDecimal(danmakuModel.GiftCount)
                                }
                            );

                        }
                    }));
                }

                if (gSettings.ShowGifts)
                {
                    Logging(string.Format(Properties.Resources.MainWindow_ProcDanmaku_收到道具__0__赠送的___1__x__2_, danmakuModel.UserName, danmakuModel.GiftName, danmakuModel.GiftCount));
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        AddDMText(Properties.Resources.MainWindow_ProcDanmaku_收到道具,
                                string.Format(Properties.Resources.MainWindow_ProcDanmaku__0__赠送的___1__x__2_, danmakuModel.UserName, danmakuModel.GiftName, danmakuModel.GiftCount), true);
                    }));
                }
            }
        }

        public void OnDanmaku_GiftTop(DanmakuModel danmakuModel) {
            foreach (var giftRank in danmakuModel.GiftRanking)
            {
                var query = gRanking.Where(p => p.uid == giftRank.uid);
                if (query.Any())
                {
                    var f = query.First();
                    Dispatcher.BeginInvoke(new Action(() => f.coin = giftRank.coin));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() => gRanking.Add(new GiftRank
                    {
                        uid = giftRank.uid,
                        coin = giftRank.coin,
                        UserName = giftRank.UserName
                    })));
                }
            }
        }

        public void OnDanmaku_SuperChat(DanmakuModel danmakuModel) {
            Logging(string.Format(Properties.Resources.SuperChatLogName, (danmakuModel.isAdmin ? Properties.Resources.MainWindow_ProcDanmaku__管理員前綴_ : ""), (danmakuModel.isVIP ? Properties.Resources.MainWindow_ProcDanmaku__VIP前綴 : ""), danmakuModel.UserName, danmakuModel.CommentText));

            AddDMText(
                Properties.Resources.MainWindow_ProcDanmaku____SuperChat___ + (danmakuModel.isAdmin ? Properties.Resources.MainWindow_ProcDanmaku__管理員前綴_ : "") + (danmakuModel.isVIP ? Properties.Resources.MainWindow_ProcDanmaku__VIP前綴 : "") +
                danmakuModel.UserName + " ￥:" + danmakuModel.Price.ToString("N2"),
                danmakuModel.CommentText, keeptime: danmakuModel.SCKeepTime, warn: true);
        }

            #endregion

            #region =========================== 主界面UI按键 ===========================

            // 【打开插件目录】
            private void OnOpenPluginFolderClick(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(StaticConfig.ConfDir))
            {
                Process.Start(StaticConfig.ConfDir);
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(StaticConfig.ConfDir);
                    Process.Start(StaticConfig.ConfDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Properties.Resources.MainWindow_OpenPluginFolder_OnClick_ + ex.Message, Properties.Resources.MainWindow_OpenPluginFolder_OnClick_打开插件文件夹出错, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 【窗口置顶】复选框
        private void OnWindowTopChecked(object sender, RoutedEventArgs e)
        {
            Topmost = WindowTop.IsChecked == true;
        }

        // 【保存日志】复选框
        private void OnSaveLogChecked(object sender, RoutedEventArgs e)
        {
            isSaveLogEnabled = true;
        }
        private void OnSaveLogUnchecked(object sender, RoutedEventArgs e)
        {
            isSaveLogEnabled = false;
        }

        // 左键点击
        private void OnUIElementMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var textBlock = sender as TextBlock;
                if (textBlock != null)
                {
                    Clipboard.SetText(textBlock.Text);
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() => {
                            MessageBox.Show(Properties.Resources.MainWindow_UIElement_OnMouseLeftButtonUp_本行记录已复制到剪贴板);
                        }));
                }
            }
            catch (Exception)
            {
            }
        }

        // 【测试】按钮
        public void OnTestButtonClick(object sender, RoutedEventArgs e)
        {
            CreateFakeComment();
        }

        // 【显示弹幕】复选框
        private void OnShowFullOverlayChecked(object sender, RoutedEventArgs e)
        {
            isFullOverlayEnabled = true;
            OpenFullOverlay();
            gFulloverlay.Show();
        }
        private void OnShowFullOverlayUnchecked(object sender, RoutedEventArgs e)
        {
            isFullOverlayEnabled = false;
            gFulloverlay.Close();
        }

        // 【侧边栏】复选框
        private void OnShowSideBarChecked(object sender, RoutedEventArgs e)
        {
            isOverlayEnabled = true;
            OpenOverlay();
            gOverlay.Show();
        }
        private void OnShowSideBarUnchecked(object sender, RoutedEventArgs e)
        {
            isOverlayEnabled = false;
            gOverlay.Close();
        }

        // 【断开房间连接】按钮
        private void OnLeaveRoomBtnClick(object sender, RoutedEventArgs e)
        {
            gDanmakuLoader.Disconnect();
            ConnBtn.IsEnabled = true;
            foreach (var dmPlugin in App.Plugins)
            {
                new Thread(() =>
                {
                    try
                    {
                        dmPlugin.MainDisconnected();
                    }
                    catch (Exception ex)
                    {
                        Utils.PluginExceptionHandler(ex, dmPlugin);
                    }
                }).Start();
            }
            Logging("断开房间连接", false);
        }

        private void ClearMe_OnClick(object sender, RoutedEventArgs e)
        {
            lock (gSessionItems)
            {
                gSessionItems.Clear();
            }
        }

        private void ClearMe2_OnClick(object sender, RoutedEventArgs e)
        {
            lock (gStatic)
            {
                gStatic.DanmakuCountShow = 0;
            }
        }

        private void ClearMe3_OnClick(object sender, RoutedEventArgs e)
        {
            lock (gStatic)
            {
                gStatic.ClearUser();
            }
        }

        private void ClearMe4_OnClick(object sender, RoutedEventArgs e)
        {
            lock (gStatic)
            {
                gStatic.DanmakuCountRaw = 0;
            }
        }

        // 【语言选择】按钮
        private void SelectLanguage(object sender, RoutedEventArgs e)
        {
            LanguageSelector lg = new LanguageSelector();
            lg.Owner = this;
            lg.ShowDialog();
        }

        // 【皮肤】按钮
        private void Skin_Click(object sender, RoutedEventArgs e)
        {
            var selector = new Selector
            {
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            var curr = App.Current.merged[0];
            var themes = selector.Themes;
            var candidates = themes.Where(item => item.Value == curr);
            var selected = candidates.SingleOrDefault();
            selector.list.SelectedItem = selected;
            selector.PreviewTheme += skin =>
            {
                if (skin == null) return;
                gSkinDictionary[0] = skin;
            };
            if (selector.Select() is ResourceDictionary result)
            {
                App.Current.merged[0] = result;
            }
            gSkinDictionary[0] = new ResourceDictionary();
        }

        // 【进入房间】按钮
        private async void OnEnterRoomBtnClick(object sender, RoutedEventArgs e)
        {
            int roomId = 0;
            try
            {
                roomId = Convert.ToInt32(RoomId.Text.Trim());
            }
            catch (Exception)
            {
                MessageBox.Show(Properties.Resources.MainWindow_connbtn_Click_请输入房间号_房间号是_数_字_);
                return;
            }

            if (roomId <= 0) {
                MessageBox.Show(Properties.Resources.MainWindow_connbtn_Click_ID非法);
                return;
            }

            ConnBtn.IsEnabled = false;
            DisconnBtn.IsEnabled = false;

            var connectresult = false;
            var trytime = 0;
            const int MAX_TRY_TIME = 5;
            Logging(Properties.Resources.MainWindow_connbtn_Click_正在连接, false);

            if (IsDebugMode){
                Logging(string.Format(Properties.Resources.MainWindow_connbtn_Click_, roomId), false);
            }

            if (gUser == null || string.IsNullOrEmpty(gUser.Data.Csrf))
            {
                // 监听
                connectresult = await gDanmakuLoader.ConnectAsync(roomId);
            }
            else {
                // 用户登录房间
                int num = (int)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
                string visit_id = Scale36.ToCurr(num);
                gUser.VisitId = visit_id;
                connectresult = await gDanmakuLoader.UserConnectAsync(roomId, visit_id, gUser.Data.Cookie, gUser.AppHeaders, gUser.Data.Csrf);
                // 依然要监听
                connectresult &= await gDanmakuLoader.ConnectAsync(roomId);
            }

            // 如果连接不成功并且出错了
            if (!connectresult){
                if(gDanmakuLoader.Error != null)
                    ErrorLogging(string.Format(Properties.Resources.MainWindow_connbtn_Click_出錯, gDanmakuLoader.Error));
                if(!string.IsNullOrEmpty(gDanmakuLoader.ErrorStr))
                    ErrorLogging(gDanmakuLoader.ErrorStr);
            }

            // 尝试重复连接
            while (!connectresult && sender == null && AutoReconnect.IsChecked == true)
            {
                if (trytime > MAX_TRY_TIME)
                    break;
                else
                    trytime++;

                await Task.Delay(1000); // 稍等一下

                Logging(Properties.Resources.MainWindow_connbtn_Click_正在连接, false);

                if (gUser == null || string.IsNullOrEmpty(gUser.Data.Csrf))
                {
                    // 监听
                    connectresult = await gDanmakuLoader.ConnectAsync(roomId);
                }
                else
                {
                    // 用户登录房间
                    int num = (int)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
                    string visit_id = Scale36.ToCurr(num);
                    gUser.VisitId = visit_id;
                    connectresult = await gDanmakuLoader.UserConnectAsync(roomId, visit_id, gUser.Data.Cookie, gUser.AppHeaders, gUser.Data.Csrf);
                    // 依然要监听
                    connectresult &= await gDanmakuLoader.ConnectAsync(roomId);
                }

                if (!connectresult)
                {
                    if (gDanmakuLoader.Error != null)
                        ErrorLogging(string.Format(Properties.Resources.MainWindow_connbtn_Click_出錯, gDanmakuLoader.Error));
                    if (!string.IsNullOrEmpty(gDanmakuLoader.ErrorStr))
                        ErrorLogging(gDanmakuLoader.ErrorStr);
                }
            }

            if (!connectresult) // 彻底连接失败了
            {
                ErrorLogging(Properties.Resources.MainWindow_connbtn_Click_連接失敗);
                AddDMText(Properties.Resources.MainWindow_connbtn_Click_彈幕姬本身, Properties.Resources.MainWindow_connbtn_Click_連接失敗, true);

                ConnBtn.IsEnabled = true;
                DisconnBtn.IsEnabled = true;
                return;
            }

            // 这里已经连接成功
            Logging(Properties.Resources.MainWindow_connbtn_Click_連接成功, false);
            AddDMText(Properties.Resources.MainWindow_connbtn_Click_彈幕姬本身, Properties.Resources.MainWindow_connbtn_Click_連接成功, true);
            SaveRoomId(roomId);
           

            // 一些清除工作在这里进行
            gRanking.Clear();

            // 通知全部组件：开始连接
            foreach (var dmPlugin in App.Plugins)
            {
                new Thread(() =>
                {
                    try
                    {
                        dmPlugin.MainConnected(roomId);
                    }
                    catch (Exception ex)
                    {
                        Utils.PluginExceptionHandler(ex, dmPlugin);
                    }
                }).Start();
            }

            DisconnBtn.IsEnabled = true;
        }

        // 【用户登录】按钮
        private async void OnUserLoginBtnClick(object sender, RoutedEventArgs e)
        {
            UserLogin.IsEnabled = false;
            Logging(Properties.Resources.MainWindow_connbtn_Click_正在连接, false);

            var account = userAccount.Text;
            var password = userPassword.Password;
            gUser = new User(account, password, this);
            if (!await gUser.Login())
            {
                Logging(Properties.Resources.MainWindow_UserLogin_Failed, false);
                UserLogin.IsEnabled = true;
                return;
            }
            /*
            int num = (int)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
            string visit_id = Scale36.ToCurr(num);
            crsfinfo.Text = "crsf:" + gUser.Data.Csrf  + " crsf_token:" + gUser.Data.Csrf + " visit_id:" + visit_id;
            */
            Logging(Properties.Resources.MainWindow_UserLogin_Success, false);

            userAccount.Clear();
            userPassword.Clear();
        }

        // 【弹幕输入框】按键
        private void OnInputKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {// 翻更老的一条记录
                gSendMsgHistoryIndex += 1;
                if (gSendMsgHistoryIndex > gSendMsgHistoryList.Count)
                    gSendMsgHistoryIndex = gSendMsgHistoryList.Count;
                input.Text = gSendMsgHistoryList[gSendMsgHistoryList.Count - gSendMsgHistoryIndex];
            }
            else if (e.Key == Key.Down)
            {
                gSendMsgHistoryIndex -= 1;
                if (gSendMsgHistoryIndex < 0)
                    gSendMsgHistoryIndex = 0;
                if (gSendMsgHistoryIndex == 0)
                    input.Text = string.Empty;
                else
                    input.Text = gSendMsgHistoryList[gSendMsgHistoryList.Count - gSendMsgHistoryIndex];
            }
        }

        // 【弹幕输入框】文字更变
        private async void OnInputTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (input.Text.Contains("\n"))
                {
                    string text = input.Text.Replace("\r", string.Empty).Replace("\n", string.Empty);
                    input.Text = string.Empty;
                    gSendMsgHistoryIndex = 0;
                    AddText2TextList(text);
                    string result = null;
                    try
                    {
                        if (Properties.Settings.Default.roomId <= 0) {
                            Logging("还未连接直播间！登录后进入直播间才能发言！", false);
                            return;
                        }

                        if (gUser == null || !gUser.IsLogin)
                        {
                            Logging("当前偷听模式，不能发言！用户登录后进入直播间才能发言！", false);
                            return;
                        }

                        string strCookie = gUser.Data.Cookie;
                        using (HttpClient client = new HttpClient())
                        {
                            int UnixTimeStamp = (int)((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
                            QueryCollection Postdata = new QueryCollection
                            {
                                { "color", 16777215.ToString() },
                                { "fontsize", 25.ToString() },
                                { "mode", 1.ToString() },
                                { "msg", text },
                                { "rnd", UnixTimeStamp.ToString() },
                                { "bubble", 0.ToString() },
                                { "roomid", Properties.Settings.Default.roomId.ToString() },
                                { "csrf_token", gUser.Data.Csrf },
                                { "csrf", gUser.Data.Csrf }
                            };
                            Dictionary<string, string> header = new Dictionary<string, string>();
                            foreach (KeyValuePair<string, string> entry in gUser.AppHeaders)
                            {
                                header.Add(entry.Key, entry.Value);
                            }
                            header.Add("accept", "application/json, text/javascript, */*; q=0.01");
                            header.Add("accept-encoding", "gzip, deflate, br");
                            header.Add("accept-language", "zh-CN,zh;q=0.8,en;q=0.6");
                            header.Add("origin", "https://live.bilibili.com/");
                            header.Add("cookie", gUser.Data.Cookie);
                            header.Add("referer", "https://live.bilibili.com/" 
                                + Properties.Settings.Default.roomId.ToString() + "?visit_id=" + gUser.VisitId);
                            header.Add("host", "api.live.bilibili.com");
                            using (HttpResponseMessage response = await gUser.Handler.SendAsync(HttpMethod.Post,
                                "https://api.live.bilibili.com/msg/send", Postdata, header))
                            {
                                result = await response.Content.ReadAsStringAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogging("发送消息失败: " + ex.ToString());
                        return;
                    }

                    if (result == null)
                    {
                        ErrorLogging("网络错误，请重试");
                    }
                    else
                    {
                        SendMessageResult r = JsonHelper.DeserializeJsonToObject<SendMessageResult>(result);

                        if (r.code == 0)
                        {
                            return;
                        }
                        else
                        {
                            ErrorLogging("登录失败，服务器报错：" + r.message + " " + r.msg);
                            return;
                        }
                    }
                }
            }
            finally
            {// 统计弹幕字数
                text_count.Text = input.Text.Length.ToString();
            }
        }

        // 【显示等级】复选框
        private void ShowLevelBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogLevel = true;
        }
        private void ShowLevelBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogLevel = false;
        }

        // 【显示进房提示】复选框
        private void ShowEnterBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogEnter = false ;
        }

        private void ShowEnterBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogEnter = true;
        }

        // 【显示勋章】复选框
        private void ShowMedalBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogMedal = true;
        }

        private void ShowMedalBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogMedal = false;
        }

        // 【显示头衔】复选框
        private void ShowTitleBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogTitle = false;
        }

        private void ShowTitleBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogTitle = true;
        }

        // 【显示关注提示】复选框
        private void ShowFollowBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogFollow = true;
        }

        private void ShowFollowBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogFollow = false;
        }

        // 【显示礼物信息】复选框
        private void ShowGiftsBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.ShowGifts = false;
        }

        private void ShowGiftsBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.ShowGifts = true;
        }

        // 【显示更多】复选框
        private void ShowExternBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogExternInfo = true;
        }

        private void ShowExternBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.LogExternInfo = false;
        }

        // 【等级屏蔽】复选框
        private void LevelShieldCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.EnableShieldLevel = false;
        }

        private void LevelShieldCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.EnableShieldLevel = true;
        }

        // 【等级屏蔽】输入框
        private void LevelShieldTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (gSettings == null)
                return;
            int x = 0;
            if (!Int32.TryParse(LevelShieldTextBox.Text, out x))
            {
                return;
            }
            if (x < 0 || x > 60)
            {
                return;
            }
            gSettings.ShieldLevel = x;
        }

        // 开启语音播报
        private void UseTTS_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.IsUseTTS = true;
            try
            {
                if (gTTSThread == null)
                {
                    CreateTSThread();
                }

                gTTSThread.Start();
            }
            catch (Exception ex){
                ErrorLogging(ex.ToString());
            }
        }

        private void UseTTS_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.IsUseTTS = false;
            try
            {
                gTTSThread?.Interrupt();
                /*
                if (gTTSThread != null)
                {
                    gTTSThread.Abort();
                    gTTSThread = null;
                }
                */
            }
            catch (Exception ex)
            {
                ErrorLogging(ex.ToString());
            }
        }

        // 更换了语音播报引擎
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gSettings == null)
                return;
            ComboBox cmb = sender as ComboBox;
            if(cmb.SelectedIndex >= 0)
                gSettings.EngineType = cmb.SelectedIndex;
        }

        // 语音播报角色更变
        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            if (gSettings == null)
                return;
            ComboBox cmb = sender as ComboBox;
            if (cmb.SelectedIndex >= 0)
                gSettings.SpeechPerson = cmb.SelectedIndex;
        }

        // 改变了音量
        private void TTSVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (gSettings == null)
                return;
            Slider s = sender as Slider;
            gSettings.TTSVolume = (int)(s.Value);
        }

        // 测试语音播报
        private async void TestTTS_Click(object sender, RoutedEventArgs e)
        {
            if (isNAudioReady)
            {
                await TTSPlayer.UnifiedPlay("这是一条测试语音, 12345, ABCDE", gSettings.EngineType, gSettings.SpeechPerson, gTTSList);
            }
        }

        // 修改语音配置
        private void EditTTSConfig_Click(object sender, RoutedEventArgs e)
        {
            TTSConfig t = new TTSConfig(this);
            t.Owner = this;
            t.ShowDialog();
        }

        private void TB_Use_Sound_UserEnterRoom_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserEnterRoom = true;
        }

        private void TB_Use_Sound_UserEnterRoom_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserEnterRoom = false;
        }

        private void TB_Use_Text_UserEnterRoom_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserEnterRoom_Reply = true;
        }

        private void TB_Use_Text_UserEnterRoom_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserEnterRoom_Reply = false;
        }

        private void TB_Use_Sound_UserEnterRoom_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserEnterRoom_Reply = true;
        }

        private void TB_Use_Sound_UserEnterRoom_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserEnterRoom_Reply = false;
        }

        private void TB_Use_Sound_UserFollow_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserFollow = true;
        }

        private void TB_Use_Sound_UserFollow_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserFollow = false;
        }

        private void TB_Use_Text_UserFollow_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserFollow_Reply = true;
        }

        private void TB_Use_Text_UserFollow_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserFollow_Reply = false;
        }

        private void TB_Use_Sound_UserFollow_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserFollow_Reply = true;
        }

        private void TB_Use_Sound_UserFollow_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserFollow_Reply = false;
        }

        private void TB_Use_Sound_UserShare_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserShare = true;
        }

        private void TB_Use_Sound_UserShare_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserShare = false;
        }

        private void TB_Use_Text_UserShare_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserShare_Reply = true;
        }

        private void TB_Use_Text_UserShare_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserShare_Reply = false;
        }

        private void TB_Use_Sound_UserShare_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserShare_Reply = true;
        }

        private void TB_Use_Sound_UserShare_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserShare_Reply = false;
        }

        private void TB_Use_Sound_UserSpecialFollow_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserSpecialFollow = true;
        }

        private void TB_Use_Sound_UserSpecialFollow_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserSpecialFollow = false;
        }

        private void TB_Use_Text_UserSpecialFollow_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserSpecialFollow_Reply = true;
        }

        private void TB_Use_Text_UserSpecialFollow_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_UserSpecialFollow_Reply = false;
        }

        private void TB_Use_Sound_UserSpecialFollow_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserSpecialFollow_Reply = true;
        }

        private void TB_Use_Sound_UserSpecialFollow_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserSpecialFollow_Reply = false;
        }
        private void TB_Use_Sound_UserSuperChat_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserSuperChat = true;
        }

        private void TB_Use_Sound_UserSuperChat_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserSuperChat = false;
        }

        private void TB_Use_Sound_UserChat_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserChat = true;
        }

        private void TB_Use_Sound_UserChat_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_UserChat = false;
        }

        private void TB_Use_Sound_ReciveGift_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_ReciveGift = true;
        }

        private void TB_Use_Sound_ReciveGift_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_ReciveGift = false;
        }

        private void TB_Use_Text_ReciveGift_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_ReciveGift_Reply = true;
        }

        private void TB_Use_Text_ReciveGift_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_ReciveGift_Reply = false;
        }

        private void TB_Use_Sound_ReciveGift_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_ReciveGift_Reply = true;
        }

        private void TB_Use_Sound_ReciveGift_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_ReciveGift_Reply = false;
        }

        private void TB_Use_Sound_GuardBuy_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_GuardBuy = true;
        }

        private void TB_Use_Sound_GuardBuy_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_GuardBuy = false;
        }

        private void TB_Use_Text_GuardBuy_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_GuardBuy_Reply = true;
        }

        private void TB_Use_Text_GuardBuy_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Text_GuardBuy_Reply = false;
        }

        private void TB_Use_Sound_GuardBuy_Reply_Checked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_GuardBuy_Reply = true;
        }

        private void TB_Use_Sound_GuardBuy_Reply_Unchecked(object sender, RoutedEventArgs e)
        {
            if (gSettings == null)
                return;
            gSettings.Is_Use_Sound_GuardBuy_Reply = false;
        }

        #endregion

        #region =========================== 配置管理相关 ===========================

        private void SaveRoomId(int roomId)
        {
            try
            {
                Properties.Settings.Default.roomId = roomId;
                Properties.Settings.Default.Save();
            }
            catch (Exception) { }
        }
        #endregion

        #region =========================== 网络消息相关 ===========================
            private static byte[] Pack(DanmuType action, string payload)
            {
                return Pack(action, payload == null ? null : Encoding.UTF8.GetBytes(payload));
            }

            private static byte[] Pack(DanmuType action, byte[] payload)
            {
                byte[] packet;

                if (payload == null)
                    payload = Array.Empty<byte>();
                packet = new byte[payload.Length + 16];
                using (MemoryStream stream = new MemoryStream(packet))
                {
                    stream.Write(System.BitConverter.GetBytes(IPAddress.HostToNetworkOrder(packet.Length)), 0, 4);
                    // packet length
                    stream.Write(System.BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)16)), 0, 2);
                    // header length
                    stream.Write(System.BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)1)), 0, 2);
                    // version
                    stream.Write(System.BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)action)), 0, 4);
                    // action
                    stream.Write(System.BitConverter.GetBytes(IPAddress.HostToNetworkOrder(1)), 0, 4);
                    // magic
                    if (payload.Length > 0)
                        stream.Write(payload, 0, payload.Length);
                    // payload
                }
                return packet;
            }



        #endregion

    }
}
