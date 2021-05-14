#pragma once

#include <windows.h>

class AutoMem
{
public:
	AutoMem()
	{
		_mem = NULL;
		_size = 0;
		_count = 0;
		_ref = (UINT*)&_size;
	}

	AutoMem(UINT aSize)
	{
		_mem = (LPSTR)::LocalAlloc(LPTR, aSize + sizeof(UINT));
		if (_mem == NULL)
			throw "[AutoMem/LocalAlloc] Memory allocation Error !";

		_ref = (UINT*)_mem;
		*_ref = 1;
		_size = aSize;
		_count = 0;
	}

	AutoMem(AutoMem& aOther)
	{
		_mem = aOther._mem;
		_size = aOther._size;
		_count = aOther._count;
		_ref = (UINT*)_mem;
		*_ref = *_ref + 1;
	}

	virtual ~AutoMem()
	{
		if (*_ref > 0)
			*_ref = *_ref - 1;

		if (*_ref == 0)
		{
			if (_mem != NULL)
				::LocalFree(_mem);

		}
	}

	LPSTR operator()(UINT aOffset = 0)
	{
		if (aOffset < _size)
			return &_mem[sizeof(UINT) + aOffset];
		return &_mem[sizeof(UINT) + _count];
	}

	LPSTR EndPtr()
	{
		return &_mem[sizeof(UINT) + _count];
	}

	char& operator [] (UINT aIndex)
	{
		if (aIndex < _size)
			return *(_mem + sizeof(UINT) + aIndex);
		else
			return *(_mem + sizeof(UINT) + _count);
	}

	const char& operator [] (UINT aIndex) const
	{
		if (aIndex < _size)
			return *(_mem + sizeof(UINT) + aIndex);
		else
			return *(_mem + sizeof(UINT) + _count);
	}

	const char* S()
	{
		_mem[sizeof(UINT) + _count] = 0;
		return &_mem[sizeof(UINT)];
	}

	void Reset(int aFill = 0xFF)
	{
		_count = 0;
		memset(&_mem[sizeof(UINT)], aFill, _size);
	}

	int Remove(UINT aCount)
	{
		if (aCount >= _count)
			Reset();
		else
		{
			::MoveMemory(&_mem[sizeof(UINT)], &_mem[sizeof(UINT) + aCount], _count - aCount);
			::ZeroMemory(&_mem[sizeof(UINT) + (_count - aCount)], aCount);
			_count -= aCount;
		}
		return _count;
	}

	int Resize(UINT aNewSize)
	{
		if (aNewSize <= _size)
		{
			LPSTR old = _mem;
			_mem = (LPSTR)::LocalAlloc(LPTR, aNewSize + sizeof(UINT));
			if (_mem == NULL)
				throw "[AutoMem/Resize] Memory allocation Error !";

			if (old != NULL)
			{
				::CopyMemory(_mem, old, _count + sizeof(UINT));
				::LocalFree(old);
			}
			_ref = (UINT*)_mem;
			_size = aNewSize;
		}

		return _count;
	}
	
	int Size() { return _size; }
	int Count() { return _count; }
	void setCount(UINT aCount) 
	{
		if (aCount >= 0 && aCount <= _size)
			_count = aCount; 
	}

	void increaseCount(UINT aCount)
	{
		if ((_count + aCount) <= _size)
			_count += aCount;
		else
			_count = _size;
	}

	int remainedSize()
	{
		return _size - _count;
	}
private:
	LPSTR _mem;
	UINT* _ref;
	UINT   _count;
	UINT   _size;
};

