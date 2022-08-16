#include "pch.h"
#include "DataReader.h"
#include "MoriaConfiguration.h"
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

int CDataReader::Initialize(tstring strDataFilePath) {
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	const int nBufferSize = pConfig.nBufferSize;

	if (strDataFilePath.empty()) return 0;

	finalize();

	std::ifstream ifs(strDataFilePath, std::ifstream::ate | std::ifstream::binary);
	ULONGLONG nFileSize = ifs.tellg();
	ifs.close();

	if (nFileSize <= 0) return 0;

	m_nNumOfSamples = (nFileSize / (nBufferSize * sizeof(unsigned short)));
	m_pReadSamples = new unsigned short* [m_nNumOfSamples];
	for (int i = 0; i < m_nNumOfSamples; i++) {
		m_pReadSamples[i] = NULL;
	}

	m_hFile = CreateFile(strDataFilePath.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING,
		FILE_FLAG_NO_BUFFERING | FILE_FLAG_SEQUENTIAL_SCAN, NULL);

	if (m_hFile != INVALID_HANDLE_VALUE) {
		return m_nNumOfSamples;
	}

	return 0;

}
unsigned short* CDataReader::GetSample(int nIndex) {
	if (nIndex < 0 || nIndex >= m_nNumOfSamples) return NULL;
	if (m_pReadSamples[nIndex] == NULL) {
		readFrame(nIndex);
	}

	return m_pReadSamples[nIndex];
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
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	const int nBufferSize = pConfig.nBufferSize;
	DWORD dwBytesRead = 0;
	bool result = true;

	if (nIndex < 0 || nIndex >= m_nNumOfSamples) return false;
	if (m_pReadSamples[nIndex] == NULL) {
		EnterCriticalSection(&m_csReadFrame);
		if(m_pReadSamples[nIndex] == NULL){
			m_pReadSamples[nIndex] = new unsigned short[nBufferSize];

			long long offset = nBufferSize * sizeof(unsigned short) * nIndex;
			long offsetL = 0xFFFFFFFF & offset;
			long offsetH = 0xFFFFFFFF & (offset >> 32);
			SetFilePointer(m_hFile, offsetL, &offsetH, FILE_BEGIN);
			result = ReadFile(m_hFile, m_pReadSamples[nIndex], nBufferSize * sizeof(unsigned short), &dwBytesRead, NULL);
		}
		LeaveCriticalSection(&m_csReadFrame);
	}

	return result;
}