/* This uses interleaved mode to
take continuous range and ambient light measurements. The
datasheet recommends using interleaved mode instead of
running "range and ALS continuous modes simultaneously (i.e.
asynchronously)".

In order to attain a faster update rate (10 Hz), the max
convergence time for ranging and integration time for
ambient light measurement are reduced from the normally
recommended defaults. See section 2.5.2 ("Continuous mode
limits") and Table 9 ("Interleaved mode limits (10 Hz
operation)") in the VL6180X datasheet for more details.

Raw ambient light readings can be converted to units of lux
using the equation in datasheet section 2.13.4 ("ALS count
to lux conversion").

Example: A VL6180X gives an ambient light reading of 613
with the default gain of 1 and an integration period of
50 ms as configured in this sketch (reduced from 100 ms as
set by configureDefault()). With the factory calibrated
resolution of 0.32 lux/count, the light level is therefore
(0.32 * 613 * 100) / (1 * 50) or 392 lux.

The range readings are in units of mm. */

#include <Wire.h>
#include <VL6180X.h>
#include <SharpIR.h>

#define DEBUG 0

#define FRONTTOF 11
#define LEFTTOF 12
#define RIGHTTOF 10

#define FRONTSHARP A5
#define LEFTSHARP A4
#define RIGHTSHARP A3

#define LEDPIN 4
#define DARK 600

/*
0 = front
1 = left
2 = right
[x,0] is distance
[x,1] is ambient 
*/
long distanceSensors[3][2];
unsigned long timeOld;
unsigned long timeNew;
VL6180X sensor[4];
//sensor objects had to be declared like this because of constructor design
SharpIR sharpsensor[3] = {
  SharpIR(FRONTSHARP,1080),
  SharpIR(LEFTSHARP,1080),
  SharpIR(RIGHTSHARP,1080)
};

// 0 = off 1 = on
int lightAverage=0;
boolean lights;
char lightOverride = '0';
boolean dark;

void setup()
{

  //Start with the headlights off
  digitalWrite(LEDPIN,LOW);
  
//*** Sensor initialization ***//

  //Set the CE pins to output and turn the sensors off
  pinMode(FRONTTOF,OUTPUT);
  pinMode(LEFTTOF,OUTPUT);
  pinMode(RIGHTTOF,OUTPUT);
  digitalWrite(FRONTTOF,LOW);
  digitalWrite(LEFTTOF,LOW);
  digitalWrite(RIGHTTOF,LOW);

  //begin transmitting on the I2C bus
  Wire.begin();

  //enables the first sensor and configures it according to the library and manual
  digitalWrite(FRONTTOF,HIGH);
  delay(50);
  sensor[0].init();
  sensor[0].configureDefault();
  sensor[0].setTimeout(500);
  sensor[0].setAddress(0x30);
  sensor[0].writeReg(VL6180X::SYSRANGE__MAX_CONVERGENCE_TIME, 30);
  sensor[0].writeReg16Bit(VL6180X::SYSALS__INTEGRATION_PERIOD, 50);
  sensor[0].setTimeout(500);
  sensor[0].stopContinuous();
 

  digitalWrite(LEFTTOF,HIGH);
  delay(50);  
  sensor[1].init();
  sensor[1].configureDefault();
  delay(1000);
  sensor[1].setTimeout(500);
  sensor[1].setAddress(0x31);
  sensor[1].writeReg(VL6180X::SYSRANGE__MAX_CONVERGENCE_TIME, 30);
  sensor[1].writeReg16Bit(VL6180X::SYSALS__INTEGRATION_PERIOD, 50);
  sensor[1].stopContinuous();
  
  digitalWrite(RIGHTTOF,HIGH);
  delay(50);  
  sensor[2].init();
  sensor[2].configureDefault();
  delay(1000);
  sensor[2].setTimeout(500);
  sensor[2].setAddress(0x32);
  sensor[2].writeReg(VL6180X::SYSRANGE__MAX_CONVERGENCE_TIME, 30);
  sensor[2].writeReg16Bit(VL6180X::SYSALS__INTEGRATION_PERIOD, 50);
  sensor[2].stopContinuous();

  delay(300);
  sensor[0].startInterleavedContinuous(100);
  sensor[1].startInterleavedContinuous(100);
  sensor[2].startInterleavedContinuous(100);

  //get the current time in milliseconds
  timeOld=millis();
  
}

void loop()
{
  //Spin tires while there is no valid serial connection (gives the finicky sensors a rest).
  while(!Serial);

  //If not in debugging mode
  #if DEBUG == 0

  //get the current time in milliseconds
  timeNew=millis();

  //If it's been equal to or longer than the period of the refresh rate of the sensor (10MHz)
  if((timeNew-timeOld) >= 100)
  {
    //*** Read sensors ***//
    for(int i=0;i<3;i++)
    {
      //If the VL610X has malfunctioned, read the Sharp sensor (this is in centimeters so multiplied by 1000 to convert.)
      distanceSensors[i][0] = sensor[i].readRangeContinuousMillimeters();
      if (sensor[i].timeoutOccurred()) { distanceSensors[i][0]=(sharpsensor[i].distance()*1000); }
      
      distanceSensors[i][1] = sensor[i].readAmbientContinuous();
      if (sensor[i].timeoutOccurred()) { distanceSensors[i][0]=-1; }
      else if (i>0)
      {
        //Averaging the side light values
        lightAverage+=distanceSensors[i][1];
      }
    }

    //Getting the current average light level and turn the lights on if it is below the threshold.
    if ((lightAverage/2) < dark){
      lights = true;
    }
    lightAverage=0;
    
    //*** Print sensor readings ***//
    Serial.print(distanceSensors[0][0]);
    Serial.print (",");
    Serial.print(distanceSensors[1][0]);
    Serial.print (",");
    Serial.print(distanceSensors[2][0]);
    Serial.print (",");
    Serial.print(distanceSensors[0][1]);
    Serial.print (",");
    Serial.print(distanceSensors[1][1]);
    Serial.print (",");
    Serial.print(distanceSensors[2][1]);
    Serial.println();
  
    timeOld=timeNew;
  }

  //*** Headlight control  ***//
  
  //override checking
  if(Serial.available() > 0) //available()returns the number of bytes in the buffer
  {
    lightOverride = Serial.read();
  }

  //Overriding the state of the lights
  if(lightOverride == '1') {lights=true;}

  //Final set of the light state
  if(lights)
  {
    digitalWrite(LEDPIN,HIGH);
  }
  else
  {
    digitalWrite(LEDPIN,LOW);
  }
  
  //*** Debug I2C Scanner ***//
  #else
  byte error;

  //attempts to open communication with device at address 0x30
  Wire.beginTransmission(0x30);
  //ends the transmission and receives the status of the success of the connection
  error = Wire.endTransmission();
  if (error == 0)
  {
     Serial.print("I2C device found at address 30!");
  }
  
  Wire.beginTransmission(0x31);
  error = Wire.endTransmission();
  if (error == 0)
  {
     Serial.print("I2C device found at address 31!");
  }
  
  Wire.beginTransmission(0x32);
  error = Wire.endTransmission();
  if (error == 0)
  {
     Serial.print("I2C device found at address 32!");
  }
    
  #endif
}
