using System;
using Env;
using Env.linuxx86;
using Utilities;

namespace tinynf_sam
{
    public class NetDevice
    {
        private UIntPtr addr;
        private IPCIDevice pciDevice;
        private static readonly Logger log = new Logger(Constants.logLevel);
        public NetDevice(UIntPtr addr, IPCIDevice pciDevice)
        {
            this.pciDevice = pciDevice ?? throw new ArgumentNullException();
            this.addr = addr;
        }
        public UIntPtr Addr { get { return addr; } }

        /// <summary>
        /// --------------------------------
        /// Section 4.2.1.6.1 Software Reset
        /// --------------------------------
        /// </summary>
        /// <returns></returns>
        public bool Reset()
        {
            // "Prior to issuing software reset, the driver needs to execute the master disable algorithm as defined in Section 5.2.5.3.2."
            // Section 5.2.5.3.2 Master Disable:
            // "The device driver disables any reception to the Rx queues as described in Section 4.6.7.1"
            for (int queue = 0; queue <= IxgbeConstants.IXGBE_RECEIVE_QUEUES_COUNT; queue++)
            {
                // Section 4.6.7.1.2 [Dynamic] Disabling [of Receive Queues]
                // "Disable the queue by clearing the RXDCTL.ENABLE bit."
                IxgbeReg.RXDCTL.Clear(addr, IxgbeRegField.RXDCTL_ENABLE, queue);

                // "The 82599 clears the RXDCTL.ENABLE bit only after all pending memory accesses to the descriptor ring are done.
                //  The driver should poll this bit before releasing the memory allocated to this queue."
                // INTERPRETATION-MISSING: There is no mention of what to do if the 82599 never clears the bit; 1s seems like a decent timeout
                if (Ixgbe.TimeoutCondition(1000 * 1000, !IxgbeReg.RXDCTL.Cleared(addr, IxgbeRegField.RXDCTL_ENABLE, queue)))
                {
                    log.Debug("RXDCTL.ENABLE did not clear, cannot disable receive");
                    return false;
                }

                // "Once the RXDCTL.ENABLE bit is cleared the driver should wait additional amount of time (~100 us) before releasing the memory allocated to this queue."
                Time.SleepMicroSec(100);
            }
            // "Then the device driver sets the PCIe Master Disable bit [in the Device Status register] when notified of a pending master disable (or D3 entry)."
            IxgbeReg.CTRL.Set(addr, IxgbeRegField.CTRL_MASTER_DISABLE);

            // "The 82599 then blocks new requests and proceeds to issue any pending requests by this function.
            //  The driver then reads the change made to the PCIe Master Disable bit and then polls the PCIe Master Enable Status bit.
            //  Once the bit is cleared, it is guaranteed that no requests are pending from this function."
            // INTERPRETATION-MISSING: The next sentence refers to "a given time"; let's say 1 second should be plenty...
            // "The driver might time out if the PCIe Master Enable Status bit is not cleared within a given time."
            if(Ixgbe.TimeoutCondition(1000*1000, !IxgbeReg.STATUS.Cleared(addr, IxgbeRegField.STATUS_PCIE_MASTER_ENABLE_STATUS)))
            {
                // "In these cases, the driver should check that the Transaction Pending bit (bit 5) in the Device Status register in the PCI config space is clear before proceeding.
                //  In such cases the driver might need to initiate two consecutive software resets with a larger delay than 1 us between the two of them."
                // INTERPRETATION-MISSING: Might? Let's say this is a must, and that we assume the software resets work...
                if(!PciReg.PCI_DEVICESTATUS.Cleared(pciDevice, PciRegField.PCI_DEVICESTATUS_TRANSACTIONPENDING))
                {
                    log.Debug("DEVICESTATUS.TRANSACTIONPENDING did not clear, cannot perform master disable");
                    return false;
                }

                // "In the above situation, the data path must be flushed before the software resets the 82599.
                //  The recommended method to flush the transmit data path is as follows:"
                // "- Inhibit data transmission by setting the HLREG0.LPBK bit and clearing the RXCTRL.RXEN bit.
                //    This configuration avoids transmission even if flow control or link down events are resumed."
                IxgbeReg.HLREG0.Set(addr, IxgbeRegField.HLREG0_LPBK);
                IxgbeReg.RXCTRL.Clear(addr, IxgbeRegField.RXCTRL_RXEN);

                // "- Set the GCR_EXT.Buffers_Clear_Func bit for 20 microseconds to flush internal buffers."
                IxgbeReg.GCREXT.Set(addr, IxgbeRegField.GCREXT_BUFFERS_CLEAR_FUNC);
                Time.SleepMicroSec(20);

                // "- Clear the HLREG0.LPBK bit and the GCR_EXT.Buffers_Clear_Func"
                IxgbeReg.HLREG0.Set(addr, IxgbeRegField.HLREG0_LPBK);
                IxgbeReg.GCREXT.Set(addr, IxgbeRegField.GCREXT_BUFFERS_CLEAR_FUNC);

                // "- It is now safe to issue a software reset."
                // see just below for an explanation of this line
                IxgbeReg.CTRL.Set(addr, IxgbeRegField.CTRL_RST);
                Time.SleepMicroSec(2);
            }

            // happy path, back to Section 4.2.1.6.1:
            // "Software reset is done by writing to the Device Reset bit of the Device Control register (CTRL.RST)."
            IxgbeReg.CTRL.Set(addr, IxgbeRegField.CTRL_RST);

            // Section 8.2.3.1.1 Device Control Register
            // "To ensure that a global device reset has fully completed and that the 82599 responds to subsequent accesses,
            //  programmers must wait approximately 1 ms after setting before attempting to check if the bit has cleared or to access (read or write) any other device register."
            Time.SleepMicroSec(1000);
            return true;
        }
    }
}
