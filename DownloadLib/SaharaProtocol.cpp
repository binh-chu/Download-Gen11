
#include "InternalCommon.h"
#include "SaharaProtocol.h"

using namespace tinyxml2;

static const char* GetSatusCode(int aCode);

CSaharaProtocol::CSaharaProtocol(IDownloadCallback* aCallback)
	: CDownloadBase(aCallback)
{
	_minVer = 1;
	_maxVer = 2;
	_mode = SAHARA_MODE_IMAGE_TX_COMPLETE;
	_state = UNKNOWN_STATE;
	_maxTargetSize = sizeof(TPacket);
	_skipReadPacket = FALSE;
	_ddInfo.file = INVALID_HANDLE_VALUE;
	_memTable.table = NULL;
	_memInfo.contents = NULL;
}


CSaharaProtocol::~CSaharaProtocol()
{
	Release();
}

void CSaharaProtocol::Initialize(CComPort* aPort, XmlElement* aXmlProtocol)
{
	CDownloadBase::Initialize(aPort, aXmlProtocol);
	
	Release();

	_state = UNKNOWN_STATE;
	memset(&_tx, 0, sizeof(TPacket));
	memset(&_rx, 0, sizeof(TPacket));

	_ddInfo.img_id = -1;
	_ddInfo.file = INVALID_HANDLE_VALUE;
	_ddInfo.sent = 0;
	_ddInfo.total = 0;
	_imageList.clear();

	_memTable.table = NULL;
	_memTable.addr = 0;
	_memTable.length = 0;
	_memTable.read = 0;

	_memInfo.contents = NULL;
	_memInfo.addr = 0;
	_memInfo.length = 0;
	_memInfo.read = 0;

	_minVer = 1;
	_maxVer = 2;

	_skipReadPacket = FALSE;

	ProcessXml();
}

void CSaharaProtocol::ProcessXml()
{
	// version
	XmlElement& xml = *_xml;
	
	{
		const char* value = xml["version"].GetText("min");
		if (value)
			_minVer = atoi(value);

		value = xml["version"].GetText("max");
		if (value)
			_maxVer = atoi(value);
	}
	// add image infomation into list for downloading
	for (XmlElement* image = xml["images"].FirstChildElement("image"); image != null ; image = image->NextSiblingElement())
	{
		const char* id = image->GetText("id");
		const char* path = image->GetText("path");

		if (id && path)
		{
			AddDownloadMap(atoi(id), path);			
		}
	} 

}

void CSaharaProtocol::Download()
{
	Logi("[SAHARA] load flash programmer [%s]", (*_imageList.begin()).second.c_str());

	StartDownload();

	Logi("[SAHARA] download done and run flash programmer [%s]", (*_imageList.begin()).second.c_str());
}

void CSaharaProtocol::Release()
{
	_mode = SAHARA_MODE_IMAGE_TX_COMPLETE;

	if (_ddInfo.file != INVALID_HANDLE_VALUE)
	{
		CloseHandle(_ddInfo.file);
		_ddInfo.file = INVALID_HANDLE_VALUE;
	}
	if (_memTable.table != NULL)
	{
		::LocalFree(_memTable.table);
		_memTable.table = NULL;
	}
	if (_memInfo.contents != NULL)
	{
		::LocalFree(_memInfo.contents);
		_memInfo.contents = NULL;
	}
	_imageList.clear();
}

void CSaharaProtocol::StartDownload()
{
	_mode = SAHARA_MODE_IMAGE_TX_PENDING;

	StateMachine();
}

void CSaharaProtocol::StartMemoryDebug()
{
	_mode = SAHARA_MODE_MEMORY_DEBUG;

	StateMachine();
}

void CSaharaProtocol::StartCommand(int aCmd, void* aResultBuf, int aBufLength)
{

	_cmdResult.cmd = aCmd;
	_cmdResult.rbuf = (LPSTR)aResultBuf;
	_cmdResult.rlength = aBufLength;
	_cmdResult.dlength = 0;
	_cmdResult.read = 0;

	_mode = SAHARA_MODE_COMMAND;

	StateMachine();
}

