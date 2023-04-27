#include "Config.h"
#include "DataReader.h"
#include <fstream>

CDataReader::CDataReader() {
	m_pReadSamples = NULL;
	m_hFile = INVALID_HANDLE_VALUE;

	InitializeCriticalSection(&m_csReadFrame);
}
CDataReader::~CDataReader() {
	finalize();

	DeleteCriticalSection(&m_csReadFrame);
}

int CDataReader::Initialize(tstring strDataFilePath, int nDataSize, int nHeaderSize) {
	if (strDataFilePath.empty()) return 0;
	if (nDataSize <= 0) return 0;

	m_nDataSize = nDataSize;
	m_nHeaderSize = nHeaderSize;
	finalize();

	std::ifstream ifs(strDataFilePath, std::ifstream::ate | std::ifstream::binary);
	ULONGLONG nFileSize = ifs.tellg();
	ifs.close();

	if (nFileSize <= 0) return 0;

	m_nNumOfSamples = ((nFileSize - nHeaderSize) / (m_nDataSize * sizeof(unsigned short)));
	m_pReadSamples = new unsigned short* [m_nNumOfSamples];
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
unsigned short* CDataReader::GetSample(int nIndex) {
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
	OCTHeader header;
	bool success = false;

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

		if (result && flag == OCTHeader::Bit::SoF)
		{
			long long offset = header.width * header.height * header.frames * (int)header.dataType * (int)header.channels;
			long offsetL = 0xFFFFFFFF & offset;
			long offsetH = 0xFFFFFFFF & (offset >> 32);
			SetFilePointer(hFile, offsetL, &offsetH, FILE_CURRENT);

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

	EnterCriticalSection(&m_csReadFrame);
	if (m_pReadSamples[nIndex] == NULL) {
		if(m_pReadSamples[nIndex] == NULL){
			m_pReadSamples[nIndex] = new unsigned short[m_nDataSize];

			long long offset = m_nHeaderSize + m_nDataSize * sizeof(unsigned short) * nIndex;
			long offsetL = 0xFFFFFFFF & offset;
			long offsetH = 0xFFFFFFFF & (offset >> 32);
			SetFilePointer(m_hFile, offsetL, &offsetH, FILE_BEGIN);
			result = ReadFile(m_hFile, m_pReadSamples[nIndex], m_nDataSize * sizeof(unsigned short), &dwBytesRead, NULL);
		}
	}
	LeaveCriticalSection(&m_csReadFrame);

	return result;
}