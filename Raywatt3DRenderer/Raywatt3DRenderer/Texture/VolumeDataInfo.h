#pragma once

#include <DXUT.h>

struct VolumeDataInfo
{
	int width;
	int height;
	int depth;
	char* pDataPath;

	double GetDiagonal()
	{
		return sqrt((width*width) + (height*height) + (depth*depth));
	}

	void GetFloatDimention(float** out)
	{
		(*out) = new float[3];
		(*out)[0] = static_cast<float>(width);
		(*out)[1] = static_cast<float>(height);
		(*out)[2] = static_cast<float>(depth);
	}

	static HRESULT LoadRawBytes(VolumeDataInfo& info, BYTE** ppOut)
	{
		size_t length = (size_t)info.width * (size_t)info.height * (size_t)info.depth;
		*ppOut = new BYTE[length];

		FILE* file = nullptr;
		errno_t err = fopen_s(&file, info.pDataPath, "rb");
		if (err == 0)
		{
			fread(*ppOut, sizeof(BYTE), length, file);
			printf("LoadRawByhtes %s OK\n", info.pDataPath);
			return S_OK;
		}
		else
		{
			SAFE_DELETE_ARRAY(*ppOut);
			fclose(file);
			return E_FAIL;
		}
	}
};