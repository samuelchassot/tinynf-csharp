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

            uint a = BitNSet(1, 5);
            Console.WriteLine(Convert.ToString(a, 2));            
        }

        
    }

}