using System;
using Env;
using Env.linuxx86;
using Utilities;

namespace tinynf_sam
{
    public class NetDevice
    {
        private UIntPtr addr;
        private PCIDevice pciDevice;

        private NetDevice(PCIDevice pciDevice)
        {
            this.pciDevice = pciDevice;
        }
        public UIntPtr Addr { get { return addr; } private set => addr = value; }

        public PCIDevice PciDevice { get => pciDevice; }

        public static NetDevice CreateInstance(Memory memory, PCIDevice pciDevice)
        {
            // The NIC supports 64-bit addresses, so pointers can't be larger
            if (UIntPtr.Size > sizeof(ulong))
            {
                return null;
            }

            // First make sure the PCI device is really an 82599ES 10-Gigabit SFI/SFP+ Network Connection
            // According to https://cateee.net/lkddb/web-lkddb/IXGBE.html, this means vendor ID (bottom 16 bits) 8086, device ID (top 16 bits) 10FB
            uint pciID = PciReg.PCI_ID.Read(pciDevice);
            Util.log.Debug(String.Format("PciReg PCI_ID read: {0:X}", pciID));
            if (pciID != ((0x10FBu << 16) | 0x8086u))
            {
                Util.log.Debug("PCI device is not what was expected");
                return null;
            }

            // By assumption PCIBRIDGES, the bridges will not get in our way
            // By assumption PCIBARS, the PCI BARs have been configured already
            // Section 9.3.7.1.4 Power Management Control / Status Register (PMCSR):
            // "No_Soft_Reset. This bit is always set to 0b to indicate that the 82599 performs an internal reset upon transitioning from D3hot to D0 via software control of the PowerState bits."
            // Thus, the device cannot go from D3 to D0 without resetting, which would mean losing the BARs.
            if (!PciReg.PCI_PMCSR.Cleared(pciDevice, PciRegField.PCI_PMCSR_POWER_STATE))
            {
                Util.log.Debug("PCI device not in D0.");
                return null;
            }

            // The bus master may not be ebabled; enable it just in case.
            PciReg.PCI_COMMAND.Set(pciDevice, PciRegField.PCI_COMMAND_BUS_MASTER_ENABLE);
            // Same for memory reads, i.e. actually using the BARs.
            PciReg.PCI_COMMAND.Set(pciDevice, PciRegField.PCI_COMMAND_MEMORY_ACCESS_ENABLE);
            // Finally, since we don't want interrupts and certainly not legacy ones, make sure they're disabled
            PciReg.PCI_COMMAND.Set(pciDevice, PciRegField.PCI_COMMAND_INTERRUPT_DISABLE);

            // Section 8.2.2 Registers Summary PF — BAR 0: As the section title indicate, registers are at the address pointed to by BAR 0, which is the only one we care about
            uint pciBar0Low = PciReg.PCI_BAR0_LOW.Read(pciDevice);
            // Sanity check: a 64-bit BAR must have bit 2 of low as 1 and bit 1 of low as 0 as per Table 9-4 Base Address Registers' Fields
            if ((pciBar0Low & IxgbeConstants.BitNSet(2)) == 0 || (pciBar0Low & IxgbeConstants.BitNSet(1)) != 0)
            {
                Util.log.Debug("BAR0 is not a 64-bit BAR");
                return null;
            }

            uint pciBar0High = PciReg.PCI_BAR0_HIGH.Read(pciDevice);
            // No need to detect the size, since we know exactly which device we're dealing with. (This also means no writes to BARs, one less chance to mess everything up)

            NetDevice newDevice = new NetDevice(pciDevice);
            // Section 9.3.6.1 Memory and IO Base Address Registers:
            // As indicated in Table 9-4, the low 4 bits are read-only and not part of the address
            UIntPtr devPhysAddr = (UIntPtr)((((ulong)pciBar0High) << 32) | (pciBar0Low & ~IxgbeConstants.BitNSet(0, 3)));
            // Section 8.1 Address Regions: "Region Size" of "Internal registers memories and Flash (memory BAR)" is "128 KB + Flash_Size"
            // Thus we can ask for 128KB, since we don't know the flash size (and don't need it thus no need to actually check it)
            UIntPtr virtAddr = memory.MemPhysToVirt(devPhysAddr, 128 * 1024);
            if (virtAddr == UIntPtr.Zero)
            {
                Util.log.Debug("Phys to virt translation failed. NetDevice creation");
                return null;
            }
            newDevice.Addr = virtAddr;

            Util.log.Verbose(string.Format("Device {0}:{1}.{2} mapped to 0x{3}", newDevice.pciDevice.Bus, newDevice.pciDevice.Device, newDevice.pciDevice.Function, newDevice.Addr));

            // "The following sequence of commands is typically issued to the device by the software device driver in order to initialize the 82599 for normal operation.
            //  The major initialization steps are:"
            // "- Disable interrupts"
            // "- Issue global reset and perform general configuration (see Section 4.6.3.2)."
            // 	Section 4.6.3.1 Interrupts During Initialization:
            // 	"Most drivers disable interrupts during initialization to prevent re-entrance.
            // 	 Interrupts are disabled by writing to the EIMC registers.
            //	 Note that the interrupts also need to be disabled after issuing a global reset, so a typical driver initialization flow is:
            //	 1. Disable interrupts.
            //	 2. Issue a global reset.
            //	 3. Disable interrupts (again)."
            // INTERPRETATION-POINTLESS: We do not support interrupts, there is no way we can have re-entrancy here, so we don't need step 1
            // 	Section 4.6.3.2 Global Reset and General Configuration:
            //	"Device initialization typically starts with a software reset that puts the device into a known state and enables the device driver to continue the initialization sequence."
            if (!newDevice.Reset())
            {
                Util.log.Debug("Could not reset.");
                return null;
            }

            //	"Following a Global Reset the Software driver should wait at least 10msec to enable smooth initialization flow."
            Time.SleepMicroSec(10 * 1000);
            //	Section 8.2.3.5.4 Extended Interrupt Mask Clear Register (EIMC):
            //	"Writing a 1b to any bit clears its corresponding bit in the EIMS register disabling the corresponding interrupt in the EICR register. Writing 0b has no impact"
            IxgbeReg.EIMC.Write(newDevice.Addr, 0x7FFFFFFFu, idx: 0);
            for (byte n = 1; n < IxgbeConstants.IXGBE_INTERRUPT_REGISTERS_COUNT; n++)
            {
                IxgbeReg.EIMC.Write(newDevice.Addr, 0xFFFFFFFFu, idx: n);
            }

            //	"To enable flow control, program the FCTTV, FCRTL, FCRTH, FCRTV and FCCFG registers.
            //	 If flow control is not enabled, these registers should be written with 0x0.
            //	 If Tx flow control is enabled then Tx CRC by hardware should be enabled as well (HLREG0.TXCRCEN = 1b).
            //	 Refer to Section 3.7.7.3.2 through Section 3.7.7.3.5 for the recommended setting of the Rx packet buffer sizes and flow control thresholds.
            //	 Note that FCRTH[n].RTH fields must be set by default regardless if flow control is enabled or not.
            //	 Typically, FCRTH[n] default value should be equal to RXPBSIZE[n]-0x6000. FCRTH[n].
            //	 FCEN should be set to 0b if flow control is not enabled as all the other registers previously indicated."
            // INTERPRETATION-POINTLESS: Sections 3.7.7.3.{2-5} are irrelevant here since we do not want flow control.
            // Section 8.2.3.3.2 Flow Control Transmit Timer Value n (FCTTVn): all init vals are 0
            // Section 8.2.3.3.3 Flow Control Receive Threshold Low (FCRTL[n]): all init vals are 0
            // Section 8.2.3.3.5 Flow Control Refresh Threshold Value (FCRTV): all init vals are 0
            // Section 8.2.3.3.7 Flow Control Configuration (FCCFG): all init vals are 0
            // INTERPRETATION-POINTLESS: Since all those registers default to zero, there is no need to overwrite them with 0x0.
            // Section 8.2.3.8.9 Receive Packet Buffer Size (RXPBSIZE[n])
            //	"The size is defined in KB units and the default size of PB[0] is 512 KB."
            // Section 8.2.3.3.4 Flow Control Receive Threshold High (FCRTH[n])
            //	"Bits 4:0; Reserved"
            //	"RTH: Bits 18:5; Receive packet buffer n FIFO high water mark for flow control transmission (32 bytes granularity)."
            //	"Bits 30:19; Reserved"
            //	"FCEN: Bit 31; Transmit flow control enable for packet buffer n."
            //	"This register contains the receive threshold used to determine when to send an XOFF packet and counts in units of bytes.
            //	 This value must be at least eight bytes less than the maximum number of bytes allocated to the receive packet buffer and the lower four bits must be programmed to 0x0 (16-byte granularity).
            //	 Each time the receive FIFO reaches the fullness indicated by RTH, hardware transmits a pause frame if the transmission of flow control frames is enabled."
            // INTERPRETATION-CONTRADICTION: There is an obvious contradiction in the stated granularities (16 vs 32 bytes). We assume 32 is correct, since this also covers the 16 byte case.
            // INTERPRETATION-MISSING: We assume that the "RXPBSIZE[n]-0x6000" calculation above refers to the RXPBSIZE in bytes (otherwise the size of FCRTH[n].RTH would be negative by default...)
            //                         Thus we set FCRTH[0].RTH = 512 * 1024 - 0x6000, which importantly has the 5 lowest bits unset.
            IxgbeReg.FCRTH.Write(newDevice.Addr, 512 * 1024 - 0x6000, IxgbeRegField.FCRTH_RTH, 0);

            // "- Wait for EEPROM auto read completion."
            // INTERPRETATION-MISSING: This refers to Section 8.2.3.2.1 EEPROM/Flash Control Register (EEC), Bit 9 "EEPROM Auto-Read Done"
            // INTERPRETATION-MISSING: No timeout is mentioned, so we use 1s.
            if (IxgbeConstants.TimeoutCondition(1000 * 1000, () => IxgbeReg.EEC.Cleared(newDevice.Addr, IxgbeRegField.EEC_AUTO_RD)))
            {
                Util.log.Debug("EEPROM auto read timed out");
                return null;
            }
            // INTERPRETATION-MISSING: We also need to check bit 8 of the same register, "EEPROM Present", which indicates "EEPROM is present and has the correct signature field. This bit is read-only.",
            //                 since bit 9 "is also set when the EEPROM is not present or whether its signature field is not valid."
            // INTERPRETATION-MISSING: We also need to check whether the EEPROM has a valid checksum, using the FWSM's register EXT_ERR_IND, where "0x00 = No error"
            if (IxgbeReg.EEC.Cleared(newDevice.Addr, IxgbeRegField.EEC_EE_PRES))
            {
                Util.log.Debug("EEPROM not present or invalid");
                return null;
            }

            // "- Wait for DMA initialization done (RDRXCTL.DMAIDONE)."
            // INTERPRETATION-MISSING: Once again, no timeout mentioned, so we use 1s
            if (IxgbeConstants.TimeoutCondition(1000 * 1000, () => IxgbeReg.RDRXCTL.Cleared(newDevice.Addr, IxgbeRegField.RDRXCTL_DMAIDONE)))
            {
                Util.log.Debug("DMA init timed out");
                return null;
            }

            // "- Setup the PHY and the link (see Section 4.6.4)."
            //	Section 8.2.3.22.19 Auto Negotiation Control Register (AUTOC):
            //	"Also programmable via EEPROM." is applied to all fields except bit 0, "Force Link Up"
            // INTERPRETATION-POINTLESS: AUTOC is already programmed via the EEPROM, we do not need to set up the PHY/link.
            // "- Initialize all statistical counters (see Section 4.6.5)."
            // By assumption NOCARE, we don't need to do anything here.
            // "- Initialize receive (see Section 4.6.7)."
            // Section 4.6.7 Receive Initialization
            //	"Initialize the following register tables before receive and transmit is enabled:"
            //	"- Receive Address (RAL[n] and RAH[n]) for used addresses."
            //	"Program the Receive Address register(s) (RAL[n], RAH[n]) per the station address
            //	 This can come from the EEPROM or from any other means (for example, it could be stored anywhere in the EEPROM or even in the platform PROM for LOM design)."
            //	Section 8.2.3.7.9 Receive Address High (RAH[n]):
            //		"After reset, if the EEPROM is present, the first register (Receive Address Register 0) is loaded from the IA field in the EEPROM, its Address Select field is 00b, and its Address Valid field is 1b."
            // INTERPRETATION-POINTLESS: Since we checked that the EEPROM is present and valid, RAH[0] and RAL[0] are initialized from the EEPROM, thus we do not need to initialize them.
            //	"- Receive Address High (RAH[n].VAL = 0b) for unused addresses."
            //	Section 8.2.3.7.9 Receive Address High (RAH[n]):
            //		"After reset, if the EEPROM is present, the first register (Receive Address Register 0) is loaded [...] The Address Valid field for all of the other registers are 0b."
            // INTERPRETATION-POINTLESS: Since we checked that the EEPROM is present and valid, RAH[n] for n != 0 has Address Valid == 0, thus we do not need to initialize them.
            //	"- Unicast Table Array (PFUTA)."
            //	Section 8.2.3.27.12 PF Unicast Table Array (PFUTA[n]):
            //		"There is one register per 32 bits of the unicast address table"
            //		"This table should be zeroed by software before start of operation."
            for (int n = 0; n < IxgbeConstants.IXGBE_UNICAST_TABLE_ARRAY_SIZE / 32; n++)
            {
                IxgbeReg.PFUTA.Clear(newDevice.Addr, idx: n);
            }

            //	"- VLAN Filter Table Array (VFTA[n])."
            //	Section 7.1.1.2 VLAN Filtering:
            //		Figure 7-3 states that matching to a valid VFTA[n] is only done if VLNCTRL.VFE is set.
            //	Section 8.2.3.7.2 VLAN Control Register (VLNCTRL):
            //		"VFE: Bit 30; Init val: 0; VLAN Filter Enable. 0b = Disabled (filter table does not decide packet acceptance)"
            // INTERPRETATION-POINTLESS: By default, VLAN filtering is disabled, and the value of VFTA[n] does not matter; thus we do not need to initialize them.
            //	"- VLAN Pool Filter (PFVLVF[n])."
            // INTERPRETATION-MISSING: While the spec appears to mention PFVLVF only in conjunction with VLNCTRL.VFE being enabled, let's be conservative and initialize them anyway.
            // 	Section 8.2.3.27.15 PF VM VLAN Pool Filter (PFVLVF[n]):
            //		"Software should initialize these registers before transmit and receive are enabled."
            for (int n = 0; n < IxgbeConstants.IXGBE_VLAN_FILTER_COUNT; n++)
            {
                IxgbeReg.PFVLVF.Clear(newDevice.Addr, idx: n);
            }
            //	"- MAC Pool Select Array (MPSAR[n])."
            //	Section 8.2.3.7.10 MAC Pool Select Array (MPSAR[n]):
            //		"Software should initialize these registers before transmit and receive are enabled."
            //		"Each couple of registers '2*n' and '2*n+1' are associated with Ethernet MAC address filter 'n' as defined by RAL[n] and RAH[n].
            //		 Each bit when set, enables packet reception with the associated pools as follows:
            //		 Bit 'i' in register '2*n' is associated with POOL 'i'.
            //		 Bit 'i' in register '2*n+1' is associated with POOL '32+i'."
            // INTERPRETATION-MISSING: We should enable all pools with address 0, just in case, and disable everything else since we only have 1 MAC address.
            IxgbeReg.MPSAR.Write(newDevice.Addr, 0xFFFFFFFFu, idx: 0);
            IxgbeReg.MPSAR.Write(newDevice.Addr, 0xFFFFFFFFu, idx: 1);

            for (int n = 2; n < IxgbeConstants.IXGBE_RECEIVE_ADDRS_COUNT * 2; n++)
            {
                IxgbeReg.MPSAR.Clear(newDevice.Addr, idx: n);
            }

            //	"- VLAN Pool Filter Bitmap (PFVLVFB[n])."
            // INTERPRETATION-MISSING: See above remark on PFVLVF
            //	Section 8.2.3.27.16: PF VM VLAN Pool Filter Bitmap
            for (int n = 0; n < IxgbeConstants.IXGBE_VLAN_FILTER_COUNT * 2; n++)
            {
                IxgbeReg.PFVLVFB.Clear(newDevice.Addr, idx: n);
            }
            //	"Set up the Multicast Table Array (MTA) registers.
            //	 This entire table should be zeroed and only the desired multicast addresses should be permitted (by writing 0x1 to the corresponding bit location).
            //	 Set the MCSTCTRL.MFE bit if multicast filtering is required."
            // Section 8.2.3.7.7 Multicast Table Array (MTA[n]): "Word wide bit vector specifying 32 bits in the multicast address filter table."
            for (int n = 0; n < IxgbeConstants.IXGBE_MULTICAST_TABLE_ARRAY_SIZE / 32; n++)
            {
                IxgbeReg.MTA.Clear(newDevice.Addr, idx: n);
            }

            //	"Initialize the flexible filters 0...5 — Flexible Host Filter Table registers (FHFT)."
            //	Section 5.3.3.2 Flexible Filter:
            //		"The 82599 supports a total of six host flexible filters.
            //		 Each filter can be configured to recognize any arbitrary pattern within the first 128 bytes of the packet.
            //		 To configure the flexible filter, software programs the required values into the Flexible Host Filter Table (FHFT).
            //		 These contain separate values for each filter.
            //		 Software must also enable the filter in the WUFC register, and enable the overall wake-up functionality must be enabled by setting the PME_En bit in the PMCSR or the WUC register."
            //	Section 8.2.3.24.2 Wake Up Filter Control Register (WUFC):
            //		"FLX{0-5}: Bits {16-21}; Init val 0b; Flexible Filter {0-5} Enable"
            // INTERPRETATION-POINTLESS: Since WUFC.FLX{0-5} are disabled by default, and FHFT(n) only matters if the corresponding WUFC.FLX is enabled, we do not need to do anything by assumption NOWANT
            //	"After all memories in the filter units previously indicated are initialized, enable ECC reporting by setting the RXFECCERR0.ECCFLT_EN bit."
            //	Section 8.2.3.7.23 Rx Filter ECC Err Insertion 0 (RXFECCERR0):
            //		"Filter ECC Error indication Enablement.
            //		 When set to 1b, enables the ECC-INT + the RXF-blocking during ECC-ERR in one of the Rx filter memories.
            //		 At 0b, the ECC Util.logic can still function overcoming only single errors while dual or multiple errors can be ignored silently."
            // INTERPRETATION-POINTLESS: Since we do not want flexible filters, this step is not necessary.
            //	"Program the different Rx filters and Rx offloads via registers FCTRL, VLNCTRL, MCSTCTRL, RXCSUM, RQTC, RFCTL, MPSAR, RSSRK, RETA, SAQF, DAQF, SDPQF, FTQF, SYNQF, ETQF, ETQS, RDRXCTL, RSCDBU."
            // We do not touch FCTRL here, if the user wants promiscuous mode they will call the appropriate function.
            //	Section 8.2.3.7.2 VLAN Control Register (VLNCTRL):
            //		"Bit 30, VLAN Filter Enable, Init val 0b; 0b = Disabled."
            // We do not need to set up VLNCTRL by assumption NOWANT
            //	Section 8.2.3.7.2 Multicast Control Register (MCSTCTRL):
            //		"Bit 2, Multicast Filter Enable, Init val 0b; 0b = The multicast table array filter (MTA[n]) is disabled."
            // Since multicast filtering is disabled by default, we do not need to set up MCSTCTRL by assumption NOWANT
            // 	Section 8.2.3.7.5 Receive Checksum Control (RXCSUM):
            //		"Bit 12, Init val 0b, IP Payload Checksum Enable."
            //		"Bit 13, Init val 0b, RSS/Fragment Checksum Status Selection."
            //		"The Receive Checksum Control register controls the receive checksum offloading features of the 82599."
            // Since checksum offloading is disabled by default, we do not need to set up RXCSUM by assumption NOWANT
            //	Section 8.2.3.7.13 RSS Queues Per Traffic Class Register (RQTC):
            //		"RQTC{0-7}, This field is used only if MRQC.MRQE equals 0100b or 0101b."
            //	Section 8.2.3.7.12 Multiple Receive Queues Command Register (MRQC):
            //		"MRQE, Init val 0x0"
            // Since RSS is disabled by default, we do not need to do anything by assumption NOWANT
            //	Section 8.2.3.7.6 Receive Filter Control Register (RFCTL):
            //		"Bit 5, Init val 0b; RSC Disable. The default value is 0b (RSC feature is enabled)."
            //		"Bit 6, Init val 0b; NFS Write disable."
            //		"Bit 7, Init val 0b; NFS Read disable."
            // We do not need to write to RFCTL by assumption NOWANT
            // We already initialized MPSAR earlier.
            //	Section 4.6.10.1.1 Global Filtering and Offload Capabilities:
            //		"In RSS mode, the RSS key (RSSRK) and redirection table (RETA) should be programmed."
            // Since we do not want RSS, we do not need to touch RSSRK or RETA.
            //	Section 8.2.3.7.19 Five tuple Queue Filter (FTQF[n]):
            //		All bits have an unspecified default value.
            //		"Mask, bits 29:25: Mask bits for the 5-tuple fields (1b = don’t compare)."
            //		"Queue Enable, bit 31; When set, enables filtering of Rx packets by the 5-tuple defined in this filter to the queue indicated in register L34TIMIR."
            // We clear Queue Enable. We then do not need to deal with SAQF, DAQF, SDPQF, SYNQF, by assumption NOWANT
            for (int n = 0; n < IxgbeConstants.IXGBE_5TUPLE_FILTERS_COUNT; n++)
            {
                IxgbeReg.FTQF.Clear(newDevice.Addr, IxgbeRegField.FTQF_QUEUE_ENABLE, n);
            }

            //	Section 7.1.2.3 L2 Ethertype Filters:
            //		"The following flow is used by the Ethertype filters:
            //		 1. If the Filter Enable bit is cleared, the filter is disabled and the following steps are ignored."
            //	Section 8.2.3.7.21 EType Queue Filter (ETQF[n]):
            //		"Bit 31, Filter Enable, Init val 0b; 0b = The filter is disabled for any functionality."
            // Since filters are disabled by default, we do not need to do anything to ETQF and ETQS by assumption NOWANT
            //	Section 8.2.3.8.8 Receive DMA Control Register (RDRXCTL):
            //		The only non-reserved, non-RO bit is "CRCStrip, bit 1, Init val 0; 0b = No CRC Strip by HW."
            // By assumption CRC, we enable CRCStrip
            IxgbeReg.RDRXCTL.Set(newDevice.Addr, IxgbeRegField.RDRXCTL_CRC_STRIP);
            //	"Note that RDRXCTL.CRCStrip and HLREG0.RXCRCSTRP must be set to the same value. At the same time the RDRXCTL.RSCFRSTSIZE should be set to 0x0 as opposed to its hardware default."
            // As explained earlier, HLREG0.RXCRCSTRP is already set to 1, so that's fine
            IxgbeReg.RDRXCTL.Set(newDevice.Addr, IxgbeRegField.RDRXCTL_RSCFRSTSIZE);
            //	Section 8.2.3.8.8 Receive DMA Control Register (RDRXCTL):
            //		"RSCACKC [...] Software should set this bit to 1b."
            IxgbeReg.RDRXCTL.Set(newDevice.Addr, IxgbeRegField.RDRXCTL_RSCACKC);
            //		"FCOE_WRFIX [...] Software should set this bit to 1b."
            IxgbeReg.RDRXCTL.Set(newDevice.Addr, IxgbeRegField.RDRXCTL_FCOE_WRFIX);
            // INTERPRETATION-MISSING: The setup forgets to mention RSCACKC and FCOE_WRFIX, but we should set those properly anyway.
            //	Section 8.2.3.8.12 RSC Data Buffer Control Register (RSCDBU):
            //		The only non-reserved bit is "RSCACKDIS, bit 7, init val 0b; Disable RSC for ACK Packets."
            // We do not need to change RSCDBU by assumption NOCARE
            //	"Program RXPBSIZE, MRQC, PFQDE, RTRUP2TC, MFLCN.RPFCE, and MFLCN.RFCE according to the DCB and virtualization modes (see Section 4.6.11.3)."
            //	Section 4.6.11.3.4 DCB-Off, VT-Off:
            //		"Set the configuration bits as specified in Section 4.6.11.3.1 with the following exceptions:"
            //		"Disable multiple packet buffers and allocate all queues and traffic to PB0:"
            //		"- RXPBSIZE[0].SIZE=0x200, RXPBSIZE[1-7].SIZE=0x0"
            //		Section 8.2.3.8.9 Receive Packet Buffer Size (RXPBSIZE[n]):
            //			"SIZE, Init val 0x200"
            //			"The default size of PB[1-7] is also 512 KB but it is meaningless in non-DCB mode."
            // INTERPRETATION-CONTRADICTION: We're told to clear PB[1-7] but also that it's meaningless. Let's clear it just in case...
            for (int n = 1; n < IxgbeConstants.IXGBE_TRAFFIC_CLASSES_COUNT; n++)
            {
                IxgbeReg.RXPBSIZE.Clear(newDevice.Addr, idx: n);
            }
            //		"- MRQC"
            //			"- Set MRQE to 0xxxb, with the three least significant bits set according to the RSS mode"
            // 			Section 8.2.3.7.12 Multiple Receive Queues Command Register (MRQC): "MRQE, Init Val 0x0; 0000b = RSS disabled"
            // Thus we do not need to modify MRQC.
            //		(from 4.6.11.3.1) "Queue Drop Enable (PFQDE) — In SR-IO the QDE bit should be set to 1b in the PFQDE register for all queues. In VMDq mode, the QDE bit should be set to 0b for all queues."
            // We do not need to change PFQDE by assumption NOWANT
            //		"- Rx UP to TC (RTRUP2TC), UPnMAP=0b, n=0,...,7"
            //		Section 8.2.3.10.4 DCB Receive User Priority to Traffic Class (RTRUP2TC): All init vals = 0
            // Thus we do not need to modify RTRUP2TC.
            //		"Allow no-drop policy in Rx:"
            //		"- PFQDE: The QDE bit should be set to 0b in the PFQDE register for all queues enabling per queue policy by the SRRCTL[n] setting."
            //		Section 8.2.3.27.9 PF PF Queue Drop Enable Register (PFQDE): "QDE, Init val 0b"
            // Thus we do not need to modify PFQDE.
            //		"Disable PFC and enable legacy flow control:"
            //		"- Disable receive PFC via: MFLCN.RPFCE=0b"
            //		Section 8.2.3.22.34 MAC Flow Control Register (MFLCN): "RPFCE, Init val 0b"
            // Thus we do not need to modify MFLCN.RPFCE.
            //		"- Enable receive legacy flow control via: MFLCN.RFCE=1b"
            IxgbeReg.MFLCN.Set(newDevice.Addr, IxgbeRegField.MFLCN_RFCE);
            //	"Enable jumbo reception by setting HLREG0.JUMBOEN in one of the following two cases:
            //	 1. Jumbo packets are expected. Set the MAXFRS.MFS to expected max packet size.
            //	 2. LinkSec encapsulation is expected."
            // We do not have anything to do here by assumption NOWANT
            //	"Enable receive coalescing if required as described in Section 4.6.7.2."
            // We do not need to do anything here by assumption NOWANT

            // We do not set up receive queues at this point.

            // "- Initialize transmit (see Section 4.6.8)."
            //	Section 4.6.8 Transmit Initialization:
            //		"- Program the HLREG0 register according to the required MAC behavior."
            //		Section 8.2.3.22.8 MAC Core Control 0 Register (HLREG0):
            //			"TXCRCEN, Init val 1b; Tx CRC Enable, Enables a CRC to be appended by hardware to a Tx packet if requested by user."
            // By assumption CRC, we want this default behavior.
            //			"RXCRCSTRP [...] Rx CRC STRIP [...] 1b = Strip CRC by HW (Default)."
            // We do not need to change this by assumption CRC
            //			"JUMBOEN [...] Jumbo Frame Enable [...] 0b = Disable jumbo frames (default)."
            // We do not need to change this by assumption NOWANT
            //			"TXPADEN, Init val 1b; Tx Pad Frame Enable. Pad short Tx frames to 64 bytes if requested by user."
            // We do not need to change this by assumption TXPAD
            //			"LPBK, Init val 0b; LOOPBACK. Turn On Loopback Where Transmit Data Is Sent Back Through Receiver."
            // We do not need to change this by assumption NOWANT
            //			"MDCSPD [...] MDC SPEED."
            //			"CONTMDC [...] Continuous MDC"
            // We do not need to change these by assumption TRUST
            //			"PREPEND [...] Number of 32-bit words starting after the preamble and SFD, to exclude from the CRC generator and checker (default – 0x0)."
            // We do not need to change this by assumption CRC
            //			"RXLNGTHERREN, Init val 1b [...] Rx Length Error Reporting. 1b = Enable reporting of rx_length_err events"
            // We do not need to change this by assumption REPORT
            //			"RXPADSTRIPEN [...] 0b = [...] (default). 1b = [...] (debug only)."
            // We do not need to change this by assumption NOWANT
            //		"- Program TCP segmentation parameters via registers DMATXCTL (while maintaining TE bit cleared), DTXTCPFLGL, and DTXTCPFLGH; and DCA parameters via DCA_TXCTRL."
            //			Section 8.2.3.9.2 DMA Tx Control (DMATXCTL):
            //				There is only one field besides TE that the user should modify: "GDV, Init val 0b, Global Double VLAN Mode."
            // We do not need to change DMATXCTL for now by assumption NOWANT
            //			Section 8.2.3.9.3 DMA Tx TCP Flags Control Low (DTXTCPFLGL),
            //			Section 8.2.3.9.4 DMA Tx TCP Flags Control High (DTXTCPFLGH):
            //				"This register holds the mask bits for the TCP flags in Tx segmentation."
            // We do not need to modify DTXTCPFLGL/H by assumption TRUST.
            // We do not need to modify DCA parameters by assumption NOWANT.
            //		"- Set RTTDCS.ARBDIS to 1b."
            IxgbeReg.RTTDCS.Set(newDevice.Addr, IxgbeRegField.RTTDCS_ARBDIS);

            //		"- Program DTXMXSZRQ, TXPBSIZE, TXPBTHRESH, MTQC, and MNGTXMAP, according to the DCB and virtualization modes (see Section 4.6.11.3)."
            //		Section 4.6.11.3.4 DCB-Off, VT-Off:
            //			"Set the configuration bits as specified in Section 4.6.11.3.1 with the following exceptions:"
            //			"- TXPBSIZE[0].SIZE=0xA0, TXPBSIZE[1-7].SIZE=0x0"
            //			Section 8.2.3.9.13 Transmit Packet Buffer Size (TXPBSIZE[n]):
            //				"SIZE, Init val 0xA0"
            //				"At default setting (no DCB) only packet buffer 0 is enabled and TXPBSIZE values for TC 1-7 are meaningless."
            // INTERPRETATION-CONTRADICTION: We're both told to clear TXPBSIZE[1-7] and that it's meaningless. Let's clear it to be safe.
            for (int n = 1; n < IxgbeConstants.IXGBE_TRAFFIC_CLASSES_COUNT; n++)
            {
                IxgbeReg.TXPBSIZE.Clear(newDevice.Addr, idx: n);
            }
            //			"- TXPBTHRESH.THRESH[0]=0xA0 — Maximum expected Tx packet length in this TC TXPBTHRESH.THRESH[1-7]=0x0"
            // INTERPRETATION-TYPO: Typo in the spec; should be TXPBTHRESH[0].THRESH
            //			Section 8.2.3.9.16 Tx Packet Buffer Threshold (TXPBTHRESH):
            //				"Default values: 0x96 for TXPBSIZE0, 0x0 for TXPBSIZE1-7."
            // INTERPRETATION-TYPO: Typo in the spec, this refers to TXPBTHRESH, not TXPBSIZE.
            // Thus we need to set TXPBTHRESH[0] but not TXPBTHRESH[1-7].


            IxgbeReg.TXPBTHRESH.Write(newDevice.Addr, 0xA0u - (IxgbeConstants.IXGBE_PACKET_BUFFER_SIZE / 1024u), IxgbeRegField.TXPBTHRESH_THRESH, idx: 0);


            //		"- MTQC"
            //			"- Clear both RT_Ena and VT_Ena bits in the MTQC register."
            //			"- Set MTQC.NUM_TC_OR_Q to 00b."
            //			Section 8.2.3.9.15 Multiple Transmit Queues Command Register (MTQC):
            //				"RT_Ena, Init val 0b"
            //				"VT_Ena, Init val 0b"
            //				"NUM_TC_OR_Q, Init val 00b"
            // Thus we do not need to modify MTQC.
            //		"- DMA TX TCP Maximum Allowed Size Requests (DTXMXSZRQ) — set Max_byte_num_req = 0xFFF = 1 MB"
            IxgbeReg.DTXMXSZRQ.Write(newDevice.Addr, 0xFFF, IxgbeRegField.DTXMXSZRQ_MAX_BYTES_NUM_REQ);
            // INTERPRETATION-MISSING: Section 4.6.11.3 does not refer to MNGTXMAP, but since it's a management-related register we can ignore it here.
            //		"- Clear RTTDCS.ARBDIS to 0b"
            IxgbeReg.RTTDCS.Clear(newDevice.Addr, IxgbeRegField.RTTDCS_ARBDIS);
            // We've already done DCB/VT config earlier, no need to do anything here.
            // "- Enable interrupts (see Section 4.6.3.1)."
            // 	Section 4.6.3.1 Interrupts During Initialization "After initialization completes, a typical driver enables the desired interrupts by writing to the IMS register."
            // We don't need to do anything here by assumption NOWANT


            //just return the new instance without any mem allocation

            return newDevice;

        }

