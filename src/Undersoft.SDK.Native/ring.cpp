#include "ring.h"

#define RING_MEMSET                   memset
#define RING_MEMCPY                   memcpy

#define RING_START_MARK             	0xACEFADED
#define RING_END_MARK                	~0xACEFADED

#if RING_MARKS_MEM
#define RING_IS_VALID               	(Data.startMark == RING_START_MARK && Data.endMark == RING_END_MARK && Data.isActive)
#else
#define RING_IS_VALID               	(Data.isActive)
#endif /* RING_MARKS_MEM */
#define RING_MIN(x, y)              	((x) < (y) ? (x) : (y))
#define RING_MAX(x, y)              	((x) > (y) ? (x) : (y))

Ring::Ring()
{
	if (Init(new uint8_t[255], 255))
		Data.isActive = 1;
}
Ring::Ring(size_t size)
{
	if (Init(new uint8_t[size], size))
		Data.isActive = 1;
}
Ring::Ring(size_t size, size_t itemSize)
{
	if (Init((void*)(new uint8_t[size * itemSize]), size, itemSize))
		Data.isActive = 1;
}
Ring::Ring(void* buffer, size_t size)
{
	if (Init((uint8_t*)buffer, size))
		Data.isActive = 1;
}
Ring::Ring(void* buffer, size_t size, size_t itemSize)
{
	if (Init(buffer, size, itemSize))
		Data.isActive = 1;
}

void* Ring::operator[](size_t index)
{
	size_t full, id, r;
	full = FilledSize();
	if (full < 1)
		return NULL;

	id = RING_MIN(index, full - 1);

	r = Data.readId;
	r += id;

	if (r >= Data.bufferSize)
	{
		r -= Data.bufferSize;
	}

	return &Data.buffer[r];
}

uint8_t 	Ring::Init(uint8_t* buffer, size_t size)
{
	if (buffer == NULL || size == 0)
	{
		return 0;
	}

	Data.itemSize = 1;
	Data.bufferSize = size;

	RING_MEMSET(buffer, 0x00, Data.bufferSize);

	Data.buffer = buffer;

#if RING_MARKS_MEM
	Data.startMark = RING_START_MARK;
	Data.endMark = RING_END_MARK;
#endif /* RING_MARKS_MEM */

	return 1;
}
uint8_t 	Ring::Init(void* buffer, size_t size, size_t itemSize)
{
	if (buffer == NULL || size == 0)
	{
		return 0;
	}

	Data.itemSize = itemSize;
	Data.bufferSize = Data.itemSize * size;
	Data.buffer = (uint8_t*)buffer;

	RING_MEMSET(Data.buffer, 0x00, Data.bufferSize);

#if RING_MARKS_MEM
	Data.startMark = RING_START_MARK;
	Data.endMark = RING_END_MARK;
#endif /* RING_MARKS_MEM */

	return 1;
}
uint8_t 	Ring::IsReady()
{
	return RING_IS_VALID;
}
void 		Ring::Free()
{
	if (RING_IS_VALID)
	{
		Data.buffer = NULL;
		delete[] Data.readBuffer;
	}
}

uint8_t 	Ring::Available()
{
	return Data.readId != Data.writeId;
}

size_t 		Ring::Write(RING_VOLATILE const void* data, size_t btw)
{
	size_t tocopy;

	/* Calculate maximum number of bytes available to write */
	btw = RING_MIN(FreeSize(), btw);
	if (btw == 0 || data == NULL)
	{
		return 0;
	}

	const uint8_t* d = (uint8_t*)data;

	/* Step 1: Write data to linear part of data */
	tocopy = RING_MIN(Data.bufferSize - Data.writeId, btw);
	RING_MEMCPY(Data.buffer + Data.writeId, d, tocopy);
	btw -= tocopy;

	/* Step 2: Write data to beginning of data (overflow part) */
	if (btw > 0)
	{
		RING_MEMCPY(Data.buffer, d + tocopy, btw);
		Data.writeId = btw;
	}
	else
	{
		Data.writeId += tocopy;
		/* Step 3: Check end of data */
		if (Data.writeId >= Data.bufferSize)
		{
			Data.writeId = 0;
		}
	}

	return tocopy + btw;
}

