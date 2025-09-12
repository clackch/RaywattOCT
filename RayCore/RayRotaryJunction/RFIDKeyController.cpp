#include "RFIDKeyController.h"

std::vector<std::vector<BYTE>> RFIDKeyController::keys;

bool RFIDKeyController::isDuplicate(BYTE* arr, size_t len) {
	for (const auto& existing : keys) {
		if (existing.size() == len && std::equal(existing.begin(), existing.end(), arr)) {
			return true;
		}
	}
	return false;
}

void RFIDKeyController::readKeys() {
	std::ifstream keyFile(keyFilePath, std::ios::in);
	if (!keyFile.is_open()) {
		writeKeysFile();
		std::ifstream keyFile(keyFilePath, std::ios::in);
		if (!keyFile.is_open()) return;
	}
	std::string line;
	while (std::getline(keyFile, line)) {
		if (line.empty()) continue;
		std::vector<BYTE> key;
		for (int i = 0; i < KEY_LEN; ++i) {
			std::string hexByte = line.substr(i * 2, 2); 
			BYTE byteVal = static_cast<BYTE>(std::stoi(hexByte, nullptr, 16));
			key.push_back(byteVal);
		}
		keys.push_back(key);
	}
	keyFile.close();
}
void RFIDKeyController::addKey(BYTE* key) {
	if (isDuplicate(key, KEY_LEN)) return;
	std::ofstream logFile(keyFilePath, std::ios::app);
	if (!logFile.is_open()) {
		return;
	}
	std::stringstream keyStr;
	for (int i = 0; i < KEY_LEN; i++) {
		keyStr << std::uppercase << std::hex << std::setw(2) << std::setfill('0') << static_cast<int>(key[i]);
	}
	logFile << keyStr.str() << "\n";

	std::vector<BYTE> tnsKey;
	for (int i = 0; i < KEY_LEN; ++i) {
		tnsKey.push_back(key[i]);
	}
	keys.push_back(tnsKey);
}
void RFIDKeyController::loadFirstKey(BYTE* key) {
	if (keys.empty()) {
		readKeys();
	}
	if (keys.empty()) {
		memset(key, 0x00, KEY_LEN);
		return;
	}
	std::vector<BYTE> firstKey = keys.at(0);
	for (int i = 0; i < KEY_LEN; i++) {
		key[i] = firstKey[i];
	}
}

std::vector<std::vector<BYTE>> RFIDKeyController::getKeys() {
	return keys;
}

void RFIDKeyController::writeKeysFile() {
	std::ifstream keyFile(keyFilePath, std::ios::in);
	if (!keyFile.is_open()) {
		std::ofstream newFile(keyFilePath);
		if (!newFile.is_open()) {
			return;
		}
		newFile.close();
		BYTE key[KEY_LEN] = DEFAULT_KEY;
		addKey(key);
		memset(key, 0xFF, KEY_LEN);
		addKey(key);
	}
	return;
}