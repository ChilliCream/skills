using System.CommandLine.Parsing;

namespace Skills.Extensions;

/// <summary>
/// Validators attached directly to an <see cref="Option{T}"/> so bad input is rejected during
/// parsing, before a command's <c>ExecuteAsync</c> ever sees it.
/// </summary>
internal static class OptionExtensions
{
    public static Option<string> NonEmptyStringsOnly(this Option<string> option)
    {
        option.Validators.Add(result =>
        {
            var value = result.GetValue(option);
            if (value is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                var optionName = option.Name.StartsWithOrdinal("--") ? option.Name : $"--{option.Name}";
                result.AddError($"Expected a non-empty value for '{optionName}'.");
            }
        });
        return option;
    }

    public static Option<string[]> NonEmptyStringsOnly(this Option<string[]> option)
    {
        option.Validators.Add(result =>
        {
            var values = result.GetValue(option);
            if (values is null)
            {
                return;
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    var optionName = option.Name.StartsWithOrdinal("--") ? option.Name : $"--{option.Name}";
                    result.AddError($"Expected a non-empty value for '{optionName}'.");
                    return;
                }
            }
        });
        return option;
    }

    public static void LegalFilePathsOnly(this Option<string> option)
    {
        option.Validators.Add(result =>
        {
            foreach (var token in result.Tokens)
            {
                ValidateFilePath(result, token.Value);
            }
        });
    }

    private static void ValidateFilePath(OptionResult result, string path)
    {
        try
        {
            Path.GetFullPath(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            result.AddError($"Invalid file path: '{path}'.");
        }
    }
}
