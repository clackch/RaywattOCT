#include "pch.h"
#include "SimulateDevice.h"
#include "MoriaConfiguration.h"
#include "DataManager.h"

CSimulateDevice::CSimulateDevice(IDataManager* pDataReader)
	:m_pDataReader(pDataReader){
	m_nCurSampleIndex = 0;
	m_bPause = false;
}
CSimulateDevice::~CSimulateDevice() {
	CleanUp();
}

int CSimulateDevice::InitDevice() {
	CleanUp();

	m_nCurSampleIndex = 0;
	m_bPause = true;
	m_isInit = true;

	return NOERROR;
}
int CSimulateDevice::CleanUp() {
	m_nCurSampleIndex = 0;
	m_isInit = false;

	return NOERROR;
}

void CSimulateDevice::PrevFrame() {
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	const int nNumOfSamples = m_pDataReader->GetNumOfSamples();

	m_nCurSampleIndex--;
	m_nCurSampleIndex = (m_nCurSampleIndex < 0) ? nNumOfSamples - 1 : m_nCurSampleIndex;
}
void CSimulateDevice::NextFrame() {
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	const int nNumOfSamples = m_pDataReader->GetNumOfSamples();

	m_nCurSampleIndex++;
	m_nCurSampleIndex = (m_nCurSampleIndex >= nNumOfSamples) ? 0 : m_nCurSampleIndex;
}
void CSimulateDevice::SetFrame(int nFrame) {
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	const int nNumOfSamples = m_pDataReader->GetNumOfSamples();

	m_nCurSampleIndex = nFrame;
	m_nCurSampleIndex = (m_nCurSampleIndex < 0) ? 0 : m_nCurSampleIndex;
	m_nCurSampleIndex = (m_nCurSampleIndex >= nNumOfSamples) ? nNumOfSamples - 1 : m_nCurSampleIndex;
}

int CSimulateDevice::start() {
	m_nCurSampleIndex = 0;

	return NOERROR;
}
int CSimulateDevice::stop() {
	return NOERROR;
}
unsigned short* CSimulateDevice::acquire(int& nCurFrame, int& nTotalFrame) {
	CMoriaConfiguration& pConfig = CMoriaConfiguration::GetInstance();
	const int nNumOfSamples = m_pDataReader->GetNumOfSamples();
	bool result = true;

	Sleep(30);

	nCurFrame = m_nCurSampleIndex;
	nTotalFrame = nNumOfSamples;

	if (m_bPause) {
		return m_pDataReader->GetSample(m_nCurSampleIndex);
	}

	NextFrame();
	nCurFrame = m_nCurSampleIndex;
	nTotalFrame = nNumOfSamples;

	return m_pDataReader->GetSample(m_nCurSampleIndex);
}