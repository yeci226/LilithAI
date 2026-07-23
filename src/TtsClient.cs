using System.Text;
using System.Text.Json;

namespace LilithAI;

public enum VoiceMode
{
    Off,
    Chinese,
    Japanese,
}

public enum VoiceServiceStatus
{
    Off,
    MissingRuntime,
    MissingReference,
    ManualStart,
    Loading,
    Ready,
    Failed,
}

public static class UiMath
{
    public static int MouseWheelDelta(long wParam) => unchecked((short)(wParam >> 16));

    public static float ClampScrollOffset(float offset, float contentHeight, float viewportHeight) =>
        Math.Clamp(offset, 0f, Math.Max(0f, contentHeight - viewportHeight));
}

public static class TtsClient
{
    public const int VoicePlaybackDelayFrames = 2;

    public static bool ShouldRestartInterruptedPlayback(bool playingExpectedClip, float remainingSeconds, bool alreadyRetried) =>
        !playingExpectedClip && remainingSeconds > 0.1f && !alreadyRetried;

    public static bool ShouldStopLocalVoiceHosts(VoiceMode mode, bool autoStart) =>
        mode == VoiceMode.Off || !autoStart;

    public static VoiceServiceStatus GetVoiceServiceStatus(
        VoiceMode mode,
        bool runtimeExists,
        bool referenceExists,
        bool autoStart,
        bool running,
        bool ready,
        bool failed)
    {
        if (mode == VoiceMode.Off)
            return VoiceServiceStatus.Off;
        if (!runtimeExists)
            return VoiceServiceStatus.MissingRuntime;
        if (!referenceExists)
            return VoiceServiceStatus.MissingReference;
        if (!autoStart)
            return VoiceServiceStatus.ManualStart;
        if (failed)
            return VoiceServiceStatus.Failed;
        return running && ready ? VoiceServiceStatus.Ready : VoiceServiceStatus.Loading;
    }

    private static readonly HttpClient Http = new();

    public static string SpokenLanguage(VoiceMode mode) => mode switch
    {
        VoiceMode.Chinese => "Chinese",
        VoiceMode.Japanese => "Japanese",
        _ => string.Empty,
    };

    public static float DialogueDuration(float speechSeconds) => Math.Max(6f, speechSeconds + 1f);

    public static string SelectSpeech(VoiceMode mode, AiReply reply, string displayLanguage)
    {
        if (mode == VoiceMode.Off || !string.IsNullOrWhiteSpace(reply.Speech))
            return mode == VoiceMode.Off ? reply.Text : reply.Speech;

        return mode == VoiceMode.Chinese && displayLanguage is "zh-Hant" or "zh-Hans" ||
               mode == VoiceMode.Japanese && displayLanguage == "ja"
            ? reply.Text
            : string.Empty;
    }

    public static AiReply[] SplitForSpeech(AiReply reply)
    {
        var textParts = SplitLines(reply.Text);
        var speechParts = string.IsNullOrWhiteSpace(reply.Speech) ? textParts : SplitLines(reply.Speech);
        if (textParts.Length <= 1 || textParts.Length != speechParts.Length)
            return new[] { reply };

        var inlineActions = reply.InlineActions.Length == textParts.Length ? reply.InlineActions : null;
        return textParts.Select((text, index) => new AiReply(
            text,
            inlineActions?[index] ?? (index == 0 ? reply.Action : "None"),
            string.IsNullOrWhiteSpace(reply.Speech) ? string.Empty : speechParts[index],
            index == 0 ? reply.Clothing : "None",
            index == 0 ? reply.Command : "None",
            index == 0 ? reply.Argument : string.Empty)).ToArray();
    }

    private static string[] SplitLines(string text) => text
        .Replace("\r\n", "\n")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string BuildPayloadJson(VoiceMode mode, string text, string referencePath) =>
        mode switch
        {
            VoiceMode.Chinese => JsonSerializer.Serialize(new
            {
                text,
                text_lang = "zh",
                ref_audio_path = referencePath,
                aux_ref_audio_paths = Array.Empty<string>(),
                prompt_lang = "zh",
                prompt_text = "你的選擇創造了我，所以我的存在本身就是你的善意。",
                text_split_method = "cut0",
                batch_size = 1,
                media_type = "wav",
                streaming_mode = false,
                seed = 42,
            }),
            VoiceMode.Japanese => JsonSerializer.Serialize(new
            {
                model = "irodori-tts",
                input = text,
                response_format = "wav",
                irodori = new { ref_wav = referencePath, seed = 42 },
            }),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };

    public static async Task<byte[]> SynthesizeAsync(
        VoiceMode mode,
        string endpoint,
        string referencePath,
        string text,
        int timeoutSeconds,
        int attempts,
        CancellationToken cancellationToken,
        Action<string>? log = null)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || !uri.IsLoopback)
            throw new InvalidOperationException("TTS endpoint must use localhost.");
        if (!File.Exists(referencePath))
            throw new FileNotFoundException("TTS reference voice was not found.", referencePath);

        var payload = BuildPayloadJson(mode, text.Trim(), referencePath);
        attempts = Math.Clamp(attempts, 1, 10);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, uri)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                };
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 300)));
                using var response = await Http.SendAsync(request, timeout.Token).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"TTS HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException exception) when (attempt < attempts)
            {
                log?.Invoke($"TTS service is starting; retrying in 5 seconds ({attempt}/{attempts}): {exception.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
