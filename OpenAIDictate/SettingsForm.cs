using System;
using System.Globalization;
using System.Runtime.InteropServices;
using OpenAIDictate.Infrastructure;
using OpenAIDictate.Models;
using OpenAIDictate.Resources;
using OpenAIDictate.Services;

namespace OpenAIDictate;

/// <summary>
/// Settings dialog for configuring OpenAIDictate
/// </summary>
public class SettingsForm : Form
{
    private readonly AppConfig _config;
    private bool _configChanged = false;

    // Controls
    private TabControl? _tabControl;
    private TextBox? _txtGlossary;
    private ComboBox? _cmbLanguage;
    private ComboBox? _cmbModel;
    private ComboBox? _cmbHotkey;
    private ComboBox? _cmbUiLanguage;
    private NumericUpDown? _numMaxRecording;
    private CheckBox? _chkPostProcessing;
    private CheckBox? _chkVAD;
    private CheckBox? _chkServerChunking;
    private CheckBox? _chkLogProbabilities;
    private CheckBox? _chkDiarizedOutput;
    private NumericUpDown? _numVadThreshold;
    private NumericUpDown? _numVadMinSilence;
    private NumericUpDown? _numVadMinSpeech;
    private NumericUpDown? _numVadPadding;
    private Button? _btnSave;
    private Button? _btnCancel;
    private Label? _lblStatus;
    private ToolTip? _toolTip;
    private const int LabelColumnLeft = 20;
    private const int FieldColumnLeft = 200;
    private const int FieldColumnWidth = 360;
    private bool _cachedDiarizedOutputPreference;
    private bool _cachedLogProbabilitiesPreference;
    private bool _suppressDiarizedCheckedEvent;
    private bool _suppressLogProbabilitiesEvent;

    public SettingsForm(AppConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _cachedDiarizedOutputPreference = _config.RequestDiarizedOutput;
        _cachedLogProbabilitiesPreference = _config.IncludeLogProbabilities;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        UpdateStyles();
        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        Text = SR.SettingsDialogTitle;
        AutoScaleMode = AutoScaleMode.Dpi;
        Width = 720;
        Height = 640;
        MinimumSize = new Size(720, 640);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = true;
        MinimizeBox = false;
        BackColor = Color.FromArgb(240, 240, 240);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        Padding = new Padding(0);

        _toolTip = new ToolTip
        {
            AutomaticDelay = 200,
            AutoPopDelay = 7000,
            InitialDelay = 200,
            ReshowDelay = 100,
            UseFading = true,
            UseAnimation = true
        };

        // Tab Control
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Padding = new Point(16, 8)
        };

        // Tab 1: General Settings
        var tabGeneral = new TabPage(SR.SettingsTabGeneral);
        CreateGeneralTab(tabGeneral);
        _tabControl.TabPages.Add(tabGeneral);

        // Tab 2: Advanced Settings
        var tabAdvanced = new TabPage(SR.SettingsTabAdvanced);
        CreateAdvancedTab(tabAdvanced);
        _tabControl.TabPages.Add(tabAdvanced);

        // Tab 3: About
        var tabAbout = new TabPage(SR.SettingsTabAbout);
        CreateAboutTab(tabAbout);
        _tabControl.TabPages.Add(tabAbout);

