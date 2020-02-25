using System;
using System.Runtime.InteropServices;

namespace Utilities
{

    public enum ExternalMacrosDefinedConstantsName
    {
        _SC_PAGESIZE = 1,
        PROT_READ = 2,
        PROT_WRITE = 3,
        MAP_HUGETLB = 4,
    }

    public static class EnumExtensions
    {
        [DllImport(@"MacrosCstVal.so")]
        private static extern int getSystemCstValues(int id);

        public static int getValue(this ExternalMacrosDefinedConstantsName val)
        {
            return getSystemCstValues((int)val);
        }
    }
}
