#include <stdio.h>
#include <stdlib.h>
#include <sys/time.h>
#include <wiringPi.h>

int waitPinLevel(int pin, int level, int timeout);

int main(void)
{
	int elapsed = 0;
	double distance = 0;

	// wiringPi numbering
	int pin_echo = 2;
	int pin_trigger = 0;

	// Setup
	wiringPiSetup();
	pinMode(pin_echo, INPUT);
	pinMode(pin_trigger, OUTPUT);
	digitalWrite(pin_trigger, LOW);
	delay(500);

	for(;;)
	{
		// Invio un segnale al trigger di 10 microsecondi
		digitalWrite(pin_trigger, HIGH);
		delayMicroseconds(10);
		digitalWrite(pin_trigger, LOW);

		// Wait for echo goes HIGH
		waitPinLevel(pin_echo, HIGH, 5000);
		if (digitalRead(pin_echo) == HIGH)
		{
			// Attendo il ritorno (echo goes LOW)
			elapsed = waitPinLevel(pin_echo, LOW, 60000L);
			if (digitalRead(pin_echo) == LOW)
			{
				// Compute distance = HIGHtime * SpeedOfsound / 2
				// speedofsound = 340m/s = 0.034 cm/microsec
				distance = 0.034 * (double)elapsed / 2.0;
				if(elapsed > 38000)
					printf("Out of range\n");
				else printf("Distance: %.1f cm\n", distance);
			}
			else printf("Timeout\n");
		}

		//wait 1 seconds between measurements
		delay(1000);
	}
	return 0;
}

int waitPinLevel(int pin, int level, int timeout)
{
	struct timeval now, start;
	int done = 0;
	long micros = 0;
	long startuSec = 0, enduSec=0;

	gettimeofday(&start, NULL);
	startuSec = start.tv_sec * 1000000L + start.tv_usec;

	while (!done)
	{
		gettimeofday(&now, NULL);
		enduSec = now.tv_sec * 1000000L + now.tv_usec;
        	micros = enduSec - startuSec;
		if (digitalRead(pin) == level || micros > timeout) done = 1;
    	}
	return micros;
}
