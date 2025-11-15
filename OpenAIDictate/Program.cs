using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAIDictate.Infrastructure;
using System.Globalization;
using OpenAIDictate.Resources;
using OpenAIDictate.Services;
using Serilog;

namespace OpenAIDictate;

/// <summary>
/// Application entry point configuring dependency injection and logging before starting the tray context.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        IAppLogger? logger = null;

        try
        {
            logger = new SerilogLogger();
            logger.LogInfo("========================================");
            logger.LogInfo("OpenAIDictate starting...");
            logger.LogInfo("Version: {Version}", "1.2.0");
            logger.LogInfo("OS: {OSVersion}", Environment.OSVersion);
            logger.LogInfo(".NET: {NetVersion}", Environment.Version);
            logger.LogInfo("========================================");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .Build();

            var services = new ServiceCollection();
            services.AddApplicationServices(configuration);

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            var appContext = serviceProvider.GetRequiredService<AppTrayContext>();

            Application.Run(appContext);

            logger.LogInfo("OpenAIDictate terminated");
        }
        catch (Exception ex)
        {
            if (logger != null)
            {
                logger.LogError(ex, "Fatal error: {Message}", ex.Message);
            }
            else
            {
                Logger.LogError($"Fatal error: {ex.Message}");
                Logger.LogError($"Stack trace: {ex.StackTrace}");
            }

            MessageBox.Show(
                string.Format(CultureInfo.CurrentCulture, SR.FatalErrorMessage, ex.Message),
                SR.FatalErrorTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

            Environment.Exit(1);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
