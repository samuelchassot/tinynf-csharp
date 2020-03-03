using System;
using System.Threading;

namespace Env.linuxx86
{
    public static class Time
    {
        public static void SleepMicroSec(int microseconds)
        {
            double mili = microseconds / 1000.0;
            Thread.Sleep((int)Math.Ceiling(mili));
        }
    }
}
