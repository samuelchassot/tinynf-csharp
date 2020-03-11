using System;
using Env.linuxx86;

namespace tinynf_sam
{
    public static class IxgbeConstants
    {

        //I use const whenever it is possible and readonly when it is not. Need to test if readonly
        //doesn't sacrify performance. Const replaces inline normally so maybe gain performance.


        // ---------
        // Constants
        // ---------

        // The following are constants defined by the data sheet.

        // Section 7.1.2.5 L3/L4 5-tuple Filters:
        // 	"There are 128 different 5-tuple filter configuration registers sets"
        public const uint IXGBE_5TUPLE_FILTERS_COUNT = 128u;
        // Section 7.3.1 Interrupts Registers:
        //	"These registers are extended to 64 bits by an additional set of two registers.
        //	 EICR has an additional two registers EICR(1)... EICR(2) and so on for the EICS, EIMS, EIMC, EIAM and EITR registers."
        public const uint IXGBE_INTERRUPT_REGISTERS_COUNT = 2u;
        // Section 7.10.3.10 Switch Control:
        //	"Multicast Table Array (MTA) - a 4 Kb array that covers all combinations of 12 bits from the MAC destination address."
        public const uint IXGBE_MULTICAST_TABLE_ARRAY_SIZE = 4u * 1024u;
        // Section 8.2.3.8.7 Split Receive Control Registers: "Receive Buffer Size for Packet Buffer. Value can be from 1 KB to 16 KB"
        // Section 7.2.3.2.2 Legacy Transmit Descriptor Format: "The maximum length associated with a single descriptor is 15.5 KB"
        public const uint IXGBE_PACKET_BUFFER_SIZE_MAX = 15u * 1024u + 512u;
        // Section 7.1.1.1.1 Unicast Filter:
        // 	"The Ethernet MAC address is checked against the 128 host unicast addresses"
        public const uint IXGBE_RECEIVE_ADDRS_COUNT = 128u;
        // Section 1.3 Features Summary:
        // 	"Number of Rx Queues (per port): 128"
        public const uint IXGBE_RECEIVE_QUEUES_COUNT = 128u;
        // 	"Number of Tx Queues (per port): 128"
        public const uint IXGBE_TRANSMIT_QUEUES_COUNT = 128u;
        // Section 7.1.2 Rx Queues Assignment:
        // 	"Packets are classified into one of several (up to eight) Traffic Classes (TCs)."
        public const uint IXGBE_TRAFFIC_CLASSES_COUNT = 8u;
        // Section 7.10.3.10 Switch Control:
        // 	"Unicast Table Array (PFUTA) - a 4 Kb array that covers all combinations of 12 bits from the MAC destination address"
        public const uint IXGBE_UNICAST_TABLE_ARRAY_SIZE = 4u * 1024u;
        // Section 7.10.3.2 Pool Selection:
        // 	"64 shared VLAN filters"
        public const uint IXGBE_VLAN_FILTER_COUNT = 64u;

        // ----------
        // Parameters
        // ----------

        public const uint IXGBE_PACKET_BUFFER_SIZE = 2 * 1024u;

        //equivalent of static assert:
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint PACKET_BUFFER_SIZE_must_be_smaller_than_BUFFER_SIZE_MAX = IXGBE_PACKET_BUFFER_SIZE < IXGBE_PACKET_BUFFER_SIZE_MAX ? 0 : -1;

        // Section 7.2.3.3 Transmit Descriptor Ring:
        // "Transmit Descriptor Length register (TDLEN 0-127) - This register determines the number of bytes allocated to the circular buffer. This value must be 0 modulo 128."
        public const uint IXGBE_RING_SIZE = 1024u;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint RING_SIZE_must_be_0_mod_128 = IXGBE_RING_SIZE % 128 == 0 ? 0 : -1;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint RING_SIZE_must_be_power_of_2 = (IXGBE_RING_SIZE & (IXGBE_RING_SIZE - 1)) == 0 ? 0 : -1;

        // Maximum number of transmit queues assigned to an agent.
        public const uint IXGBE_AGENT_OUTPUTS_MAX = 4u;

        // Updating period for the transmit tail
        public const int IXGBE_AGENT_PROCESS_PERIOD = 8;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint PROCESS_PERIOD_must_be_greater_or_equal_than_1 = IXGBE_AGENT_PROCESS_PERIOD >= 1 ? 0 : -1;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint PROCESS_PERIOD_must_be_smaller_than_RING_SIZE = IXGBE_AGENT_PROCESS_PERIOD < IXGBE_RING_SIZE ? 0 : -1;

        // Updating period for receiving transmit head updates from the hardware and writing new values of the receive tail based on it.
        public const int IXGBE_AGENT_TRANSMIT_PERIOD = 64;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint TRANSMIT_PERIOD_must_be_greater_or_equal_than_1 = IXGBE_AGENT_TRANSMIT_PERIOD >= 1 ? 0 : -1;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint TRANSMIT_PERIOD_must_be_smaller_than_RING_SIZE = IXGBE_AGENT_TRANSMIT_PERIOD < IXGBE_RING_SIZE ? 0 : -1;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "used as static assert")]
        const uint TRANSMIT_PERIOD_must_be_power_of_2 = (IXGBE_AGENT_TRANSMIT_PERIOD & (IXGBE_AGENT_TRANSMIT_PERIOD - 1)) == 0 ? 0 : -1;

        public static uint BitNSet(int n)
        {
            if (n > 31)
            {
                return 0u;
            }
            return 1u << n;

        }

        public static uint BitNSet(int start, int end)
        {
            if (end >= 31 || start < 0 || start > 31)
            {
                return 0u;
            }
            return (uint.MaxValue << (end + 1)) ^ (uint.MaxValue << start);

        }

        public static ulong BitNSetLong(int n)
        {
            if (n > 63)
            {
                return 0ul;
            }
            return 1ul << n;

        }

        public static ulong BitNSetLong(int start, int end)
        {
            if (end >= 63 || start < 0 || start > 63)
            {
                return 0ul;
            }
            return (ulong.MaxValue << (end + 1)) ^ (ulong.MaxValue << start);

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
