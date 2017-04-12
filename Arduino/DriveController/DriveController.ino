#define d0 3
#define d1 4
#define p0 5
#define p1 6

int leftPWM = 0;
int rightPWM = 0;
int leftDir;
int rightDir;
String in;
int leftSpeed = 1500;
int rightSpeed = 1500;
bool toggle = false;

void setup() {
  pinMode(d0, OUTPUT);
  pinMode(p0, OUTPUT);
  pinMode(d1, OUTPUT);
  pinMode(p1, OUTPUT);
  Serial.begin(9600);
  while(!Serial){
  }
}

void loop() {
  int speed = 0;
  while (Serial.available() > 0){
   int in = Serial.read() - '0';
    speed *= 10;
    speed += in;
    Serial.println(speed);
    if (speed > 2000){
      leftSpeed = 1500;
      rightSpeed = 1500;
    }else if(speed >= 1000 && speed <= 2000){
      if (toggle){
        leftSpeed = speed;
        toggle = !toggle;
      } else {
        rightSpeed = speed;
        toggle = !toggle;
      }
      break;
    } else {
      continue;
    }  
  }
  if (leftSpeed >= 1500){
    leftPWM = map(leftSpeed, 1500, 2000, 0, 255);
    leftDir = 1;
  } else if (leftSpeed <= 1500){
    leftPWM = map(leftSpeed, 1500, 1000, 0, 255);
    leftDir = 0;
  }
  if (rightSpeed >= 1500){
    rightPWM = map(rightSpeed, 1500, 2000, 0, 255);
    rightDir = 1;
  } else if (rightSpeed <= 1500){
    rightPWM = map(rightSpeed, 1500, 1000, 0, 255);
    rightDir = 0;
  }
  digitalWrite(d0,leftDir);
  analogWrite(p0, leftPWM);
  digitalWrite(d1,rightDir);
  analogWrite(p1, rightPWM);
}

