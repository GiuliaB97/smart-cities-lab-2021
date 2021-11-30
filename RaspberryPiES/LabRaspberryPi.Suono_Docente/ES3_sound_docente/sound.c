#include <wiringPi.h>
#include <stdio.h>
#include <stdlib.h>
#include <gertboard.h>

int soundSensorChannel=0;
int soundReading=0;
int soundThreshold=673; 
double v1;

int main (void)
{
	printf ("Raspberry Pi wiringPi Sound Sensor:\n");
	
	// Setup
	if(wiringPiSetup() < 0)
	{
		printf("Error in initialization WiringPi.\n");
		return -1;
	}
	
	if(gertboardSPISetup() < 0)
	{
		printf("Error in initialization SPI bus.\n");
		return -1;
	}
	
	while (1) 
	{
		// Nella gertboard uso l'analog-to-digital converter (ADC) per 
		// prendere il valore della sensore sonoro
		// Esistono 2 canali: AD0 e AD1 io uso AD0
		soundReading = gertboardAnalogRead(soundSensorChannel);
		if(soundReading<soundThreshold)
		{
			v1 = (double)soundReading / 1023.0 * 3.3 ;
			printf("Value: %d (volt: %6.3f)\n", soundReading, v1);
		}
	}
	return 0 ;
}