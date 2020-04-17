rm -rf build/*

cd tinynf-sam
dotnet clean -c Release --runtime linux-x64
dotnet clean -c Debug --runtime linux-x64

COMPlus_TieredCompilation=0 COMPlus_TC_QuickJit=0 dotnet build --runtime linux-x64 -c Release
cd ..

cp -r tinynf-sam/tinynf-sam/bin/Release/netcoreapp3.1/linux-x64/* ./build

cd cwrapper
make
cd ..

cp cwrapper/FunctionsWrapper.so ./build/FunctionsWrapper.so
cp cwrapper/MacrosCstVal.so ./build/MacrosCstVal.so
