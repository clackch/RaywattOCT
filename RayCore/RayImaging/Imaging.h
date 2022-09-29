#pragma once

#define WM_PROCESS_OCT_DONE		(WM_USER + 0x0001)

class IImaging {
public:
	IImaging() {}
	virtual ~IImaging() {}

public:
	virtual void DoAsyncRender(USHORT* fringes) = 0;
	virtual void SetFrameInfo(int nCurFrame, int nTotalFrame) = 0;
};