// src/ShellExtension/ClassFactory.h
#pragma once

#include <unknwn.h>
#include <windows.h>
#include <shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

class ClassFactory : public IClassFactory
{
public:
    // IUnknown
    IFACEMETHODIMP QueryInterface(REFIID riid, void** ppv) override;
    IFACEMETHODIMP_(ULONG) AddRef() override;
    IFACEMETHODIMP_(ULONG) Release() override;

    // IClassFactory
    IFACEMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv) override;
    IFACEMETHODIMP LockServer(BOOL fLock) override;

    ClassFactory();

private:
    ~ClassFactory();
    LONG m_refCount;
};
