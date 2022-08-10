#pragma once
#include <Windows.h>
#include "DataManager.h"

class CDataWriter : public IDataManager
{
private:
	char* m_pRecordBuffer;
	unsigned int m_nBufferSize;
	unsigned int m_nElementSize;

	bool m_isRecording;
	HANDLE m_hRecordingFile;

public:
	CDataWriter();
	virtual ~CDataWriter();

	void Initialize(int nFrameBytes);

	int StartRecording();
	void StopRecording();
	bool IsRecording() { return m_isRecording; }
	void PushToBuffer(void* pFrame);

	void StartSave(tstring strFilePath);
	bool WriteFrame(int nFrame);
	void StopSave();

	virtual unsigned short* GetSample(int nFrame);

private:
	void finalize();
	void flush(unsigned int nSaveBufferSize);
};

