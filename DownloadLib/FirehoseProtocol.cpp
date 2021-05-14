
#include "InternalCommon.h"
#include "FirehoseProtocol.h"

#include <string>

using namespace tinyxml2;

#include "fbXmlTables.h"

#define RETRY_XML_READ		10
#define RETRY_XML_SLEEP		100

CFirehoseProtocol::CFirehoseProtocol(IDownloadCallback* aCallback)
	: CDownloadBase(aCallback), _txMem(4096), _rxMem(1024 * 5)
{
}


CFirehoseProtocol::~CFirehoseProtocol()
{
	Release();
}

void CFirehoseProtocol::Initialize(CComPort* aPort, XmlElement* aXmlProtocol)
{
	CDownloadBase::Initialize(aPort, aXmlProtocol);
	
	ProcessXml();
}

void CFirehoseProtocol::ProcessXml()
{
	XmlElement& xfb = *_xml;
	XmlElement& xconf = xfb["configure"];

	_gInfo.memoryName		= xconf.GetText("memoryName");
	_gInfo.skipStorageInit	= atoi(xconf.GetText("skipStorageInit"));
	_gInfo.targetName		= xconf.GetText("targetName");
	_gInfo.payloadTxSize		= atoi(xconf.GetText("maxPayloadSizeToTargetInBytes"));
	_gInfo.verbose			= atoi(xconf.GetText("verbose"));
	_gInfo.erase				=  atoi(xconf.GetText("allErase"));
	_gInfo.reset				= atoi(xconf.GetText("reset"));
//	_gInfo.dir				= xconf.GetText("dir");
	_gInfo.payloadRxSize		= 2048;
	_gInfo.skipEraseEfs      = atoi(xconf.GetText("skipEraseEfs"));

	replaceAll((char *)xconf.GetText("dir"), _gInfo.dirname, "&amp;", "&"); // jwoh &amp; -> &

	XmlElement* image = xfb["images"].FirstChildElement();
	while (image)
	{
		XmlElement& m = *image;
		
		TImageInfo img;
		img.use			= atoi(m("use"));
		img.file		= INVALID_HANDLE_VALUE;
		img.filesize	= 0;
		img.sent		= 0;
		img.name		= m("name");
		img.id			= atoi(m("id"));
		img.erase		= atoi(m("erase"));
		img.dump		= atoi(m("dump"));
		bool validFileName = strlen(m("filename")) > 0;
		if (validFileName)
			sprintf_s(img.path, sizeof(img.path), "%s\\%s", _gInfo.dirname, m("filename"));
		else
			sprintf_s(img.path, sizeof(img.path), "%s\\%s.bak", _gInfo.dirname, m("name"));

		XmlElement& xp	= m["program"];
		img.blockpage	= atoi(xp("PAGES_PER_BLOCK"));
		img.sectorsize	= atoi(xp("SECTOR_SIZE_IN_BYTES"));
		img.sectorcount = atoi(xp("num_partition_sectors"));
		img.partition	= atoi(xp("physical_partition_number"));
		img.startsector = atoi(xp("start_sector"));

		img.total = img.sectorsize * img.sectorcount;
		_imageList.insert(_imageList.end(), img);
		

		image = image->NextSiblingElement();
	}

}

void CFirehoseProtocol::Download()
{
	ULONGLONG start = GetTickCount64();

	Logi("[FIREHOSE] start images downloading");
	
	InternalDownload();

	ULONGLONG elapsed = GetTickCount64() - start;

	Logi("[FIREHOSE] Finished successfully. (elapsed:%lld.%lld seconds)", elapsed / 1000, elapsed % 1000);
}

void CFirehoseProtocol::Readback()
{
	ULONGLONG start = GetTickCount64();

	Logi("[FIREHOSE] start dump");

	InternalReadback();

	ULONGLONG elapsed = GetTickCount64() - start;

	Logi("[FIREHOSE] Finished successfully. (elapsed:%lld.%lld seconds)", elapsed / 1000, elapsed % 1000);
}

