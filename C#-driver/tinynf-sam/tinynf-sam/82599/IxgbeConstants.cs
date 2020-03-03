using System;
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


        // -------------
        // PCI registers
        // -------------

        // Section 9.3.2 PCIe Configuration Space Summary: "0x10 Base Address Register 0" (32 bit), "0x14 Base Address Register 1" (32 bit)
        public const uint IXGBE_PCIREG_BAR0_LOW = 0x10u;
        public const uint IXGBE_PCIREG_BAR0_HIGH = 0x14u;

        // Section 9.3.3.3 Command Register (16 bit)
        // Section 9.3.3.4 Status Register (16 bit, unused)
        public const ushort IXGBE_PCIREG_COMMAND = (ushort)0x04u;
        public static readonly ushort IXGBE_PCIREG_COMMAND_MEMORY_ACCESS_ENABLE = (ushort)BitNSet(1);
        public static readonly ushort IXGBE_PCIREG_COMMAND_BUS_MASTER_ENABLE = (ushort)BitNSet(2);
        public static readonly ushort IXGBE_PCIREG_COMMAND_INTERRUPT_DISABLE = (ushort)BitNSet(10);

        // Section 9.3.10.6 Device Status Register (16 bit)
        // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
        public const ushort IXGBE_PCIREG_DEVICESTATUS = (ushort)0xAAu;
        public static readonly ushort IXGBE_PCIREG_DEVICESTATUS_TRANSACTIONPENDING = (ushort)BitNSet(5);

        // Section 9.3.3.1 Vendor ID Register (16 bit)
        // Section 9.3.3.2 Device ID Register (16 bit)
        public const ushort IXGBE_PCIREG_ID = (ushort)0x00u;

        // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
        // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
        // Section 9.3.7.1.6 Data Register (8 bit, unused)
        public const ushort IXGBE_PCIREG_PMCSR = (ushort)0x44u;
        public static readonly uint IXGBE_PCIREG_PMCSR_POWER_STATE = BitNSet(0, 1);


        // -------------
        // NIC registers
        // -------------

        // Section 8.2.3.1.1 Device Control Register
        public const uint IXGBE_REG_CTRL = 0x00000u;
        public static readonly uint IXGBE_REG_CTRL_MASTER_DISABLE = BitNSet(2);
        public static readonly uint IXGBE_REG_CTRL_RST = BitNSet(26);

        // Section 8.2.3.1.3 Extended Device Control Register
        public const uint IXGBE_REG_CTRLEXT = 0x00018u;
        public static readonly uint IXGBE_REG_CTRLEXT_NSDIS = BitNSet(16);

        // Section 8.2.3.11.1 Rx DCA Control Register
        public static uint IXGBE_REG_DCARXCTRL(int n) => n <= 63u ? (uint)(0x0100Cu + (0x40u * n)) : (uint)(0x0D00Cu + (0x40u * (n - 64u)));

        // This bit is reserved, has no name, but must be used anyway
        public static readonly uint IXGBE_REG_DCARXCTRL_UNKNOWN = BitNSet(12);

        // Section 8.2.3.11.2 Tx DCA Control Registers
        public static uint IXGBE_REG_DCATXCTRL(int n) { return (uint)(0x0600Cu + 0x40u * n); }
        public static readonly uint IXGBE_REG_DCATXCTRL_TX_DESC_WB_RO_EN = BitNSet(11);

        // Section 8.2.3.9.2 DMA Tx Control
        public const uint IXGBE_REG_DMATXCTL = 0x04A80u;
        public static readonly uint IXGBE_REG_DMATXCTL_TE = BitNSet(0);

        // Section 8.2.3.9.1 DMA Tx TCP Max Allow Size Requests
        public const uint IXGBE_REG_DTXMXSZRQ = 0x08100u;
        public static readonly uint IXGBE_REG_DTXMXSZRQ_MAX_BYTES_NUM_REQ = BitNSet(0, 11);

        // Section 8.2.3.2.1 EEPROM/Flash Control Register
        public const uint IXGBE_REG_EEC = 0x10010u;
        public static readonly uint IXGBE_REG_EEC_EE_PRES = BitNSet(8);
        public static readonly uint IXGBE_REG_EEC_AUTO_RD = BitNSet(9);

        // Section 8.2.3.5.9 Extended Interrupt Mask Clear Registers
        public static uint IXGBE_REG_EIMC(int n) => (uint)(0x00AB0u + (4u * n));
        public static readonly uint IXGBE_REG_EIMC_MASK = BitNSet(0, 31);

        // Section 8.2.3.3.4 Flow Control Receive Threshold High
        public static uint IXGBE_REG_FCRTH(int n) => (uint)(0x03260u + (4u * n));
        public static readonly uint IXGBE_REG_FCRTH_RTH = BitNSet(5, 18);

        // Section 8.2.3.7.1 Filter Control Register (FCTRL)
        public const uint IXGBE_REG_FCTRL = 0x05080u;
        public static readonly uint IXGBE_REG_FCTRL_MPE = BitNSet(8);
        public static readonly uint IXGBE_REG_FCTRL_UPE = BitNSet(9);

        // Section 8.2.3.7.19 Five tuple Queue Filter
        public static uint IXGBE_REG_FTQF(int n) => (uint)(0x0E600u + (4u * n));
        public static readonly uint IXGBE_REG_FTQF_QUEUE_ENABLE = BitNSet(31);

        // Section 8.2.3.4.10 Firmware Semaphore Register
        public const uint IXGBE_REG_FWSM = 0x10148u;
        public static readonly uint IXGBE_REG_FWSM_EXT_ERR_IND = BitNSet(19, 24);

        // Section 8.2.3.4.12 PCIe Control Extended Register
        public const uint IXGBE_REG_GCREXT = 0x11050u;
        public static readonly uint IXGBE_REG_GCREXT_BUFFERS_CLEAR_FUNC = BitNSet(30);

        // Section 8.2.3.22.8 MAC Core Control 0 Register
        public const uint IXGBE_REG_HLREG0 = 0x04240u;
        public static readonly uint IXGBE_REG_HLREG0_LPBK = BitNSet(15);

        // Section 8.2.3.22.34 MAC Flow Control Register
        public const uint IXGBE_REG_MFLCN = 0x04294u;
        public static readonly uint IXGBE_REG_MFLCN_RFCE = BitNSet(3);

        // Section 8.2.3.7.10 MAC Pool Select Array
        public static uint IXGBE_REG_MPSAR(int n) => (uint)(0x0A600u + (4u * n));

        // Section 8.2.3.7.7 Multicast Table Array
        public static uint IXGBE_REG_MTA(int n) => (uint)(0x05200u + (4u * n));

        // Section 8.2.3.27.17 PF Unicast Table Array
        public static uint IXGBE_REG_PFUTA(int n) => (uint)(0x0F400u + (4u * n));

        // Section 8.2.3.27.15 PF VM VLAN Pool Filter
        public static uint IXGBE_REG_PFVLVF(int n) => (uint)(0x0F100u + (4u * n));

        // Section 8.2.3.27.16 PF VM VLAN Pool Filter Bitmap
        public static uint IXGBE_REG_PFVLVFB(int n) => (uint)(0x0F200u + (4u * n));

        // Section 8.2.3.8.2 Receive Descriptor Base Address High
        public static uint IXGBE_REG_RDBAH(int n) => (uint)(n <= 63u ? (0x01004u + (0x40u * n)) : (0x0D004u + (0x40u * (n - 64u))));

        // Section 8.2.3.8.1 Receive Descriptor Base Address Low
        public static uint IXGBE_REG_RDBAL(int n) => (uint)(n <= 63u ? (0x01000u + (0x40u * n)) : (0x0D000u + 0x40u * (n - 64u)));

        // Section 8.2.3.8.3 Receive Descriptor Length
        public static uint IXGBE_REG_RDLEN(int n) => (uint)(n <= 63u ? (0x01008u + (0x40u * n)) : (0x0D008 + (0x40u * (n - 64u))));

        // Section 8.2.3.8.8 Receive DMA Control Register
        // INTERPRETATION-MISSING: Bit 0, which is not mentioned in the table, is reserved
        public const uint IXGBE_REG_RDRXCTL = 0x02F00u;
        public static readonly uint IXGBE_REG_RDRXCTL_CRC_STRIP = BitNSet(1);
        public static readonly uint IXGBE_REG_RDRXCTL_DMAIDONE = BitNSet(3);
        public static readonly uint IXGBE_REG_RDRXCTL_RSCFRSTSIZE = BitNSet(17, 24);
        public static readonly uint IXGBE_REG_RDRXCTL_RSCACKC = BitNSet(25);
        public static readonly uint IXGBE_REG_RDRXCTL_FCOE_WRFIX = BitNSet(26);

        // Section 8.2.3.8.5 Receive Descriptor Tail
        public static uint IXGBE_REG_RDT(int n) => (uint)(n <= 63u ? (0x01018u + (0x40u * n)) : (0x0D018u + 0x40u * (n - 64u)));

        // Section 8.2.3.10.2 DCB Transmit Descriptor Plane Control and Status
        public const uint IXGBE_REG_RTTDCS = 0x04900u;
        public static readonly uint IXGBE_REG_RTTDCS_ARBDIS = BitNSet(6);

        // Section 8.2.3.8.10 Receive Control Register
        public const uint IXGBE_REG_RXCTRL = 0x03000u;
        public static readonly uint IXGBE_REG_RXCTRL_RXEN = BitNSet(0);

        // Section 8.2.3.8.6 Receive Descriptor Control
        public static uint IXGBE_REG_RXDCTL(int n) => (uint)(n <= 63u ? (0x01028u + 0x40u * n) : (0x0D028u + 0x40u * (n - 64u)));
        public static readonly uint IXGBE_REG_RXDCTL_ENABLE = BitNSet(25);

        // Section 8.2.3.8.9 Receive Packet Buffer Size
        public static uint IXGBE_REG_RXPBSIZE(int n) => (uint)(0x03C00u + (4u * n));

        // Section 8.2.3.12.5 Security Rx Control
        public const uint IXGBE_REG_SECRXCTRL = 0x08D00u;
        public static readonly uint IXGBE_REG_SECRXCTRL_RX_DIS = BitNSet(1);

        // Section 8.2.3.12.6 Security Rx Status
        public const uint IXGBE_REG_SECRXSTAT = 0x08D04u;
        public static readonly uint IXGBE_REG_SECRXSTAT_SECRX_RDY = BitNSet(0);

        // Section 8.2.3.8.7 Split Receive Control Registers
        public static uint IXGBE_REG_SRRCTL(int n) => (uint)(n <= 63u ? (0x01014u + (0x40u * n)) : (0x0D014u + (0x40u * (n - 64u))));
        public static readonly uint IXGBE_REG_SRRCTL_BSIZEPACKET = BitNSet(0, 4);
        public static readonly uint IXGBE_REG_SRRCTL_DROP_EN = BitNSet(28);

        // Section 8.2.3.1.2 Device Status Register
        public const uint IXGBE_REG_STATUS = 0x00008u;
        public static readonly uint IXGBE_REG_STATUS_PCIE_MASTER_ENABLE_STATUS = BitNSet(19);

        // Section 8.2.3.9.6 Transmit Descriptor Base Address High
        public static uint IXGBE_REG_TDBAH(int n) => (uint)(0x06004u + (0x40u * n));

        // Section 8.2.3.9.5 Transmit Descriptor Base Address Low
        public static uint IXGBE_REG_TDBAL(int n) => (uint)(0x06000u + (0x40u * n));

        // Section 8.2.3.9.7 Transmit Descriptor Length
        public static uint IXGBE_REG_TDLEN(int n) => (uint)(0x06008u + (0x40u * n));

        // Section 8.2.3.9.9 Transmit Descriptor Tail
        public static uint IXGBE_REG_TDT(int n) => (uint)(0x06018u + (0x40u * n));

        // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address High
        public static uint IXGBE_REG_TDWBAH(int n) => (uint)(0x0603Cu + (0x40u * n));

        // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address Low
        public static uint IXGBE_REG_TDWBAL(int n) => (uint)(0x06038u + (0x40u * n));

        // Section 8.2.3.9.10 Transmit Descriptor Control
        public static uint IXGBE_REG_TXDCTL(int n) => (uint)(0x06028u + (0x40u * n));
        public static readonly uint IXGBE_REG_TXDCTL_PTHRESH = BitNSet(0, 6);
        public static readonly uint IXGBE_REG_TXDCTL_HTHRESH = BitNSet(8, 14);
        public static readonly uint IXGBE_REG_TXDCTL_ENABLE = BitNSet(25);

        // Section 8.2.3.9.13 Transmit Packet Buffer Size
        public static uint IXGBE_REG_TXPBSIZE(int n) => (uint)(0x0CC00u + (4u * n));

        // Section 8.2.3.9.16 Tx Packet Buffer Threshold
        public static uint IXGBE_REG_TXPBTHRESH(int n) => (uint)(0x04950u + (4u * n));
        public static readonly uint IXGBE_REG_TXPBTHRESH_THRESH = BitNSet(0, 9);

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
    }
}
