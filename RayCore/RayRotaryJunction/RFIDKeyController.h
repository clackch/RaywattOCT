#pragma once

#include <vector>
#include <list>
#include <fstream>
#include <sstream>
#include <iomanip>
#define keyFilePath "./keys.txt"
#define KEY_LEN 6
#define DEFAULT_KEY { 0x7A, 0x3F, 0x2B, 0xC1, 0x8E, 0x49 }

typedef unsigned char       BYTE;

class RFIDKeyController
{
private:
	static std::vector<std::vector<BYTE>> keys;
	static bool isDuplicate(BYTE* arr, size_t len);

public:
	void static readKeys();
	void static addKey(BYTE* key);
	void static loadFirstKey(BYTE* key);
  void static writeKeysFile();
	static const std::vector<std::vector<BYTE>>& getKeys();
};