#define d0 3
#define d1 4
#define p0 5
#define p1 6

// for the pro micro 5v

int leftPWM;
int rightPWM;
int leftDir;
int rightDir;

boolean newData = false;
const byte numChars = 32;
char receivedChars[numChars];
int channel = 0;
int value = 0;

void setup() {
  pinMode(d0, OUTPUT);
  pinMode(p0, OUTPUT);
  pinMode(d1, OUTPUT);
  pinMode(p1, OUTPUT);
  Serial.begin(9600);
  while(!Serial){
  }
}

void loop(){
  recvWithStartEndMarkers();
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
    parseInput();
}

void parseInput(){
  if (newData == true){
    channel = atoi(strtok(receivedChars,","));
    value = atoi(strtok(NULL,","));
    drive(channel, value);
  }
  newData = false;
}

void drive(int c, int s){
  if (c == 0){
    if (s >= 1500){
      rightPWM = map(s, 1500, 2000, 0, 255);
      rightDir = 1;
    } else {
      rightPWM = map(s, 1500, 1000, 0, 255);
      rightDir = 0;
    }
    digitalWrite(d0, leftDir);
    analogWrite(p0, leftPWM);
  }
  else if (c == 1){
    if (s >= 1500){
      leftPWM = map(s, 1500, 2000, 0, 255);
      leftDir = 1;
    } else {
      leftPWM = map(s, 1500, 1000, 0, 255);
      leftDir = 0;
    }
    digitalWrite(d1,rightDir);
    analogWrite(p1, rightPWM);
  }
}

