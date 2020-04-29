using System;
using System.Runtime.InteropServices;
using Utilities;

namespace Env.linuxx86
{
    public class PCIDevice
    {
        private const ushort PCI_CONFIG_ADDR = 0xCF8;
        private const ushort PCI_CONFIG_DATA = 0xCFC;
        private byte bus;
        private byte device;
        private byte function;

        public byte Bus { get => bus; }
        public byte Device { get => device; }
        public byte Function { get => function; }

        [DllImport("libc")]
        private static extern int ioperm(ulong from, ulong num, int turn_on);

        [DllImport(@"FunctionsWrapper.so")]
        private static extern void outlCustom(uint value, ushort port);

        [DllImport(@"FunctionsWrapper.so")]
        private static extern void outbCustom(byte value, ushort port);

        [DllImport(@"FunctionsWrapper.so")]
        private static extern uint inlCustom(ushort port);
        
        public PCIDevice(byte bus, byte device, byte function)
        {
            this.bus = bus;
            this.device = device;
            this.function = function;
        }

        public static bool GetIoportAccess()
        {
            // Make sure we can talk to the devices
            // We access port 0x80 to wait after an outl, since it's the POST port so safe to do anything with (it's what glibc uses in the _p versions of outl/inl)
            // Also note that since reading an int32 is 4 bytes, we need to access 4 consecutive ports for PCI config/data.

            if (ioperm(0x80, 1, 1) < 0 || ioperm(PCI_CONFIG_ADDR, 4, 1) < 0 || ioperm(PCI_CONFIG_DATA, 4, 1) < 0)
            {
                //DEBUG -- BEGIN
                int i1 = ioperm(0x80, 1, 1);
                int i2 = ioperm(PCI_CONFIG_ADDR, 4, 1);
                int i3 = ioperm(PCI_CONFIG_DATA, 4, 1);
                Util.log.Debug("ioperm(0x80, 1, 1) = " + i1);
                Util.log.Debug("ioperm(PCI_CONFIG_ADDR, 4, 1) = " + i2);
                Util.log.Debug("ioperm(PCI_CONFIG_DATA, 4, 1) = " + i3);


                //DEBUG -- END
                Util.log.Debug("PCIDevice: PCI device is not what was expected ioperms pci failed");
                return false;
            }
            return true;
        }

        /// <summary>
        /// get the device node, return it as a ulong. Returns ulong.MaxValue if a problem occured
        /// </summary>
        /// <returns></returns>
        public ulong GetDeviceNode()
        {
            ulong nodeStrSize = 3ul;
            string filename = string.Format("/sys/bus/pci/devices/0000:{0:X2}:{1:D2}.{2}/numa_node", bus, device, function);
            char[] nodeStr = Filesystem.TnFsReadline((int)nodeStrSize, filename);
            if(nodeStr == null)
            {
                Util.log.Debug("Cannot read node string : GetDeviceNode");
                return ulong.MaxValue;
            }
            if(nodeStr[1] != '\n')
            {
                Util.log.Debug("Long NUMA node, not supported");
                return ulong.MaxValue;
            }
            char nodeChar = nodeStr[0];
            if(nodeChar < '0' || nodeChar > '9')
            {
                Util.log.Debug("Unknown NUMA node encoding");
                return ulong.MaxValue;
            }
            return (ulong) (nodeChar - '0');
        }

        public uint GetPciRegAddr(byte reg)
        {
            return 0x80000000u | ((uint)bus << 16) | ((uint)device << 11) | ((uint)function << 8) | reg;
        }

        public void PciAddress(byte reg)
        {
            uint addr = GetPciRegAddr(reg);
            outlCustom(addr, PCI_CONFIG_ADDR);

            outbCustom(0, 0x80);

        }

        public uint PciRead(byte reg)
        {
            if (GetIoportAccess())
            {       
                ulong deviceNode = GetDeviceNode();
                if (deviceNode != ulong.MaxValue)
                {       
                    if (Numa.TnNumaIsCurrentNode(deviceNode))
                    {
                        PciAddress(reg);
                        uint result = inlCustom(PCI_CONFIG_DATA);
                        Util.log.Verbose(string.Format("Read PCI : from reg {0} -> {1}", reg, result));
                        return result;
                    }
                }
            }
            return 0xFFFFFFFFu; // same as reading unknown reg
        }

        public void PciWrite(byte reg, uint value)
        {
            if (GetIoportAccess())
            {
                ulong deviceNode = GetDeviceNode();
                if (deviceNode != ulong.MaxValue)
                {
                    if (Numa.TnNumaIsCurrentNode(deviceNode))
                    {
                        PciAddress(reg);
                        outlCustom(value, PCI_CONFIG_DATA);
                        Util.log.Verbose(string.Format("Write PCI : to reg {0} := {1}", reg, value));
                    }
                }
            }
            
        }
    }

}
