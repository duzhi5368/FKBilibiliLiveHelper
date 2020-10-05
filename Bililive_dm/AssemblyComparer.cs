using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bililive_dm
{
    public class AssemblyComparer : IComparer<Assembly>
    {
        public int Compare(Assembly a, Assembly b)
        {
            return string.Compare(a.FullName, b.FullName);
        }
    }
}
