rm -rf build
mkdir build

cd tinynf-sam
dotnet clean -c Release
dotnet clean -c Debug

dotnet build --runtime linux-x64 -c $1
cd ..

cp -r tinynf-sam/tinynf-sam/bin/$1/netcoreapp3.1/linux-x64/* ./build/

cd cwrapper
make
cd ..

cp cwrapper/FunctionsWrapper.so ./build/FunctionsWrapper.so
