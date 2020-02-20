#include <stdlib.h>
#include <stdio.h>

#define _GNU_SOURCE  
#include <unistd.h>
#include <sys/syscall.h>

int main(int argc, const char *argv[]){
    printf("Hello\n");
    unsigned this_node = (unsigned) -1;
    int v = syscall(SYS_getcpu, NULL, &this_node, NULL);
    printf("this_node = %d\n", this_node);
}