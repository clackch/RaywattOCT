#include "TIFFReader.h"

CTIFFReader::CTIFFReader()
	: m_pTif(nullptr), m_nWidth(0), m_nHeight(0), m_nChannels(0)
{
}
CTIFFReader::~CTIFFReader()
{
	finalize();
}

int CTIFFReader::Initialize(const char* strDataFilePath)
{
	finalize();

	m_pTif = TIFFOpen(strDataFilePath, "r");	
	if (m_pTif == nullptr) return 0;

	int read = false;
	do
	{
		m_nNumOfSamples++;
		read = TIFFReadDirectory(m_pTif);
	} while (read);

	TIFFSetDirectory(m_pTif, 0);

	TIFFGetField(m_pTif, TIFFTAG_IMAGEWIDTH, &m_nWidth);
	TIFFGetField(m_pTif, TIFFTAG_IMAGELENGTH, &m_nHeight);
	TIFFGetField(m_pTif, TIFFTAG_SAMPLESPERPIXEL, &m_nChannels);

	m_nDataSize = m_nWidth * m_nHeight;
	m_pReadSamples = new char* [m_nNumOfSamples];
	for (int i = 0; i < m_nNumOfSamples; i++)
	{
		m_pReadSamples[i] = nullptr;
	}

	return m_nNumOfSamples;
}
void CTIFFReader::GetImageSize(int& width, int& height)
{
	width = m_nWidth;
	height = m_nHeight;
}

void CTIFFReader::finalize()
{
	if (m_pTif != nullptr)
	{
		TIFFClose(m_pTif);
		m_pTif = nullptr;
	}
	m_nNumOfSamples = 0;
}
bool CTIFFReader::readFrame(int nIndex)
{
	if (nIndex < 0 || nIndex >= m_nNumOfSamples) return false;
	
	if (m_pReadSamples[nIndex] == NULL) {
		m_pReadSamples[nIndex] = new char[m_nDataSize * sizeof(char)];

		//int revertedIndex = (m_nNumOfSamples - nIndex - 1);
		TIFFSetDirectory(m_pTif, nIndex);

		for (int y = 0; y < m_nHeight; y++) {
			TIFFReadScanline(m_pTif, m_pReadSamples[nIndex] + (y * m_nWidth), y);
		}
	}

	return true;
}