using Xunit;

namespace LECommonLibrary.Tests;

public class PEFileReaderTests : IDisposable
{
    private readonly string _tempDir;

    public PEFileReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "PEFileReaderTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void GetPEType_NullOrEmpty_ReturnsUnknown()
    {
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(null!));
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(string.Empty));
    }

    [Fact]
    public void GetPEType_NonExistentFile_ReturnsUnknown()
    {
        var path = Path.Combine(_tempDir, "nonexistent.exe");
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_TooSmallFile_ReturnsUnknown()
    {
        var path = Path.Combine(_tempDir, "tiny.exe");
        File.WriteAllBytes(path, new byte[] { 0x4D, 0x5A });  // MZ but too short
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_NotMZSignature_ReturnsUnknown()
    {
        var path = Path.Combine(_tempDir, "not_pe.exe");
        File.WriteAllBytes(path, new byte[256]);  // all zeros, not MZ
        Assert.Equal(PEType.Unknown, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_X86PE_ReturnsX32()
    {
        var pe = new byte[256];
        pe[0] = 0x4D; pe[1] = 0x5A;  // MZ signature
        pe[0x3C] = 0x80;  // PE header at offset 0x80
        pe[0x80] = 0x50; pe[0x81] = 0x45; pe[0x82] = 0x00; pe[0x83] = 0x00;
        pe[0x84] = 0x4C; pe[0x85] = 0x01;  // 0x014C = x86

        var path = Path.Combine(_tempDir, "x86.exe");
        File.WriteAllBytes(path, pe);

        Assert.Equal(PEType.X32, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_X64PE_ReturnsX64()
    {
        var pe = new byte[256];
        pe[0] = 0x4D; pe[1] = 0x5A;
        pe[0x3C] = 0x80;
        pe[0x80] = 0x50; pe[0x81] = 0x45; pe[0x82] = 0x00; pe[0x83] = 0x00;
        pe[0x84] = 0x64; pe[0x85] = 0x86;  // 0x8664 = x64

        var path = Path.Combine(_tempDir, "x64.exe");
        File.WriteAllBytes(path, pe);

        Assert.Equal(PEType.X64, PEFileReader.GetPEType(path));
    }

    [Fact]
    public void GetPEType_RealNotepad_ReturnsNonUnknown()
    {
        var notepadPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "notepad.exe");

        if (!File.Exists(notepadPath))
            return;

        var result = PEFileReader.GetPEType(notepadPath);
        Assert.NotEqual(PEType.Unknown, result);
    }
}
