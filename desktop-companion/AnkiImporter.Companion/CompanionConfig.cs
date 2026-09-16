using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AnkiImporter.Companion;

public sealed record CompanionConfig(
    string ServerUrl,
    string DeviceId,
    string DeviceToken,
    DateTimeOffset PairedAtUtc);

public static class CompanionConfigStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string ConfigDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AnkiImporter");

    public static string ConfigPath => Path.Combine(ConfigDirectory, "companion.json");

    public static async Task SaveAsync(CompanionConfig config, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(ConfigDirectory);

        var protectedToken = Protect(config.DeviceToken);
        var disk = new DiskConfig(
            config.ServerUrl,
            config.DeviceId,
            protectedToken,
            config.PairedAtUtc);

        await File.WriteAllTextAsync(
            ConfigPath,
            JsonSerializer.Serialize(disk, Json),
            new UTF8Encoding(false),
            cancellationToken);
    }

    public static async Task<CompanionConfig?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ConfigPath)) return null;

        var text = await File.ReadAllTextAsync(ConfigPath, cancellationToken);
        var disk = JsonSerializer.Deserialize<DiskConfig>(text, Json)
                   ?? throw new InvalidOperationException("Invalid Companion configuration file.");

        return new CompanionConfig(
            disk.ServerUrl,
            disk.DeviceId,
            Unprotect(disk.ProtectedDeviceToken),
            disk.PairedAtUtc);
    }

    public static void Delete()
    {
        if (File.Exists(ConfigPath)) File.Delete(ConfigPath);
    }

    private static string Protect(string value)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The Companion credential store currently supports Windows only.");

        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string value)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The Companion credential store currently supports Windows only.");

        var protectedBytes = Convert.FromBase64String(value);
        var bytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }

    private sealed record DiskConfig(
        string ServerUrl,
        string DeviceId,
        string ProtectedDeviceToken,
        DateTimeOffset PairedAtUtc);
}
