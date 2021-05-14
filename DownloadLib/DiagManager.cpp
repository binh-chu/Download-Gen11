#include "InternalCommon.h"
#include "DiagManager.h"
#include "DiagProtocol.h"
#include "ProtocolException.h"

#include <stdio.h>
#include <stdarg.h>

CDiagManager::CDiagManager(void)
	: _port(GetILog()), _protocol(GetILog())
{
	_logHandler = NULL;
	_logLevel = 0;

	// install handler
	INSTALL_HANDLER(version);
	INSTALL_HANDLER(ereboot);
	INSTALL_HANDLER(nreboot);
	INSTALL_HANDLER(efsbackup);
	INSTALL_HANDLER(efsrecovery);
	INSTALL_HANDLER(micomup); 
	INSTALL_HANDLER(chmode);
}

CDiagManager::~CDiagManager(void)
{
	_port.Close();
	_logHandler = NULL;
}

void CDiagManager::Initialize(const char* aPortName, int aLogLevel)
{
	try
	{
		_logLevel = aLogLevel;

		_port.Open(aPortName);
		_port.SetConfig(115200, 0, 1);

		_protocol.SetPort(&_port);
	}
	catch (CProtocolException& pe)
	{
		this->InvokeException("COM Port:%s, Open Failed ! Reason:%s.", aPortName, pe.What());
	}
	catch (...)
	{
		this->InvokeException("COM Port:%s, Open Failed ! Check that the port is accessble.", aPortName);
	}
}

BOOL CDiagManager::Execute(const char* aReqCommand, char* aResultBuf, int aBufLen)
{
	TMethod method;
	std::string cmd; 
	strMap args;
	std::string result;
	
	Parse(aReqCommand, method, cmd, args);

    if (_handlerList.find(cmd) != _handlerList.end())
    {
		WriteLog(LOGINFO, "Request command:%s", cmd.c_str());
		if((this->*_handlerList[cmd])(method, cmd, args, result))
		{
			if (result.length() > 0)
			{
				memcpy(aResultBuf, result.data(), result.length());
			}
			WriteLog(LOGVERBOSE, "command:%s is executed successfully", cmd.c_str());
			return TRUE;
		}
		WriteLog(LOGINFO, "command:%s request is failed.", cmd.c_str()); // jwoh change log level - error->info
    }
	else
	{
		WriteLog(LOGERROR, "command:%s is not found. check it.", cmd.c_str());
	}
	return FALSE;
}


void CDiagManager::Parse(const char* aReqCommand, TMethod& aMethod, std::string& aCmd, strMap& aArgs)
{
	aMethod = (TMethod)aReqCommand[0];
	if (aMethod != EGet && aMethod != ESet && aMethod != ERun)
		this->InvokeException("Unknown diag command string - invalid method (%c)", (char)aMethod);
		

	strVector inputs;
	if (CSplitter::Tokens(aReqCommand, (char)aMethod, inputs) > 0)
	{
		aCmd = inputs[1];
		if (inputs[2].length() > 0)
		{
			CSplitter::Maps(inputs[2], ESpPair, ESpItem, aArgs);
		}			
	}
	else
	{
		this->InvokeException("Unknown diag command string - cannot seperator items (%s)", aReqCommand);
	}

}

