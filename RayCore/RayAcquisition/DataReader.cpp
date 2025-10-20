#include "Config.h"
#include "DataReader.h"
#include <fstream>

CDataReader::CDataReader() {
	m_nDataSize = 0;
	m_nHeaderSize = 0;
	m_pReadSamples = NULL;
	m_hFile = INVALID_HANDLE_VALUE;

	InitializeCriticalSection(&m_csReadFrame);
}
CDataReader::~CDataReader() {
	finalize();

	DeleteCriticalSection(&m_csReadFrame);
}

int CDataReader::Initialize(tstring strDataFilePath, int nDataSize) {
	if (strDataFilePath.empty()) return 0;
	if (nDataSize <= 0) return 0;

	m_nDataSize = nDataSize;
	finalize();

	std::ifstream ifs(strDataFilePath, std::ifstream::ate | std::ifstream::binary);
	ULONGLONG nFileSize = ifs.tellg();
	ifs.close();

	if (nFileSize <= 0) return 0;

	m_nNumOfSamples = ((nFileSize - m_nHeaderSize) / (m_nDataSize * sizeof(unsigned short)));

	if (std::abs(m_nNumOfSamples) > 1024 * 1024 * 1024) {
		return 0;
		PLOGI.printf("m_nNumOfSamples is too big : %d", m_nNumOfSamples);
	}
	m_pReadSamples = new char* [m_nNumOfSamples];
	for (int i = 0; i < m_nNumOfSamples; i++) {
		m_pReadSamples[i] = NULL;
	}

	m_hFile = CreateFile(strDataFilePath.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
		FILE_FLAG_SEQUENTIAL_SCAN, NULL);

	if (m_hFile != INVALID_HANDLE_VALUE) {
		return m_nNumOfSamples;
	}

	return 0;

}
char* CDataReader::GetSample(int nIndex) {
	if (nIndex < 0 || nIndex >= m_nNumOfSamples) return NULL;

	EnterCriticalSection(&m_csReadFrame);
	if (m_pReadSamples[nIndex] == NULL) {
		readFrame(nIndex);
	}
	LeaveCriticalSection(&m_csReadFrame);

	return m_pReadSamples[nIndex];
}

