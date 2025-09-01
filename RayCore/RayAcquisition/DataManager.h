#pragma once
#include "Config.h"
#include <map>

class OCTHeader {
public:
	enum class Bit : UCHAR {
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
	enum class ExtraData : UCHAR {
		None = 0x00,
		Dispersion = 0x01,
		Background = 0x02
	};

public:
	Type type;
	DataType dataType;
	Channels channels;
	USHORT width;
	USHORT height;
	USHORT frames;
	UCHAR extraData;

	static int Size()
	{
		return sizeof(Bit) + sizeof(Type) + sizeof(DataType) + sizeof(Channels) + (sizeof(USHORT) * 3) + sizeof(ExtraData);
	}
};

class IDataManager
{
protected:
	int m_nNumOfSamples;
	std::map<OCTHeader::ExtraData, void*> mapExtraData;
public:
	IDataManager() { m_nNumOfSamples = 0;}
	virtual ~IDataManager() {
		std::map<OCTHeader::ExtraData, void*>::iterator it = mapExtraData.begin();
		while (it != mapExtraData.end())
		{
			delete[] it->second;
			it++;
		}
		mapExtraData.clear();
	}

	int GetNumOfSamples() { return m_nNumOfSamples; }
	virtual char* GetSample(int nIndex) = 0;
	virtual void AddFrame(void* pFrame) = 0;

	void AddExtraData(OCTHeader::ExtraData extraData, void* pData, int nSize) {
		std::map<OCTHeader::ExtraData, void*>::iterator it = mapExtraData.find(extraData);
		if (it != mapExtraData.end()) {
			delete[] it->second;
			mapExtraData.erase(it);
		}
		void* pCopyData = new char[nSize];
		memcpy(pCopyData, pData, nSize);
		mapExtraData.insert(std::make_pair(extraData, pCopyData));
	}
	void* GetExtraData(OCTHeader::ExtraData extraData) {
		std::map<OCTHeader::ExtraData, void*>::iterator it = mapExtraData.find(extraData);
		if (it != mapExtraData.end()) {
			return it->second;
		}
		return nullptr;
	}
};