void CFirehoseProtocol::Release()
{
	for (UINT n = 0; n < _imageList.size(); n++)
	{
		if (_imageList[n].file != INVALID_HANDLE_VALUE)
			CloseHandle(_imageList[n].file);
	}
	
	_imageList.clear();
}

void CFirehoseProtocol::InternalDownload()
{
	// hand shake
//	RunNop();

	// configure step
	RunConfigure();

	if (GetLogLevel() >= LOGVERBOSE)
		RunStorageInfo();

	RunErase(null);
		
	// program step
	RunProgram();
	
	// reset step
	RunReset(_gInfo.reset);

}

void CFirehoseProtocol::InternalReadback()
{
	// hand shake
	//	RunNop();

	// configure step
	RunConfigure();

	//	RunStorageInfo();

	// program step
	RunRead();

}

void CFirehoseProtocol::RunConfigure()
{
	_txMem.Reset();

	GetXmlConfigure(_txMem, _gInfo.memoryName, _gInfo.targetName, _gInfo.payloadTxSize, _gInfo.verbose, _gInfo.skipStorageInit);

	SendXml(_txMem);

	XmlDocument xdoc;
	TResponseResult result = ReadResponse(xdoc);
	if (result == RESP_ACK)
	{
		XmlElement& xresp = xdoc.Root()["response"];
		if (xresp.Attribute("MaxPayloadSizeToTargetInBytesSupported"))
		{
			int payload = atoi(xresp("MaxPayloadSizeToTargetInBytesSupported"));
			if (payload != _gInfo.payloadTxSize)
			{
				_gInfo.payloadTxSize = payload;
				RunConfigure();
				return;
			}
		}
		_gInfo.payloadTxSize = atoi(xresp("MaxPayloadSizeToTargetInBytes"));
		if (xresp.Attribute("MaxPayloadSizeFromTargetInBytes"))
		{
			_gInfo.payloadRxSize = atoi(xresp("MaxPayloadSizeFromTargetInBytes"));
		}
	}
	else if (result == RESP_NAK)
	{
		Logv("[FIREHOSE] <configure> received NAK but go ahead ... ");
		XmlElement& xresp = xdoc.Root()["response"];
		_gInfo.payloadTxSize = atoi(xresp("MaxPayloadSizeToTargetInBytes"));
	}
	else
	{
		Exception("No valid responses for <configure> (result:%d). Try again later.", result);
	}
}

void CFirehoseProtocol::RunProgram()
{
	Logv("Start download image <program> ..");

	for (size_t n = 0; n < _imageList.size(); n++)
	{
		TImageInfo& info = _imageList[n];
		if (info.use)
		{
			RunErase(&info);
			InvokeProgram(info);
		}
	}

	Logv(".. End download image <program> ");
}

void CFirehoseProtocol::RunRead()
{
	Logv("Start readback image <read> ..");

	_rxMem.Resize(_gInfo.payloadRxSize);
	_rxMem.Reset(0);

	for (size_t n = 0; n < _imageList.size(); n++)
	{
		TImageInfo& info = _imageList[n];
		if (info.dump)
			InvokeRead(info);
	}

	Logv(".. End readback image <read> ");
}

void CFirehoseProtocol::RunReset(int aDelayTime)
{
	if (aDelayTime == 0)
		return;

	Logv("Try request <power reset>...");

	_txMem.Reset();

	GetXmlReset(_txMem, aDelayTime);

	SendXml(_txMem);

	if (ReadResponse() != RESP_ACK)
	{
		Loge("[FIREHOSE] Reset Failed !");
	}
	else
	{
		Logi("[FIREHOSE] Will be reset after %d seconds.", aDelayTime);
	}
}

void CFirehoseProtocol::RunNop()
{
	Logv("Try knok <nop>...");

	_txMem.Reset();

	GetXmlNop(_txMem);

	SendXml(_txMem);

	if (ReadResponse() != RESP_ACK)
	{
		Loge("[FIREHOSE] Reset Failed !");
	}
}

