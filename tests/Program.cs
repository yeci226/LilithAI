using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LilithAI;

AiReply.SelfTest();
var configTestDirectory = Path.Combine(Path.GetTempPath(), $"LilithAI-config-{Guid.NewGuid():N}");
Directory.CreateDirectory(configTestDirectory);
try
{
    var legacyConfig = Path.Combine(configTestDirectory, ConfigMigration.LegacyFileName);
    File.WriteAllText(legacyConfig, "ApiKey = preserved");
    var migratedConfig = ConfigMigration.Prepare(configTestDirectory);
    if (Path.GetFileName(migratedConfig) != ConfigMigration.FileName ||
        !File.Exists(migratedConfig) || File.Exists(legacyConfig) ||
        File.ReadAllText(migratedConfig) != "ApiKey = preserved")
        throw new InvalidOperationException("Config filename migration self-test failed");
}
finally
{
    Directory.Delete(configTestDirectory, true);
}
var memoryTestDirectory = Path.Combine(Path.GetTempPath(), $"LilithAI-memory-{Guid.NewGuid():N}");
var memoryTestFile = Path.Combine(memoryTestDirectory, "memory.json");
try
{
    LocalJsonFile.Save(memoryTestFile, new[] { "first" });
    LocalJsonFile.Save(memoryTestFile, new[] { "second" });
    File.WriteAllText(memoryTestFile, "not json");
    var recovered = LocalJsonFile.Load<string[]>(memoryTestFile);
    var restored = JsonSerializer.Deserialize<string[]>(File.ReadAllText(memoryTestFile));
    var memories = new List<LongTermMemory>
    {
        new("玩家喜歡草莓蛋糕", DateTimeOffset.Parse("2026-07-20T10:00:00+08:00")),
        new("玩家明天要見小美", DateTimeOffset.Parse("2026-07-21T10:00:00+08:00")),
    };
    var found = LongTermMemoryStore.Search(memories, "還記得我喜歡哪種蛋糕嗎？", 1);
    if (recovered?.Length != 1 || recovered[0] != "first" || restored?.Length != 1 || restored[0] != "first" ||
        found.Length != 1 || found[0].Text != "玩家喜歡草莓蛋糕" ||
        !LongTermMemoryStore.Remember(memories, "我們約好週五一起聽音樂", DateTimeOffset.Now) ||
        LongTermMemoryStore.Remember(memories, "我們約好週五一起聽音樂", DateTimeOffset.Now))
        throw new InvalidOperationException("Long-term memory and backup recovery self-test failed");
}
finally
{
    if (Directory.Exists(memoryTestDirectory))
        Directory.Delete(memoryTestDirectory, true);
}
if (UiMath.MouseWheelDelta(120L << 16) != 120 ||
    UiMath.MouseWheelDelta((long)unchecked((ushort)(short)-120) << 16) != -120 ||
    UiMath.ClampScrollOffset(-10f, 300f, 100f) != 0f ||
    UiMath.ClampScrollOffset(250f, 300f, 100f) != 200f ||
    UiMath.ClampScrollOffset(20f, 80f, 100f) != 0f)
    throw new InvalidOperationException("Tray scroll bounds self-test failed");
if (TtsClient.VoicePlaybackDelayFrames != 2 ||
    TtsClient.ShouldStopLocalVoiceHosts(VoiceMode.Japanese, true) ||
    !TtsClient.ShouldStopLocalVoiceHosts(VoiceMode.Off, true) ||
    !TtsClient.ShouldStopLocalVoiceHosts(VoiceMode.Chinese, false) ||
    TtsClient.GetVoiceServiceStatus(VoiceMode.Off, false, false, true, false, false, false) != VoiceServiceStatus.Off ||
    TtsClient.GetVoiceServiceStatus(VoiceMode.Japanese, false, true, true, false, false, false) != VoiceServiceStatus.MissingRuntime ||
    TtsClient.GetVoiceServiceStatus(VoiceMode.Japanese, true, false, true, false, false, false) != VoiceServiceStatus.MissingReference ||
    TtsClient.GetVoiceServiceStatus(VoiceMode.Japanese, true, true, true, true, true, false) != VoiceServiceStatus.Ready ||
    TtsClient.GetVoiceServiceStatus(VoiceMode.Chinese, true, true, false, false, false, false) != VoiceServiceStatus.ManualStart)
    throw new InvalidOperationException("TTS playback and warm-host policy self-test failed");
