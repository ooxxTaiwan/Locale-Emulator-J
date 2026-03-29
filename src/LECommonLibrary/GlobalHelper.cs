using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace LECommonLibrary;

public static class GlobalHelper
{
    public static string GetVersionString()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
    }

    public static void ShowErrorDebugMessageBox(string commandLine, uint errorCode)
    {
        MessageBox.Show(
            $"Error Number: {Convert.ToString(errorCode, 16).ToUpper()}\r\n"
            + $"Commands: {commandLine}\r\n" + "\r\n"
            + $"{Environment.OSVersion} {(SystemHelper.Is64BitOS() ? "x64" : "x86")}\r\n"
            + $"{GenerateSystemDllVersionList()}\r\n"
            + "If you have any antivirus software running, please turn it off and try again.\r\n"
            + "If this window still shows, try go to Safe Mode and drag the application "
            + "executable onto LEProc.exe.\r\n"
            + "If you have tried all above but none of them works, feel free to submit an issue at\r\n"
            + "https://github.com/ooxxTaiwan/Locale-Emulator-J/issues.\r\n" + "\r\n" + "\r\n"
            + "You can press CTRL+C to copy this message to your clipboard.\r\n",
            "Locale Emulator Version " + GetVersionString());
    }

    private static string GenerateSystemDllVersionList()
    {
        string[] dlls = ["NTDLL.DLL", "KERNELBASE.DLL", "KERNEL32.DLL", "USER32.DLL", "GDI32.DLL"];

        var result = new StringBuilder();

        foreach (var dll in dlls)
        {
            var version = FileVersionInfo.GetVersionInfo(
                Path.Combine(
                    Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\",
                    SystemHelper.Is64BitOS()
                        ? @"Windows\SysWOW64\"
                        : @"Windows\System32\",
                    dll));

            result.Append(dll);
            result.Append(": ");
            result.Append(
                $"{version.FileMajorPart}.{version.FileMinorPart}.{version.FileBuildPart}.{version.FilePrivatePart}");
            result.Append("\r\n");
        }

        return result.ToString();
    }

    public static bool CheckCoreDLLs()
    {
        string[] dlls = ["LoaderDll.dll", "LocaleEmulator.dll"];

        return dlls
            .Select(dll => Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".",
                dll))
            .All(File.Exists);
    }
}
