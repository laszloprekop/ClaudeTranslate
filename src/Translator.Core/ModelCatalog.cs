namespace Translator.Core;

public static class ModelCatalog
{
    public const string Default = "claude-opus-5";

    // Offered in the desktop Settings. Anything else still works if it is put in the settings
    // file by hand — this is a shortlist, not a whitelist.
    public static readonly IReadOnlyList<string> Known =
    [
        "claude-opus-5",
        "claude-opus-4-8",
        "claude-sonnet-5",
        "claude-haiku-4-5",
    ];
}
