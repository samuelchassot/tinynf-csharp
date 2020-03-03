using System;
namespace tinynf_sam
{
    public enum IxgbeReg
    {
        // Section 8.2.3.1.1 Device Control Register
        CTRL,
        CTRL_MASTER_DISABLE,
        CTRL_RST,

        // Section 8.2.3.1.3 Extended Device Control Register
        CTRLEXT,
        CTRLEXT_NSDIS,

        // Section 8.2.3.11.1 Rx DCA Control Register
        DCARXCTRL,

        // This bit is reserved, has no name, but must be used anyway
        DCARXCTRL_UNKNOWN,

        // Section 8.2.3.11.2 Tx DCA Control Registers
        DCATXCTRL,
        DCATXCTRL_TX_DESC_WB_RO_EN,

        // Section 8.2.3.9.2 DMA Tx Control
        DMATXCTL,
        DMATXCTL_TE,

        // Section 8.2.3.9.1 DMA Tx TCP Max Allow Size Requests
        DTXMXSZRQ,
        DTXMXSZRQ_MAX_BYTES_NUM_REQ,

        // Section 8.2.3.2.1 EEPROM/Flash Control Register
        EEC,
        EEC_EE_PRES,
        EEC_AUTO_RD,

        // Section 8.2.3.5.9 Extended Interrupt Mask Clear Registers
        EIMC,
        EIMC_MASK,

        // Section 8.2.3.3.4 Flow Control Receive Threshold High
        FCRTH,
        FCRTH_RTH,

        // Section 8.2.3.7.1 Filter Control Register (FCTRL)
        FCTRL,
        FCTRL_MPE,
        FCTRL_UPE,

        // Section 8.2.3.7.19 Five tuple Queue Filter
        FTQF,
        FTQF_QUEUE_ENABLE,

        // Section 8.2.3.4.10 Firmware Semaphore Register
        FWSM,
        FWSM_EXT_ERR_IND,

        // Section 8.2.3.4.12 PCIe Control Extended Register
        GCREXT,
        GCREXT_BUFFERS_CLEAR_FUNC,

        // Section 8.2.3.22.8 MAC Core Control 0 Register
        HLREG0,
        HLREG0_LPBK,

        // Section 8.2.3.22.34 MAC Flow Control Register
        MFLCN,
        MFLCN_RFCE,

        // Section 8.2.3.7.10 MAC Pool Select Array
        MPSAR,

        // Section 8.2.3.7.7 Multicast Table Array
        MTA,

        // Section 8.2.3.27.17 PF Unicast Table Array
        PFUTA,

        // Section 8.2.3.27.15 PF VM VLAN Pool Filter
        PFVLVF,

        // Section 8.2.3.27.16 PF VM VLAN Pool Filter Bitmap
        PFVLVFB,

        // Section 8.2.3.8.2 Receive Descriptor Base Address High
        RDBAH,

        // Section 8.2.3.8.1 Receive Descriptor Base Address Low
        RDBAL,

        // Section 8.2.3.8.3 Receive Descriptor Length
        RDLEN,

        // Section 8.2.3.8.8 Receive DMA Control Register
        // INTERPRETATION-MISSING: Bit 0, which is not mentioned in the table, is reserved
        RDRXCTL,
        RDRXCTL_CRC_STRIP,
        RDRXCTL_DMAIDONE,
        RDRXCTL_RSCFRSTSIZE,
        RDRXCTL_RSCACKC,
        RDRXCTL_FCOE_WRFIX,

        // Section 8.2.3.8.5 Receive Descriptor Tail
        RDT,

        // Section 8.2.3.10.2 DCB Transmit Descriptor Plane Control and Status
        RTTDCS,
        RTTDCS_ARBDIS,

        // Section 8.2.3.8.10 Receive Control Register
        RXCTRL,
        RXCTRL_RXEN,

