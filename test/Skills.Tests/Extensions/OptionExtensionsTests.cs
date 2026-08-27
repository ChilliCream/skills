using System.CommandLine;
using Skills.Extensions;
using Xunit;

namespace Skills.Tests.Extensions;

public class OptionExtensionsTests
{
    [Fact]
    public void NonEmptyStringsOnly_String_Rejects_WhitespaceValue()
    {
        var option = new Option<string>("--name").NonEmptyStringsOnly();
        var command = new Command("cmd") { option };

        var parseResult = command.Parse(["--name", " "]);

        Assert.Contains(parseResult.Errors, e => e.Message.Contains("--name", StringComparison.Ordinal));
    }

    [Fact]
    public void NonEmptyStringsOnly_String_Accepts_NonEmptyValue()
    {
        var option = new Option<string>("--name").NonEmptyStringsOnly();
        var command = new Command("cmd") { option };

        var parseResult = command.Parse(["--name", "value"]);

        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void NonEmptyStringsOnly_String_Accepts_MissingValue()
    {
        var option = new Option<string>("--name").NonEmptyStringsOnly();
        var command = new Command("cmd") { option };

        var parseResult = command.Parse([]);

        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void NonEmptyStringsOnly_StringArray_Rejects_EmptyElement()
    {
        var option = new Option<string[]>("--items") { AllowMultipleArgumentsPerToken = true }.NonEmptyStringsOnly();
        var command = new Command("cmd") { option };

        var parseResult = command.Parse(["--items", "a", ""]);

        Assert.Contains(parseResult.Errors, e => e.Message.Contains("--items", StringComparison.Ordinal));
    }

    [Fact]
    public void NonEmptyStringsOnly_StringArray_Accepts_AllNonEmptyElements()
    {
        var option = new Option<string[]>("--items") { AllowMultipleArgumentsPerToken = true }.NonEmptyStringsOnly();
        var command = new Command("cmd") { option };

        var parseResult = command.Parse(["--items", "a", "b"]);

        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void LegalFilePathsOnly_Rejects_PathWithEmbeddedNul()
    {
        var option = new Option<string>("--path");
        option.LegalFilePathsOnly();
        var command = new Command("cmd") { option };

        var parseResult = command.Parse(["--path", "bad\0path"]);

        Assert.Contains(parseResult.Errors, e => e.Message.Contains("Invalid file path", StringComparison.Ordinal));
    }

    [Fact]
    public void LegalFilePathsOnly_Accepts_ValidPath()
    {
        var option = new Option<string>("--path");
        option.LegalFilePathsOnly();
        var command = new Command("cmd") { option };

        var parseResult = command.Parse(["--path", "/tmp/some/path"]);

        Assert.Empty(parseResult.Errors);
    }
}
