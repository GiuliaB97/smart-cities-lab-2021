#include <wiringPi.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#define MAXTIMINGS 85
#define pin_data 7

int data[5] = {0,0,0,0,0};

void read_dht11()
{
	uint8_t laststate = HIGH;
	uint8_t counter = 0;
	uint8_t j = 0, i;
	data[0] = data[1] = data[2] = data[3] = data[4] = 0;

	// Setup
	pinMode(pin_data, OUTPUT);
	digitalWrite(pin_data, LOW);
	delay(18);
	digitalWrite(pin_data, HIGH);
	delayMicroseconds(40); 
	pinMode(pin_data, INPUT);

	for ( i=0; i< MAXTIMINGS; i++) {
		// Attendo il cambiamento di stato
		counter = 0;
		while (digitalRead(pin_data) == laststate) {
			counter++;
			//delayMicroseconds(1); //Raspberry Pi 1
			delayMicroseconds(2); //Raspberry Pi 3
			if (counter == 255) break;
		}
		laststate = digitalRead(pin_data);
		if (counter == 255) break;
		
		// Ignoro le prime 3 transizioni che corrispondono a
		// 80micros low, 80 micros high, 50 micros low
		if ((i > 3) && (i%2 == 0)) {
			data[j/8] <<= 1;
			if (counter > 16)
				data[j/8] |= 1;
			j++;
		}
	}

	// Devo aver letto almeno 5 byte (40bit)
	// Verifico il checksum dei primi 4 byte con il quinto byte
	// Il sensore DHT11 non fornisce la parte decimale di temperatura e umidità
	if ((j >= 39) && (data[4] == ((data[0] + data[1] + data[2] + data[3]) & 0xFF)) ) 
		printf("Humidity = %d%% (+-4)   Temperature = %dC (+-2)\n", data[0], data[2]);
}

int main (void)
{
	printf ("Raspberry Pi wiringPi DHT11 Temperature test program:\n") ;
	wiringPiSetup();
	while (1) 
	{
		read_dht11();
		delay(4000);
	}
	return 0 ;
}
