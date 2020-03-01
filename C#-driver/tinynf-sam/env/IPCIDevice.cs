using System;
namespace Env
{
    public interface IPCIDevice
    {
        /// <summary>
        /// read the given register of the current pci device and return its value
        /// </summary>
        /// <param name="reg">the register to read</param>
        /// <returns>the value contained in the register</returns>
        public uint TnPciRead(byte reg);

        /// <summary>
        /// write the given value to the given register in the current pci device
        /// </summary>
        /// <param name="reg">the register to write to</param>
        /// <param name="value">the value to write</param>
        public void TnPciWrite(byte reg, uint value);
    }
}
