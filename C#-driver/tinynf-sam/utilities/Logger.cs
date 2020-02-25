using System;
namespace Utilities
{
    // Logging utilities.
    // There are 4 log levels:
    // - 0: No logging at all
    // - 1: Logging important information, like "initialization complete"
    // - 2: Logging debugging information, like "some operation failed, although the overall system may be fine"
    // - 3: Logging everything, like "wrote value 0xAB to register CD of device 3"
    public class Logger
    {
        private byte level;
        public Logger(byte level)
        {
            this.level = level;
        }
        public void Verbose(string msg)
        {
            if (level >= 3)
            {
                Console.SetOut(Console.Error);
                Console.WriteLine("VERBOSE");
                Console.WriteLine(msg);
                Console.Error.Flush();
                Console.SetOut(Console.Out);
            }
        }

        public void Debug(string msg)
        {
            if (level >= 2)
            {
                Console.SetOut(Console.Error);
                Console.WriteLine("DEBUG");
                Console.WriteLine(msg);
                Console.Error.Flush();
                Console.SetOut(Console.Out);
            }
        }

        public void Info(string msg)
        {
            if (level >= 1)
            {
                Console.SetOut(Console.Error);
                Console.WriteLine("INFO");
                Console.WriteLine(msg);
                Console.Error.Flush();
                Console.SetOut(Console.Out);
            }
        }
    }
}
