// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers

#include <string.h>

#include <map>
#include <memory>
#include <set>
#include <vector>

#define DllExport extern "C" _declspec(dllexport)

namespace dkx
{
	typedef unsigned char byte;
	typedef unsigned int uint;

	typedef char int1;
	typedef unsigned char uns1;
	typedef short int2;
	typedef unsigned short uns2;
	typedef int int4;
	typedef unsigned int uns4;
	typedef struct { unsigned char bytes[6]; } int6;
	typedef struct { unsigned char bytes[6]; } uns6;
	typedef __int64 int8;
	typedef unsigned __int64 uns8;
	typedef struct { unsigned char bytes[9]; } int9;
	typedef struct { unsigned char bytes[9]; } uns9;
}
