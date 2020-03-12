using System;
using System.Runtime.InteropServices;

namespace Env.linuxx86
{
    public class Numa
    {
        [DllImport(@"FunctionsWrapper.so")]
        private unsafe static extern int getcpu(uint* cpu, uint* node);

        [DllImport(@"FunctionsWrapper.so")]
        private unsafe static extern long get_mempolicy(int* mode, ulong* nodemask,
                         ulong maxnode, void* addr,
                         ulong flags);

        public unsafe static bool TnNumaIsCurrentNode(UInt64 node)
        {
            uint this_node = uint.MaxValue;
            if(getcpu(null, &this_node) != 0)
            {
                return false;
            }
            return this_node == (uint)node;
        }
        public unsafe static (bool, ulong) TnNumaGetAddrNode(UIntPtr addr)
        {
            int node = -1;
            ulong outNode = ulong.MaxValue;
            bool ok = false;
            if(get_mempolicy(&node, null, 0, (void*)addr, 1 | 2) == 0)
            {
                outNode = (ulong)node;
                ok = true;
            }
            return (ok, outNode);
        }
    }
}
