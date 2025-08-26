#pragma once
#include <Windows.h>
#include <vector>
#include "DataManager.h"

class CDataWriter : public IDataManager
{
private:
	char* m_pRecordBuffer;
	unsigned int m_nBufferSize;
	unsigned int m_nElementSize;

	bool m_isRecording;
	HANDLE m_hRecordingFile;

	enum class PullbackType {
		HISH_20_60 = 0,  // Speed_Distance
		HILO_40_100,
		STSH_60_60,
		STLO_100_100,
		FAST_120_60
	};

	typedef struct PullbackImageSkipParameters {
		double a, b, c;
		double threshold;
		int frameNum;
	}PISP; 
	
	PISP m_pisp[2][5];
	std::vector<int, bool> skipIdx;
	int m_SMProfile;
public:
	CDataWriter();
	virtual ~CDataWriter();

	void Initialize(int nFrameBytes);

	int StartRecording();
	void StopRecording();
	bool IsRecording() { return m_isRecording; }

	void StartSave(tstring strFilePath);
	void WriteHeader(OCTHeader::Type type, OCTHeader::DataType dataType, OCTHeader::Channels ch, int width, int height, UCHAR extraData);
	void WriteExtraData(void* pExtraData, long nSize);
	bool WriteFrame(int nFrame);
	void WriteEOF();
	void StopSave();
	
	void CutPullbackLength(int pullbackType);
	void ReadAccelDecelPofileParameter();
	void SkipFrames(int stopRecordedFrames, int maximumFrames, int pullbackType, int rotationRatio);
	void SetSMProfile(int SMProfile) { m_SMProfile = SMProfile; }

	virtual char* GetSample(int nFrame);
	virtual void AddFrame(void* pFrame);

private:
	void finalize();
	void flush(unsigned int nSaveBufferSize);
	std::vector<char> createHeader(OCTHeader::Type type, OCTHeader::DataType dataType, OCTHeader::Channels ch, int width, int height, int frames, UCHAR extraData);
};

