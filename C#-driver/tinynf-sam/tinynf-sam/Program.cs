using System;
using System.Runtime.InteropServices;
using System.Linq;
using Env.linuxx86;

namespace tinynf_sam
{
    class Program
    {

        [DllImport(@"CWrapper.so")]
        public static extern void PrintHelloWorld();

        unsafe static void Main(string[] args)
        {
            Console.WriteLine("salut");
            PrintHelloWorld();
        }

    }
}