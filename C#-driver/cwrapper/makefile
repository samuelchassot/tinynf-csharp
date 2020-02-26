all : MacrosCstVal FunctionsWrapper
MacrosCstVal: MacrosCstVal.c
	gcc -shared -o MacrosCstVal.so MacrosCstVal.c
FunctionsWrapper: FunctionsWrapper.c
	gcc -shared -o FunctionsWrapper.so FunctionsWrapper.c
