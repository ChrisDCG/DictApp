namespace OpenAIDictate.Tests;

/// <summary>
/// Test categories for organizing and filtering tests
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Fast unit tests that don't require external dependencies
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Integration tests that require external services (API, network, hardware)
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// Tests that require Windows Forms or UI components
    /// </summary>
    public const string UI = "UI";

    /// <summary>
    /// Tests that require audio hardware
    /// </summary>
    public const string Audio = "Audio";

    /// <summary>
    /// Tests that require OpenAI API key
    /// </summary>
    public const string RequiresApiKey = "RequiresApiKey";

    /// <summary>
    /// Tests that require network connectivity
    /// </summary>
    public const string RequiresNetwork = "RequiresNetwork";
}