var translatedReply = new AiReply("English display", "None", "Chinese speech");
var splitReply = TtsClient.SplitForSpeech(new AiReply("第一段\n\n第二段", "Greet", "一段目\n\n二段目"));
var inlineReply = AiReply.Parse("first\n\uFF08\u52D5\u4F5C\uFF1ATiltHead\uFF09\nsecond\n(action: Stretch)");
var inlineSegments = TtsClient.SplitForSpeech(inlineReply);
if (splitReply.Length != 2 || splitReply[0].Text != "第一段" || splitReply[0].Action != "Greet" ||
    splitReply[1].Speech != "二段目" || splitReply[1].Action != "None" ||
    inlineSegments.Length != 2 || inlineSegments[0].Text != "first" || inlineSegments[0].Action != "TiltHead" ||
    inlineSegments[1].Text != "second" || inlineSegments[1].Action != "Stretch" ||
    TtsClient.SplitForSpeech(new AiReply("第一段\n第二段", "None", "一段だけ")).Length != 1)
    throw new InvalidOperationException("Synchronized speech splitting self-test failed");
var clothingReply = AiReply.Parse("{\"text\":\"去換衣服\",\"action\":\"None\",\"clothing\":\"Pajamas\"}");
var commandReply = AiReply.Parse("{\"text\":\"開始計時\",\"action\":\"None\",\"command\":\"SetTimer\",\"argument\":\"25\"}");
var memoryReply = AiReply.Parse("{\"text\":\"我記住了\",\"action\":\"None\",\"memory\":\"玩家喜歡草莓蛋糕\"}");
var splitClothingReply = TtsClient.SplitForSpeech(clothingReply with { Text = "第一段\n第二段" });
var splitCommandReply = TtsClient.SplitForSpeech(commandReply with { Text = "第一段\n第二段" });
var now = new DateTime(2026, 7, 23, 10, 0, 0);
if (clothingReply.Clothing != "Pajamas" || memoryReply.Memory != "玩家喜歡草莓蛋糕" || AiReply.Parse("{\"text\":\"只聊天\",\"action\":\"None\"}").Command != "None" ||
    AiCommandProtocol.ResolveClothing("只是在聊睡衣", "睡衣確實很舒服", "Pajamas") != "Pajamas" ||
    AiCommandProtocol.ResolveClothing("可以換成睡衣嗎", "好喔，那換成舒服的睡衣吧", "None") != "Pajamas" ||
    AiCommandProtocol.ResolveClothing("Please switch to casual clothes", "Sure, I will change into casual clothes.", "None") != "Casual" ||
    AiCommandProtocol.ResolveClothing("你的睡衣很好看", "謝謝你喜歡這套睡衣", "None") != "None" ||
    AiCommandProtocol.ResolveClothing("可以換成睡衣嗎", "我們只聊聊睡衣吧", "None") != "None" ||
    splitClothingReply[1].Clothing != "None" || commandReply.Command != "SetTimer" || commandReply.Argument != "25" ||
    splitCommandReply[1].Command != "None" ||
    !AiCommandProtocol.TryParseTimerSeconds("25", out var timerSeconds) || timerSeconds != 1500f ||
    AiCommandProtocol.TryParseTimerSeconds("0", out _) || AiCommandProtocol.TryParseTimerSeconds("1441", out _) ||
    !AiCommandProtocol.TryParseAlarm("2026-07-24T08:00:00", now, out var alarm) || alarm.Hour != 8 ||
    AiCommandProtocol.TryParseAlarm("2026-07-23T09:00:00", now, out _))
    throw new InvalidOperationException("Chat command protocol self-test failed");
