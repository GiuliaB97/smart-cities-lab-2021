import io
import cv2 as cv
import time
import numpy as np
from picamera.array import PiRGBArray
from picamera import PiCamera
from gpiozero import LED

# ------------------------------------------------------------------------------------------
# ------------------------------------------------------------------------------------------

# PI SETUP

resolution = (320, 240)
frame_rate = 32
sleep_time = 3
q_char_code = 1048689

pin_led = 4 
led = LED(pin_led)
led.off()

# FACE DETECTION SETUP

face_cascade = cv.CascadeClassifier("haarcascade_frontalface_default.xml")
box_color = (0, 0, 255)
scale_factor = 1.1
n_neighbors = 6

# A method for finding faces
def find_faces(image, cascade_clf, box_color):
    gray_image = cv.cvtColor(image, cv.COLOR_BGR2GRAY)
    faces = face_cascade.detectMultiScale(gray_image, scale_factor, n_neighbors)
    bounding_boxes_image = np.copy(image)
    found_faces = False
    for (x, y, w, h) in faces:
        cv.rectangle(bounding_boxes_image, (x, y), (x+w, y+h), box_color, 2)
        found_faces = True
    return bounding_boxes_image, found_faces

# ------------------------------------------------------------------------------------------
# ------------------------------------------------------------------------------------------

print('***** Press the key "q" to exit *****')
time.sleep(sleep_time)

# CAMERA LOOP
with PiCamera() as camera:
    camera.resolution = resolution
    camera.framerate = frame_rate
    raw_capture = PiRGBArray(camera, size=resolution)
    for frame in camera.capture_continuous(raw_capture, format="bgr", use_video_port=True):
        image = frame.array
        image_with_selected_faces, face_found = find_faces(image, face_cascade, box_color)
        cv.imshow("Face Detection", image_with_selected_faces)
        key = cv.waitKey(1)
        # switch on/off the led
        if face_found:
            led.on()
        else:
            led.off()
        # check if 'q' is pressed
        if key == q_char_code:
            break
	raw_capture.truncate(0)
    raw_capture.close()


print("Goodbye...")



