using System;
using System.Runtime.InteropServices;
using Utilities;

namespace Env.linuxx86
{
    public class PCIDevice : IPCIDevice
    {
        private const ulong PCI_CONFIG_ADDR = 0xCF8;
        private const ulong PCI_CONFIG_DATA = 0xCFC;
        private static Logger log = new Logger(Constants.logLevel);
        private byte bus;
        private byte device;
        private byte function;
        private byte[] padding; //will be an array of size 5

        [DllImport("libc")]
        private static extern int ioperm(ulong from, ulong num, int turn_on);

        public PCIDevice()
        {

        }

        private static bool getIoportAccess()
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

        uint IPCIDevice.TnPciRead(byte reg)
        {
            throw new NotImplementedException();
        }

        void IPCIDevice.TnPciWrite(byte reg, uint value)
        {
            throw new NotImplementedException();
        }
    }

}