DEFINE_HANDLER(version)
{
	std::string req, resp;
	// request verno_req
	req.clear();
	resp.clear();
	req += (char)DIAG_VERNO_F;

	WriteLog(LOGVERBOSE, "request version_no");

	if (_protocol.Request(req, resp))
	{
		diag_verno_rsp_type* rver = (diag_verno_rsp_type*)resp.data();

		if (rver->mob_firm_rev == 0) // 아직 타겟이 준비 상태가 아닌 경우.
		{
			WriteLog(LOGINFO, "target preparation is not done.");
			return TRUE;
		}

		aResult.append("Compile").append(1, (char)CDiagManager::ESpPair)
			.append(rver->comp_date, sizeof(rver->comp_date)).append(" ")
			.append(rver->comp_time, sizeof(rver->comp_time)).append(1, (char)CDiagManager::ESpItem);
		aResult.append("Release").append(1, (char)CDiagManager::ESpPair)
			.append(rver->rel_date, sizeof(rver->rel_date)).append(" ")
			.append(rver->rel_time, sizeof(rver->rel_time)).append(1, (char)CDiagManager::ESpItem);
		aResult.append("Software_Ver").append(1, (char)CDiagManager::ESpPair)
			.append(rver->ver_dir, sizeof(rver->ver_dir)).append(1, (char)CDiagManager::ESpItem);

		// compile date-time
		//ILog::Print(aResult, "%s%c%s %s%c", "Compile", CDiagManager::ESpPair, rver->comp_date, rver->comp_time, CDiagManager::ESpItem);
		// release date-time
		//ILog::Print(aResult, "%s%c%s %s%c", "Release", CDiagManager::ESpPair, rver->rel_date, rver->rel_time, CDiagManager::ESpItem);
		// sw version
		//ILog::Print(aResult, "%s%c%s%c", "Software_Ver", CDiagManager::ESpPair, rver->ver_dir, CDiagManager::ESpItem);
		// firmware rev
		ILog::Print(aResult, "%s%c%d%c", "Firmware_Rev", CDiagManager::ESpPair, rver->mob_firm_rev, CDiagManager::ESpItem);

		// request miscellineous version information
		req.clear();
		resp.clear();
		req += (char)DIAG_DOWNLOAD_EXT_F;
		req += (char)DIAG_SUBSYS_DOWNLOAD;
		req += (char)DOWNLOAD_VERSION;

		WriteLog(LOGVERBOSE, "request download version");
		if (_protocol.Request(req, resp))
		{
			diag_download_get_ver_rsp_type* dver = (diag_download_get_ver_rsp_type*)resp.data();

			// Hardware revision
			ILog::Print(aResult, "%s%c%s%c", "HW_Rev", CDiagManager::ESpPair, dver->hw_rev, CDiagManager::ESpItem);
			// Hardware revision
			ILog::Print(aResult, "%s%c%d%c", "HW_Area", CDiagManager::ESpPair, dver->hw_area, CDiagManager::ESpItem);
			// MCFG version
			ILog::Print(aResult, "%s%c%s%c", "MCFG_Ver", CDiagManager::ESpPair, dver->mcfg_ver, CDiagManager::ESpItem);

			WriteLog(LOGALWAYS, "Version-Info: %s", aResult.c_str());

			return TRUE;
		}
		else
		{
			WriteLog(LOGINFO, "response download version - error"); // jwoh change log level - error->info
		}
	}
	else
	{
		WriteLog(LOGINFO, "response version_no - error"); // jwoh change log level - error->info
	}


	return FALSE;
}

DEFINE_HANDLER(ereboot)
{
	std::string req, resp;
	// request edl reboot req
	req.clear();
	resp.clear();
	req += (char)DIAG_DOWNLOAD_EXT_F;
	req += (char)DIAG_SUBSYS_DOWNLOAD;
	req += (char)DOWNLOAD_REBOOT;

	WriteLog(LOGVERBOSE, "request reboot edl");

	if (_protocol.Request(req, resp))
	{
		return TRUE;
	}
	else
	{
		WriteLog(LOGINFO, "response reboot edl - error"); // jwoh change log level - error->info
	}

	return FALSE;
}

DEFINE_HANDLER(nreboot)
{
	std::string req, resp;
	// request edl reboot req
	req.clear();
	resp.clear();
	req += (char)DIAG_DOWNLOAD_EXT_F;
	req += (char)DIAG_SUBSYS_DOWNLOAD;
	req += (char)DOWNLOAD_NRESET;

	WriteLog(LOGVERBOSE, "request reboot normal");

	if (_protocol.Request(req, resp))
	{
		return TRUE;
	}
	else
	{
		WriteLog(LOGINFO, "response reboot normal - error"); // jwoh change log level - error->info
	}

	return FALSE;
}

DEFINE_HANDLER(efsrecovery)
{
	const int WAIT_INTERVAL = 200;
	const int MAX_WAIT_COUNT = 10;
	// TMethod aMethod, std::string& aCmd, strMap& aArgs, std::string& aResult
	std::string req, resp;
	// request efs backup
	req.clear();
	resp.clear();
	req += (char)DIAG_SUBSYS_CMD_VER_2_F;
	req += (char)DIAG_SUBSYS_EFSBACKUP;
	req += (char)8; //sysbsys cmd code (2 bytes)
	req += (char)0;

	WriteLog(LOGVERBOSE, "request EFS recovery");

	if (_protocol.Request(req, resp))
	{
		diag_efs_backup_resp_type* rver = (diag_efs_backup_resp_type*)resp.data();
		if (rver->status == 0)
		{
			WriteLog(LOGINFO, "response EFS recovery - Resp status error"); // jwoh change log level - error->info
			return FALSE;
		}

		return TRUE;

	}
	else
	{
		WriteLog(LOGINFO, "response EFS recovery - Protocol error"); // jwoh change log level - error->info
	}

	return FALSE;
}