        // Section 8.2.3.8.6 Receive Descriptor Control
        RXDCTL,
        RXDCTL_ENABLE,

        // Section 8.2.3.8.9 Receive Packet Buffer Size
        RXPBSIZE,

        // Section 8.2.3.12.5 Security Rx Control
        SECRXCTRL,
        SECRXCTRL_RX_DIS,

        // Section 8.2.3.12.6 Security Rx Status
        SECRXSTAT,
        SECRXSTAT_SECRX_RDY,

        // Section 8.2.3.8.7 Split Receive Control Registers
        SRRCTL,
        SRRCTL_BSIZEPACKET,
        SRRCTL_DROP_EN,

        // Section 8.2.3.1.2 Device Status Register
        STATUS,
        STATUS_PCIE_MASTER_ENABLE_STATUS,

        // Section 8.2.3.9.6 Transmit Descriptor Base Address High
        TDBAH,

        // Section 8.2.3.9.5 Transmit Descriptor Base Address Low
        TDBAL,

        // Section 8.2.3.9.7 Transmit Descriptor Length
        TDLEN,

        // Section 8.2.3.9.9 Transmit Descriptor Tail
        TDT,

        // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address High
        TDWBAH,

        // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address Low
        TDWBAL,

        // Section 8.2.3.9.10 Transmit Descriptor Control
        TXDCTL,
        TXDCTL_PTHRESH,
        TXDCTL_HTHRESH,
        TXDCTL_ENABLE,

        // Section 8.2.3.9.13 Transmit Packet Buffer Size
        TXPBSIZE,

        // Section 8.2.3.9.16 Tx Packet Buffer Threshold
        TXPBTHRESH,
        TXPBTHRESH_THRESH,

        //-------------------
        // PCI Registers
        //-------------------

        // Section 9.3.2 PCIe Configuration Space Summary: "0x10 Base Address Register 0" (32 bit), "0x14 Base Address Register 1" (32 bit)
        PCI_BAR0_LOW,
        PCI_BAR0_HIGH,

        // Section 9.3.3.3 Command Register (16 bit)
        // Section 9.3.3.4 Status Register (16 bit, unused)
        PCI_COMMAND,
        PCI_COMMAND_MEMORY_ACCESS_ENABLE,
        PCI_COMMAND_BUS_MASTER_ENABLE,
        PCI_COMMAND_INTERRUPT_DISABLE,

        // Section 9.3.10.6 Device Status Register (16 bit)
        // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
        PCI_DEVICESTATUS,
        PCI_DEVICESTATUS_TRANSACTIONPENDING,

        // Section 9.3.3.1 Vendor ID Register (16 bit)
        // Section 9.3.3.2 Device ID Register (16 bit)
        PCI_ID,

        // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
        // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
        // Section 9.3.7.1.6 Data Register (8 bit, unused)
        PCI_PMCSR,
        PCI_PMCSR_POWER_STATE
    }

