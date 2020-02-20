# Sam - Semester project - Driver High Level Language

## Observations, notes
- will need to call libc things, especially to perform syscall or to work on the memory directly. C# can't access hw features so need to pass by C. But don't seem to loose advantages of HL languages, even taking that into account as we have them in other places.

## Useful links
- https://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=5&ved=2ahUKEwivjqn1w9vnAhWV7KYKHeRbDxUQFjAEegQIBhAB&url=https%3A%2F%2Fwww.jungo.com%2Fst%2Fsupport%2Fwindriver-technical-documents%2F&usg=AOvVaw01GNwyUtBJYrtfCVCvEL-z

- https://csharp.hotexamples.com/examples/-/PCI/-/php-pci-class-examples.html

- https://github.com/FlingOS/FlingOS

- https://guidedhacking.com/threads/using-syscalls-in-c.12164/

- https://developers.redhat.com/blog/2019/03/25/using-net-pinvoke-for-linux-system-functions/

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