void CFirehoseProtocol::RunErase(TImageInfo* aInfo)
{
	if (aInfo != null) // item erase
	{
		Logv("Erase item:%s partition, start-sector:%d  num-sector", aInfo->name, aInfo->startsector, aInfo->sectorcount);
		_txMem.Reset();
		GetXmlErase(_txMem, aInfo->startsector, aInfo->sectorcount);
		SendXml(_txMem);

		if (ReadResponse() != RESP_ACK)
		{
			Loge("[FIREHOSE] Erase item area[%s-start:%d-num:%d] Failed !", aInfo->name, aInfo->startsector, aInfo->sectorcount);
		}
	}
	else if (!_gInfo.skipEraseEfs  && _gInfo.erase) // all erase
	{
		Logi("Erase flash all partition");

		_txMem.Reset();
		GetXmlErase(_txMem);
		SendXml(_txMem);

		if (ReadResponse() != RESP_ACK)
		{
			Loge("[FIREHOSE] Erase All Failed !");
		}
	} 
	else if (_gInfo.skipEraseEfs  && _gInfo.erase) // erase all except EFS_2
	{
		Logi("Erase  All except a EFS backup area");
		for (size_t n = 0; n < _imageList.size(); n++)
		{
			TImageInfo& info = _imageList[n];
			if (strstr(info.name, "EFS_2") == null)
			{
				RunErase(&info);
			}
		}
	}
	else if (_gInfo.skipEraseEfs  && !_gInfo.erase) // erase EFS only
	{
		Logi("Erase  a EFS partition");
		for (size_t n = 0; n < _imageList.size(); n++)
		{
			TImageInfo& info = _imageList[n];
			if (_strcmpi(info.name, "EFS") == 0)
			{
				RunErase(&info);
			}
		}

		for (size_t n = 0; n < _imageList.size(); n++)
		{
			TImageInfo& info = _imageList[n];
			if (_strcmpi(info.name, "FOTA_SELFTEST") == 0 && info.erase)
			{
				Logi("Check FOTA Erase option"); // jwoh add FOTA erasing TCPXI-13803 
				RunErase(&info);
			}
		}
	}
	else // FOTA erasing
	{
		for (size_t n = 0; n < _imageList.size(); n++)
		{
			TImageInfo& info = _imageList[n];
			if (_strcmpi(info.name, "FOTA_SELFTEST") == 0 && info.erase)
			{
				Logi("Check FOTA Erase option");
				RunErase(&info);
			}
		}
	}
}

void CFirehoseProtocol::RunStorageInfo()
{
	Logv("Try read <getStorageInfo>");

	_txMem.Reset();

	GetXmlStorageInfo(_txMem, 0);

	SendXml(_txMem);

	XmlDocument xdoc;
	if (ReadResponse(xdoc) != RESP_ACK)
	{
		Loge("[FIREHOSE] get StorageInfo/0 Failed !");
	}

	GetXmlStorageInfo(_txMem, 1);

	SendXml(_txMem);

	if (ReadResponse(xdoc) != RESP_ACK)
	{
		Loge("[FIREHOSE] get StorageInfo/1 Failed !");
	}
}

void CFirehoseProtocol::InvokeProgram(TImageInfo & aInfo)
{
	PrepareImage(aInfo);

	if (aInfo.total == 0)
		return;

	// send program
	GetXmlProgram(_txMem, aInfo.blockpage, aInfo.name, aInfo.sectorsize, aInfo.sectorcount, aInfo.partition, aInfo.startsector);
	SendXml(_txMem);

	// read response
	if (ReadResponse() != RESP_ACK)
	{
		Exception("[FIREHOSE] No responses for <program>");
	}

	// send raw data
	SendFileData(aInfo);

	// read response
	if (ReadResponse() != RESP_ACK)
	{
		Exception("[FIREHOSE] PROGRAM (write all raw data) - No response then exit task.");
	}
	Logi("[FIREHOSE] Completed information sent: [id:%d] [name:%s] Sector-Start:%d, Sector-Count:%d",
				aInfo.id, aInfo.name, aInfo.startsector, aInfo.sectorcount);

}

