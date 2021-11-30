/*
 * Copyright 2018 Ing. Luca Calderoni. All Rights Reserved.
 */

/**
 * @brief Invia rilevazioni di temperatura fittizie ogni 15 secondi
 *
 * I parametri di configurazione per la connessione al back end sono letti da aws_iot_config.h.
 * La pubblicazione delle rilevazioni fittizie di temperatura è fatta sul topic - "SCTM/AWSLAB/temperatura"
 * L'applicazione si registra al topic "SCTM/AWSLAB/temperatura" e al topic "SCTM/AWSLAB/alarm" per ricevere particolari notifiche inviate dal back end
 * Dopo aver ricevuto un allarme da tutti e quattro i dispositivi previsti (Device1, Device2, Device3, Device4), il dispositivo viene spento.
 *
 */

/**
 * @file aws_iot_core_lab.c
 */
#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include <unistd.h>
#include <limits.h>
#include <string.h>
#include <stdbool.h>

#include "aws_iot_config.h"
#include "aws_iot_log.h"
#include "aws_iot_version.h"
#include "aws_iot_mqtt_client_interface.h"

#define HOST_ADDRESS_SIZE 255
/**
 * @brief Certificate directory
 */
char certDirectory[PATH_MAX + 1] = "../../../certs";

/**
 * @brief MQTT HOST URL (aws_iot_config.h)
 */
char HostAddress[HOST_ADDRESS_SIZE] = AWS_IOT_MQTT_HOST;

/**
 * @brief MQTT port (aws_iot_config.h)
 */
uint32_t port = AWS_IOT_MQTT_PORT;

bool flag1 = false;
bool flag2 = false;
bool flag3 = false;
bool flag4 = false;

void iot_subscribe_callback_handler_temperatura(AWS_IoT_Client *pClient, char *topicName, uint16_t topicNameLen,
                                                                        IoT_Publish_Message_Params *params, void *pData) {
        IOT_UNUSED(pData);
        IOT_UNUSED(pClient);
        IOT_INFO("Subscribe callback");
        IOT_INFO("%.*s\t%.*s", topicNameLen, topicName, (int) params->payloadLen, (char *) params->payload);
}

void iot_subscribe_callback_handler_alarm(AWS_IoT_Client *pClient, char *topicName, uint16_t topicNameLen,
                                                                        IoT_Publish_Message_Params *params, void *pData) {
        IOT_UNUSED(pData);
        IOT_UNUSED(pClient);
        IOT_INFO("Subscribe callback");
        IOT_INFO("%.*s\t%.*s", topicNameLen, topicName, (int) params->payloadLen, (char *) params->payload);

        if(strstr((char *) params->payload, "Device1")){ if(!flag1) IOT_INFO("Ricevuto allarme da Device1"); flag1=true; }
        if(strstr((char *) params->payload, "Device2")){ if(!flag2) IOT_INFO("Ricevuto allarme da Device2"); flag2=true; }
        if(strstr((char *) params->payload, "Device3")){ if(!flag3) IOT_INFO("Ricevuto allarme da Device3"); flag3=true; }
        if(strstr((char *) params->payload, "Device4")){ if(!flag4) IOT_INFO("Ricevuto allarme da Device4"); flag4=true; }

        if(flag1 && flag2 && flag3 && flag4) system("sudo shutdown -h now");

}


void parseInputArgsForConnectParams(int argc, char **argv) {
        int opt;

        while(-1 != (opt = getopt(argc, argv, ""))) {
                switch(opt) {
                        default:
                                IOT_ERROR("Error in command line argument parsing");
                                break;
                }
        }

}