void CSaharaProtocol::AddDownloadMap(int aImageId, const char* aFilePath)
{
	_imageList[aImageId] = aFilePath;

	Logv("[SAHARA] add image into list (%d:%s)", aImageId, aFilePath);
}

void CSaharaProtocol::StateMachine()
{
	_state = WAIT_HELLO;
	_skipReadPacket = FALSE;
	
	ULONGLONG start = GetTickCount64();

	while (_state != STATE_DONE)
	{
		if (!_skipReadPacket && !ReadPacket())
		{
			if (_state == WAIT_HELLO)
			{
				TryPseudoHello();
			}
			else
				continue;
		}

		TProtocolID id = (TProtocolID)_rx.header.command;

		switch (_state)
		{
		case WAIT_HELLO:
			ProcessHello(id);
			break;
		case WAIT_COMMAND:
			if (id == READ_DATA_REQ || id == READ_DATA64_REQ)
			{
				ProcessDownload(id);
			}
			else if (id == ENDOFIMAGE_TX)
			{
				ProcessEndOfImage();
			}
			else if (id == MEMORY_DEBUG || id == MEMORY64_DEBUG)
			{
				ProcessMemoryDebug(id);
			}
			else if(id == COMMAND_READY)
			{
				ProcessCommand();
			}
			break;
		case WAIT_DONE_RESP:
			ProcessDoneResp();
			break;
		case WAIT_RESET_RESP:
			_state = STATE_DONE;
			break;
		case WAIT_MEMORY_TABLE:
			ReadMemoryTable((TProtocolID)_memTable.cmd);
			break;
		case WAIT_MEMORY_REGION:
			ReadMemoryContents((TProtocolID)_memTable.cmd);
			break;
		case WAIT_CMD_EXEC_RESP:
			ProcessCmdResponse(id);
			break;
		case WAIT_CMD_RESULT_RESP:
			ProcessCmdData();
			break;
		default:
			SendReset();
			Exception("[SAHARA] Current Run State is invalid (%d) !", _state);
			break;
		}

		if (_state == UNKNOWN_STATE)
			break;
	}

	ULONGLONG elapsed = GetTickCount64() - start;

	Logi("[SAHARA] Finished successfully. (Mode=%d, elapsed:%lld.%lld seconds)", _mode, elapsed / 1000, elapsed % 1000);
}

void CSaharaProtocol::ProcessCmdData()
{
	int length = min(_cmdResult.rlength, _cmdResult.dlength) - _cmdResult.read;

	_cmdResult.read += this->ReadRawData(&_cmdResult.rbuf[_cmdResult.read], length);
	if (_cmdResult.read == min(_cmdResult.rlength, _cmdResult.dlength))
	{
		Logv("[SAHARA] Command [%d] result received all (length:%d)", _cmdResult.read);

		S_COMMAND_SWITCH_MODE& req = _tx.switch_mode;
		req.header.command = COMMAND_SWITCH_MODE;
		req.header.length = sizeof(S_COMMAND_SWITCH_MODE);
		req.mode = SAHARA_MODE_IMAGE_TX_PENDING;

		SendPacket();

		_state = UNKNOWN_STATE;
		_skipReadPacket = FALSE;
	}
}

