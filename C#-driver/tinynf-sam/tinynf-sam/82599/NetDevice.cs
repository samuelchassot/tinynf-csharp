using System;
using Env;

namespace tinynf_sam
{
    public class NetDevice
    {
        private UIntPtr addr;

        private IPCIDevice pciDevice;

        public NetDevice(UIntPtr addr, IPCIDevice pciDevice)
        {
            this.pciDevice = pciDevice ?? throw new ArgumentNullException();
            this.addr = addr;
        }
        public UIntPtr Addr { get { return addr; } }
        public bool Reset()
        {
            // "Prior to issuing software reset, the driver needs to execute the master disable algorithm as defined in Section 5.2.5.3.2."
            // Section 5.2.5.3.2 Master Disable:
            // "The device driver disables any reception to the Rx queues as described in Section 4.6.7.1"
        }
    }
}
