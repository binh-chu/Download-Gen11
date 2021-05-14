#pragma once

#include "DownloadBase.h"
#include "diag_def.h"

#include <string>

class CDiagProtocol
{
public:
	CDiagProtocol(ILog* aLogInterface);
	virtual ~CDiagProtocol(void);

	void SetPort(CComPort* aPort) { _port = aPort; }
public:
	static word CalculateCRC(const char* aBytes, int aCount);

	BOOL Request(std::string& aRequest, std::string& aResult);
	BOOL Receive(char aCmd, char aCmd2, std::string& aResult);

protected:
	void Encoding(std::string& aBytes, std::string& aOutput);
	void Decoding(const char* aRxBuf, int aRxCount, std::string& aOutput);
	BOOL CheckResult(char aCmd, char aCmd2, std::string& aOutput);
	bool CheckRxCRC(std::string& aRxBuffer);

private:
	ILog*   _ilog;
	CComPort* _port;
};
