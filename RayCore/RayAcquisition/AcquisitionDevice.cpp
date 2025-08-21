#include "Config.h"
#include "AcquisitionDevice.h"
#include "Imaging.h"
#include "DataManager.h"
#include "Utility.h"

IAcquisitionDevice::IAcquisitionDevice()
{
	m_pThread = NULL;
	m_isInit = false;

	m_pImaging = NULL;
	m_pWriter = NULL;
	
	m_fps = 0.0f;
}
IAcquisitionDevice::~IAcquisitionDevice() {
}
int IAcquisitionDevice::StartAcquisition() {
	BOOL result = FALSE;

	result = CUtility::StartThread(threadAcquire, m_pThread, (LPVOID)this);

	if (result) return NOERROR;
	else return -1;
}
int IAcquisitionDevice::StopAcquisition() {
	CUtility::StopThread(m_pThread);

	return NOERROR;
}

UINT IAcquisitionDevice::threadAcquire(LPVOID param) {
	IAcquisitionDevice *pDevice = (IAcquisitionDevice *)param;
	IImaging* pImaging = pDevice->m_pImaging;
	int nCurFrame = 0;
	int nTotalFrame = 0;

	pDevice->start();

	while (pDevice->m_pThread->isRun) {
		char *pBuffer = pDevice->acquire(nCurFrame, nTotalFrame);
		// To-Do : need Critical Section?
		if (pImaging != NULL && pBuffer != NULL) {
			pImaging->DoAsyncRender(pBuffer);
			pImaging->SetFrameInfo(nCurFrame, nTotalFrame);
		}

		if (isAccelDecelProfileRange(nCurFrame)) {

		}

		else if (pDevice->m_pWriter != NULL) {
			pDevice->m_pWriter->AddFrame(pBuffer);
		}
	}

	pDevice->stop();
	return NOERROR;
}

bool IAcquisitionDevice::isAccelDecelProfileRange(int nCurFrame) {

}