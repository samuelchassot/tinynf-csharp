﻿using System;
using System.Runtime.InteropServices;
using Utilities;

namespace env.linuxx86
{
    public class Memory
    {
        private const uint HUGEPAGE_SIZE_POWER = (10 + 10 + 1);
        private const ulong HUGEPAGE_SIZE = 1ul << (int)HUGEPAGE_SIZE_POWER;
        private const uint MAP_HUGE_SHIFT = 26;

        [DllImport("libc")]
        private static extern long sysconf(int name);

        //Chose to represent size_t by UIntPtr, which seems to be the corresponding type but without any guarantees given
        [DllImport("libc")]
        private static extern unsafe void* mmap(void* addr, UIntPtr length, int prot, int flags,
                  int fd, long offset);


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
        
        public static unsafe bool Tn_mem_allocate(ulong size, UIntPtr out_addr)
        {
            if(size > HUGEPAGE_SIZE)
            {
                return false;
            }
            return true;
        }

    }
}