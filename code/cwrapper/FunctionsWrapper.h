#include <sys/syscall.h>
#include <unistd.h>
#include <sys/mman.h>
#include <fcntl.h>
#include <unistd.h>
#include <stdint.h>
#include <sys/io.h>

#ifdef __cplusplus
extern "C"
{
#endif

#ifdef _WIN32
#ifdef MODULE_API_EXPORTS
#define MODULE_API __declspec(dllexport)
#else
#define MODULE_API __declspec(dllimport)
#endif
#else
#define MODULE_API
#endif

    MODULE_API long get_mempolicy(int *mode, unsigned long *nodemask,
                                  unsigned long maxnode, void *addr,
                                  unsigned long flags);

    //I omitted the last argument ( struct getcpu_cache *tcache ) because it is NULL anyway and I do not have the struct definition
    MODULE_API int get_cpu(unsigned *cpu, unsigned *node);
    MODULE_API uintptr_t virt_to_phys_mem(uintptr_t addr, unsigned long size);
    MODULE_API int mem_virt_to_phys(const uintptr_t page, const uintptr_t map_offset, uint64_t *outMetadata);
    MODULE_API void outl_custom(unsigned int value, unsigned short int port);
    MODULE_API void outb_custom(unsigned char value, unsigned short int port);
    MODULE_API unsigned int inl_custom(unsigned short int port);
    MODULE_API int get_sc_pagesize();
    MODULE_API uintptr_t mem_allocate(const uint64_t size, const uint64_t HUGEPAGE_SIZE, const int HUGEPAGE_SIZE_POWER);
    MODULE_API void mem_free(const uintptr_t addr, const uint64_t HUGEPAGE_SIZE);
#ifdef __cplusplus
}
#endif