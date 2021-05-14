#pragma once

#include "DownloadBase.h"
#include "sahara_def.h"

#include <map>
#include <string>
class CSaharaProtocol : public CDownloadBase
{
public:
	CSaharaProtocol(IDownloadCallback* aCallback);
	virtual ~CSaharaProtocol();

	virtual void Initialize(CComPort* aPort, tinyxml2::XmlElement* aXmlProtocol);
	virtual void Download();

	void Release();
	void StartDownload();
	void StartMemoryDebug();
	void StartCommand(int aCmd, void* aResultBuf, int aBufLength);
	void AddDownloadMap(int aImageId, const char* aFilePath);
private:
	void ProcessXml();
	void StateMachine();
	BOOL ReadPacket();
	void SendPacket();
	void SendReset();
	void ReadMemoryTable(TProtocolID aCmd);
	void AnalysisMemoryTable();
	void ReadMemoryContents(TProtocolID aCmd);
	void AnalysisMemoryContents();

	static int GetPacketSize(TProtocolID aCmd);
	static const char* GetPacketName(TProtocolID aCmd);
	static const char* GetModeName(TMode aMode);

	void ProcessCmdData();
	void ProcessCmdResponse(TProtocolID aCmd);
	void ProcessCommand();
	void ProcessMemoryDebug(TProtocolID aCmd);
	void ProcessDoneResp();
	void ProcessEndOfImage();
	void ProcessDownload(TProtocolID aCmd);
	void ProcessHello(TProtocolID aCmd);

	void TryPseudoHello();
private:
	UINT _minVer;
	UINT _maxVer;
	TMode _mode;
	TState _state;
	TPacket _rx;
	TPacket _tx;
	int _maxTargetSize;
	BOOL _skipReadPacket;

	// download information
	struct _TDownloadInfo {
		int img_id;
		HANDLE file;
		UINT64 sent;
		UINT64 total;
	} _ddInfo;
	// memory table information
	struct _TMemoryTableInfo {
		UINT64 addr;
		UINT64 length;
		UINT64 read;
		LPSTR  table;
		int    cmd;
	} _memTable;

	// memory table information
	struct _TMemoryContentsInfo {
		UINT64 addr;
		UINT64 length;
		UINT64 read;
		LPSTR  contents;
	} _memInfo;

	// memory table information
	struct _TCommandResult {
		UINT cmd;
		LPSTR rbuf;
		UINT rlength; //  for rbuf
		UINT dlength; // for received result
		UINT read;
	} _cmdResult;

	std::map<int, std::string> _imageList;
};
