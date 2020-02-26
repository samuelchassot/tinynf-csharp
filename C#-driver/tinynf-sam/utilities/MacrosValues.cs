using System;
using System.Runtime.InteropServices;

namespace Utilities
{

    public enum MacrosValues
    {
        _SC_PAGESIZE = 1,
        PROT_READ = 2,
        PROT_WRITE = 3,
        MAP_HUGETLB = 4,
        MAP_ANONYMOUS = 5,
        MAP_SHARED = 6,
        MAP_POPULATE = 7
    }

    public static class EnumExtensions
    {
        [DllImport(@"MacrosCstVal.so")]
        private static extern int getSystemCstValues(int id);

        public static int GetValue(this MacrosValues val)
        {
            return getSystemCstValues((int)val);
        }
    }
}
