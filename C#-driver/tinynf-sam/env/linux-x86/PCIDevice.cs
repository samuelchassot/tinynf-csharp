using System;
using System.Runtime.InteropServices;

namespace Env.linuxx86
{
    public class PCIDevice : IPCIDevice
    {
        private byte bus;
        private byte device;
        private byte function;
        private byte[] padding; //will be an array of size 5

        [DllImport("libc")]
        private static extern int ioperm(ulong from, ulong num, int turn_on);

        public PCIDevice()
        {

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
