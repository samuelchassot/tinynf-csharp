#include "FunctionsWrapper.h"

#include <sys/syscall.h>

#include <unistd.h>

//Most of the functions here are just wrapped because they cannot be called from C# directly

long get_mempolicy(int *mode, unsigned long *nodemask,
                   unsigned long maxnode, void *addr,
                   unsigned long flags)
{
    return syscall(SYS_get_mempolicy, mode, nodemask, maxnode, addr, flags);
}