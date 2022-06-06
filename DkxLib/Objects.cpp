#include "stdafx.h"

#define OBJ_DEBUG

#include "Object.h"

namespace dkx
{
	size_t g_gcAlloc = 0;				// Amount of memory allocated so far.
	size_t g_gcThreshold = 0x100000;	// Starts at 1 MB
	const size_t k_gcGrowPercent = 75;

	std::map<void*, std::shared_ptr<Object>> g_objects;
}
using namespace dkx;

void GCCollect()
{
#ifdef OBJ_DEBUG
	wprintf(L"Starting garbage collection...\nCurrent allocated: %u\n", g_gcAlloc);
#endif

	auto beforeAlloc = g_gcAlloc;

	// Reset the links on all objects, but keep those which are referenced in native code.
	std::set<void*> links;
	for (auto& o : g_objects)
	{
		if (o.second->GetNativeRefCount() > 0)
		{
			o.second->GCMark(true);
			links.insert(o.first);
		}
		else
		{
			o.second->GCMark(false);
		}
	}

	// Keep iterating through the links until all have been found.
	std::set<void*> newLinks;
	while (links.size())
	{
		for (auto& o : links)
		{
			auto obj = g_objects.find(o);
			if (obj != g_objects.end())
			{
				obj->second->GCMarkLinks(g_objects, newLinks);
			}
		}
		links = newLinks;
		newLinks.clear();
	}

	// Generate a list of objects that are not marked and can be deleted.
	std::set<void*> toDelete;
	g_gcAlloc = 0;
	for (auto& obj : g_objects)
	{
		if (!obj.second->GCIsMarked()) toDelete.insert(obj.first);
		else g_gcAlloc += obj.second->GetSize();
	}

	// Delete the unmarked objects.
	for (auto& obj : toDelete)
	{
		g_objects.erase(obj);
	}

	// Check the new memory allocation.
	auto percent = g_gcAlloc * 100 / g_gcThreshold;
#ifdef OBJ_DEBUG
	wprintf(L"GC complete: before %u after %u alloc percent %u%% of threshold %u\n", beforeAlloc, g_gcAlloc, percent, g_gcThreshold);
#endif
	if (percent > k_gcGrowPercent)
	{
		g_gcThreshold += g_gcThreshold >> 1;	// Grow 50%
#ifdef OBJ_DEBUG
		wprintf(L"GC threshold grew to %u\n", g_gcThreshold);
#endif
	}
}

DllExport void* dkx_new(uint size)
{
	auto obj = std::make_shared<Object>(size);
	g_objects[obj->GetPtr()] = obj;

	g_gcAlloc += size;
	if (g_gcAlloc > g_gcThreshold) GCCollect();

	return obj->GetPtr();
}

DllExport void* dkx_addref(void* ptr)
{
	if (ptr == nullptr) return ptr;

	auto objIter = g_objects.find(ptr);
	if (objIter == g_objects.end())
	{
#ifdef OBJ_DEBUG
		wprintf(L"WARNING: dkx_addref() was called with pointer 0x%08X which does not exist.\n", ptr);
#endif
		return ptr;
	}

	objIter->second->AddNativeRef();
	return ptr;
}

DllExport void* dkx_release(void* ptr)
{
	if (ptr == nullptr) return ptr;

	auto objIter = g_objects.find(ptr);
	if (objIter == g_objects.end())
	{
#ifdef OBJ_DEBUG
		wprintf(L"WARNING: dkx_release() was called with pointer 0x%08X which does not exist.\n", ptr);
#endif
		return ptr;
	}

	objIter->second->ReleaseNativeRef();
	return ptr;
}

DllExport void* dkx_swap(void* oldPtr, void* newPtr)
{
	dkx_release(oldPtr);
	return dkx_addref(newPtr);
}

DllExport void* dkx_swapnoadd(void* oldPtr, void* newPtr)
{
	dkx_release(oldPtr);
	return newPtr;
}

DllExport void* dkx_link(void* fromPtr, void* toPtr)
{
	if (fromPtr == nullptr || toPtr == nullptr) return toPtr;

	auto objIter = g_objects.find(fromPtr);
	if (objIter == g_objects.end())
	{
#ifdef OBJ_DEBUG
		wprintf(L"WARNING: dkx_link() was called with 'from' pointer 0x%08X which does not exist.\n", fromPtr);
#endif
		return toPtr;
	}

#ifdef OBJ_DEBUG
	auto toIter = g_objects.find(toPtr);
	if (toIter == g_objects.end()) wprintf(L"WARNING: dkx_link() was called with 'to' pointer 0x%08X which does not exist.\n", toPtr);
#endif

	objIter->second->AddLink(toPtr);
	return toPtr;
}

DllExport void* dkx_unlink(void* fromPtr, void* toPtr)
{
	if (fromPtr == nullptr || toPtr == nullptr) return toPtr;

	auto objIter = g_objects.find(fromPtr);
	if (objIter == g_objects.end())
	{
#ifdef OBJ_DEBUG
		wprintf(L"WARNING: dkx_link() was called with 'from' pointer 0x%08X which does not exist.\n", fromPtr);
#endif
		return toPtr;
	}

	objIter->second->ReleaseLink(toPtr);
	return toPtr;
}

DllExport void* dkx_swaplink(void* fromPtr, void* oldToPtr, void* newToPtr)
{
	if (fromPtr == nullptr) return newToPtr;

	auto objIter = g_objects.find(fromPtr);
	if (objIter == g_objects.end())
	{
#ifdef OBJ_DEBUG
		wprintf(L"WARNING: dkx_swaplink() was called with 'from' pointer 0x%08X which does not exist.\n", fromPtr);
#endif
		return newToPtr;
	}

	if (oldToPtr != nullptr)
	{
#ifdef OBJ_DEBUG
		auto oldToIter = g_objects.find(oldToPtr);
		if (oldToIter == g_objects.end()) wprintf(L"WARNING: dkx_swaplink() was called with old 'to' pointer 0x%08X which does not exist.\n", oldToPtr);
#endif
		objIter->second->ReleaseLink(oldToPtr);
	}

	if (newToPtr != nullptr)
	{
#ifdef OBJ_DEBUG
		auto newToIter = g_objects.find(newToPtr);
		if (newToIter == g_objects.end()) wprintf(L"WARNING: dkx_swaplink() was called with new 'to' pointer 0x%08X which does not exist.\n", newToPtr);
#endif
		objIter->second->AddLink(newToPtr);
	}

	return newToPtr;
}

DllExport void dkx_gc()
{
	GCCollect();
}

DllExport uint dkx_gccount()
{
	return g_objects.size();
}

DllExport uint dkx_gcalloc()
{
	return g_gcAlloc;
}
