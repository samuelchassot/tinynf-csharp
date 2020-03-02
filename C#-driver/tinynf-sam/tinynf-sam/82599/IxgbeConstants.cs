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
        private const uint IXGBE_5TUPLE_FILTERS_COUNT = 128u;
        // Section 7.3.1 Interrupts Registers:
        //	"These registers are extended to 64 bits by an additional set of two registers.
        //	 EICR has an additional two registers EICR(1)... EICR(2) and so on for the EICS, EIMS, EIMC, EIAM and EITR registers."
        private const uint IXGBE_INTERRUPT_REGISTERS_COUNT = 2u;
        // Section 7.10.3.10 Switch Control:
        //	"Multicast Table Array (MTA) - a 4 Kb array that covers all combinations of 12 bits from the MAC destination address."
        private const uint IXGBE_MULTICAST_TABLE_ARRAY_SIZE = 4u * 1024u;
        // Section 8.2.3.8.7 Split Receive Control Registers: "Receive Buffer Size for Packet Buffer. Value can be from 1 KB to 16 KB"
        // Section 7.2.3.2.2 Legacy Transmit Descriptor Format: "The maximum length associated with a single descriptor is 15.5 KB"
        private const uint IXGBE_PACKET_BUFFER_SIZE_MAX = 15u * 1024u + 512u;
        // Section 7.1.1.1.1 Unicast Filter:
        // 	"The Ethernet MAC address is checked against the 128 host unicast addresses"
        private const uint IXGBE_RECEIVE_ADDRS_COUNT = 128u;
        // Section 1.3 Features Summary:
        // 	"Number of Rx Queues (per port): 128"
        private const uint IXGBE_RECEIVE_QUEUES_COUNT = 128u;
        // 	"Number of Tx Queues (per port): 128"
        private const uint IXGBE_TRANSMIT_QUEUES_COUNT = 128u;
        // Section 7.1.2 Rx Queues Assignment:
        // 	"Packets are classified into one of several (up to eight) Traffic Classes (TCs)."
        private const uint IXGBE_TRAFFIC_CLASSES_COUNT = 8u;
        // Section 7.10.3.10 Switch Control:
        // 	"Unicast Table Array (PFUTA) - a 4 Kb array that covers all combinations of 12 bits from the MAC destination address"
        private const uint IXGBE_UNICAST_TABLE_ARRAY_SIZE = 4u * 1024u;
        // Section 7.10.3.2 Pool Selection:
        // 	"64 shared VLAN filters"
        private const uint IXGBE_VLAN_FILTER_COUNT = 64u;


        // -------------
        // PCI registers
        // -------------

        // Section 9.3.2 PCIe Configuration Space Summary: "0x10 Base Address Register 0" (32 bit), "0x14 Base Address Register 1" (32 bit)
        private const uint IXGBE_PCIREG_BAR0_LOW = 0x10u;
        private const uint IXGBE_PCIREG_BAR0_HIGH = 0x14u;

        // Section 9.3.3.3 Command Register (16 bit)
        // Section 9.3.3.4 Status Register (16 bit, unused)
        private const ushort IXGBE_PCIREG_COMMAND = (ushort)0x04u;
        private readonly ushort IXGBE_PCIREG_COMMAND_MEMORY_ACCESS_ENABLE = (ushort)BitNSet(1);
        private readonly ushort IXGBE_PCIREG_COMMAND_BUS_MASTER_ENABLE = (ushort)BitNSet(2);
        private readonly ushort IXGBE_PCIREG_COMMAND_INTERRUPT_DISABLE = (ushort)BitNSet(10);

        // Section 9.3.10.6 Device Status Register (16 bit)
        // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
        private const ushort IXGBE_PCIREG_DEVICESTATUS = (ushort)0xAAu;
        private readonly ushort IXGBE_PCIREG_DEVICESTATUS_TRANSACTIONPENDING = (ushort)BitNSet(5);

        // Section 9.3.3.1 Vendor ID Register (16 bit)
        // Section 9.3.3.2 Device ID Register (16 bit)
        private const ushort IXGBE_PCIREG_ID = (ushort)0x00u;

        // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
        // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
        // Section 9.3.7.1.6 Data Register (8 bit, unused)
        private const ushort IXGBE_PCIREG_PMCSR = (ushort)0x44u;
        private readonly uint IXGBE_PCIREG_PMCSR_POWER_STATE = BitNSet(0, 1);


        // -------------
        // NIC registers
        // -------------

        // Section 8.2.3.1.1 Device Control Register
        private const uint IXGBE_REG_CTRL(_) 0x00000u;
#define IXGBE_REG_CTRL_MASTER_DISABLE BIT(2)
#define IXGBE_REG_CTRL_RST BIT(26)

        // Section 8.2.3.1.3 Extended Device Control Register
#define IXGBE_REG_CTRLEXT(_) 0x00018u
#define IXGBE_REG_CTRLEXT_NSDIS BIT(16)

        // Section 8.2.3.11.1 Rx DCA Control Register
