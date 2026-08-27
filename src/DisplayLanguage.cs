using System.Runtime.InteropServices;
using System.Text;

namespace LilithAI;

public static class DisplayLanguage
{
    private const uint TraditionalChineseMap = 0x04000000;
    private const uint SimplifiedChineseMap = 0x02000000;

    public static string NormalizeChinese(string? text, string? language, Action<string>? warning = null)
    {
        var input = text ?? string.Empty;
        var code = ProviderProfiles.LanguageCode(language);
        var (locale, flags) = code switch
        {
            "zh-Hant" => ("zh-TW", TraditionalChineseMap),
            "zh-Hans" => ("zh-CN", SimplifiedChineseMap),
            _ => (string.Empty, 0u),
        };
        if (string.IsNullOrEmpty(input) || flags == 0)
            return input;
        if (!OperatingSystem.IsWindows())
        {
            warning?.Invoke($"Chinese display normalization unavailable on {Environment.OSVersion.Platform}; preserving provider text.");
            return input;
        }

        try
        {
            var output = new StringBuilder(input.Length * 2 + 1);
            var length = LCMapStringEx(locale, flags, input, input.Length, output, output.Capacity,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (length <= 0)
                throw new InvalidOperationException($"LCMapStringEx failed with Win32 error {Marshal.GetLastWin32Error()}");
            return output.ToString(0, Math.Min(length, output.Length));
        }
        catch (Exception exception)
        {
            warning?.Invoke($"Chinese display normalization failed ({exception.GetType().Name}); preserving provider text.");
            return input;
        }
    }

    public static AiReply NormalizeReply(AiReply reply, string? language, Action<string>? warning = null)
    {
        var text = NormalizeChinese(reply.Text, language, warning);
        var speech = NormalizeChinese(reply.Speech, language, warning);
        var memory = NormalizeChinese(reply.Memory, language, warning);
        return reply with { Text = text, Speech = speech, Memory = memory };
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int LCMapStringEx(
        string localeName,
        uint mapFlags,
        string source,
        int sourceLength,
        StringBuilder destination,
        int destinationCapacity,
        IntPtr versionInformation,
        IntPtr reserved,
        IntPtr sortHandle);
}
