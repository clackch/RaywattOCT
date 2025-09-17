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
	std::map<OCTHeader::ExtraData, uint8_t*> mapExtraData;
	bool bLongitudeOrientation;
public:
	IDataManager() { m_nNumOfSamples = 0;}
	virtual ~IDataManager() {
		for (auto& kv : mapExtraData) {
			delete[] kv.second;
		}
		mapExtraData.clear();
	}

	int GetNumOfSamples() const { return m_nNumOfSamples; }

	virtual char* GetSample(int nIndex) = 0;
	virtual void AddFrame(void* pFrame) = 0;

	void AddExtraData(OCTHeader::ExtraData extraData, void* pData, int nSize) {
		auto it = mapExtraData.find(extraData);
		if (it != mapExtraData.end()) {
			delete[] it->second;
			mapExtraData.erase(it);
		}
		if (nSize <= 0 || pData == nullptr) {
			PLOGI.printf("Invalid ExtraData input (nullptr or size <= 0)");
			return;
		}
		if (static_cast<size_t>(nSize) > (size_t(2) << 30)) {
			PLOGI.printf("Allocation Size too big");
			return;
		}

		uint8_t* buf = new (std::nothrow) uint8_t[static_cast<size_t>(nSize)];
		if (!buf) {
			PLOGI.printf("Allocation failed");
			return;
		}
		std::memcpy(buf, pData, static_cast<size_t>(nSize));
		mapExtraData[extraData] = buf;
	}

	// raw pointer
	uint8_t* GetExtraData(OCTHeader::ExtraData extraData) {
		auto it = mapExtraData.find(extraData);
		return (it != mapExtraData.end()) ? it->second : nullptr;
	}

	void SetLongitudeOrientation(bool nOrientation)
	{
		PLOGI.printf("[junghw] Datamanager longitude: %d", nOrientation);
		bLongitudeOrientation = nOrientation;
	}
};