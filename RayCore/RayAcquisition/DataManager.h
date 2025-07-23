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
	std::map<OCTHeader::ExtraData, std::vector<uint8_t>> mapExtraData;

public:
	IDataManager() { m_nNumOfSamples = 0; }
	virtual ~IDataManager() {
		mapExtraData.clear();
	}

	int GetNumOfSamples() const { return m_nNumOfSamples; }

	virtual char* GetSample(int nIndex) = 0;
	virtual void AddFrame(void* pFrame) = 0;

	void AddExtraData(OCTHeader::ExtraData extraData, void* pData, int nSize) {
		if (nSize <= 0 || pData == nullptr) {
			PLOGI.printf("Invalid ExtraData input (nullptr or size <= 0)");
			return;
		}
		if (nSize > 1024 * 1024 * 1024 * 2) { // 2GB 이상 방어
			PLOGI.printf("Allocation Size too big");
			return;
		}

		std::vector<uint8_t> vecData(reinterpret_cast<uint8_t*>(pData),
			reinterpret_cast<uint8_t*>(pData) + nSize);
		mapExtraData[extraData] = std::move(vecData);
	}

	// raw pointer가 필요하다면 const-cast
	uint8_t* GetExtraData(OCTHeader::ExtraData extraData) {
		auto it = mapExtraData.find(extraData);
		if (it != mapExtraData.end()) {
			return it->second.data();
		}
		return nullptr;
	}
};