void CSaharaProtocol::ProcessCmdResponse(TProtocolID aCmd)
{
	if (aCmd != COMMAND_EXEC_RESP)
	{
		Loge("[SAHARA] Wait for COMMAND_EXEC_RESP but received %s", this->GetPacketName(aCmd));
		return;
	}

	S_COMMAND_EXEC_RESP & resp = _rx.cmd_exec_resp;
	
	if (resp.client_command != _cmdResult.cmd)
	{
		Loge("[SAHARA] Wait for response about command [%d] but received command is %d.. wait again for it", _cmdResult.cmd, resp.client_command);
	}
	else
	{
		Logv("[SAHARA] Command [%d] execution is completed (result Length:%d)", resp.client_command, resp.resp_length);
		_cmdResult.dlength = resp.resp_length;
		_cmdResult.read = 0;

		if (resp.resp_length > 0)
		{
			S_COMMAND_EXEC_DATA_REQ& req = _tx.cmd_exec_data_req;
			req.header.command = COMMAND_EXEC_DATA_REQ;
			req.header.length = sizeof(S_COMMAND_EXEC_DATA_REQ);
			req.client_command = _cmdResult.cmd;

			SendPacket();

			_state = WAIT_CMD_RESULT_RESP;
			_skipReadPacket = TRUE;
		}
		else
		{
			S_COMMAND_SWITCH_MODE& req = _tx.switch_mode;
			req.header.command = COMMAND_SWITCH_MODE;
			req.header.length = sizeof(S_COMMAND_SWITCH_MODE);
			req.mode = SAHARA_MODE_IMAGE_TX_PENDING;

			SendPacket();

			_skipReadPacket = TRUE;
			_state = UNKNOWN_STATE;
		}

	}
}

void CSaharaProtocol::ProcessCommand()
{
	S_COMMAND_EXEC_REQ& req = _tx.cmd_exec_req;
	req.header.command = COMMAND_EXEC_REQ;
	req.header.length = sizeof(S_COMMAND_EXEC_REQ);
	req.client_command = _cmdResult.cmd;

	Logv("[SAHARA] Request Command EXEC [%d]", _cmdResult.cmd);

	SendPacket();
	_state = WAIT_CMD_EXEC_RESP;
}

void CSaharaProtocol::ProcessMemoryDebug(TProtocolID aCmd)
{
	UINT64 addr, length;
	if (aCmd == MEMORY_DEBUG)
	{
		S_MEMORY_DEBUG & resp = _rx.memory_debug;
		addr = resp.memory_table_addr;
		length = resp.memory_table_length;
	}
	else // MEMORY64_DEBUG
	{
		S_MEMORY64_DEBUG & resp = _rx.memory64_debug;
		addr = resp.memory_table_addr;
		length = resp.memory_table_length;
	}

	if (_memTable.table != NULL)
	{
		::LocalFree(_memTable.table);
		_memTable.table = NULL;
	}

	LPSTR pMem =  (LPSTR)::LocalAlloc(LPTR, (SIZE_T)length);
	if (pMem == NULL)
	{
		_callback->ReportGetLastError("[SAHARA] MemoryAllocation Failed in MemoryDebug");
		Exception("[SAHARA] Memory debug table allocation failed");
	}

	_memTable.cmd = aCmd;
	_memTable.addr = addr;
	_memTable.length = length;
	_memTable.read = 0;
	_memTable.table = pMem;

	_state = WAIT_MEMORY_TABLE;
	_skipReadPacket = TRUE;
}

void CSaharaProtocol::ProcessDoneResp()
{
	S_DONE_RESP& resp = _rx.done_resp;
	Logv("RX Packet [%s] - ImageTxStatus:%d", GetPacketName((TProtocolID)resp.header.command), resp.image_tx_status);

	if (resp.image_tx_status == (UINT32)SAHARA_MODE_IMAGE_TX_COMPLETE)
	{
		Logv("[SAHARA] All images completed downloding.");
		_imageList.clear();
		SendReset();
		_skipReadPacket = TRUE;
		_state = UNKNOWN_STATE;
	}
	else
	{
		//_state = WAIT_HELLO;
		//Log("[SAHARA] File transering is finished, go to next download");
		// 현재의 QPST sahara에서는 firehose 이미지만 다운로드 하고 사하라 프로토콜을 종료한다.
		Logv("[SAHARA] FB device programmer downloading is done. Force quit sahara protocol.");
		_skipReadPacket = TRUE;
		_state = UNKNOWN_STATE;
	}

}

