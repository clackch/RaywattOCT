#include "pch.h"
#include "AcquisitionDevice.h"
#include "MoriaImaging.h"
#include "DataWriter.h"
#include "Utility.h"

IAcquisitionDevice::IAcquisitionDevice() {
	m_pThread = NULL;
	m_runThread = false;
	m_isInit = false;

	m_pImaging = NULL;
	m_pWriter = NULL;
}
IAcquisitionDevice::~IAcquisitionDevice() {
}
int IAcquisitionDevice::StartAcquisition() {
	BOOL result = FALSE;

	result = CUtility::StartThread(threadAcquire, m_pThread, m_runThread, (LPVOID)this);

	if (result) return NOERROR;
	else return -1;
}
int IAcquisitionDevice::StopAcquisition() {
	CUtility::StopThread(m_pThread, m_runThread);

	return NOERROR;
}

UINT IAcquisitionDevice::threadAcquire(LPVOID param) {
	IAcquisitionDevice *pDevice = (IAcquisitionDevice *)param;
	CMoriaImaging* pImaging = pDevice->m_pImaging;
	CDataWriter* pWriter = pDevice->m_pWriter;
	int nCurFrame = 0;
	int nTotalFrame = 0;

	pDevice->start();

	while (pDevice->m_runThread) {
		unsigned short *pBuffer = pDevice->acquire(nCurFrame, nTotalFrame);
		// To-Do : need Critical Section?
		if (pImaging != NULL && pBuffer != NULL) {
			pImaging->DoAsyncRender(pBuffer);
			pImaging->SetFrameInfo(nCurFrame, nTotalFrame);
		}

		if (pWriter != NULL) {
			pWriter->PushToBuffer(pBuffer);
		}
	}

	pDevice->stop();
	return NOERROR;
}