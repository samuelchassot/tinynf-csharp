using System;
namespace Env.linuxx86
{
    public class PCIDevice : IPCIDevice
    {
        private byte bus;
        private byte device;
        private byte function;
        private byte[] padding; //will be an array of size 5


        public PCIDevice()
        {

        }
        uint IPCIDevice.tn_pci_read(byte reg)
        {
            throw new NotImplementedException();
        }

        void IPCIDevice.tn_pci_write(byte reg, uint value)
        {
            throw new NotImplementedException();
        }
    }

}
