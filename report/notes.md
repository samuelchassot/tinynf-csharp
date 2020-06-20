# Notes about driver for report
- Tiered JIT: the compiler generates 2 versions of methods: 
    - one quick to launch but less optimized
    - one optimized but slower to launch
    when the code is ran, if a function is called more than, let's say, 20 times, the runtime switched the two version.

    It should not affect performance of the driver IMO, because after the heating up part, everything should be in "fast mode"

- Quick JIT

## Call to C
We need to call C to perform some actions:
- the value of ```_SC_PAGESIZE``` cannot be known in advance as it depends on the system. It is defined as a C macro so it is mandatory to call C to get its value. (https://www.man7.org/linux/man-pages/man3/sysconf.3.html)
- outb, outl, inl are defined as macros in C (x86 instructions directly), cannot be called in C# too
- ```mmap```: cannot use C# version because of reading 0 length files that is impossible (https://github.com/dotnet/runtime/issues/26626). Even .NET framework calls a C library to call ```mmap``` : https://github.com/dotnet/runtime/blob/master/src/libraries/Common/src/Interop/Unix/System.Native/Interop.MMap.cs#L33
- We can use C# to allocate memory though, using ```MemoryMappedFiles```. We need to write a dummy 0 in it to force the system to load the page in memory before trying to translate the address into physical (was donc in C by the flag ```MAP_POPULATE```)



## performance
For now, compiler optimize only for the projects *env* and *utilities*, it doesn't work if I activate them for *tinynf-sam*. In Release mode: all debug information is not generated.
- with Tiered JIT and Quick JIT activated (*true*): throughput = 3476
- with Tiered JIT and Quick JIT disabled (*false*): throughput = 3515

So changes but few.

By creating a new project to optimize some classes of tinynf-sam, I obtained:
- Everything optimized, only Program.cs, NetAgent.cs and NetDevice.cs not optimized: 4023
- Everything optimized, only Program.cs, NetAgent.cs not optimized: 3925 --> don't really know why it goes down there

### Using annotations
By using this annotation `x x  ` on:
All these are obtained by disabling Tiered Compilation and Quick JIT on all project
- `Main`, `Receive`, `Transmit` and `Process`: 5625
- `Receive`, `Transmit` and `Process`: 5976
- `Receive`, `Transmit`: 6484
- `Receive`: 12421
- `Process`: 12421
- `Transmit`: 6171
- If removed from Receive, doesn't work anymore, will look into it to find why. Seems to be the receiveMetadata & BitNLong(32) == 0 which is not working
- New observation: it works only if optimizations are disable for 1 of `Transmit`, `Receive` or `Process`. If `Transmit` is not optimized, it affects throughput by a factor 1/2. If either `Process`or `Receive` is not optimized, we obtain same throughput

#### Find out difference of performance due to Tiered Compilation and Quick JIT
Both are performed with `[MethodImpl(MethodImplOptions.NoOptimization)]` on `Receive` (doesn't help to enable both of them on this issue).
- Quick JIT: Disabled. Tiered compilation: Enabled: throughput = 12382
- Quick JIT: Enabled. Tiered compilation: Disabled: throughput = 12226
- Quick JIT: Enabled. Tiered compilation: Enabled: throughput = 12421
- Quick JIT: Disabled. Tiered compilation: Disabled: throughput = 12421

Conclusion: if enable both quick jit and tiered compilation, it doesn't change anything. Heatup seems to do its job here.

#### try to make opti works:
- If I move ```outputs``` as a field instead of a local variable and let the compiler optimize ```Process``` but not optimize ```Receive``` (where it is used), it doesn't work anymore. But it works if ```Process``` is not optimize but ```Receive``` is.
- Not optimizing ```Main``` doesn't change anything.
- Let opti everything but split ```Receive``` in two methods seems to work. (commit b752608e1fb209d96bb665ce2a148373fbba6921)
- With split ```Receive```, it doesn't seem to work if we do a tail call to the second method, which seems normal if compiler does tail call elimination.
### Debugging
Remote debugging doesn't work well.


#### Pointers vs ```Span<>```
Try to replace memory accesses done using pointers by ones using ```Span<>```

- With a ```Thread.MemoryBarrier()``` before and after, real slowdown in performance (loose about 2Gpbs on 12.6).
- Without any barriers, it works with a really small loss of performance (12460 vs 12539). Probably due to the index/length check performed by the ```Span``` (https://github.com/dotnet/runtime/blob/master/src/libraries/System.Private.CoreLib/src/System/Span.cs#L142)


With normal pointers accesses:
- removing ```Volatile.Read``` and ```Volatile.Write``` in registers read/write doesn't change anything in performance (12539Mbps with and without)

## Old notes

## Observations, notes
- will need to call libc things, especially to perform syscall or to work on the memory directly. C# can't access hw features so need to pass by C. But don't seem to loose advantages of HL languages, even taking that into account as we have them in other places.

- instead of using mmap in C, I will use *MemoryMappedFile* from .NET (https://docs.microsoft.com/en-us/dotnet/api/system.io.memorymappedfiles.memorymappedfile?view=netframework-4.8#remarks). With the method *CreateNew*, it can create a region in memory without mapping to a file, useful for interprocess communication. Exactly what I need.

- I need to allocate memory using MemoryMappedFile for the *transmitHeads* even if not the case in the C code, because using field doesn't ensure fixed position in memory.

### Debugging

## Instructions
- ```make``` the library in *cwrapper* folder
- copy the *CWrapper.so* in the same as the executable built by VS
- run the dotnet executable

## Useful links
- https://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=5&ved=2ahUKEwivjqn1w9vnAhWV7KYKHeRbDxUQFjAEegQIBhAB&url=https%3A%2F%2Fwww.jungo.com%2Fst%2Fsupport%2Fwindriver-technical-documents%2F&usg=AOvVaw01GNwyUtBJYrtfCVCvEL-z

- https://csharp.hotexamples.com/examples/-/PCI/-/php-pci-class-examples.html

- https://github.com/FlingOS/FlingOS

- https://guidedhacking.com/threads/using-syscalls-in-c.12164/

- https://developers.redhat.com/blog/2019/03/25/using-net-pinvoke-for-linux-system-functions/

- https://stackoverflow.com/questions/31179076/how-to-get-an-intptr-to-access-the-view-of-a-memorymappedfile (memorymappedfile and pointer)

## Working examples
### Working example that calls kill to kill a process

```c#
using System.Runtime.InteropServices;

namespace tinynf_sam
{
    class Program
    {
        [DllImport("libc", SetLastError = true)]
        public static extern int kill(int pid, int sig);

        static void Main(string[] args)
        {
            kill(1281, 6);
        }
    }
}
```