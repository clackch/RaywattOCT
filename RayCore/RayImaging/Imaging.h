#pragma once

class IImaging {
private:
	unsigned int m_nSession;
public:
	IImaging() : m_nSession(0) {}
	virtual ~IImaging() {}

	void SetSession(unsigned int nSession) { m_nSession = nSession; }
	unsigned int GetSession() { return m_nSession; }
public:
	virtual void DoAsyncRender(USHORT* fringes) = 0;
	virtual void SetFrameInfo(int nCurFrame, int nTotalFrame) = 0;
};