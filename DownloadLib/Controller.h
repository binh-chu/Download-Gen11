#pragma once
#include "ComPort.h"
#include <map>
#include <string>

#define IMG_SAHARA		0
#define IMG_FIREHOSE	1
#define IMG_ALL			100

class CController : public IDownloadCallback
{
public:
	CController();
	virtual ~CController();

public:
	void Initialize(const char* aPortName);
	void RunDownload(const char* aXmlPath);
	void RunReadback(const char* aXmlPath);
		
	static LPSTR ReadAllText(const char* aPath, ILog* aLogIf = NULL);

private:
	CComPort _port;
	
};

