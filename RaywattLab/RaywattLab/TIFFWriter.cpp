#include "pch.h"
#include "TIFFWriter.h"

CTIFFWriter::CTIFFWriter(CString strTIFPath) {
	m_pImageTIFF = TIFFOpen(CStringA(strTIFPath), "w+");
}
CTIFFWriter::~CTIFFWriter() {
	if (m_pImageTIFF != NULL) TIFFClose(m_pImageTIFF);
}

bool CTIFFWriter::SaveFrame(cv::Mat image) {
	cv::Mat imageConvert;

	cv::cvtColor(image, imageConvert, cv::COLOR_RGB2BGR);
	TIFFSetField(m_pImageTIFF, TIFFTAG_IMAGEWIDTH, imageConvert.cols);
	TIFFSetField(m_pImageTIFF, TIFFTAG_IMAGELENGTH, imageConvert.rows);
	TIFFSetField(m_pImageTIFF, TIFFTAG_SAMPLESPERPIXEL, imageConvert.channels());
	TIFFSetField(m_pImageTIFF, TIFFTAG_BITSPERSAMPLE, 8);
	TIFFSetField(m_pImageTIFF, TIFFTAG_ROWSPERSTRIP, imageConvert.rows);
	TIFFSetField(m_pImageTIFF, TIFFTAG_COMPRESSION, COMPRESSION_NONE);
	TIFFSetField(m_pImageTIFF, TIFFTAG_PLANARCONFIG, PLANARCONFIG_CONTIG);

	TIFFWriteEncodedStrip(m_pImageTIFF, 0, imageConvert.data, imageConvert.rows * imageConvert.cols * imageConvert.channels());
	TIFFWriteDirectory(m_pImageTIFF);

	return true;
}