    public static class IxgbeRegExtension
    {
        public static uint GetValue(this IxgbeReg reg, int n = 1)
        {
            return reg switch
            {
                // Section 8.2.3.1.1 Device Control Register
                IxgbeReg.CTRL => 0x00000u,
                IxgbeReg.CTRL_MASTER_DISABLE => IxgbeConstants.BitNSet(2),
                IxgbeReg.CTRL_RST => IxgbeConstants.BitNSet(26),
                // Section 8.2.3.1.3 Extended Device Control Register
                IxgbeReg.CTRLEXT => 0x00018u,
                IxgbeReg.CTRLEXT_NSDIS => IxgbeConstants.BitNSet(16),
                // Section 8.2.3.11.1 Rx DCA Control Register
                IxgbeReg.DCARXCTRL => n <= 63u ? (uint)(0x0100Cu + (0x40u * n)) : (uint)(0x0D00Cu + (0x40u * (n - 64u))),
                // This bit is reserved, has no name, but must be used anyway
                IxgbeReg.DCARXCTRL_UNKNOWN => IxgbeConstants.BitNSet(12),
                // Section 8.2.3.11.2 Tx DCA Control Registers
                IxgbeReg.DCATXCTRL => (uint)(0x0600Cu + 0x40u * n),
                IxgbeReg.DCATXCTRL_TX_DESC_WB_RO_EN => IxgbeConstants.BitNSet(11),
                // Section 8.2.3.9.2 DMA Tx Control
                IxgbeReg.DMATXCTL => 0x04A80u,
                IxgbeReg.DMATXCTL_TE => IxgbeConstants.BitNSet(0),
                // Section 8.2.3.9.1 DMA Tx TCP Max Allow Size Requests
                IxgbeReg.DTXMXSZRQ => 0x08100u,
                IxgbeReg.DTXMXSZRQ_MAX_BYTES_NUM_REQ => IxgbeConstants.BitNSet(0, 11),
                // Section 8.2.3.2.1 EEPROM/Flash Control Register
                IxgbeReg.EEC => 0x10010u,
                IxgbeReg.EEC_EE_PRES => IxgbeConstants.BitNSet(8),
                IxgbeReg.EEC_AUTO_RD => IxgbeConstants.BitNSet(9),
                // Section 8.2.3.5.9 Extended Interrupt Mask Clear Registers
                IxgbeReg.EIMC => (uint)(0x00AB0u + (4u * n)),
                IxgbeReg.EIMC_MASK => IxgbeConstants.BitNSet(0, 31),
                // Section 8.2.3.3.4 Flow Control Receive Threshold High
                IxgbeReg.FCRTH => (uint)(0x03260u + (4u * n)),
                IxgbeReg.FCRTH_RTH => IxgbeConstants.BitNSet(5, 18),
                // Section 8.2.3.7.1 Filter Control Register (FCTRL)
                IxgbeReg.FCTRL => 0x05080u,
                IxgbeReg.FCTRL_MPE => IxgbeConstants.BitNSet(8),
                IxgbeReg.FCTRL_UPE => IxgbeConstants.BitNSet(9),
                // Section 8.2.3.7.19 Five tuple Queue Filter
                IxgbeReg.FTQF => (uint)(0x0E600u + (4u * n)),
                IxgbeReg.FTQF_QUEUE_ENABLE => IxgbeConstants.BitNSet(31),
                // Section 8.2.3.4.10 Firmware Semaphore Register
                IxgbeReg.FWSM => 0x10148u,
                IxgbeReg.FWSM_EXT_ERR_IND => IxgbeConstants.BitNSet(19, 24),
                // Section 8.2.3.4.12 PCIe Control Extended Register
                IxgbeReg.GCREXT => 0x11050u,
                IxgbeReg.GCREXT_BUFFERS_CLEAR_FUNC => IxgbeConstants.BitNSet(30),
                // Section 8.2.3.22.8 MAC Core Control 0 Register
                IxgbeReg.HLREG0 => 0x04240u,
                IxgbeReg.HLREG0_LPBK => IxgbeConstants.BitNSet(15),
                // Section 8.2.3.22.34 MAC Flow Control Register
                IxgbeReg.MFLCN => 0x04294u,
                IxgbeReg.MFLCN_RFCE => IxgbeConstants.BitNSet(3),
                // Section 8.2.3.7.10 MAC Pool Select Array
                IxgbeReg.MPSAR => (uint)(0x0A600u + (4u * n)),
                // Section 8.2.3.7.7 Multicast Table Array
                IxgbeReg.MTA => (uint)(0x05200u + (4u * n)),
                // Section 8.2.3.27.17 PF Unicast Table Array
                IxgbeReg.PFUTA => (uint)(0x0F400u + (4u * n)),
                // Section 8.2.3.27.15 PF VM VLAN Pool Filter
                IxgbeReg.PFVLVF => (uint)(0x0F100u + (4u * n)),
                // Section 8.2.3.27.16 PF VM VLAN Pool Filter Bitmap
                IxgbeReg.PFVLVFB => (uint)(0x0F200u + (4u * n)),
                // Section 8.2.3.8.2 Receive Descriptor Base Address High
                IxgbeReg.RDBAH => (uint)(n <= 63u ? (0x01004u + (0x40u * n)) : (0x0D004u + (0x40u * (n - 64u)))),
                // Section 8.2.3.8.1 Receive Descriptor Base Address Low
                IxgbeReg.RDBAL => (uint)(n <= 63u ? (0x01000u + (0x40u * n)) : (0x0D000u + 0x40u * (n - 64u))),
                // Section 8.2.3.8.3 Receive Descriptor Length
                IxgbeReg.RDLEN => (uint)(n <= 63u ? (0x01008u + (0x40u * n)) : (0x0D008 + (0x40u * (n - 64u)))),
                // Section 8.2.3.8.8 Receive DMA Control Register
                // INTERPRETATION-MISSING: Bit 0, which is not mentioned in the table, is reserved
                IxgbeReg.RDRXCTL => 0x02F00u,
                IxgbeReg.RDRXCTL_CRC_STRIP => IxgbeConstants.BitNSet(1),
                IxgbeReg.RDRXCTL_DMAIDONE => IxgbeConstants.BitNSet(3),
                IxgbeReg.RDRXCTL_RSCFRSTSIZE => IxgbeConstants.BitNSet(17, 24),
                IxgbeReg.RDRXCTL_RSCACKC => IxgbeConstants.BitNSet(25),
                IxgbeReg.RDRXCTL_FCOE_WRFIX => IxgbeConstants.BitNSet(26),
                // Section 8.2.3.8.5 Receive Descriptor Tail
                IxgbeReg.RDT => (uint)(n <= 63u ? (0x01018u + (0x40u * n)) : (0x0D018u + 0x40u * (n - 64u))),
                // Section 8.2.3.10.2 DCB Transmit Descriptor Plane Control and Status
                IxgbeReg.RTTDCS => 0x04900u,
                IxgbeReg.RTTDCS_ARBDIS => IxgbeConstants.BitNSet(6),
                // Section 8.2.3.8.10 Receive Control Register
                IxgbeReg.RXCTRL => 0x03000u,
                IxgbeReg.RXCTRL_RXEN => IxgbeConstants.BitNSet(0),
                // Section 8.2.3.8.6 Receive Descriptor Control
                IxgbeReg.RXDCTL => (uint)(n <= 63u ? (0x01028u + 0x40u * n) : (0x0D028u + 0x40u * (n - 64u))),
                IxgbeReg.RXDCTL_ENABLE => IxgbeConstants.BitNSet(25),
                // Section 8.2.3.8.9 Receive Packet Buffer Size
                IxgbeReg.RXPBSIZE => (uint)(0x03C00u + (4u * n)),
                // Section 8.2.3.12.5 Security Rx Control
                IxgbeReg.SECRXCTRL => 0x08D00u,
                IxgbeReg.SECRXCTRL_RX_DIS => IxgbeConstants.BitNSet(1),
                // Section 8.2.3.12.6 Security Rx Status
                IxgbeReg.SECRXSTAT => 0x08D04u,
                IxgbeReg.SECRXSTAT_SECRX_RDY => IxgbeConstants.BitNSet(0),
                // Section 8.2.3.8.7 Split Receive Control Registers
                IxgbeReg.SRRCTL => (uint)(n <= 63u ? (0x01014u + (0x40u * n)) : (0x0D014u + (0x40u * (n - 64u)))),
                IxgbeReg.SRRCTL_BSIZEPACKET => IxgbeConstants.BitNSet(0, 4),
                IxgbeReg.SRRCTL_DROP_EN => IxgbeConstants.BitNSet(28),
                // Section 8.2.3.1.2 Device Status Register
                IxgbeReg.STATUS => 0x00008u,
                IxgbeReg.STATUS_PCIE_MASTER_ENABLE_STATUS => IxgbeConstants.BitNSet(19),
                // Section 8.2.3.9.6 Transmit Descriptor Base Address High
                IxgbeReg.TDBAH => (uint)(0x06004u + (0x40u * n)),
                // Section 8.2.3.9.5 Transmit Descriptor Base Address Low
                IxgbeReg.TDBAL => (uint)(0x06000u + (0x40u * n)),
                // Section 8.2.3.9.7 Transmit Descriptor Length
                IxgbeReg.TDLEN => (uint)(0x06008u + (0x40u * n)),
                // Section 8.2.3.9.9 Transmit Descriptor Tail
                IxgbeReg.TDT => (uint)(0x06018u + (0x40u * n)),
                // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address High
                IxgbeReg.TDWBAH => (uint)(0x0603Cu + (0x40u * n)),
                // Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address Low
                IxgbeReg.TDWBAL => (uint)(0x06038u + (0x40u * n)),
                // Section 8.2.3.9.10 Transmit Descriptor Control
                IxgbeReg.TXDCTL => (uint)(0x06028u + (0x40u * n)),
                IxgbeReg.TXDCTL_PTHRESH => IxgbeConstants.BitNSet(0, 6),
                IxgbeReg.TXDCTL_HTHRESH => IxgbeConstants.BitNSet(8, 14),
                IxgbeReg.TXDCTL_ENABLE => IxgbeConstants.BitNSet(25),
                // Section 8.2.3.9.13 Transmit Packet Buffer Size
                IxgbeReg.TXPBSIZE => (uint)(0x0CC00u + (4u * n)),
                // Section 8.2.3.9.16 Tx Packet Buffer Threshold
                IxgbeReg.TXPBTHRESH => (uint)(0x04950u + (4u * n)),
                IxgbeReg.TXPBTHRESH_THRESH => IxgbeConstants.BitNSet(0, 9),


                //-------------------
                // PCI Registers
                //-------------------


                // Section 9.3.2 PCIe Configuration Space Summary: "0x10 Base Address Register 0" (32 bit), "0x14 Base Address Register 1" (32 bit)
                IxgbeReg.PCI_BAR0_LOW => 0x10u,
                IxgbeReg.PCI_BAR0_HIGH => 0x14u,
                // Section 9.3.3.3 Command Register (16 bit)
                // Section 9.3.3.4 Status Register (16 bit, unused)
                IxgbeReg.PCI_COMMAND => (ushort)0x04u,
                IxgbeReg.PCI_COMMAND_MEMORY_ACCESS_ENABLE => (ushort)IxgbeConstants.BitNSet(1),
                IxgbeReg.PCI_COMMAND_BUS_MASTER_ENABLE => (ushort)IxgbeConstants.BitNSet(2),
                IxgbeReg.PCI_COMMAND_INTERRUPT_DISABLE => (ushort)IxgbeConstants.BitNSet(10),
                // Section 9.3.10.6 Device Status Register (16 bit)
                // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
                IxgbeReg.PCI_DEVICESTATUS => (ushort)0xAAu,
                IxgbeReg.PCI_DEVICESTATUS_TRANSACTIONPENDING => (ushort)IxgbeConstants.BitNSet(5),
                // Section 9.3.3.1 Vendor ID Register (16 bit)
                // Section 9.3.3.2 Device ID Register (16 bit)
                IxgbeReg.PCI_ID => (ushort)0x00u,
                // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
                // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
                // Section 9.3.7.1.6 Data Register (8 bit, unused)
                IxgbeReg.PCI_PMCSR => (ushort)0x44u,
                IxgbeReg.PCI_PMCSR_POWER_STATE => IxgbeConstants.BitNSet(0, 1),

                _ => uint.MaxValue,
            };
        }
    }
}