if (TtsClient.SpokenLanguage(VoiceMode.Chinese) != "Chinese" ||
    TtsClient.SpokenLanguage(VoiceMode.Japanese) != "Japanese" ||
    TtsClient.SpokenLanguage(VoiceMode.Off) != string.Empty ||
    !TtsClient.ShouldRestartInterruptedPlayback(false, 2f, false) ||
    TtsClient.ShouldRestartInterruptedPlayback(true, 2f, false) ||
    TtsClient.ShouldRestartInterruptedPlayback(false, 0.05f, false) ||
    TtsClient.ShouldRestartInterruptedPlayback(false, 2f, true) ||
    TtsClient.SelectSpeech(VoiceMode.Chinese, translatedReply, "en") != "Chinese speech" ||
    TtsClient.SelectSpeech(VoiceMode.Japanese, translatedReply, "en") != "Chinese speech" ||
    TtsClient.SelectSpeech(VoiceMode.Off, translatedReply, "en") != "English display" ||
    TtsClient.SelectSpeech(VoiceMode.Chinese, translatedReply with { Speech = "" }, "zh-Hant") != "English display" ||
    TtsClient.SelectSpeech(VoiceMode.Japanese, translatedReply with { Speech = "" }, "ja") != "English display" ||
    TtsClient.SelectSpeech(VoiceMode.Japanese, translatedReply with { Speech = "" }, "zh-Hant") != string.Empty ||
    TtsClient.SelectSpeech(VoiceMode.Japanese, translatedReply with { Speech = "" }, "ja") != "English display" ||
    TtsClient.SelectSpeech(VoiceMode.Chinese, translatedReply with { Speech = "" }, "en") != string.Empty ||
    TtsClient.DialogueDuration(3.6f) != 6f || TtsClient.DialogueDuration(8f) != 9f)
    throw new InvalidOperationException("Independent display and speech language self-test failed");
if (!ProviderProfiles.IsSelfHosted(ProviderKind.Ollama) || ProviderProfiles.IsSelfHosted(ProviderKind.OpenAI) ||
    ProviderProfiles.NeedsApiKey(ProviderKind.Ollama) || !ProviderProfiles.NeedsApiKey(ProviderKind.OpenAI) ||
    string.IsNullOrWhiteSpace(ProviderProfiles.DefaultModel(ProviderKind.OpenAI)) ||
    ProviderProfiles.DefaultModel(ProviderKind.OpenRouter) != "nvidia/nemotron-nano-9b-v2:free" ||
    !ProviderProfiles.Models(ProviderKind.OpenRouter).Contains("openrouter/free") ||
    ProviderProfiles.LanguageCode("zh-TW") != "zh-Hant" || ProviderProfiles.LanguageCode("zh-CN") != "zh-Hans" ||
    ProviderProfiles.LanguageCode("ja-JP") != "ja" ||
    !ProviderProfiles.CharacterPrompt("ja-JP").Contains("あなたは") ||
    !ProviderProfiles.CharacterPrompt("ja-JP").Contains("Reply only in Japanese") ||
    !ProviderProfiles.CharacterPrompt("zh-CN").Contains("莉莉丝") ||
    !ProviderProfiles.CharacterPrompt("en-US").StartsWith("You are Lilith") ||
    ProviderProfiles.Localize("ja-JP", "中", "中", "日", "EN") != "日")
    throw new InvalidOperationException("Provider profile self-test failed");
if (!ProviderProfiles.DefaultPrompt.Contains("草莓蛋糕"))
    throw new InvalidOperationException("Character prompt self-test failed");
