#!/usr/bin/python3
import socket
import sys
import cv2
import numpy as np
import struct
import platform
import pickle
import time

HOST = 'localhost' 
PORT = 8765
DATA_SIZE = 65535
SHRUNK_HEIGHT = 127
SHRUNK_WIDTH = 170
LARGE_WIDTH = 640
LARGE_HEIGHT = 480
connected = False
addr = None
data_front = None
first = True


data = ""
data_front = None

# Creates 2 named windows, one for the front camera and one for the rear camera
#cv2.namedWindow("Front Camera")
#cv2.namedWindow("Rear Camera")

# Sets socket default timeout
socket.setdefaulttimeout(1) 

# Creates a socket s, and displays a message that it has been created
s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
print ('Socket Created')

# Binds socket s to an ip address and port
s.bind((HOST, PORT))
print ('Socket Bind Complete')

# Text describing how to close the server
print ('Press esc to close stream')
print ('Please close server before closing the client')
	
# Main loop for the server
while True:
	
	try:
		# Recieve packet from Client
		data_front, addr = s.recvfrom(DATA_SIZE)
		connected = True
		if first:
			print("Connected")
			first = False
	except socket.timeout:
		print("Timed Out")
		first = True
		connected = False
	
	if connected:
		# Store packet into stored_data
		stored_data = pickle.loads(data_front)
		
		# Recieve Frame Info from packet
		frame_front = np.fromstring(stored_data[1], dtype = np.uint8)
		
		# Reshape data into proper picture size
		frame_front = frame_front.reshape(SHRUNK_WIDTH, SHRUNK_HEIGHT, 3)
		
		# Resize picture to be actually viewable, uses INTER_CUBIC interpolation
		frame_front = cv2.resize(frame_front, (LARGE_WIDTH, LARGE_HEIGHT), interpolation = cv2.INTER_CUBIC)
		
		#========================================================
		# This if block is used to flip the image on my tablet
		if platform.machine() == 'AMD64':
			frame_front = cv2.flip(frame_front,0,1)
		#========================================================
		
		# Decode which camera frame is from and display it to the proper window
		if stored_data[0] == 0:
			cv2.imshow("Front Camera", frame_front)
		else:
			cv2.imshow("Rear Camera", frame_front)
	
		# Sets data_front to null so checks can be made
		data_front = None
		addr = None
	
	# Checks if esc has been pressed and closes out program if it has
	if cv2.waitKey(5) == 27: 
		print ('Closing the Server')
		print ('Server Closed')
		break  # esc to quit