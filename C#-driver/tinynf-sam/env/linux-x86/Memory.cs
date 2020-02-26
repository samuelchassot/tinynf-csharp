using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Utilities;

namespace Env.linuxx86
{
    public class Memory
    {
        private static Logger log = new Logger(Constants.logLevel);

        private const uint HUGEPAGE_SIZE_POWER = (10 + 10 + 1);
        private const ulong HUGEPAGE_SIZE = 1ul << (int)HUGEPAGE_SIZE_POWER;
        private const uint MAP_HUGE_SHIFT = 26;

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
        /// Allocates memory using MemoryMappedFile.CreateNew(). Returns the pointer of this memory zone, UIntPtr.Zero if allocation failed.
        /// </summary>
        /// <param name="size"></param>
        /// <returns>The pointer to the newly allocated zone, or UIntPtr.Zero if allocation failed</returns>
        public static unsafe UIntPtr Tn_mem_allocate(ulong size)
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
                var accessor = mappedFile.CreateViewAccessor();
                byte* poke = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref poke);
                UIntPtr ptr = (UIntPtr)poke;
                UInt64 node;
                if (Numa.Tn_numa_get_addr_node(ptr, &node))
                {
                    if (Numa.Tn_numa_is_current_node(node))
                    {
                        return ptr;
                    }
                }
            }
            return UIntPtr.Zero;
            

        }

    }
}
