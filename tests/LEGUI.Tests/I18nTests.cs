using Xunit;

namespace LEGUI.Tests;

public class I18nTests
{
    [Fact]
    public void CurrentCultureInfo_IsNotNull()
    {
        Assert.NotNull(I18n.CurrentCultureInfo);
    }

    [Fact]
    public void CurrentCultureInfo_HasNonEmptyName()
    {
        Assert.False(string.IsNullOrEmpty(I18n.CurrentCultureInfo.Name));
    }
}