#define IXGBE_REG_DCARXCTRL(n) ((n) <= 63u ? (0x0100Cu + 0x40u*(n)) : (0x0D00Cu + 0x40u*((n)-64u)))
        // This bit is reserved, has no name, but must be used anyway
#define IXGBE_REG_DCARXCTRL_UNKNOWN BIT(12)

        // Section 8.2.3.11.2 Tx DCA Control Registers
#define IXGBE_REG_DCATXCTRL(n) (0x0600Cu + 0x40u*(n))
#define IXGBE_REG_DCATXCTRL_TX_DESC_WB_RO_EN BIT(11)

        // Section 8.2.3.9.2 DMA Tx Control
#define IXGBE_REG_DMATXCTL(_) 0x04A80u
#define IXGBE_REG_DMATXCTL_TE BIT(0)

        // Section 8.2.3.9.1 DMA Tx TCP Max Allow Size Requests
#define IXGBE_REG_DTXMXSZRQ(_) 0x08100u
#define IXGBE_REG_DTXMXSZRQ_MAX_BYTES_NUM_REQ BITS(0,11)

        // Section 8.2.3.2.1 EEPROM/Flash Control Register
#define IXGBE_REG_EEC(_) 0x10010u
#define IXGBE_REG_EEC_EE_PRES BIT(8)
#define IXGBE_REG_EEC_AUTO_RD BIT(9)

        // Section 8.2.3.5.9 Extended Interrupt Mask Clear Registers
#define IXGBE_REG_EIMC(n) (0x00AB0u + 4u*(n))
#define IXGBE_REG_EIMC_MASK BITS(0,31)

        // Section 8.2.3.3.4 Flow Control Receive Threshold High
#define IXGBE_REG_FCRTH(n) (0x03260u + 4u*(n))
#define IXGBE_REG_FCRTH_RTH BITS(5,18)

        // Section 8.2.3.7.1 Filter Control Register (FCTRL)
#define IXGBE_REG_FCTRL(_) 0x05080u
#define IXGBE_REG_FCTRL_MPE BIT(8)
#define IXGBE_REG_FCTRL_UPE BIT(9)

        // Section 8.2.3.7.19 Five tuple Queue Filter
#define IXGBE_REG_FTQF(n) (0x0E600u + 4u*(n))
#define IXGBE_REG_FTQF_QUEUE_ENABLE BIT(31)

        // Section 8.2.3.4.10 Firmware Semaphore Register
#define IXGBE_REG_FWSM(_) 0x10148u
#define IXGBE_REG_FWSM_EXT_ERR_IND BITS(19,24)

        // Section 8.2.3.4.12 PCIe Control Extended Register
#define IXGBE_REG_GCREXT(_) 0x11050u
#define IXGBE_REG_GCREXT_BUFFERS_CLEAR_FUNC BIT(30)

        // Section 8.2.3.22.8 MAC Core Control 0 Register
#define IXGBE_REG_HLREG0(_) 0x04240u
#define IXGBE_REG_HLREG0_LPBK BIT(15)

        // Section 8.2.3.22.34 MAC Flow Control Register
#define IXGBE_REG_MFLCN(_) 0x04294u
#define IXGBE_REG_MFLCN_RFCE BIT(3)

        // Section 8.2.3.7.10 MAC Pool Select Array
#define IXGBE_REG_MPSAR(n) (0x0A600u + 4u*(n))

        // Section 8.2.3.7.7 Multicast Table Array
#define IXGBE_REG_MTA(n) (0x05200u + 4u*(n))

        // Section 8.2.3.27.17 PF Unicast Table Array
#define IXGBE_REG_PFUTA(n) (0x0F400u + 4u*(n))

        // Section 8.2.3.27.15 PF VM VLAN Pool Filter
#define IXGBE_REG_PFVLVF(n) (0x0F100u + 4u*(n))

        // Section 8.2.3.27.16 PF VM VLAN Pool Filter Bitmap
#define IXGBE_REG_PFVLVFB(n) (0x0F200u + 4u*(n))

        // Section 8.2.3.8.2 Receive Descriptor Base Address High
#define IXGBE_REG_RDBAH(n) ((n) <= 63u ? (0x01004u + 0x40u*(n)) : (0x0D004u + 0x40u*((n)-64u)))

        // Section 8.2.3.8.1 Receive Descriptor Base Address Low
#define IXGBE_REG_RDBAL(n) ((n) <= 63u ? (0x01000u + 0x40u*(n)) : (0x0D000u + 0x40u*((n)-64u)))

        // Section 8.2.3.8.3 Receive Descriptor Length
#define IXGBE_REG_RDLEN(n) ((n) <= 63u ? (0x01008u + 0x40u*(n)) : (0x0D008 + 0x40u*((n)-64u)))

        // Section 8.2.3.8.8 Receive DMA Control Register
        // INTERPRETATION-MISSING: Bit 0, which is not mentioned in the table, is reserved