int main(int argc, char **argv) {
        bool infinitePublishFlag = true;

        char rootCA[PATH_MAX + 1];
        char clientCRT[PATH_MAX + 1];
        char clientKey[PATH_MAX + 1];
        char CurrentWD[PATH_MAX + 1];
        char cPayload[100];

        IoT_Error_t rc = FAILURE;

        AWS_IoT_Client client;
        IoT_Client_Init_Params mqttInitParams = iotClientInitParamsDefault;
        IoT_Client_Connect_Params connectParams = iotClientConnectParamsDefault;

        IoT_Publish_Message_Params paramsQOS0;

        parseInputArgsForConnectParams(argc, argv);

        IOT_INFO("\nAWS IoT SDK Version %d.%d.%d-%s\n", VERSION_MAJOR, VERSION_MINOR, VERSION_PATCH, VERSION_TAG);

        getcwd(CurrentWD, sizeof(CurrentWD));
        snprintf(rootCA, PATH_MAX + 1, "%s/%s/%s", CurrentWD, certDirectory, AWS_IOT_ROOT_CA_FILENAME);
        snprintf(clientCRT, PATH_MAX + 1, "%s/%s/%s", CurrentWD, certDirectory, AWS_IOT_CERTIFICATE_FILENAME);
        snprintf(clientKey, PATH_MAX + 1, "%s/%s/%s", CurrentWD, certDirectory, AWS_IOT_PRIVATE_KEY_FILENAME);

        IOT_DEBUG("rootCA %s", rootCA);
        IOT_DEBUG("clientCRT %s", clientCRT);
        IOT_DEBUG("clientKey %s", clientKey);
        mqttInitParams.enableAutoReconnect = false;
        mqttInitParams.pHostURL = HostAddress;
        mqttInitParams.port = port;
        mqttInitParams.pRootCALocation = rootCA;
        mqttInitParams.pDeviceCertLocation = clientCRT;
        mqttInitParams.pDevicePrivateKeyLocation = clientKey;
        mqttInitParams.mqttCommandTimeout_ms = 20000;
        mqttInitParams.tlsHandshakeTimeout_ms = 5000;
        mqttInitParams.isSSLHostnameVerify = true;

        rc = aws_iot_mqtt_init(&client, &mqttInitParams);
        if(SUCCESS != rc) {
                IOT_ERROR("aws_iot_mqtt_init returned error : %d ", rc);
                return rc;
        }

        connectParams.keepAliveIntervalInSec = 600;
        connectParams.isCleanSession = true;
        connectParams.MQTTVersion = MQTT_3_1_1;
        connectParams.pClientID = AWS_IOT_MQTT_CLIENT_ID;
        connectParams.clientIDLen = (uint16_t) strlen(AWS_IOT_MQTT_CLIENT_ID);
        connectParams.isWillMsgPresent = false;

        IOT_INFO("Connecting...");
        rc = aws_iot_mqtt_connect(&client, &connectParams);
        if(SUCCESS != rc) {
                IOT_ERROR("Error(%d) connecting to %s:%d", rc, mqttInitParams.pHostURL, mqttInitParams.port);
                return rc;
        }
        else{
                IOT_INFO("MQTT connectiion established");
        }

 	IOT_INFO("Subscribing to topic SCTM/AWSLAB/temperatura");
        rc = aws_iot_mqtt_subscribe(&client, "SCTM/AWSLAB/temperatura", 23, QOS0, iot_subscribe_callback_handler_temperatura, NULL);
        if(SUCCESS != rc) {
                IOT_ERROR("Error subscribing : %d ", rc);
                return rc;
        }

        IOT_INFO("Subscribing to topic SCTM/AWSLAB/alarm");
        rc = aws_iot_mqtt_subscribe(&client, "SCTM/AWSLAB/alarm", 17, QOS0, iot_subscribe_callback_handler_alarm, NULL);
        if(SUCCESS != rc) {
                IOT_ERROR("Error subscribing : %d ", rc);
                return rc;
        }


        sprintf(cPayload, "%s", "payload");

        paramsQOS0.qos = QOS0;
        paramsQOS0.payload = (void *) cPayload;
        paramsQOS0.isRetained = 0;

        int temperatura = 0;

	while(SUCCESS == rc) {

                //Max time the yield function will wait for read messages
                rc = aws_iot_mqtt_yield(&client, 100);

                IOT_INFO("-->sleep");
                sleep(15);

                IOT_INFO("Publishing to topic SCTM/AWSLAB/temperatura");
                int temperatura = rand() % 40;
                sprintf(cPayload, "{\"deviceId\": \"%s\",\"temperatura\": %d}", AWS_IOT_MQTT_CLIENT_ID, temperatura);
                paramsQOS0.payloadLen = strlen(cPayload);
                rc = aws_iot_mqtt_publish(&client, "SCTM/AWSLAB/temperatura", 23, &paramsQOS0);

        }

	return rc;
}

