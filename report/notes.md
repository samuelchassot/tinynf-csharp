# Notes about driver for report
- Tiered JIT: the compiler generates 2 versions of methods: 
    - one quick to launch but less optimized
    - one optimized but slower to launch
    when the code is ran, if a function is called more than, let's say, 20 times, the runtime switched the two version.

    It should not affect performance of the driver IMO, because after the heating up part, everything should be in "fast mode"

- Quick JIT



## performance
For now, compiler optimize only for the projects *env* and *utilities*, it doesn't work if I activate them for *tinynf-sam*. In Release mode: all debug information is not generated.
- with Tiered JIT and Quick JIT activated (*true*): throughput = 3476
- with Tiered JIT and Quick JIT disabled (*false*): throughput = 3515

So changes but few.

By creating a new project to optimize some classes of tinynf-sam, I obtained:
- Everything optimized, only Program.cs, NetAgent.cs and NetDevice.cs not optimized: 4023
- Everything optimized, only Program.cs, NetAgent.cs not optimized: 3925 --> don't really know why it goes down there

### Using annotations
By using this annotation `[MethodImpl(MethodImplOptions.NoOptimization)]` on:
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
### Debugging
Remote debugging doesn't work well.