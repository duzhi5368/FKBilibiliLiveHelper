using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Net;

namespace Bililive_dm.TTS
{
    public static class TTS_Youdao
    {
        public static async Task<string> Download(string content)
        {
            var errorCount = 0;
        Retry:
            try
            {
                var fileName = Path.Combine(StaticConfig.DefaultCacheDir, "TTS_" + BaseFunc.GetRandomString(10) + "_YOUDAO.mp3");
                using (var downloader = new WebClient())
                {
                    downloader.Headers.Add(HttpRequestHeader.AcceptEncoding, "identity;q=1, *;q=0");
                    downloader.Headers.Add(HttpRequestHeader.Referer, "http://fanyi.youdao.com/");
                    downloader.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36");
                    await downloader.DownloadFileTaskAsync(StaticConfig.ApiYoudao.Replace("$TTSTEXT", content),
                                                           fileName);
                    // validate if file is playable
                    using (var reader = new AudioFileReader(fileName)) { }
                    return fileName;
                }
            }
            catch (Exception)
            {
                errorCount += 1;
                if (errorCount <= StaticConfig.TTS_DOWNLOAD_FAIL_RETRY)
                {
                    goto Retry;
                }
                return null;
            }
        }
    }
}
