using System;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Text;

namespace Bililive_dm
{
    public static class BaseFunc
    {
        private static bool net461 = false;
        public static void Get45or451FromRegistry()
        {
            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
            {
                int releaseKey = Convert.ToInt32(ndpKey?.GetValue("Release"));
                if (releaseKey >= 394254)
                {
                    net461 = true;
                }
                else
                {
                    net461 = false;
                }
            }
        }

        public static bool IsNet461() {
            return net461;
        }

        public static string GetRandomString(int length = 10)
        {
            const string valid = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = System.BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }
            return "TTS" + res.ToString();
        }
    }
}
