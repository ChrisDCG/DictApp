using System.Security.Cryptography;
using System.Text;

namespace OpenAIDictate.Services;

/// <summary>
/// Handles encryption and decryption of secrets using Windows DPAPI (Data Protection API)
/// Secrets are encrypted per-user and cannot be decrypted by other users or on other machines
/// </summary>
public static class SecretStore
{
    /// <summary>
    /// Encrypts a plaintext secret using DPAPI (CurrentUser scope)
    /// </summary>
    /// <param name="plaintext">The secret to encrypt</param>
    /// <returns>Base64-encoded encrypted data</returns>
    public static string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));
        }

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] encryptedBytes = ProtectedData.Protect(
            plaintextBytes,
            optionalEntropy: null,
            scope: DataProtectionScope.CurrentUser
        );

        return Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// Decrypts a DPAPI-encrypted secret
    /// </summary>
    /// <param name="encrypted">Base64-encoded encrypted data</param>
    /// <returns>Decrypted plaintext</returns>
    public static string Decrypt(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
        {
            throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encrypted));
        }

        try
        {
            byte[] encryptedBytes = Convert.FromBase64String(encrypted);
            byte[] decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser
            );

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "Failed to decrypt secret. It may have been encrypted by a different user or on a different machine.",
                ex
            );
        }
    }
}
