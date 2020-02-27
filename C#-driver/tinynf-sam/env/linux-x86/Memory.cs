using System;
using System.Collections.Generic;
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
        public unsafe UIntPtr Tn_mem_allocate(ulong size)
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
                mappedFile = MemoryMappedFile.CreateNew(null, (long)size, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, System.IO.HandleInheritability.None);
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
                UInt64 node;
                if (Numa.Tn_numa_get_addr_node(ptr, &node))
                {
                    if (Numa.Tn_numa_is_current_node(node))
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
        public void Tn_mem_free(UIntPtr ptr)
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

        public UIntPtr Tn_mem_phys_to_virt(UIntPtr addr, ulong size)
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

            var mmf = MemoryMappedFile.CreateFromFile("/dev/mem", System.IO.FileMode.Open, null, size, MemoryMappedFileAccess.ReadWrite);
        }

    }
}
