using System.Runtime.InteropServices;
using Xunit;

namespace LEProc.Tests;

public class LEBStructLayoutTests
{
    [Fact]
    public void LEB_HasExpectedSize()
    {
        var size = Marshal.SizeOf<LoaderWrapper.LEB>();
        Assert.True(size > 200, $"LEB size should be > 200 bytes, got {size}");
        Assert.True(size < 512, $"LEB size should be < 512 bytes, got {size}");
    }

    [Fact]
    public void TIME_FIELDS_HasExpectedSize()
    {
        // 8 ushorts = 16 bytes
        var size = Marshal.SizeOf<LoaderWrapper.TIME_FIELDS>();
        Assert.Equal(16, size);
    }

    [Fact]
    public void RTL_TIME_ZONE_INFORMATION_HasExpectedSize()
    {
        var size = Marshal.SizeOf<LoaderWrapper.RTL_TIME_ZONE_INFORMATION>();
        // int(4) + byte[64] + TIME_FIELDS(16) + int(4) + byte[64] + TIME_FIELDS(16) + int(4)
        // = 4 + 64 + 16 + 4 + 64 + 16 + 4 = 172
        Assert.Equal(172, size);
    }

    [Fact]
    public void REGISTRY_REDIRECTION_ENTRY64_HasConsistentSize()
    {
        var entrySize = Marshal.SizeOf<LERegistryRedirector.REGISTRY_ENTRY64>();
        var redirectionSize = Marshal.SizeOf<LERegistryRedirector.REGISTRY_REDIRECTION_ENTRY64>();

        // REGISTRY_REDIRECTION_ENTRY64 contains two REGISTRY_ENTRY64
        Assert.Equal(entrySize * 2, redirectionSize);
    }

    [Fact]
    public void UNICODE_STRING64_HasExpectedSize()
    {
        var size = Marshal.SizeOf<LERegistryRedirector.UNICODE_STRING64>();
        // ushort(2) + ushort(2) + long(8) = 12, but may have padding
        Assert.True(size >= 12, $"UNICODE_STRING64 should be >= 12 bytes, got {size}");
    }
}