uint8_t	Ring::WriteByte(uint8_t byte)
{
	{
		if (FreeSize() < 2)
		{
			return 0;
		}

		Data.buffer[Data.writeId] = byte;

		Data.writeId += 1;

		/* Step 3: Check end of data*/
		if (Data.writeId >= Data.bufferSize)
			Data.writeId = 0;

		return 1;
	}
}
uint8_t	Ring::WriteByte(RING_VOLATILE uint8_t* data, uint32_t index)
{
	if (data == NULL)
		return 0;

	if (FreeSize() < 2)
	{
		return 0;
	}

	Data.buffer[Data.writeId] = data[index];

	Data.writeId += 1;

	/* Step 3: Check end of data*/
	if (Data.writeId >= Data.bufferSize)
		Data.writeId = 0;

	return 1;
}
uint8_t	Ring::OverwriteByte(RING_VOLATILE uint8_t* data, uint32_t index)
{
	if (data == NULL)
		return 0;

	Data.buffer[Data.writeId] = data[index];

	Data.writeId += 1;

	/* Step 3: Check end of data*/
	if (Data.writeId >= Data.bufferSize)
		Data.writeId = 0;

	if (Data.writeId - Data.readId == 0)
	{
		Data.readId += 1;

		if (Data.readId >= Data.bufferSize)
			Data.readId = 0;
	}
	return 1;
}

size_t 		Ring::Rewrite(Ring* destRing, size_t btc)
{
	size_t dll, dwp, srte, srfs = 0, dsz;
	btc = RING_MIN(btc, destRing->FreeSize());
	if (btc == 0)
	{
		return 0;
	}

	dsz = destRing->Data.bufferSize;
	dwp = destRing->Data.writeId;
	dll = RING_MIN(dsz - dwp, btc);
	srte = Read(destRing->Data.buffer + dwp, dll);
	dll -= srte;
	btc -= srte;

	if (btc > 0 && dll < 1)
	{
		srfs = Read(destRing->Data.buffer, btc);
		dwp = srfs;
	}
	else
	{
		dwp += srte;
		if (dwp >= dsz)
		{
			dwp = 0;
		}

	}
	destRing->Data.writeId = dwp;

	return srte + srfs;
}
size_t 		Ring::Rewrite(Ring* destRing)
{
	return Rewrite(destRing, FilledSize());
}

size_t 		Ring::OverRewrite(Ring* destRing, size_t btc)
{
	size_t dll, dwp, srte, srfs = 0, dsz, skip, free;
	if (btc == 0)
	{
		return 0;
	}
	free = destRing->FreeSize();
	skip = btc - free;
	if (skip > 0)
	{
		free += destRing->Skip(skip);
	}
	btc = RING_MIN(btc, free);

	dsz = destRing->Data.bufferSize;
	dwp = destRing->Data.writeId;
	dll = RING_MIN(dsz - dwp, btc);
	srte = Read(destRing->Data.buffer + dwp, dll);
	dll -= srte;
	btc -= srte;

	if (btc > 0 && dll < 1)
	{
		srfs = Read(destRing->Data.buffer, btc);
		dwp = srfs;
	}
	else
	{
		dwp += srte;
		if (dwp >= dsz)
		{
			dwp = 0;
		}
	}
	destRing->Data.writeId = dwp;

	return srte + srfs;
}
size_t 		Ring::OverRewrite(Ring* destRing)
{
	return Overwrite(destRing, FilledSize());
}

size_t 		Ring::Overwrite(RING_VOLATILE const void* data, size_t btw)
{
	size_t tocopy, skip, free;
	btw = RING_MIN(FullSize(), btw);
	if (data == NULL || btw == 0)
	{
		return 0;
	}

	free = FreeSize();
	skip = btw - free;
	if (skip > 0)
	{
		free += Skip(skip);
	}

	const uint8_t* d = (uint8_t*)data;

	/* Calculate maximum number of bytes available to write */


	/* Step 1: Write data to linear part of data */
	tocopy = RING_MIN(Data.bufferSize - Data.writeId, btw);
	RING_MEMCPY(Data.buffer + Data.writeId, d, tocopy);
	btw -= tocopy;

	/* Step 2: Write data to beginning of data (overflow part) */
	if (btw > 0)
	{
		RING_MEMCPY(Data.buffer, d + tocopy, btw);
		Data.writeId = btw;
	}
	else
	{
		Data.writeId += tocopy;
		/* Step 3: Check end of data */
		if (Data.writeId >= Data.bufferSize)
		{
			Data.writeId = 0;
		}
	}

	return tocopy + btw;
}

