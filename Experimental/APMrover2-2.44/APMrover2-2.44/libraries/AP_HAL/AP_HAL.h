
#ifndef __AP_HAL_H__
#define __AP_HAL_H__

#include <stdint.h>
#include <stdbool.h>

#include "AP_HAL_Namespace.h"
#include "AP_HAL_Boards.h"
#include "AP_HAL_Macros.h"

/*
HAL - Hardware Abstraction Layer (by Pat Hickey)

The AP_HAL directory itself defines the new API that abstracts out the differences between the various 
autopilot board types we support. Then we have a separate AP_HAL_$board directory for each board.  The 
code for the APM1 and APM2, which are based on AVR CPUs, is in AP_HAL_AVR, but you may notice there 
are some other board types in there as well. http://diydrones.com/profiles/blogs/lots-of-changes-to-apm-development

The HAL API replaces the Arduino specific calls which we used previously, and gives us a lot of flexibility 
to implement all the board specific functions in different ways for each board. Note that the port to PX4 doesn't 
mean we are abandoning the APM1 and APM2. All the APM code builds on all 3 platforms (plus the 'SITL' simulator platform), 
and our plan is to continue to support the APM1 and APM2 for as long as we can. What we expect to happen is 
that some new CPU and memory intensive features will only be enabled when you build for PX4, where we have so much more 
CPU and memory available.
*/

/* HAL Module Classes (all pure virtual) */
#include "UARTDriver.h"
#include "I2CDriver.h"
#include "SPIDriver.h"
#include "AnalogIn.h"
#include "Storage.h"
#include "GPIO.h"
#include "RCInput.h"
#include "RCOutput.h"
#include "Scheduler.h"
#include "Semaphores.h"
#include "Util.h"

#include "utility/Print.h"
#include "utility/Stream.h"
#include "utility/BetterStream.h"

/* HAL Class definition */
#include "HAL.h"

#endif // __AP_HAL_H__

