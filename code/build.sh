rm -rf build/*

cd tinynf-sam
dotnet clean -c Release --runtime linux-x64
dotnet clean -c Debug --runtime linux-x64

COMPlus_TieredCompilation=0 COMPlus_TC_QuickJit=0 dotnet build --runtime linux-x64 -c $1
cd ..

<<<<<<< HEAD
cp -r tinynf-sam/tinynf-sam/bin/$1/netcoreapp3.1/linux-x64/ ./build
=======
cp -r tinynf-sam/tinynf-sam/bin/Release/netcoreapp3.1/linux-x64/* ./build
>>>>>>> d08aa2f388a94c421780be9285e5689be01446f7

cd cwrapper
make
cd ..

cp cwrapper/FunctionsWrapper.so ./build/FunctionsWrapper.so
cp cwrapper/MacrosCstVal.so ./build/MacrosCstVal.so
