// src/ShellExtension/ClassFactory.cpp
#include "ClassFactory.h"
#include "ContextMenuHandler.h"
#include <new>

extern LONG g_dllRefCount;

ClassFactory::ClassFactory()
    : m_refCount(1)
{
    InterlockedIncrement(&g_dllRefCount);
}

ClassFactory::~ClassFactory()
{
    InterlockedDecrement(&g_dllRefCount);
}

// IUnknown

IFACEMETHODIMP ClassFactory::QueryInterface(REFIID riid, void** ppv)
{
    static const QITAB qit[] =
    {
        QITABENT(ClassFactory, IClassFactory),
        { nullptr, 0 },
    };
    return QISearch(this, qit, riid, ppv);
}

IFACEMETHODIMP_(ULONG) ClassFactory::AddRef()
{
    return InterlockedIncrement(&m_refCount);
}

IFACEMETHODIMP_(ULONG) ClassFactory::Release()
{
    ULONG refCount = InterlockedDecrement(&m_refCount);
    if (refCount == 0)
        delete this;
    return refCount;
}

// IClassFactory

IFACEMETHODIMP ClassFactory::CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv)
{
    if (pUnkOuter != nullptr)
        return CLASS_E_NOAGGREGATION;

    auto* handler = new (std::nothrow) ContextMenuHandler();
    if (!handler)
        return E_OUTOFMEMORY;

    HRESULT hr = handler->QueryInterface(riid, ppv);
    handler->Release(); // QI already AddRef'd if successful
    return hr;
}

IFACEMETHODIMP ClassFactory::LockServer(BOOL fLock)
{
    if (fLock)
        InterlockedIncrement(&g_dllRefCount);
    else
        InterlockedDecrement(&g_dllRefCount);
    return S_OK;
}
