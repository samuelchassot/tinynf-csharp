#include "CWrapper.h"
#include <stdio.h>
#include <unistd.h>

//memory.c functions
int cst_sc_pagesize(){
    return _SC_PAGESIZE;
}
