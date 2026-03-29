// LocaleTestApp - Locale Emulator Smoke Test Target
// 將程序的 locale 相關 Windows API 回傳值寫入檔案，供 smoke test 腳本比對。
//
// 重要：LEProc 透過 LeCreateProcess 建立新程序，目標 app 的 stdout
// 不會回傳到 LEProc 的 stdout。因此必須寫檔而非 printf。
//
// 用法：LocaleTestApp.exe <output-file-path>
// 輸出格式為 KEY=VALUE，每行一個，便於 PowerShell 解析。

#include <windows.h>
#include <stdio.h>

int main(int argc, char* argv[])
{
    // 決定輸出位置：若有參數則寫入指定檔案，否則寫入同目錄下的 locale_result.txt
    FILE* out = NULL;
    if (argc > 1)
    {
        out = fopen(argv[1], "w");
    }
    else
    {
        // 預設寫入 exe 同目錄下的 locale_result.txt
        char path[MAX_PATH];
        GetModuleFileNameA(NULL, path, MAX_PATH);
        // 找到最後的反斜線，替換檔名
        char* lastSlash = strrchr(path, '\\');
        if (lastSlash) *(lastSlash + 1) = '\0';
        strcat_s(path, MAX_PATH, "locale_result.txt");
        out = fopen(path, "w");
    }

    if (!out) return 1;

    fprintf(out, "ACP=%u\n", GetACP());
    fprintf(out, "OEMCP=%u\n", GetOEMCP());
    fprintf(out, "LCID=0x%04X\n", GetUserDefaultLCID());
    fprintf(out, "UILANG=0x%04X\n", GetUserDefaultUILanguage());
    fprintf(out, "THREADLOCALE=0x%04X\n", GetThreadLocale());

    fclose(out);
    return 0;
}
