﻿using System;
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
    /// LanguageSelector.xaml 的交互逻辑
    /// </summary>
    public partial class LanguageSelector : Window
    {
        public LanguageSelector()
        {
            InitializeComponent();

            switch (Properties.Settings.Default.lang)
            {
                case "ja-JP":
                    this.jp.IsChecked = true;
                    break;
                case "zh":
                    default:
                    this.cn.IsChecked = true;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, "抱歉，当前翻译工作未完成，暂不支持本功能");
            this.Close();
            return;
            /*
            if (this.cn.IsChecked == true)
            {
                Properties.Settings.Default.lang = "zh";
            }
            else if (this.jp.IsChecked==true)
            {
                Properties.Settings.Default.lang = "zh";//ja-JP";
            }
            Properties.Settings.Default.Save();
            MessageBox.Show(this,"语言设定将在重启本软件后生效");
            this.Close();*/
        }
    }
}
