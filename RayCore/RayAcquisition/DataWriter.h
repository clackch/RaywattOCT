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

	void StartSave(tstring strFilePath);
	bool WriteFrame(int nFrame);
	void StopSave();

	virtual unsigned short* GetSample(int nFrame);
	virtual void AddFrame(void* pFrame);

private:
	void finalize();
	void flush(unsigned int nSaveBufferSize);
};

