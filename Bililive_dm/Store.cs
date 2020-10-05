using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using BilibiliDM_PluginFramework;
using Newtonsoft.Json;

namespace Bililive_dm
{
    public static class Store
    {
        public static double MainOverlayXoffset = 0;
        public static double MainOverlayYoffset = 0;
        public static double MainOverlayWidth = 250;
        public static double MainOverlayEffect1 = 0.8;          //拉伸
        public static double MainOverlayEffect2 = 1.4 - 0.8;    //文字出現
        public static double MainOverlayEffect3 = 6 - 1.4;      //文字停留
        public static double MainOverlayEffect4 = 1;            //窗口消失
        public static double MainOverlayFontsize = 18.667;


        public static double FullOverlayEffect1 = 400;          //文字速度
        public static double FullOverlayFontsize = 35;
        public static bool WtfEngineEnabled = true;
        public static bool DisplayAffinity = false;
        public static string FullScreenMonitor = null;
    }


    public static class DefaultStore
    {
        public static double MainOverlayXoffset = 0;
        public static double MainOverlayYoffset = 0;
        public static double MainOverlayWidth = 250;
        public static double MainOverlayEffect1 = 0.8;          //拉伸
        public static double MainOverlayEffect2 = 1.4 - 0.8;    //文字出現
        public static double MainOverlayEffect3 = 6 - 1.4;      //文字停留
        public static double MainOverlayEffect4 = 1;            //窗口消失
        public static double MainOverlayFontsize = 18.667;


        public static double FullOverlayEffect1 = 400;          //文字速度
        public static double FullOverlayFontsize = 35;
        public static bool WtfEngineEnabled = true;
        public static bool DisplayAffinity = false;
        public static string FullScreenMonitor = null;
    }

    public static class Utils
    {
        public static string FormatJson(string json)
        {
            using (StringWriter writer = new StringWriter())
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
            using (StringReader reader = new StringReader(json))
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                jsonWriter.WriteToken(jsonReader);
                return writer.ToString();
            }
        }
        public static IPAddress GetBroadcastAddress(this IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAddressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAddressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAddressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAddressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }
        public static void PluginExceptionHandler(Exception ex, DMPlugin plugin=null)
        {
            if (plugin != null)
            {
                MessageBox.Show("插件：" + plugin.PluginName + "遇到不明错误，日志保存在桌面，请联系作者： " + plugin.PluginAuth + " ，联系方式： " + plugin.PluginCont);
                try
                {
                    using (StreamWriter outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                    {
                        outfile.WriteLine("请联系 " + plugin.PluginCont + " 谢谢");
                        outfile.WriteLine(plugin.PluginName + " " + plugin.PluginVer);
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception) { }
            }
            else
            {
                MessageBox.Show("遇到不明错误，日志保存在桌面，请联系作者：freeknight ，联系方式： duzhi5368@gmail.com");
                try
                {
                    using (StreamWriter outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                    {
                        outfile.WriteLine("请联系 duzhi5368@gmail.com 谢谢");
                        outfile.WriteLine(DateTime.Now + "");
                        outfile.Write(ex.ToString());
                    }
                }
                catch (Exception) { }
            }
        }
        public static void ReleaseMemory(bool removePages)
        {
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            if (removePages)
            {
                // just kidding
                WINAPI.ReleasePages(Process.GetCurrentProcess().Handle);
            }
        }
    }
}