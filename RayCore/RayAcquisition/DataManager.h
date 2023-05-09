#pragma once
#include "Config.h"

class OCTHeader {
public:
	enum class Bit : UCHAR{
		SoF = 0x7E,
		EoF = 0xE7
	};
	enum class Type : UCHAR {
		Unknown = 0x00,
		TimeSignal = 0x01,
		FreqSignal = 0x02,
		Image = 0x03
	};
	enum class DataType : UCHAR {
		Unknown = 0x00,
		Byte = 0x01,
		UShort = 0x02,
		UInt32 = 0x04,
		Float64 = 0x08
	};
	enum class Channels : UCHAR {
		Unknown = 0x00,
		Single = 0x01,
		Dual = 0x02,
		RGB = 0x03,
		ARGB = 0x04
	};

public:
	Type type;
	DataType dataType;
	Channels channels;
	USHORT width;
	USHORT height;
	USHORT frames;

	static int Size()
	{
		return sizeof(Bit) + sizeof(Type) + sizeof(DataType) + sizeof(Channels) + (sizeof(USHORT) * 3);
	}
};

class IDataManager
{
protected:
	int m_nNumOfSamples;
public:
	IDataManager() { m_nNumOfSamples = 0; }
	virtual ~IDataManager() {}

	int GetNumOfSamples() { return m_nNumOfSamples; }
	virtual char* GetSample(int nIndex) = 0;
	virtual void AddFrame(void* pFrame) = 0;
};

