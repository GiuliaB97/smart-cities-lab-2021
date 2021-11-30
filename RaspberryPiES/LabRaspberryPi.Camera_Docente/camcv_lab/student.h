#ifndef STUDENT_H_
#define STUDENT_H_

#include <stdio.h>
#include <stdlib.h>
extern "C"{
	#include "bcm_host.h"
	#include "interface/vcos/vcos.h"

	#include "interface/mmal/mmal.h"
	#include "interface/mmal/mmal_logging.h"
	#include "interface/mmal/mmal_buffer.h"
	#include "interface/mmal/util/mmal_util.h"
	#include "interface/mmal/util/mmal_util_params.h"
	#include "interface/mmal/util/mmal_default_components.h"
	#include "interface/mmal/util/mmal_connection.h"

	#include "RaspiCamControl.h"
	#include "RaspiPreview.h"
	#include "RaspiCLI.h"
}

int init_opencv(int* w, int* h, int* timeout);
void release_opencv(void);
void process_buffer(MMAL_BUFFER_HEADER_T *buffer, int w, int h, int mode);
int init_wiringPi();
void release_wiringPi(void);

 #endif