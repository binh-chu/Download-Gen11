#pragma once

#include "InternalCommon.h"

#include <string>

class CProtocolException
{
public:
	CProtocolException()
	{
		_msg.append("<예외>");
	}

	CProtocolException(const char* aMsg)
	{
		_msg.append("<예외>");
		this->SetMsg(aMsg);
	}

	virtual ~CProtocolException()
	{
	}

	const char* What() const
	{
		return _msg.c_str();
	}

public:
	static void Throw(const char * aMsg)
	{
		CProtocolException exp;
		exp._msg.append(aMsg);
		
		throw exp;
	}
private:
	void SetMsg(const char* aMsg)
	{
		_msg.append(aMsg);
	}

	std::string _msg;
};

