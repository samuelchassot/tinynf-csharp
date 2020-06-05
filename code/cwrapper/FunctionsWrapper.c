#include "FunctionsWrapper.h"



#ifndef MAP_HUGE_SHIFT
#define MAP_HUGE_SHIFT 26
#endif

//Most of the functions here are just wrapped because they cannot be called from C# directly

long get_mempolicy(int *mode, unsigned long *nodemask,
				   unsigned long maxnode, void *addr,
				   unsigned long flags)
{
	return syscall(SYS_get_mempolicy, mode, nodemask, maxnode, addr, flags);
}

int get_cpu(unsigned *cpu, unsigned *node)
{
	return syscall(SYS_getcpu, cpu, node, NULL);
}

uintptr_t mem_phys_to_virt(uintptr_t addr, unsigned long size)
{
	//checks are already performed in C#
	int mem_fd = open("/dev/mem", O_SYNC | O_RDWR);
	if (mem_fd == -1)
	{
		return (uintptr_t)(void *)-1;
	}
	void *mapped = mmap(
		// No specific address
		NULL,
		// Size of the mapping (cast OK because we checked above)
		(size_t)size,
		// R/W page
		PROT_READ | PROT_WRITE,
		// Send updates to the underlying "file"
		MAP_SHARED,
		// /dev/mem
		mem_fd,
		// Offset is the address (cast OK because we checked above)
		(off_t)addr);
	//and checks will be performed in C#
	return (uintptr_t)mapped;
}

int mem_virt_to_phys(const uintptr_t page, const uintptr_t map_offset, uint64_t *out_metadata)
{

	const int map_fd = open("/proc/self/pagemap", O_RDONLY);
	if (map_fd < 0)
	{
		//TN_DEBUG("Could not open the pagemap");
		return 1;
	}

	if (lseek(map_fd, (off_t)map_offset, SEEK_SET) == (off_t)-1)
	{
		//TN_DEBUG("Could not seek the pagemap");
		close(map_fd);
		return 2;
	}

	uint64_t metadata;
	const ssize_t read_result = read(map_fd, &metadata, sizeof(uint64_t));
	close(map_fd);
	if (read_result != sizeof(uint64_t))
	{
		//TN_DEBUG("Could not read the pagemap");
		return 3;
	}
	*out_metadata = metadata;
	return 0;
}

//need to define them like that because they are defined as macros so cannot be called directly from C#
void outl_custom(unsigned int value, unsigned short int port)
{
	outl(value, port);
}

void outb_custom(unsigned char value, unsigned short int port)
{
	outb(value, port);
}

unsigned int inl_custom(unsigned short int port)
{
	return inl(port);
}

int get_sc_pagesize()
{
	return _SC_PAGESIZE;
}