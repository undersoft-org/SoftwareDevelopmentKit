#ifndef _SYSTEM_DATA_RING_H
#define _SYSTEM_DATA_RING_H

#include <string.h>
#include <stdint.h>

#define RING_VOLATILE                       volatile

#define RING_MARKS_MEM                      0

typedef struct
{
#if RING_MARKS_MEM
	uint32_t startMark = 0;
#endif /* RING_MARKS_MEM */
	uint8_t* buffer = NULL;
	uint8_t* readBuffer = NULL;
	volatile uint8_t readedByte = 0;
	volatile size_t bufferSize = 0;
	volatile size_t readId = 0;
	volatile size_t writeId = 0;
	volatile uint8_t isActive = 0;
	volatile size_t readBufferSize = 0;
	volatile size_t itemSize = 0;
#if RING_MARKS_MEM
	uint32_t endMark = 0;
#endif /* RING_MARKS_MEM */
} RingBuffer;

class Ring
{
public:
	~Ring()
	{
		delete[] Data.readBuffer;
	}
	Ring();
	Ring(size_t size);
	Ring(size_t size, size_t itemSize);
	Ring(void* buffer, size_t size);
	Ring(void* buffer, size_t size, size_t itemSize);

	void* operator[](size_t index);

	RING_VOLATILE
		RingBuffer Data;

	uint8_t Init(void* buffer, size_t size, size_t itemSize);
	uint8_t Init(uint8_t* buffer, size_t size);
	uint8_t IsReady();
	void Free();
	void Reset();

	uint8_t Available();

	size_t Write(RING_VOLATILE const void* data, size_t btw);

	uint8_t WriteByte(uint8_t byte);
	uint8_t WriteByte(RING_VOLATILE uint8_t* data, uint32_t index);
	uint8_t OverwriteByte(RING_VOLATILE uint8_t* data, uint32_t index);

	size_t Rewrite(Ring* destRing, size_t btc);
	size_t Rewrite(Ring* destRing);

	size_t OverRewrite(Ring* destRing, size_t btc);
	size_t OverRewrite(Ring* destRing);

	size_t Overwrite(RING_VOLATILE const void* data, size_t btw);

	uint8_t Read();
	uint8_t ReadedByte();
	size_t Read(void* data, size_t btr);

	void* Read(size_t btr);
	void* ReadAll();

	size_t Peek(size_t skip_count, void* data, size_t btp);
	void* Peek(size_t skip_count, size_t btp);
	void* PeekAll();
	void* PeekLast(size_t btp);
	void* PeekFirst(size_t btp);

	size_t FreeSize();
	size_t FilledSize();
	size_t FullSize();

	uint8_t* ReadBuffer(size_t size);
	void* ReadAddress();
	size_t ReadLinearLength();
	size_t Skip(size_t len);

	void* WriteAddress();
	size_t WriteLinearLength();
	size_t Step(size_t len);

	void* StartAddress();

};
#endif /* _SYSTEM_DATA_RING_H */