void CFirehoseProtocol::InvokeRead(TImageInfo & aInfo)
{
	PrepareReading(aInfo);

	if (aInfo.file == INVALID_HANDLE_VALUE)
		return;

	// send program
	GetXmlRead(_txMem, aInfo.startsector, aInfo.sectorcount, aInfo.partition, aInfo.sectorsize);
	SendXml(_txMem);

	// read response
	if (ReadResponse() != RESP_ACK)
	{
		Exception("[FIREHOSE] No responses for <read> then exit");
	}

	if (_rxMem.Count() > 0) // xml다음에 바로 bin가 왔을 경우 처리.
	{
		DWORD written;
		if (::WriteFile(aInfo.file, _rxMem(), _rxMem.Count(), &written, NULL) == FALSE)
		{
			Exception("[FIREHOSE] Failed data write to file ! (file:%s)", aInfo.path);
		}

		aInfo.filesize += _rxMem.Count();
		_rxMem.Reset();
	}

	// send raw data
	ReadFileData(aInfo);

	// read response
	if (ReadResponse() != RESP_ACK)
	{
		Exception("[FIREHOSE] READ(%s) - No response then exit a task.", aInfo.path);
	}
	Logi("[FIREHOSE] Completed dump information: [id:%d] [name:%s] Sector-Start:%d, Sector-Count:%d, File:%s",
		aInfo.id, aInfo.name, aInfo.startsector, aInfo.sectorcount, aInfo.path);

}

void CFirehoseProtocol::PrepareImage(TImageInfo & aInfo)
{

	LARGE_INTEGER fsize;
	HANDLE hfile = CreateFile(aInfo.path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hfile == INVALID_HANDLE_VALUE || !GetFileSizeEx(hfile, &fsize))
	{
		_callback->ReportGetLastError(aInfo.path);
		//Exception("[FIREHOSE] File Open Error:[%d-%s] path: %s", aInfo.id, aInfo.name, aInfo.path); // throw exception
		// 파일이 없을 경우에는 erase모드로 동작
		aInfo.total = 0;
		Loge("[FIREHOSE] No file exist [%s] then skip", aInfo.name);			
	}
	else
	{
		aInfo.file = hfile;
		aInfo.filesize = (UINT)fsize.LowPart;
		aInfo.sectorcount = (aInfo.filesize + aInfo.sectorsize - 1) / aInfo.sectorsize;
			
		if (aInfo.total >= aInfo.filesize)
			aInfo.total = aInfo.sectorcount * aInfo.sectorsize;
		else
		{
			aInfo.filesize = aInfo.total;
			aInfo.sectorcount = (aInfo.filesize + aInfo.sectorsize - 1) / aInfo.sectorsize;
			aInfo.total = aInfo.sectorcount * aInfo.sectorsize;
		}

		::SetFilePointer(aInfo.file, 0, NULL, FILE_BEGIN);
	}

	Logi("[FIREHOSE] File information transfer: [id:%d] [name:%s] path:(%s), file-size:%d ",
		aInfo.id, aInfo.name, aInfo.path, aInfo.filesize);

	Logv("[FIREHOSE] Sectors information transfer: [id:%d] [name:%s] Sector-Start:%d, Sector-Count:%d, Partition:%d",
		aInfo.id, aInfo.name, aInfo.startsector, aInfo.sectorcount, aInfo.partition);
}

