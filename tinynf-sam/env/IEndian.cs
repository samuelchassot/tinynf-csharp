using System;
namespace env
{
    public interface IEndian
    {
        public UInt32 tn_cpu_to_le(UInt32 val);

        public UInt32 tn_le_to_cpu(UInt32 val);
    }
}
