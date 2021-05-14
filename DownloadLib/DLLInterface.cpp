// DownloaderSahara.cpp : DLL 응용 프로그램을 위해 내보낸 함수를 정의합니다.
//

#include "InternalCommon.h"
#include "DLLInterface.h"

#include "Controller.h"
#include "DownloadBase.h"
#include "ProtocolException.h"
#include "DiagManager.h"

// windows error handling (SEH)
#include <eh.h> 

static void trans_func(unsigned int, EXCEPTION_POINTERS*);

class SehException
{
private:
	unsigned int nSE;
	PEXCEPTION_POINTERS     m_pExceptionPointers;
public:
	SehException() {}
	SehException(unsigned int n, PEXCEPTION_POINTERS  pExceptionPointers) : nSE(n), m_pExceptionPointers(pExceptionPointers) {}
	~SehException() {}
	unsigned int getSeNumber() const { return nSE; }
	PEXCEPTION_POINTERS getExceptionPointers() { return m_pExceptionPointers; }
};

void trans_func(unsigned int u, EXCEPTION_POINTERS* pExp)
{
	throw SehException(u, pExp);
}

static int  GetDllVersion(void)
{
	return DLL_DOWNLAODER_VERSION;
}

static int RunDownloader(const char* aPortName, const char* aXmlPath, fnLogHandler aLogHandler, fnDLHandler aProgressHandler)
{
	ULONGLONG start = GetTickCount64();
	_set_se_translator(trans_func);
	
	int result = 0;

	if (aLogHandler)
		aLogHandler(LOGVERBOSE, "Starting Gen11 Downloader... " CURRENT_VERSION_STRING " .!..");

	std::auto_ptr<CController> controller;
	try
	{
		controller.reset(new CController());

		controller->SetLogHandler(aLogHandler);
		controller->SetProgressHandler(aProgressHandler);

		controller->WriteLog(LOGVERBOSE, "Gen11 Downloader Input (PortName:%s, ConfigXml:%s)", aPortName, aXmlPath);

		controller->Initialize(aPortName);
		controller->RunDownload(aXmlPath);

		result = 1;

		controller->WriteLog(LOGALWAYS, "The download is complete, exit the top.");
	}
	catch (const CProtocolException& pe)
	{
		controller->WriteLog(LOGERROR, "The download is interrupted for the following reasons: [%s] ", pe.What());

		goto EXIT_FUN;
	}
	catch (const SehException& se)
	{
		controller->WriteLog(LOGERROR, "Windows Core dll exception then exit downloading. [ErrorCode:0x%X] ", se.getSeNumber());

		goto EXIT_FUN;
	}
	catch (const char* xe)
	{
		controller->WriteLog(LOGERROR, "The download is interrupted due to an exception. [%s]", xe);
	}
	catch (...)
	{
		controller->WriteLog(LOGERROR, "Is due to an unknown exception was interrupted downloads.");

		goto EXIT_FUN;
	}
		
EXIT_FUN:	
	ULONGLONG elapsed = GetTickCount64() - start;
	controller->WriteLog(LOGVERBOSE, "[Gen11-Downloader] Finished. (elapsed:%lld.%lld seconds)", elapsed / 1000, elapsed % 1000);

	if (aProgressHandler != null)
		aProgressHandler(-1, 0, result, EXTRA_ALL);
	controller.reset();

	return (result);
}


