#pragma once
#include "ComPort.h"
#include "InternalCommon.h"
#include "Splitter.h"
#include "DiagProtocol.h"

class CDiagManager  : public ILog
{
public:
	CDiagManager(void);
	virtual ~CDiagManager(void);
	typedef enum
	{
		EUnknown = 0,
		EGet = '?',
		ESet = '=',
		ERun = '>'
	} TMethod;

	typedef enum
	{
		ESpPair = ']',
		ESpItem = '\r',
	} TItemSeperator;


	typedef BOOL (CDiagManager::*TDiagHandler)(TMethod aMethod, std::string& aCmd, strMap& aArgs, std::string& aResult);
	typedef std::map<std::string, TDiagHandler>  handlerMap;

#define DECLARE_HANDLER(name)	BOOL Handler_##name(TMethod aMethod, std::string& aCmd, strMap& aArgs, std::string& aResult)
#define DEFINE_HANDLER(name)	BOOL CDiagManager::Handler_##name(TMethod aMethod, std::string& aCmd, strMap& aArgs, std::string& aResult)
#define INSTALL_HANDLER(name)	_handlerList[#name] = &CDiagManager::Handler_##name

public:
	void SetLogHandler(fnLogHandler aLogHandler)
	{
		_logHandler = aLogHandler;
	}

	void Initialize(const char* aPortName, int aLogLevel = LOGINFO);

	BOOL Execute(const char* aReqCommand, char* aResultBuf, int aBufLen);
	void Parse(const char* aReqCommand, TMethod& aMethod, std::string& aCmd, strMap& aArgs);
private:
	// command handlers
	DECLARE_HANDLER(version);
	DECLARE_HANDLER(ereboot);
	DECLARE_HANDLER(nreboot);
	DECLARE_HANDLER(efsbackup);
	DECLARE_HANDLER(efsrecovery);
	DECLARE_HANDLER(micomup);
	DECLARE_HANDLER(chmode);

private:
	CComPort _port;	
	handlerMap _handlerList;
	CDiagProtocol _protocol;
};