uint8_t 	Ring::Read()
{

	if (!RING_IS_VALID)
	{
		return false;
	}

	uint32_t w = Data.writeId;
	uint32_t r = Data.readId;

	if (w == r)
	{
		return 0;
	}

	Data.readedByte = Data.buffer[Data.readId++];

	if (Data.readId >= Data.bufferSize)
	{
		Data.readId = 0;
	}

	return 1;
}
uint8_t 	Ring::ReadedByte()
{
	return Data.readedByte;
}
size_t 		Ring::Read(void* data, size_t btr)
{
	size_t tocopy;

	btr = RING_MIN(FilledSize(), btr);

	if (data == NULL || btr == 0)
	{
		return 0;
	}

	uint8_t* d = (uint8_t*)data;

	/* Step 1: Read data from linear part of data */
	tocopy = RING_MIN(Data.bufferSize - Data.readId, btr);
	RING_MEMCPY(d, &Data.buffer[Data.readId], tocopy);
	btr -= tocopy;

	/* Step 2: Read data from beginning of data (overflow part) */
	if (btr > 0)
	{
		RING_MEMCPY(d + tocopy, Data.buffer, btr);
		Data.readId = btr;
	}
	else
	{
		Data.readId += tocopy;
		/* Step 3: Check end of Data */
		if (Data.readId >= Data.bufferSize)
		{
			Data.readId = 0;
		}
	}

	return tocopy + btr;
}
void* Ring::Read(size_t btr)
{
	size_t tocopy;

	/* Calculate maximum number of bytes available to read */
	btr = RING_MIN(FilledSize(), btr);
	if (btr == 0)
	{
		return 0;
	}

	uint8_t* d = ReadBuffer(btr);

	/* Step 1: Read data from linear part of data */
	tocopy = RING_MIN(Data.bufferSize - Data.readId, btr);
	RING_MEMCPY(d, &Data.buffer[Data.readId], tocopy);
	btr -= tocopy;

	/* Step 2: Read data from beginning of data (overflow part) */
	if (btr > 0)
	{
		RING_MEMCPY(&d[tocopy], Data.buffer, btr);
		Data.readId = btr;
	}
	else
	{
		Data.readId += tocopy;
		/* Step 3: Check end of Data */
		if (Data.readId >= Data.bufferSize)
		{
			Data.readId = 0;
		}
	}
	return d;
}
void* Ring::ReadAll()
{
	size_t tocopy;

	/* Calculate maximum number of bytes available to read */
	size_t btr = FilledSize();
	if (btr == 0)
	{
		return 0;
	}

	if (Data.readBufferSize != btr)
	{
		delete[] Data.readBuffer;
		Data.readBuffer = new uint8_t[btr];
		Data.readBufferSize = btr;
	}

	uint8_t* d = ReadBuffer(btr);

	/* Step 1: Read data from linear part of data */
	tocopy = RING_MIN(Data.bufferSize - Data.readId, btr);
	RING_MEMCPY(d, &Data.buffer[Data.readId], tocopy);
	btr -= tocopy;

	/* Step 2: Read data from beginning of data (overflow part) */
	if (btr > 0)
	{
		RING_MEMCPY(&d[tocopy], Data.buffer, btr);
		Data.readId = btr;
	}
	else
	{
		Data.readId += tocopy;
		/* Step 3: Check end of Data */
		if (Data.readId >= Data.bufferSize)
		{
			Data.readId = 0;
		}
	}
	return d;
}

size_t 		Ring::Peek(size_t skip_count, void* data, size_t btp)
{
	size_t full, tocopy, r;

	if (data == NULL || btp == 0)
	{
		return 0;
	}

	uint8_t* d = (uint8_t*)data;

	r = Data.readId;

	/* Calculate maximum number of bytes available to read */
	full = FilledSize();

	if (skip_count >= full)
	{
		return 0;
	}

	r += skip_count;
	full -= skip_count;

	if (r >= Data.bufferSize)
	{
		r -= Data.bufferSize;
	}

	/* Check maximum number of bytes available to read after skip */
	btp = RING_MIN(full, btp);

	/* Step 1: Read data from linear part of data */
	tocopy = RING_MIN(Data.bufferSize - r, btp);
	RING_MEMCPY(d, &Data.buffer[r], tocopy);
	btp -= tocopy;

	/* Step 2: Read data from beginning of data (overflow part) */
	if (btp > 0)
	{
		RING_MEMCPY(&d[tocopy], Data.buffer, btp);
	}
	return tocopy + btp;
}
void* Ring::Peek(size_t skip_count, size_t btp)
{
	size_t full, tocopy, r;

	if (btp == 0)
	{
		return 0;
	}

	r = Data.readId;

	/* Calculate maximum number of bytes available to read */
	full = FilledSize();

	if (skip_count >= full)
	{
		return 0;
	}

	r += skip_count;
	full -= skip_count;

	if (r >= Data.bufferSize)
	{
		r -= Data.bufferSize;
	}

	/* Check maximum number of bytes available to read after skip */
	btp = RING_MIN(full, btp);

	uint8_t* d = ReadBuffer(btp);

	/* Step 1: Read data from linear part of data */
	tocopy = RING_MIN(Data.bufferSize - r, btp);
	RING_MEMCPY(d, &Data.buffer[r], tocopy);
	btp -= tocopy;

	/* Step 2: Read data from beginning of data (overflow part) */
	if (btp > 0)
	{
		RING_MEMCPY(&d[tocopy], Data.buffer, btp);
	}
	return d;
}
void* Ring::PeekAll()
{
	size_t tocopy, r;

	r = Data.readId;

	/* Calculate maximum number of bytes available to read */

	/* Check maximum number of bytes available to read after skip */
	size_t btp = FilledSize();
	if (btp == 0)
	{
		return 0;
	}

	uint8_t* d = ReadBuffer(btp);

	/* Step 1: Read data from linear part of data */
	tocopy = RING_MIN(Data.bufferSize - r, btp);
	RING_MEMCPY(d, &Data.buffer[r], tocopy);
	btp -= tocopy;

	/* Step 2: Read data from beginning of data (overflow part) */
	if (btp > 0)
	{
		RING_MEMCPY(&d[tocopy], Data.buffer, btp);
	}
	return d;
}
void* Ring::PeekLast(size_t btp)
{
	size_t diff = FilledSize() - btp;
	if (diff < 1)
		diff = 0;
	return Peek(diff, btp);
}
void* Ring::PeekFirst(size_t btp)
{
	size_t tocopy, r;

	r = Data.readId;

	btp = RING_MIN(FilledSize(), btp);

	if (btp == 0)
	{
		return 0;
	}

	uint8_t* d = ReadBuffer(btp);

	/* Step 1: Read data from linear part of data */
	tocopy = RING_MIN(Data.bufferSize - r, btp);

	RING_MEMCPY(d, &Data.buffer[r], tocopy);

	btp -= tocopy;

	/* Step 2: Read data from beginning of data (overflow part) */
	if (btp > 0)
	{
		RING_MEMCPY(&d[tocopy], Data.buffer, btp);
	}
	return d;
}

