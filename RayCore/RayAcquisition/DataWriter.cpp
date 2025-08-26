#include "Config.h"
#include "DataWriter.h"
#include "Utility.h"
#include <fstream>

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
	std::vector<char> vHeader = createHeader(type, dataType, ch, width, height, m_nNumOfKeepSamples, extraData);

	DWORD dwBytesWrote = 0;
	WriteFile(m_hRecordingFile, vHeader.data(), vHeader.size(), &dwBytesWrote, NULL);
}
void CDataWriter::WriteExtraData(void* pExtraData, long nSize) {
	DWORD dwBytesWrote = 0;
	WriteFile(m_hRecordingFile, pExtraData, nSize, &dwBytesWrote, NULL);
}
bool CDataWriter::WriteFrame(int nFrame) {
	if (skipIdx[nFrame]) {
		char* pBuffer = (char*)GetSample(nFrame);
		if (pBuffer == NULL) return false;

		DWORD dwBytesWrote = 0;
		if (!WriteFile(m_hRecordingFile, pBuffer, m_nElementSize, &dwBytesWrote, NULL))
		{
			printf("Failed to write acquisition data to file: (LastError = 0x%08X)\n", GetLastError());
			return false;
		}

		return true;
	}
	else {
		return false;
	}
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

	//unsigned long long ulOffset = (m_nNumOfSamples - nFrame - 1) * (unsigned long long) m_nElementSize;	// Get Sample from Proximal to Distal
	unsigned long long ulOffset = (nFrame) * (unsigned long long) m_nElementSize;	// Get Sample from Distal to Proximal
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

void CDataWriter::CutPullbackLength(int pullbackType) {
	int rotationRatio = 2;  // 200바퀴로 테스트
	int stopFrames = 15 * rotationRatio;
	int maxFrames = 0;
	double deleteRatio = 0;

	switch (pullbackType) {
	case (int)PullbackType::HISH_20_60:
		maxFrames = 1200; break;
	case (int)PullbackType::HILO_40_100:
		maxFrames = 1000; break;
	case (int)PullbackType::STSH_60_60:
		maxFrames = 400; break;
	case (int)PullbackType::STLO_100_100:
		maxFrames = 400; break;
	case (int)PullbackType::FAST_120_60:
		maxFrames = 200; break;
	}

	maxFrames /= rotationRatio;
	SkipFrames(stopFrames, maxFrames, pullbackType, rotationRatio);
}

void CDataWriter::SkipFrames(int stopRecordedFrames, int maximumFrames, int pullbackType, int rotationRatio) {
	const double A = m_pisp[m_SMProfile][pullbackType].a;
	const double B = m_pisp[m_SMProfile][pullbackType].b; // CSV의 음수 그대로 사용
	const double C = m_pisp[m_SMProfile][pullbackType].c;
	const double threshold = m_pisp[m_SMProfile][pullbackType].threshold;
	const int    frameNum = m_pisp[m_SMProfile][pullbackType].frameNum / rotationRatio;

	// 구간 길이 정의
	const int halfLen = std::max(0, frameNum / 2);
	const int accelStart = stopRecordedFrames;                 // 가속 구간 시작 인덱스
	const int accelEnd = accelStart + halfLen - 1;           // 포함
	const int decelStart = maximumFrames - halfLen;            // 감속 구간 시작 인덱스
	const int decelEnd = maximumFrames - 1;                  // 포함
	
	int numOfKeeps = 0;
	// 누적합(스킵 시 0으로 리셋)
	double acc = 0.0;

	// 시작/끝 정지구간은 Skip(false)
	for (int i = 0; i < stopRecordedFrames / 2 && i < m_nNumOfSamples; ++i)
	{
		skipIdx[i] = false;
		numOfKeeps += 2;
	}
	for (int i = std::max(0, m_nNumOfSamples - stopRecordedFrames / 2); i < m_nNumOfSamples; ++i)
	{
		skipIdx[i] = false;
	}
	// ---------- 1) 가속 구간 패턴 생성 ----------
	std::vector<bool> accelKeep(halfLen, true); // true=keep, false=skip
	acc = 0.0;
	for (int k = 0; k < halfLen; ++k)
	{
		const int i = accelStart + k;
		if (i < 0 || i >= m_nNumOfSamples) break;

		// x는 가속 구간 내 1-based 프레임 인덱스
		const double x = static_cast<double>(k + 1);

		// --- y 계산 (pullbackType 조건에 따른 분기) ---
		double y = 0.0;
		if (pullbackType <= 2) {
			// 유리함수: y = A / (x + B) + C
			double denom = x + B;
			if (std::abs(denom) < 1e-12) denom = (denom >= 0.0 ? 1e-12 : -1e-12); // 0 방어
			y = A / denom + C;
		}
		else {
			// 멱감쇠(지수/로그 계열): y = A * x^B + C   (B<0 이면 감쇠)
			y = A * std::pow(x, B) + C;
		}

		acc += y;

		if (acc >= threshold) {
			// 스킵 & 누적 리셋
			accelKeep[k] = false;       // skip
			skipIdx[i] = false;
			acc = 0.0;
			numOfKeeps += 2;
		}
		else {
			// 킵
			accelKeep[k] = true;        // keep
			skipIdx[i] = true;
		}
	}

	// ---------- 2) 중간(정속) 구간: 전부 keep ----------
	const int midStart = std::max(accelEnd + 1, 0);
	const int midEnd = std::min(decelStart - 1, m_nNumOfSamples - 1);
	for (int i = midStart; i <= midEnd; ++i)
	{
		skipIdx[i] = true;
	}

	// ---------- 3) 감속 구간: 가속 패턴을 역순 적용 ----------
	// 예: accelKeep = [skip, skip, keep, skip, keep, ...]
	//     decel은 이를 뒤집어 적용
	for (int j = 0; j < halfLen; ++j)
	{
		const int i = decelStart + j;
		if (i < 0 || i >= m_nNumOfSamples) break;

		const bool keep = (j < (int)accelKeep.size())
			? accelKeep[halfLen - 1 - j]
			: true; // 안전장치

		skipIdx[i] = keep; // true=keep, false=skip
	}
	m_nNumOfKeepSamples = m_nNumOfSamples - numOfKeeps;

	PLOGI.printf("m_nNumOfKeepSamples = %d, m_nNumOfSamples = %d, gap = %d", numOfKeeps);
}

void CDataWriter::ReadAccelDecelPofileParameter()
{	
	std::ifstream reader("./SMProfileParameters.txt");

	if (reader.is_open()) {
		std::string line;
		int profile;
		int index = 0;
		while (std::getline(reader, line)) {
			std::vector<std::string> parameter;
			std::stringstream ss(line);
			std::string token;

			while (std::getline(ss, token, ',')) {
				parameter.push_back(token);
			}

			if (parameter.size() == 2) {
				profile = stoi(parameter[1]);
				index = 0;
			}
			else {
				m_pisp[profile][index].a = stoi(parameter[0]);
				m_pisp[profile][index].b = stoi(parameter[1]);
				m_pisp[profile][index].c = stoi(parameter[2]);
				m_pisp[profile][index].threshold = stoi(parameter[3]);
				m_pisp[profile][index].frameNum = stoi(parameter[4]);
				index++;
			}
		}
		reader.close();
	}
	else {
		PLOGI.printf("Cannot SMProfileParameters open .txt");
	}
}
