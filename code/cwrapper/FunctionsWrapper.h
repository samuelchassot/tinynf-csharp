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
    MODULE_API int getcpu(unsigned *cpu, unsigned *node);

#ifdef __cplusplus
}
#endif