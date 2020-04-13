using System;
namespace Env
{
    public interface IEndian
    {
        public UInt32 TnCpuToLe(UInt32 val);

        public UInt32 TnLeToCpu(UInt32 val);
    }
}
