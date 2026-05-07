namespace Firely.Fhir.Validation.Compilation;

internal class StringCorrector() : RegexCorrector("string", STRING_REGEX)
{
    private const string STRING_REGEX = @"[\r\n\t\u0020-\uFFFF]*";
}