#ifndef DKX_I
#define DKX_I

#ifndef _I
#define _I(x) x
#endif

#define REPEAT2		REPEAT(00) REPEAT(01) REPEAT(02)
#define REPEAT4		REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04)
#define REPEAT9		REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04) REPEAT(05) REPEAT(06) REPEAT(07) REPEAT(08) REPEAT(09)
#define REPEAT14	REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04) REPEAT(05) REPEAT(06) REPEAT(07) REPEAT(08) REPEAT(09) REPEAT(10) REPEAT(11) REPEAT(12) REPEAT(13) REPEAT(14)
#define REPEAT18	REPEAT(00) REPEAT(01) REPEAT(02) REPEAT(03) REPEAT(04) REPEAT(05) REPEAT(06) REPEAT(07) REPEAT(08) REPEAT(09) REPEAT(10) REPEAT(11) REPEAT(12) REPEAT(13) REPEAT(14) REPEAT(15) REPEAT(16) REPEAT(17) REPEAT(18) 

extern unsigned int dkx_rowptr(numeric(9) unsigned &ptr);
extern unsigned int dkx_chptr(char(1) &ptr);
extern void dkx_memcpy(unsigned int dst, unsigned int src, unsigned int len);

extern short dkx_getint2(unsigned int ptr, unsigned int offset);
extern int dkx_getint4(unsigned int ptr, unsigned int offset);
extern unsigned short dkx_getuns2(unsigned int ptr, unsigned int offset);
extern unsigned int dkx_getuns4(unsigned int ptr, unsigned int offset);
extern char(255) dkx_getstr(unsigned int ptr, unsigned int offset);
extern date dkx_getdate(unsigned int ptr, unsigned int offset);
extern time dkx_gettime(unsigned int ptr, unsigned int offset);

extern void dkx_setint2(unsigned int ptr, unsigned int offset, short value);
extern void dkx_setint4(unsigned int ptr, unsigned int offset, int value);
extern void dkx_setuns2(unsigned int ptr, unsigned int offset, unsigned short value);
extern void dkx_setuns4(unsigned int ptr, unsigned int offset, unsigned int value);
extern void dkx_setstr(unsigned int ptr, unsigned int offset, char(255) value);
extern void dkx_setdate(unsigned int ptr, unsigned int offset, date value);
extern void dkx_settime(unsigned int ptr, unsigned int offset, time value);

// Signed: 1-2 = int1, 3-4 = int2, 5-9 = int4, 10-14 = int6, 15-18 = int8, 19+ = int9
// Unsigned: 1-2 = uns1, 3-4 = uns2, 5-9 = uns4, 10-14 = int6, 15-18 = int8, 19+ = int9

#define REPEAT(xx) extern numeric(2,xx) _I(dkx_getint1)xx(unsigned int ptr, unsigned int offset);
REPEAT2
#undef REPEAT
#define REPEAT(xx) extern numeric(4,xx) _I(dkx_getint2)xx(unsigned int ptr, unsigned int offset);
REPEAT4
#undef REPEAT
#define REPEAT(xx) extern numeric(9,xx) _I(dkx_getint4)xx(unsigned int ptr, unsigned int offset);
REPEAT9
#undef REPEAT
#define REPEAT(xx) extern numeric(14,xx) _I(dkx_getint6)xx(unsigned int ptr, unsigned int offset);
REPEAT14
#undef REPEAT
#define REPEAT(xx) extern numeric(18,xx) _I(dkx_getint8)xx(unsigned int ptr, unsigned int offset);
REPEAT18
#undef REPEAT
#define REPEAT(xx) extern numeric(38,xx) _I(dkx_getint9)xx(unsigned int ptr, unsigned int offset);
REPEAT18
#undef REPEAT

#define REPEAT(xx) extern numeric(2,xx) _I(dkx_getuns1)xx(unsigned int ptr, unsigned int offset);
REPEAT2
#undef REPEAT
#define REPEAT(xx) extern numeric(4,xx) _I(dkx_getuns2)xx(unsigned int ptr, unsigned int offset);
REPEAT4
#undef REPEAT
#define REPEAT(xx) extern numeric(9,xx) _I(dkx_getuns4)xx(unsigned int ptr, unsigned int offset);
REPEAT9
#undef REPEAT
#define REPEAT(xx) extern numeric(14,xx) _I(dkx_getuns6)xx(unsigned int ptr, unsigned int offset);
REPEAT14
#undef REPEAT
#define REPEAT(xx) extern numeric(18,xx) _I(dkx_getuns8)xx(unsigned int ptr, unsigned int offset);
REPEAT18
#undef REPEAT
#define REPEAT(xx) extern numeric(38,xx) _I(dkx_getuns9)xx(unsigned int ptr, unsigned int offset);
REPEAT18
#undef REPEAT

#define REPEAT(xx) extern void _I(dkx_setint1)xx(unsigned int ptr, unsigned int offset, numeric(2,xx) unsigned value);
REPEAT2
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setint2)xx(unsigned int ptr, unsigned int offset, numeric(4,xx) unsigned value);
REPEAT4
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setint4)xx(unsigned int ptr, unsigned int offset, numeric(9,xx) unsigned value);
REPEAT9
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setint6)xx(unsigned int ptr, unsigned int offset, numeric(14,xx) unsigned value);
REPEAT14
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setint8)xx(unsigned int ptr, unsigned int offset, numeric(18,xx) unsigned value);
REPEAT18
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setint9)xx(unsigned int ptr, unsigned int offset, numeric(38,xx) unsigned value);
REPEAT18
#undef REPEAT

#define REPEAT(xx) extern void _I(dkx_setuns1)xx(unsigned int ptr, unsigned int offset, numeric(2,xx) unsigned value);
REPEAT2
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setuns2)xx(unsigned int ptr, unsigned int offset, numeric(4,xx) unsigned value);
REPEAT4
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setuns4)xx(unsigned int ptr, unsigned int offset, numeric(9,xx) unsigned value);
REPEAT9
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setuns6)xx(unsigned int ptr, unsigned int offset, numeric(14,xx) unsigned value);
REPEAT14
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setuns8)xx(unsigned int ptr, unsigned int offset, numeric(18,xx) unsigned value);
REPEAT18
#undef REPEAT
#define REPEAT(xx) extern void _I(dkx_setuns9)xx(unsigned int ptr, unsigned int offset, numeric(38,xx) unsigned value);
REPEAT18
#undef REPEAT

extern unsigned int dkx_new(unsigned int size);
extern unsigned int dkx_addref(unsigned int ptr);
extern unsigned int dkx_release(unsigned int ptr);
extern unsigned int dkx_releasedefer(unsigned int ptr);
extern void dkx_releasenow();
extern unsigned int dkx_swap(unsigned int oldPtr, unsigned int newPtr);
extern unsigned int dkx_swapnoadd(unsigned int oldPtr, unsigned int newPtr);
extern unsigned int dkx_link(unsigned int fromPtr, unsigned int toPtr);
extern unsigned int dkx_unlink(unsigned int fromPtr, unsigned int toPtr);
extern unsigned int dkx_swaplink(unsigned int fromPtr, unsigned int oldToPtr, unsigned int newToPtr);

extern void dkx_gc();
extern unsigned int dkx_gccount();
extern unsigned int dkx_gcalloc();


#endif
