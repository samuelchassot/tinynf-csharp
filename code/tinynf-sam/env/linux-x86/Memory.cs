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
        private const uint HUGEPAGE_SIZE_POWER = (10 + 10 + 1);
        private const ulong HUGEPAGE_SIZE = 1ul << (int)HUGEPAGE_SIZE_POWER;
        private readonly ulong SIZE_MAX = UIntPtr.Size == 64 ? 18446744073709551615UL : 4294967295UL;
        private Dictionary<UIntPtr, MemoryMappedFile> allocatedMemoryMappedFiles;

        public Memory()
        {
            allocatedMemoryMappedFiles = new Dictionary<UIntPtr, MemoryMappedFile>();
        }

        [DllImport("libc")]
        private static extern long sysconf(int name);

        [DllImport("FunctionsWrapper.so")]
        private static extern int get_sc_pagesize();

        /// <summary>
        /// Return the page size or 0 in case of an error
        /// </summary>
        /// <returns></returns>
        private static UIntPtr GetPageSize()
        {
            // sysconf is documented to return -1 on error; let's check all negative cases along the way, to make sure the conversion to unsigned is sound
            long pageSizeLong = sysconf(get_sc_pagesize());
            if (pageSizeLong > 0)
            {
                return (UIntPtr)pageSizeLong;
            }
            return (UIntPtr)0;
        }

        /// <summary>
        /// Allocates memory using MemoryMappedFile.CreateNew().
        /// </summary>
        /// <param name="size"> in bytes</param>
        /// <returns>The </returns>
        public unsafe UIntPtr MemAllocate(ulong size)
        {
            if(size > HUGEPAGE_SIZE)
            {
                Util.log.Debug("Tn_mem_allocated: size is bigger than HUGE_PAGESIZE");
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
                Util.log.Debug("Tn_mem_allocated: allocation failed");
                return UIntPtr.Zero;
            }
            if (mappedFile != null)
            {
                //here we can cast because size is smaller than HUGEPAGE_SIZE = 2^21
                var accessor = mappedFile.CreateViewAccessor(0, (long)size);
                byte* poke = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref poke);
                UIntPtr ptr = (UIntPtr)poke;
                (bool okGetAddr, ulong node) = Numa.NumaGetAddrNode(ptr);
                if (okGetAddr)
                {
                    if (Numa.NumaIsCurrentNode(node))
                    {
                        allocatedMemoryMappedFiles.Add(ptr, mappedFile);
                        //write a 0 in the page to force the loading into memory
                        *(byte*)ptr = 0;
                        return ptr;
                    }
                    else
                    {
                        Util.log.Debug("Allocated memory is not in our NUMA node");
                    }
                }
                else
                {
                    Util.log.Debug("Could not get memory's NUMA node");
                }
                mappedFile.Dispose();
            }

            
            return UIntPtr.Zero;
        }

        /// <summary>
        /// Dispose the MemoryMappedFile object's resources
        /// </summary>
        /// <param name="ptr"><param>
        public void MemFree(UIntPtr ptr)
        {
            MemoryMappedFile toFree;
            if (allocatedMemoryMappedFiles.TryGetValue(ptr, out toFree))
            {
                toFree.Dispose();
            }
            
            
        }

        [DllImport(@"FunctionsWrapper.so")]
        private static unsafe extern UIntPtr mem_phys_to_virt(UIntPtr addr, ulong size);
        /// <summary>
        /// Returns the virtual address corresponding to the given physical address
        /// returns UIntPtr.zero if it failed
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public unsafe UIntPtr MemPhysToVirt(UIntPtr addr, ulong size)
        {
            if(size > SIZE_MAX)
            {
                Util.log.Debug("Cannot mem_phys_to_virt with size bigger than SIZE_MAX");
                return UIntPtr.Zero;
            }
            if(addr != (UIntPtr) ((long)addr))
            {
                Util.log.Debug("mem_phys_to-virt: addr is to big to fit in a UIntPtr, exit");
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
            var ptr = Memory.mem_phys_to_virt(addr, size);
            //end of part in C
            if(ptr != (UIntPtr)((void*)-1))
            {
                return ptr;
            }
            Util.log.Debug("Error while mmap /dev/mem passing through C code, in Tn_mem_phys_to_virt");
            return UIntPtr.Zero;

        }

        [DllImport(@"FunctionsWrapper.so")]
        private static unsafe extern int mem_virt_to_phys(UIntPtr page, UIntPtr map_offset, ulong* out_metadata);

        public unsafe UIntPtr TnMemVirtToPhys(UIntPtr addr)
        {
            UIntPtr pageSize = GetPageSize();
            if(pageSize == (UIntPtr)0)
            {
                Util.log.Debug("Couldn't get page size, in mem_virt_to_phys");
                return UIntPtr.Zero;
            }

            UIntPtr nPage = (UIntPtr)((ulong)addr / (ulong)pageSize);
            UIntPtr offset = (UIntPtr)((ulong)nPage * sizeof(ulong));

            //use long to represent offset
            if(offset != (UIntPtr)((long)offset))
            {
                Util.log.Debug("the offset is to big to be represented as long, Tn_mem_virt_to_phys");
                return UIntPtr.Zero;
            }

            long pos = (long)offset;
            //x86 is little endian
            ulong metadata;
            int execCode = mem_virt_to_phys(nPage, offset, &metadata);
            if(execCode != 0)
            {
                switch (execCode)
                {
                    case 1:
                        Util.log.Debug("MemVirtToPhys: Could not open the pagemap");
                        break;
                    case 2:
                        Util.log.Debug("MemVirtToPhys: Could not seek the pagemap");
                        break;
                    case 3:
                        Util.log.Debug("MemVirtToPhys: Could not read the pagemap");
                        break;
                    default:
                        break;
                }

                return UIntPtr.Zero;
            }

            // We want the PFN, but it's only meaningful if the page is present; bit 63 indicates whether it is
            if ((metadata & 0x8000000000000000) == 0)
            {
                Util.log.Debug("page is not present, Tn_mem_virt_to_phys");
                return UIntPtr.Zero;
            }

            // PFN = bits 0-54
            ulong pfn = metadata & 0x7FFFFFFFFFFFFF;
            if (pfn == 0)
            {
                Util.log.Debug("Page not mapped, Tn_mem_virt_to_phys");
                return UIntPtr.Zero;
            }
            ulong addrOffset = (ulong)addr % (ulong)pageSize;
            return (UIntPtr)(pfn * (ulong)pageSize + addrOffset);


        }

    }
}
