#include <stdio.h>
#include <string.h>
#include <errno.h>
#include <wiringPi.h>
#include<wiringSerial.h>

#define BAUD 9600

int main() {
    int fd;
    if ((fd=serialOpen("/dev/ttyACM0", BAUD)) < 0) {
	printf(“Unable to open serial device\n”);
	return 1;
    }

    if (wiringPiSetup() == -1) {
	printf(“WiringPi Error!\n”);
	return 1;
    }

 while(1) {
         while (serialDataAvail(fd)) {	        
	         printf("%c", serialGetchar(fd));
	         fflush(stdout);
	        }
    }
    printf("\n");
    return 0;
}
		
