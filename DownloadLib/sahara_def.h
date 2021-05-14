#pragma once

// sahara_def.h
// SAHARA PROTOCOL defintions

// Modes
typedef enum _TMode {
	SAHARA_MODE_IMAGE_TX_PENDING = 0x0,	//	Image Transfer Pending mode
	SAHARA_MODE_IMAGE_TX_COMPLETE = 0x1,	//	Image Transfer Complete mode
	SAHARA_MODE_MEMORY_DEBUG = 0x2,		//	Memory Debug mode
	SAHARA_MODE_COMMAND = 0x3,				// 	Command mode
	SAHARA_MODE_MAX = 4
} TMode;

// state machine
typedef enum _TState {
	UNKNOWN_STATE			= 0,
	WAIT_HELLO				= 1,
	WAIT_COMMAND			= 2,
	WAIT_DONE_RESP			= 3,
	WAIT_RESET_RESP			= 4,
	WAIT_MEMORY_TABLE		= 5,
	WAIT_MEMORY_REGION		= 6,
	WAIT_CMD_EXEC_RESP		= 7,
	WAIT_CMD_RESULT_RESP	= 8,
	STATE_DONE				= 100
} TState;

// PROTOCOL ID 
typedef enum _TProtocolID {
	UNKNOWN_ID				= 0, 
	HELLO					= 1,
	HELLO_RESP				= 2,
	READ_DATA_REQ			= 3,
	ENDOFIMAGE_TX			= 4,
	DONE_REQ				= 5,
	DONE_RESP				= 6,
	RESET_REQ				= 7,
	RESET_RESP				= 8,
	MEMORY_DEBUG			= 9,
	MEMORY_READ_REQ			= 10,
	COMMAND_READY			= 11,
	COMMAND_SWITCH_MODE		= 12,
	COMMAND_EXEC_REQ		= 13,
	COMMAND_EXEC_RESP		= 14,
	COMMAND_EXEC_DATA_REQ	= 15,
	MEMORY64_DEBUG			= 16,
	MEMORY64_READ_REQ		= 17,
	READ_DATA64_REQ			= 18,
	ID_MAX					= 19,
}TProtocolID;

typedef enum _TCommandID
{
	SAHARA_EXEC_CMD_NOP = 0x00,
	SAHARA_EXEC_CMD_SERIAL_NUM_READ = 0x01,
	SAHARA_EXEC_CMD_MSM_HW_ID_READ = 0x02,
	SAHARA_EXEC_CMD_OEM_PK_HASH_READ = 0x03,
	SAHARA_EXEC_CMD_SWITCH_DMSS = 0x04,
	SAHARA_EXEC_CMD_SWITCH_STREAMING = 0x05,
	SAHARA_EXEC_CMD_READ_DEBUG_DATA = 0x06,

	// place all new commands above this
	SAHARA_EXEC_CMD_LAST,
	SAHARA_EXEC_CMD_MAX = 0x7FFFFFFF
} TCommandID;

// protocol packet defintions
#pragma pack(4)
typedef struct SAHARA_PACKET_HEADER {
	UINT32	command;
	UINT32	length;
}S_HEADER;

typedef struct SAHARA_PACKET_HELLO_REQ {
	S_HEADER header;
	UINT32 version;                 // target protocol version number
	UINT32 version_supported;       // minimum protocol version number supported on target
	UINT32 cmd_packet_length;       // maximum packet size supported for command packets
	UINT32 mode;                    // expected mode of target operation
	UINT32 reserved0;               // reserved field
	UINT32 reserved1;               // reserved field
	UINT32 reserved2;               // reserved field
	UINT32 reserved3;               // reserved field
	UINT32 reserved4;               // reserved field
	UINT32 reserved5;               // reserved field 
}S_HELLO_REQ;

typedef struct SAHARA_PACKET_HELLO_RESP {
	S_HEADER header;
	UINT32 version;                 // host protocol version number
	UINT32 version_supported;       // minimum protocol version number supported on host
	UINT32 status;                  // OK or error condition
	UINT32 mode;                    // mode of operation for target to execute
	UINT32 reserved0;               // reserved field
	UINT32 reserved1;               // reserved field
	UINT32 reserved2;               // reserved field
	UINT32 reserved3;               // reserved field
	UINT32 reserved4;               // reserved field
	UINT32 reserved5;               // reserved field 
}S_HELLO_RESP;

