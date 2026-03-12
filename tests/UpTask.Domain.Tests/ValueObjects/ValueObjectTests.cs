using FluentAssertions;
using UpTask.Domain.Exceptions;
using UpTask.Domain.ValueObjects;
using Xunit;

namespace UpTask.Domain.Tests.ValueObjects;

public sealed class EmailTests
{
    [Theory]
    [InlineData("user@example.com", "user@example.com")]
    [InlineData("USER@EXAMPLE.COM", "user@example.com")]
    [InlineData("  user@example.com  ", "user@example.com")]
    public void Email_WithValidInput_ShouldNormalizeToLowerCase(string input, string expected)
    {
        var email = new Email(input);
        email.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("notanemail")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@.com")]
    [InlineData("user@domain.")]
    public void Email_WithInvalidInput_ShouldThrowDomainException(string input)
    {
        var act = () => new Email(input);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Email_EqualityIsValueBased()
    {
        var email1 = new Email("user@example.com");
        var email2 = new Email("USER@EXAMPLE.COM");

        email1.Should().Be(email2);
    }
}

public sealed class HexColorTests
{
    [Theory]
    [InlineData("#1976D2", "#1976D2")]
    [InlineData("#1976d2", "#1976D2")]
    [InlineData("#FFF", "#FFF")]
    public void HexColor_WithValidInput_ShouldNormalizeToUpperCase(string input, string expected)
    {
        var color = new HexColor(input);
        color.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("1976D2")]   // missing #
    [InlineData("#GGGGGG")]  // invalid hex chars
    [InlineData("#12345")]   // wrong length
    [InlineData("")]
    public void HexColor_WithInvalidInput_ShouldThrowDomainException(string input)
    {
        var act = () => new HexColor(input);
        act.Should().Throw<DomainException>();
    }
}

public sealed class TaskTitleTests
{
    [Fact]
    public void TaskTitle_ShouldTrimWhitespace()
    {
        var title = new TaskTitle("  My Task  ");
        title.Value.Should().Be("My Task");
    }

    [Fact]
    public void TaskTitle_ExceedingMaxLength_ShouldThrowDomainException()
    {
        var longTitle = new string('X', TaskTitle.MaxLength + 1);
        var act = () => new TaskTitle(longTitle);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TaskTitle_AtExactMaxLength_ShouldSucceed()
    {
        var title = new TaskTitle(new string('X', TaskTitle.MaxLength));
        title.Value.Length.Should().Be(TaskTitle.MaxLength);
    }
}
