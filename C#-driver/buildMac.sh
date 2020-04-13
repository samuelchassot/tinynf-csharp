git pull
cd tinynf-sam
dotnet build --runtime linux-x64
cd ..

cp cwrapper/FunctionsWrapper.so tinynf-sam/tinynf-sam/bin/Debug/netcoreapp3.1/linux-x64/FunctionsWrapper.so
cp cwrapper/MacrosCstVal.so tinynf-sam/tinynf-sam/bin/Debug/netcoreapp3.1/linux-x64/MacrosCstVal.so

cp -r tinynf-sam/tinynf-sam/bin/Debug/netcoreapp3.1/linux-x64/* ../tinynf-sam/code/