typedef struct SAHARA_PACKET_READ_DATA_REQ {
	S_HEADER header;
	UINT32 image_id;                // ID of image to be transferred
	UINT32 data_offset;             // offset into image file to read data from
	UINT32 data_length;             // length of data segment to be retreived 
}S_READ_DATA_REQ;

typedef struct SAHARA_PACKET_ENDOFIMAGE_TX {
	S_HEADER header;
	UINT32 image_id;                // ID of image to be transferred
	UINT32 status;                  // OK or error condition 
}S_ENDOFIMAGE_TX;

typedef struct SAHARA_PACKET_DONE_REQ {
	S_HEADER header;
}S_DONE_REQ;

typedef struct SAHARA_PACKET_DONE_RESP {
	S_HEADER header;
	UINT32 image_tx_status;         // indicates if all images have been 
}S_DONE_RESP;

typedef struct SAHARA_PACKET_RESET_REQ {
	S_HEADER header;	
}S_RESET_REQ;

typedef struct SAHARA_PACKET_RESET_RESP {
	S_HEADER header;
} S_RESET_RESP;

typedef struct SAHARA_PACKET_MEMORY_DEBUG {
	S_HEADER header;
	UINT32 memory_table_addr;       // location of memory region table
	UINT32 memory_table_length;     // length of memory table 
}S_MEMORY_DEBUG;

typedef struct SAHARA_PACKET_MEMORY_READ_REQ {
	S_HEADER header;
	UINT32 memory_addr;             // memory location to read from
	UINT32 memory_length;           // length of data to send 
}S_MEMORY_READ_REQ;

typedef struct SAHARA_PACKET_COMMAND_READY {
	S_HEADER header;
}S_COMMAND_READY;

typedef struct SAHARA_PACKET_COMMAND_SWITCH_MODE {
	S_HEADER header;
	UINT32 mode;                    // mode of operation for target to execute 
}S_COMMAND_SWITCH_MODE;

typedef struct SAHARA_PACKET_COMMAND_EXEC_REQ {
	S_HEADER header;
	UINT32 client_command;          // command ID for target Sahara client to 
}S_COMMAND_EXEC_REQ;

typedef struct SAHARA_PACKET_COMMAND_EXEC_RESP {
	S_HEADER header;
	UINT32 client_command;          // command ID for target Sahara client to
	UINT32 resp_length;             // length of response returned from command 
}S_COMMAND_EXEC_RESP;

typedef struct SAHARA_PACKET_COMMAND_EXEC_DATA_REQ {
	S_HEADER header;
	UINT32 client_command;          // command ID for target Sahara client to 
}S_COMMAND_EXEC_DATA_REQ;

typedef struct SAHARA_PACKET_MEMORY64_DEBUG {
	S_HEADER header;
	UINT64 memory_table_addr;       // location of memory region table
	UINT64 memory_table_length;     // length of memory table 
}S_MEMORY64_DEBUG;

typedef struct SAHARA_PACKET_MEMORY64_READ_REQ {
	S_HEADER header;
	UINT64 memory_addr;             // memory location to read from
	UINT64 memory_length;           // length of data to send 
}S_MEMORY64_READ_REQ;

typedef struct SAHARA_PACKET_READ_DATA64_REQ {
	S_HEADER header;
	UINT64 image_id;                // ID of image to be transferred
	UINT64 data_offset;             // offset into image file to read data from
	UINT64 data_length;             // length of data segment to be retreived 
}S_READ_DATA64_REQ;

typedef union _TPacket {
	S_HEADER					header;
	S_HELLO_REQ					hello_req;
	S_HELLO_RESP				hello_resp;
	S_READ_DATA_REQ				read_data_req;
	S_ENDOFIMAGE_TX				endofimage_tx;
	S_DONE_REQ					done_req;
	S_DONE_RESP					done_resp;
	S_RESET_REQ					reset_req;
	S_RESET_RESP				reset_resp;
	S_MEMORY_DEBUG				memory_debug;
	S_MEMORY_READ_REQ			memory_read_req;
	S_COMMAND_READY				command_ready;
	S_COMMAND_SWITCH_MODE		switch_mode;
	S_COMMAND_EXEC_REQ			cmd_exec_req;
	S_COMMAND_EXEC_RESP			cmd_exec_resp;
	S_COMMAND_EXEC_DATA_REQ		cmd_exec_data_req;
	S_MEMORY64_DEBUG			memory64_debug;
	S_MEMORY64_READ_REQ			memory64_read_req;
	S_READ_DATA64_REQ			read_data64_req;
} TPacket;

#pragma pack()

