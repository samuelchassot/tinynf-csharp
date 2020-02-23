using System;
using System.Runtime.InteropServices;
using System.Linq;
using Env.linuxx86;
namespace tinynf_sam
{
    class Program
    {

        [DllImport("libc", SetLastError = true)]
        public unsafe static extern int getcpu(uint* cpu, uint* node, void* tcache);

        unsafe static void Main(string[] args)
        {
            char[] line = Filesystem.Tn_fs_readline(1024, args[0]);
            Console.WriteLine(line);

        }

    }
}