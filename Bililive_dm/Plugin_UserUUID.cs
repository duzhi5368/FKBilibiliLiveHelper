using BilibiliDM_PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Bililive_dm
{
    public sealed class Plugin_UserUUID : DMPlugin
    {
        public Plugin_UserUUID() {
            ReceivedDanmaku += OnReceivedDanmaku;
            PluginAuth = "FreeKnight";
            PluginName = "UID显示插件";
            PluginDesc = "在用户名后添加UID，主要测试用";
            PluginCont = "duzhi5368@gmail.com";
            PluginVer = "v1.0.0";
        }

        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (e.Danmaku.UserID != 0)
            {
                e.Danmaku.UserName += string.Format(" ({0})", e.Danmaku.UserID);
            }
        }

        public override void Admin()
        {
            base.Admin();
            MessageBox.Show("这时可以显示一个测试面板");
        }
    }
}
