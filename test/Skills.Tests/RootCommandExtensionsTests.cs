using Skills.Extensions;
using Xunit;

namespace Skills.Tests;

public class RootCommandExtensionsTests
{
    [Fact]
    public void StripBareTerminators_Removes_Terminator_Between_Options_And_Positionals()
    {
        // Act & Assert
        Assert.Equal(
            ["add", "--agent", "codex", "owner/repo"],
            RootCommandExtensions.StripBareTerminators(["add", "--agent", "codex", "--", "owner/repo"]));
    }

    [Fact]
    public void StripBareTerminators_Removes_Terminator_Before_OptionLike_Value()
    {
        // Act & Assert
        Assert.Equal(
            ["add", "--upload-pack=sh"],
            RootCommandExtensions.StripBareTerminators(["add", "--", "--upload-pack=sh"]));
    }

    [Fact]
    public void StripBareTerminators_Removes_Multiple_Terminators()
    {
        // Act & Assert
        Assert.Equal(
            ["add", "owner/repo"],
            RootCommandExtensions.StripBareTerminators(["--", "add", "--", "owner/repo", "--"]));
    }
}
