using Ganss.Xss;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Infrastructure.Services;

public class HtmlSanitizerService : ISanitizer
{
    private static readonly HtmlSanitizer _sanitizer = BuildSanitizer();

    public string Sanitize(string input) => _sanitizer.Sanitize(input);

    private static HtmlSanitizer BuildSanitizer()
    {
        var s = new HtmlSanitizer();
        s.AllowedTags.Clear();
        s.AllowedAttributes.Clear();
        s.AllowedCssProperties.Clear();
        s.AllowedSchemes.Clear();
        return s;
    }
}
