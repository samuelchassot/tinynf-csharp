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
            return false;
        }
    }
}
