using System;
using System.Threading;

namespace Env.linuxx86
{
    public static class Time
    {
        /// <summary>
        /// Sleep for the given time in microseconds. Given the implementation of the
        /// sleep function in .Net, if the given time is < 1 milisecond, it will
        /// sleep for 1 ms
        /// </summary>
        /// <param name="microseconds"></param>
        public static void SleepMicroSec(int microseconds)
        {
            double mili = microseconds / 1000.0;
            Thread.Sleep((int)Math.Ceiling(mili));
        }
    }
}
