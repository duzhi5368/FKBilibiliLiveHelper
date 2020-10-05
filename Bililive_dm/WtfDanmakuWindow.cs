﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using static WINAPI.USER32;

namespace Bililive_dm
{
    using static ExtendedWindowStyles;

    public partial class WtfDanmakuWindow : Form, IDanmakuWindow
    {
        [DllImport("libwtfdanmaku")]
        private static extern IntPtr WTF_CreateInstance();

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_ReleaseInstance(IntPtr instance);

        [DllImport("libwtfdanmaku")]
        private static extern int WTF_InitializeWithHwnd(IntPtr instance, IntPtr hwnd);

        [DllImport("libwtfdanmaku")]
        private static extern int WTF_InitializeOffscreen(IntPtr instance, uint initialWidth, uint initialHeight);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_Terminate(IntPtr instance);

        [DllImport("libwtfdanmaku", CharSet = CharSet.Unicode)]
        private static extern void WTF_AddLiveDanmaku(IntPtr instance, int type, long time, string comment, int fontSize, int fontColor, long timestamp, int danmakuId);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_Start(IntPtr instance);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_Pause(IntPtr instance);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_Resume(IntPtr instance);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_Stop(IntPtr instance);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_Resize(IntPtr instance, uint width, uint height);

        [DllImport("libwtfdanmaku")]
        private static extern int WTF_IsRunning(IntPtr instance);

        [DllImport("libwtfdanmaku")]
        private static extern float WTF_GetFontScaleFactor(IntPtr instance);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_SetFontScaleFactor(IntPtr instance, float factor);

        [DllImport("libwtfdanmaku", CharSet = CharSet.Unicode)]
        private static extern void WTF_SetFontName(IntPtr instance, string fontName);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_SetDanmakuStyle(IntPtr instance, int style);

        [DllImport("libwtfdanmaku")]
        private static extern void WTF_SetCompositionOpacity(IntPtr instance, float opacity);

        private IntPtr _wtf;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOREDIRECTIONBITMAP;
                return cp;
            }
        }

        public WtfDanmakuWindow()
        {
            InitializeComponent();
            this.StartPosition=FormStartPosition.Manual;
            this.Resize += WtfDanmakuWindow_Resize;
            this.FormClosing += WtfDanmakuWindow_FormClosing;
        }

        ~WtfDanmakuWindow()
        {
            (this as IDanmakuWindow).Dispose();
        }

        private void WtfDanmakuWindow_Load(object sender, EventArgs e)
        {
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            var exStyles = GetExtendedWindowStyles(Handle);
            SetExtendedWindowStyles(Handle, exStyles | Layered | Transparent | ToolWindow);

            CreateWTF();
        }

        private void WtfDanmakuWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            DestroyWTF();
        }

        private void WtfDanmakuWindow_Resize(object sender, EventArgs e)
        {
            if (_wtf != IntPtr.Zero)
            {
                WTF_Resize(_wtf, (uint)ClientSize.Width, (uint)ClientSize.Height);
            }
        }

        private void CreateWTF() {
            _wtf = WTF_CreateInstance();
            WTF_InitializeWithHwnd(_wtf, this.Handle);
            WTF_SetFontName(_wtf, "SimHei");
            WTF_SetFontScaleFactor(_wtf, (float)(Store.FullOverlayFontsize / 25.0f));
            WTF_SetCompositionOpacity(_wtf, 0.85f);
            SetWindowDisplayAffinity(_wtf, Store.DisplayAffinity ? WindowDisplayAffinity.ExcludeFromCapture : 0);
            SetMonitor(Store.FullScreenMonitor);
        }

        private void DestroyWTF()
        {
            if (_wtf != IntPtr.Zero)
            {
                if (WTF_IsRunning(_wtf) != 0)
                {
                    WTF_Stop(_wtf);
                }
                WTF_Terminate(_wtf);
                WTF_ReleaseInstance(_wtf);
                _wtf = IntPtr.Zero;
            }
        }

        void IDisposable.Dispose()
        {
            if (_wtf != IntPtr.Zero)
                DestroyWTF();
        }

        void IDanmakuWindow.Show()
        {
            (this as Form).Show();
            WTF_Start(_wtf);
        }

        void IDanmakuWindow.Close()
        {
            (this as Form).Close();
        }

        void IDanmakuWindow.ForceTopmost()
        {
            //this.TopMost = false;
            //this.TopMost = true;
        }

        void IDanmakuWindow.AddDanmaku(DanmakuType type, string comment, uint color)
        {
            WTF_AddLiveDanmaku(_wtf, (int)type, 0, comment, 25, (int)color, 0, 0);
        }

        public void SetMonitor(string deviceName)
        {
            Screen s = Screen.AllScreens.FirstOrDefault(p => p.DeviceName == deviceName) ?? Screen.PrimaryScreen;
            System.Drawing.Rectangle r = s.WorkingArea;
            this.WindowState = FormWindowState.Normal;
            this.Top = r.Top;
            this.Left = r.Left;
            this.Width = r.Width;
            this.Height = r.Height;
            this.WindowState = FormWindowState.Maximized;

        }

        void IDanmakuWindow.OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_wtf != IntPtr.Zero)
            {
                WTF_SetFontScaleFactor(_wtf, (float)(Store.FullOverlayFontsize / 25.0f));
                SetWindowDisplayAffinity(_wtf, Store.DisplayAffinity ? WindowDisplayAffinity.ExcludeFromCapture : 0);
                if (e.PropertyName == nameof(Store.FullScreenMonitor))
                {
                    SetMonitor(Store.FullScreenMonitor);
                }

            }

        }
    }
}
