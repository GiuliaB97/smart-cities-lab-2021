from libs.tempimage import TempImage
from picamera.array import PiRGBArray
from picamera import PiCamera
import argparse
import warnings
import datetime
import dropbox
import imutils
import json
import time
import cv2
import wiringpi

resize_width = 500
smoothingKernelSize = (21,21)
dilateIterations = 2

# Definizione argument parse e parsing degli argomenti
ap = argparse.ArgumentParser()
ap.add_argument("-c", "--conf", required=True,
	help="Percorso al file di configurazione JSON")
args = vars(ap.parse_args())

# Filtraggio warnings, caricamento configurazione ed inizializzazione 
# client dropbox
warnings.filterwarnings("ignore")
conf = json.load(open(args["conf"]))
client = None

# Controllo utilizzo dropbox
if conf["use_dropbox"]:
	# Connessione a dropbox ed avvio del relativo processo di autorizzazione
	client = dropbox.Dropbox(conf["db_access_token"])
	print("[SUCCESS] dropbox account collegato")

# Inizializzazione della camera
camera = PiCamera()
camera.resolution = tuple(conf["resolution"])
camera.framerate = conf["fps"]
rawCapture = PiRGBArray(camera, size=tuple(conf["resolution"]))

# Warmup della camera, definizione time stamp ultimo aggiornamento,
# inizializzazione motion conunter e frame medio 
print("[INFO] warmup camera...")
time.sleep(conf["camera_warmup_time"])
lastUploaded = datetime.datetime.now()
motionCounter = 0 
avg = None 

# Inizializzazione wiringPi e pin
wiringpi.wiringPiSetup()
wiringpi.pinMode(0,1)

# Cattura fotogrammi dalla camera
for f in camera.capture_continuous(rawCapture, format="bgr", use_video_port=True):
	# raccolta dei NumPy array grezzi rappresentanti l'immagine, 
	# inizializzazione del timestamp e della label stanza occupata/libera
	frame = f.array
	timestamp = datetime.datetime.now()
	text = "Libera"
	
	# ridimensionamento frame, conversione in grayscale e blurring
	frame = imutils.resize(frame, width=resize_width)
	gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY) 
	gray = cv2.GaussianBlur(gray, smoothingKernelSize, 0) 

	
	# se il frame medio e' None, inizializzazione di esso
	if avg is None:
		print("[INFO] definizione modello background...")
		avg = gray.copy().astype("float")
		rawCapture.truncate(0)
		continue
	
	# accumulo della media pesata tra il frame corrente ed i frame precedenti (definizion background model, variabile avg)
	# calcolo della differenza tra il frame corrente e la media corrente 	
	cv2.accumulateWeighted(gray, avg, 0.5) 
	frameDelta = cv2.absdiff(gray, cv2.convertScaleAbs(avg)) 

	# operazione di sogliatura sull'immagine delta seguita da dilatazione per
	# riempimento di eventuali holes e definizione dei contorni
	thresh = cv2.threshold(frameDelta, conf["delta_thresh"], 255,
		cv2.THRESH_BINARY)[1]
	thresh = cv2.dilate(thresh, None, iterations=dilateIterations) 
	cnts = cv2.findContours(thresh.copy(), cv2.RETR_EXTERNAL,
		cv2.CHAIN_APPROX_SIMPLE)
	cnts = imutils.grab_contours(cnts)

	# ciclo sui contorni determinati 
	for c in cnts:
		# se il contorno e' troppo piccolo viene scartato
		if cv2.contourArea(c) < conf["min_area"]:
			continue
		  
		# estrazione e successivo disegno del bounding box per il contorno 
		(x, y, w, h) = cv2.boundingRect(c)
		cv2.rectangle(frame, (x, y), (x + w, y + h), (0, 255, 0), 2)
		# aggiornamento label (i.e., se vi e' un contorno la stanza e' occupata)
		text = "Occupata"

	
	# stampa testo label e timestamp sul frame
	ts = timestamp.strftime("%A %d %B %Y %I:%M:%S%p")
	cv2.putText(frame, "Stato stanza: {}".format(text), (10, 20),
		cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 255), 2)
	cv2.putText(frame, ts, (10, frame.shape[0] - 10), cv2.FONT_HERSHEY_SIMPLEX,
		0.35, (0, 0, 255), 1)

	# gestione stanza occupata
	if text == "Occupata":
		# Controllo tempo trascorso tra differenti uploads
		if (timestamp - lastUploaded).seconds >= conf["min_upload_seconds"]:
			motionCounter += 1

			# controllo superamento soglia numero frames
			if motionCounter >= conf["min_motion_frames"]:
				# accensione led
				wiringpi.digitalWrite(0,1)
				# controllo utilizzo dropbox
				if conf["use_dropbox"]:
					# scrittura immagine in file temporaneo
					t = TempImage()
					cv2.imwrite(t.path, frame)
				
					# upload dropbox dell'immagine e cleanup dell'immagine temporanea
					print("[UPLOAD] {}".format(ts))
					path = "/{base_path}/{timestamp}.jpg".format(
					    base_path=conf["db_base_path"], timestamp=ts)
					client.files_upload(open(t.path, "rb").read(), path)
					t.cleanup()
				
				# aggiornamento timestamp di last upload e azzeramento motion counter
				lastUploaded = timestamp
				motionCounter = 0

	# gestione stanza Libera, il motion counter viene azzerato
	else:
		motionCounter = 0
		#spegnimento led
		wiringpi.digitalWrite(0,0)
		
	# gestione visualizzazione a schermo interfaccia
	if conf["show_video"]:
		# Mostra lo streaming video
		cv2.imshow("Motion detection", frame)
		key = cv2.waitKey(1) & 0xFF

		# pressione del tasto q permette l'uscita
		if key == ord("q"):
			wiringpi.pinMode(0,0)
			break
			
	rawCapture.truncate(0)
