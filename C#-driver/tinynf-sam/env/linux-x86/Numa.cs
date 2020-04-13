using System;
using System.Runtime.InteropServices;
using Utilities;

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
            uint thisNode = uint.MaxValue;
            if(getcpu(null, &thisNode) != 0)
            {
                Util.log.Debug("NumaIsCurrentNode: cannot getCpu");
                return false;
            }
            //Util.log.Debug("NUMA: NumaIsCurrentNode:  this_node = " + thisNode + "    node in argument = " + node);
            return thisNode == (uint)node;
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
