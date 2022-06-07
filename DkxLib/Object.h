namespace dkx
{
	class Object
	{
	private:
		void* _ptr;
		size_t _size;
		int _nativeRefCount;
		std::vector<void*> _linkRefs;
		bool _gcMark;	// To mark which objects are in referenced during GC

	public:
		Object(size_t size)
			: _ptr(nullptr)
			, _size(size)
			, _nativeRefCount(1)
		{
			_ptr = malloc(_size);
			memset(_ptr, 0, _size);
#if OBJ_DEBUG >= 2
			wprintf(L"Allocate object: 0x%08X size: %d\n", _ptr, _size);
#endif
		}

		~Object()
		{
#if OBJ_DEBUG >= 2
			wprintf(L"Delete object: 0x%08X size: %d\n", _ptr, _size);
#endif
			free(_ptr);
		}

		void* GetPtr() { return _ptr; }
		const void* GetPtr() const { return _ptr; }
		size_t GetSize() const { return _size; }

		void AddNativeRef()
		{
			_nativeRefCount++;
#if OBJ_DEBUG >= 3
			wprintf(L"Native refcount on object 0x%08X increased to %d\n", _ptr, _nativeRefCount);
#endif
		}

		void ReleaseNativeRef()
		{
			_nativeRefCount--;
#if OBJ_DEBUG >= 3
			wprintf(L"Native refcount on object 0x%08X has dropped to %d\n", _ptr, _nativeRefCount);
#endif
#if OBJ_DEBUG >= 1
			if (_nativeRefCount < 0) wprintf(L"WARNING: Native refcount on object 0x%08X has dropped to %d\n", _ptr, _nativeRefCount);
#endif
		}

		int GetNativeRefCount() const { return _nativeRefCount; }

		void AddLink(void* ptr)
		{
			_linkRefs.push_back(ptr);
#if OBJ_DEBUG >= 3
			wprintf(L"Link ref added on object 0x%08X to 0x%08X\n", _ptr, ptr);
#endif
		}

		void ReleaseLink(void* ptr)
		{
			auto iter = std::find(_linkRefs.begin(), _linkRefs.end(), ptr);
			if (iter == _linkRefs.end())
			{
#if OBJ_DEBUG >= 3
			wprintf(L"Link ref released on object 0x%08X to 0x%08X\n", _ptr, ptr);
#endif
#if OBJ_DEBUG >= 1
				wprintf(L"WARNING: Attempted to release link on object 0x%08X to 0x%08X but it was not found\n", _ptr, ptr);
#endif
				return;
			}

			_linkRefs.erase(iter);
		}

		bool GCIsMarked() const { return _gcMark; }
		void GCMark(bool value) { _gcMark = value; }
		void GCMarkLinks(std::map<void*, std::shared_ptr<Object>> &objMap, std::set<void*> &newLinks)
		{
			for (auto ptr : _linkRefs)
			{
				auto obj = objMap.find(ptr);
				if (obj != objMap.end())
				{
					if (!obj->second->GCIsMarked())
					{
						obj->second->GCMark(true);
						newLinks.insert(ptr);
					}
				}
				else
				{
#if OBJ_DEBUG >= 1
					wprintf(L"WARNING: Object 0x%08X contains a link to non-existent 0x%08X\n", _ptr, ptr);
#endif
				}
			}
		}
	};
}
