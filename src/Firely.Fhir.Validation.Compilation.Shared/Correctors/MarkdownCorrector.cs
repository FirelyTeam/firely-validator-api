namespace Firely.Fhir.Validation.Compilation;

internal class MarkdownCorrector() : RegexCorrector("markdown", MARKDOWN_REGEX)
{
    private const string MARKDOWN_REGEX = @"[\r\n\t\u0020-\uFFFF]*";
}