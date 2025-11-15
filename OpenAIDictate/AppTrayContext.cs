using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAIDictate.Models;
using OpenAIDictate.Resources;
using OpenAIDictate.Services;

namespace OpenAIDictate;

/// <summary>
/// Main application context running in system tray
/// Implements state machine: Idle → Recording → Transcribing → Idle
/// </summary>
public class AppTrayContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _settingsMenuItem;
    private readonly ToolStripMenuItem _exitMenuItem;
    private readonly GlobalHotkeyService _hotkeyService;
    private AudioRecorder? _audioRecorder;
    private readonly NetworkStatusService _networkStatusService;
    private readonly SemaphoreSlim _connectivitySemaphore = new(1, 1);
    private readonly SemaphoreSlim _stateSemaphore = new(1, 1);
    private readonly AppConfig _config;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IAppLogger _logger;
    private readonly IMetricsService _metrics;
    private readonly IServiceProvider? _serviceProvider;

    private AppState _currentState = AppState.Idle;
    private TranscriptionService? _transcriptionService;
    private HiddenWindow? _messageWindow;
    private System.Windows.Forms.Timer? _recordingTimer;
    private System.Windows.Forms.Timer? _networkTimer;
    private bool _isOffline;

    /// <summary>
    /// Constructor with Dependency Injection support
    /// </summary>
    public AppTrayContext(
        IOptions<AppConfig>? configOptions = null,
        IAppLogger? logger = null,
        IMetricsService? metrics = null,
        NetworkStatusService? networkStatusService = null,
        IServiceProvider? serviceProvider = null)
    {
        // Use DI if available, otherwise fallback to static loading
        _config = configOptions?.Value ?? ConfigService.Load();
        _logger = logger ?? new SerilogLogger();
        _metrics = metrics ?? new MetricsService(_logger);
        _serviceProvider = serviceProvider;

        var networkService = networkStatusService ?? new NetworkStatusService();
        _networkStatusService = networkService;

        // Create hidden window for hotkey messages
        _messageWindow = new HiddenWindow(this);

        // Initialize services
        _hotkeyService = new GlobalHotkeyService(_messageWindow.Handle);
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        // AudioRecorder will be created when needed (transient service)
        // Create initial instance for compatibility
        _audioRecorder = _serviceProvider?.GetService<AudioRecorder>() ?? new AudioRecorder(_config);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application, // TODO: Use custom icon
            Visible = true
        };

        // Create context menu
        _contextMenu = new ContextMenuStrip();
        _settingsMenuItem = new ToolStripMenuItem(SR.ContextMenuSettings, null, OnSettings);
        _exitMenuItem = new ToolStripMenuItem(SR.ContextMenuExit, null, OnExit);
        _contextMenu.Items.Add(_settingsMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(_exitMenuItem);

        _trayIcon.ContextMenuStrip = _contextMenu;
        UpdateTrayTextUnsafe();

        // Register hotkey
        try
        {
            var gesture = string.IsNullOrWhiteSpace(_config.HotkeyGesture) ? "F5" : _config.HotkeyGesture;
            _hotkeyService.Register(gesture);
            _logger.LogInfo("Application started successfully");
        }
        catch (Exception ex)
        {
            ShowError(string.Format(CultureInfo.CurrentCulture, SR.HotkeyRegistrationError, ex.Message));
            Application.Exit();
            return;
        }

        // Initialize recording duration timer (updates tray tooltip every second)
        _recordingTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000 // 1 second
        };
        _recordingTimer.Tick += RecordingTimer_Tick;

        _networkTimer = new System.Windows.Forms.Timer
        {
            Interval = 30000 // Check connectivity every 30 seconds
        };
        _networkTimer.Tick += NetworkTimer_Tick;
        _networkTimer.Start();

        // Initial connectivity check (silent)
        _ = CheckConnectivityAsync(notify: false);

        // Check/prompt for API key
        InitializeApiKey();
    }

    /// <summary>
    /// Initializes or prompts for OpenAI API key
    /// </summary>
    private void InitializeApiKey()
    {
        string? apiKey = ConfigService.GetApiKey(_config);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // First-time setup: prompt for API key
            using var inputForm = new ApiKeyInputForm();
            if (inputForm.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(inputForm.ApiKey))
            {
                ConfigService.SetApiKey(_config, inputForm.ApiKey);
                apiKey = inputForm.ApiKey;
                _logger.LogInfo("API key configured successfully");
            }
            else
            {
                ShowError(SR.ApiKeyRequired);
                Application.Exit();
                return;
            }
        }

        // Initialize transcription service (via DI if available)
        _transcriptionService = _serviceProvider?.GetService<TranscriptionService>()
            ?? new TranscriptionService(_config, apiKey, _logger, _metrics, new AudioPreprocessor(_config));
    }

    /// <summary>
    /// Handles F5 hotkey press - state machine controller (thread-safe, async-safe)
    /// </summary>
    private async void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // Prevent concurrent hotkey presses (non-blocking check)
        if (!_stateSemaphore.Wait(0))
        {
            _logger.LogInfo("Hotkey ignored (previous action still in progress)");
            return;
        }

        try
        {
            AppState currentState = _currentState;
            _stateSemaphore.Release();

            await HandleHotkeyAsync(currentState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in hotkey handler: {Message}", ex.Message);
            ShowError(string.Format(CultureInfo.CurrentCulture, SR.GenericError, ex.Message));
            await SetStateAsync(AppState.Idle);
        }
    }

    /// <summary>
    /// Handles hotkey state transitions (extracted for better error handling)
    /// </summary>
    private async Task HandleHotkeyAsync(AppState currentState)
    {
        switch (currentState)
        {
            case AppState.Idle:
                if (await CheckConnectivityAsync(notify: false))
                {
                    await StartRecordingAsync();
                }
                else
                {
                    ShowWarning(SR.OfflineStartWarning);
                }
                break;

            case AppState.Recording:
                await StopRecordingAndTranscribeAsync();
                break;

            case AppState.Transcribing:
                // Ignore F5 while transcribing
                _logger.LogInfo("Hotkey ignored (currently transcribing)");
                break;
        }
    }

    /// <summary>
    /// State: Idle → Recording
    /// </summary>
    private Task StartRecordingAsync()
    {
        try
        {
            SetState(AppState.Recording);

            // Create AudioRecorder if needed (transient service)
            if (_audioRecorder == null)
            {
                _audioRecorder = _serviceProvider?.GetService<AudioRecorder>() ?? new AudioRecorder(_config);
            }

            _audioRecorder.StartRecording();

            // Start real-time duration display timer
            _recordingTimer?.Start();

            // Monitor for max duration
            _ = Task.Run(async () =>
            {
                try
                {
                    while (_currentState == AppState.Recording && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        if (_audioRecorder?.HasExceededMaxDuration() == true)
                        {
                            ShowWarning(string.Format(CultureInfo.CurrentCulture, SR.MaxDurationReachedWarning, _config.MaxRecordingMinutes));
                            await StopRecordingAndTranscribeAsync();
                            break;
                        }
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in max duration monitor: {Message}", ex.Message);
                }
            }, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start recording: {Message}", ex.Message);
            ShowError(string.Format(CultureInfo.CurrentCulture, SR.RecordingFailed, ex.Message));
            SetState(AppState.Idle);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// State: Recording → Transcribing → Idle
    /// </summary>
    private async Task StopRecordingAndTranscribeAsync()
    {
        if (_currentState != AppState.Recording || _audioRecorder == null)
            return;

        try
        {
            // Stop recording timer
            _recordingTimer?.Stop();

            SetState(AppState.Transcribing);

            // Stop recording and get audio
            using MemoryStream audioStream = await _audioRecorder.StopRecordingAsync();
            _metrics.RecordAudioDuration(_audioRecorder.GetRecordingDuration());

            await CheckConnectivityAsync(notify: false);
            if (_isOffline)
            {
                ShowWarning(SR.OfflineAfterRecordingWarning);
                return;
            }

            // Transcribe
            if (_transcriptionService == null)
            {
                throw new InvalidOperationException("Transcription service not initialized");
            }

            string transcription = await _transcriptionService.TranscribeAsync(audioStream);

            // Inject text at cursor
            await TextInjector.InjectAsync(transcription);

            ShowInfo(string.Format(CultureInfo.CurrentCulture, SR.TranscriptionComplete, transcription.Length));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed: {Message}", ex.Message);
            _metrics.RecordError("transcription_failed");
            ShowError(string.Format(CultureInfo.CurrentCulture, SR.TranscriptionFailed, ex.Message));
        }
        finally
        {
            SetState(AppState.Idle);
        }
    }

    /// <summary>
    /// Timer tick event for updating recording duration in tray (optimized, non-blocking)
    /// </summary>
    private void RecordingTimer_Tick(object? sender, EventArgs e)
    {
        // Non-blocking check - if semaphore is busy, skip this update (next tick will catch up)
        if (!_stateSemaphore.Wait(0))
            return;

        try
        {
            if (_currentState == AppState.Recording && _audioRecorder != null)
            {
                TimeSpan duration = _audioRecorder.GetRecordingDuration();
                int totalSeconds = (int)duration.TotalSeconds;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;

                string gesture = _hotkeyService.GetCurrentGesture();

                // Use string interpolation with format provider for better performance
                string stateText = string.Format(CultureInfo.CurrentCulture, SR.TrayRecordingText, minutes, seconds, gesture);

                if (_isOffline)
                {
                    stateText += SR.TrayOfflineSuffix;
                }

                _trayIcon.Text = SR.TrayTooltipPrefix + stateText;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating recording timer: {Message}", ex.Message);
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    private async void NetworkTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            await CheckConnectivityAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Network timer error: {Message}", ex.Message);
            // Don't show error to user - connectivity check failures are expected
        }
    }

    /// <summary>
    /// Updates application state and UI (thread-safe, async version)
    /// </summary>
    private async Task SetStateAsync(AppState newState)
    {
        await _stateSemaphore.WaitAsync();
        try
        {
            _currentState = newState;
            UpdateTrayTextUnsafe(); // Already inside lock
            _logger.LogInfo("State changed: {State}", newState);
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates application state and UI (thread-safe, synchronous version for compatibility)
    /// </summary>
    private void SetState(AppState newState)
    {
        _stateSemaphore.Wait();
        try
        {
            _currentState = newState;
            UpdateTrayTextUnsafe(); // Already inside lock
            _logger.LogInfo("State changed: {State}", newState);
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    /// <summary>
    /// Updates tray text (must be called within semaphore lock)
    /// </summary>
    private void UpdateTrayTextUnsafe()
    {
        string gesture = _hotkeyService?.GetCurrentGesture() ?? _config.HotkeyGesture ?? "F5";

        string stateText = _currentState switch
        {
            AppState.Idle => string.Format(CultureInfo.CurrentCulture, SR.TrayReadyText, gesture),
            AppState.Recording => string.Format(CultureInfo.CurrentCulture, SR.TrayRecordingText, 0, 0, gesture),
            AppState.Transcribing => SR.TrayTranscribingText,
            _ => SR.TrayUnknownState
        };

        if (_isOffline)
        {
            stateText += SR.TrayOfflineSuffix;
        }

        _trayIcon.Text = SR.TrayTooltipPrefix + stateText;
    }

    private void ApplyLocalization()
    {
        _settingsMenuItem.Text = SR.ContextMenuSettings;
        _exitMenuItem.Text = SR.ContextMenuExit;
        UpdateTrayText();
    }

    /// <summary>
    /// Updates tray text (thread-safe wrapper)
    /// </summary>
    private void UpdateTrayText()
    {
        if (!_stateSemaphore.Wait(0))
            return; // Skip if busy

        try
        {
            UpdateTrayTextUnsafe();
        }
        finally
        {
            _stateSemaphore.Release();
        }
    }

    private async Task<bool> CheckConnectivityAsync(bool notify = true)
    {
        await _connectivitySemaphore.WaitAsync();
        try
        {
            bool isOnline = await _networkStatusService.CheckOnlineAsync();
            bool newOfflineState = !isOnline;

            if (newOfflineState != _isOffline)
            {
                _isOffline = newOfflineState;

                if (notify)
                {
                    if (_isOffline)
                    {
                        ShowWarning(SR.OfflineWarning);
                    }
                    else
                    {
                        ShowInfo(SR.OnlineInfo);
                    }
                }

                UpdateTrayText();
            }

            return !_isOffline;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Connectivity check failed: {Message}", ex.Message);
            return !_isOffline;
        }
        finally
        {
            _connectivitySemaphore.Release();
        }
    }

    /// <summary>
    /// Shows tray notification (info)
    /// </summary>
    private void ShowInfo(string message)
    {
        _trayIcon.ShowBalloonTip(3000, SR.AppDisplayName, message, ToolTipIcon.Info);
    }

    /// <summary>
    /// Shows tray notification (warning)
    /// </summary>
    private void ShowWarning(string message)
    {
        _trayIcon.ShowBalloonTip(5000, SR.AppDisplayName, message, ToolTipIcon.Warning);
    }

    /// <summary>
    /// Shows tray notification (error)
    /// </summary>
    private void ShowError(string message)
    {
        _trayIcon.ShowBalloonTip(5000, SR.FatalErrorTitle, message, ToolTipIcon.Error);
    }

    private void OnSettings(object? sender, EventArgs e)
    {
        try
        {
            string previousHotkey = _hotkeyService.GetCurrentGesture();
            using var settingsForm = new SettingsForm(_config);
            var result = settingsForm.ShowDialog();

            if (result == DialogResult.OK && settingsForm.ConfigChanged)
            {
                // Settings were saved, reinitialize transcription service with new config
                string? apiKey = ConfigService.GetApiKey(_config);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    // Create TranscriptionService via DI if available
                    _transcriptionService = _serviceProvider?.GetService<TranscriptionService>()
                        ?? new TranscriptionService(_config, apiKey, _logger, _metrics, new AudioPreprocessor(_config));
                    ShowInfo(SR.SettingsAppliedInfo);
                    _logger.LogInfo("Configuration updated, services reinitialized");
                }

                ApplyLocalization();

                if (!string.IsNullOrWhiteSpace(_config.HotkeyGesture) &&
                    !string.Equals(previousHotkey, _config.HotkeyGesture, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _hotkeyService.ChangeHotkey(_config.HotkeyGesture);
                        UpdateTrayText();
                        ShowInfo(string.Format(CultureInfo.CurrentCulture, SR.HotkeyUpdatedInfo, _config.HotkeyGesture));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update hotkey: {Message}", ex.Message);
                        ShowError(string.Format(CultureInfo.CurrentCulture, SR.HotkeyUpdateFailed, ex.Message));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings dialog: {Message}", ex.Message);
            ShowError(string.Format(CultureInfo.CurrentCulture, SR.SettingsOpenError, ex.Message));
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _logger.LogInfo("Application exiting");

        // Cancel all background tasks
        _cancellationTokenSource.Cancel();

        _recordingTimer?.Stop();
        _recordingTimer?.Dispose();
        _networkTimer?.Stop();
        _networkTimer?.Dispose();
        _hotkeyService?.Dispose();
        _audioRecorder?.Dispose();
        _networkStatusService?.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _messageWindow?.Dispose();
        _cancellationTokenSource.Dispose();
        _stateSemaphore.Dispose();

        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _recordingTimer?.Stop();
            _recordingTimer?.Dispose();
            _networkTimer?.Stop();
            _networkTimer?.Dispose();
            _hotkeyService?.Dispose();
            _audioRecorder?.Dispose();
            _networkStatusService?.Dispose();
            _trayIcon?.Dispose();
            _contextMenu?.Dispose();
            _messageWindow?.Dispose();
            _connectivitySemaphore.Dispose();
            _stateSemaphore.Dispose();
            _cancellationTokenSource.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Hidden window for receiving hotkey messages
    /// </summary>
    private class HiddenWindow : Form
    {
        private readonly AppTrayContext _context;

        public HiddenWindow(AppTrayContext context)
        {
            _context = context;

            // Make window invisible
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
        }

        protected override void WndProc(ref Message m)
        {
            // Forward hotkey messages to service
            _context._hotkeyService.ProcessMessage(m.Msg, m.WParam);

            base.WndProc(ref m);
        }
    }
}

/// <summary>
/// Simple form for API key input
/// </summary>
public class ApiKeyInputForm : Form
{
    private readonly TextBox _textBox;
    public string ApiKey => _textBox.Text;

    public ApiKeyInputForm()
    {
        Text = SR.ApiKeyPromptTitle;
        Width = 500;
        Height = 180;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;

        var label = new Label
        {
            Text = SR.ApiKeyPromptMessage,
            Left = 20,
            Top = 20,
            Width = 450,
            Height = 40
        };

        _textBox = new TextBox
        {
            Left = 20,
            Top = 70,
            Width = 450,
            PasswordChar = '*'
        };

        var okButton = new Button
        {
            Text = SR.DialogOk,
            Left = 300,
            Top = 110,
            Width = 80,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = SR.DialogCancel,
            Left = 390,
            Top = 110,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(label);
        Controls.Add(_textBox);
        Controls.Add(okButton);
        Controls.Add(cancelButton);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
