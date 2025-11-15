using FluentAssertions;
using OpenAIDictate.Services;
using Xunit;

namespace OpenAIDictate.Tests.Unit.Services;

/// <summary>
/// Unit tests for SecretStore (DPAPI encryption)
/// </summary>
public class SecretStoreTests
{
    [Fact]
    public void Encrypt_ValidPlaintext_ShouldReturnBase64String()
    {
        // Arrange
        const string plaintext = "test-api-key-12345";

        // Act
        var encrypted = SecretStore.Encrypt(plaintext);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plaintext);
        
        // Should be valid base64
        var act = () => Convert.FromBase64String(encrypted);
        act.Should().NotThrow();
    }

    [Fact]
    public void Encrypt_EmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => SecretStore.Encrypt("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encrypt_NullString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => SecretStore.Encrypt(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decrypt_ValidEncryptedString_ShouldReturnOriginalPlaintext()
    {
        // Arrange
        const string plaintext = "test-api-key-12345";
        var encrypted = SecretStore.Encrypt(plaintext);

        // Act
        var decrypted = SecretStore.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        const string original = "sk-test1234567890abcdefghijklmnopqrstuvwxyz";

        // Act
        var encrypted = SecretStore.Encrypt(original);
        var decrypted = SecretStore.Decrypt(encrypted);

        // Assert
        decrypted.Should().Be(original);
    }

    [Fact]
    public void Decrypt_InvalidBase64_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string invalidEncrypted = "not-valid-base64!!!";

        // Act & Assert
        var act = () => SecretStore.Decrypt(invalidEncrypted);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Decrypt_EmptyString_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => SecretStore.Decrypt("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Encrypt_DifferentPlaintexts_ShouldProduceDifferentCiphertexts()
    {
        // Arrange
        const string plaintext1 = "key1";
        const string plaintext2 = "key2";

        // Act
        var encrypted1 = SecretStore.Encrypt(plaintext1);
        var encrypted2 = SecretStore.Encrypt(plaintext2);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }
}

