#pragma once

#include "InternalCommon.h"
#include <string>

class CComPort
{
public:
	CComPort(ILog* aLogInterface);
	virtual ~CComPort();

public:
	void Open(const char* aPortName);
	void SetConfig(int aBaudrate, int aParity, int aStopBits);
	void Close();

	int Read(void* aBuffer, int aBytesToRead);
	int Write(const void* aBuffer, int aBytesToWrite);

	BOOL IsOpened()
	{
		return (_hcom != INVALID_HANDLE_VALUE);
	}
	static const int RX_BUFFER_SIZE = 1024 * 32;
	static const int TX_BUFFER_SIZE = 1024 * 64;
public:
	void Flush();
	void Purge();
private:
	HANDLE _hcom;
	ILog*   _ilog;
	std::string _portName;
};

