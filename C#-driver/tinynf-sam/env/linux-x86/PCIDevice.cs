using System;
using System.Runtime.InteropServices;
using Utilities;

namespace Env.linuxx86
{
    public class PCIDevice : IPCIDevice
    {
        private const ushort PCI_CONFIG_ADDR = 0xCF8;
        private const ushort PCI_CONFIG_DATA = 0xCFC;
        private static Logger log = new Logger(Constants.logLevel);
        private byte bus;
        private byte device;
        private byte function;
        private byte[] padding; //will be an array of size 5

        [DllImport("libc")]
        private static extern int ioperm(ulong from, ulong num, int turn_on);

        [DllImport(@"FunctionsWrapper.so")]
        private static extern void outlCustom(uint value, ushort port);

        [DllImport(@"FunctionsWrapper.so")]
        private static extern void outbCustom(byte value, ushort port);

        [DllImport(@"FunctionsWrapper.so")]
        private static extern uint inlCustom(ushort port);

        public PCIDevice()
        {

        }

        public static bool GetIoportAccess()
        {
            // Make sure we can talk to the devices
            // We access port 0x80 to wait after an outl, since it's the POST port so safe to do anything with (it's what glibc uses in the _p versions of outl/inl)
            // Also note that since reading an int32 is 4 bytes, we need to access 4 consecutive ports for PCI config/data.
            if (ioperm(0x80, 1, 1) < 0 || ioperm(PCI_CONFIG_ADDR, 4, 1) < 0 || ioperm(PCI_CONFIG_DATA, 4, 1) < 0)
            {
                log.Debug("PCIDevice: ioperms pci failed");
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
            string filename = string.Format("/sys/bus/pci/devices/0000:{0:D2}:{1:D2}.{2}/numa_node", bus, device, function);
            char[] nodeStr = Filesystem.TnFsReadline((int)nodeStrSize, filename);
            if(nodeStr == null)
            {
                log.Debug("Cannot read node string : GetDeviceNode");
                return ulong.MaxValue;
            }
            if(nodeStr[1] != '\n')
            {
                log.Debug("Long NUMA node, not supported");
                return ulong.MaxValue;
            }
            char nodeChar = nodeStr[0];
            if(nodeChar < '0' || nodeChar > '9')
            {
                log.Debug("Unknown NUMA node encoding");
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

        uint IPCIDevice.TnPciRead(byte reg)
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
                        log.Verbose(string.Format("Read PCI : from reg {0} = {1}", reg, result));
                        return result;
                    }
                }
            }
            return 0xFFFFFFFFu; // same as reading unknown reg
        }

        void IPCIDevice.TnPciWrite(byte reg, uint value)
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
                        log.Verbose(string.Format("Write PCI : to reg {0} = {1}", reg, value));
                    }
                }
            }
            
        }
    }

}
