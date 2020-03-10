using System;
using Env.linuxx86;
using Utilities;

namespace tinynf_sam
{
    public unsafe class NetAgent
    {
        private const uint RING_ADDRESS_should_be_128_byte_aligned = IxgbeConstants.IXGBE_RING_SIZE * 16 >= 128 ? 0 : -1;


        private UIntPtr buffer;
        private UIntPtr receiveTailAddr;
        private ulong processed_delimiter;
        private ulong outputs_count;
        private ulong flushedProcessedDelimiter; // -1 if there was no packet last time, otherwise last flushed processed_delimiter
        private byte[] padding;
        // transmit heads must be 16-byte aligned; see alignment remarks in transmit queue setup
        // (there is also a runtime check to make sure the array itself is aligned properly)
        // plus, we want each head on its own cache line to avoid conflicts
        // thus, using assumption CACHE, we multiply indices by 16
        public const uint TRANSMIT_HEAD_MULTIPLIER = 16;

        private uint[] transmitHeads; // size = IXGBE_AGENT_OUTPUTS_MAX * TRANSMIT_HEAD_MULTIPLIER
        private volatile UIntPtr[] rings; // 0 == shared receive/transmit, rest are exclusive transmit, size = IXGBE_AGENT_OUTPUTS_MAX
        private UIntPtr[] transmitTailAddrs;

        private readonly static Logger log = new Logger(Constants.logLevel);

        /// <summary>
        /// Create a new NetAgent and return it. Because it needs to allocate memory and do other things
        /// that can fail, it throws exceptions
        /// </summary>
        /// <returns></returns>
        public NetAgent(Memory mem)
        {
            receiveTailAddr = UIntPtr.Zero;
            padding = new byte[3 * 8];
            transmitHeads = new uint[(int)IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX * (int)TRANSMIT_HEAD_MULTIPLIER];
            transmitTailAddrs = new UIntPtr[(int)IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX];
            rings = new UIntPtr[IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX];

            UIntPtr buffPtr = mem.TnMemAllocate(IxgbeConstants.IXGBE_RING_SIZE * IxgbeConstants.IXGBE_PACKET_BUFFER_SIZE);
            if (buffPtr == UIntPtr.Zero)
            {
                log.Debug("Cannot allocate memory for the buffer in net agent init");
                throw new MemoryAllocationErrorException();
            }
            this.buffer = buffPtr;

            for (ulong n = 0; n < IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX; n++)
            {
                UIntPtr ringAddr = mem.TnMemAllocate(IxgbeConstants.IXGBE_RING_SIZE * 16);
                if (ringAddr == UIntPtr.Zero)
                {
                    log.Debug("Cannot allocate memory for the ring number " + n + " for InitNetAgent");
                    for (ulong m = 0; m < n; m++)
                    {
                        mem.TnMemFree(this.rings[m]);
                    }
                    mem.TnMemFree(buffPtr);
                    throw new MemoryAllocationErrorException();
                }
                this.rings[n] = ringAddr;

                // initialize to uintptr.zero
                this.transmitTailAddrs[n] = UIntPtr.Zero;//(UIntPtr)(&padding[0]);
            }

            // Start in "no packet" state
            flushedProcessedDelimiter = ulong.MaxValue; //which is equal to (ulong)-1
        }


        public UIntPtr Buffer { get => buffer; private set => buffer = value; }
        public UIntPtr Receive_tail_addr { get => receiveTailAddr; private set => receiveTailAddr = value; }
        public ulong Processed_delimiter { get => processed_delimiter; private set => processed_delimiter = value; }
        public ulong Outputs_count { get => outputs_count; private set => outputs_count = value; }
        public ulong Flushed_processed_delimiter { get => flushedProcessedDelimiter; private set => flushedProcessedDelimiter = value; }
        public byte[] Padding { get => padding; private set => padding = value; }

