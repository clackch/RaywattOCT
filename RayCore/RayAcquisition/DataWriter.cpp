#include "Config.h"
#include "DataWriter.h"
#include "Utility.h"

CDataWriter::CDataWriter() {
	m_pRecordBuffer = NULL;
	m_nBufferSize = 0;
	m_nElementSize = 0;

	m_isRecording = false;
	m_hRecordingFile = NULL;
}

CDataWriter::~CDataWriter() {
	finalize();
}

void CDataWriter::Initialize(int nFrameBytes) {
	const unsigned int nDefaultBufferSize = 1500;

	finalize();
	m_nBufferSize = nDefaultBufferSize;
	m_nElementSize = nFrameBytes;

	unsigned long long ulAllocSize = (unsigned long long) m_nElementSize * (unsigned long long) m_nBufferSize;
	m_pRecordBuffer = new char[ulAllocSize];
}

int CDataWriter::StartRecording() {
	if (m_pRecordBuffer == NULL) return -1;

	m_nNumOfSamples = 0;
	m_isRecording = true;
	PLOGI.printf("Start Recording");

	return NOERROR;
}

void CDataWriter::StopRecording() {
	m_isRecording = false;
	PLOGI.printf("Stop Recording");
}

void CDataWriter::StartSave(tstring strFilePath) {
	// create file
	m_hRecordingFile = CreateFile(
		strFilePath.c_str(), GENERIC_WRITE,
		FILE_SHARE_READ, NULL, CREATE_ALWAYS,
		FILE_FLAG_SEQUENTIAL_SCAN, NULL);
}
void CDataWriter::WriteHeader(OCTHeader::Type type, OCTHeader::DataType dataType, OCTHeader::Channels ch, int width, int height, UCHAR extraData) {
	std::vector<char> vHeader = createHeader(type, dataType, ch, width, height, m_nNumOfSamples, extraData);

	DWORD dwBytesWrote = 0;
	WriteFile(m_hRecordingFile, vHeader.data(), vHeader.size(), &dwBytesWrote, NULL);
}
void CDataWriter::WriteExtraData(void* pExtraData, long nSize) {
	DWORD dwBytesWrote = 0;
	WriteFile(m_hRecordingFile, pExtraData, nSize, &dwBytesWrote, NULL);
}
bool CDataWriter::WriteFrame(int nFrame) {
	char* pBuffer = (char *) GetSample(nFrame);
	if (pBuffer == NULL) return false;

	DWORD dwBytesWrote = 0;
	if (!WriteFile(m_hRecordingFile, pBuffer, m_nElementSize, &dwBytesWrote, NULL))
	{
		printf("Failed to write acquisition data to file: (LastError = 0x%08X)\n", GetLastError());
		return false;
	}

	return true;
}
void CDataWriter::WriteEOF() {
	OCTHeader::Bit flag = OCTHeader::Bit::EoF;
	DWORD dwBytesWrote = 0;
	WriteFile(m_hRecordingFile, &flag, sizeof(flag), &dwBytesWrote, NULL);
}
void CDataWriter::StopSave() {
	CloseHandle(m_hRecordingFile);
	m_hRecordingFile = NULL;
}

char* CDataWriter::GetSample(int nFrame) {
	if (nFrame >= m_nNumOfSamples) return NULL;

	unsigned long long ulOffset = (m_nNumOfSamples - nFrame - 1) * (unsigned long long) m_nElementSize;	// Get Sample from Proximal to Distal
	return (m_pRecordBuffer + ulOffset);
}

void CDataWriter::AddFrame(void* pFrame) {
	if (m_isRecording == false) return;
	if (m_nNumOfSamples >= m_nBufferSize) return;
	if (pFrame == nullptr) return;

	unsigned long long ulOffset = (unsigned long long) m_nNumOfSamples * (unsigned long long) m_nElementSize;
	memcpy(m_pRecordBuffer + ulOffset, pFrame, m_nElementSize);
	m_nNumOfSamples++;
}

void CDataWriter::finalize() {
	if (m_pRecordBuffer != NULL) {
		delete[] m_pRecordBuffer;
		m_pRecordBuffer = NULL;
	}
}
void CDataWriter::flush(unsigned int nSaveBufferSize) {
	DWORD dwBytesWrote = 0;

	for(int i=0; i<nSaveBufferSize; i++){
		char* pBuffer = (char*)GetSample(i);
		if (pBuffer != NULL) {
			if (!WriteFile(m_hRecordingFile, pBuffer, m_nElementSize, &dwBytesWrote, NULL))
			{
				printf("Failed to write acquisition data to file: (LastError = 0x%08X)\n", GetLastError());
			}
		}
	}
}
std::vector<char> CDataWriter::createHeader(OCTHeader::Type type, OCTHeader::DataType dataType, OCTHeader::Channels ch, int width, int height, int frames, UCHAR extraData) {
	std::vector<char> vPacket;

	vPacket.push_back((char)OCTHeader::Bit::SoF);

	vPacket.push_back((char)type);
	vPacket.push_back((char)dataType);
	vPacket.push_back((char)(width & 0xFF));
	vPacket.push_back((char)((width >> 8) & 0xFF));
	vPacket.push_back((char)(height & 0xFF));
	vPacket.push_back((char)((height >> 8) & 0xFF));
	vPacket.push_back((char)(frames & 0xFF));
	vPacket.push_back((char)((frames >> 8) & 0xFF));
	vPacket.push_back((char)ch);
	vPacket.push_back((char)extraData);

	return vPacket;
}