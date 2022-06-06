#include "stdafx.h"

using namespace dkx;

#define REPEAT2		REPEAT(00) REPEAT(01) REPEAT(02)
#define REPEAT4		REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04)
#define REPEAT9		REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04) REPEAT(05) REPEAT(06) REPEAT(07) REPEAT(08) REPEAT(09)
#define REPEAT14	REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04) REPEAT(05) REPEAT(06) REPEAT(07) REPEAT(08) REPEAT(09) REPEAT(10) REPEAT(11) REPEAT(12) REPEAT(13) REPEAT(14)
#define REPEAT18	REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04) REPEAT(05) REPEAT(06) REPEAT(07) REPEAT(08) REPEAT(09) REPEAT(10) REPEAT(11) REPEAT(12) REPEAT(13) REPEAT(14) REPEAT(15) REPEAT(16) REPEAT(17) REPEAT(18) 

namespace dkx
{
	int6 int6_zero = { 0 };
	int9 int9_zero = { 0 };
	uns6 uns6_zero = { 0 };
	uns9 uns9_zero = { 0 };
}

DllExport uns4 dkx_rowptr(uns4 ptr) { return ptr; }
DllExport uns4 dkx_chptr(uns4 ptr) { return ptr; }
DllExport void dkx_memcpy(void* dst, void* src, uns4 len) { memcpy(dst, src, len); }

DllExport int2 dkx_getint2(const byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<const int2*>(ptr + offset); }
DllExport uns2 dkx_getuns2(const byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<const uns2*>(ptr + offset); }
DllExport int4 dkx_getint4(const byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<const int4*>(ptr + offset); }
DllExport uns4 dkx_getuns4(const byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<const uns4*>(ptr + offset); }
DllExport const wchar_t* dkx_getstr(const byte* ptr, uint offset) { return ptr == nullptr ? L"" : reinterpret_cast<const wchar_t*>(ptr + offset); }
DllExport unsigned short dkx_getdate(const byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<const uns2*>(ptr + offset); }
DllExport unsigned short dkx_gettime(const byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<const uns2*>(ptr + offset); }

DllExport void dkx_setint2(byte* ptr, uint offset, short value) { if (ptr != nullptr) *reinterpret_cast<short*>(ptr + offset) = value; }
DllExport void dkx_setint4(byte* ptr, uint offset, int value) { if (ptr != nullptr) *reinterpret_cast<int*>(ptr + offset) = value; }
DllExport void dkx_setuns2(byte* ptr, uint offset, unsigned short value) { if (ptr != nullptr) *reinterpret_cast<unsigned short*>(ptr + offset) = value; }
DllExport void dkx_setuns4(byte* ptr, uint offset, unsigned int value) { if (ptr != nullptr) *reinterpret_cast<unsigned int*>(ptr + offset) = value; }
DllExport void dkx_setstr(byte* ptr, uint offset, const wchar_t* value) { if (ptr != nullptr) wcscpy(reinterpret_cast<wchar_t*>(ptr + offset), value); }
DllExport void dkx_setdate(byte* ptr, uint offset, unsigned short value) { if (ptr != nullptr) *reinterpret_cast<unsigned short*>(ptr + offset) = value; }
DllExport void dkx_settime(byte* ptr, uint offset, unsigned short value) { if (ptr != nullptr) *reinterpret_cast<unsigned short*>(ptr + offset) = value; }

// Signed: 1-2 = int1, 3-4 = int2, 5-9 = int4, 10-14 = int6, 15-18 = int8, 19+ = int9
// Unsigned: 1-2 = uns1, 3-4 = uns2, 5-9 = uns4, 10-14 = int6, 15-18 = int8, 19+ = int9

