using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows;

namespace Bililive_dm
{
    public partial class Plugin_TTSReply
    {
        public void GlobalErrorHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if (!StaticConfig.CatchGlobalError) 
                return;
            MessageBox.Show("已捕获Bilibili小助手不明错误，日志保存在桌面，请联系作者：freeknight ，联系方式： duzhi5368@gmail.com");
            SystemSounds.Hand.Play();
            var obj = (Exception)e.ExceptionObject;
            var sb = new StringBuilder($"**CRITICAL ERROR REPORT**\nTime: {DateTime.Now:o}\n\n");
            sb.Append($"Error details:\n{obj}\n\n");
            try
            {
                sb.Append($"CLR: {Environment.Version}\n");
                var assembliesArray = AppDomain.CurrentDomain.GetAssemblies();
                var assemblies = assembliesArray.ToList();
                assemblies.Sort(new AssemblyComparer());
                sb.Append($"Loaded assemblies ({assemblies.Count}): \n");
                foreach (var assembly in assemblies)
                {
                    sb.Append($"{assembly.FullName}{(!assembly.IsDynamic ? $"@{assembly.Location}" : "")}\n");
                }
            }
            catch (Exception ex)
            {
                sb.Append($"(Unable to retrieve assembly info: {ex.Message})\n");
            }

            try
            {
                using (StreamWriter outfile = new StreamWriter(StaticConfig.ErrorLogFilePath))
                {
                    outfile.WriteLine("请联系 duzhi5368@gmail.com 谢谢");
                    outfile.WriteLine(DateTime.Now + "");
                    outfile.Write(sb);
                }
            }
            catch (Exception) { }
        }
    }
}
