using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bililive_dm
{
    /// <summary>
    /// TTSConfig.xaml 的交互逻辑
    /// </summary>
    public partial class TTSConfig : Window
    {
        public MainWindow mw;
        public TTSConfig(MainWindow main)
        {
            InitializeComponent();
            mw = main;
            LoadConfig();
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            PassConfig();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PassConfig() {
            mw.gSettings.Text_UserEnterRoom = Text_UserEnterRoom.Text;
            mw.gSettings.Text_UserEnterRoom_Reply = Text_UserEnterRoom_Reply.Text;
            mw.gSettings.Text_UserFollow = Text_UserFollow.Text;
            mw.gSettings.Text_UserFollow_Reply = Text_UserFollow_Reply.Text;
            mw.gSettings.Text_UserShare = Text_UserShare.Text;
            mw.gSettings.Text_UserShare_Reply = Text_UserShare_Reply.Text;
            mw.gSettings.Text_UserSpecialFollow = Text_UserSpecialFollow.Text;
            mw.gSettings.Text_UserSpecialFollow_Reply = Text_UserSpecialFollow_Reply.Text;
            mw.gSettings.Text_UserSuperChat = Text_UserSuperChat.Text;
            mw.gSettings.Text_UserChat = Text_UserChat.Text;
            mw.gSettings.Text_ReciveGift = Text_ReciveGift.Text;
            mw.gSettings.Text_ReciveGift_Reply = Text_ReciveGift_Reply.Text;
            mw.gSettings.Text_GuardBuy = Text_GuardBuy.Text;
            mw.gSettings.Text_GuardBuy_Reply = Text_GuardBuy_Reply.Text;
        }
        private void LoadConfig() {
            (this.FindName("Text_UserEnterRoom") as TextBox).Text = mw.gSettings.Text_UserEnterRoom;
            (this.FindName("Text_UserEnterRoom_Reply") as TextBox).Text = mw.gSettings.Text_UserEnterRoom_Reply;
            (this.FindName("Text_UserFollow") as TextBox).Text = mw.gSettings.Text_UserFollow;
            (this.FindName("Text_UserFollow_Reply") as TextBox).Text = mw.gSettings.Text_UserFollow_Reply;
            (this.FindName("Text_UserShare") as TextBox).Text = mw.gSettings.Text_UserShare;
            (this.FindName("Text_UserShare_Reply") as TextBox).Text = mw.gSettings.Text_UserShare_Reply;
            (this.FindName("Text_UserSpecialFollow") as TextBox).Text = mw.gSettings.Text_UserSpecialFollow;
            (this.FindName("Text_UserSpecialFollow_Reply") as TextBox).Text = mw.gSettings.Text_UserSpecialFollow_Reply;
            (this.FindName("Text_UserSuperChat") as TextBox).Text = mw.gSettings.Text_UserSuperChat;
            (this.FindName("Text_UserChat") as TextBox).Text = mw.gSettings.Text_UserChat;
            (this.FindName("Text_ReciveGift") as TextBox).Text = mw.gSettings.Text_ReciveGift;
            (this.FindName("Text_ReciveGift_Reply") as TextBox).Text = mw.gSettings.Text_ReciveGift_Reply;
            (this.FindName("Text_GuardBuy") as TextBox).Text = mw.gSettings.Text_GuardBuy;
            (this.FindName("Text_GuardBuy_Reply") as TextBox).Text = mw.gSettings.Text_GuardBuy_Reply;
        }
    }
}
