// DownloaderSahara.h

#pragma once


#ifdef DOWNLOADLIB_EXPORTS
#	define SAHARA_API	extern "C" __declspec(dllexport)
#else
#	define SAHARA_API	extern "C" __declspec(dllimport)
#endif


// Callback Handlers for Information
// 로그 정보를 받을 콜백 함수
typedef void (*fnLogHandler)(int aLogLevel, const char* aMessage);
// 다운로드 진행 상태를 받을 콜백 함수 (image id로 구분한다. -1은 경우 전체 다운로드 완료)
typedef void (*fnDLHandler)(int aImgID, UINT64 aSentBytes, UINT64 aTotalBytes, int aExtraInfo);
typedef void (*fnCmdHandler)(int aCmd, int aResult, void* aData, int* aContinue);
typedef void (*fnMemoryHandler)(int aAddr, int aLength, void* aBuffer);

//
// DLL이 제공하는 인터페이스 정보
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
// DLL의 인터페이스 정보를 리턴한다. (ITarget*)
// 외부에서 직접 호출되는 유일한 함수 
// 인터페이스 목록을 반환한다.
//
SAHARA_API void*  GetTargetInterface();


//
//  Downloader 버젼 히스토리
//   - 각 버젼 정보 및 내용
//  
/// 사하라 기반 다운로드 라이브러리 버젼 명기
//  초기 기본 사하라 프로토콜 스펙 내용만 처리되는 버젼. (2016-04-29, elgod)
#define DLL_DOWNLAODER_VERSION_1	100	
//  사하라 프로토콜과 파이어호스 같이 사용 고려한 버젼 정의 (2016-05-11, elgod)
#define DLL_DOWNLAODER_VERSION_2	101	

//  사하라로 프로그래머 다운로드, 파이어호스로 이미지들 다운로드 구현 (2016-05-26, elgod)
#define DLL_DOWNLAODER_VERSION_3	102

//  Diagnostic protocol(ereboot, version info) 추가, ...  (2016-07-19, elgod)
#define DLL_DOWNLAODER_VERSION_5	105

//  V2에 맞게 시작 지점.   (2016-07-21 ~, elgod)
#define DLL_DOWNLAODER_VERSION_6	200

// V2, Micom update용 Diagnostic 처리 추가 등.. 추가적인 Diag Packet  처리 구현. (2016-08-04 ~, elgod)
#define DLL_DOWNLAODER_VERSION_7	201

// V2, 옵션 간소화에 따른 옵션 기능 변경 적용. - efs only erase (2016-08-25, elgod)
#define DLL_DOWNLAODER_VERSION_8	202

// EFS clear flag 추가(Diag Msg) - efs only erase (2016-10-07, elgod)
#define DLL_DOWNLAODER_VERSION_9	203

// EFS clear flag - 처리 에러 수정 버젼.
#define DLL_DOWNLAODER_VERSION_10	204

// Normal reboot 잘 안되는 것을 추가 명령(diag cmd)으로 새로 적용된 버젼.
#define DLL_DOWNLAODER_VERSION_11	205

// Micom reset 명령 추가, 마이컴 명령 종료 전달 추가. (2016-11-07)
#define DLL_DOWNLAODER_VERSION_12	206

// Micom reset 명령 코드가 맞지 않는 문제 수정. (2016-11-10)
#define DLL_DOWNLAODER_VERSION_13	207

// Fota Erase option 처리 추가 . (2016-12-16)
#define DLL_DOWNLAODER_VERSION_14	208

// Error level 변경 err->info . (2017-02-21)
#define DLL_DOWNLAODER_VERSION_15	209

// response packet이 한번에 3개 수신될 경우 처리 . (2017-03-30)
#define DLL_DOWNLAODER_VERSION_16	210

// folder name에 특정 특수 문자 인식 문제 &amp; -> &. (2017-05-12)
#define DLL_DOWNLAODER_VERSION_17	211

// Download 완료 후 platform ID 체크. (2017-07-13)
#define DLL_DOWNLAODER_VERSION_18	212

// packet wait time counter 변경 10->20 (2017-09-12)
#define DLL_DOWNLAODER_VERSION_19	213

// micom emergency를 위해 command 추가 (2017-09-21)
#define DLL_DOWNLAODER_VERSION_20	214

// key erase check command 추가 (2018-01-23)
#define DLL_DOWNLAODER_VERSION_21	215

// response packet 2byte 까지 체크  (2018-05-25)
#define DLL_DOWNLAODER_VERSION_22	216

// Demp menu 에서 ECC option 추가  (2018-11-09)
#define DLL_DOWNLAODER_VERSION_23	217

#define DLL_DOWNLAODER_VERSION DLL_DOWNLAODER_VERSION_23
#define CURRENT_VERSION_STRING			"PROTOCOL-v2.16"

