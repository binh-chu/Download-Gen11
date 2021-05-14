#pragma once

// firehose_def.h


typedef enum _TFbCommand {
	FB_NOTDEFINED	= 0,
	FB_NOP			= 1,
	FB_CONFIGURE	= 2,
	FB_PROGRAM		= 3,
	FB_PATCH		= 4,
	FB_READ			= 5,
	FB_PEEK			= 6,
	FB_POKE			= 7,
	FB_RESET		= 8,
	FB_SETDRIVE		= 9,
	FB_CREATEDRIVE	= 10,
	FB_STORAGEINFO	= 11
}TFbCommand;


#define CONF_DEFAULT_AlwaysValidate				0		// No VIP use.
#define CONF_DEFAULT_MaxDigestTableSizeInBytes  8192	// for digest table when used valiating(VIP)
#define CONF_WINDOWS_ZlpAwareHost				1		// Windows:1, Linux: 0