#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <memory.h>
#include <wiringPi.h>
#include "student.h"

#include <cv.h>
#include <highgui.h>
#include "time.h"

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

#include <semaphore.h>

#include <iostream>
#include <fstream>
#include <sstream>
#include "opencv2/core/core.hpp"
#include "opencv2/contrib/contrib.hpp"
#include "opencv2/highgui/highgui.hpp"
#include "opencv2/imgproc/imgproc.hpp"
#include "opencv2/objdetect/objdetect.hpp"

using namespace cv;
using namespace std;
CascadeClassifier face_cascade; 
string fn_haar;
string fn_csv;
Mat gray,frame,face,face_resized;
vector<Mat> images;
vector<int> labels;
char key;
int im_width;				
int im_height;	
vector< Rect_<int> > faces;
IplImage *py, *pu, *pv, *pu_big, *pv_big, *image,* dstImage;


/* Inizializzo la libreria wiringPi per il lampeggio LED */
int PIN_LED = 0;
int init_wiringPi()
{
	if(wiringPiSetup()<0)
	{
		printf("Errore di inizializzazione wiringPi.\n");
		return -1;
	}
	pinMode(PIN_LED, OUTPUT);
	digitalWrite(PIN_LED, LOW);
	
	return 0;
}

/* Imposta il led a LOW */
void release_wiringPi(void)
{
	digitalWrite(PIN_LED, LOW);
}

/* Inizializzazione della libreria OpenCV e allocazioni */
int init_opencv(int* w, int* h, int* timeout)
{
	// Inizializzazione Detection
	fn_haar = "lbpcascade_frontalface.xml";
	if (!face_cascade.load(fn_haar))
   	{
		printf("Face cascade model not loaded.\n"); 
		return -1;
	}
	
	// Dimensioni immagine
	im_width = 320;		// usare un multiplo di 320 (640, 1280)
	im_height = 240;		// usare un multiplo di 240 (480, 960)
	*w = im_width;
	*h = im_height;
	
	// Tempo di acquisizione in millisecondi
	*timeout = 20000;
	
	cvNamedWindow("camcvWin", CV_WINDOW_AUTOSIZE); 
	dstImage = cvCreateImage(cvSize(im_width,im_height), IPL_DEPTH_8U, 3);
	py = cvCreateImage(cvSize(im_width,im_height), IPL_DEPTH_8U, 1);		// Y component of YUV I420 frame
	pu = cvCreateImage(cvSize(im_width/2,im_height/2), IPL_DEPTH_8U, 1);		// U component of YUV I420 frame
	pv = cvCreateImage(cvSize(im_width/2,im_height/2), IPL_DEPTH_8U, 1);		// V component of YUV I420 frame
	pu_big = cvCreateImage(cvSize(im_width,im_height), IPL_DEPTH_8U, 1);
	pv_big = cvCreateImage(cvSize(im_width,im_height), IPL_DEPTH_8U, 1);
	image = cvCreateImage(cvSize(im_width,im_height), IPL_DEPTH_8U, 3);		// immagine finale
	
	return 0;
}

/* Rilascio delle risorse di OpenCV */
void release_opencv()
{
	cvReleaseImage(&dstImage);
	cvReleaseImage(&pu);
	cvReleaseImage(&pv);
	cvReleaseImage(&py);
	cvReleaseImage(&pu_big);
	cvReleaseImage(&pv_big);
}

/* Funzione di processing eseguita su ogni frame del video 
    -Parametri 
    buffer: contiene i dati immagine forniti dalla camera nel formato YUV I420
    w: larghezza immagine
    h: larghezza immagine
    mode: modalità immagine (0=colori; 1=grayscale)
*/
void process_buffer(MMAL_BUFFER_HEADER_T *buffer, int w, int h, int mode)
{		
	// Serve per prendere i pixel di U e V (li abbiamo ogni 4)
	int h4=h/4;
		
	// Immagine grayscale (Y)
	memcpy(py->imageData,buffer->data,w*h);
		
	// Se sono in modalità colore
	if (mode==0)
	{	
		memcpy(pu->imageData,buffer->data+w*h,w*h4); 		// legge U
		memcpy(pv->imageData,buffer->data+w*h+w*h4,w*h4);      // legge V
	
		cvResize(pu, pu_big, CV_INTER_NN);			// devo ridimensionare e interpolare per ottenere i
		cvResize(pv, pv_big, CV_INTER_NN);  			// pixel mancanti. Uso il metodo CV_INTER_NN
		cvMerge(py, pu_big, pv_big, NULL, image);		// fondo le tre immagini per ottenere l'immagine a colori
	
		cvCvtColor(image,dstImage,CV_YCrCb2RGB);		// converto in RGB
		gray=cvarrToMat(dstImage); 
	}
	// Se sono in grayscale
	else
	{	
		gray=cvarrToMat(py); 						// metto l'immagine gryascale in una Mat
	}
		
	// Eseguo la Detection con il metodo detectMultiScale
	face_cascade.detectMultiScale(gray, faces, 1.1, 3, CV_HAAR_SCALE_IMAGE, Size(80,80));
	
	// Se c'è almeno un volto accendo il led
	if(faces.size() > 0)
		digitalWrite(PIN_LED, HIGH);
	else digitalWrite(PIN_LED, LOW);
	
	// Per ogni volto trovato
	for(int i = 0; i < faces.size(); i++) 
	{       
		// Prendo le coordinate del rettangolo del volto trovato 
		Rect face_i = faces[i];
		
		// Il rettangolo del volto lo evidenzio nell'immagine
		rectangle(gray, face_i, CV_RGB(255, 255 ,255), 1);
		
		
		//************************ ESTENSIONI ***************************************************************
		// Posso estrapolare il rettangolo del volto dalla Mat per altre elaborazioni
		//face = gray(face_i); 
		
		// Posso ridimensionare il volto per altre elaborazioni
		//cv::resize(face, face_resized, Size(im_width, im_height), 1.0, 1.0, CV_INTER_NN); //INTER_CUBIC);		
	
		//Con questo blocco di codice posso eventualmente mettere del testo sul volto trovato
		//int pos_x = std::max(face_i.tl().x - 10, 0);
		//int pos_y = std::max(face_i.tl().y - 10, 0);	
		//putText(gray, "", Point(pos_x, pos_y), FONT_HERSHEY_PLAIN, 1.0, CV_RGB(255,255,255), 1.0);	
	}
	
	
	imshow("camcvWin", gray);
	key = cvWaitKey(1);
		
}