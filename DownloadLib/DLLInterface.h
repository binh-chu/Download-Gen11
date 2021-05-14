// DownloaderSahara.h

#pragma once


#ifdef DOWNLOADLIB_EXPORTS
#	define SAHARA_API	extern "C" __declspec(dllexport)
#else
#	define SAHARA_API	extern "C" __declspec(dllimport)
#endif


// Callback Handlers for Information
// �α� ������ ���� �ݹ� �Լ�
typedef void (*fnLogHandler)(int aLogLevel, const char* aMessage);
// �ٿ�ε� ���� ���¸� ���� �ݹ� �Լ� (image id�� �����Ѵ�. -1�� ��� ��ü �ٿ�ε� �Ϸ�)
typedef void (*fnDLHandler)(int aImgID, UINT64 aSentBytes, UINT64 aTotalBytes, int aExtraInfo);
typedef void (*fnCmdHandler)(int aCmd, int aResult, void* aData, int* aContinue);
typedef void (*fnMemoryHandler)(int aAddr, int aLength, void* aBuffer);

//
// DLL�� �����ϴ� �������̽� ����
// 
typedef int(*TGetDllVersion)(void);
typedef int(*TRunDownloader)(const char* aPortName, const char* aXmlPath, fnLogHandler aLogHandler, fnDLHandler aProgressHandler);
typedef int(*TRunReadback)	(const char* aPortName, const char* aXmlPath, fnLogHandler aLogHandler, fnDLHandler aProgressHandler);
typedef int(*TDiagRequest)	(const char* aReqCommand, char* aRespBuffer, int aBuffLength, const char* aPortName, fnLogHandler aLogHandler, int aLogLevel);

typedef struct _ITarget
{
	TGetDllVersion		GetDllVersion;
	TRunDownloader		RunDownloader;
	TRunReadback		RunReadback;
	TDiagRequest		DiagRequest;
} ITarget;


//
// DLL�� �������̽� ������ �����Ѵ�. (ITarget*)
// �ܺο��� ���� ȣ��Ǵ� ������ �Լ� 
// �������̽� ����� ��ȯ�Ѵ�.
//
SAHARA_API void*  GetTargetInterface();


//
//  Downloader ���� �����丮
//   - �� ���� ���� �� ����
//  
/// ���϶� ��� �ٿ�ε� ���̺귯�� ���� ���
//  �ʱ� �⺻ ���϶� �������� ���� ���븸 ó���Ǵ� ����. (2016-04-29, elgod)
#define DLL_DOWNLAODER_VERSION_1	100	
//  ���϶� �������ݰ� ���̾�ȣ�� ���� ��� ����� ���� ���� (2016-05-11, elgod)
#define DLL_DOWNLAODER_VERSION_2	101	

//  ���϶�� ���α׷��� �ٿ�ε�, ���̾�ȣ���� �̹����� �ٿ�ε� ���� (2016-05-26, elgod)
#define DLL_DOWNLAODER_VERSION_3	102

//  Diagnostic protocol(ereboot, version info) �߰�, ...  (2016-07-19, elgod)
#define DLL_DOWNLAODER_VERSION_5	105

//  V2�� �°� ���� ����.   (2016-07-21 ~, elgod)
#define DLL_DOWNLAODER_VERSION_6	200

// V2, Micom update�� Diagnostic ó�� �߰� ��.. �߰����� Diag Packet  ó�� ����. (2016-08-04 ~, elgod)
#define DLL_DOWNLAODER_VERSION_7	201

// V2, �ɼ� ����ȭ�� ���� �ɼ� ��� ���� ����. - efs only erase (2016-08-25, elgod)
#define DLL_DOWNLAODER_VERSION_8	202

// EFS clear flag �߰�(Diag Msg) - efs only erase (2016-10-07, elgod)
#define DLL_DOWNLAODER_VERSION_9	203

// EFS clear flag - ó�� ���� ���� ����.
#define DLL_DOWNLAODER_VERSION_10	204

// Normal reboot �� �ȵǴ� ���� �߰� ���(diag cmd)���� ���� ����� ����.
#define DLL_DOWNLAODER_VERSION_11	205

// Micom reset ��� �߰�, ������ ��� ���� ���� �߰�. (2016-11-07)
#define DLL_DOWNLAODER_VERSION_12	206

// Micom reset ��� �ڵ尡 ���� �ʴ� ���� ����. (2016-11-10)
#define DLL_DOWNLAODER_VERSION_13	207

// Fota Erase option ó�� �߰� . (2016-12-16)
#define DLL_DOWNLAODER_VERSION_14	208

// Error level ���� err->info . (2017-02-21)
#define DLL_DOWNLAODER_VERSION_15	209

// response packet�� �ѹ��� 3�� ���ŵ� ��� ó�� . (2017-03-30)
#define DLL_DOWNLAODER_VERSION_16	210

// folder name�� Ư�� Ư�� ���� �ν� ���� &amp; -> &. (2017-05-12)
#define DLL_DOWNLAODER_VERSION_17	211

// Download �Ϸ� �� platform ID üũ. (2017-07-13)
#define DLL_DOWNLAODER_VERSION_18	212

// packet wait time counter ���� 10->20 (2017-09-12)
#define DLL_DOWNLAODER_VERSION_19	213

// micom emergency�� ���� command �߰� (2017-09-21)
#define DLL_DOWNLAODER_VERSION_20	214

// key erase check command �߰� (2018-01-23)
#define DLL_DOWNLAODER_VERSION_21	215

// response packet 2byte ���� üũ  (2018-05-25)
#define DLL_DOWNLAODER_VERSION_22	216

// Demp menu ���� ECC option �߰�  (2018-11-09)
#define DLL_DOWNLAODER_VERSION_23	217

#define DLL_DOWNLAODER_VERSION DLL_DOWNLAODER_VERSION_23
#define CURRENT_VERSION_STRING			"PROTOCOL-v2.16"

