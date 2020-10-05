using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Speech.Synthesis;

namespace Bililive_dm.TTS
{
    public static class TTS_NetFramework
    {
        public static string Download(string content)
        {
            var fileName = Path.Combine(StaticConfig.DefaultCacheDir, "TTS_" + BaseFunc.GetRandomString(10) + "_NETFRA.wav");
            var frame = new DispatcherFrame();
            var thread = new Thread(() =>
            {
                using (var synth = new SpeechSynthesizer()
                {
                    Rate = StaticConfig.TTS_NETFRAMEWORK_SPEECH_SPEED
                })
                {
                    synth.SetOutputToWaveFile(fileName);
                    synth.Speak(content);
                }
                frame.Continue = false;
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            Dispatcher.PushFrame(frame);
            return fileName;
        }
    }
}
