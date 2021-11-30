#include <wiringPi.h>
#include <stdio.h>
#include <stdlib.h>
#include <signal.h>

int ChargingTime(int pin);
void PhotocellParsing(int reading);
void sighandler(int sig);

int PIN_fotoresistor = 7;
int PIN_STATE = 0;		// yellow led
int PIN_LOWLIGHT = 2;	// red led
int PIN_HIGHLIGHT = 3;  // green led

int main (void)
{
	int reading = 0;
	printf ("Raspberry Pi wiringPi Fotoresistor:\n");
	
	// Signal handler (CTRL-C)
	signal(SIGINT, sighandler);
	
	// Setup
	if(wiringPiSetup() < 0)
	{
		printf("Error in initialization WiringPi.\n");
		return -1;
	}
	// Fotoresistor
	pinMode(PIN_fotoresistor, OUTPUT);
	digitalWrite(PIN_fotoresistor, LOW);
	
	// Leds
	pinMode(PIN_STATE, OUTPUT);
	digitalWrite(PIN_STATE, HIGH);
	pinMode(PIN_LOWLIGHT, OUTPUT);
	digitalWrite(PIN_LOWLIGHT, LOW);
	pinMode(PIN_HIGHLIGHT, OUTPUT);
	digitalWrite(PIN_HIGHLIGHT, LOW);
	
	while (1) 
	{
		reading=ChargingTime(PIN_fotoresistor);
		PhotocellParsing(reading);
		printf("Reading: %d\n", reading);
	}
	return 0 ;
}

int ChargingTime(int pin)
{
	int reading=0;
	
	// Scarico condensatore
	pinMode(pin, OUTPUT);
	digitalWrite(pin, LOW);
	delay(100);	    // sleep 100 ms
	
	pinMode(pin, INPUT);
	while(digitalRead(pin) == LOW)
		reading++;
	return reading;
}

void PhotocellParsing(int reading)
{
	// Luce molto alta
	if(reading < 6000)
	{
		digitalWrite(PIN_LOWLIGHT, HIGH);
		digitalWrite(PIN_HIGHLIGHT, HIGH);
	}
	// Luce moderata
	else if(reading>6000 && reading<15000)
	{
		digitalWrite(PIN_LOWLIGHT, LOW);
		digitalWrite(PIN_HIGHLIGHT, HIGH);
	}
	// Luce bassa
	else if(reading>15000)
	{
		digitalWrite(PIN_LOWLIGHT, HIGH);
		digitalWrite(PIN_HIGHLIGHT, LOW);
	}
	else{}
}

void sighandler(int sig)
{
	digitalWrite(PIN_STATE, LOW);
	digitalWrite(PIN_LOWLIGHT, LOW);
	digitalWrite(PIN_HIGHLIGHT, LOW);
	exit(sig);
}