OCTHeader CDataReader::ReadHeader(tstring strFilePath)
{
	OCTHeader header = {};
	bool success = false;
	
	m_nHeaderSize = 0;

	HANDLE hFile = CreateFile(strFilePath.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
	if (hFile != INVALID_HANDLE_VALUE)
	{
		BOOL result = TRUE;
		DWORD dwBytesRead = 0;

		OCTHeader::Bit flag;

		result &= ReadFile(hFile, &flag, sizeof(OCTHeader::Bit), &dwBytesRead, NULL);
		result &= ReadFile(hFile, &header.type, sizeof(OCTHeader::Type), &dwBytesRead, NULL);
		result &= ReadFile(hFile, &header.dataType, sizeof(OCTHeader::DataType), &dwBytesRead, NULL);
		result &= ReadFile(hFile, &header.width, sizeof(USHORT), &dwBytesRead, NULL);
		result &= ReadFile(hFile, &header.height, sizeof(USHORT), &dwBytesRead, NULL);
		result &= ReadFile(hFile, &header.frames, sizeof(USHORT), &dwBytesRead, NULL);
		result &= ReadFile(hFile, &header.channels, sizeof(OCTHeader::Channels), &dwBytesRead, NULL);
		result &= ReadFile(hFile, &header.extraData, sizeof(OCTHeader::ExtraData), &dwBytesRead, NULL);

		if (!result) {
			PLOGE.printf("Header read failed.");
			CloseHandle(hFile);
			return header;
		}

		if (header.width > 1024 * 10 || header.height > 1024 * 10) {
			PLOGI.printf("FIle Header data is too big : width : %d, height : %d", header.width, header.height);
			header.width = 0;
			header.height = 0;
			CloseHandle(hFile);
			return header;
		}

		if (result && flag == OCTHeader::Bit::SoF)
		{
			m_nHeaderSize += OCTHeader::Size(); // 11
			
			if (header.extraData & (UCHAR)OCTHeader::ExtraData::Dispersion)
			{
				int nSize = header.width * 2 * sizeof(int);
				if (readExtraData(hFile, OCTHeader::ExtraData::Dispersion, nSize)) {
					m_nHeaderSize += nSize;
					PLOGI.printf("Disperion ExtraData Size = %d", nSize);
				}
			}
			if (header.extraData & (UCHAR)OCTHeader::ExtraData::Background)
			{
				int nSize = header.width * header.height * sizeof(USHORT);
				if (readExtraData(hFile, OCTHeader::ExtraData::Background, nSize)) {
					m_nHeaderSize += nSize;
					PLOGI.printf("Background ExtraData Size = %d", nSize);
				}
			}
			if (header.extraData & (UCHAR)OCTHeader::ExtraData::RFID) {
				int nSize = (8) * sizeof(unsigned char);
				if (readExtraData(hFile, OCTHeader::ExtraData::RFID, nSize)) {
					m_nHeaderSize += nSize;
					PLOGI.printf("RFID data Size = %d", nSize);
				}
			}
			long long offset = header.width * header.height * header.frames * (int)header.dataType * (int)header.channels;
			long offsetL = 0xFFFFFFFF & offset;
			long offsetH = 0xFFFFFFFF & (offset >> 32);
			DWORD newPos = SetFilePointer(hFile, offsetL, &offsetH, FILE_CURRENT);
			if (newPos != INVALID_SET_FILE_POINTER) {
				DWORD err = GetLastError();
				if (err != NO_ERROR) {
					PLOGI.printf("SetFilePointer failed. Error : %lu", err);
				}
			}

			result &= ReadFile(hFile, &flag, sizeof(OCTHeader::Bit), &dwBytesRead, NULL);

			if (result && flag == OCTHeader::Bit::EoF) {
				success = true;
			}
		}

		CloseHandle(hFile);
	}

	if (success == false) {
		header.type = OCTHeader::Type::Unknown;
		header.dataType = OCTHeader::DataType::Unknown;
		header.channels = OCTHeader::Channels::Unknown;
		header.extraData = (UCHAR) OCTHeader::ExtraData::None;
		header.width = 0;
		header.height = 0;
		header.frames = 0;
	}

	return header;
}

void CDataReader::finalize() {
	if (m_pReadSamples != NULL) {
		for (int i = 0; i < m_nNumOfSamples; i++) {
			if (m_pReadSamples[i] != NULL) delete[] m_pReadSamples[i];
		}
		delete[] m_pReadSamples;
		m_pReadSamples = NULL;
	}

	if (m_hFile != INVALID_HANDLE_VALUE) {
		CloseHandle(m_hFile);
		m_hFile = INVALID_HANDLE_VALUE;
	}
}
bool CDataReader::readFrame(int nIndex) {
	DWORD dwBytesRead = 0;
	bool result = true;

	if (nIndex < 0 || nIndex >= m_nNumOfSamples) return false;

	if (m_nDataSize > 1024 * 1024 * 10) {
		PLOGI.printf("Data size is too big : %d", m_nDataSize);
		return false;
	}

	if (m_pReadSamples[nIndex] == NULL) {
		m_pReadSamples[nIndex] = new char[m_nDataSize * sizeof(unsigned short)];

		long long offset = m_nHeaderSize + m_nDataSize * sizeof(unsigned short) * nIndex;
		long offsetL = 0xFFFFFFFF & offset;
		long offsetH = 0xFFFFFFFF & (offset >> 32);
		DWORD newPos = SetFilePointer(m_hFile, offsetL, &offsetH, FILE_BEGIN);
		if (newPos != INVALID_SET_FILE_POINTER) {
			DWORD err = GetLastError();
			if (err != NO_ERROR) {
				PLOGI.printf("SetFilePointer failed. Error : %lu", err);
			}
		}

		result = ReadFile(m_hFile, m_pReadSamples[nIndex], m_nDataSize * sizeof(unsigned short), &dwBytesRead, NULL);
	}

	return result;
}
bool CDataReader::readExtraData(HANDLE hFile, OCTHeader::ExtraData extraData, int nSize) {
	DWORD dwBytesRead = 0;

	size_t size = static_cast<size_t>(nSize);

	void* pData = new char[size];
	
	BOOL result = ReadFile(hFile, pData, size, &dwBytesRead, NULL);
	if (result)
	{
		PLOGI.printf("Success Reading Extra Data");
		AddExtraData(extraData, pData, size);
	}
	else {
		PLOGI.printf("Fail Reading Extra Data");
	}

	delete[] pData;
	
	return result;
}