DEFINE_HANDLER(efsbackup)
{
	const int WAIT_INTERVAL = 200;
	const int MAX_WAIT_COUNT = 10;
	// TMethod aMethod, std::string& aCmd, strMap& aArgs, std::string& aResult
	std::string req, resp;
	// request efs backup
	req.clear();
	resp.clear();
	req += (char)DIAG_SUBSYS_CMD_VER_2_F;
	req += (char)DIAG_SUBSYS_EFSBACKUP;
	req += (char)0; //sysbsys cmd code (2 bytes)
	req += (char)0;

	WriteLog(LOGVERBOSE, "request EFS backup");

	if (_protocol.Request(req, resp))
	{
		diag_efs_backup_resp_type* rver = (diag_efs_backup_resp_type*)resp.data();
		if (rver->resp_count == 0 && rver->status != 0)
		{
			WriteLog(LOGINFO, "response EFS backup - Resp status error"); // jwoh change log level - error->info
			return FALSE;
		}

		//  완료 시까지 읽는다.
		int retry = 0;
		do {
			resp.clear();
			if (_protocol.Receive(req[0], req[1], resp))
			{
				rver = (diag_efs_backup_resp_type*)resp.data();

				if (rver->resp_count > 0 && rver->status == 0)
				{
					return TRUE;
				}
				else if (rver->resp_count > 0 && rver->status != 0)
				{
					WriteLog(LOGINFO, "response EFS backup - FAILED "); // jwoh change log level - error->info
					break;
				}
			}
			
			::Sleep(WAIT_INTERVAL);

		} while (++retry < MAX_WAIT_COUNT);

		if (retry >= MAX_WAIT_COUNT)
			WriteLog(LOGINFO, "response EFS backup - Timeout error"); // jwoh change log level - error->info
			
	}
	else
	{
		WriteLog(LOGINFO, "response EFS backup - Protocol error"); // jwoh change log level - error->info
	}

	return FALSE;
}

// start update
DEFINE_HANDLER(micomup)
{
	// TMethod aMethod, std::string& aCmd, strMap& aArgs, std::string& aResult
	std::string req, resp;
	if (aArgs.find("cmd") == aArgs.end())
	{
		WriteLog(LOGINFO, "MicomUpdate - diag command format is invalid (not found 'cmd')."); // jwoh change log level - error->info
		return FALSE;
	}
	const char* cmd = aArgs["cmd"].c_str();
	char reqCmd = 0;
	if (strcmp(cmd, "up1") == 0)
		reqCmd = MICOM_FIRST_UPDATE;
	else if (strcmp(cmd, "up2") == 0)
		reqCmd = MICOM_SECOND_UPDATE;
	else if (strcmp(cmd, "upall") == 0)
		reqCmd = MICOM_ALL_UPDATE;
	else if (strcmp(cmd, "check") == 0)
		reqCmd = MICOM_UPDATE_STATUS;
	else if (strcmp(cmd, "result") == 0)
		reqCmd = MICOM_RESULT_STATUS;
	else if (strcmp(cmd, "rr2normal") == 0)
		reqCmd = MICOM_RR_TO_NORMAL;
	else if (strcmp(cmd, "normal2rr") == 0)
		reqCmd = MICOM_NORMAL_TO_RR;
	else if (strcmp(cmd, "mode") == 0)
		reqCmd = MICOM_READ_MODE;
	else if (strcmp(cmd, "reset") == 0)
		reqCmd = MICOM_RESET;
	else if (strcmp(cmd, "endcmd") == 0)
		reqCmd = MICOM_CMD_END;
	else if (strcmp(cmd, "mkeyerase") == 0) // jwoh add GB key erase function
		reqCmd = MICOM_MKEY_ERASE;
	else if (strcmp(cmd, "mkeyerasecheck") == 0) // jwoh add GB key erase function
		reqCmd = MICOM_MKEY_ERASE_CHECK;
	else
	{
		WriteLog(LOGINFO, "MicomUpdate - diag command format is invalid (unknown cmd type '%s').", cmd); // jwoh change log level - error->info
		return FALSE;
	}

	// request micom update
	req.clear();
	resp.clear();
	req += (char)DIAG_MICOM_F;
	req += reqCmd;
	req.append(6, (char)0);

	WriteLog(LOGINFO, "request Micom Update (req=%s)", cmd);

	if (_protocol.Request(req, resp))
	{
		if (resp.length() <  req.length())
		{
			WriteLog(LOGINFO, "request Micom Update (req=%s) - Response length is less than format size(%d)", cmd, resp.length()); // jwoh change log level - error->info
			return FALSE;
		}
		char result = resp[7];
		// response result

		if (resp.length() == 10 && resp[7] == 1) // jwoh add [
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[7], CDiagManager::ESpItem);
		}
		else if (resp.length() == 20 && resp[17] == 1)
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[17], CDiagManager::ESpItem);
		}
		else if (resp.length() == 30 && resp[27] == 1)
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[27], CDiagManager::ESpItem);
		} // jwoh add ]
		else
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[7], CDiagManager::ESpItem);
		}

		WriteLog(LOGINFO, "request Micom Update (req=%s), (result=%s)", cmd, aResult.c_str());
		return TRUE;
	}
	else
	{
		WriteLog(LOGINFO, "response Micom Update (req=%s) - Protocol error", cmd); // jwoh change log level - error->info
	}

	return FALSE;
}

