using System;
using Env.linuxx86;
using Utilities;

namespace tinynf_sam
{
    public class Ixgbe
    {


        private NetDevice device;
        private readonly Logger log = new Logger(Constants.logLevel);
        public Ixgbe()
        {
        }

        /// <summary>
        /// Poll for the condition regularly and return true if and only if the
        /// condition is still true after the given timeoutMicroSec. Return false
        /// if condition is false at any poll or at the end
        /// </summary>
        /// <param name="timeoutMicroSec"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public static bool TimeoutCondition(int timeoutMicroSec, bool condition)
        {
            bool timedOut = true;
            Time.SleepMicroSec(timeoutMicroSec % 10);
            for (int i = 0; i < 10; i++)
            {
                if (!condition)
                {
                    timedOut = false;
                    break;
                }
                Time.SleepMicroSec(timeoutMicroSec / 10);
            }
            return timedOut;
        }


    }
}