        /// <summary>
        /// ----------------------------
        /// Section 7.1.1.1 L2 Filtering
        /// ----------------------------
        /// </summary>
        /// <returns></returns>
        public bool SetPromiscuous()
        {
            // "A packet passes successfully through L2 Ethernet MAC address filtering if any of the following conditions are met:"
            // 	Section 8.2.3.7.1 Filter Control Register:
            // 	"Before receive filters are updated/modified the RXCTRL.RXEN bit should be set to 0b.
            // 	After the proper filters have been set the RXCTRL.RXEN bit can be set to 1b to re-enable the receiver."
            bool WasRxEnabled = !IxgbeReg.RXCTRL.Cleared(addr, IxgbeRegField.RXCTRL_RXEN);

            if (WasRxEnabled)
            {
                IxgbeReg.RXCTRL.Clear(addr, IxgbeRegField.RXCTRL_RXEN);
            }
            // "Unicast packet filtering — Promiscuous unicast filtering is enabled (FCTRL.UPE=1b) or the packet passes unicast MAC filters (host or manageability)."
            IxgbeReg.FCTRL.Set(addr, IxgbeRegField.FCTRL_UPE);
            // "Multicast packet filtering — Promiscuous multicast filtering is enabled by either the host or manageability (FCTRL.MPE=1b or MANC.MCST_PASS_L2 =1b) or the packet matches one of the multicast filters."
            IxgbeReg.FCTRL.Set(addr, IxgbeRegField.FCTRL_MPE);
            // "Broadcast packet filtering to host — Promiscuous multicast filtering is enabled (FCTRL.MPE=1b) or Broadcast Accept Mode is enabled (FCTRL.BAM = 1b)."
            // Nothing to do here, since we just enabled MPE

            if (WasRxEnabled)
            {
                IxgbeReg.RXCTRL.Set(addr, IxgbeRegField.RXCTRL_RXEN);
            }

            // This function cannot fail (for now?)
            return true;
        }

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
            for (int queue = 0; queue < IxgbeConstants.IXGBE_RECEIVE_QUEUES_COUNT; queue++)
            {
                // Section 4.6.7.1.2 [Dynamic] Disabling [of Receive Queues]
                // "Disable the queue by clearing the RXDCTL.ENABLE bit."
                IxgbeReg.RXDCTL.Clear(addr, IxgbeRegField.RXDCTL_ENABLE, queue);

                // "The 82599 clears the RXDCTL.ENABLE bit only after all pending memory accesses to the descriptor ring are done.
                //  The driver should poll this bit before releasing the memory allocated to this queue."
                // INTERPRETATION-MISSING: There is no mention of what to do if the 82599 never clears the bit; 1s seems like a decent timeout
                if (IxgbeConstants.TimeoutCondition(1000 * 1000, () => !IxgbeReg.RXDCTL.Cleared(addr, IxgbeRegField.RXDCTL_ENABLE, queue)))
                {
                    Util.log.Debug("RXDCTL.ENABLE did not clear, cannot disable receive");
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
            if (IxgbeConstants.TimeoutCondition(1000 * 1000, () => !IxgbeReg.STATUS.Cleared(addr, IxgbeRegField.STATUS_PCIE_MASTER_ENABLE_STATUS)))
            {
                // "In these cases, the driver should check that the Transaction Pending bit (bit 5) in the Device Status register in the PCI config space is clear before proceeding.
                //  In such cases the driver might need to initiate two consecutive software resets with a larger delay than 1 us between the two of them."
                // INTERPRETATION-MISSING: Might? Let's say this is a must, and that we assume the software resets work...
                if (!PciReg.PCI_DEVICESTATUS.Cleared(pciDevice, PciRegField.PCI_DEVICESTATUS_TRANSACTIONPENDING))
                {
                    Util.log.Debug("DEVICESTATUS.TRANSACTIONPENDING did not clear, cannot perform master disable");
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
