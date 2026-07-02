namespace Translator.Core;

public class PromptBuilder
{
    public const string DefaultStyle =
        "Natural, idiomatic, slighltly casual. Sound like a fluent bilingual speaker" +
        " writing quickly to a collegue. Translate intent, not words." +
        " Leave technical terms and proper nouns untouched.";

    public static string Build(string text, string styleGuide = DefaultStyle)
    {
        var style = string.IsNullOrWhiteSpace(styleGuide) ? DefaultStyle : styleGuide;
        return $"""
                        You translate between English and Swedish for a fluent bilingual user.
                        Step 1. Detect input language. If English, translate to Swedish. If Swedish, translate to English.
                        Step 2. Translate following this style guide exactly:
                        {style}
                        Input:
                        \"\"\"{text}\"\"\"
                """;
    }

    public static string Build(string text, string styleGuide, string targetLanguage)
    {
        var style = string.IsNullOrWhiteSpace(styleGuide) ? DefaultStyle : styleGuide;
        return $"""
                        You translate into {targetLanguage} for a fluent multilingual user.
                        Step 1. Detect the input language and report it as "source".
                        If the input is already {targetLanguage}, return the text unchanged with source equal to target.
                        Step 2. Otherwise translate following this style guide exactly:
                        {style}
                        Input:
                        \"\"\"{text}\"\"\"
                """;
    }
}
