using System;
using System.Runtime.InteropServices;
using System.Linq;
using Env.linuxx86;
using Utilities;

namespace tinynf_sam
{
    class Program
    {
        private static Memory mem = new Memory();

        /// <summary>
        /// PCI addresses format: Bus:Device.Function
        /// </summary>
        /// <param name="args"></param>
        unsafe static int Main(string[] args)
        {
            ulong devicesCount = (ulong)args.Length;
            PCIDevice[] pCIDevices = new PCIDevice[2];
            if (devicesCount != 2)
            {
                Util.log.Info("Not two arguments passed, Failed");
                return 1;
            }
            //Parses PCI addresses in Bus:Device.Function format
            for (int i = 0; i < 2; ++i)
            {
                PCIDevice dev = parsePciDevice(args[i]);
                if (dev == null)
                {
                    return 1;
                }
                pCIDevices[i] = dev;
            }

            NetAgent[] agents = new NetAgent[2];
            NetDevice[] devices = new NetDevice[2];

            for (int n = 0; n < (int)devicesCount; n++)
            {
                try
                {
                    agents[n] = new NetAgent(mem);
                }
                catch (Exception e)
                {
                    Util.log.Info("Cannot initialize NetAgent n° " + n);
                    Util.log.Debug(e.ToString());
                    return 2 + 100 * n;
                }
                devices[n] = NetDevice.CreateInstance(mem, pCIDevices[n]);
                if(devices[n] == null)
                {
                    Util.log.Info("cannot initialize netDevice n° " + n);
                    return 3 + 100 * n;
                }
                if (!devices[n].SetPromiscuous())
                {
                    Util.log.Info("cannot make the netDevice n° " + n + " promiscuous");
                    return 4 + 100 * n;
                }
                if(!agents[n].SetInput(mem, devices[n]))
                {
                    Util.log.Info("cannot set input of netAgent n° " + n);
                    return 5 + 100 * n;
                }
                Util.log.Info("Initialized agent : " + n);
            }

            for (int n = 0; n < (int)devicesCount; n++)
            {
                if (!agents[n].AddOutput(mem, devices[1 - n], 0))
                {
                    Util.log.Info("Couldn't set agent TX");
                    return 6 + 100 * n;
                }
            }
            Util.log.Info("TinyNF initialized successfully!");


            while (true)
            {
                for (ulong p = 0; p < 2; p++)
                {
                    agents[p].Process((int packetLength, UIntPtr packetPtr, bool[] outputs) =>
                    {
                        outputs[0] = true;
                        return packetLength;
                    });
                }
            }

        }

        private static PCIDevice parsePciDevice(string addr)
        {
            string[] args = addr.Split(":");
            string busString = args[0];
            string[] deviceFuncstring = args[1].Split(".");
            string deviceString = deviceFuncstring[0];
            string functionString = deviceFuncstring[1];
            byte bus;
            byte device;
            byte function;

            try
            {
                bus = byte.Parse(busString, style: System.Globalization.NumberStyles.HexNumber);
                device = byte.Parse(deviceString, style: System.Globalization.NumberStyles.HexNumber);
                function = byte.Parse(functionString, style: System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception ex)
            {
                Util.log.Info("Cannot parse the PCI address given");
                Util.log.Debug(ex.ToString());
                return null;
            }
            return new PCIDevice(bus, device, function);


        }



    }

}