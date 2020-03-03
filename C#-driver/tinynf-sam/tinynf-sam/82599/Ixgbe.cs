using System;
using Env.linuxx86;
using Utilities;

namespace tinynf_sam
{
    public class Ixgbe
    {


        private NetDevice device;
        private readonly Logger log = new Logger(Constants.logLevel);
        public Ixgbe()
        {
        }

        public unsafe uint ReadReg(uint reg)
        {
            uint valLe = *((uint*)((ulong)device.Addr + (ulong)reg));
            var val = Endian.LeToCpu(valLe);
            log.Verbose(string.Format("Read value {0} from reg {1} at addr {2}", val, reg, device.Addr));
            return val;
        }
        public unsafe void WriteRegRaw(UIntPtr regAddr, uint value)
        {
            log.Verbose(string.Format("Write raw value {0} to regAddr", value, regAddr));
            *((uint*)regAddr) = Endian.CpuToLe(value);
        }
        public unsafe void WriteReg(uint reg, uint value)
        {
            log.Verbose(string.Format("Write value {0} to reg {1} at addr {2}", value, reg, device.Addr));
            WriteRegRaw((UIntPtr)((ulong)device.Addr + reg), value);
        }
    }
}
