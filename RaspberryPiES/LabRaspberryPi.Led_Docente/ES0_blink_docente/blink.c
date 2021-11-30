#include <stdio.h>
#include <stdlib.h>
#include <wiringPi.h>


int main(void)
{
	int pin = 0;

	wiringPiSetup();
	pinMode(pin, OUTPUT);
	for(;;)
	{
		digitalWrite(pin, HIGH);
		delay(500);
		digitalWrite(pin, LOW);
		delay(500);
	}
	return 0;
}
