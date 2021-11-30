import io
import cv2 as cv
import time
import numpy as np
from picamera.array import PiRGBArray
from picamera import PiCamera
from threading import Thread
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

# A class that create a thread for streaming
class PiVideoStream:

    def __init__(self, resolution, frame_rate):
        self.camera = PiCamera()
        self.camera.resolution = resolution
        self.camera.framerate = frame_rate
        self.raw_capture = PiRGBArray(self.camera, size=resolution)
        self.stream = self.camera.capture_continuous(self.raw_capture, format="bgr", use_video_port=True)
        self.frame = None
        self.stop_capture = False

    # start the thread to read frames from the video stream
    def start(self):
        Thread(target=self.update, args=()).start()
        return self

    # thread loop
    def update(self):
        for captured_frame in self.stream:
            self.frame = captured_frame.array
            self.raw_capture.truncate(0)
            # check stop condition
            if self.stop_capture:
                self.stream.close()
                self.raw_capture.close()
                self.camera.close()
                return

    # return the frame most recently read
    def get_frame(self):
        return self.frame

    # stop the thread
    def stop(self):
        self.stop_capture = True

# ------------------------------------------------------------------------------------------
# ------------------------------------------------------------------------------------------

print('***** Press the key "q" to exit *****')

print("Start Streaming...")
video_stream = PiVideoStream(resolution, frame_rate).start()
time.sleep(sleep_time)
close_app = False

while close_app == False:
    image = video_stream.get_frame()
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
        close_app = True

video_stream.stop()

print("Goodbye...")



