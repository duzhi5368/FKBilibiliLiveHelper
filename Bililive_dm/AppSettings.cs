using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Bililive_dm
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DoNotCopyProperty : Attribute { }

    public class Settings : INotifyPropertyChanged
    {
        protected readonly string FilePath;
        public Settings(string filePath)
        {
            FilePath = filePath;
        }
        public virtual void SaveConfig()
        {
            JObject j = JObject.FromObject(this);
            File.WriteAllText(FilePath, j.ToString());
        }
        public virtual void LoadConfig()
        {
            if (File.Exists(FilePath))
            {
                Type configType = this.GetType();
                object configInstance = JsonConvert.DeserializeObject(File.ReadAllText(FilePath), configType);
                foreach (PropertyInfo property in configType.GetProperties())
                {
                    if (!Attribute.IsDefined(property, typeof(DoNotCopyProperty)))
                    {
                        property.SetValue(this, property.GetValue(configInstance));
                    }
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName]string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class AppSettings : Settings
    {
        private bool _LogLevel = false;
        public bool LogLevel { get => _LogLevel; set { if (_LogLevel != value) { _LogLevel = value; OnPropertyChanged(); } } }
        private bool _LogMedal = false;
        public bool LogMedal { get => _LogMedal; set { if (_LogMedal != value) { _LogMedal = value; OnPropertyChanged(); } } }
        private bool _LogTitle = false;
        public bool LogTitle { get => _LogTitle; set { if (_LogTitle != value) { _LogTitle = value; OnPropertyChanged(); } } }
        private bool _LogExternInfo = false;
        public bool LogExternInfo { get => _LogExternInfo; set { if (_LogExternInfo != value) { _LogExternInfo = value; OnPropertyChanged(); } } }
        private bool _LogEnter = true;
        public bool LogEnter { get => _LogEnter; set { if (_LogEnter != value) { _LogEnter = value; OnPropertyChanged(); } } }
        private bool _LogFollow = true;
        public bool LogFollow { get => _LogFollow; set { if (_LogFollow != value) { _LogFollow = value; OnPropertyChanged(); } } }
        private bool _ShowGifts = true;
        public bool ShowGifts { get => _ShowGifts; set { if (_ShowGifts != value) { _ShowGifts = value; OnPropertyChanged(); } } }
        private bool _EnableShieldLevel = false;
        public bool EnableShieldLevel { get => _EnableShieldLevel; set { if (_EnableShieldLevel != value) { _EnableShieldLevel = value; OnPropertyChanged(); } } }
        private int _ShieldLevel = 0;
        public int ShieldLevel { get => _ShieldLevel; set { if (_ShieldLevel != value) { _ShieldLevel = value; OnPropertyChanged(); } } }
        

        private bool _IsUseTTS = true;
        public bool IsUseTTS { get => _IsUseTTS; set { if (_IsUseTTS != value) { _IsUseTTS = value; OnPropertyChanged(); } } }
        private int _EngineType = 4;
        public int EngineType { get => _EngineType; set { if (_EngineType != value) { _EngineType = value; OnPropertyChanged(); } } }
        private int _SpeechPerson = 1;
        public int SpeechPerson { get => _SpeechPerson; set { if (_SpeechPerson != value) { _SpeechPerson = value; OnPropertyChanged(); } } }
        private int _TTSVolume = 70;
        public int TTSVolume { get => _TTSVolume; set { if (_TTSVolume != value) { _TTSVolume = value; OnPropertyChanged(); } } }


        private string _Text_UserEnterRoom = "$USER 进入直播间";
        public string Text_UserEnterRoom { get => _Text_UserEnterRoom; set { if (!_Text_UserEnterRoom.Equals(value)) { _Text_UserEnterRoom = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserEnterRoom = false;
        public bool Is_Use_Sound_UserEnterRoom { get => _Is_Use_Sound_UserEnterRoom; set { if (_Is_Use_Sound_UserEnterRoom != value) { _Is_Use_Sound_UserEnterRoom = value; OnPropertyChanged(); } } }
        private string _Text_UserEnterRoom_Reply = "欢迎 $USER";
        public string Text_UserEnterRoom_Reply { get => _Text_UserEnterRoom_Reply; set { if (!_Text_UserEnterRoom_Reply.Equals(value)) { _Text_UserEnterRoom_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Text_UserEnterRoom_Reply = false;
        public bool Is_Use_Text_UserEnterRoom_Reply { get => _Is_Use_Text_UserEnterRoom_Reply; set { if (_Is_Use_Text_UserEnterRoom_Reply != value) { _Is_Use_Text_UserEnterRoom_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserEnterRoom_Reply = false;
        public bool Is_Use_Sound_UserEnterRoom_Reply { get => _Is_Use_Sound_UserEnterRoom_Reply; set { if (_Is_Use_Sound_UserEnterRoom_Reply != value) { _Is_Use_Sound_UserEnterRoom_Reply = value; OnPropertyChanged(); } } }
        private string _Text_UserFollow = "$USER 关注了直播间";
        public string Text_UserFollow { get => _Text_UserFollow; set { if (!_Text_UserFollow.Equals(value)) { _Text_UserFollow = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserFollow = false;
        public bool Is_Use_Sound_UserFollow { get => _Is_Use_Sound_UserFollow; set { if (_Is_Use_Sound_UserFollow != value) { _Is_Use_Sound_UserFollow = value; OnPropertyChanged(); } } }
        private string _Text_UserFollow_Reply = "感谢 $USER 的关注";
        public string Text_UserFollow_Reply { get => _Text_UserFollow_Reply; set { if (!_Text_UserFollow_Reply.Equals(value)) { _Text_UserFollow_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Text_UserFollow_Reply = false;
        public bool Is_Use_Text_UserFollow_Reply { get => _Is_Use_Text_UserFollow_Reply; set { if (_Is_Use_Text_UserFollow_Reply != value) { _Is_Use_Text_UserFollow_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserFollow_Reply = false;
        public bool Is_Use_Sound_UserFollow_Reply { get => _Is_Use_Sound_UserFollow_Reply; set { if (_Is_Use_Sound_UserFollow_Reply != value) { _Is_Use_Sound_UserFollow_Reply = value; OnPropertyChanged(); } } }
        private string _Text_UserShare = "$USER 分享了直播间";
        public string Text_UserShare { get => _Text_UserShare; set { if (!_Text_UserShare.Equals(value)) { _Text_UserShare = value; OnPropertyChanged(); } } }
       private bool _Is_Use_Sound_UserShare = false;
        public bool Is_Use_Sound_UserShare { get => _Is_Use_Sound_UserShare; set { if (_Is_Use_Sound_UserShare != value) { _Is_Use_Sound_UserShare = value; OnPropertyChanged(); } } }
        private string _Text_UserShare_Reply = "感谢 $USER 的分享";
        public string Text_UserShare_Reply { get => _Text_UserShare_Reply; set { if (!_Text_UserShare_Reply.Equals(value)) { _Text_UserShare_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Text_UserShare_Reply = false;
        public bool Is_Use_Text_UserShare_Reply { get => _Is_Use_Text_UserShare_Reply; set { if (_Is_Use_Text_UserShare_Reply != value) { _Is_Use_Text_UserShare_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserShare_Reply = false;
        public bool Is_Use_Sound_UserShare_Reply { get => _Is_Use_Sound_UserShare_Reply; set { if (_Is_Use_Sound_UserShare_Reply != value) { _Is_Use_Sound_UserShare_Reply = value; OnPropertyChanged(); } } }
        private string _Text_UserSpecialFollow = "$USER 特别关注了直播间";
        public string Text_UserSpecialFollow { get => _Text_UserSpecialFollow; set { if (!_Text_UserSpecialFollow.Equals(value)) { _Text_UserSpecialFollow = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserSpecialFollow = false;
        public bool Is_Use_Sound_UserSpecialFollow { get => _Is_Use_Sound_UserSpecialFollow; set { if (_Is_Use_Sound_UserSpecialFollow != value) { _Is_Use_Sound_UserSpecialFollow = value; OnPropertyChanged(); } } }
        private string _Text_UserSpecialFollow_Reply = "感谢 $USER 的特别关注";
        public string Text_UserSpecialFollow_Reply { get => _Text_UserSpecialFollow_Reply; set { if (!_Text_UserSpecialFollow_Reply.Equals(value)) { _Text_UserSpecialFollow_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Text_UserSpecialFollow_Reply = false;
        public bool Is_Use_Text_UserSpecialFollow_Reply { get => _Is_Use_Text_UserSpecialFollow_Reply; set { if (_Is_Use_Text_UserSpecialFollow_Reply != value) { _Is_Use_Text_UserSpecialFollow_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserSpecialFollow_Reply = false;
        public bool Is_Use_Sound_UserSpecialFollow_Reply { get => _Is_Use_Sound_UserSpecialFollow_Reply; set { if (_Is_Use_Sound_UserSpecialFollow_Reply != value) { _Is_Use_Sound_UserSpecialFollow_Reply = value; OnPropertyChanged(); } } }
        private string _Text_UserSuperChat = "$USER 醒目留言: $DM";
        public string Text_UserSuperChat { get => _Text_UserSuperChat; set { if (!_Text_UserSuperChat.Equals(value)) { _Text_UserSuperChat = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserSuperChat = false;
        public bool Is_Use_Sound_UserSuperChat { get => _Is_Use_Sound_UserSuperChat; set { if (_Is_Use_Sound_UserSuperChat != value) { _Is_Use_Sound_UserSuperChat = value; OnPropertyChanged(); } } }
        private string _Text_UserChat = "$USER 说: $DM";
        public string Text_UserChat { get => _Text_UserChat; set { if (!_Text_UserChat.Equals(value)) { _Text_UserChat = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_UserChat = false;
        public bool Is_Use_Sound_UserChat { get => _Is_Use_Sound_UserChat; set { if (_Is_Use_Sound_UserChat != value) { _Is_Use_Sound_UserChat = value; OnPropertyChanged(); } } }
        private string _Text_ReciveGift = "收到来自 $USER 的 $COUNT 个 $GIFT，谢谢";
        public string Text_ReciveGift { get => _Text_ReciveGift; set { if (!_Text_ReciveGift.Equals(value)) { _Text_ReciveGift = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_ReciveGift = false;
        public bool Is_Use_Sound_ReciveGift { get => _Is_Use_Sound_ReciveGift; set { if (_Is_Use_Sound_ReciveGift != value) { _Is_Use_Sound_ReciveGift = value; OnPropertyChanged(); } } }
        private string _Text_ReciveGift_Reply = "感谢 $USER 投喂的 $GIFT";
        public string Text_ReciveGift_Reply { get => _Text_ReciveGift_Reply; set { if (!_Text_ReciveGift_Reply.Equals(value)) { _Text_ReciveGift_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Text_ReciveGift_Reply = false;
        public bool Is_Use_Text_ReciveGift_Reply { get => _Is_Use_Text_ReciveGift_Reply; set { if (_Is_Use_Text_ReciveGift_Reply != value) { _Is_Use_Text_ReciveGift_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_ReciveGift_Reply = false;
        public bool Is_Use_Sound_ReciveGift_Reply { get => _Is_Use_Sound_ReciveGift_Reply; set { if (_Is_Use_Sound_ReciveGift_Reply != value) { _Is_Use_Sound_ReciveGift_Reply = value; OnPropertyChanged(); } } }
        private string _Text_GuardBuy = "$USER 上船了";
        public string Text_GuardBuy { get => _Text_GuardBuy; set { if (!_Text_GuardBuy.Equals(value)) { _Text_GuardBuy = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_GuardBuy = false;
        public bool Is_Use_Sound_GuardBuy { get => _Is_Use_Sound_GuardBuy; set { if (_Is_Use_Sound_GuardBuy != value) { _Is_Use_Sound_GuardBuy = value; OnPropertyChanged(); } } }
        private string _Text_GuardBuy_Reply = "哇，谢谢船长 $USER";
        public string Text_GuardBuy_Reply { get => _Text_GuardBuy_Reply; set { if (!_Text_GuardBuy_Reply.Equals(value)) { _Text_GuardBuy_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Text_GuardBuy_Reply = false;
        public bool Is_Use_Text_GuardBuy_Reply { get => _Is_Use_Text_GuardBuy_Reply; set { if (_Is_Use_Text_GuardBuy_Reply != value) { _Is_Use_Text_GuardBuy_Reply = value; OnPropertyChanged(); } } }
        private bool _Is_Use_Sound_GuardBuy_Reply = false;
        public bool Is_Use_Sound_GuardBuy_Reply { get => _Is_Use_Sound_GuardBuy_Reply; set { if (_Is_Use_Sound_GuardBuy_Reply != value) { _Is_Use_Sound_GuardBuy_Reply = value; OnPropertyChanged(); } } }
        public AppSettings(string filePath) : base(filePath)
        {

        }
        protected override void OnPropertyChanged([CallerMemberName]string name = null)
        {
            if (!string.IsNullOrEmpty(FilePath))
            {
                SaveConfig();
            }
            base.OnPropertyChanged(name);
        }
    }
}
