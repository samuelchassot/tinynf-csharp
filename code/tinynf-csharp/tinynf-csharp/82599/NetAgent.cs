using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Env.linuxx86;
using Utilities;

namespace tinynf_sam
{
    public unsafe class NetAgent
    {
        private const uint RING_ADDRESS_should_be_128_byte_aligned = IxgbeConstants.IXGBE_RING_SIZE * 16 >= 128 ? 0 : -1;
        private const byte TransmitQueuesFitInUint8 = byte.MaxValue >= IxgbeConstants.IXGBE_TRANSMIT_QUEUES_COUNT ? 0 : -1;


        private UIntPtr buffer;
        private UIntPtr receiveTailAddr;
        private ulong processedDelimiter;
        private ulong outputsCount;
        private ulong flushedProcessedDelimiter; // -1 if there was no packet last time, otherwise last flushed processed_delimiter
        private UIntPtr padding;
        // transmit heads must be 16-byte aligned; see alignment remarks in transmit queue setup
        // (there is also a runtime check to make sure the array itself is aligned properly)
        // plus, we want each head on its own cache line to avoid conflicts
        // thus, using assumption CACHE, we multiply indices by 16
        public const uint TRANSMIT_HEAD_MULTIPLIER = 16;

        //transmitHeadsPtr here is the ptr pointing at the beginning of an allocated part of the memory of the size
        // IXGBE_AGENT_OUTPUTS_MAX * TRANSMIT_HEAD_MULTIPLIER
        private UIntPtr transmitHeadsPtr; // size = IXGBE_AGENT_OUTPUTS_MAX * TRANSMIT_HEAD_MULTIPLIER
        private UIntPtr[] rings; // 0 == shared receive/transmit, rest are exclusive transmit, size = IXGBE_AGENT_OUTPUTS_MAX
        private UIntPtr[] transmitTailAddrs;


        /// <summary>
        /// Create a new NetAgent and return it. Because it needs to allocate memory and do other things
        /// that can fail, it throws exceptions
        /// </summary>
        /// <returns></returns>
        public NetAgent(Memory mem)
        {
            receiveTailAddr = UIntPtr.Zero;
            padding = mem.MemAllocate(3*8);
            if(padding == UIntPtr.Zero)
            {
                Util.log.Debug("Cannot allocate memory for the padding");
                throw new MemoryAllocationErrorException();
            }
            transmitTailAddrs = new UIntPtr[(int)IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX];
            rings = new UIntPtr[IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX];
            processedDelimiter = 0;

            transmitHeadsPtr = mem.MemAllocate(IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX * TRANSMIT_HEAD_MULTIPLIER * 4);
            if(transmitHeadsPtr == UIntPtr.Zero)
            {
                Util.log.Debug("Cannot allocate memory for the transmitHeads");
                throw new MemoryAllocationErrorException();
            }


            UIntPtr buffPtr = mem.MemAllocate(IxgbeConstants.IXGBE_RING_SIZE * IxgbeConstants.IXGBE_PACKET_BUFFER_SIZE);
            if (buffPtr == UIntPtr.Zero)
            {
                Util.log.Debug("Cannot allocate memory for the buffer in net agent init");
                throw new MemoryAllocationErrorException();
            }
            this.buffer = buffPtr;

            for (ulong n = 0; n < IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX; n++)
            {
                UIntPtr ringAddr = mem.MemAllocate(IxgbeConstants.IXGBE_RING_SIZE * 16);
                if (ringAddr == UIntPtr.Zero)
                {
                    Util.log.Debug("Cannot allocate memory for the ring number " + n + " for InitNetAgent");
                    for (ulong m = 0; m < n; m++)
                    {
                        mem.MemFree(this.rings[m]);
                    }
                    mem.MemFree(buffPtr);
                    throw new MemoryAllocationErrorException();
                }
                this.rings[n] = ringAddr;

                // initialize to uintptr.zero
                this.transmitTailAddrs[n] = padding;
            }

            // Start in "no packet" state
            flushedProcessedDelimiter = ulong.MaxValue; //which is equal to (ulong)-1
        }