void CFirehoseProtocol::PrepareReading(TImageInfo & aInfo)
{
	HANDLE hfile = CreateFile(aInfo.path, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hfile == INVALID_HANDLE_VALUE)
	{
		_callback->ReportGetLastError(aInfo.path);
		Loge("[FIREHOSE] backup file(%s) creation error, then skip !", aInfo.path);
	}
	else
	{
		aInfo.file = hfile;
		::SetFilePointer(aInfo.file, 0, NULL, FILE_BEGIN);
	}

	Logi("[FIREHOSE] Partition file information dump: [id:%d] [name:%s] path:(%s)",
		aInfo.id, aInfo.name, aInfo.path);

	Logv("[FIREHOSE] Partition sectors information dump: [id:%d] [name:%s] Sector-Start:%d, Sector-Count:%d, Partition:%d",
		aInfo.id, aInfo.name, aInfo.startsector, aInfo.sectorcount, aInfo.partition);
}

void CFirehoseProtocol::SendFileData(TImageInfo& aInfo)
{
	AutoMem mdata(_gInfo.payloadTxSize);

	int read = _gInfo.payloadTxSize;
	while (aInfo.sent < aInfo.filesize)
	{
		mdata.Reset();
		read = ReadContents(aInfo.file, mdata);
		
		// 다 채워지지 않으면 다 채우도록 한다. (다 채워지지 않을 경우 응답이 안 옴)
		if (mdata.Count() < _gInfo.payloadTxSize)
		{
			mdata.setCount(min(aInfo.total - aInfo.sent, (UINT)_gInfo.payloadTxSize));
		}

		SendRawData(mdata);
		aInfo.sent += mdata.Count();

		FirehoseProgress(aInfo.id, aInfo.sent, aInfo.total);
		//::Sleep(1);
	}
	while (aInfo.sent < aInfo.total) // file's end part
	{
		mdata.Reset();
		mdata.setCount( min(aInfo.total - aInfo.sent, (UINT)_gInfo.payloadTxSize));
		SendRawData(mdata);
		aInfo.sent += mdata.Count();

		FirehoseProgress(aInfo.id, aInfo.sent, aInfo.total);
		//::Sleep(1);
	}

	if (aInfo.file != INVALID_HANDLE_VALUE)
	{
		CloseHandle(aInfo.file);
		aInfo.file = INVALID_HANDLE_VALUE;
	}
}

void CFirehoseProtocol::ReadFileData(TImageInfo& aInfo)
{
	AutoMem mdata(_gInfo.payloadRxSize);

	int read = _gInfo.payloadRxSize;
	while (aInfo.filesize < aInfo.total)
	{
		mdata.Reset();

		read = ReadRawData(mdata(), mdata.Size());
		mdata.setCount(read);
		if (mdata.Count() == 0)
		{
			Exception("[FIREHOSE] Failed  data read from target ! (file:%s)", aInfo.path);
		}

		if (aInfo.total < (aInfo.filesize + read)) // response xml부분이 남음.
		{
			int remain = aInfo.filesize + read - aInfo.total;
			::CopyMemory(_rxMem(), mdata(read - remain), remain);
			_rxMem.setCount(remain);
			mdata.setCount(read - remain);
		}

		DWORD written;
		if (::WriteFile(aInfo.file, mdata(), mdata.Count(), &written, NULL) == FALSE)
		{
			Exception("[FIREHOSE] Failed data write to file ! (file:%s)", aInfo.path);
		}

		aInfo.filesize += mdata.Count();

		FirehoseProgress(aInfo.id, aInfo.filesize, aInfo.total);

		//if (read < mdata.Size())
		//	break;
	}

	if (aInfo.file != INVALID_HANDLE_VALUE)
	{
		CloseHandle(aInfo.file);
		aInfo.file = INVALID_HANDLE_VALUE;
	}
}

void CFirehoseProtocol::SendRawData(AutoMem& aMem)
{
	CDownloadBase::SendRawData(aMem(), aMem.Count());
}

CFirehoseProtocol::TResponseResult CFirehoseProtocol::ReadResponse()
{
	XmlDocument xdoc;

	return ReadResponse(xdoc);
}

