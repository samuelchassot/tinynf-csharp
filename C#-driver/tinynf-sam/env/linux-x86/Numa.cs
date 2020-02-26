using System;
using System.Runtime.InteropServices;

namespace Env.linuxx86
{
    public class Numa
    {
        [DllImport(@"FunctionsWrapper.so")]
        private unsafe static extern int getcpu(uint* cpu, uint* node, void* tcache);

        [DllImport(@"FunctionsWrapper.so")]
        private unsafe static extern long get_mempolicy(int* mode, ulong* nodemask,
                         ulong maxnode, void* addr,
                         ulong flags);

        public unsafe static bool Tn_numa_is_current_node(UInt64 node)
        {
            uint this_node = uint.MaxValue;
            if(getcpu(null, &this_node, null) != 0)
            {
                return false;
            }
            return this_node == (uint)node;
        }
        public unsafe static bool Tn_numa_get_addr_node(UIntPtr addr, UInt64* out_node)
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
