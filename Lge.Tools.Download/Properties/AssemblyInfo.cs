using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using Lge.Tools.Download;

// 어셈블리에 대한 일반 정보는 다음 특성 집합을 통해 
// 제어됩니다. 어셈블리와 관련된 정보를 수정하려면
// 이러한 특성 값을 변경하세요.
[assembly: AssemblyTitle("Lge.Tools.Download")]
[assembly: AssemblyDescription("GEN11 multi-downloader tool")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("LG Electronics")]
[assembly: AssemblyProduct("Lge.Tools.Download")]
[assembly: AssemblyCopyright("Copyright © LGE  2016")]
[assembly: AssemblyTrademark("LGE")]
[assembly: AssemblyCulture("")]
[assembly: CustomProject("GEN-11, Telematics.")]

// ComVisible을 false로 설정하면 이 어셈블리의 형식이 COM 구성 요소에 
// 표시되지 않습니다.  COM에서 이 어셈블리의 형식에 액세스하려면 
// 해당 형식에 대해 ComVisible 특성을 true로 설정하세요.
[assembly: ComVisible(false)]

//지역화 가능 응용 프로그램 빌드
//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //테마별 리소스 사전의 위치
                                     //(페이지, 앱 또는 모든 테마별 리소스 사전에 
                                     // 리소스가 없는 경우에 사용됨)
    ResourceDictionaryLocation.SourceAssembly //제네릭 리소스 사전의 위치
                                              //(페이지, 앱 또는 모든 테마별 리소스 사전에 
                                              // 리소스가 없는 경우에 사용됨)
)]
// Fixed to distinguish lge signed from gm signed
[assembly: AssemblyVersion("2.3.0.0")]
[assembly: AssemblyFileVersion("2.3.40")]
[assembly: CustomVesionDate("July 11, 2019")]
[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Fixed a error version parsing
//[assembly: AssemblyVersion("2.2.9.0")]
//[assembly: AssemblyFileVersion("2.3.39")]
//[assembly: CustomVesionDate("Jan 30, 2019")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Added ECC option in Dump menu.
//[assembly: AssemblyVersion("2.2.8.0")]
//[assembly: AssemblyFileVersion("2.3.38")]
//[assembly: CustomVesionDate("Nov 9, 2018")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Fixed to check response packet up to 2nd byte.
// Fixed to retry in case of response error to "end command".
// The code for checking the USB path has been fixed.
//[assembly: AssemblyVersion("2.2.7.0")]
//[assembly: AssemblyFileVersion("2.3.37")]
//[assembly: CustomVesionDate("May 25, 2018")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Modified to update the task list when selecting the platform ID.
//[assembly: AssemblyVersion("2.2.6.0")]
//[assembly: AssemblyFileVersion("2.3.36")]
//[assembly: CustomVesionDate("Feb 28, 2018")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Changed to delay time 7sec before key erase step.
//[assembly: AssemblyVersion("2.2.5.0")]
//[assembly: AssemblyFileVersion("2.3.35")]
//[assembly: CustomVesionDate("Feb 02, 2018")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Added Baudrate information popup.
// Added Platform ID selection menu.
//[assembly: AssemblyVersion("2.2.4.0")]
//[assembly: AssemblyFileVersion("2.3.34")]
//[assembly: CustomVesionDate("Jan 25, 2018")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Added command for key provisioning check. (MY21 GB)
//[assembly: AssemblyVersion("2.2.3.9")]
//[assembly: AssemblyFileVersion("2.3.33")]
//[assembly: CustomVesionDate("Jan 23, 2018")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Backup partition overwrite issue during image dump has been fixed.
// The download log file has been modified to save only one month.
//[assembly: AssemblyVersion("2.2.3.8")]
//[assembly: AssemblyFileVersion("2.3.32")]
//[assembly: CustomVesionDate("Jan 12, 2018")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// Added baudrate setting menu at emergency download and CCM dump.
//[assembly: AssemblyVersion("2.2.3.7")]
//[assembly: AssemblyFileVersion("2.3.31")]
//[assembly: CustomVesionDate("Nov 03, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Added "Edit" button in Emergency Download tab
// 2. Key erase option is always true.
//[assembly: AssemblyVersion("2.2.3.6")]
//[assembly: AssemblyFileVersion("2.3.30")]
//[assembly: CustomVesionDate("Oct 26, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Save the 'Key Erase(GB Only)' option
// 2. Added 'CCM Dump' Tab
//[assembly: AssemblyVersion("2.2.3.5")]
//[assembly: AssemblyFileVersion("2.3.29")]
//[assembly: CustomVesionDate("Sep 22, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

//1. Added download tool option for secure boot by GM signing.
//[assembly: AssemblyVersion("2.2.3.4")]
//[assembly: AssemblyFileVersion("2.3.28")]
//[assembly: CustomVesionDate("Sep 12, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Fixed not to delete FOTA partition when downloading CCM.
//[assembly: AssemblyVersion("2.2.3.3")]
//[assembly: AssemblyFileVersion("2.3.27")]
//[assembly: CustomVesionDate("Sep 05, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Added job of platformID check, After normal reboot. - Factory mode
//[assembly: AssemblyVersion("2.2.3.2")]
//[assembly: AssemblyFileVersion("2.3.26")]
//[assembly: CustomVesionDate("July 28, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Master key / unlock key erase issue.
//[assembly: AssemblyVersion("2.2.3.1")]
//[assembly: AssemblyFileVersion("2.3.25")]
//[assembly: CustomVesionDate("July 18, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. After downloading, it does not change to debug mode. - factory mode
// 2. After changing to debug mode, progress bar is displayed in green. - factory mode
// 3. Factory menu option remove in emergency download tab.
//[assembly: AssemblyVersion("2.2.3.0")]
//[assembly: AssemblyFileVersion("2.3.24")]
//[assembly: CustomVesionDate("July 13, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. After the com port is connected, it changes to debug mode. - Factory mode
//[assembly: AssemblyVersion("2.2.2.9")]
//[assembly: AssemblyFileVersion("2.3.23")]
//[assembly: CustomVesionDate("July 07, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Changed download order for 'debug off'
// 2. Changed download log level.
//[assembly: AssemblyVersion("2.2.2.8")]
//[assembly: AssemblyFileVersion("2.3.22")]
//[assembly: CustomVesionDate("Jun 22, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. After nomal reboot, version information checking.
// 2. When 'Edit' button click, List up partition name. - emergency tab
//[assembly: AssemblyVersion("2.2.2.7")]
//[assembly: AssemblyFileVersion("2.3.21")]
//[assembly: CustomVesionDate("May 31, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Added "CCM PCB Only" hidden menu
//[assembly: AssemblyVersion("2.2.2.6")]
//[assembly: AssemblyFileVersion("2.3.20")]
//[assembly: CustomVesionDate("May 27, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Error recognizing '&' in file path.
// 2. When 'Edit' button click, List up partition name.
//[assembly: AssemblyVersion("2.2.2.5")]
//[assembly: AssemblyFileVersion("2.3.19")]
//[assembly: CustomVesionDate("May 12, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Save the log level
//[assembly: AssemblyVersion("2.2.2.4")]
//[assembly: AssemblyFileVersion("2.3.18")]
//[assembly: CustomVesionDate("May 02, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. signed image를 위한 'prog_nand_firehose_9x45.mbn' 파일 변경
// 2. micom update1, micom update2 단계에서 retry시 오류 수정
//[assembly: AssemblyVersion("2.2.2.3")]
//[assembly: AssemblyFileVersion("2.3.17")]
//[assembly: CustomVesionDate("April 26, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Changed the wait time for each task.
// 2. When the download fail in the micom update1, retry micom update1 step
// 3. When the download fail in the micom update2, retry micom update2 step
//[assembly: AssemblyVersion("2.2.2.2")]
//[assembly: AssemblyFileVersion("2.3.16")]
//[assembly: CustomVesionDate("April 07, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Changed download order for 'backup&restore'
// 2. when response packet data size is exceeded, exception handling
// 3. 3. Fixed error in retry processing for debug mode chaning.
//[assembly: AssemblyVersion("2.2.2.1")]
//[assembly: AssemblyFileVersion("2.3.15")]
//[assembly: CustomVesionDate("March 30, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Changed 'prog_nand_firehose_9x45.mbn' file.
// 2. Added information popup when download fails.
// 3. Added waiting time when changing debug mode.
//[assembly: AssemblyVersion("2.2.2.0")]
//[assembly: AssemblyFileVersion("2.3.14")]
//[assembly: CustomVesionDate("March 23, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Changed 'prog_nand_firehose_9x45.mbn' file for non-signed / signed images
// 2. When switching fail to RR mode in update1, recheck micom mode.
// 3. When switching fail to RR mode in update2, recheck micom mode.
// 4. Fixed message box popup isssue on dual monitor.
//[assembly: AssemblyVersion("2.2.1.9")]
//[assembly: AssemblyFileVersion("2.3.13")]
//[assembly: CustomVesionDate("March 14, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Added popup message box on fail during debug off sequence
// 2. When switching fail to RR mode in update1, exception handling
// 3. When switching fail to RR mode in update2, exception handling
// 4. changed the transmission code of 'debug on'
//[assembly: AssemblyVersion("2.2.1.8")]
//[assembly: AssemblyFileVersion("2.3.12")]
//[assembly: CustomVesionDate("February 27, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// 1. Remove 'FOTA Erase' button in hidden menu
// 2. Changed Error log level
//[assembly: AssemblyVersion("2.2.1.7")]
//[assembly: AssemblyFileVersion("2.3.11")]
//[assembly: CustomVesionDate("February 21, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

// backup&restore시 FOTA 이미지 erase
// GB Master key / Unlock key Remove - Hidden Menu 추가
// Reboot EDL 후 Normal booting 시 Reboot EDL 다시 시도
//[assembly: AssemblyVersion("2.2.1.6")]
//[assembly: AssemblyFileVersion("2.3.10")]
//[assembly: CustomVesionDate("February 16, 2017")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

//// Fota erase  옵션 및 처리 추가.
//[assembly: AssemblyVersion("2.2.1.5")]
//[assembly: AssemblyFileVersion("2.3.9")]
//[assembly: CustomVesionDate("December 16, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

//// Normal Page 제거, Theme 제거, 완료 창에 Animation bubles 추가. 
//[assembly: AssemblyVersion("2.2.1.4")]
//[assembly: AssemblyFileVersion("2.3.8")]
//[assembly: CustomVesionDate("November 15, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

//// Image update Only: MICOM이 RR모드일 경우, 에러 처리. Micom Update일 경우, RR모드에서는 debug on 생략.
//[assembly: AssemblyVersion("2.2.1.3")]
//[assembly: AssemblyFileVersion("2.3.7")]
//[assembly: CustomVesionDate("November 10, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

//// Lge_diag에 명령 처리 후, 항상 종료 명령 전달 추가, 마이컴 리셋 추가.
//[assembly: AssemblyVersion("2.2.1.2")]
//[assembly: AssemblyFileVersion("2.3.6")]
//[assembly: CustomVesionDate("November 7, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.09i")] // lge_diag: micom reset, command reset 추가

//// Lge_diag에 명령 처리 후, 항상 종료 명령 전달 추가, 마이컴 리셋 추가.
//[assembly: AssemblyVersion("2.2.1.102")]
//[assembly: AssemblyFileVersion("2.3.5")]
//[assembly: CustomVesionDate("November 7, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.03")] // PowerSupply 시, debug-on에서 suspend 진입 안 되게 함.

//// 포트 정보가 있어도 다시 가져와서 추가하도록 수정, 기타 diag command 타임아웃 모두 60% 이상 늘림.
//[assembly: AssemblyVersion("2.2.0.9")]
//[assembly: AssemblyFileVersion("2.3.5")]
//[assembly: CustomVesionDate("November 4, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.03")] // PowerSupply 시, debug-on에서 suspend 진입 안 되게 함.

//// 멀티 타겟의 경우, 포트 정보(PATH)가 없어서 처리 못하는 문제 => 백그라운드에서 재시도 계속 하도록 수정. 
//[assembly: AssemblyVersion("2.2.0.8")]
//[assembly: AssemblyFileVersion("2.3.5")]
//[assembly: CustomVesionDate("November 4, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.03")] // PowerSupply 시, debug-on에서 suspend 진입 안 되게 함.

//// Diag response : 응답이 없거나 문제가 생겨도 항상 최소 10회 이상 재시도 하도록 수정.(타겟이 문제 생겨도 일정 시간은 다시 요청)
//[assembly: AssemblyVersion("2.2.0.7")]
//[assembly: AssemblyFileVersion("2.3.5")]
//[assembly: CustomVesionDate("November 2, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.03")] // PowerSupply 시, debug-on에서 suspend 진입 안 되게 함.

//// Micom test version - Port timeout, version check timeout 증가
//[assembly: AssemblyVersion("2.2.0.6")]
//[assembly: AssemblyFileVersion("2.3.5")]
//[assembly: CustomVesionDate("November 1, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.02")] // download stuck 현상 제거 (일부 경우는 발생)
//[assembly: CustomRequiredCcmVersion("v2.03")] // PowerSupply 시, debug-on에서 suspend 진입 안 되게 함.

//// Update-1,2 timeout increated version
//[assembly: AssemblyVersion("2.2.0.5")]
//[assembly: AssemblyFileVersion("2.3.4")]
//[assembly: CustomVesionDate("October 31, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.01")]
//[assembly: CustomRequiredCcmVersion("v2.01")]


// Micom 업데이트용 검증 버젼
//[assembly: AssemblyVersion("2.2.0.401")]
//[assembly: AssemblyFileVersion("2.3.3")]
//[assembly: CustomVesionDate("October 29, 2016")]
//[assembly: CustomRequiredMicomVersion("v2.01")]
//[assembly: CustomRequiredCcmVersion("v2.01")]