        public bool SetInput(Memory mem, NetDevice device)
        {
            if (this.receiveTailAddr != UIntPtr.Zero)
            {
                log.Debug("NetAgent receive was already set");
                return false;
            }
            // The 82599 has more than one receive queue, but we only need queue 0
            ushort queueIndex = 0;

            // See later for details of RXDCTL.ENABLE
            if (IxgbeReg.RXDCTL.Cleared(device.Addr, IxgbeRegField.RXDCTL_ENABLE, queueIndex))
            {
                log.Debug("Receive queue is already in use");
                return false;
            }

            // "The following should be done per each receive queue:"
            // "- Allocate a region of memory for the receive descriptor list."
            // This is already done in agent initialization as agent->rings[0]
            // "- Receive buffers of appropriate size should be allocated and pointers to these buffers should be stored in the descriptor ring."
            // This will be done when setting up the first transmit ring
            // Note that only the first line (buffer address) needs to be configured, the second line is only for write-back except End Of Packet (bit 33)
            // and Descriptor Done (bit 32), which must be 0 as per table in "EOP (End of Packet) and DD (Descriptor Done)"
            // "- Program the descriptor base address with the address of the region (registers RDBAL, RDBAL)."
            // INTERPRETATION-TYPO: This is a typo, the second "RDBAL" should read "RDBAH".
            // 	Section 8.2.3.8.1 Receive Descriptor Base Address Low (RDBAL[n]):
            // 	"The receive descriptor base address must point to a 128 byte-aligned block of data."
            // This alignment is guaranteed by the agent initialization
            UIntPtr ringPhysAddr = mem.TnMemVirtToPhys(this.rings[0]);
            if (ringPhysAddr == UIntPtr.Zero)
            {
                log.Debug("Could not get phys addr of main ring");
                return false;
            }
            IxgbeReg.RDBAH.Write(device.Addr, (uint)((ulong)ringPhysAddr >> 32), idx: queueIndex);
            IxgbeReg.RDBAL.Write(device.Addr, (uint)((ulong)ringPhysAddr & 0xFFFFFFFFu), idx: queueIndex);
            // "- Set the length register to the size of the descriptor ring (register RDLEN)."
            // Section 8.2.3.8.3 Receive DEscriptor Length (RDLEN[n]):
            // "This register sets the number of bytes allocated for descriptors in the circular descriptor buffer."
            // Note that receive descriptors are 16 bytes.
            IxgbeReg.RDLEN.Write(device.Addr, IxgbeConstants.IXGBE_RING_SIZE * 16u, idx: queueIndex);
            // "- Program SRRCTL associated with this queue according to the size of the buffers and the required header control."
            //	Section 8.2.3.8.7 Split Receive Control Registers (SRRCTL[n]):
            //		"BSIZEPACKET, Receive Buffer Size for Packet Buffer. The value is in 1 KB resolution. Value can be from 1 KB to 16 KB."
            // Set it to the ceiling of PACKET_SIZE_MAX in KB.
            uint value = IxgbeConstants.IXGBE_PACKET_BUFFER_SIZE / 1024u + (IxgbeConstants.IXGBE_PACKET_BUFFER_SIZE % 1024u != 0 ? 1 : 0);
            IxgbeReg.SRRCTL.Write(device.Addr, value, IxgbeRegField.SRRCTL_BSIZEPACKET, idx: queueIndex);
            //		"DESCTYPE, Define the descriptor type in Rx: Init Val 000b [...] 000b = Legacy."
            //		"Drop_En, Drop Enabled. If set to 1b, packets received to the queue when no descriptors are available to store them are dropped."
            // Enable this because of assumption DROP
            IxgbeReg.SRRCTL.Set(device.Addr, IxgbeRegField.SRRCTL_DROP_EN, idx: queueIndex);

            // "- If header split is required for this queue, program the appropriate PSRTYPE for the appropriate headers."
            // Section 7.1.10 Header Splitting: "Header Splitting mode might cause unpredictable behavior and should not be used with the 82599."
            // "- Program RSC mode for the queue via the RSCCTL register."
            // Nothing to do, we do not want RSC.
            // "- Program RXDCTL with appropriate values including the queue Enable bit. Note that packets directed to a disabled queue are dropped."
            IxgbeReg.RXDCTL.Set(device.Addr, IxgbeRegField.RXDCTL_ENABLE, queueIndex);
            // "- Poll the RXDCTL register until the Enable bit is set. The tail should not be bumped before this bit was read as 1b."
            // INTERPRETATION-MISSING: No timeout is mentioned here, let's say 1s to be safe.
            if (Ixgbe.TimeoutCondition(1000 * 1000, IxgbeReg.RXDCTL.Cleared(device.Addr, IxgbeRegField.RXDCTL_ENABLE, queueIndex)))
            {
                log.Debug("RXDCTL.ENABLE did not set, cannot enable queue");
                return false;
            }

            // "- Bump the tail pointer (RDT) to enable descriptors fetching by setting it to the ring length minus one."
            // 	Section 7.1.9 Receive Descriptor Queue Structure:
            // 	"Software inserts receive descriptors by advancing the tail pointer(s) to refer to the address of the entry just beyond the last valid descriptor."
            IxgbeReg.RDT.Write(device.Addr, IxgbeConstants.IXGBE_RING_SIZE - 1u, idx: queueIndex);
            // "- Enable the receive path by setting RXCTRL.RXEN. This should be done only after all other settings are done following the steps below."
            //	"- Halt the receive data path by setting SECRXCTRL.RX_DIS bit."
            IxgbeReg.SECRXCTRL.Set(device.Addr, IxgbeRegField.SECRXCTRL_RX_DIS);
            //	"- Wait for the data paths to be emptied by HW. Poll the SECRXSTAT.SECRX_RDY bit until it is asserted by HW."
            // INTERPRETATION-MISSING: Another undefined timeout, assuming 1s as usual
            if (Ixgbe.TimeoutCondition(1000 * 1000, IxgbeReg.SECRXSTAT.Cleared(device.Addr, IxgbeRegField.SECRXSTAT_SECRX_RDY)))
            {
                log.Debug("SECRXSTAT.SECRXRDY timed out, cannot enable queue");
                return false;
            }

            //	"- Set RXCTRL.RXEN"
            IxgbeReg.RXCTRL.Set(device.Addr, IxgbeRegField.RXCTRL_RXEN);
            //	"- Clear the SECRXCTRL.SECRX_DIS bits to enable receive data path"
            // INTERPRETATION-TYPO: This refers to RX_DIS, not SECRX_DIS, since it's RX_DIS being cleared that enables the receive data path.
            IxgbeReg.SECRXCTRL.Clear(device.Addr, IxgbeRegField.SECRXCTRL_RX_DIS);
            //	"- If software uses the receive descriptor minimum threshold Interrupt, that value should be set."
            // We do not have to set this by assumption NOWANT
            // "  Set bit 16 of the CTRL_EXT register and clear bit 12 of the DCA_RXCTRL[n] register[n]."
            // Section 8.2.3.1.3 Extended Device Control Register (CTRL_EXT): Bit 16 == "NS_DIS, No Snoop Disable"
            IxgbeReg.CTRLEXT.Set(device.Addr, IxgbeRegField.CTRLEXT_NSDIS);
            // Section 8.2.3.11.1 Rx DCA Control Register (DCA_RXCTRL[n]): Bit 12 == "Default 1b; Reserved. Must be set to 0."
            IxgbeReg.DCARXCTRL.Set(device.Addr, IxgbeRegField.DCARXCTRL_UNKNOWN, idx: queueIndex);


            this.receiveTailAddr = (UIntPtr)((ulong)device.Addr + IxgbeReg.RDT.Read(device.Addr, idx: queueIndex));
            return true;
        }
    }

    public class MemoryAllocationErrorException : Exception
    {

    }
}
