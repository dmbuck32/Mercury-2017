#!/usr/bin/python3
import cv2
import numpy as np
import pickle
import socket
import sys
import time

UDP_IP = "127.0.0.1"
UDP_PORT = 8765
SHRUNK_WIDTH = 170
SHRUNK_HEIGHT = 127
CAMERA_FPS = 10

# Opening the front and rear camera
cap_front = cv2.VideoCapture(0)
cap_rear = cv2.VideoCapture(1)

# Adjust Camera FPS
cap_front.set(cv2.CAP_PROP_FPS, CAMERA_FPS)
cap_rear.set(cv2.CAP_PROP_FPS, CAMERA_FPS)

# Create socket
clientsocket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Info on how to close the client
print ('Be sure to close server before closing client')
print ('Press ctrl-c to close client')

# main loop
try:
	while True:
	#=================================================================
	# Front Camera
		if cap_front.isOpened():
			# Grab frame from front camera
			ret_front,frame_front = cap_front.read()
			
			# resize frame to a smaller size, uses INTER_AREA interpolation
			data_front = cv2.resize(frame_front, (SHRUNK_HEIGHT, SHRUNK_WIDTH), interpolation = cv2.INTER_AREA)
			
			# Create list of data with which camera its from and the frame info
			dataToSend_front = pickle.dumps([0, data_front])
			
			# Send packet
			clientsocket.sendto(dataToSend_front, (UDP_IP, UDP_PORT))
		
	#=================================================================
	
	#=================================================================
	# Rear Camera
		if cap_rear.isOpened():
			# Grab frame from rear camera
			ret_rear,frame_rear = cap_rear.read()
			
			# resize frame to a smaller size, uses INTER_AREA interpolation
			data_rear = cv2.resize(frame_rear, (SHRUNK_HEIGHT, SHRUNK_WIDTH), interpolation = cv2.INTER_AREA)
			
			# Create list of data with which camera its from and the frame info
			dataToSend_rear = pickle.dumps([1, data_rear])
			
			# Send Packet
			clientsocket.sendto(dataToSend_rear, (UDP_IP, UDP_PORT))
	#=================================================================
	
except KeyboardInterrupt:
	print('Closing the Client')
	print('Client Closed')
	pass
	
	