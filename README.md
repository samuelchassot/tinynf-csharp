# Sam - Semester project - Driver High Level Language

## Observations, notes
- will need to call libc things, especially to perform syscall or to work on the memory directly. C# can't access hw features so need to pass by C. But don't seem to loose advantages of HL languages, even taking that into account as we have them in other places.

- instead of using mmap in C, I will use *MemoryMappedFile* from .NET (https://docs.microsoft.com/en-us/dotnet/api/system.io.memorymappedfiles.memorymappedfile?view=netframework-4.8#remarks). With the method *CreateNew*, it can create a region in memory without mapping to a file, useful for interprocess communication. Exactly what I need.

- I need to allocate memory using MemoryMappedFile for the *transmitHeads* even if not the case in the C code, because using field doesn't ensure fixed position in memory.
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

### To compile for linux on another machine
```shell
dotnet build --runtime linux-x64
```
