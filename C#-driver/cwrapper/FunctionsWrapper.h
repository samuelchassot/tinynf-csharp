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

    MODULE_API int getcpu(unsigned *cpu, unsigned *node, struct getcpu_cache *tcache);

#ifdef __cplusplus
}
#endif