using (var chinese = JsonDocument.Parse(TtsClient.BuildPayloadJson(VoiceMode.Chinese, "你好", "calm.wav")))
    if (chinese.RootElement.GetProperty("text_lang").GetString() != "zh" ||
        chinese.RootElement.GetProperty("ref_audio_path").GetString() != "calm.wav")
        throw new InvalidOperationException("Chinese TTS payload self-test failed");
using (var japanese = JsonDocument.Parse(TtsClient.BuildPayloadJson(VoiceMode.Japanese, "こんにちは", "calm-ja.wav")))
    if (japanese.RootElement.GetProperty("model").GetString() != "irodori-tts" ||
        japanese.RootElement.GetProperty("irodori").GetProperty("ref_wav").GetString() != "calm-ja.wav")
        throw new InvalidOperationException("Japanese TTS payload self-test failed");
var remoteTtsRejected = false;
try
{
    await TtsClient.SynthesizeAsync(VoiceMode.Chinese, "https://example.com/tts", "calm.wav", "你好", 10, 1, CancellationToken.None);
}
catch (InvalidOperationException)
{
    remoteTtsRejected = true;
}
if (!remoteTtsRejected)
    throw new InvalidOperationException("Remote TTS endpoint self-test failed");
await Test(ProviderKind.OpenAI, "{\"choices\":[{\"message\":{\"content\":\"{\\\"text\\\":\\\"OpenAI OK\\\",\\\"action\\\":\\\"Greet\\\"}\"}}]}", "OpenAI OK");
await Test(ProviderKind.Anthropic, "{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"text\\\":\\\"Claude OK\\\",\\\"action\\\":\\\"Think\\\"}\"}]}", "Claude OK");
await TestEmptyContentRetry();
await TestMissingSpeechRetry();
await TestAnthropicMissingSpeechFallback();
await TestModels();
Console.WriteLine("Lilith AI protocol smoke tests passed");

static async Task TestModels()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var server = Serve(listener, "{\"data\":[{\"id\":\"z-model\"},{\"id\":\"a-model\"}]}");
    var models = await AiClient.ListModelsAsync($"http://127.0.0.1:{port}/v1", "", 10, CancellationToken.None);
    await server;
    if (!models.SequenceEqual(new[] { "a-model", "z-model" }))
        throw new InvalidOperationException("Model list self-test failed");
}

static async Task TestEmptyContentRetry()
{
    var logs = new List<string>();
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var server = Serve(listener,
        "{\"choices\":[{\"message\":{\"content\":null,\"reasoning\":\"internal reasoning only\"},\"finish_reason\":\"stop\"}]}",
        "{\"choices\":[{\"message\":{\"content\":\"retry OK\"},\"finish_reason\":\"stop\"}]}");
    var reply = await AiClient.SendAsync(ProviderKind.OpenRouter, $"http://127.0.0.1:{port}/v1", "test-key",
        "nvidia/nemotron-nano-9b-v2:free", "test prompt", Array.Empty<ChatMessage>(), "hello", 10, CancellationToken.None, logs.Add);
    await server;
    if (reply.Text != "retry OK" || logs.Count(log => log.StartsWith("AI REQUEST")) != 2 ||
        !logs.Any(log => log.StartsWith("AI RETRY")) ||
        !logs.Any(log => log.Contains("\"max_tokens\":512")) ||
        !logs.Any(log => log.Contains("\"content\":\"/no_think\\ntest prompt\"")) ||
        !logs.Any(log => log.Contains("\"reasoning\":{\"effort\":\"none\",\"exclude\":true}")))
        throw new InvalidOperationException("Empty response retry self-test failed");
}