        public UIntPtr Buffer { get => buffer; private set => buffer = value; }
        public UIntPtr Receive_tail_addr { get => receiveTailAddr; private set => receiveTailAddr = value; }
        public ulong Processed_delimiter { get => processedDelimiter; private set => processedDelimiter = value; }
        public ulong Outputs_count { get => outputsCount; private set => outputsCount = value; }
        public ulong Flushed_processed_delimiter { get => flushedProcessedDelimiter; private set => flushedProcessedDelimiter = value; }
        public UIntPtr Padding { get => padding; private set => padding = value; }

        public bool SetInput(Memory mem, NetDevice device)
        {
            if (this.receiveTailAddr != UIntPtr.Zero)
            {
                Util.log.Debug("NetAgent receive was already set");
                return false;
            }
            // The 82599 has more than one receive queue, but we only need queue 0
            ushort queueIndex = 0;

            // See later for details of RXDCTL.ENABLE
            if (!IxgbeReg.RXDCTL.Cleared(device.Addr, IxgbeRegField.RXDCTL_ENABLE, queueIndex))
            {
                Util.log.Debug("Receive queue is already in use");
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
            UIntPtr ringPhysAddr = mem.MemVirtToPhys(this.rings[0]);
            if (ringPhysAddr == UIntPtr.Zero)
            {
                Util.log.Debug("Could not get phys addr of main ring");
                return false;
            }

            Util.log.Debug("For agent with rings[0] = " + rings[0] + " , the phys addr of the rings[0] is " + ringPhysAddr);

            IxgbeReg.RDBAH.Write(device.Addr, (uint)((ulong)ringPhysAddr >> 32), idx: queueIndex);
            IxgbeReg.RDBAL.Write(device.Addr, (uint)(ulong)ringPhysAddr, idx: queueIndex);
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
            if (IxgbeConstants.TimeoutCondition(1000 * 1000, () => IxgbeReg.RXDCTL.Cleared(device.Addr, IxgbeRegField.RXDCTL_ENABLE, queueIndex)))
            {
                Util.log.Debug("RXDCTL.ENABLE did not set, cannot enable queue");
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
            if (IxgbeConstants.TimeoutCondition(1000 * 1000, () => IxgbeReg.SECRXSTAT.Cleared(device.Addr, IxgbeRegField.SECRXSTAT_SECRX_RDY)))
            {
                Util.log.Debug("SECRXSTAT.SECRXRDY timed out, cannot enable queue");
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
            IxgbeReg.DCARXCTRL.Clear(device.Addr, IxgbeRegField.DCARXCTRL_UNKNOWN, idx: queueIndex);


            this.receiveTailAddr = (UIntPtr)((ulong)device.Addr + IxgbeReg.RDT.GetAddr(queueIndex));
            return true;
        }

        public bool AddOutput(Memory mem, NetDevice device, ulong longQueueIndex)
        {
            ulong outputsCountLocalVar = 0;
            for(; outputsCountLocalVar < IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX; outputsCountLocalVar++)
            {
                if(this.transmitTailAddrs[outputsCountLocalVar] == padding)
                {
                    break;
                }
            }

            if (outputsCountLocalVar == IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX)
            {
                Util.log.Debug("The agent is already using the maximum amount of transmit queues");
                return false;
            }
            if (longQueueIndex >= IxgbeConstants.IXGBE_TRANSMIT_QUEUES_COUNT)
            {
                Util.log.Debug("Transmit queue does not exist");
                return false;
            }

            //This code assumes transmit queues fit in an Uint8, see private const def above for assert like
            byte queueIndex = (byte)longQueueIndex;

            // See later for details of TXDCTL.ENABLE
            if(!IxgbeReg.TXDCTL.Cleared(device.Addr, IxgbeRegField.TXDCTL_ENABLE, queueIndex))
            {
                Util.log.Debug("Transmit queue is already in use");
                return false;
            }

            // "The following steps should be done once per transmit queue:"
            // "- Allocate a region of memory for the transmit descriptor list."
            // This is already done in agent initialization as agent->rings[*]

            // was volatile in C code but in C#,
            //cannot make pointer to volatile value so we will read/write to it using Thread.Volatile.Read/Write
            ulong* ring = (ulong*)this.rings[outputsCount];  

            // Program all descriptors' buffer address now
            // n was a uintptr_t in C, I use ulong to be sure
            for (ulong n = 0; n < IxgbeConstants.IXGBE_RING_SIZE; n++)
            {
                // Section 7.2.3.2.2 Legacy Transmit Descriptor Format:
                // "Buffer Address (64)", 1st line offset 0
                UIntPtr packet = (UIntPtr)((ulong)buffer + n * IxgbeConstants.IXGBE_PACKET_BUFFER_SIZE);
                // Write a 0 in the page to force the system to load the page into memory
                *(byte*)packet = 0;

                UIntPtr packetPhysAddr = mem.MemVirtToPhys(packet);
                if(packetPhysAddr == UIntPtr.Zero)
                {
                    Util.log.Debug("Could not get a packet's physical address for ring n = " + n);
                    return false;
                }

                //ring[n * 2u] = packetPhysAddr; IN C CODE
                Volatile.Write(ref *(ring + n * 2u), (ulong)packetPhysAddr);
            }

            // "- Program the descriptor base address with the address of the region (TDBAL, TDBAH)."
            // 	Section 8.2.3.9.5 Transmit Descriptor Base Address Low (TDBAL[n]):
            // 	"The Transmit Descriptor Base Address must point to a 128 byte-aligned block of data."
            // This alignment is guaranteed by the agent initialization
            UIntPtr ringPhysAddr = mem.MemVirtToPhys((UIntPtr)ring);
            if(ringPhysAddr == UIntPtr.Zero)
            {
                Util.log.Debug("Could not get a transmit ring's physical address");
                return false;
            }

            IxgbeReg.TDBAH.Write(device.Addr, (uint)((ulong)ringPhysAddr >> 32), idx: queueIndex);
            IxgbeReg.TDBAL.Write(device.Addr, (uint)(ulong)ringPhysAddr, idx: queueIndex);


            // "- Set the length register to the size of the descriptor ring (TDLEN)."
            // 	Section 8.2.3.9.7 Transmit Descriptor Length (TDLEN[n]):
            // 	"This register sets the number of bytes allocated for descriptors in the circular descriptor buffer."
            // Note that each descriptor is 16 bytes.
            IxgbeReg.TDLEN.Write(device.Addr, IxgbeConstants.IXGBE_RING_SIZE * 16u, idx: queueIndex);
            // "- Program the TXDCTL register with the desired TX descriptor write back policy (see Section 8.2.3.9.10 for recommended values)."
            //	Section 8.2.3.9.10 Transmit Descriptor Control (TXDCTL[n]):
            //	"HTHRESH should be given a non-zero value each time PTHRESH is used."
            //	"For PTHRESH and HTHRESH recommended setting please refer to Section 7.2.3.4."
            // INTERPRETATION-MISSING: The "recommended values" are in 7.2.3.4.1 very vague; we use (cache line size)/(descriptor size) for HTHRESH (i.e. 64/16 by assumption CACHE),
            //                         and a completely arbitrary value for PTHRESH
            // PERFORMANCE: This is required to forward 10G traffic on a single NIC.
            IxgbeReg.TXDCTL.Write(device.Addr, 60, IxgbeRegField.TXDCTL_PTHRESH, idx: queueIndex);
            IxgbeReg.TXDCTL.Write(device.Addr, 4, IxgbeRegField.TXDCTL_HTHRESH, idx: queueIndex);
            // "- If needed, set TDWBAL/TWDBAH to enable head write back."

            //SAM SPECIFIC
            // here in C : !tn_mem_virt_to_phys((uintptr_t) & (agent->transmit_heads[outputs_count * TRANSMIT_HEAD_MULTIPLIER]), &head_phys_addr)
            // so we need transmitHeadsPtr + outputsCount*TRANSMIT_HEAD_MULTIPLIER*4 (*4 because it is an uint32_t array in C)
            UIntPtr headPhysAddr = mem.MemVirtToPhys((UIntPtr)((ulong)transmitHeadsPtr + outputsCount*TRANSMIT_HEAD_MULTIPLIER*4));
            if (headPhysAddr == UIntPtr.Zero)
            {
                Util.log.Debug("Could not get the physical address of the transmit head");
                return false;
            }
            //	Section 7.2.3.5.2 Tx Head Pointer Write Back:
            //	"The low register's LSB hold the control bits.
            // 	 * The Head_WB_EN bit enables activation of tail write back. In this case, no descriptor write back is executed.
            // 	 * The 30 upper bits of this register hold the lowest 32 bits of the head write-back address, assuming that the two last bits are zero."
            //	"software should [...] make sure the TDBAL value is Dword-aligned."
            //	Section 8.2.3.9.11 Tx Descriptor completion Write Back Address Low (TDWBAL[n]): "the actual address is Qword aligned"
            // INTERPRETATION-CONTRADICTION: There is an obvious contradiction here; qword-aligned seems like a safe option since it will also be dword-aligned.
            // INTERPRETATION-INCORRECT: Empirically, the answer is... 16 bytes. Write-back has no effect otherwise. So both versions are wrong.
            if ((ulong)headPhysAddr % 16u != 0)
            {
                Util.log.Debug("Transmit head's physical address is not aligned properly");
                return false;
            }
            //	Section 8.2.3.9.11 Tx Descriptor Completion Write Back Address Low (TDWBAL[n]):
            //	"Head_WB_En, bit 0 [...] 1b = Head write-back is enabled."
            //	"Reserved, bit 1"
            IxgbeReg.TDWBAH.Write(device.Addr, (uint)((ulong)headPhysAddr >> 32), idx: queueIndex);
            IxgbeReg.TDWBAL.Write(device.Addr, (uint)((ulong)headPhysAddr | 1), idx: queueIndex);
            // INTERPRETATION-MISSING: We must disable relaxed ordering of head pointer write-back, since it could cause the head pointer to be updated backwards
            IxgbeReg.DCATXCTRL.Clear(device.Addr, IxgbeRegField.DCATXCTRL_TX_DESC_WB_RO_EN, idx: queueIndex);
            // "- Enable transmit path by setting DMATXCTL.TE.
            //    This step should be executed only for the first enabled transmit queue and does not need to be repeated for any following queues."
            // Do it every time, it makes the code simpler.
            IxgbeReg.DMATXCTL.Set(device.Addr, IxgbeRegField.DMATXCTL_TE);
            // "- Enable the queue using TXDCTL.ENABLE.
            //    Poll the TXDCTL register until the Enable bit is set."
            // INTERPRETATION-MISSING: No timeout is mentioned here, let's say 1s to be safe.
            IxgbeReg.TXDCTL.Set(device.Addr, IxgbeRegField.TXDCTL_ENABLE, queueIndex);

            if(IxgbeConstants.TimeoutCondition(1000*1000, () => IxgbeReg.TXDCTL.Cleared(device.Addr, IxgbeRegField.TXDCTL_ENABLE, queueIndex)))
            {
                Util.log.Debug("TXDCTL.ENABLE did not set, cannot enable queue");
                return false;
            }
            // "Note: The tail register of the queue (TDT) should not be bumped until the queue is enabled."
            // Nothing to transmit yet, so leave TDT alone.
            transmitTailAddrs[outputsCount] = (UIntPtr)((ulong)device.Addr + IxgbeReg.TDT.GetAddr(queueIndex));
            outputsCount += 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public (bool ok, int packetLength, UIntPtr packetAddr) Receive()
        {
            // Since descriptors are 16 bytes, the index must be doubled
            ulong* mainMetadataAddr = (ulong*)rings[0] + 2u * processedDelimiter + 1;
            ulong receiveMetadata = Volatile.Read(ref *mainMetadataAddr);
            // Section 7.1.5 Legacy Receive Descriptor Format:
            // "Status Field (8-bit offset 32, 2nd line)": Bit 0 = DD, "Descriptor Done."

            if ((receiveMetadata & IxgbeConstants.BitNSetLong(32) ) == 0)
            {
                // No packet; flush if we need to, i.e., 2nd part of the processor
                // Done here since we must eventually flush after processing a packet even if no more packets are received
                if (flushedProcessedDelimiter != ulong.MaxValue && flushedProcessedDelimiter != processedDelimiter)
                {
                    for (ulong n = 0; n < IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX; n++)
                    {
                        IxgbeRegExtension.WriteRegRaw(transmitTailAddrs[n], (uint)processedDelimiter);
                    }
                }
                // Record that there was no packet
                
                flushedProcessedDelimiter = ulong.MaxValue;
                return (false, -1, (UIntPtr)0);
            }
            // This cannot overflow because the packet is by definition in an allocated block of memory
            byte* outPacketAddr = (byte*)buffer + IxgbeConstants.IXGBE_PACKET_BUFFER_SIZE * processedDelimiter;
            // "Length Field (16-bit offset 0, 2nd line): The length indicated in this field covers the data written to a receive buffer."
            int outPacketLength = (int)(receiveMetadata & 0xFFu);
            // Note that the out_ parameters have no meaning if this is false, but it's fine, their value will still make sense
            return (true, outPacketLength, (UIntPtr)outPacketAddr);
        }

        public void Transmit(int packetLength, bool[] outputs)
        {
            // Section 7.2.3.2.2 Legacy Transmit Descriptor Format:
            // "Buffer Address (64)", 1st line
            // 2nd line:
            // "Length", bits 0-15: "Length (TDESC.LENGTH) specifies the length in bytes to be fetched from the buffer address provided"
            // "Note: Descriptors with zero length (null descriptors) transfer no data."
            // "CSO", bits 16-23: "A Checksum Offset (TDESC.CSO) field indicates where, relative to the start of the packet, to insert a TCP checksum if this mode is enabled"
            // All zero
            // "CMD", bits 24-31:
            // "RSV (bit 7) - Reserved
            //  VLE (bit 6) - VLAN Packet Enable [...]
            //  DEXT (bit 5) - Descriptor extension (zero for legacy mode)
            //  RSV (bit 4) - Reserved
            //  RS (bit 3) - Report Status: "signals hardware to report the DMA completion status indication"
            //  IC (bit 2) - Insert Checksum - Hardware inserts a checksum at the offset indicated by the CSO field if the Insert Checksum bit (IC) is set.
            //  IFCS (bit 1) — Insert FCS:
            //	"There are several cases in which software must set IFCS as follows: -Transmitting a short packet while padding is enabled by the HLREG0.TXPADEN bit."
            //      Section 8.2.3.22.8 MAC Core Control 0 Register (HLREG0): "TXPADEN, init val 1b; 1b = Pad frames"
            //  EOP (bit 0) - End of Packet"
            // STA, bits 32-35: "DD (bit 0) - Descriptor Done. The other bits in the STA field are reserved."
            // All zero
            // Rsvd, bits 36-39: "Reserved."
            // All zero
            // CSS, bits 40-47: "A Checksum Start (TDESC.CSS) field indicates where to begin computing the checksum."
            // All zero
            // VLAN, bits 48-63:
            // All zero
            // INTERPRETATION-INCORRECT: Despite being marked as "reserved", the buffer address does not get clobbered by write-back, so no need to set it again
            // This means all we have to do is set the length in the first 16 bits, then bits 0,1 of CMD, and bit 3 of CMD if we want to get updated
            // Importantly, since bit 32 will stay at 0, and we share the receive ring and the first transmit ring, it will clear the Descriptor Done flag of the receive descriptor
            // If not all transmit rings are used, we will write into an unused (but allocated!) ring, that's fine
            // Not setting the RS bit every time is a huge perf win in throughput (a few Gb/s) with no apparent impact on latency

            ulong rsBit = (ulong)((processedDelimiter & (IxgbeConstants.IXGBE_AGENT_TRANSMIT_PERIOD - 1)) == (IxgbeConstants.IXGBE_AGENT_TRANSMIT_PERIOD - 1) ? 1 : 0) << (24 + 3);
            for (ulong n = 0; n < IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX; n++)
            { 
               Volatile.Write(ref *((ulong*)rings[n] + 2u * processedDelimiter + 1), 
                    ((outputs[n] ? 1u : 0u) * (ulong)packetLength) | rsBit | IxgbeConstants.BitNSetLong(24 + 1) | IxgbeConstants.BitNSetLong(24));
            }

            // Increment the processed delimiter, modulo the ring size
            processedDelimiter = (processedDelimiter + 1u) & (IxgbeConstants.IXGBE_RING_SIZE - 1);
            // Flush if we need to, i.e., 2nd part of the processor
            // Done here so that latency is minimal in low-load cases
            if ((flushedProcessedDelimiter == ulong.MaxValue) || (processedDelimiter == ((flushedProcessedDelimiter + IxgbeConstants.IXGBE_AGENT_PROCESS_PERIOD) & (IxgbeConstants.IXGBE_RING_SIZE - 1))))
            {
                for (ulong n = 0; n < IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX; n++)
                {
                    IxgbeRegExtension.WriteRegRaw(transmitTailAddrs[n], (uint)processedDelimiter);
                }
                flushedProcessedDelimiter = processedDelimiter;
            }

            // Transmitter 2nd part, moving descriptors to the receive pool
            // This should happen as rarely as the update period since that's the period controlling transmit head updates from the NIC
            // Doing it here allows us to (1) reuse rs_bit and (2) make less divergent paths for symbolic execution (as opposed to doing it in receive)
            if (rsBit != 0)
            {
                // In case there are no transmit queues, the "earliest" transmit head is the processed delimiter
                uint earliestTransmitHead = (uint)processedDelimiter;
                ulong minDiff = ulong.MaxValue;

                // Race conditions are possible here, but all they can do is make our "earliest transmit head" value too low, which is fine
                for (ulong n = 0; n < outputsCount; n++)
                {
                    uint head = Volatile.Read(ref *((uint*)transmitHeadsPtr + n * TRANSMIT_HEAD_MULTIPLIER)); //in C: uint32_t head = agent->transmit_heads[n * TRANSMIT_HEAD_MULTIPLIER];
                    ulong diff = head - processedDelimiter;
                    if(diff <= minDiff)
                    {
                        earliestTransmitHead = head;
                        minDiff = diff;
                    }
                }
                IxgbeRegExtension.WriteRegRaw(receiveTailAddr, (earliestTransmitHead - 1) & (IxgbeConstants.IXGBE_RING_SIZE - 1));

            }
        }

        // --------------
        // High-level API
        // --------------
        
        public void Process(Func<int, UIntPtr, bool[], int> packetHandler)
        {
            (bool ok, int packetLength, UIntPtr packetPtr) = this.Receive();
            if (!ok)
            {
                return;
            }

            bool[] outputs = new bool[IxgbeConstants.IXGBE_AGENT_OUTPUTS_MAX];
            int newpacketLength = packetHandler(packetLength, packetPtr, outputs);
            Transmit(newpacketLength, outputs);
        }

    }

    public class MemoryAllocationErrorException : Exception
    {

    }
}