#define IXGBE_REG_RDRXCTL(_) 0x02F00u
#define IXGBE_REG_RDRXCTL_CRC_STRIP BIT(1)
#define IXGBE_REG_RDRXCTL_DMAIDONE BIT(3)
#define IXGBE_REG_RDRXCTL_RSCFRSTSIZE BITS(17,24)
#define IXGBE_REG_RDRXCTL_RSCACKC BIT(25)
#define IXGBE_REG_RDRXCTL_FCOE_WRFIX BIT(26)

        // Section 8.2.3.8.5 Receive Descriptor Tail
#define IXGBE_REG_RDT(n) ((n) <= 63u ? (0x01018u + 0x40u*(n)) : (0x0D018u + 0x40u*((n)-64u)))

        // Section 8.2.3.10.2 DCB Transmit Descriptor Plane Control and Status
#define IXGBE_REG_RTTDCS(_) 0x04900u
#define IXGBE_REG_RTTDCS_ARBDIS BIT(6)

        // Section 8.2.3.8.10 Receive Control Register
#define IXGBE_REG_RXCTRL(_) 0x03000u
#define IXGBE_REG_RXCTRL_RXEN BIT(0)

        // Section 8.2.3.8.6 Receive Descriptor Control
#define IXGBE_REG_RXDCTL(n) ((n) <= 63u ? (0x01028u + 0x40u*(n)) : (0x0D028u + 0x40u*((n)-64u)))
#define IXGBE_REG_RXDCTL_ENABLE BIT(25)

        // Section 8.2.3.8.9 Receive Packet Buffer Size
#define IXGBE_REG_RXPBSIZE(n) (0x03C00u + 4u*(n))

        // Section 8.2.3.12.5 Security Rx Control
#define IXGBE_REG_SECRXCTRL(_) 0x08D00u
#define IXGBE_REG_SECRXCTRL_RX_DIS BIT(1)

        // Section 8.2.3.12.6 Security Rx Status
#define IXGBE_REG_SECRXSTAT(_) 0x08D04u
#define IXGBE_REG_SECRXSTAT_SECRX_RDY BIT(0)

        // Section 8.2.3.8.7 Split Receive Control Registers
#define IXGBE_REG_SRRCTL(n) ((n) <= 63u ? (0x01014u + 0x40u*(n)) : (0x0D014u + 0x40u*((n)-64u)))
#define IXGBE_REG_SRRCTL_BSIZEPACKET BITS(0,4)
#define IXGBE_REG_SRRCTL_DROP_EN BIT(28)

        // Section 8.2.3.1.2 Device Status Register
#define IXGBE_REG_STATUS(_) 0x00008u
#define IXGBE_REG_STATUS_PCIE_MASTER_ENABLE_STATUS BIT(19)

        // Section 8.2.3.9.6 Transmit Descriptor Base Address High
#define IXGBE_REG_TDBAH(n) (0x06004u + 0x40u*(n))

        // Section 8.2.3.9.5 Transmit Descriptor Base Address Low
#define IXGBE_REG_TDBAL(n) (0x06000u + 0x40u*(n))

        // Section 8.2.3.9.7 Transmit Descriptor Length
#define IXGBE_REG_TDLEN(n) (0x06008u + 0x40u*(n))

        // Section 8.2.3.9.9 Transmit Descriptor Tail
#define IXGBE_REG_TDT(n) (0x06018u + 0x40u*(n))

        // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address High
#define IXGBE_REG_TDWBAH(n) (0x0603Cu + 0x40u*(n))

        // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address Low
#define IXGBE_REG_TDWBAL(n) (0x06038u + 0x40u*(n))

        // Section 8.2.3.9.10 Transmit Descriptor Control
#define IXGBE_REG_TXDCTL(n) (0x06028u + 0x40u*(n))
#define IXGBE_REG_TXDCTL_PTHRESH BITS(0,6)
#define IXGBE_REG_TXDCTL_HTHRESH BITS(8,14)
#define IXGBE_REG_TXDCTL_ENABLE BIT(25)

        // Section 8.2.3.9.13 Transmit Packet Buffer Size
#define IXGBE_REG_TXPBSIZE(n) (0x0CC00u + 4u*(n))

        // Section 8.2.3.9.16 Tx Packet Buffer Threshold
#define IXGBE_REG_TXPBTHRESH(n) (0x04950u + 4u*(n))
#define IXGBE_REG_TXPBTHRESH_THRESH BITS(0,9)


        private static uint BitNSet(int n)
        {
            if (n > 31)
            {
                return 0u;
            }
            return 1u << n;

        }

        private static uint BitNSet(int start, int end)
        {
            if (end >= 31 || start < 0 || start > 31)
            {
                return 0u;
            }
            return (uint.MaxValue << (end + 1)) ^ (uint.MaxValue << start);

        }
    }
}
