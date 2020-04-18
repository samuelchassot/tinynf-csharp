rm -rf build/*

cd tinynf-sam
dotnet clean -c Release --runtime linux-x64
dotnet clean -c Debug --runtime linux-x64

dotnet build --runtime linux-x64 --no-restore --no-incremental -c $1
cd ..

cp -r tinynf-sam/tinynf-sam/bin/$1/netcoreapp3.1/linux-x64/* ./build/

cd cwrapper
make
cd ..

cp cwrapper/FunctionsWrapper.so ./build/FunctionsWrapper.so
cp cwrapper/MacrosCstVal.so ./build/MacrosCstVal.so
