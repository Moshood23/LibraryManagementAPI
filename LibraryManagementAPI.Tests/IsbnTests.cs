using LibraryManagementAPI.Validation;
using Xunit;

namespace LibraryManagementAPI.Tests;

public class IsbnTests
{
    [Theory]
    [InlineData("9780451524935")]
    [InlineData("978-0-385-47454-2")]
    [InlineData("0-306-40615-2")]
    [InlineData("080442957X")]
    [InlineData("080442957x")]
    [InlineData("978 0 306 40615 7")]
    public void ValidIsbns_Pass(string isbn) => Assert.True(Isbn.IsValid(isbn));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("123")]
    [InlineData("9780451524936")]   // wrong check digit
    [InlineData("0306406153")]      // wrong check digit
    [InlineData("97804515249X5")]   // X only allowed as ISBN-10 check digit
    [InlineData("abcdefghij")]
    public void InvalidIsbns_Fail(string? isbn) => Assert.False(Isbn.IsValid(isbn));

    [Fact]
    public void Normalize_StripsHyphensAndSpaces_AndUppercasesX() =>
        Assert.Equal("080442957X", Isbn.Normalize(" 0-8044-2957-x "));
}
