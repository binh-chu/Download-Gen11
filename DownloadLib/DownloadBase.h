#pragma once

#include "InternalCommon.h"
#include "tinyxml2/tinyxml2.h"
#include "ComPort.h"

#include <stdio.h>

#define READ_RETRY_COUNT	20 // jwoh add GM Signing
#define READ_WAIT_TIME		100 // msec 

class CDownloadBase
{
public:
	CDownloadBase(IDownloadCallback* aCallback)
		: _callback(aCallback)
	{

	}
	virtual ~CDownloadBase()
	{ 
	}

	virtual void Initialize(CComPort* aPort, tinyxml2::XmlElement* aXmlProtocol)
	{
		_xml = aXmlProtocol;
		_port = aPort;
	}
	
	virtual void Download() = 0;

protected:
#define EXTRA_SAHARA	 0
#define EXTRA_FIREHOSE	 1
#define EXTRA_ALL		100

	// Interface functions
#define Logv( ...)					if (_callback != NULL) _callback->WriteLog(LOGVERBOSE, __VA_ARGS__);
#define Logi( ...)					if (_callback != NULL) _callback->WriteLog(LOGINFO, __VA_ARGS__);
#define Loge( ...)					if (_callback != NULL) _callback->WriteLog(LOGERROR, __VA_ARGS__);
#define Loga( ...)					if (_callback != NULL) _callback->WriteLog(LOGALWAYS, __VA_ARGS__);
#define Exception( ...)				if (_callback != NULL) _callback->InvokeException(__VA_ARGS__);
#define Progress(id, sent, total, extra)	if (_callback != NULL) _callback->ReportProgress(id, sent, total, extra);

#define SaharaProgress(id, sent, total)		Progress(id, sent, total, EXTRA_SAHARA)
#define FirehoseProgress(id, sent, total)	Progress(id, sent, total, EXTRA_FIREHOSE)
protected:
	int ReadRawData(LPSTR aAddr, UINT64 aLength)
	{
		int retry;
		int total = 0, read;
		char* p = aAddr;
		// read header
		for (retry = 0; retry < READ_RETRY_COUNT; retry++)
		{
			read = _port->Read(&p[total], (int)(aLength - total));
			if (read > 0)
			{
				total += read;
				return total;
			} // if
			::Sleep(READ_WAIT_TIME);
		} // for
		return total;
	}

	int ReadText(AutoMem& aMem)
	{
		int read = _port->Read(aMem.EndPtr(), aMem.remainedSize());

		if (read > 0)
			aMem.increaseCount(read);

		return read;
	}

	AutoMem ReadContents(HANDLE aFile, int aLength)
	{
		AutoMem pMem(aLength);

		DWORD read = 0;
		
		if (!ReadFile(aFile, pMem(), aLength, &read, NULL))
		{
			_callback->ReportGetLastError("[DownloadBase] File Read Error", 0, true);
		}

		pMem.setCount(read);
		return pMem;
	}

	int ReadContents(HANDLE aFile, AutoMem& aMem, int aLength = 0)
	{
		DWORD read = 0;

		int len = (aLength > 0) ? aLength : aMem.remainedSize();

		if (!ReadFile(aFile, aMem.EndPtr(), len, &read, NULL))
		{
			_callback->ReportGetLastError("[DownloadBase] File Read Error", 0, true);
		}

		aMem.increaseCount(read);

		return read;
	}

	void SendRawData(LPSTR aMem, int length)
	{
		_callback->WriteLog(LOGVERBOSE, "[DownloadBase] Send RAWDATA >>> : %d bytes", length);

		int sent = 0;

		while (sent < length)
		{
			sent += _port->Write(&aMem[sent], length - sent);
		}
	}

	int GetLogLevel()
	{
		return _callback->GetLogLevel();
	}

protected:
	tinyxml2::XmlElement* _xml;
	CComPort* _port;
	IDownloadCallback* _callback;
};