#define REPEAT(xx) DllExport int1 dkx_getint1##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<int1*>(ptr + offset); }
REPEAT2
#undef REPEAT
#define REPEAT(xx) DllExport int2 dkx_getint2##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<int2*>(ptr + offset); }
REPEAT4
#undef REPEAT
#define REPEAT(xx) DllExport int4 dkx_getint4##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<int4*>(ptr + offset); }
REPEAT9
#undef REPEAT
#define REPEAT(xx) DllExport int6 dkx_getint6##xx(byte* ptr, uint offset) { return ptr == nullptr ? int6_zero : *reinterpret_cast<int6*>(ptr + offset); }
REPEAT14
#undef REPEAT
#define REPEAT(xx) DllExport int8 dkx_getint8##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<int8*>(ptr + offset); }
REPEAT18
#undef REPEAT
#undef REPEAT
#define REPEAT(xx) DllExport int9 dkx_getint9##xx(byte* ptr, uint offset) { return ptr == nullptr ? int9_zero : *reinterpret_cast<int9*>(ptr + offset); }
REPEAT18
#undef REPEAT

#define REPEAT(xx) DllExport uns1 dkx_getuns1##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<uns1*>(ptr + offset); }
REPEAT2
#undef REPEAT
#define REPEAT(xx) DllExport uns2 dkx_getuns2##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<uns2*>(ptr + offset); }
REPEAT4
#undef REPEAT
#define REPEAT(xx) DllExport uns4 dkx_getuns4##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<uns4*>(ptr + offset); }
REPEAT9
#undef REPEAT
#define REPEAT(xx) DllExport uns6 dkx_getuns6##xx(byte* ptr, uint offset) { return ptr == nullptr ? uns6_zero : *reinterpret_cast<uns6*>(ptr + offset); }
REPEAT14
#undef REPEAT
#define REPEAT(xx) DllExport uns8 dkx_getuns8##xx(byte* ptr, uint offset) { return ptr == nullptr ? 0 : *reinterpret_cast<uns8*>(ptr + offset); }
REPEAT18
#undef REPEAT
#undef REPEAT
#define REPEAT(xx) DllExport uns9 dkx_getuns9##xx(byte* ptr, uint offset) { return ptr == nullptr ? uns9_zero : *reinterpret_cast<uns9*>(ptr + offset); }
REPEAT18
#undef REPEAT

#define REPEAT(xx) DllExport void dkx_setint1##xx(byte* ptr, uint offset, int1 value) { if (ptr != nullptr) *reinterpret_cast<int1*>(ptr + offset) = value; }
REPEAT2
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setint2##xx(byte* ptr, uint offset, int2 value) { if (ptr != nullptr) *reinterpret_cast<int2*>(ptr + offset) = value; }
REPEAT4
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setint4##xx(byte* ptr, uint offset, int4 value) { if (ptr != nullptr) *reinterpret_cast<int4*>(ptr + offset) = value; }
REPEAT9
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setint6##xx(byte* ptr, uint offset, int6 value) { if (ptr != nullptr) *reinterpret_cast<int6*>(ptr + offset) = value; }
REPEAT14
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setint8##xx(byte* ptr, uint offset, int8 value) { if (ptr != nullptr) *reinterpret_cast<int8*>(ptr + offset) = value; }
REPEAT18
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setint9##xx(byte* ptr, uint offset, int9 value) { if (ptr != nullptr) *reinterpret_cast<int9*>(ptr + offset) = value; }
REPEAT18
#undef REPEAT

#define REPEAT(xx) DllExport void dkx_setuns1##xx(byte* ptr, uint offset, uns1 value) { if (ptr != nullptr) *reinterpret_cast<uns1*>(ptr + offset) = value; }
REPEAT2
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setuns2##xx(byte* ptr, uint offset, uns2 value) { if (ptr != nullptr) *reinterpret_cast<uns2*>(ptr + offset) = value; }
REPEAT4
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setuns4##xx(byte* ptr, uint offset, uns4 value) { if (ptr != nullptr) *reinterpret_cast<uns4*>(ptr + offset) = value; }
REPEAT9
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setuns6##xx(byte* ptr, uint offset, uns6 value) { if (ptr != nullptr) *reinterpret_cast<uns6*>(ptr + offset) = value; }
REPEAT14
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setuns8##xx(byte* ptr, uint offset, uns8 value) { if (ptr != nullptr) *reinterpret_cast<uns8*>(ptr + offset) = value; }
REPEAT18
#undef REPEAT
#define REPEAT(xx) DllExport void dkx_setuns9##xx(byte* ptr, uint offset, uns9 value) { if (ptr != nullptr) *reinterpret_cast<uns9*>(ptr + offset) = value; }
REPEAT18
#undef REPEAT
