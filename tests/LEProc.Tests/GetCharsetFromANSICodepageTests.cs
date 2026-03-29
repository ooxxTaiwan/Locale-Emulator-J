using Xunit;

namespace LEProc.Tests;

public class GetCharsetFromANSICodepageTests
{
    [Theory]
    [InlineData(932, 128)]   // Japanese -> SHIFTJIS_CHARSET
    [InlineData(936, 134)]   // Simplified Chinese -> GB2312_CHARSET
    [InlineData(949, 129)]   // Korean -> HANGEUL_CHARSET
    [InlineData(950, 136)]   // Traditional Chinese -> CHINESEBIG5_CHARSET
    [InlineData(1250, 238)]  // Eastern Europe -> EASTEUROPE_CHARSET
    [InlineData(1251, 204)]  // Russian -> RUSSIAN_CHARSET
    [InlineData(1252, 0)]    // Western -> ANSI_CHARSET
    [InlineData(1253, 161)]  // Greek -> GREEK_CHARSET
    [InlineData(1254, 162)]  // Turkish -> TURKISH_CHARSET
    [InlineData(1255, 177)]  // Hebrew -> HEBREW_CHARSET
    [InlineData(1256, 178)]  // Arabic -> ARABIC_CHARSET
    [InlineData(1257, 186)]  // Baltic -> BALTIC_CHARSET
    [InlineData(874, 222)]   // Thai -> THAI_CHARSET
    [InlineData(1258, 163)]  // Vietnamese -> VIETNAMESE_CHARSET
    [InlineData(437, 0)]     // OEM US -> not in switch, returns ANSI_CHARSET (0)
    [InlineData(0, 0)]       // Unknown -> ANSI_CHARSET (0)
    public void GetCharsetFromANSICodepage_ReturnsExpectedCharset(int codepage, int expectedCharset)
    {
        var result = Program.GetCharsetFromANSICodepage(codepage);
        Assert.Equal(expectedCharset, result);
    }
}
