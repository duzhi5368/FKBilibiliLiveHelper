using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Bililive_dm.TTS
{
    public static partial class TTS_BaiduExtra
    {
        public static async Task<string> Download(string content, SpeechPerson person)
        {
            var errorCount = 0;
        Retry:
            try
            {
                var fileName = Path.Combine(StaticConfig.DefaultCacheDir, "TTS_" + BaseFunc.GetRandomString(10) + "_BAIEXT.mp3");
                using (var downloader = new WebClient())
                {
                    if (string.IsNullOrEmpty(StaticConfig.ApiBaiduAiAccessToken))
                    {
                        var rawJson =
                            await downloader.DownloadStringTaskAsync(
                                $"https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials&client_id={StaticConfig.ApiBaiduAiAppKey}&client_secret={StaticConfig.ApiBaiduAiSecretKey}"
                                );
                        StaticConfig.ApiBaiduAiAccessToken = JObject.Parse(rawJson)["access_token"].ToString();
                    }
                    downloader.Headers.Add(HttpRequestHeader.AcceptEncoding, "identity;q=1, *;q=0");
                    downloader.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/75.0.3770.100 Safari/537.36");
                    var url = StaticConfig.ApiBaiduAi
                        .Replace("$PERSON", ((int)person).ToString())
                        .Replace("$SPEED", 5.ToString())
                        .Replace("$PITCH", 5.ToString())
                        .Replace("$TOKEN", StaticConfig.ApiBaiduAiAccessToken)
                        .Replace("$TTSTEXT", content);
                    await downloader.DownloadFileTaskAsync(url, fileName);
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

        public static SpeechPerson ParseToSpeechPerson(int index)
        {
            switch (index)
            {
                case 0: return SpeechPerson.DuXiaoMei;
                case 1: return SpeechPerson.DuXiaoYu;
                case 2: return SpeechPerson.DuXiaoYao;
                case 3: return SpeechPerson.DuYaYa;
                case 4: return SpeechPerson.DuXiaoJiao;
                case 5: return SpeechPerson.DuMiDuo;
                case 6: return SpeechPerson.DuBoWen;
                case 7: return SpeechPerson.DuXiaoTong;
                case 8: return SpeechPerson.DuXiaoMeng;
                default: return SpeechPerson.DuXiaoMei;
            }
        }

        public enum SpeechPerson
        {
            DuXiaoMei = 0,
            DuXiaoYu = 1,
            DuXiaoYao = 3,
            DuYaYa = 4,
            DuXiaoJiao = 5,
            DuMiDuo = 103,
            DuBoWen = 106,
            DuXiaoTong = 110,
            DuXiaoMeng = 111
        }
    }
}
