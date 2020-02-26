#include "MacrosCstVal.h"
#include <unistd.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/mman.h>

#include <sys/mman.h>

//The values of the id corresponding to each macro definited value are arbitrary and used to simplify the code in C# using an enum
int getSystemCstValues(int id){
    switch (id){
        case 1:
            return _SC_PAGESIZE;
            break;
        case 2:
            return PROT_READ;
            break;
        case 3:
            return PROT_WRITE;
            break;
        case 4:
            return MAP_HUGETLB;
            break;
        case 5:
            return MAP_ANONYMOUS;
            break;
        case 6:
            return MAP_SHARED;
            break;
        case 7:
            return MAP_POPULATE;
            break;
    }
}
