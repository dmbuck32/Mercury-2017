#include <PololuMaestro.h>

#include <SoftwareSerial.h>

SoftwareSerial maestroSerial(10, 11); // RX, TX

MiniMaestro maestro(maestroSerial);
  
byte LeftMotors = 0;
byte RightMotors = 1;
byte RearRightServo = 2;
byte RearLeftServo = 3;
byte FrontRightServo = 4;
byte FrontLeftServo = 5;
byte LOS_LED = 6;
byte ShoulderServo = 8;
byte ElbowServo = 9;
byte WristServo = 10;
byte GripperServo = 11;
boolean newData = false;
const byte numChars = 32;
char receivedChars[numChars];
int channel = 0;
int value = 0;

void setup() {
  Serial.begin(9600);
  while (!Serial){
  }
  maestroSerial.begin(9600);
  Serial.println("<Arduino is ready>");
}

void loop() {
  recvWithStartEndMarkers();
  parseInput();
  setServo(channel, value);
}

void recvWithStartEndMarkers() {
    static boolean recvInProgress = false;
    static byte ndx = 0;
    char startMarker = '<';
    char endMarker = '>';
    char rc;
 
    while (Serial.available() > 0 && newData == false) {
        rc = Serial.read();

        if (recvInProgress == true) {
            if (rc != endMarker) {
                receivedChars[ndx] = rc;
                ndx++;
                if (ndx >= numChars) {
                    ndx = numChars - 1;
                }
            }
            else {
                receivedChars[ndx] = '\0'; // terminate the string
                recvInProgress = false;
                ndx = 0;
                newData = true;
            }
        }

        else if (rc == startMarker) {
            recvInProgress = true;
        }
    }
}

void parseInput(){
  if (newData == true){
    channel = atoi(strtok(receivedChars,","));
    value = atoi(strtok(NULL,","));
  }
  newData = false;
}

void setServo(int channel, int value){
  Serial.println(channel);
  Serial.println(value);
  maestro.setTarget(channel, value * 4); 
}
