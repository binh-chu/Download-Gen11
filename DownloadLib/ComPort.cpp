#include "InternalCommon.h"
#include "ComPort.h"
#include "ProtocolException.h"

CComPort::CComPort(ILog * aLogInterface)
	: _ilog(aLogInterface)
{
	_hcom = INVALID_HANDLE_VALUE;
}

CComPort::~CComPort()
{ 
	Close();
}

void CComPort::Open(const char * aPortName)
{
	Close();
	// port open
	_portName = aPortName;
	if (_portName.find("\\\\.\\") != 0)
	{
		_portName.assign("\\\\.\\").append(aPortName);
	}
	_hcom = CreateFile(_portName.c_str(), GENERIC_READ | GENERIC_WRITE, 0, NULL, OPEN_EXISTING, 0, NULL);
	if (_hcom == INVALID_HANDLE_VALUE)
	{
		_ilog->ReportGetLastError("ComPort Open Failed", 0, true);
	}
	_ilog->WriteLog(LOGVERBOSE, "Port (%s), it has been opened successfully.", aPortName);
	// buffer size setting
	if (SetupComm(_hcom, RX_BUFFER_SIZE, TX_BUFFER_SIZE) == FALSE)
	{
		_ilog->ReportGetLastError("ComPort SetupComm Failed");
	}

	//Purge();

}

void CComPort::SetConfig(int aBaudrate, int aParity, int aStopBits)
{
	if (IsOpened())
	{
		// timeout setting
		COMMTIMEOUTS timeouts;
		timeouts.ReadIntervalTimeout = 0;
		timeouts.ReadTotalTimeoutMultiplier = 0;
		timeouts.ReadTotalTimeoutConstant = 200;
		timeouts.WriteTotalTimeoutMultiplier = 0;
		timeouts.WriteTotalTimeoutConstant = 1000;
		if(SetCommTimeouts(_hcom, &timeouts) == FALSE)
		{
			_ilog->ReportGetLastError("ComPort SetCommTimeouts Failed");
		}

		DCB dcb;
		if(GetCommState(_hcom, &dcb) == FALSE)
		{
			_ilog->ReportGetLastError("ComPort GetCommState Failed");
		}

		dcb.BaudRate = aBaudrate;
		dcb.Parity = aParity;
		dcb.fParity = aParity == 0 ? FALSE : TRUE;
		dcb.StopBits = aStopBits;
		if (SetCommState(_hcom, &dcb) == FALSE)
		{
			_ilog->ReportGetLastError("ComPort GetCommState Failed");
		}
	}
}

void CComPort::Close()
{
	if (IsOpened())
	{
		Flush();
		CloseHandle(_hcom);
		_hcom = INVALID_HANDLE_VALUE;
	}
}

int CComPort::Read(void * aBuffer, int aBytesToRead)
{
	if (!IsOpened())
		_ilog->InvokeException("COM Port is not opened..!");

	DWORD received = 0;
	if (ReadFile(_hcom, aBuffer, aBytesToRead, &received, NULL) == FALSE)
	{
		_ilog->ReportGetLastError("ComPort Read Failed.", 0, true);
	}
	else
	{
		_ilog->WriteLog(LOGVERBOSE, "[%s] RX<< %d bytes",  _portName.c_str(), received);
	}
	return (int)received;
}

int CComPort::Write(const void* aBuffer, int aBytesToWrite)
{
	if (!IsOpened())
		_ilog->InvokeException("COM Port is not opened..!");

	DWORD sent = 0, total = 0;
	const char* p = (const char*)aBuffer;
	int retry = 0;
	const int RETRY_WRITE = 10;	

	while ((int)total < aBytesToWrite && retry++ < RETRY_WRITE)
	{
		if (WriteFile(_hcom, &p[total], aBytesToWrite - total, &sent, NULL) == FALSE)
		{
			retry++;
			_ilog->ReportGetLastError("ComPort Write Failed");
			::Sleep(10);
		}
		else
		{
			total += sent;
			_ilog->WriteLog(LOGVERBOSE, "[%s] TX>> %d bytes", _portName.c_str(), sent);
		}
	}

	if (retry >= RETRY_WRITE)
		_ilog->InvokeException("[FIREHOSE] Send Failed !");
	
	if (total > 0)
		Flush();

	return (int)total;
}

void CComPort::Flush()
{
	if (IsOpened())
	{
		if (FlushFileBuffers(_hcom) == FALSE)
		{
			_ilog->ReportGetLastError("ComPort Flush Failed");
		}
	}
}

void CComPort::Purge()
{
	if (IsOpened())
	{
		if (PurgeComm(_hcom, PURGE_RXABORT | PURGE_RXCLEAR | PURGE_TXABORT | PURGE_TXCLEAR) == FALSE)
		{
			_ilog->ReportGetLastError("ComPort PurgeComm Failed");
		}
	}
}

