using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using JetBrains.Annotations;

namespace BilibiliDM_PluginFramework
{

   
    public  class DMPlugin: DispatcherObject,INotifyPropertyChanged
    {
        private bool _status = false;
        public event ReceivedDanmakuEvt ReceivedDanmaku;
        public event DisconnectEvt Disconnected;
        public event ReceivedRoomCountEvt ReceivedRoomCount;
        public event ConnectedEvt Connected;

        public void MainConnected(int roomid)
         {
             this.RoomID = roomid;
            try
            {
                Connected?.Invoke(null, new ConnectedEvtArgs() { roomid = roomid });
            }
            catch (Exception ex)
            {
                MessageBox.Show("插件：" + PluginName + "遇到不明错误，日志保存在桌面，请联系作者： " + PluginAuth + " ，联系方式： " + PluginCont);
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    using (StreamWriter outfile = new StreamWriter(path + @"\Bilibili直播小助手插件" + PluginName + "错误报告.txt"))
                    {
                        outfile.WriteLine("请联系 " + PluginCont + " 谢谢");
                        outfile.WriteLine(PluginName + " " + PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception){}
            }
            
         }

        public void MainReceivedDanMaku(ReceivedDanmakuArgs e)
        {
            try
            {
                ReceivedDanmaku?.Invoke(null, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("插件：" + PluginName + "遇到不明错误，日志保存在桌面，请联系作者： " + PluginAuth + " ，联系方式： " + PluginCont);
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    using (StreamWriter outfile = new StreamWriter(path + @"\Bilibili直播小助手插件" + PluginName + "错误报告.txt"))
                    {
                        outfile.WriteLine("请联系 " + PluginCont + " 谢谢");
                        outfile.WriteLine(PluginName + " " + PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception) { }
            }
            
        }

        public void MainReceivedRoomCount(ReceivedRoomCountArgs e)
        {
            try
            {
                ReceivedRoomCount?.Invoke(null, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("插件：" + PluginName + "遇到不明错误，日志保存在桌面，请联系作者： " + PluginAuth + " ，联系方式： " + PluginCont);
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    using (StreamWriter outfile = new StreamWriter(path + @"\Bilibili直播小助手插件" + PluginName + "错误报告.txt"))
                    {
                        outfile.WriteLine("请联系 " + PluginCont + " 谢谢");
                        outfile.WriteLine(PluginName + " " + PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception) { }
            }
        }

        public void MainDisconnected()
        {
            this.RoomID = null;
            try
            {
                Disconnected?.Invoke(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("插件：" + PluginName + "遇到不明错误，日志保存在桌面，请联系作者： " + PluginAuth + " ，联系方式： " + PluginCont);
                try
                {
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    using (StreamWriter outfile = new StreamWriter(path + @"\Bilibili直播小助手插件" + PluginName + "错误报告.txt"))
                    {
                        outfile.WriteLine("请联系 " + PluginCont + " 谢谢");
                        outfile.WriteLine(PluginName + " " + PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception) { }
            }
           
        }

        /// <summary>
        /// 插件名稱
        /// </summary>
        public string PluginName { get; set; } = "无名插件";

        /// <summary>
        /// 插件作者
        /// </summary>
        public string PluginAuth { get; set; } = "FreeKnight";

        /// <summary>
        /// 插件作者聯繫方式
        /// </summary>
        public string PluginCont { get; set; } = "281862942";

        /// <summary>
        /// 插件版本號
        /// </summary>
        public string PluginVer { get; set; } = "V0.0.0";
        /// <summary>
        /// 插件描述
        /// </summary>
        public string PluginDesc { get; set; } = "本插件没有任何描述";

        /// <summary>
        /// 插件描述, 已過期, 請使用PluginDesc
        /// </summary>
        [Obsolete("插件过期，请参考插件描述")]
        public string PlubinDesc
        {
            get { return this.PluginDesc; }
            set { this.PluginDesc = value; }
        }

        /// <summary>
        /// 插件狀態
        /// </summary>
        public bool Status
        {
            get { return _status; }
            private set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        /// <summary>
        /// 當前連接中的房間
        /// </summary>
        public int? RoomId => RoomID;

        private int? RoomID;

        public DMPlugin()
        {
                
        }
        /// <summary>
        /// 啟用插件方法 請重寫此方法
        /// </summary>
        public virtual void Start()
        {
            this.Status = true;
            Console.WriteLine("启动：" + this.PluginName);
        }
        /// <summary>
        /// 禁用插件方法 請重寫此方法
        /// </summary>
        public virtual void Stop()
        {

            this.Status = false;
            Console.WriteLine("停止：" + this.PluginName);
        }
        /// <summary>
        /// 管理插件方法 請重寫此方法
        /// </summary>
        public virtual void Admin()
        {
            
        }
        /// <summary>
        /// 此方法在所有插件加载完毕后调用
        /// </summary>
        public virtual void Inited()
        {

        }
        /// <summary>
        /// 反初始化方法, 在弹幕姬主程序退出时调用, 若有需要请重写,
        /// </summary>
        public virtual void Destory()
        {
            
        }
        /// <summary>
        /// 打日志
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                dynamic mw = Application.Current.MainWindow;
                mw.logging(this.PluginName + " " + text);

            }));
            
        }
        /// <summary>
        /// 弹幕姬是否是以Debug模式启动的
        /// </summary>
        public bool DebugMode
        {
            get
            {
                return (Application.Current.MainWindow as dynamic).debug_mode;
            }
        }
        /// <summary>
        /// 打彈幕
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fullscreen"></param>
        public void AddDM(string text, bool fullscreen = false)
        {

            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                dynamic mw = Application.Current.MainWindow;
                mw.AddDMText(this.PluginName, text, true, fullscreen);

            }));
           
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

  
}
