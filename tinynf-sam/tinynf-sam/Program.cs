using System;
using System.Runtime.InteropServices;

namespace tinynf_sam
{
    class Program
    {

        [DllImport("libc", SetLastError = true)]
        public unsafe static extern int getcpu(uint* cpu, uint* node, void* tcache);

        unsafe static void Main(string[] args)
        {
            uint node = uint.MaxValue;
            int v = getcpu(null, &node, null);
            Console.WriteLine("v = " + v);
            Console.WriteLine("current node = " + node);

        }

    }
}