#include "dht11.h"
#include "string.h"

dht11 DHT;
#define DHT11_PIN 3
#define BAUD 9600
#define DELAY_INTERVAL 2000

void setup(){
  Serial.begin(BAUD);
  Serial.println("DHT Test!");
  Serial.println(DHT11LIB_VERSION);
  Serial.println();
}

void loop(){
  int chk;
  chk = DHT.read(DHT11_PIN);    // Lettura dati
  switch (chk){
    case DHTLIB_OK:  
                Serial.print("Humidity: "); 
                Serial.print(DHT.humidity,DEC);
                Serial.print("%\t Temperature: ");
                Serial.print((float)DHT.temperature,2);
                Serial.print("*C");
                break;
    case DHTLIB_ERROR_CHECKSUM: 
                Serial.print("Checksum error,\t"); 
                break;
    case DHTLIB_ERROR_TIMEOUT: 
                Serial.print("Time out error,\t"); 
                break;
    default: 
                Serial.print("Unknown error,\t"); 
                break;
  }
  
  Serial.println();
  delay(DELAY_INTERVAL);
}

