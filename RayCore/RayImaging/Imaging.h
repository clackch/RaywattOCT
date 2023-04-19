#pragma once

class IImaging {
public:
	class Setting {
	public:
		int nAScan;		// Axial Scan (z-Depth)
		int nBScan;		// Number of A-Scan (Transverse)
		int nBufferSize;
		int nCircleSize;
		int nFFTOrder;
		int nFFTLength;
		int nOutputLength;

		float brightness;
		float contrast;
		float lowLevel;
		float highLevel;
		
		void Set(int nAScan, int nBScan)
		{
			this->nAScan = nAScan;
			this->nBScan = nBScan;
			this->nBufferSize = (this->nBScan * this->nAScan);
			this->nFFTOrder = 1;
			this->nFFTLength = 1 << this->nFFTOrder;
			while (this->nFFTLength < this->nAScan) { // AScan 보다 큰 2^n 중에서 제일 작은 수
				this->nFFTOrder++;
				this->nFFTLength = 1 << this->nFFTOrder;
			}
			this->nOutputLength = this->nFFTLength / 2;
			this->nCircleSize = this->nOutputLength;
		}
	};

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