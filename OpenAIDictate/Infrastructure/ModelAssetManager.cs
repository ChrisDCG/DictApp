using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using OpenAIDictate.Services;

namespace OpenAIDictate.Infrastructure;

/// <summary>
/// Ensures availability of external ML assets required by the application.
/// Downloads and caches models from their official sources when necessary.
/// </summary>
public static class ModelAssetManager
{
    private const string SileroVadFileName = "silero_vad_16k_op15.onnx";
    private const string SileroVadSha256 = "7ed98ddbad84ccac4cd0aeb3099049280713df825c610a8ed34543318f1b2c49";

    private static readonly Uri SileroVadDownloadUri = new("https://raw.githubusercontent.com/snakers4/silero-vad/master/src/silero_vad/data/silero_vad_16k_op15.onnx");
    private static readonly HttpClient HttpClient = CreateHttpClient();

    /// <summary>
    /// Resolves the Silero VAD model path, downloading it from the official repository if required.
    /// </summary>
    public static async Task<string> EnsureSileroVadModelAsync(string? overridePath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
        {
            return overridePath;
        }

        string? envOverride = Environment.GetEnvironmentVariable("SILERO_VAD_MODEL_PATH");
        if (!string.IsNullOrWhiteSpace(envOverride) && File.Exists(envOverride))
        {
            return envOverride;
        }

        foreach (string candidate in EnumerateLocalCandidates())
        {
            if (File.Exists(candidate) && await VerifyChecksumAsync(candidate, cancellationToken).ConfigureAwait(false))
            {
                return candidate;
            }
        }

        string cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenAIDictate",
            "Models");

        Directory.CreateDirectory(cacheDirectory);
        string destinationPath = Path.Combine(cacheDirectory, SileroVadFileName);

        if (File.Exists(destinationPath) && await VerifyChecksumAsync(destinationPath, cancellationToken).ConfigureAwait(false))
        {
            return destinationPath;
        }

        await DownloadSileroVadAsync(destinationPath, cancellationToken).ConfigureAwait(false);

        if (!await VerifyChecksumAsync(destinationPath, cancellationToken).ConfigureAwait(false))
        {
            TryDelete(destinationPath);
            throw new InvalidDataException("Silero VAD model checksum verification failed after download.");
        }

        return destinationPath;
    }

    private static IEnumerable<string> EnumerateLocalCandidates()
    {
        string baseDirectory = AppContext.BaseDirectory;
        yield return Path.Combine(baseDirectory, SileroVadFileName);
        yield return Path.Combine(baseDirectory, "Resources", "Models", SileroVadFileName);
    }

    private static async Task DownloadSileroVadAsync(string destinationPath, CancellationToken cancellationToken)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            Logger.LogInfo("Downloading Silero VAD model from official repository...");

            using HttpResponseMessage response = await HttpClient.GetAsync(
                SileroVadDownloadUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            await using (var destination = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempFile, destinationPath, overwrite: true);
            Logger.LogInfo($"Silero VAD model stored at {destinationPath}.");
        }
        catch
        {
            TryDelete(tempFile);
            throw;
        }
    }

    private static async Task<bool> VerifyChecksumAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        string actual = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Equals(actual, SileroVadSha256, StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignore cleanup failures
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("OpenAIDictate/1.0 (+https://github.com/openai)");
        return client;
    }
}
