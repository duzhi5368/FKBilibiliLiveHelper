using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bililive_dm
{
    public class TTSEntry
    {
        public string Filename { get; set; }
        public bool DoNotDelete { get; set; }
        public TTSEntry(string filename = "", bool doNotDelete = false)
        {
            Filename = filename;
            DoNotDelete = doNotDelete;
        }
    }
}
