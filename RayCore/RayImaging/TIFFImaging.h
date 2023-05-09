#pragma once
#include "OCTImaging.h"

class CTIFFImaging : public COCTImaging
{
private:
	cv::Mat imageConvert;

public:
	CTIFFImaging(Setting, CMessageService*);
	virtual ~CTIFFImaging();

	void Initialize();
	virtual void Process(char* fringes);
};

