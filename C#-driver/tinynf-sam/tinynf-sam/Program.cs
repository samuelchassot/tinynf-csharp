using System;
using System.Runtime.InteropServices;
using System.Linq;
using Env.linuxx86;

namespace tinynf_sam
{
    class Program
    {
        unsafe static void Main(string[] args)
        {
            Console.WriteLine("Test program:");
            var ptr = Memory.Tn_mem_allocate(2048ul);
            Console.WriteLine(ptr);
            
        }

    }
}