size_t 		Ring::Skip(size_t len)
{
	if (len == 0)
	{
		return 0;
	}

	len = RING_MIN(len, FilledSize());
	Data.readId += len;
	if (Data.readId >= Data.bufferSize)
	{
		Data.readId -= Data.bufferSize;
	}
	return len;
}
size_t 		Ring::Step(size_t len)
{

	if (len == 0)
	{
		return 0;
	}

	len = RING_MIN(len, FreeSize());
	Data.writeId += len;
	if (Data.writeId >= Data.bufferSize)
	{
		Data.writeId -= Data.bufferSize;
	}
	return len;
}

size_t 		Ring::FreeSize()
{
	int32_t s = Data.readId - Data.writeId;

	if (s < 0)
		return Data.bufferSize - (s * -1) - 1;

	return s - 1;

}
size_t 		Ring::FilledSize()
{
	int32_t s = Data.writeId - Data.readId;

	if (s < 0)
		return Data.bufferSize - (s * -1);

	return s;
}
size_t 		Ring::FullSize()
{
	return Data.bufferSize;
}

void 		Ring::Reset()
{
	Data.writeId = 0;
	Data.readId = 0;
}

uint8_t* Ring::ReadBuffer(size_t size)
{
	if (Data.readBufferSize < size)
	{
		delete[] Data.readBuffer;
		Data.readBuffer = new uint8_t[size];
		Data.readBufferSize = size;
	}
	return Data.readBuffer;
}
void* Ring::ReadAddress()
{
	if (!RING_IS_VALID)
	{
		return NULL;
	}
	return &Data.buffer[Data.readId];
}
size_t 		Ring::ReadLinearLength()
{
	size_t w, r, len;

	if (!RING_IS_VALID)
	{
		return 0;
	}

	/* Use temporary values in case they are changed during operations */
	w = Data.writeId;
	r = Data.readId;
	if (w > r)
	{
		len = w - r;
	}
	else if (r > w)
	{
		len = Data.bufferSize - r;
	}
	else
	{
		len = 0;
	}
	return len;
}

void* Ring::WriteAddress()
{
	return &Data.buffer[Data.writeId];
}
size_t 		Ring::WriteLinearLength()
{
	size_t w, r, len;

	/* Use temporary values in case they are changed during operations */
	w = Data.writeId;
	r = Data.readId;
	if (w >= r)
	{
		len = Data.bufferSize - w;
		/*
		 * When read pointer is 0,
		 * maximal length is one less as if too many bytes
		 * are written, data would be considered empty again (r == w)
		 */
		if (r == 0)
		{
			/*
			 * Cannot overflow:
			 * - If r is not 0, statement does not get called
			 * - Data.size cannot be 0 and if r is 0, len is greater 0
			 */
			--len;
		}
	}
	else
	{
		len = r - w - 1;
	}
	return len;
}

void* Ring::StartAddress()
{
	return Data.buffer;
}
