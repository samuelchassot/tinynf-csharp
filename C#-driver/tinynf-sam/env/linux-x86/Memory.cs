using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Utilities;

namespace Env.linuxx86
{
    public class Memory
    {
        private Logger log = new Logger(Constants.logLevel);
        private Dictionary<UIntPtr, MemoryMappedFile> allocatedMMF;

        private const uint HUGEPAGE_SIZE_POWER = (10 + 10 + 1);
        private const ulong HUGEPAGE_SIZE = 1ul << (int)HUGEPAGE_SIZE_POWER;
        private const uint MAP_HUGE_SHIFT = 26;
        private readonly ulong SIZE_MAX = UIntPtr.Size == 64 ? 18446744073709551615UL : 4294967295UL;

        public Memory()
        {
            allocatedMMF = new Dictionary<UIntPtr, MemoryMappedFile>();
        }

        [DllImport("libc")]
        private static extern long sysconf(int name);

        /// <summary>
        /// Return the page size or 0 in case of an error
        /// </summary>
        /// <returns></returns>
        private static UIntPtr GetPageSize()
        {
            // sysconf is documented to return -1 on error; let's check all negative cases along the way, to make sure the conversion to unsigned is sound
            long pageSizeLong = sysconf(MacrosValues._SC_PAGESIZE.GetValue());
            if (pageSizeLong > 0)
            {
                return (UIntPtr)pageSizeLong;
            }
            return (UIntPtr)0;
        }

        /// <summary>
        /// Allocates memory using MemoryMappedFile.CreateNew().
        /// </summary>
        /// <param name="size"></param>
        /// <returns>The </returns>
        public unsafe UIntPtr TnMemAllocate(ulong size)
        {
            if(size > HUGEPAGE_SIZE)
            {
                log.Debug("Tn_mem_allocated: size is bigger than HUGE_PAGESIZE");
                return UIntPtr.Zero;
            }

            MemoryMappedFile mappedFile;
            try
            {
                //the mapName must be null on non-Windows OS
                mappedFile = MemoryMappedFile.CreateNew(null, (long)size, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, System.IO.HandleInheritability.Inheritable);
            }
            catch (Exception)
            {
                log.Debug("Tn_mem_allocated: allocation failed");
                return UIntPtr.Zero;
            }
            if (mappedFile != null)
            {
                //here we can cast because size is smaller than HUGEPAGE_SIZE = 2^21
                var accessor = mappedFile.CreateViewAccessor(0, (long)size);
                byte* poke = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref poke);
                UIntPtr ptr = (UIntPtr)poke;
                ulong node;
                if (Numa.TnNumaGetAddrNode(ptr, &node))
                {
                    if (Numa.TnNumaIsCurrentNode(node))
                    {
                        allocatedMMF[ptr] = mappedFile;
                        return ptr;
                    }
                }
                mappedFile.Dispose();
            }
            return UIntPtr.Zero;
        }

        /// <summary>
        /// Dispose the MemoryMappedFile object's resources
        /// </summary>
        /// <param name="ptr"><param>
        public void TnMemFree(UIntPtr ptr)
        {
            var mmf = allocatedMMF[ptr];
            if(mmf != null)
            {
                try
                {
                    mmf.Dispose();
                }
                catch (Exception)
                {

                }
                allocatedMMF.Remove(ptr);
            }
            
        }

        [DllImport(@"FunctionsWrapper.so")]
        private static unsafe extern UIntPtr virt_to_phys_mem(UIntPtr addr, ulong size);
        /// <summary>
        /// Returns the virtual address corresponding to the given physical address
        /// returns UIntPtr.zero if it failed
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public unsafe UIntPtr TnMePhysToVirt(UIntPtr addr, ulong size)
        {
            if(size > SIZE_MAX)
            {
                log.Debug("Cannot mem_phys_to_virt with size bigger than SIZE_MAX");
                return UIntPtr.Zero;
            }
            if(addr != (UIntPtr) ((long)addr))
            {
                log.Debug("mem_phys_to-virt: addr is to big to fit in a UIntPtr, exit");
                return UIntPtr.Zero;
            }


            //THIS is the part that should work after the .net 5.0 update
            /// the way I implemented it won't work until .net core 5.0 release.
            /// It is a known issue: https://github.com/dotnet/runtime/issues/26626
            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
            ////size needs to be cast to long, as CreateFromFile takes a long
            //var mmf = MemoryMappedFile.CreateFromFile("/dev/mem", System.IO.FileMode.Open, null, (long)size, MemoryMappedFileAccess.ReadWrite);
            //if (mmf != null)
            //{
            //    using (var accessor = mmf.CreateViewAccessor())
            //    {
            //        byte* poke = null;
            //        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref poke);
            //        return (UIntPtr)poke;
            //    }
            //}
            //>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

            //So we just call same in C
            var ptr = Memory.virt_to_phys_mem(addr, size);
            if(ptr != (UIntPtr)((void*)-1))
            {
                return ptr;
            }
            log.Debug("Error while mmap /dev/mem passing through C code, in Tn_mem_phys_to_virt");
            return UIntPtr.Zero;

        }

        public unsafe UIntPtr TnMemVirtToPhys(UIntPtr addr)
        {
            UIntPtr pageSize = GetPageSize();
            if(pageSize == (UIntPtr)0)
            {
                log.Debug("Couldn't get page size, in mem_virt_to_phys");
                return UIntPtr.Zero;
            }

            UIntPtr nPage = (UIntPtr)((ulong)addr / (ulong)pageSize);
            UIntPtr offset = (UIntPtr)((ulong)nPage * (ulong)addr);

            //use int to represent offset
            if(offset != (UIntPtr)(int)offset)
            {
                log.Debug("the offset is to big to be represented as int, Tn_mem_virt_to_phys");
                return UIntPtr.Zero;
            }
            int required = sizeof(ulong);
            byte[] res = new byte[required];

            try
            {
                using (BinaryReader b = new BinaryReader(File.Open("/proc/self/pagemap", FileMode.Open)))
                {
                    int pos = (int)offset;
                    int count = 0;

                    b.BaseStream.Seek(pos, SeekOrigin.Begin);
                    while (count < required)
                    {
                        Console.WriteLine("count = " + count);
                        byte y = b.ReadByte();
                        res[count] = y;
                        pos++;
                        count++;
                    }
                    //x86 is little endian
                    ulong metadata = BitConverter.ToUInt64(res);

                    // We want the PFN, but it's only meaningful if the page is present; bit 63 indicates whether it is
                    if ((metadata & 0x8000000000000000) == 0)
                    {
                        log.Debug("page is not present, Tn_mem_virt_to_phys");
                        return UIntPtr.Zero;
                    }

                    // PFN = bits 0-54
                    ulong pfn = metadata & 0x7FFFFFFFFFFFFF;
                    if (pfn == 0)
                    {
                        log.Debug("Page not mapped, Tn_mem_virt_to_phys");
                        return UIntPtr.Zero;
                    }
                    ulong addrOffset = (ulong)addr % (ulong)pageSize;
                    return (UIntPtr)(pfn * (ulong)pageSize + addrOffset);

                }
            }
            catch (Exception ex)
            {
                log.Debug("Cannot read the /proc/self/pagemap, in Tn_mem_virt_to_phys\n" + ex.ToString());
                return UIntPtr.Zero;
            }


        }

    }
}
