cd tinynf-sam
dotnet build
cd ..

cd cwrapper
make
cd ..

cp cwrapper/FunctionsWrapper.so tinynf-sam/tinynf-sam/bin/Debug/netcoreapp3.1/FunctionsWrapper.so
cp cwrapper/MacrosCstVal.so tinynf-sam/tinynf-sam/bin/Debug/netcoreapp3.1/MacrosCstVal.so
