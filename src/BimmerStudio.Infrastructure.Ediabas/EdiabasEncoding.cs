using System.Text;

namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// Registers the legacy code pages EDIABAS data depends on.
/// </summary>
/// <remarks>
/// Text inside SGBDs — job comments, result strings, fault-code texts — is stored as
/// Windows-1252 bytes. .NET defaults to UTF-8 and does not carry the CP1252 codec outside
/// Windows, so without this every German umlaut would decode to a replacement character.
/// </remarks>
public static class EdiabasEncoding
{
    private static int _registered;

    /// <summary>Code page 1252, available only after <see cref="EnsureRegistered"/>.</summary>
    public const int WindowsLatin1CodePage = 1252;

    /// <summary>
    /// Idempotent and safe to call from multiple threads. Call once during composition, before
    /// any SGBD is read.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>The Windows-1252 encoding. Registers the provider if needed.</summary>
    public static Encoding WindowsLatin1
    {
        get
        {
            EnsureRegistered();
            return Encoding.GetEncoding(WindowsLatin1CodePage);
        }
    }
}
