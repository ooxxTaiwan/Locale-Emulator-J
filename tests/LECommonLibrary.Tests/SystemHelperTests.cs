using Xunit;

namespace LECommonLibrary.Tests;

public class SystemHelperTests
{
    [Fact]
    public void Is64BitOS_ReturnsConsistentResult()
    {
        var result = SystemHelper.Is64BitOS();
        Assert.Equal(Environment.Is64BitOperatingSystem, result);
    }

    [Fact]
    public void EnsureAbsolutePath_AbsolutePath_ReturnsSame()
    {
        var path = @"C:\Windows\System32\notepad.exe";
        var result = SystemHelper.EnsureAbsolutePath(path);
        Assert.Equal(path, result);
    }

    [Fact]
    public void EnsureAbsolutePath_RelativePath_ReturnsAbsolute()
    {
        var result = SystemHelper.EnsureAbsolutePath("test.exe");
        Assert.True(Path.IsPathRooted(result));
        Assert.EndsWith("test.exe", result);
    }

    [Fact]
    public void RedirectToWow64_System32Path_ReplacesSysWOW64()
    {
        if (!Environment.Is64BitOperatingSystem)
            return; // SysWOW64 only exists on 64-bit OS

        var input = @"%SystemRoot%\System32\test.dll";
        var result = SystemHelper.RedirectToWow64(input);
        Assert.Contains("syswow64", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("system32", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckPermission_TempDirectory_ReturnsTrue()
    {
        var result = SystemHelper.CheckPermission(Path.GetTempPath());
        Assert.True(result);
    }

    [Fact]
    public void CheckPermission_NonExistentDirectory_ReturnsFalse()
    {
        var result = SystemHelper.CheckPermission(@"C:\NonExistentDir_" + Guid.NewGuid().ToString("N"));
        Assert.False(result);
    }

    [Fact]
    public void IsAdministrator_DoesNotThrow()
    {
        var result = SystemHelper.IsAdministrator();
        Assert.IsType<bool>(result);
    }
}