void CSaharaProtocol::ProcessEndOfImage()
{
	S_ENDOFIMAGE_TX& resp = _rx.endofimage_tx;

	Logv("RX Packet [%s] - ImageID:%d, Status:%s", GetPacketName((TProtocolID)resp.header.command), resp.image_id, GetSatusCode(resp.status));

	if (resp.status == ESAHARA_STATUS_SUCCESS)
	{
		Logv("[SAHARA] ENDOFIMAGE_TX: [%d-%s] download completed",
						_ddInfo.img_id, _imageList[_ddInfo.img_id].c_str());

		SaharaProgress(_ddInfo.img_id, _ddInfo.total, _ddInfo.total);

		if (_ddInfo.file != INVALID_HANDLE_VALUE)
		{
			::CloseHandle(_ddInfo.file);
			_ddInfo.file = INVALID_HANDLE_VALUE;
		}
		_ddInfo.img_id = -1;

		// send done
		S_DONE_REQ& req = _tx.done_req;
		req.header.command = DONE_REQ;
		req.header.length = sizeof(S_DONE_REQ);

		SendPacket();
		_state = WAIT_DONE_RESP;
	}
	else
	{
		SendReset();
		Exception("[SAHARA] ENDOFIMAGE_TX: error returned, code=%d", resp.status);
	}
}

