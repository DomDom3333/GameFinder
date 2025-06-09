using System.Text.RegularExpressions;

namespace GameFinder;

public static class Misc
{
    public static string RemoveHtmlTags(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }
        // This regex pattern will match any HTML-like tags
        string pattern = "<.*?>";
        return Regex.Replace(input, pattern, string.Empty);
    }
}