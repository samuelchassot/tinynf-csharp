using System;
using System.Runtime.InteropServices;

namespace Env.linuxx86
{
    public class Numa
    {
        [DllImport("libc", SetLastError = true)]
        private unsafe static extern int getcpu(uint* cpu, uint* node, void* tcache);

        [DllImport("libc", SetLastError = true)]
        private unsafe static extern int get_mempolicy(int* mode, uint* nodemask,
                         uint maxnode, void* addr,
                         uint flags);

        public unsafe static bool Tn_numa_is_current_node(UInt64 node)
        {
            uint this_node = uint.MaxValue;
            if(getcpu(null, &this_node, null) != 0)
            {
                return false;
            }
            return this_node == (uint)node;
        }
        public unsafe static bool Tn_numa_get_addr_node(uint* addr, UInt64* out_node)
        {
            int node = -1;
            if(get_mempolicy(&node, null, 0, (void*)addr, 1 | 2) == 0)
            {
                *out_node = (UInt64)node;
                return true;
            }
            return false;
        }
    }
}
