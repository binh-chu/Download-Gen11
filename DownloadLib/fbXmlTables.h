// don't include this header file.

#include "AutoMem.h"
#include "firehose_def.h"
#include <stdio.h>
//
// Firehose protocol pre-defined xml string tables
//

const char* XML_CONFIGURE =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<configure MemoryName=\"%s\" Verbose=\"%d\" AlwaysValidate=\"%d\""
			" MaxDigestTableSizeInBytes=\"%d\" MaxPayloadSizeToTargetInBytes=\"%d\""
			" ZlpAwareHost=\"%d\" SkipStorageInit=\"%d\" TargetName=\"%s\" />\n"
"</data>\n";

static void GetXmlConfigure(AutoMem& aMem, const char* aMemoryName, const char* aTargetName,
	int aPayloadSizeToTarget, int aVerbose, int aSkipStoreageInit,
	int aZipAwareHost = CONF_WINDOWS_ZlpAwareHost,
	int aMaxDigestTableSize = CONF_DEFAULT_MaxDigestTableSizeInBytes,
	int aAlwaysValidate = CONF_DEFAULT_AlwaysValidate)
{
	sprintf_s(aMem(), aMem.Size(), XML_CONFIGURE, aMemoryName, aVerbose, aAlwaysValidate,
					aMaxDigestTableSize, aPayloadSizeToTarget, aZipAwareHost,
					aSkipStoreageInit, aTargetName);
	aMem.setCount(strlen(aMem()));
}

const char* XML_PROGRAM =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<program PAGES_PER_BLOCK=\"%d\" filename=\"%s\" SECTOR_SIZE_IN_BYTES=\"%d\" num_partition_sectors=\"%d\""
" physical_partition_number=\"%d\" start_sector=\"%d\"/>\n"
"</data>\n";

static void GetXmlProgram(AutoMem& aMem, int aBlockPages, const char* aFileName, int aSectorSize, int aSectorCount, int aPartition, int aStartSector)
{
	sprintf_s(aMem(), aMem.Size(), XML_PROGRAM, aBlockPages,aFileName, aSectorSize, aSectorCount, aPartition, aStartSector);
	aMem.setCount(strlen(aMem()));
}

const char* XML_PROGRAM_SIMPLE =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"%s\n"
"</data>\n";

static void GetXmlProgramSimple(AutoMem& aMem, const char* program)
{
	sprintf_s(aMem(), aMem.Size(), XML_PROGRAM_SIMPLE, program);
	aMem.setCount(strlen(aMem()));
}

const char* XML_RESET =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>"
"<power value=\"reset\" DelayInSeconds=\"%d\" />"
"</data>\n";

static void GetXmlReset(AutoMem& aMem, int aDelaySeconds = 2)
{
	sprintf_s(aMem(), aMem.Size(), XML_RESET, aDelaySeconds);
	aMem.setCount(strlen(aMem()));
}

const char* XML_ERASE =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<erase start_sector=\"%d\" num_partition_sectors=\"%d\" />\n"
"</data>\n";

const char* XML_ERASE_ALL =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<erase start_sector=\"0\" num_partition_sectors=\"0xFFFFFFFF\" />\n"
"</data>\n";

static void GetXmlErase(AutoMem& aMem, int aStartSector = 0, int aSectorCount = 0xFFFFFFFF)
{
	if (aStartSector == 0 && aSectorCount == 0xFFFFFFFF)
		sprintf_s(aMem(), aMem.Size(), XML_ERASE_ALL);
	else
		sprintf_s(aMem(), aMem.Size(), XML_ERASE, aStartSector, aSectorCount);
	aMem.setCount(strlen(aMem()));
}

const char* XML_READ =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<read SECTOR_SIZE_IN_BYTES=\"%d\" filename=\"dummy.bin\" num_partition_sectors=\"%d\""
" physical_partition_number=\"%d\" start_sector=\"%d\"/>\n"
"</data>\n";

const char* XML_READ_ECC =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<read SECTOR_SIZE_IN_BYTES=\"%d\" filename=\"dummy.bin\" num_partition_sectors=\"%d\""
" physical_partition_number=\"%d\" start_sector=\"%d\" ecc_disabled=\"%d\"/>\n"
"</data>\n";

static void GetXmlRead(AutoMem& aMem, int aStartSector, int aNumSectors, int aPartitionNumber = 0, int aSectorSize = 2048 )
{
	if (aSectorSize == 2112)
	{
		sprintf_s(aMem(), aMem.Size(), XML_READ_ECC, aSectorSize, aNumSectors, aPartitionNumber, aStartSector, 1);
	}
	else
	{
		sprintf_s(aMem(), aMem.Size(), XML_READ, aSectorSize, aNumSectors, aPartitionNumber, aStartSector);
	}
	aMem.setCount(strlen(aMem()));
}

const char* XML_PEEK =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<peek address64=\"0x%lX\" size_in_bytes=\"%d\"/>\n"
"</data>\n";

static void GetXmlPeek(AutoMem& aMem, UINT64 address, int aBytes)
{
	sprintf_s(aMem(), aMem.Size(), XML_PEEK, address, aBytes);
	aMem.setCount(strlen(aMem()));
}

const char* XML_POKE =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<poke address64=\"0x%lX\" size_in_bytes=\"%d\" value=\"0x%lX\"/>\n"
"</data>\n";

static void GetXmlPoke(AutoMem& aMem, UINT64 address, int aBytes, UINT64 aValue)
{
	sprintf_s(aMem(), aMem.Size(), XML_POKE, address, aBytes, aValue);
	aMem.setCount(strlen(aMem()));
}

const char* XML_BOOTDRIVE =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<setbootablestoragedrive value=\"1\" />\n"
"</data>\n";

static void GetXmlBootDrive(AutoMem& aMem, int aBootDrive)
{
	sprintf_s(aMem(), aMem.Size(), XML_BOOTDRIVE, aBootDrive);
	aMem.setCount(strlen(aMem()));
}

const char* XML_NOP =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<nop value=\"ping\" project=\"%s\" />\n"
"</data>\n";

static void GetXmlNop(AutoMem& aMem)
{
	sprintf_s(aMem(), aMem.Size(), XML_NOP, "Gen11 DOWNLOADER");
	aMem.setCount(strlen(aMem()));
}

const char* XML_CREATEDRIVE =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<createstoragedrives\n"
"%s"
"</data>\n";

static void GetXmlCreateDrive(AutoMem& aMem, const char* aDriveInfo)
{
	// "DRIVE4_SIZE_IN_KB=\"524288\" DRIVE5_SIZE_IN_KB=\"0\"\n"
	// "DRIVE6_SIZE_IN_KB=\"0\" DRIVE7_SIZE_IN_KB=\"0\"/>\n"
	sprintf_s(aMem(), aMem.Size(), XML_CREATEDRIVE, aDriveInfo);
	aMem.setCount(strlen(aMem()));
}

const char* XML_STORAGEINFO =
"<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n"
"<data>\n"
"<getStorageInfo physical_partition_number=\"%d\"/>\n"
"</data>\n";

static void GetXmlStorageInfo(AutoMem& aMem, int aPartionNumber)
{
	sprintf_s(aMem(), aMem.Size(), XML_STORAGEINFO, aPartionNumber);
	aMem.setCount(strlen(aMem()));
}

