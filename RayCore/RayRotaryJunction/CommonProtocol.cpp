#include "CommonProtocol.h"


void ICommonProtocol::addPacket(BYTE* packet, int size)
{
	for (int i = 0; i < size; i++) {
		m_vPacket.push_back(packet[i]);
	}
}
bool ICommonProtocol::sliceUntilSTX(int index)
{
	bool findSTX = false;
	std::vector<char> vPacket;
	for (int i = index; i < m_vPacket.size(); i++) {
		if (m_vPacket[i] == m_stx) findSTX = true;
		if (findSTX) vPacket.push_back(m_vPacket[i]);
	}

	m_vPacket.clear();
	if (findSTX) {
		m_vPacket.assign(vPacket.begin(), vPacket.end());
	}

	return findSTX;
}
bool ICommonProtocol::parseSerialPacket() {
	if (m_vPacket.size() > 0) {
		bool findSTX = true;
		if (m_vPacket[0] != m_stx) {
			findSTX = sliceUntilSTX(1);
		}

		if (findSTX) {
			for (int idxETX = 0; idxETX < m_vPacket.size(); idxETX++) {
				if (m_vPacket[idxETX] == m_etx)
				{
					BYTE length = m_vPacket[LENGTH_IDX];
					if (idxETX != (length - 1)) continue;
					if (length - 2 < 0) {
						PLOGI.printf("Packet Length is Too Small");
						continue;
					}
					BYTE checksum = calcChecksum(&m_vPacket[0], length - 2);
					if (checksum == m_vPacket[length - 2]) {
						handlePacket();
						sliceUntilSTX(idxETX);
					}
					else {
						PLOGI.printf("Checksum mismatch: FID=0x%02X, expected=0x%02X, got=0x%02X", m_vPacket[FID_IDX], checksum, m_vPacket[length - 2]); // Tmp debug log
						sliceUntilSTX(1);
					}
				}
			}
		}
	}

	return true;
}
void ICommonProtocol::getSerialPacket(eFID fid, int dataSize, BYTE* packet, int& packetLength) {
	if (packet == nullptr || dataSize < 0) return;

	packetLength = dataSize + HEADER_LEN;

	packet[0] = m_stx;
	packet[LENGTH_IDX] = (BYTE)packetLength;
	packet[FID_IDX] = (BYTE)fid;
	packet[RET_IDX] = 0;
	packet[PHOTO_IDX] = 0;
	packet[KEY_IDX] = 0;
	packet[packetLength - 1] = m_etx;
}


BYTE ICommonProtocol::calcChecksum(BYTE* packet, int length) {
	unsigned int crc = 0x00;
	for (int i = 1; i < length; i++)
	{
		crc += packet[i];
	}

	return (BYTE)crc;
}