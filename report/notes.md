# Notes about driver for report
- Tiered JIT: the compiler generates 2 versions of methods: 
    - one quick to launch but less optimized
    - one optimized but slower to launch
    when the code is ran, if a function is called more than, let's say, 20 times, the runtime switched the two version.

    It should not affect performance of the driver IMO, because after the heating up part, everything should be in "fast mode"

- quick JIT
