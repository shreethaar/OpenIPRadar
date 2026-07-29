using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using OpenIPRadar.Core.Abstractions;

namespace OpenIPRadar.Services.Security;

/// <summary>
/// Persists provider API keys encrypted at rest with Windows DPAPI (CurrentUser scope).
/// DPAPI is invoked directly through <c>crypt32.dll</c> to avoid taking a NuGet dependency
/// on <c>System.Security.Cryptography.ProtectedData</c>. Keys are stored as a small JSON map
/// of provider name to Base64 ciphertext at <c>%AppData%\OpenIPRadar\keys.dat</c>.
/// Plaintext keys are never written to disk and never logged.
/// </summary>
public sealed class DpapiKeyStore : ISecureKeyStore
{
    private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    private readonly string _filePath;
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _cache;

    /// <summary>Initializes the store, loading any previously saved keys.</summary>
    /// <param name="userDirectory">The per-user directory in which to store <c>keys.dat</c>.</param>
    public DpapiKeyStore(string userDirectory)
    {
        Directory.CreateDirectory(userDirectory);
        _filePath = Path.Combine(userDirectory, "keys.dat");
        _cache = LoadFile();
    }

    /// <inheritdoc />
    public string? GetKey(string providerName)
    {
        lock (_sync)
        {
            if (!_cache.TryGetValue(providerName, out var base64))
            {
                return null;
            }

            try
            {
                var cipher = Convert.FromBase64String(base64);
                var plain = Unprotect(cipher);
                return Encoding.UTF8.GetString(plain);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <inheritdoc />
    public void SetKey(string providerName, string apiKey)
    {
        lock (_sync)
        {
            var cipher = Protect(Encoding.UTF8.GetBytes(apiKey));
            _cache[providerName] = Convert.ToBase64String(cipher);
            SaveFile();
        }
    }

    /// <inheritdoc />
    public void RemoveKey(string providerName)
    {
        lock (_sync)
        {
            if (_cache.Remove(providerName))
            {
                SaveFile();
            }
        }
    }

    /// <inheritdoc />
    public bool HasKey(string providerName)
    {
        lock (_sync)
        {
            return _cache.ContainsKey(providerName);
        }
    }

    private Dictionary<string, string> LoadFile()
    {
        if (!File.Exists(_filePath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void SaveFile()
    {
        var json = JsonSerializer.Serialize(_cache);
        File.WriteAllText(_filePath, json);
    }

    private static byte[] Protect(byte[] data) => Transform(data, encrypt: true);

    private static byte[] Unprotect(byte[] data) => Transform(data, encrypt: false);

    private static byte[] Transform(byte[] input, bool encrypt)
    {
        var inBlob = default(DATA_BLOB);
        var outBlob = default(DATA_BLOB);
        var handle = GCHandle.Alloc(input, GCHandleType.Pinned);
        try
        {
            inBlob.cbData = input.Length;
            inBlob.pbData = handle.AddrOfPinnedObject();

            var ok = encrypt
                ? CryptProtectData(ref inBlob, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob)
                : CryptUnprotectData(ref inBlob, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, ref outBlob);

            if (!ok)
            {
                throw new InvalidOperationException(
                    $"DPAPI {(encrypt ? "encryption" : "decryption")} failed.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            var result = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
            return result;
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }

            if (outBlob.pbData != IntPtr.Zero)
            {
                LocalFree(outBlob.pbData);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DATA_BLOB
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DATA_BLOB pDataIn, string? szDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DATA_BLOB pDataIn, IntPtr ppszDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags, ref DATA_BLOB pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