// Status codes for Sahara
typedef enum _TErrorCode
{
	// Success
	ESAHARA_STATUS_SUCCESS = 0x00,

	// Invalid command received in current state
	ESAHARA_NAK_INVALID_CMD = 0x01,

	// Protocol mismatch between host and target
	ESAHARA_NAK_PROTOCOL_MISMATCH = 0x02,

	// Invalid target protocol version
	ESAHARA_NAK_INVALID_TARGET_PROTOCOL = 0x03,

	// Invalid host protocol version
	ESAHARA_NAK_INVALID_HOST_PROTOCOL = 0x04,

	// Invalid packet size received
	ESAHARA_NAK_INVALID_PACKET_SIZE = 0x05,

	// Unexpected image ID received
	ESAHARA_NAK_UNEXPECTED_IMAGE_ID = 0x06,

	// Invalid image header size received
	ESAHARA_NAK_INVALID_HEADER_SIZE = 0x07,

	// Invalid image data size received
	ESAHARA_NAK_INVALID_DATA_SIZE = 0x08,

	// Invalid image type received
	ESAHARA_NAK_INVALID_IMAGE_TYPE = 0x09,

	// Invalid tranmission length
	ESAHARA_NAK_INVALID_TX_LENGTH = 0x0A,

	// Invalid reception length
	ESAHARA_NAK_INVALID_RX_LENGTH = 0x0B,

	// General transmission or reception error
	ESAHARA_NAK_GENERAL_TX_RX_ERROR = 0x0C,

	// Error while transmitting READ_DATA packet
	ESAHARA_NAK_READ_DATA_ERROR = 0x0D,

	// Cannot receive specified number of program headers
	ESAHARA_NAK_UNSUPPORTED_NUM_PHDRS = 0x0E,

	// Invalid data length received for program headers
	ESAHARA_NAK_INVALID_PDHR_SIZE = 0x0F,

	// Multiple shared segments found in ELF image
	ESAHARA_NAK_MULTIPLE_SHARED_SEG = 0x10,

	// Uninitialized program header location
	ESAHARA_NAK_UNINIT_PHDR_LOC = 0x11,

	// Invalid destination address
	ESAHARA_NAK_INVALID_DEST_ADDR = 0x12,

	// Invalid data size receieved in image header
	ESAHARA_NAK_INVALID_IMG_HDR_DATA_SIZE = 0x13,

	// Invalid ELF header received
	ESAHARA_NAK_INVALID_ELF_HDR = 0x14,

	// Unknown host error received in HELLO_RESP
	ESAHARA_NAK_UNKNOWN_HOST_ERROR = 0x15,

	// Timeout while receiving data
	ESAHARA_NAK_TIMEOUT_RX = 0x16,

	// Timeout while transmitting data
	ESAHARA_NAK_TIMEOUT_TX = 0x17,

	// Invalid mode received from host
	ESAHARA_NAK_INVALID_HOST_MODE = 0x18,

	// Invalid memory read access
	ESAHARA_NAK_INVALID_MEMORY_READ = 0x19,

	// Host cannot handle read data size requested
	ESAHARA_NAK_INVALID_DATA_SIZE_REQUEST = 0x1A,

	// Memory debug not supported
	ESAHARA_NAK_MEMORY_DEBUG_NOT_SUPPORTED = 0x1B,

	// Invalid mode switch
	ESAHARA_NAK_INVALID_MODE_SWITCH = 0x1C,

	// Failed to execute command
	ESAHARA_NAK_CMD_EXEC_FAILURE = 0x1D,

	// Invalid parameter passed to command execution
	ESAHARA_NAK_EXEC_CMD_INVALID_PARAM = 0x1E,

	// Unsupported client command received
	ESAHARA_NAK_EXEC_CMD_UNSUPPORTED = 0x1F,

	// Invalid client command received for data response
	ESAHARA_NAK_EXEC_DATA_INVALID_CLIENT_CMD = 0x20,

	// Failed to authenticate hash table
	ESAHARA_NAK_HASH_TABLE_AUTH_FAILURE = 0x21,

	// Failed to verify hash for a given segment of ELF image
	ESAHARA_NAK_HASH_VERIFICATION_FAILURE = 0x22,

	// Failed to find hash table in ELF image
	ESAHARA_NAK_HASH_TABLE_NOT_FOUND = 0x23,

	// Place all new error codes above this
	ESAHARA_NAK_LAST_CODE,

	ESAHARA_NAK_MAX_CODE = 0x7FFFFFFF // To ensure 32-bits wide
} TErrorCode;
