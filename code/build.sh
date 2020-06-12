rm -rf build
mkdir build

cd tinynf-csharp
dotnet clean -c Release
dotnet clean -c Debug

dotnet build --runtime linux-x64 -c $1
cd ..

cp -r tinynf-csharp/tinynf-csharp/bin/$1/netcoreapp3.1/linux-x64/* ./build/

cd cwrapper
rm -f FunctionsWrapper.so
make
cd ..

cp cwrapper/FunctionsWrapper.so ./build/FunctionsWrapper.so