void CSaharaProtocol::ProcessDownload(TProtocolID aCmd)
{
	int imageId, length;
	UINT64 offset;
	if (aCmd == READ_DATA_REQ)
	{
		S_READ_DATA_REQ& req = _rx.read_data_req;
		imageId = req.image_id;
		offset = req.data_offset;
		length = req.data_length;

		Logv("RX Packet [%s] - Offset:%d, Length:%d", GetPacketName((TProtocolID)req.header.command), req.data_offset, req.data_length);
	}
	else // READ_DATA64_REQ
	{
		S_READ_DATA64_REQ& req = _rx.read_data64_req;
		imageId = (int)req.image_id;
		offset = req.data_offset;
		length = (int)req.data_length;

		Logv("RX Packet [%s] - Offset:%lld, Length:%lld", GetPacketName((TProtocolID)req.header.command), req.data_offset, req.data_length);
	}

	// 전송할 파일에 대한 처리를 수행한다.
	if (_ddInfo.img_id != imageId || _ddInfo.file == INVALID_HANDLE_VALUE)
	{
		// 처리할 정보가 없는 이미지 요청 - 
		if (_imageList.find(imageId) == _imageList.end())
		{
			Loge("[SAHARA] Invalid Image ID (%d) requested from host", imageId);
			SendReset();			
		}

		const char* path = _imageList[imageId].c_str();
		if (_ddInfo.file != INVALID_HANDLE_VALUE)
		{
			CloseHandle(_ddInfo.file);
			_ddInfo.file = INVALID_HANDLE_VALUE;
		}

		LARGE_INTEGER fsize;
		HANDLE hfile = CreateFile( path, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		if (hfile == INVALID_HANDLE_VALUE || !GetFileSizeEx(hfile, &fsize))
		{
			_callback->ReportGetLastError(path);
			Exception("[SAHARA] File Open Error:[%d-%s]", imageId, path); // throw exception
		}

		_ddInfo.file = hfile;
		_ddInfo.total = fsize.QuadPart;
		_ddInfo.sent = 0;
		_ddInfo.img_id = imageId;

		Logv("[SAHARA] File Prepared for download: File Size (%lld) [%d:%s]", _ddInfo.total, imageId, path);
	}

	LONG high = (LONG)(offset >> 32);
	if (::SetFilePointer(_ddInfo.file, (LONG)offset, &high, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
	{
		_callback->ReportGetLastError("[SAHARA] Set File Position Error");
		Exception("[SAHARA] Download File position err ([%s] Offset:%lld, Length:%lld)",
			_imageList[_ddInfo.img_id].c_str(), offset, length);
	}

	AutoMem pMem = ReadContents(_ddInfo.file, length);

	SendRawData(pMem(), pMem.Count());

	_ddInfo.sent += length;
	SaharaProgress(imageId, _ddInfo.sent, _ddInfo.total);
}

void CSaharaProtocol::ProcessHello(TProtocolID aCmd)
{
	if (aCmd == HELLO)
	{
		S_HELLO_REQ& req = _rx.hello_req;
		TErrorCode err = ESAHARA_STATUS_SUCCESS;

		Logv("RX Packet [%s] - Version:%d, Mode:%d, PacketLength:%d", GetPacketName((TProtocolID)req.header.command), req.version, req.mode, req.cmd_packet_length);

		if (req.version < _maxVer || req.version > _maxVer)
		{
			SendReset();
			err = ESAHARA_NAK_PROTOCOL_MISMATCH;
			Exception("[SAHARA] Protocol version is not allowd (rx_min_ver:%d, rx_max_ver:%d, cur_min_ver:%d, cur_max_ver:%d)",
				req.version_supported, req.version, _minVer, _maxVer);
		}

		if (_mode != (TMode)req.mode)
		{
			Logv("[SAHARA] Request Mode: %s, Expected Mode: %s", GetModeName((TMode)req.mode), GetModeName(_mode));
		}

		_maxTargetSize = req.cmd_packet_length;
		
		S_HELLO_RESP& resp = _tx.hello_resp;
		resp.header.command = HELLO_RESP;
		resp.header.length = GetPacketSize(HELLO_RESP);
		resp.version = _maxVer;
		resp.version_supported = _minVer;
		resp.mode = (UINT32)_mode;
		resp.status = err;
		SendPacket();

		_state = WAIT_COMMAND;
	}
	else
	{
		Exception("[SAHARA] WAIT_HELLO state: received undesirable command(%s), then RESET", GetPacketName(aCmd));
		SendReset();
	}

}


void CSaharaProtocol::SendReset()
{
	S_RESET_REQ& req = _tx.reset_req;
	req.header.command = RESET_REQ;
	req.header.length = sizeof(S_RESET_REQ);

	SendPacket();
	_state = WAIT_RESET_RESP;
}

void CSaharaProtocol::SendPacket()
{
	int size = _tx.header.length;

	Logv("[SAHARA] SEND --> [%s]", GetPacketName((TProtocolID)_tx.header.command));

	_port->Write(&_tx, size);
}

BOOL CSaharaProtocol::ReadPacket()
{
	int retry;
	int total = 0, read, packet_length;
	char* p = (char*)&_rx;
	// read header
	for (retry = 0; retry < READ_RETRY_COUNT; retry++)
	{
		read = _port->Read(&p[total], sizeof(S_HEADER) - total);
		if (read > 0)
		{
			total += read;
			if (total == sizeof(S_HEADER))
			{
				if (_rx.header.length <= sizeof(_rx))
				{
					packet_length = GetPacketSize( (TProtocolID)_rx.header.command);
					if (packet_length == _rx.header.length)
					{
						if (packet_length == total)
						{
							Logv("[SAHARA] RECEIVED <-- [%s]", GetPacketName((TProtocolID)_rx.header.command) );
							return TRUE;
						}

						break;
					}
					else
					{
						Exception("[SAHARA] packet size missmatch (rx_cmd:%s, rx_length:%d != packet length:%d)",
							GetPacketName((TProtocolID)_rx.header.command), _rx.header.length, packet_length);
					}
				}
				else
				{
					Exception("[SAHARA] Packet size over (cmd:%s, len:%d)", GetPacketName((TProtocolID)_rx.header.command), _rx.header.length);
				}
			}
		} // if
		::Sleep(READ_WAIT_TIME);
	} // for

	if (retry == READ_RETRY_COUNT)
	{
		Exception("[SAHARA] Packet wait time over. (Max: %d msec)", READ_WAIT_TIME * READ_RETRY_COUNT); // jwoh add GM Signing
		return FALSE;
	}

	// read contents
	for ( ; retry < READ_RETRY_COUNT; retry++)
	{
		read = _port->Read(&p[total], packet_length - total);
		if (read > 0)
		{
			total += read;
			if (total == packet_length)
			{
				Logv("[SAHARA] RECEIVED <-- [%s]", GetPacketName((TProtocolID)_rx.header.command));
				break;
			}
		} // if
		::Sleep(READ_WAIT_TIME);
	} // for

	if (retry == READ_RETRY_COUNT)
	{
		Loge("[SAHARA] Packet wait time over. (Max: %d msec)", READ_WAIT_TIME * READ_RETRY_COUNT);
		return FALSE;
	}
	return TRUE;
}

void CSaharaProtocol::ReadMemoryTable(TProtocolID aCmd)
{
	UINT64 reqLength;
	
	if (aCmd == MEMORY_DEBUG)
	{
		S_MEMORY_READ_REQ& req = _tx.memory_read_req;
		req.header.command = MEMORY_READ_REQ;
		req.header.length = sizeof(S_MEMORY_READ_REQ);
		reqLength = min((_memTable.length - _memTable.read), _maxTargetSize);
		req.memory_length = (UINT32)reqLength;
		req.memory_addr = (UINT32)(_memTable.addr + _memTable.read);

		SendPacket();
	}
	else
	{
		S_MEMORY64_READ_REQ& req = _tx.memory64_read_req;
		req.header.command = MEMORY64_READ_REQ;
		req.header.length = sizeof(S_MEMORY64_READ_REQ);
		reqLength = req.memory_length = min((_memTable.length - _memTable.read), _maxTargetSize);
		req.memory_addr = _memTable.addr + _memTable.read;

		SendPacket();
	}
	Logv("[SAHARA] request memory table (%d/%d)", reqLength, _memTable.length);
	

	if (ReadRawData(&_memTable.table[_memTable.read], reqLength) == FALSE)
	{
		Exception("[SAHARA] Read memory debug table error ");
	}
	_memTable.read += reqLength;

	if (_memTable.read == _memTable.length)
	{
		AnalysisMemoryTable();
	}
}

void CSaharaProtocol::AnalysisMemoryTable()
{
	// 메모리에 대한 처리를 한다.

	//


	// 테이블 정보를 토대로 실제 정보 데이터를 읽어온다.

	int length = 000;
	if (_memInfo.contents != NULL)
	{
		::LocalFree(_memInfo.contents);
		_memInfo.contents = NULL;
	}

	LPSTR pMem = (LPSTR)::LocalAlloc(LPTR, length);
	if (pMem == NULL)
	{
		_callback->ReportGetLastError("[SAHARA] MemoryAllocation Failed in MemoryDebug");
		Exception("[SAHARA] Memory debug table allocation failed");
	}

	if (length == 0)
	{
		Logv("[SAHARA] Complete memory debug");
		_skipReadPacket = TRUE;
		_state = UNKNOWN_STATE;
		return;
	}

	_memInfo.addr = 000;
	_memInfo.length = length;
	_memInfo.read = 0;
	_memInfo.contents = pMem;
	_state = WAIT_MEMORY_REGION;
}

void CSaharaProtocol::ReadMemoryContents(TProtocolID aCmd)
{
	UINT64 reqLength;

	if (aCmd == MEMORY_DEBUG)
	{
		S_MEMORY_READ_REQ& req = _tx.memory_read_req;
		req.header.command = MEMORY_READ_REQ;
		req.header.length = sizeof(S_MEMORY_READ_REQ);
		reqLength = min((_memInfo.length - _memInfo.read), _maxTargetSize);
		req.memory_length = (UINT32)reqLength;
		req.memory_addr = (UINT32)(_memInfo.addr + _memInfo.read);

		SendPacket();
	}
	else
	{
		S_MEMORY64_READ_REQ& req = _tx.memory64_read_req;
		req.header.command = MEMORY64_READ_REQ;
		req.header.length = sizeof(S_MEMORY64_READ_REQ);
		reqLength = req.memory_length = min((_memInfo.length - _memInfo.read), _maxTargetSize);
		req.memory_addr = _memInfo.addr + _memInfo.read;

		SendPacket();
	}
	Logv("[SAHARA] request memory contents (%d/%d)", reqLength, _memInfo.length);



	if (ReadRawData(&_memInfo.contents[_memInfo.read], reqLength) == FALSE)
	{
		Loge("[SAHARA] Read memory debug contents error ");
	}
	_memInfo.read += reqLength;

	if (_memInfo.read == _memInfo.length)
	{
		AnalysisMemoryContents();
		Logv("[SAHARA] Complete memory debug");
		_skipReadPacket = TRUE;
		_state = UNKNOWN_STATE;
	}
}

void CSaharaProtocol::AnalysisMemoryContents()
{
	// 메모리 내용에서 정보를 추출 등 처리를 한다.

	//


	// 처리 완료 후 초기화..
	if (_memInfo.contents != NULL)
	{
		::LocalFree(_memInfo.contents);
		_memInfo.contents = NULL;
	}

	SendReset();
	
}

int CSaharaProtocol::GetPacketSize(TProtocolID aCmd)
{
	switch (aCmd)
	{
	case HELLO: return sizeof(S_HELLO_REQ);
	case HELLO_RESP: return sizeof(S_HELLO_RESP);
	case READ_DATA_REQ: return sizeof(S_READ_DATA_REQ);
	case ENDOFIMAGE_TX: return sizeof(S_ENDOFIMAGE_TX);
	case DONE_REQ: return sizeof(S_DONE_REQ);
	case DONE_RESP: return sizeof(S_DONE_RESP);
	case RESET_REQ: return sizeof(S_RESET_REQ);
	case RESET_RESP: return sizeof(S_RESET_RESP);
	case MEMORY_DEBUG: return sizeof(S_MEMORY_DEBUG);
	case MEMORY_READ_REQ: return sizeof(S_MEMORY_READ_REQ);
	case COMMAND_READY: return sizeof(S_COMMAND_READY);
	case COMMAND_SWITCH_MODE: return sizeof(S_COMMAND_SWITCH_MODE);
	case COMMAND_EXEC_REQ: return sizeof(S_COMMAND_EXEC_REQ);
	case COMMAND_EXEC_RESP: return sizeof(S_COMMAND_EXEC_RESP);
	case COMMAND_EXEC_DATA_REQ: return sizeof(S_COMMAND_EXEC_DATA_REQ);
	case MEMORY64_DEBUG: return sizeof(S_MEMORY64_DEBUG);
	case MEMORY64_READ_REQ: return sizeof(S_MEMORY64_READ_REQ);
	case READ_DATA64_REQ: return sizeof(S_READ_DATA64_REQ);
	}
	
	return - 1;
}

const char* CSaharaProtocol::GetPacketName(TProtocolID aCmd)
{

	switch (aCmd)
	{
	case HELLO: return "HELLO";
	case HELLO_RESP: return "HELLO_RESP";
	case READ_DATA_REQ: return "READ_DATA_REQ";
	case ENDOFIMAGE_TX: return "ENDOFIMAGE_TX";
	case DONE_REQ: return "DONE_REQ";
	case DONE_RESP: return "DONE_RESP";
	case RESET_REQ: return "RESET_REQ";
	case RESET_RESP: return "RESET_RESP";
	case MEMORY_DEBUG: return "MEMORY_DEBUG";
	case MEMORY_READ_REQ: return "MEMORY_READ_REQ";
	case COMMAND_READY: return "COMMAND_READY";
	case COMMAND_SWITCH_MODE: return "COMMAND_SWITCH_MODE";
	case COMMAND_EXEC_REQ: return "COMMAND_EXEC_REQ";
	case COMMAND_EXEC_RESP: return "COMMAND_EXEC_RESP";
	case COMMAND_EXEC_DATA_REQ: return "COMMAND_EXEC_DATA_REQ";
	case MEMORY64_DEBUG: return "MEMORY64_DEBUG";
	case MEMORY64_READ_REQ: return "MEMORY64_READ_REQ";
	case READ_DATA64_REQ: return "READ_DATA64_REQ";
	}

	return ("Unknown Packet !");
}

const char* CSaharaProtocol::GetModeName(TMode aMode)
{
	switch ((TMode)aMode)
	{
	case SAHARA_MODE_IMAGE_TX_PENDING: return "SAHARA_MODE_IMAGE_TX_PENDING";
	case SAHARA_MODE_IMAGE_TX_COMPLETE: return "SAHARA_MODE_IMAGE_TX_COMPLETE";
	case SAHARA_MODE_MEMORY_DEBUG: return "SAHARA_MODE_MEMORY_DEBUG";
	case SAHARA_MODE_COMMAND: return "SAHARA_MODE_COMMAND";
	}

	return "Unknown Mode !";
}

void CSaharaProtocol::TryPseudoHello()
{
	S_HELLO_REQ& req = _rx.hello_req;
	req.header.command = HELLO;
	req.header.length = sizeof(S_HELLO_REQ);
	req.mode = SAHARA_MODE_IMAGE_TX_PENDING;
	req.cmd_packet_length = 1024;
	req.version = 2;
	req.version_supported = 1;
}

const char* GetSatusCode(int aCode)
{
	const char* statusTexts[] = {
		"ESAHARA_STATUS_SUCCESS",
		"ESAHARA_NAK_INVALID_CMD",
		"ESAHARA_NAK_PROTOCOL_MISMATCH",
		"ESAHARA_NAK_INVALID_TARGET_PROTOCOL",
		"ESAHARA_NAK_INVALID_HOST_PROTOCOL",
		"ESAHARA_NAK_INVALID_PACKET_SIZE",
		"ESAHARA_NAK_UNEXPECTED_IMAGE_ID",
		"ESAHARA_NAK_INVALID_HEADER_SIZE",
		"ESAHARA_NAK_INVALID_DATA_SIZE",
		"ESAHARA_NAK_INVALID_IMAGE_TYPE",
		"ESAHARA_NAK_INVALID_TX_LENGTH",
		"ESAHARA_NAK_INVALID_RX_LENGTH",
		"ESAHARA_NAK_GENERAL_TX_RX_ERROR",
		"ESAHARA_NAK_READ_DATA_ERROR",
		"ESAHARA_NAK_UNSUPPORTED_NUM_PHDRS",
		"ESAHARA_NAK_INVALID_PDHR_SIZE",
		"ESAHARA_NAK_MULTIPLE_SHARED_SEG",
		"ESAHARA_NAK_UNINIT_PHDR_LOC",
		"ESAHARA_NAK_INVALID_DEST_ADDR",
		"ESAHARA_NAK_INVALID_IMG_HDR_DATA_SIZE",
		"ESAHARA_NAK_INVALID_ELF_HDR",
		"ESAHARA_NAK_UNKNOWN_HOST_ERROR",
		"ESAHARA_NAK_TIMEOUT_RX",
		"ESAHARA_NAK_TIMEOUT_TX",
		"ESAHARA_NAK_INVALID_HOST_MODE",
		"ESAHARA_NAK_INVALID_MEMORY_READ",
		"ESAHARA_NAK_INVALID_DATA_SIZE_REQUEST",
		"ESAHARA_NAK_MEMORY_DEBUG_NOT_SUPPORTED",
		"ESAHARA_NAK_INVALID_MODE_SWITCH",
		"ESAHARA_NAK_CMD_EXEC_FAILURE",
		"ESAHARA_NAK_EXEC_CMD_INVALID_PARAM",
		"ESAHARA_NAK_EXEC_CMD_UNSUPPORTED",
		"ESAHARA_NAK_EXEC_DATA_INVALID_CLIENT_CMD",
		"ESAHARA_NAK_HASH_TABLE_AUTH_FAILURE",
		"ESAHARA_NAK_HASH_VERIFICATION_FAILURE",
		"ESAHARA_NAK_HASH_TABLE_NOT_FOUND",
	};

	int count = sizeof(statusTexts) / sizeof(char*);

	if (aCode < 0 || aCode >= count)
		return "UNKNOWN_STATUS_CODE";

	return statusTexts[aCode];

}