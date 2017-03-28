/* This example demonstrates how to use interleaved mode to
take continuous range and ambient light measurements. The
datasheet recommends using interleaved mode instead of
running "range and ALS continuous modes simultaneously (i.e.
asynchronously)".

In order to attain a faster update rate (10 Hz), the max
convergence time for ranging and integration time for
ambient light measurement are reduced from the normally
recommended defaults. See section 2.4.4 ("Continuous mode
limits") and Table 6 ("Interleaved mode limits (10 Hz
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

#define DEBUG 0

/*
0 = front
1 = left
2 = right
[x,0] is distance
[x,1] is ambient 
*/
long distanceSensors[3][2];
VL6180X sensor[4];

void setup()
{
  pinMode(7,OUTPUT);
  pinMode(8,OUTPUT);
  pinMode(9,OUTPUT);
  digitalWrite(7,LOW);
  digitalWrite(8,LOW);
  digitalWrite(9,LOW);
  
  Wire.begin();
  
  digitalWrite(7,HIGH);
  delay(50);
  sensor[0].init();
  sensor[0].configureDefault();
  sensor[0].setTimeout(500);
  sensor[0].setAddress(0x30);
  sensor[0].writeReg(VL6180X::SYSRANGE__MAX_CONVERGENCE_TIME, 30);
  sensor[0].writeReg16Bit(VL6180X::SYSALS__INTEGRATION_PERIOD, 50);
  sensor[0].setTimeout(500);
  sensor[0].stopContinuous();
 

  digitalWrite(8,HIGH);
  delay(50);  
  sensor[1].init();
  sensor[1].configureDefault();
  delay(1000);
  sensor[1].setTimeout(500);
  sensor[1].setAddress(0x31);
  sensor[1].writeReg(VL6180X::SYSRANGE__MAX_CONVERGENCE_TIME, 30);
  sensor[1].writeReg16Bit(VL6180X::SYSALS__INTEGRATION_PERIOD, 50);
  sensor[1].stopContinuous();
  
  digitalWrite(9,HIGH);
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
  
}

void loop()
{
  while(!Serial);
 
  for(int i=0;i<3;i++)
  {
    distanceSensors[i][0] = sensor[i].readRangeContinuousMillimeters();
    if (sensor[i].timeoutOccurred()) { distanceSensors[i][0]=-1; }
    
    distanceSensors[i][1] = sensor[i].readAmbientContinuous();
    if (sensor[i].timeoutOccurred()) { distanceSensors[i][0]=-1; }
  }

    #if DEBUG == 0
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

    #else
    byte error;
    
    Wire.beginTransmission(0x30);
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
