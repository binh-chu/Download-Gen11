#include "InternalCommon.h"
#include "Controller.h"

#include "ProtocolException.h"
#include "tinyxml2/tinyxml2.h"
#include "SaharaProtocol.h"
#include "FirehoseProtocol.h"

#include <stdio.h>
#include <stdarg.h>

CController::CController()
	: _port(GetILog())
{
	_logHandler = NULL;
	_progressHandler = NULL;
	_logLevel = 3;
}

CController::~CController()
{
	_port.Close();
	_logHandler = NULL;
}

void CController::Initialize(const char* aPortName)
{
	try
	{
		_port.Open(aPortName);
		_port.SetConfig(115200, 0, 1);
	}
	catch (...)
	{
		this->InvokeException("COM Port:%s, Open Failed ! Check that the port is accessble.", aPortName);
	}
}

void CController::RunDownload(const char* aXmlPath)
{
	LPSTR strXml = NULL; 
	
	// 환경 설정 파일 읽기
	strXml = ReadAllText(aXmlPath);
	if (strXml == NULL)
	{
		this->InvokeException("You can not open the configuration file. Please check and try again. (%s)", aXmlPath);
	}
	std::string sxml = strXml;
	::LocalFree(strXml);
		
	tinyxml2::XmlDocument doc;
	tinyxml2::XMLError err = doc.Parse(sxml.c_str());

	if (err != tinyxml2::XML_SUCCESS)
		this->InvokeException("Configuration XML  parsing exception: (%s), Error:%s ", aXmlPath, doc.ErrorName());
		
	tinyxml2::XmlElement& root = doc.Root();
	tinyxml2::XmlElement& xmlSahara = root["downloader"]["sahara"];
	tinyxml2::XmlElement& xmlFirehose = root["downloader"]["firehose"];
	
	this->_logLevel = atoi(root["downloader"].GetText("loglevel"));

	this->WriteLog(LOGVERBOSE, "The configuation file(%s) is loaded successfully.", aXmlPath);
		
	std::auto_ptr<CSaharaProtocol> sahara( new  CSaharaProtocol(static_cast<IDownloadCallback*>(this)));
	std::auto_ptr<CFirehoseProtocol> firehose(new CFirehoseProtocol(static_cast<IDownloadCallback*>(this)));
	
	// SAHARA 프로토콜로 <device programer> 다운로드 시작
	sahara->Initialize(&_port, &xmlSahara);
	sahara->Download();

	this->ReportProgress(-1, 0, 1, EXTRA_SAHARA);

	// FIREHOSE 프로토콜로 <NAND Image Binaries> 다운로드 시작
	firehose->Initialize(&_port, &xmlFirehose);
	firehose->Download();
	
	this->ReportProgress(-1, 0, 1, EXTRA_FIREHOSE);

	this->WriteLog(LOGINFO, "Downloading is finished successfully");

}

void CController::RunReadback(const char* aXmlPath)
{
	LPSTR strXml = NULL;


	// 환경 설정 파일 읽기
	strXml = ReadAllText(aXmlPath);
	if (strXml == NULL)
	{
		this->InvokeException("You can not open the configuration file. Please check and try again. (%s)", aXmlPath);
	}
	std::string sxml = strXml;
	::LocalFree(strXml);

	tinyxml2::XmlDocument doc;
	tinyxml2::XMLError err = doc.Parse(sxml.c_str());

	if (err != tinyxml2::XML_SUCCESS)
		this->InvokeException("Configuration XML  parsing exception: (%s), Error:%s ", aXmlPath, doc.ErrorName());

	tinyxml2::XmlElement& root = doc.Root();
	tinyxml2::XmlElement& xmlSahara = root["downloader"]["sahara"];
	tinyxml2::XmlElement& xmlFirehose = root["downloader"]["firehose"];

	this->_logLevel = atoi(root["downloader"].GetText("loglevel"));

	this->WriteLog(LOGVERBOSE, "The configuation file(%s) is loaded successfully.", aXmlPath);

	std::auto_ptr<CSaharaProtocol> sahara(new  CSaharaProtocol(static_cast<IDownloadCallback*>(this)));
	std::auto_ptr<CFirehoseProtocol> firehose(new CFirehoseProtocol(static_cast<IDownloadCallback*>(this)));

	// SAHARA 프로토콜로 <device programer> 다운로드 시작
	sahara->Initialize(&_port, &xmlSahara);
	sahara->Download();

	// FIREHOSE 프로토콜로 <NAND Image Binaries> dump 시작
	firehose->Initialize(&_port, &xmlFirehose);
	firehose->Readback();

	this->ReportProgress(-1, 0, 1, EXTRA_FIREHOSE);

	this->WriteLog(LOGINFO, "Dump is finished successfully");

}


LPSTR CController::ReadAllText(const char* aPath, ILog* aLogIf)
{
	HANDLE hfile = INVALID_HANDLE_VALUE;
	LPSTR pMem = NULL;

	__try
	{
		hfile = CreateFile(aPath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		if (hfile != INVALID_HANDLE_VALUE)
		{
			DWORD dwSize = ::GetFileSize(hfile, NULL);
			if (dwSize > 0)
			{
				pMem = (LPSTR)::LocalAlloc(LPTR, dwSize + 1);
				if (pMem)
				{
					DWORD read = 0, total = 0;
					do 
					{
						if (::ReadFile(hfile, pMem + total, dwSize - total, &read, NULL))
						{
							total += read;
						}
						else
						{
							if (aLogIf != NULL)
								aLogIf->ReportGetLastError("[ReadAllText] file reading error");
							
							::LocalFree(pMem);
							pMem = NULL;
							break;
						}
						
					} while (total < dwSize);
				}
				else
				{
					if (aLogIf != NULL)
						aLogIf->ReportGetLastError("[ReadAllText] memory allocation failed");
				}
			}
			else
			{
				if (aLogIf != NULL)
					aLogIf->ReportGetLastError("[ReadAllText] file size is zero");
			}
		}
		else
		{
			if (aLogIf != NULL)
				aLogIf->ReportGetLastError("[ReadAllText] file reading failed");
		}
	}
	__finally
	{
		if (hfile != INVALID_HANDLE_VALUE)
			CloseHandle(hfile);

	}
	return pMem;
}