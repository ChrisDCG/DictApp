using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace OpenAIDictate.Resources;

/// <summary>
/// Strongly-typed accessors for UI resources.
/// </summary>
public static class SR
{
    private static readonly ResourceManager ResourceManager = new("OpenAIDictate.Resources.Strings", typeof(SR).Assembly);

    private static string Get([CallerMemberName] string? name = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }

    public static string AppDisplayName => Get();
    public static string TrayReadyText => Get();
    public static string TrayRecordingText => Get();
    public static string TrayTranscribingText => Get();
    public static string TrayUnknownState => Get();
    public static string TrayOfflineSuffix => Get();
    public static string ContextMenuSettings => Get();
    public static string ContextMenuExit => Get();
    public static string HotkeyRegistrationError => Get();
    public static string FatalErrorTitle => Get();
    public static string FatalErrorMessage => Get();
    public static string OfflineStartWarning => Get();
    public static string MaxDurationReachedWarning => Get();
    public static string OfflineAfterRecordingWarning => Get();
    public static string TranscriptionComplete => Get();
    public static string TranscriptionFailed => Get();
    public static string RecordingFailed => Get();
    public static string OfflineWarning => Get();
    public static string OnlineInfo => Get();
    public static string SettingsAppliedInfo => Get();
    public static string HotkeyUpdatedInfo => Get();
    public static string HotkeyUpdateFailed => Get();
    public static string SettingsOpenError => Get();
    public static string SettingsDialogTitle => Get();
    public static string SettingsTabGeneral => Get();
    public static string SettingsTabAdvanced => Get();
    public static string SettingsTabAbout => Get();
    public static string SaveButton => Get();
    public static string CancelButton => Get();
    public static string ModelLabel => Get();
    public static string ModelOptionGpt4o => Get();
    public static string ModelOptionGpt4oMini => Get();
    public static string ModelOptionGpt4oDiarize => Get();
    public static string ModelOptionWhisper => Get();
    public static string TranscriptionLanguageLabel => Get();
    public static string LanguagePlaceholder => Get();
    public static string HotkeyLabel => Get();
    public static string HotkeyHelp => Get();
    public static string MaxRecordingLabel => Get();
    public static string GlossaryLabel => Get();
    public static string GlossaryPlaceholder => Get();
    public static string PostProcessingLabel => Get();
    public static string ServerChunkingLabel => Get();
    public static string VadLabel => Get();
    public static string VadThresholdLabel => Get();
    public static string VadMinSilenceLabel => Get();
    public static string VadMinSpeechLabel => Get();
    public static string VadPadLabel => Get();
    public static string LogProbabilitiesLabel => Get();
    public static string DiarizedOutputLabel => Get();
    public static string UiLanguageLabel => Get();
    public static string UiLanguageHelp => Get();
    public static string BestPracticesGroup => Get();
    public static string BestPracticesText => Get();
    public static string AboutText => Get();
    public static string SettingsSavedStatus => Get();
    public static string SettingsSaveError => Get();
    public static string GenericError => Get();
    public static string HotkeyEmptyError => Get();
    public static string HotkeyInvalidError => Get();
    public static string ApiKeyRequired => Get();
    public static string ApiKeyPromptTitle => Get();
    public static string ApiKeyPromptMessage => Get();
    public static string DialogOk => Get();
    public static string DialogCancel => Get();
    public static string TrayTooltipPrefix => Get();
    public static string SettingsRestartNotice => Get();
}
