#include <stdlib.h>
#include <stdio.h>

#define _GNU_SOURCE  
#include <unistd.h>
#include <sys/syscall.h>
#include <fcntl.h>
#include <unistd.h>
#include <stdint.h>

int main(int argc, const char *argv[]){
    printf("Hello\n");
    const int map_fd = open("/proc/self/pagemap", O_RDONLY);
	if (map_fd < 0) {
		printf("%s", "error");
		return 0;
	}
    if (lseek(map_fd, (off_t) 1024, SEEK_SET) == (off_t) -1) {
		printf("%s", "error");
		close(map_fd);
		return 0;
	}
    uint64_t metadata;
	const ssize_t read_result = read(map_fd, &metadata, sizeof(uint64_t));
	close(map_fd);
    printf("%ld    %ld       ", read_result, sizeof(uint64_t));
    printf("%lu\n", metadata);
}