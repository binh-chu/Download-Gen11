#pragma once

#include <string>
#include <sstream>
#include <vector>
#include <map>

typedef std::vector<std::string> strVector;
typedef std::map<std::string, std::string> strMap;

class CSplitter
{
public:
	static int Tokens(const char* aInput, char aSeperator, strVector& aOutput)
	{
		std::string ins(aInput);

		return Tokens(ins, aSeperator, aOutput);
	}

	static int Tokens(const std::string& aInput, char aSeperator, strVector& aOutput)
	{
		std::stringstream stream(aInput);
		std::string word;
		while(std::getline(stream, word, aSeperator))
		{
			aOutput.push_back(word);
		}

		return aOutput.size();
	}

	static int Maps(const std::string& aInput, char aPairSeperator, char aItemSeperator, strMap& aOutput)
	{
		strVector items;
		Tokens(aInput, aItemSeperator, items);
		for (strVector::iterator it = items.begin(); it != items.end(); it++)
		{
			if (it->length() > 0)
			{
				strVector item;
				if (Tokens(*it, aPairSeperator, item) == 2)
				{
					aOutput[item[0]] = item[1];
				}
			}
		}
		return aOutput.size();
	}
	
};
