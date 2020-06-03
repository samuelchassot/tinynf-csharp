using System;
using System.Runtime.InteropServices;

namespace Utilities
{

    public enum MacrosValues
    {
        _SC_PAGESIZE = 1
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
