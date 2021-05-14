// InternalCommon.h

#pragma once

#include "stdafx.h"
#include "AutoMem.h"
#include "DLLInterface.h"
#include "ProtocolException.h"
#include "AutoMem.h"

#include <stdio.h>
#include <stdarg.h>
#include <string>

#define null	0

#define LOGVERBOSE		3
#define LOGINFO			2
#define LOGERROR		1
#define LOGDISABLE		0
#define LOGALWAYS		-1
/// 라이브러리 내부에서 사용될 전역 함수들.
class ILog
{
public:
	virtual void WriteLog(int aLogLevel, const char * aFormat, ...)
	{
		if (_logHandler != NULL && _logLevel >= aLogLevel)
		{
			AutoMem tmp(1024 * 8);

			strcpy_s(tmp(), tmp.Size(), "Gen11/");
			tmp.setCount(strlen(tmp()));
			char* pbuf = tmp.EndPtr();

			va_list args;
			va_start(args, aFormat);
			
			vsnprintf_s(pbuf, tmp.remainedSize(), tmp.remainedSize() - 1, aFormat, args);

			_logHandler(aLogLevel, tmp());

			va_end(args);
		}
	}

	virtual void InvokeException(const char * aFormat, ...)
	{
		AutoMem tmp(1024 * 8);

		va_list args;
		va_start(args, aFormat);
		vsprintf_s(tmp(), tmp.Size(), aFormat, args);

		va_end(args);

		CProtocolException::Throw(tmp());
	}

	virtual void ReportGetLastError(const char* aMsg, DWORD aErrorCode = 0, bool aException = false)
	{
		LPVOID lpMsgBuf = null;
		if (aErrorCode == 0)
			aErrorCode = ::GetLastError();

		FormatMessage(
			FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM,
			NULL,
			aErrorCode,
			MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), // Default language
			(LPTSTR)&lpMsgBuf,
			0,
			null
		);

		if (aException)
			InvokeException("System Error: (%s) code:%d, errorMessage:%s", aMsg, aErrorCode, lpMsgBuf);
		else
			WriteLog(LOGERROR, "System Error: (%s) code:%d, errorMessage:%s", aMsg, aErrorCode, lpMsgBuf);

		if (lpMsgBuf != null)
			LocalFree(lpMsgBuf);
	}

	static void Print(std::string& str, const char * aFormat, ...)
	{
		AutoMem tmp(1024 * 8);

		va_list args;
		va_start(args, aFormat);
		vsprintf_s(tmp(), tmp.Size(), aFormat, args);

		va_end(args);

		str.append(tmp());
	}

	int GetLogLevel()
	{
		return _logLevel;
	}

	void SetLogHandler(fnLogHandler aLogHandler)
	{
		_logHandler = aLogHandler;
	}

	ILog*  GetILog()
	{	return this; }

protected:
	int _logLevel;
	fnLogHandler _logHandler;
};

class IDownloadCallback : public ILog
{
public:
	virtual void ReportProgress(int aImgID, UINT64 aSentBytes, UINT64 aTotalBytes, int aExtraInfo)
	{
		if (_progressHandler != NULL)
		{
			_progressHandler(aImgID, aSentBytes, aTotalBytes, aExtraInfo);
		}
	}

	void SetProgressHandler(fnDLHandler aProgressHandler)
	{
		_progressHandler = aProgressHandler;
	}
protected:
	fnDLHandler  _progressHandler;
};