static int RunReadback(const char* aPortName, const char* aXmlPath, fnLogHandler aLogHandler, fnDLHandler aProgressHandler)
{
	ULONGLONG start = GetTickCount64();
	_set_se_translator(trans_func);

	int result = 0;

	if (aLogHandler)
		aLogHandler(LOGVERBOSE, "Gen11 Starting dump files... " CURRENT_VERSION_STRING " .!..");

	std::auto_ptr<CController> controller;
	try
	{
		controller.reset(new CController());

		controller->SetLogHandler(aLogHandler);
		controller->SetProgressHandler(aProgressHandler);

		controller->WriteLog(LOGVERBOSE, "Gen11 Readback Input (PortName:%s, ConfigXml:%s)", aPortName, aXmlPath);

		controller->Initialize(aPortName);
		controller->RunReadback(aXmlPath);

		result = 1;

		controller->WriteLog(LOGALWAYS, "The dump is complete, exit the top.");
	}
	catch (const CProtocolException& pe)
	{
		controller->WriteLog(LOGERROR, "The dump is interrupted for the following reasons: [%s] ", pe.What());

		goto EXIT_FUN;
	}
	catch (const SehException& se)
	{
		controller->WriteLog(LOGERROR, "Windows Core dll exception then exit dump. [ErrorCode:0x%X] ", se.getSeNumber());

		goto EXIT_FUN;
	}
	catch (const char* xe)
	{
		controller->WriteLog(LOGERROR, "The dump is interrupted due to an exception.. [%s]", xe);
	}
	catch (...)
	{
		controller->WriteLog(LOGERROR, "Is due to an unknown exception was interrupted dumps.");

		goto EXIT_FUN;
	}

EXIT_FUN:
	ULONGLONG elapsed = GetTickCount64() - start;
	controller->WriteLog(LOGVERBOSE, "[Gen11-Readback] Finished. (elapsed:%lld.%lld seconds)", elapsed / 1000, elapsed % 1000);

	if (aProgressHandler != null)
		aProgressHandler(-1, 0, result, EXTRA_ALL);
	controller.reset();

	return (result);
}

static int DiagRequest(const char* aReqCommand, char* aRespBuffer, int aBuffLength, const char* aPortName, fnLogHandler aLogHandler, int aLogLevel = LOGINFO)
{
	ULONGLONG start = GetTickCount64();
	_set_se_translator(trans_func);
	
	int result = 0;

	if (aLogHandler)
		aLogHandler(LOGVERBOSE, "Begin Gen11 Diagnostic... " CURRENT_VERSION_STRING " .!");

	std::auto_ptr<CDiagManager> manager;
	try
	{
		manager.reset(new CDiagManager());

		manager->SetLogHandler(aLogHandler);

		manager->WriteLog(LOGVERBOSE, "Gen11 Diag Port (PortName:%s)", aPortName);
		manager->WriteLog(LOGVERBOSE, "Gen11 Diag Request (Command:%s)", aReqCommand);

		manager->Initialize(aPortName, aLogLevel);
		result = manager->Execute(aReqCommand, aRespBuffer, aBuffLength);

		manager->WriteLog(LOGVERBOSE, "End Diagnostic.");
	}
	catch (const CProtocolException& pe)
	{
		manager->WriteLog(LOGINFO, "Diagnostics Exception: [%s] ", pe.What()); // jwoh change log level - error->info

		goto EXIT_FUN;
	}
	catch (const SehException& se)
	{
		manager->WriteLog(LOGERROR, "Windows Core dll exception then exit diagnostic. [ErrorCode:0x%X] ", se.getSeNumber());

		goto EXIT_FUN;
	}
	catch (const char* xe)
	{
		manager->WriteLog(LOGERROR, "The Diagnostic is interrupted due to an exception. [%s]", xe);
	}
	catch (...)
	{
		manager->WriteLog(LOGERROR, "Is due to an unknown exception was interrupted Diagnostic.");

		goto EXIT_FUN;
	}
		
EXIT_FUN:	
	ULONGLONG elapsed = GetTickCount64() - start;
	manager->WriteLog(LOGVERBOSE, "[Gen11-Diagnostic] Finished. (elapsed:%lld.%lld seconds)", elapsed / 1000, elapsed % 1000);
	manager.reset();

	return (result);
}


static ITarget sInterface = {
	GetDllVersion,
	RunDownloader,
	RunReadback,
	DiagRequest
};

//
// interface 지원 API 함수. (dll only call entry function)
//
void* GetTargetInterface()
{
	return &sInterface;
}
