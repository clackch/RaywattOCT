#pragma once

class IImaging {
private:
	unsigned int m_nSession;
protected:
	unsigned int m_nWidth, m_nHeight, m_nChannels;
public:
	IImaging() : m_nSession(0), m_nWidth(0), m_nHeight(0), m_nChannels(0) {}
	virtual ~IImaging() {}

	void SetSession(unsigned int nSession) { m_nSession = nSession; }
	unsigned int GetSession() { return m_nSession; }
	unsigned int GetImageWidth() { return m_nWidth; }
	unsigned int GetImageHeight() { return m_nHeight; }
	unsigned int GetImageChannels() { return m_nChannels; }
public:
	virtual void DoAsyncRender(USHORT* fringes) = 0;
	virtual void SetFrameInfo(int nCurFrame, int nTotalFrame) = 0;
};