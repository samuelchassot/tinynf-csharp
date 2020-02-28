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
            var mem = new Memory();
            var ptr = mem.Tn_mem_allocate(2048ul);
            Console.WriteLine(ptr);
            var ptrvirt = mem.Tn_mem_phys_to_virt((UIntPtr)(1ul << 32), 5);
            Console.WriteLine(ptrvirt);
            Console.WriteLine(ptr);
            
        }

    }
}