CFirehoseProtocol::TResponseResult CFirehoseProtocol::ReadResponse(XmlDocument& xdoc, const char* aRespTag)
{
	int retry = 0;
	while (retry < RETRY_XML_READ)
	{
		// read xml
		ReadXml(_rxMem);

		while (_rxMem.Count() > 0)
		{
			std::string strx;
			// 유효한 xml 블럭이 있는지 확인한다.
			// <?xml ?><data></data>
			strx.assign(_rxMem(), _rxMem.Count());
			int start = strx.find("<?xml");
			int next  = strx.find("<?xml", 10);
			int end   = strx.rfind("</data>");

			if (start >= 0 && end > start)
			{
				end = (next > start) ? (next) : (end + 7);

				Logv("[FIREHOSE] RECV XML <<\n%s", strx.substr(start, end - start).c_str());

				// parsing
				xdoc.Clear();
				XMLError err = xdoc.Parse(_rxMem(start), end - start);
				_rxMem.Remove(end);

				if (err != XML_SUCCESS)
				{
					Loge("[FIREHOSE] ReadResponse XML Parsing Error.[%d, %s]", xdoc.ErrorID(), xdoc.ErrorName());
					if (_rxMem.Count() < 10)
						return RESP_PARSEERROR;
				}
				else
				{
					XmlElement* xdata = xdoc.FirstChildElement();
					if (xdata == null || _stricmp(xdata->Name(), "data") != 0)
					{
						Loge("[FIREHOSE] root tag/ Not found <data> tag..", xdata->Name());
					}
					else
					{
						XmlElement* xresp = xdata->FirstChildElement();
						if (_stricmp(xresp->Name(), "log") == 0)
						{
							Logv("[FIREHOSE] <log> %s", xresp->AttributeValue("value"));
							if (aRespTag != null && _stricmp(xresp->Name(), aRespTag) == 0)
							{
								return RESP_SUCCESS;
							}
						}
						else if (_stricmp(xresp->Name(), "response") == 0)
						{
							Logv("[FIREHOSE] <response> %s", xresp->AttributeValue("value"));

							if (_stricmp(xresp->AttributeValue("value"), "ACK") == 0)
								return RESP_ACK;
							
							return RESP_NAK;
						}
						else
						{
							Loge("[FIREHOSE] Unexpected xml tag <%s>", xresp->Name());
						}
					}
				}				
			}
			else
			{
				if (_rxMem.Count() == _rxMem.Size())
					_rxMem.Reset(); // 16K 이상되는 xml은 없다. 혹 있어도 잘못 된 것으로 간주해서 버린다.
				break;
			}
		}
		
		retry++;
		::Sleep(RETRY_XML_SLEEP);
	}
	Loge("[FIREHHOSE] NO Response Read !!!");

	return RESP_NORESPONSE;
}

void CFirehoseProtocol::SendXml(AutoMem& aMem)
{
	Logi("[FIREHOSE] SEND XML >>\n%s", aMem());

	_port->Write(aMem(), aMem.Count());
		
}

int CFirehoseProtocol::ReadXml(AutoMem& aMem)
{
	int lastPos = aMem.Count();
	int length = _port->Read(aMem.EndPtr(), aMem.remainedSize());
	if (length > 0)
	{
		aMem.increaseCount(length);		
	}

	return length;
}

void CFirehoseProtocol::replaceAll(char *s, char *result, const char *olds, const char *news) // jwoh &amp; -> &
{
	char *sr;
	size_t i, count = 0;
	size_t oldlen = strlen(olds);
	size_t newlen = strlen(news);


	if (newlen != oldlen)
	{
		for (i = 0; s[i] != '\0';) {
			if (memcmp(&s[i], olds, oldlen) == 0) count++, i += oldlen;
			else i++;
		}
	}
	else
	{
		i = strlen(s);
	}

	sr = result;
	while (*s)
	{
		if (memcmp(s, olds, oldlen) == 0)
		{
			memcpy(sr, news, newlen);
			sr += newlen;
			s += oldlen;
		}
		else *sr++ = *s++;
	}
	*sr = '\0';
}