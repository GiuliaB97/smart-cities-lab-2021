#include <stdio.h>
#include <stdlib.h>
#include <signal.h>
#include <wiringPi.h>
#include <pthread.h>

// Configurazione Hardware senza bottone
#define NO_PHYSICAL_BTN 

void registerSignalHandler();
void signalHandlerCtrlC(int sig);
void signalHandlerBackSlash(int sig);
void testLED(int pin, int time);
void buttonPressInterrupt(void);
PI_THREAD(pwmBlinkingThread);

const int INPUT_PIN = 0;
#ifdef NO_PHYSICAL_BTN
	const int OUTPUT_PIN = 4;
#endif
const int PIN_RED = 1;
const int PIN_YELLOW = 2;
const int PIN_GREEN = 3;
const int testInterval = 1000;
const int pwmInterval = 2;
const int blinkInterval = 500;

int statusYellowLed = LOW;
unsigned int threadID = 0;

int main()
{
	int threadCreationStatus = 0;
	registerSignalHandler();
	
	printf("Registering WiringPI: ");
	if(wiringPiSetup() < 0)
	{
		printf("ERROR\n");
		exit(1);
	}
	else
	{
		printf("SUCCESS\n");
	}
	
	pinMode(PIN_RED, OUTPUT);
	pinMode(PIN_YELLOW, OUTPUT);
	pinMode(PIN_GREEN, OUTPUT);
	#ifdef NO_PHYSICAL_BTN
		pinMode(OUTPUT_PIN, OUTPUT);
	#endif
	
	pinMode(PIN_RED, PWM_OUTPUT);
	
	printf("Registering interrupt: ");
	if ( wiringPiISR (INPUT_PIN, INT_EDGE_FALLING, &buttonPressInterrupt) < 0 ) 
	{
      	printf ("Unable to setup ISR\n");
      	exit(1);
  	}
	else
	{
		printf("SUCCESS\n");
	}
	
	piLock(0);
	printf("Creating blinking thread: ");
	threadCreationStatus = piThreadCreate(pwmBlinkingThread);
	if(threadCreationStatus != 0)
	{
		printf("It didn't start\n");
		piUnlock(0);
		exit(1);
	}
	else
	{
		piLock(0);
		printf("SUCCESS [threadID %u]\n", threadID);
		piUnlock(0);
	}
	
	while(1)
	{
		digitalWrite(PIN_GREEN, HIGH);
		delay(blinkInterval);
		digitalWrite(PIN_GREEN, LOW);
		delay(blinkInterval);
	}
		
	return 0;
}

void registerSignalHandler()
{
	// Per intercettare Ctrl+C
	signal(SIGINT, signalHandlerCtrlC);
	/* Per intercettare Ctrl+\ */
	signal(SIGQUIT, signalHandlerBackSlash);
}

void signalHandlerCtrlC(int sig)
{
	pinMode(PIN_RED, OUTPUT);
	pinMode(PIN_YELLOW, OUTPUT);
	pinMode(PIN_GREEN, OUTPUT);
	digitalWrite(PIN_RED, LOW);
	digitalWrite(PIN_YELLOW, LOW);
	digitalWrite(PIN_GREEN, LOW);
	#ifdef NO_PHYSICAL_BTN
		digitalWrite(OUTPUT_PIN, LOW);
	#endif
	printf("\rBye bye\n");
	exit(0);
}

void signalHandlerBackSlash(int sig)
{
	printf("\rPressed Ctrl-\\\n");
	#ifdef NO_PHYSICAL_BTN		
		digitalWrite(OUTPUT_PIN, HIGH);
		delay(1);
		digitalWrite(OUTPUT_PIN, LOW);
	#endif
}

void testLED(int pin, int time)
{
	digitalWrite(pin, HIGH);
	delay(time);
	digitalWrite(pin, LOW);
}

void buttonPressInterrupt(void)
{
	if(digitalRead(INPUT_PIN) == LOW)
	{
		if(statusYellowLed == LOW)
		{
			statusYellowLed = HIGH;
			digitalWrite(PIN_YELLOW, HIGH);
		}
		else
		{
			statusYellowLed = LOW;
			digitalWrite(PIN_YELLOW, LOW);
		}
	}
}

PI_THREAD(pwmBlinkingThread)
{
	int i = 0;
	pthread_t me = 0;
	
	threadID = me = pthread_self();
	piUnlock(0);
	
	while(1)
	{
		i = (i+1) % 1024;
		
		pwmWrite(PIN_RED, i);
		delay(pwmInterval);
	}
}
