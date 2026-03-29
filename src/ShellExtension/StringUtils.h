// src/ShellExtension/StringUtils.h
// Shared string conversion utilities for the shell extension.
#pragma once

#include <string>
#include <windows.h>

namespace StringUtils
{

inline std::wstring Utf8ToWide(const std::string& utf8)
{
    if (utf8.empty())
        return {};

    int len = MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(),
                                  static_cast<int>(utf8.size()), nullptr, 0);
    if (len <= 0)
        return {};

    std::wstring wide(len, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, utf8.c_str(),
                        static_cast<int>(utf8.size()), &wide[0], len);
    return wide;
}

} // namespace StringUtils
