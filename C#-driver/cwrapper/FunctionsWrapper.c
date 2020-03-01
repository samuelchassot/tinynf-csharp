#include "FunctionsWrapper.h"

#include <sys/syscall.h>
#include <unistd.h>
#include <sys/mman.h>
#include <fcntl.h>
#include <unistd.h>
#include <stdint.h>
#include <sys/io.h>

//Most of the functions here are just wrapped because they cannot be called from C# directly

long get_mempolicy(int *mode, unsigned long *nodemask,
                   unsigned long maxnode, void *addr,
                   unsigned long flags)
{
    return syscall(SYS_get_mempolicy, mode, nodemask, maxnode, addr, flags);
}

int getcpu(unsigned *cpu, unsigned *node){
    return syscall(SYS_getcpu, cpu, node, NULL);
}

uintptr_t virt_to_phys_mem(uintptr_t addr, unsigned long size){
    //checks are already performed in C#
    int mem_fd = open("/dev/mem", O_SYNC | O_RDWR);
	if (mem_fd == -1) {
		return (uintptr_t) (void*)-1;
	}
    void* mapped = mmap(
		// No specific address
		NULL,
		// Size of the mapping (cast OK because we checked above)
		(size_t) size,
		// R/W page
		PROT_READ | PROT_WRITE,
		// Send updates to the underlying "file"
		MAP_SHARED,
		// /dev/mem
		mem_fd,
		// Offset is the address (cast OK because we checked above)
		(off_t) addr
	);
    //and checks will be performed in C#
    return (uintptr_t) mapped;
}

//need to define them like that because they are defined as macros so cannot be called directly from C#
unsigned int outlCustom(unsigned int value, unsigned short int port){
    return outl(value, port);
}

unsigned int outbCustom(unsigned char value, unsigned short int port){
    return outb(value, port);
}

unsigned int inlCustom(unsigned short int port){
    return inl(port);
}