// change debug mode
DEFINE_HANDLER(chmode)
{
	// TMethod aMethod, std::string& aCmd, strMap& aArgs, std::string& aResult
	std::string req, resp;
	if (aArgs.find("cmd") == aArgs.end())
	{
		WriteLog(LOGINFO, "DebugModeChange - diag command format is invalid (not found 'cmd')."); // jwoh change log level - error->info
		return FALSE;
	}
	const char* cmd = aArgs["cmd"].c_str();
	char reqCmd = 0;
	if (strcmp(cmd, "dbgon") == 0)
		reqCmd = MDM_DEBUGMODE_ON;
	else if (strcmp(cmd, "dbgoff") == 0)
		reqCmd = MDM_DEBUGMODE_OFF;
	else if (strcmp(cmd, "platformid") == 0) // jwoh platfom id check
		reqCmd = MDM_PLATFORMID_CHECK;
	else if (strcmp(cmd, "check") == 0)
		reqCmd = MDM_DEBUGMODE_STATUS;
	else
	{
		WriteLog(LOGINFO, "DebugModeChange - diag command format is invalid (unknown cmd type '%s').", cmd); // jwoh change log level - error->info
		return FALSE;
	}

	// request change debug mode
	req.clear();
	resp.clear();
	req += (char)DIAG_DEBUG_MODE_F;
	req += reqCmd;
	req.append(6, (char)0);

	WriteLog(LOGINFO, "request DebugModeChange (req=%s)", cmd);

	if (_protocol.Request(req, resp))
	{
		if (resp.length() <  req.length())
		{
			WriteLog(LOGINFO, "response DebugModeChange Error - Response length is less than format size(%d)", resp.length()); // jwoh change log level - error->info
			return FALSE;
		}
		char result = resp[7];
		// response result
		if (resp.length() == 10 && resp[7] == 1) // jwoh add [
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[7], CDiagManager::ESpItem);
		}
		else if (resp.length() == 20 && resp[17] == 1)
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[17], CDiagManager::ESpItem);
		}
		else if (resp.length() == 30 && resp[27] == 1)
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[27], CDiagManager::ESpItem);
		} // jwoh add ]
		else
		{
			ILog::Print(aResult, "%s%c%d%c", "status", CDiagManager::ESpPair, resp[7], CDiagManager::ESpItem);
		}

		WriteLog(LOGINFO, "request debug mode (req=%s), (result=%s)", cmd, aResult.c_str());

		return TRUE;
	}
	else
	{
		WriteLog(LOGINFO, "response DebugMode change - Protocol error"); // jwoh change log level - error->info
	}

	return FALSE;
}

