using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using System.Threading;
using NAudio.Wave;
using System.IO;
using Bililive_dm.TTS;

namespace Bililive_dm
{
    public static class TTSPlayer
    {
        public static async Task UnifiedPlay(string content, int engineType, int speechPerson, List<TTSEntry> gTTSList)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }
            string fileName = "";
            //content = HttpUtility.UrlEncode(content);
            switch (engineType)
            {
                default:
                    fileName = await TTS_BaiduBase.Download(content);
                    break;
                case 1:
                    fileName = TTS_NetFramework.Download(content);
                    break;
                case 2:
                    fileName = TTS_Google.Download(content, "zh-CN");
                    break;
                case 3:
                    fileName = await TTS_Youdao.Download(content);
                    break;
                case 4:
                    fileName = await TTS_BaiduExtra.Download(content, TTS_BaiduExtra.ParseToSpeechPerson(speechPerson));
                    break;
            }
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            gTTSList.Add(new TTSEntry(fileName));
        }

        public static bool IsNearEnough(this float _float, float comparison, float epsilon = float.Epsilon) => Math.Abs(_float - comparison) < epsilon;
        public static void PlayTTS(string filename, int ttsVolume, bool isUseTTS, bool wait = true, bool forceKeepCache = false)
        {
            var frame = new DispatcherFrame();
            var thread = new Thread(() =>
            {
                try
                {
                    using (var reader = new AudioFileReader(filename))
                    {
                        using (var waveOut = new WaveOutEvent())
                        {
                            waveOut.Init(reader);
                            reader.Volume = ((float)ttsVolume) / 100;
                            waveOut.Play();
                            if (!wait)
                                frame.Continue = false;
                            while (waveOut.PlaybackState != PlaybackState.Stopped)
                            {
                                if (!isUseTTS)
                                    waveOut.Stop();
                                if (!reader.Volume.IsNearEnough(((float)ttsVolume) / 100, 0.02f))
                                {
                                    reader.Volume = ((float)ttsVolume) / 100;
                                }
                                Thread.Sleep(StaticConfig.TTS_SPEACH_THREAD_ELSP);
                            }
                        }
                    }
                    if (!forceKeepCache)
                    {
                        File.Delete(filename);
                    }
                }
                catch (Exception ex)
                {
                     throw new Exception($"播放过程中发生错误: {Path.GetFileName(filename)}: {ex.Message}");
                }
                frame.Continue = false;
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Dispatcher.PushFrame(frame);
        }
    }
}
