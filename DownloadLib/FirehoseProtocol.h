#pragma once

#include "sahara_def.h"
#include "DownloadBase.h"

#include <vector>

class CFirehoseProtocol : public CDownloadBase
{
public:
	CFirehoseProtocol(IDownloadCallback* aCallback);
	virtual ~CFirehoseProtocol();

	virtual void Initialize(CComPort* aPort, tinyxml2::XmlElement* aXmlProtocol);
	virtual void Download();
	void Readback();

	void Release();	

	enum TResponseResult
	{
		RESP_NAK		= 0,
		RESP_ACK		= 1,
		RESP_SUCCESS	= 1,
		RESP_NORESPONSE = 2,
		RESP_NOTFOUND	= 3,
		RESP_PARSEERROR = 4
	};

	// download image information
	typedef struct _TImageInfo {
		BOOL use;
		const char* name;
		int id;		
		HANDLE file;
		UINT total; // total sectors in bytes
		UINT filesize;
		UINT sent;
		int sectorsize;
		int sectorcount;
		int startsector;
		int partition;
		int blockpage;
		BOOL erase;
		BOOL dump;
		char path[256];
	} TImageInfo;
private:
	void ProcessXml();
	void InternalDownload();
	void InternalReadback();
	void RunConfigure();
	void RunProgram();
	void RunRead();
	void RunReset(int aDelayTime);
	void RunNop();
	void RunErase(TImageInfo* aInfo);
	void RunStorageInfo();
	void InvokeProgram(TImageInfo& aInfo);
	void InvokeRead(TImageInfo& aInfo);
	void PrepareImage(TImageInfo & aInfo);
	void PrepareReading(TImageInfo & aInfo);
	void SendFileData(TImageInfo& aInfo);
	void ReadFileData(TImageInfo & aInfo);
	void SendRawData(AutoMem& aMem);
	TResponseResult  ReadResponse();
	TResponseResult  ReadResponse(tinyxml2::XmlDocument& xdoc, const char* aRespTag = null);
	void SendXml(AutoMem & aMem);
	int ReadXml(AutoMem& aMem);
	void replaceAll(char *s, char *result, const char *olds, const char *news); // jwoh &amp; -> &
private:
	// default information
	struct _default_info {
		int payloadTxSize;
		int payloadRxSize;
		int verbose;
		BOOL erase;
		int reset;
		const char* memoryName;
		const char* targetName;
//		const char* dir;
		int skipStorageInit;
		BOOL skipEraseEfs;
		char dirname[256]; // jwoh &amp; -> &
	} _gInfo;
	
	std::vector<TImageInfo> _imageList;
	AutoMem  _txMem;
	AutoMem  _rxMem;
};
