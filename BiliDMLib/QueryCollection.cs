using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BiliDMLib
{
    public sealed class QueryCollection : List<KeyValuePair<string, string>>
    {
        public void Add(string key, string value)
        {
            Add(new KeyValuePair<string, string>(key, value));
        }
    }
}
