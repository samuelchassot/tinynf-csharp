cd tinynf-sam
dotnet build --runtime linux-x64 -c Release
cd ..

cp -r tinynf-sam/tinynf-sam/bin/Debug/netcoreapp3.1/linux-x64/ ./build

cd cwrapper
make
cd ..

cp cwrapper/FunctionsWrapper.so ./build/FunctionsWrapper.so
cp cwrapper/MacrosCstVal.so ./build/MacrosCstVal.so