        Controls.Add(_tabControl);

        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            Padding = new Padding(20, 10, 20, 20),
            BackColor = Color.FromArgb(244, 244, 244)
        };
        Controls.Add(bottomPanel);

        _lblStatus = new Label
        {
            Text = string.Empty,
            Dock = DockStyle.Left,
            Width = 420,
            Height = 30,
            ForeColor = Color.FromArgb(0, 120, 215),
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        bottomPanel.Controls.Add(_lblStatus);

        var buttonFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        bottomPanel.Controls.Add(buttonFlow);

        _btnSave = new Button
        {
            Text = SR.SaveButton,
            Width = 110,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular),
            Margin = new Padding(0, 0, 10, 0)
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += BtnSave_Click;
        buttonFlow.Controls.Add(_btnSave);

        _btnCancel = new Button
        {
            Text = SR.CancelButton,
            Width = 110,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(225, 225, 225),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0)
        };
        _btnCancel.FlatAppearance.BorderSize = 0;
        buttonFlow.Controls.Add(_btnCancel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;
    }

    private void CreateGeneralTab(TabPage tab)
    {
        tab.BackColor = Color.White;
        int yPos = 20;

        // Model Selection
        var lblModel = new Label
        {
            Text = SR.ModelLabel,
            Left = 20,
            Top = yPos,
            Width = 150,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblModel);

        _cmbModel = new ComboBox
        {
            Left = 180,
            Top = yPos,
            Width = 350,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        _cmbModel.Items.AddRange(new object[]
        {
            SR.ModelOptionGpt4o,
            SR.ModelOptionGpt4oMini,
            SR.ModelOptionGpt4oDiarize,
            SR.ModelOptionWhisper
        });
        _cmbModel.SelectedIndexChanged += CmbModel_SelectedIndexChanged;
        tab.Controls.Add(_cmbModel);
        yPos += 40;

        // Language
        var lblLanguage = new Label
        {
            Text = SR.TranscriptionLanguageLabel,
            Left = 20,
            Top = yPos,
            Width = 150,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblLanguage);

        _cmbLanguage = new ComboBox
        {
            Left = 180,
            Top = yPos - 3,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        _cmbLanguage.Items.AddRange(new object[]
        {
            "Auto",
            "Deutsch (de)",
            "English (en)",
            "Français (fr)",
            "Español (es)",
            "Italiano (it)"
        });
        tab.Controls.Add(_cmbLanguage);
        yPos += 40;

        // UI Language
        var lblUiLanguage = new Label
        {
            Text = SR.UiLanguageLabel,
            Left = 20,
            Top = yPos,
            Width = 150,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblUiLanguage);

        _cmbUiLanguage = new ComboBox
        {
            Left = 180,
            Top = yPos - 3,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };

        foreach (var culture in LocalizationService.SupportedCultures)
        {
            _cmbUiLanguage.Items.Add(new CultureDisplayItem(culture));
        }

        tab.Controls.Add(_cmbUiLanguage);

        var lblUiLanguageHelp = new Label
        {
            Text = SR.UiLanguageHelp,
            Left = 180,
            Top = yPos + 25,
            Width = 350,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular)
        };
        tab.Controls.Add(lblUiLanguageHelp);
        yPos += 60;

        // Hotkey
        var lblHotkey = new Label
        {
            Text = SR.HotkeyLabel,
            Left = 20,
            Top = yPos,
            Width = 150,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblHotkey);

        _cmbHotkey = new ComboBox
        {
            Left = 180,
            Top = yPos - 3,
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        _cmbHotkey.Items.AddRange(HotkeyParser.GetSuggestions().ToArray());
        tab.Controls.Add(_cmbHotkey);

        var lblHotkeyHelp = new Label
        {
            Text = SR.HotkeyHelp,
            Left = 180,
            Top = yPos + 25,
            Width = 350,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular)
        };
        tab.Controls.Add(lblHotkeyHelp);
        yPos += 60;

        // Max Recording Duration
        var lblMaxRecording = new Label
        {
            Text = SR.MaxRecordingLabel,
            Left = 20,
            Top = yPos,
            Width = 150,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblMaxRecording);

        _numMaxRecording = new NumericUpDown
        {
            Left = 180,
            Top = yPos,
            Width = 100,
            Minimum = 1,
            Maximum = 30,
            Value = 10,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_numMaxRecording);
        yPos += 40;

        // Glossary
        var lblGlossary = new Label
        {
            Text = SR.GlossaryLabel,
            Left = 20,
            Top = yPos,
            Width = 640,
            Height = 40,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblGlossary);
        yPos += 45;

        _txtGlossary = new TextBox
        {
            Left = 20,
            Top = yPos,
            Width = 640,
            Height = 100,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        SetPlaceholderText(_txtGlossary, SR.GlossaryPlaceholder);
        tab.Controls.Add(_txtGlossary);
    }

    private void CreateAdvancedTab(TabPage tab)
    {
        tab.BackColor = Color.White;
        int yPos = 20;

        // Post-Processing
        _chkPostProcessing = new CheckBox
        {
            Text = SR.PostProcessingLabel,
            Left = 20,
            Top = yPos,
            Width = 640,
            Checked = true,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_chkPostProcessing);
        yPos += 40;

        // Server Auto Chunking
        _chkServerChunking = new CheckBox
        {
            Text = SR.ServerChunkingLabel,
            Left = 20,
            Top = yPos,
            Width = 640,
            Checked = true,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_chkServerChunking);
        yPos += 40;

        // VAD
        _chkVAD = new CheckBox
        {
            Text = SR.VadLabel,
            Left = 20,
            Top = yPos,
            Width = 640,
            Checked = true,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_chkVAD);
        yPos += 40;

        // Log probabilities
        _chkLogProbabilities = new CheckBox
        {
            Text = SR.LogProbabilitiesLabel,
            Left = 20,
            Top = yPos,
            Width = 640,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        _chkLogProbabilities.CheckedChanged += LogProbabilities_CheckedChanged;
        tab.Controls.Add(_chkLogProbabilities);
        yPos += 40;

        // Diarized output
        _chkDiarizedOutput = new CheckBox
        {
            Text = SR.DiarizedOutputLabel,
            Left = 20,
            Top = yPos,
            Width = 640,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        _chkDiarizedOutput.CheckedChanged += DiarizedOutput_CheckedChanged;
        tab.Controls.Add(_chkDiarizedOutput);
        yPos += 40;

        // VAD Threshold
        var lblVadThreshold = new Label
        {
            Text = SR.VadThresholdLabel,
            Left = 20,
            Top = yPos,
            Width = 260,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblVadThreshold);

        _numVadThreshold = new NumericUpDown
        {
            Left = 300,
            Top = yPos,
            Width = 80,
            Minimum = 0.10M,
            Maximum = 0.95M,
            DecimalPlaces = 2,
            Increment = 0.05M,
            Value = 0.50M,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_numVadThreshold);
        yPos += 40;

        // Min silence
        var lblVadMinSilence = new Label
        {
            Text = SR.VadMinSilenceLabel,
            Left = 20,
            Top = yPos,
            Width = 260,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblVadMinSilence);

        _numVadMinSilence = new NumericUpDown
        {
            Left = 300,
            Top = yPos,
            Width = 80,
            Minimum = 50,
            Maximum = 2000,
            Increment = 10,
            Value = 120,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_numVadMinSilence);
        yPos += 40;

        // Min speech duration
        var lblVadMinSpeech = new Label
        {
            Text = SR.VadMinSpeechLabel,
            Left = 20,
            Top = yPos,
            Width = 260,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblVadMinSpeech);

        _numVadMinSpeech = new NumericUpDown
        {
            Left = 300,
            Top = yPos,
            Width = 80,
            Minimum = 50,
            Maximum = 4000,
            Increment = 10,
            Value = 250,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_numVadMinSpeech);
        yPos += 40;

        // Padding per segment
        var lblVadPad = new Label
        {
            Text = SR.VadPadLabel,
            Left = 20,
            Top = yPos,
            Width = 260,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(lblVadPad);

        _numVadPadding = new NumericUpDown
        {
            Left = 300,
            Top = yPos,
            Width = 80,
            Minimum = 0,
            Maximum = 1000,
            Increment = 10,
            Value = 60,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(_numVadPadding);
        yPos += 60;

        // Info Box
        var infoBox = new GroupBox
        {
            Text = SR.BestPracticesGroup,
            Left = 20,
            Top = yPos,
            Width = 640,
            Height = 120,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat
        };

        var infoText = new Label
        {
            Text = SR.BestPracticesText,
            Left = 10,
            Top = 25,
            Width = 620,
            Height = 90,
            ForeColor = Color.FromArgb(0, 100, 0),
            Font = new Font("Segoe UI", 8.75F, FontStyle.Regular)
        };
        infoBox.Controls.Add(infoText);
        tab.Controls.Add(infoBox);
    }

    private void CreateAboutTab(TabPage tab)
    {
        tab.BackColor = Color.White;

        var aboutText = new Label
        {
            Text = SR.AboutText,
            Left = 20,
            Top = 20,
            Width = 640,
            Height = 400,
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular)
        };
        tab.Controls.Add(aboutText);
    }

    private void CmbModel_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateAdvancedOptionStates();
    }

    private void UpdateAdvancedOptionStates()
    {
        if (_cmbModel == null)
        {
            return;
        }

        bool diarizationSelected = _cmbModel.SelectedIndex == 2; // gpt-4o-transcribe-diarize
        bool logProbSupported = _cmbModel.SelectedIndex is 0 or 1; // gpt-4o or gpt-4o-mini

        if (_chkLogProbabilities != null)
        {
            _suppressLogProbabilitiesEvent = true;
            try
            {
                if (!logProbSupported)
                {
                    _cachedLogProbabilitiesPreference = _chkLogProbabilities.Checked;
                    _chkLogProbabilities.Checked = false;
                }
                else
                {
                    _chkLogProbabilities.Checked = _cachedLogProbabilitiesPreference;
                }

                _chkLogProbabilities.Enabled = logProbSupported;
            }
            finally
            {
                _suppressLogProbabilitiesEvent = false;
            }
        }

        if (_chkDiarizedOutput == null)
        {
            return;
        }

        _suppressDiarizedCheckedEvent = true;
        try
        {
            if (!diarizationSelected)
            {
                _cachedDiarizedOutputPreference = _chkDiarizedOutput.Checked;
                _chkDiarizedOutput.Checked = false;
            }
            else
            {
                _chkDiarizedOutput.Checked = _cachedDiarizedOutputPreference;
            }

            _chkDiarizedOutput.Enabled = diarizationSelected;
        }
        finally
        {
            _suppressDiarizedCheckedEvent = false;
        }
    }

    private void DiarizedOutput_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressDiarizedCheckedEvent)
        {
            return;
        }

        if (_chkDiarizedOutput?.Enabled == true)
        {
            _cachedDiarizedOutputPreference = _chkDiarizedOutput.Checked;
        }
    }

    private void LogProbabilities_CheckedChanged(object? sender, EventArgs e)
    {
        if (_suppressLogProbabilitiesEvent)
        {
            return;
        }

        if (_chkLogProbabilities?.Enabled == true)
        {
            _cachedLogProbabilitiesPreference = _chkLogProbabilities.Checked;
        }
    }

    private void LoadSettings()
    {
        // Model
        if (_config.Model.Contains("gpt-4o-transcribe-diarize", StringComparison.OrdinalIgnoreCase))
        {
            _cmbModel!.SelectedIndex = 2;
        }
        else if (_config.Model.Contains("gpt-4o-transcribe", StringComparison.OrdinalIgnoreCase) && !_config.Model.Contains("mini", StringComparison.OrdinalIgnoreCase))
        {
            _cmbModel!.SelectedIndex = 0;
        }
        else if (_config.Model.Contains("gpt-4o-mini-transcribe"))
        {
            _cmbModel!.SelectedIndex = 1;
        }
        else
        {
            _cmbModel!.SelectedIndex = 3;
        }

        // Language
        if (_cmbLanguage != null)
        {
            string lang = _config.Language?.ToLowerInvariant() ?? "";
            if (string.IsNullOrWhiteSpace(lang))
            {
                _cmbLanguage.SelectedIndex = 0; // Auto
            }
            else if (lang == "de")
            {
                _cmbLanguage.SelectedIndex = 1; // Deutsch
            }
            else if (lang == "en")
            {
                _cmbLanguage.SelectedIndex = 2; // English
            }
            else if (lang == "fr")
            {
                _cmbLanguage.SelectedIndex = 3; // Français
            }
            else if (lang == "es")
            {
                _cmbLanguage.SelectedIndex = 4; // Español
            }
            else if (lang == "it")
            {
                _cmbLanguage.SelectedIndex = 5; // Italiano
            }
            else
            {
                _cmbLanguage.SelectedIndex = 0; // Auto (default)
            }
        }

        // UI Language
        if (_cmbUiLanguage != null)
        {
            var resolved = LocalizationService.ResolveCulture(_config.UiCulture);
            for (int i = 0; i < _cmbUiLanguage.Items.Count; i++)
            {
                if (_cmbUiLanguage.Items[i] is CultureDisplayItem item &&
                    item.Culture.Name.Equals(resolved.Name, StringComparison.OrdinalIgnoreCase))
                {
                    _cmbUiLanguage.SelectedIndex = i;
                    break;
                }
            }
        }

        // Hotkey
        _cmbHotkey!.Text = _config.HotkeyGesture ?? "F5";

        // Max Recording
        _numMaxRecording!.Value = _config.MaxRecordingMinutes;

        // Glossary
        string glossary = _config.Glossary ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(glossary) && !string.Equals(glossary, SR.GlossaryPlaceholder, StringComparison.Ordinal))
        {
            _txtGlossary!.Text = glossary;
            _txtGlossary.ForeColor = Color.Black;
        }

        // Post-Processing
        _chkPostProcessing!.Checked = _config.EnablePostProcessing;

        _chkServerChunking!.Checked = _config.EnableServerAutoChunking;
        _chkLogProbabilities!.Checked = _config.IncludeLogProbabilities;
        _chkDiarizedOutput!.Checked = _config.RequestDiarizedOutput;

        _cachedLogProbabilitiesPreference = _config.IncludeLogProbabilities;
        _cachedDiarizedOutputPreference = _config.RequestDiarizedOutput;

        // VAD
        _chkVAD!.Checked = _config.EnableVAD;

        _numVadThreshold!.Value = (decimal)_config.VadSpeechThreshold;
        _numVadMinSilence!.Value = Math.Clamp(_config.VadMinSilenceDurationMs, (int)_numVadMinSilence.Minimum, (int)_numVadMinSilence.Maximum);
        _numVadMinSpeech!.Value = Math.Clamp(_config.VadMinSpeechDurationMs, (int)_numVadMinSpeech.Minimum, (int)_numVadMinSpeech.Maximum);
        _numVadPadding!.Value = Math.Clamp(_config.VadSpeechPaddingMs, (int)_numVadPadding.Minimum, (int)_numVadPadding.Maximum);

        UpdateAdvancedOptionStates();
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // Validate and save settings
            if (_cmbModel!.SelectedIndex == 0)
            {
                _config.Model = "gpt-4o-transcribe";
            }
            else if (_cmbModel.SelectedIndex == 1)
            {
                _config.Model = "gpt-4o-mini-transcribe";
            }
            else if (_cmbModel.SelectedIndex == 2)
            {
                _config.Model = "gpt-4o-transcribe-diarize";
            }
            else
            {
                _config.Model = "whisper-1";
            }

            // Language from dropdown
            if (_cmbLanguage!.SelectedIndex == 0)
            {
                _config.Language = null; // Auto
            }
            else if (_cmbLanguage.SelectedIndex == 1)
            {
                _config.Language = "de"; // Deutsch
            }
            else if (_cmbLanguage.SelectedIndex == 2)
            {
                _config.Language = "en"; // English
            }
            else if (_cmbLanguage.SelectedIndex == 3)
            {
                _config.Language = "fr"; // Français
            }
            else if (_cmbLanguage.SelectedIndex == 4)
            {
                _config.Language = "es"; // Español
            }
            else if (_cmbLanguage.SelectedIndex == 5)
            {
                _config.Language = "it"; // Italiano
            }
            else
            {
                _config.Language = null; // Default to Auto
            }

            string hotkeyInput = _cmbHotkey!.Text.Trim();
            if (string.IsNullOrWhiteSpace(hotkeyInput))
            {
                throw new InvalidOperationException(SR.HotkeyEmptyError);
            }

            if (!HotkeyParser.IsValid(hotkeyInput))
            {
                throw new InvalidOperationException(SR.HotkeyInvalidError);
            }

            var (modifiers, virtualKey) = HotkeyParser.Parse(hotkeyInput);
            _config.HotkeyGesture = HotkeyParser.Format(modifiers, virtualKey);
            _config.HotkeyModifiers = (int)modifiers;
            _config.HotkeyVirtualKey = (int)virtualKey;

            _config.MaxRecordingMinutes = (int)_numMaxRecording!.Value;
            string glossaryText = _txtGlossary!.Text.Trim();
            if (string.Equals(glossaryText, SR.GlossaryPlaceholder, StringComparison.Ordinal))
            {
                glossaryText = string.Empty;
            }
            _config.Glossary = string.IsNullOrWhiteSpace(glossaryText) ? null : glossaryText;
            _config.EnablePostProcessing = _chkPostProcessing!.Checked;
            _config.EnableServerAutoChunking = _chkServerChunking!.Checked;

            bool logProbSupported = _cmbModel.SelectedIndex is 0 or 1;
            _config.IncludeLogProbabilities = logProbSupported && _chkLogProbabilities!.Checked;

            bool diarizationSelected = _cmbModel.SelectedIndex == 2;
            _config.RequestDiarizedOutput = diarizationSelected && _chkDiarizedOutput!.Checked;

            _config.EnableVAD = _chkVAD!.Checked;
            _config.VadSpeechThreshold = (double)_numVadThreshold!.Value;
            _config.VadMinSilenceDurationMs = (int)_numVadMinSilence!.Value;
            _config.VadMinSpeechDurationMs = (int)_numVadMinSpeech!.Value;
            _config.VadSpeechPaddingMs = (int)_numVadPadding!.Value;

            if (_cmbUiLanguage?.SelectedItem is CultureDisplayItem cultureItem)
            {
                _config.UiCulture = cultureItem.Culture.Name;
                LocalizationService.ApplyCulture(_config.UiCulture);
            }

            // Save to disk
            ConfigService.Save(_config);

            _lblStatus!.Text = string.Format(CultureInfo.CurrentCulture, "{0} {1}", SR.SettingsSavedStatus, SR.SettingsRestartNotice);
            _lblStatus.ForeColor = Color.Green;

            _configChanged = true;

            Logger.LogInfo("Settings saved successfully");

            // Auto-close after 1 second
            var timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                DialogResult = DialogResult.OK;
                Close();
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            _lblStatus!.Text = string.Format(CultureInfo.CurrentCulture, SR.SettingsSaveError, ex.Message);
            _lblStatus.ForeColor = Color.Red;
            Logger.LogError($"Failed to save settings: {ex.Message}");
        }
    }

    public bool ConfigChanged => _configChanged;

    /// <summary>
    /// Sets placeholder text for a TextBox using Windows API (Windows 10+) or fallback method
    /// </summary>
    private static void SetPlaceholderText(TextBox textBox, string placeholder)
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            try
            {
                // Use Windows API SendMessage for placeholder text (Windows 10+)
                const int EM_SETCUEBANNER = 0x1501;
                var handle = textBox.Handle;
                if (handle != IntPtr.Zero)
                {
                    SendMessage(handle, EM_SETCUEBANNER, 0, placeholder);
                    return;
                }
            }
            catch
            {
                // Fall through to fallback
            }
        }

        // Fallback: set text as hint (will be cleared on focus)
        textBox.Text = placeholder;
        textBox.ForeColor = Color.Gray;
        textBox.Enter += (s, e) =>
        {
            if (textBox.Text == placeholder)
            {
                textBox.Text = "";
                textBox.ForeColor = Color.Black;
            }
        };
        textBox.Leave += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.ForeColor = Color.Gray;
            }
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

    private sealed class CultureDisplayItem
    {
        public CultureDisplayItem(CultureInfo culture)
        {
            Culture = culture;
        }

        public CultureInfo Culture { get; }

        public override string ToString() => LocalizationService.GetDisplayName(Culture);
    }
}
