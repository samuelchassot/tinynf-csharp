using System;
using Env;
using Env.linuxx86;

namespace tinynf_sam
{
    public enum PciReg
    {
        // Section 9.3.2 PCIe Configuration Space Summary: "0x10 Base Address Register 0" (32 bit), "0x14 Base Address Register 1" (32 bit)
        PCI_BAR0_LOW,
        PCI_BAR0_HIGH,

        // Section 9.3.3.3 Command Register (16 bit)
        // Section 9.3.3.4 Status Register (16 bit, unused)
        PCI_COMMAND,

        // Section 9.3.10.6 Device Status Register (16 bit)
        // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
        PCI_DEVICESTATUS,

        // Section 9.3.3.1 Vendor ID Register (16 bit)
        // Section 9.3.3.2 Device ID Register (16 bit)
        PCI_ID,

        // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
        // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
        // Section 9.3.7.1.6 Data Register (8 bit, unused)
        PCI_PMCSR,
    }

    public enum PciRegField
    {
        // Section 9.3.3.3 Command Register (16 bit)
        // Section 9.3.3.4 Status Register (16 bit, unused)
        PCI_COMMAND_MEMORY_ACCESS_ENABLE,
        PCI_COMMAND_BUS_MASTER_ENABLE,
        PCI_COMMAND_INTERRUPT_DISABLE,

        // Section 9.3.10.6 Device Status Register (16 bit)
        // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
        PCI_DEVICESTATUS_TRANSACTIONPENDING,

        // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
        // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
        // Section 9.3.7.1.6 Data Register (8 bit, unused)
        PCI_PMCSR_POWER_STATE,

        // Used for Read/Write methods
        NONE
    }

    public static class PciRegExtension
    {
        public static byte GetAddr(this PciReg reg)
        {
            return reg switch
            {
                // Section 9.3.2 PCIe Configuration Space Summary: "0x10 Base Address Register 0" (32 bit), "0x14 Base Address Register 1" (32 bit)
                PciReg.PCI_BAR0_LOW => (byte)0x10u,
                PciReg.PCI_BAR0_HIGH => (byte)0x14u,
                // Section 9.3.3.3 Command Register (16 bit)
                // Section 9.3.3.4 Status Register (16 bit, unused)
                PciReg.PCI_COMMAND => (byte)0x04u,
                // Section 9.3.10.6 Device Status Register (16 bit)
                // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
                PciReg.PCI_DEVICESTATUS => (byte)0xAAu,
                // Section 9.3.3.1 Vendor ID Register (16 bit)
                // Section 9.3.3.2 Device ID Register (16 bit)
                PciReg.PCI_ID => (byte)0x00u,
                // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
                // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
                // Section 9.3.7.1.6 Data Register (8 bit, unused)
                PciReg.PCI_PMCSR => (byte)0x44u,

                _ => byte.MaxValue
            };
        }

        public static uint GetValue(this PciRegField field)
        {
            return field switch
            {
                // Section 9.3.3.3 Command Register (16 bit)
                // Section 9.3.3.4 Status Register (16 bit, unused)
                PciRegField.PCI_COMMAND_MEMORY_ACCESS_ENABLE => (ushort)IxgbeConstants.BitNSet(1),
                PciRegField.PCI_COMMAND_BUS_MASTER_ENABLE => (ushort)IxgbeConstants.BitNSet(2),
                PciRegField.PCI_COMMAND_INTERRUPT_DISABLE => (ushort)IxgbeConstants.BitNSet(10),
                // Section 9.3.10.6 Device Status Register (16 bit)
                // Section 9.3.10.7 Link Capabilities Register (16 bit, unused)
                PciRegField.PCI_DEVICESTATUS_TRANSACTIONPENDING => (ushort)IxgbeConstants.BitNSet(5),
                // Section 9.3.7.1.4 Power Management Control / Status Register (16 bit)
                // Section 9.3.7.1.5 PMCSR_BSE Bridge Support Extensions Register (8 bit, hardwired to 0)
                // Section 9.3.7.1.6 Data Register (8 bit, unused)
                PciRegField.PCI_PMCSR_POWER_STATE => IxgbeConstants.BitNSet(0, 1),

                _ => uint.MaxValue
            };
        }

        public static uint Read(this PciReg reg, PCIDevice pciDevice)
        {
            PCIDevice.log.Debug(string.Format("read pci reg at addr : {0:X}", reg.GetAddr()));
            return pciDevice.PciRead(reg.GetAddr());
        }

        public static void Set(this PciReg reg, PCIDevice pciDevice, PciRegField field)
        {
            pciDevice.PciWrite(reg.GetAddr(), reg.Read(pciDevice) | field.GetValue());
        }

        public static bool Cleared(this PciReg reg, PCIDevice pciDevice, PciRegField field)
        {
            return (reg.Read(pciDevice) & field.GetValue()) == 0u;
        }
    }
}