static async Task TestMissingSpeechRetry()
{
    var logs = new List<string>();
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var server = Serve(listener,
        "{\"choices\":[{\"message\":{\"content\":\"只有顯示文字\"},\"finish_reason\":\"stop\"}]}",
        "{\"choices\":[{\"message\":{\"content\":\"{\\\"text\\\":\\\"晚安\\\",\\\"speech\\\":\\\"おやすみ\\\",\\\"action\\\":\\\"None\\\"}\"},\"finish_reason\":\"stop\"}]}");
    var reply = await AiClient.SendAsync(ProviderKind.OpenRouter, $"http://127.0.0.1:{port}/v1", "test-key",
        "openrouter/free", "test prompt", Array.Empty<ChatMessage>(), "hello", 10, CancellationToken.None, logs.Add, "Japanese");
    await server;
    if (reply.Speech != "おやすみ" || logs.Count(log => log.StartsWith("AI REQUEST")) != 2 ||
        !logs.Any(log => log.Contains("omitted Japanese speech")) ||
        !logs.Any(log => log.Contains("previous response omitted speech")) ||
        !logs.Any(log => log.Contains("\"response_format\":{\"type\":\"json_schema\"")) ||
        !logs.Any(log => log.Contains("\"minLength\":1")) ||
        !logs.Any(log => log.Contains("\"provider\":{\"require_parameters\":true}")))
        throw new InvalidOperationException("Missing speech retry self-test failed");
}

static async Task TestAnthropicMissingSpeechFallback()
{
    var logs = new List<string>();
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var server = Serve(listener,
        "{\"content\":[{\"type\":\"text\",\"text\":\"{\\\"text\\\":\\\"Display reply\\\",\\\"action\\\":\\\"None\\\"}\"}]}",
        "{\"content\":[{\"type\":\"text\",\"text\":\"Japanese speech\"}]}");
    var reply = await AiClient.SendAsync(ProviderKind.Anthropic, $"http://127.0.0.1:{port}/v1", "test-key",
        "test-model", "test prompt", Array.Empty<ChatMessage>(), "hello", 10, CancellationToken.None, logs.Add, "Japanese");
    await server;
    if (reply.Text != "Display reply" || reply.Speech != "Japanese speech" ||
        logs.Count(log => log.Contains("Mode: Speech fallback")) != 1 ||
        !logs.Any(log => log.Contains("Return only the translation")))
        throw new InvalidOperationException("Anthropic speech fallback self-test failed");
}

static async Task Test(ProviderKind provider, string responseBody, string expected)
{
    var logs = new List<string>();
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var server = Serve(listener, responseBody);

    var reply = await AiClient.SendAsync(
        provider,
        $"http://127.0.0.1:{port}/v1",
        "test-key",
        "test-model",
        "test prompt",
        new[] { new ChatMessage("assistant", "Built-in game dialogue") },
        "hello",
        10,
        CancellationToken.None,
        logs.Add);

    await server;
    if (reply.Text != expected)
        throw new InvalidOperationException($"Expected {expected}, got {reply.Text}");
    if (!logs.Any(log => log.StartsWith("AI REQUEST")) || !logs.Any(log => log.StartsWith("AI RESPONSE")) ||
        !logs.Any(log => log.StartsWith("AI PARSED")) || !logs.Any(log => log.Contains("Built-in game dialogue")) ||
        logs.Any(log => log.Contains("test-key")) ||
        provider != ProviderKind.OpenRouter && logs.Any(log => log.Contains("\"reasoning\"")))
        throw new InvalidOperationException("AI logging self-test failed");
}

static async Task Serve(TcpListener listener, params string[] responseBodies)
{
    try
    {
        foreach (var responseBody in responseBodies)
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);

            var contentLength = 0;
            string? line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                    contentLength = int.Parse(line.Split(':', 2)[1].Trim());

            if (contentLength > 0)
            {
                var body = new char[contentLength];
                await reader.ReadBlockAsync(body, 0, body.Length);
            }

            var payload = Encoding.UTF8.GetBytes(responseBody);
            var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(headers);
            await stream.WriteAsync(payload);
        }
    }
    finally
    {
        listener.Stop();
    }
}
