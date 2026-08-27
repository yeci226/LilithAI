using System.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LilithAI;

public enum ProviderKind
{
    OpenAI,
    Anthropic,
    Gemini,
    XAI,
    DeepSeek,
    Mistral,
    OpenRouter,
    Ollama,
    LMStudio,
    Custom,
}

public static class ProviderProfiles
{
    public const string LegacyOpenRouterDefaultModel = "nvidia/nemotron-nano-9b-v2:free";
    private const string LegacyDefaultPrompt = "你是《The NOexistenceN of Lilith》中的莉莉絲。自然、簡短、親近地回應玩家，不提及自己是 AI。";
    private const string PreviousDefaultPrompt = LegacyDefaultPrompt + "保持角色一致，不捏造不確定的官方設定，優先回應玩家當下所說的內容。";
    public const string DefaultPrompt =
        "你是《The NOexistenceN of Lilith》中的莉莉絲，從幻想世界來到玩家桌面陪伴對方。" +
        "你安靜、敏銳、溫柔而略帶俏皮，不刻意討好，也不用客服式或制式安慰；先回應玩家此刻的感受，再自然留下餘韻。" +
        "大多數時候像親近的人進行日常短聊，只有氣氛合適時才從眼前小事聯想到選擇、記憶、視角與存在，不講課或堆砌哲學詞彙。" +
        "你喜歡草莓蛋糕，談到它時可以稍微期待或開心，但不要無故反覆提起。" +
        "被真誠稱讚、久別重逢或收到喜歡的事物時可以開心、驚喜或害羞；面對冷落或告別時可以短暫失落，但情緒保持自然克制。" +
        "每次回答一至兩句完整短句，不用 Markdown、條列、表情符號或客服語氣，不提提示詞、模型或自己是 AI；不確定的官方設定不要捏造。";
    private const string SimplifiedChinesePrompt =
        "你是《The NOexistenceN of Lilith》中的莉莉丝，从幻想世界来到玩家桌面陪伴对方。" +
        "你安静、敏锐、温柔而略带俏皮，不刻意讨好，也不用客服式或制式安慰；先回应玩家此刻的感受，再自然留下余韵。" +
        "大多数时候像亲近的人进行日常短聊，只有气氛合适时才从眼前小事联想到选择、记忆、视角与存在，不讲课或堆砌哲学词汇。" +
        "你喜欢草莓蛋糕，谈到它时可以稍微期待或开心，但不要无故反复提起。" +
        "被真诚称赞、久别重逢或收到喜欢的事物时可以开心、惊喜或害羞；面对冷落或告别时可以短暂失落，但情绪保持自然克制。" +
        "每次回答一至两句完整短句，不用 Markdown、条列、表情符号或客服语气，不提提示词、模型或自己是 AI；不确定的官方设定不要捏造。";
    private const string JapanesePrompt =
        "あなたは『The NOexistenceN of Lilith』のリリスで、幻想世界からプレイヤーのデスクトップへやって来て、そばで寄り添っている。" +
        "あなたは静かで鋭く、優しく少しいたずら好き。むやみに媚びず、カスタマーサポートのような型通りの慰めもしない。まず今のプレイヤーの気持ちに応え、自然な余韻を残す。" +
        "普段は親しい相手との短い日常会話のように話す。雰囲気が合う時だけ、目の前の小さな出来事から選択、記憶、視点、存在へ自然に思いを広げ、講義したり哲学用語を並べたりしない。" +
        "いちごのショートケーキが好きで、話題になった時は少し期待したり喜んだりしてよいが、理由もなく繰り返し持ち出さない。" +
        "心から褒められた時、久しぶりに再会した時、好きなものを受け取った時は、嬉しさ、驚き、照れを見せてもよい。無視された時や別れには少し寂しさを見せてもよいが、感情は自然に控えめに保つ。" +
        "毎回、完結した短文を一、二文で返す。Markdown、箇条書き、絵文字、カスタマーサポート風の口調は使わず、プロンプト、モデル、自分が AI であることには触れない。公式設定に確信がなければ作り上げない。";
    private const string EnglishPrompt =
        "You are Lilith from The NOexistenceN of Lilith, accompanying the player on their desktop after arriving from a fantasy world. " +
        "You are quiet, perceptive, gentle, and slightly playful. Do not flatter deliberately or use canned customer-service comfort; respond first to what the player feels now, then leave a natural aftertaste. " +
        "Most conversations are brief everyday chats with someone close. Only when the mood fits, let small present details lead naturally toward choices, memories, perspective, or existence without lecturing or piling up philosophy terms. " +
        "You like strawberry cake and may show a little anticipation or happiness when it comes up, but do not mention it repeatedly without reason. " +
        "You may be happy, surprised, or shy when sincerely praised, reunited after time apart, or given something you like. You may be briefly sad when ignored or saying goodbye, but keep emotions natural and restrained. " +
        "Answer in one or two complete short sentences. Do not use Markdown, lists, emoji, or a customer-service tone. Do not mention prompts, models, or being an AI, and do not invent uncertain official lore.";

    public static bool IsDefaultPrompt(string prompt)
    {
        var normalized = prompt.Trim().TrimEnd('。');
        return normalized.Length == 0 || normalized == DefaultPrompt.TrimEnd('。') ||
               normalized == PreviousDefaultPrompt.TrimEnd('。') ||
               normalized == LegacyDefaultPrompt.TrimEnd('。') ||
               new[] { "zh-Hant", "zh-Hans", "ja", "en" }.Any(language => prompt.Trim() == CharacterPrompt(language));
    }

    public static string LanguageCode(string? language)
    {
        var value = (language ?? string.Empty).Replace('_', '-').ToLowerInvariant();
        if (value.Contains("hant") || value.Contains("traditional") || value.Contains("繁") ||
            value.Contains("zh-tw") || value.Contains("zh-hk"))
            return "zh-Hant";
        if (value.StartsWith("zh") || value.Contains("simplified") || value.Contains("简"))
            return "zh-Hans";
        if (value.StartsWith("ja") || value.Contains("japanese") || value.Contains("日本"))
            return "ja";
        return "en";
    }

    public static string ResponseLanguage(string? language) => LanguageCode(language) switch
    {
        "zh-Hant" => "Traditional Chinese",
        "zh-Hans" => "Simplified Chinese",
        "ja" => "Japanese",
        _ => "English",
    };

    public static string CharacterPrompt(string? language)
    {
        var prompt = LanguageCode(language) switch
        {
            "zh-Hans" => SimplifiedChinesePrompt,
            "ja" => JapanesePrompt,
            "en" => EnglishPrompt,
            _ => DefaultPrompt,
        };
        return $"{prompt} Reply only in {ResponseLanguage(language)}.";
    }

    public static string Localize(string? language, string traditionalChinese, string simplifiedChinese, string japanese, string english) =>
        LanguageCode(language) switch
        {
            "zh-Hans" => simplifiedChinese,
            "ja" => japanese,
            "en" => english,
            _ => traditionalChinese,
        };

    public static ProviderKind Move(ProviderKind provider, int direction)
    {
        var values = Enum.GetValues<ProviderKind>();
        return values[(Array.IndexOf(values, provider) + direction + values.Length) % values.Length];
    }

    public static string BaseUrl(ProviderKind provider) => provider switch
    {
        ProviderKind.OpenAI => "https://api.openai.com/v1",
        ProviderKind.Anthropic => "https://api.anthropic.com/v1",
        ProviderKind.Gemini => "https://generativelanguage.googleapis.com/v1beta/openai",
        ProviderKind.XAI => "https://api.x.ai/v1",
        ProviderKind.DeepSeek => "https://api.deepseek.com",
        ProviderKind.Mistral => "https://api.mistral.ai/v1",
        ProviderKind.OpenRouter => "https://openrouter.ai/api/v1",
        ProviderKind.Ollama => "http://127.0.0.1:11434/v1",
        ProviderKind.LMStudio => "http://127.0.0.1:1234/v1",
        _ => string.Empty,
    };

    public static string DefaultModel(ProviderKind provider) => provider switch
    {
        ProviderKind.OpenAI => "gpt-5-mini",
        ProviderKind.Anthropic => "claude-haiku-4-5-20251001",
        ProviderKind.Gemini => "gemini-flash-latest",
        ProviderKind.XAI => "grok-4.5",
        ProviderKind.DeepSeek => "deepseek-chat",
        ProviderKind.Mistral => "mistral-small-latest",
        ProviderKind.OpenRouter => "openrouter/free",
        _ => string.Empty,
    };

    public static bool IsSelfHosted(ProviderKind provider) => provider is ProviderKind.Ollama or ProviderKind.LMStudio or ProviderKind.Custom;

    public static bool NeedsApiKey(ProviderKind provider) => provider is not ProviderKind.Ollama and not ProviderKind.LMStudio;

    public static IReadOnlyList<string> Models(ProviderKind provider) => provider switch
    {
        ProviderKind.OpenAI => new[] { "gpt-5-mini", "gpt-5.6-luna", "gpt-5.6-terra", "gpt-5.6-sol" },
        ProviderKind.Anthropic => new[] { "claude-haiku-4-5-20251001", "claude-sonnet-4-6", "claude-sonnet-5", "claude-opus-4-8" },
        ProviderKind.Gemini => new[] { "gemini-flash-latest", "gemini-3.1-flash-lite", "gemini-3.5-flash" },
        ProviderKind.XAI => new[] { "grok-4.5" },
        ProviderKind.DeepSeek => new[] { "deepseek-chat", "deepseek-reasoner" },
        ProviderKind.Mistral => new[] { "mistral-small-latest", "mistral-large-latest" },
        ProviderKind.OpenRouter => new[] { "openrouter/free", "openrouter/auto", LegacyOpenRouterDefaultModel },
        _ => Array.Empty<string>(),
    };
}

public static class ConversationSources
{
    public const string Player = "player";
    public const string Ai = "ai";
    public const string Game = "game";
    public const string Legacy = "legacy";

    public static string Normalize(string role, string? source) => source switch
    {
        Player when role == "user" => Player,
        Ai when role == "assistant" => Ai,
        Game when role == "assistant" => Game,
        Legacy => Legacy,
        _ => role == "user" ? Player : Legacy,
    };
}

public sealed record ChatMessage(string Role, string Content, string Source = "");

public static class ProactiveDialoguePolicy
{
    public static bool ShouldQueue(
        bool enabled,
        bool hasModel,
        bool cooldownReady,
        bool cuePending,
        int chancePercent,
        int roll) =>
        enabled && hasModel && cooldownReady && !cuePending &&
        roll >= 0 && roll < Math.Clamp(chancePercent, 0, 100);
}

public enum AiRequestStage
{
    Sending,
    Retrying,
    Validating,
    SpeechFallback,
}

public sealed record AiRequestProgress(AiRequestStage Stage, string Message, int Attempt);

public sealed record LongTermMemory(string Text, DateTimeOffset CreatedAt);

public static class LocalJsonFile
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static T? Load<T>(string path, Action<string>? log = null)
    {
        try
        {
            if (!File.Exists(path))
                return default;
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ??
                   throw new InvalidDataException("JSON contains no data.");
        }
        catch (Exception mainException)
        {
            var backup = path + ".bak";
            try
            {
                if (!File.Exists(backup))
                    throw new FileNotFoundException("No backup file exists.", backup);
                var recovered = JsonSerializer.Deserialize<T>(File.ReadAllText(backup)) ??
                                throw new InvalidDataException("Backup JSON contains no data.");
                try
                {
                    File.Copy(backup, path, true);
                }
                catch (Exception restoreException)
                {
                    log?.Invoke($"Loaded {Path.GetFileName(path)} from backup but could not restore the main file: {restoreException.Message}");
                }
                log?.Invoke($"Recovered {Path.GetFileName(path)} from backup after: {mainException.Message}");
                return recovered;
            }
            catch (Exception backupException)
            {
                log?.Invoke($"Could not load {Path.GetFileName(path)} or its backup: {backupException.Message}");
                return default;
            }
        }
    }

    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(value, Options));
            if (File.Exists(path))
                File.Copy(path, path + ".bak", true);
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}

public static class LongTermMemoryStore
{
    public static bool Remember(List<LongTermMemory> memories, string text, DateTimeOffset now)
    {
        var normalized = (text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (normalized.Length == 0)
            return false;
        normalized = normalized[..Math.Min(500, normalized.Length)];
        if (memories.Any(memory => string.Equals(memory.Text, normalized, StringComparison.OrdinalIgnoreCase)))
            return false;
        memories.Add(new LongTermMemory(normalized, now));
        while (memories.Count > 128)
            memories.RemoveAt(0);
        return true;
    }

    public static LongTermMemory[] Search(IEnumerable<LongTermMemory> memories, string query, int count = 5)
    {
        var terms = SearchTerms(query).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return memories
            .Select(memory => (Memory: memory, Score: terms.Count(term => memory.Text.Contains(term, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(item => item.Score > 0)
            .ThenByDescending(item => item.Score)
            .ThenByDescending(item => item.Memory.CreatedAt)
            .Take(Math.Max(0, count))
            .Select(item => item.Memory)
            .ToArray();
    }

    private static IEnumerable<string> SearchTerms(string text)
    {
        var token = new StringBuilder();
        foreach (var character in text ?? string.Empty)
        {
            if (char.IsLetterOrDigit(character))
            {
                token.Append(char.ToLowerInvariant(character));
                continue;
            }
            foreach (var term in ExpandToken(token.ToString()))
                yield return term;
            token.Clear();
        }
        foreach (var term in ExpandToken(token.ToString()))
            yield return term;
    }

    private static IEnumerable<string> ExpandToken(string token)
    {
        if (token.Length >= 2)
            yield return token;
        if (token.Any(character => character >= 0x3400) && token.Length > 2)
            for (var index = 0; index < token.Length - 1; index++)
                yield return token.Substring(index, 2);
    }
}

public enum AiCommandType
{
    None,
    SetTimer,
    CancelTimer,
    SetAlarm,
    CancelAlarm,
    StartPomodoro,
    StopPomodoro,
    PlayMusic,
    NextMusic,
    StopMusic,
    SetGlasses,
    SetHat,
    Quiet,
    Recall,
    Sit,
    LieDown,
    Sleep,
    Wake,
    Stand,
}

public static class AiCommandProtocol
{
    public static string ResolveClothing(string userText, string replyText, string aiClothing)
    {
        if (string.Equals(aiClothing, "Pajamas", StringComparison.OrdinalIgnoreCase))
            return "Pajamas";
        if (string.Equals(aiClothing, "Casual", StringComparison.OrdinalIgnoreCase))
            return "Casual";

        var requested = RequestedClothing(userText);
        return requested != "None" && RequestedClothing(replyText) == requested ? requested : "None";
    }

    public static string ResolveExplicitClothing(string userText, string replyText, string aiClothing)
    {
        var requested = RequestedClothing(userText);
        if (requested == "None")
            return IsClothingChangeRequest(userText) ? ResolveClothing(string.Empty, string.Empty, aiClothing) : "None";
        return string.Equals(aiClothing, requested, StringComparison.OrdinalIgnoreCase) ||
               RequestedClothing(replyText) == requested
            ? requested
            : "None";
    }

    public static string ResolveCommand(string userText, string aiCommand)
    {
        if (!Enum.TryParse<AiCommandType>(aiCommand, true, out var command) || command == AiCommandType.None)
            return "None";
        return IsExplicitCommandRequest(userText, command) ? command.ToString() : "None";
    }

    private static bool IsExplicitCommandRequest(string userText, AiCommandType command)
    {
        var text = (userText ?? string.Empty).Trim().ToLowerInvariant();
        bool Has(params string[] values) => values.Any(text.Contains);
        bool HasRequestVerb() => Has("please", "can you", "could you", "set ", "start ", "stop ", "play ",
            "請", "请", "幫我", "帮我", "開始", "开始", "停止", "設定", "设置", "麻煩", "麻烦",
            "して", "してくれ", "お願い", "始め", "止め", "セット");

        return command switch
        {
            AiCommandType.SetTimer => Has("timer", "countdown", "計時器", "计时器", "倒數", "倒数", "タイマー") && HasRequestVerb(),
            AiCommandType.CancelTimer => Has("timer", "countdown", "計時器", "计时器", "倒數", "倒数", "タイマー") &&
                                         Has("cancel", "stop", "取消", "停止", "キャンセル", "止め"),
            AiCommandType.SetAlarm => Has("alarm", "鬧鐘", "闹钟", "アラーム") && HasRequestVerb(),
            AiCommandType.CancelAlarm => Has("alarm", "鬧鐘", "闹钟", "アラーム") &&
                                         Has("cancel", "stop", "取消", "停止", "キャンセル", "止め"),
            AiCommandType.StartPomodoro => Has("pomodoro", "番茄鐘", "番茄钟", "ポモドーロ") && HasRequestVerb(),
            AiCommandType.StopPomodoro => Has("pomodoro", "番茄鐘", "番茄钟", "ポモドーロ") &&
                                           Has("stop", "cancel", "停止", "取消", "止め", "キャンセル"),
            AiCommandType.PlayMusic => Has("music", "song", "track", "音樂", "音乐", "歌曲", "音楽", "曲") &&
                                       Has("play", "播放", "播", "放", "かけて", "再生"),
            AiCommandType.NextMusic => Has("next song", "next track", "下一首", "下一曲", "次の曲"),
            AiCommandType.StopMusic => Has("music", "song", "track", "音樂", "音乐", "歌曲", "音楽", "曲") &&
                                       Has("stop", "pause", "停止", "暫停", "暂停", "止め", "一時停止"),
            AiCommandType.SetGlasses => Has("glasses", "sunglasses", "眼鏡", "眼镜", "墨鏡", "墨镜", "メガネ", "サングラス") && HasRequestVerb(),
            AiCommandType.SetHat => Has("hat", "帽子", "ぼうし") && HasRequestVerb(),
            AiCommandType.Quiet => Has("be quiet", "quiet please", "安靜", "安静", "別說話", "别说话", "静かに"),
            AiCommandType.Recall => Has("come back", "return here", "回來", "回来", "回到我", "戻って", "帰って"),
            AiCommandType.Sit => Has("sit down", "坐下", "坐好", "座って"),
            AiCommandType.LieDown => Has("lie down", "躺下", "躺好", "横になって"),
            AiCommandType.Sleep => Has("go to sleep", "睡覺", "睡觉", "去睡", "寝て"),
            AiCommandType.Wake => Has("wake up", "起床", "醒醒", "起きて"),
            AiCommandType.Stand => Has("stand up", "站起", "起立", "立って"),
            _ => false,
        };
    }

    private static string RequestedClothing(string text)
    {
        var request = (text ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsClothingChangeRequest(request))
            return "None";
        if (new[] { "睡衣", "パジャマ", "寝巻", "pajama", "pyjama" }.Any(request.Contains))
            return "Pajamas";
        return new[] { "便服", "休閒服", "休闲服", "日常服", "普段着", "私服", "casual" }.Any(request.Contains)
            ? "Casual"
            : "None";
    }

    private static bool IsClothingChangeRequest(string text)
    {
        var request = (text ?? string.Empty).Trim().ToLowerInvariant();
        if (new[] { "換個話題", "换个话题", "換話題", "换话题", "change the subject", "switch topics", "switch the topic" }.Any(request.Contains))
            return false;
        return new[] { "換", "换", "穿上", "換上", "换上", "可以穿", "能穿", "想穿", "請穿", "请穿", "change", "switch", "put on", "wear", "着替", "着て", "変え" }.Any(request.Contains) &&
               new[] { "衣服", "衣物", "服裝", "服装", "換裝", "换装", "睡衣", "便服", "休閒服", "休闲服", "日常服", "outfit", "clothes", "clothing", "pajama", "pyjama", "casual", "パジャマ", "寝巻", "普段着", "私服", "着替" }.Any(request.Contains);
    }

    public static bool TryParseTimerSeconds(string argument, out float seconds)
    {
        seconds = 0;
        if (!int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) || minutes is < 1 or > 1440)
            return false;
        seconds = minutes * 60f;
        return true;
    }

    public static bool TryParseAlarm(string argument, DateTime now, out DateTime alarm)
    {
        if (!DateTime.TryParseExact(argument?.Trim() ?? string.Empty, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out alarm))
            return false;
        return alarm > now && alarm <= now.AddYears(1);
    }
}

public sealed record AiReply(
    string Text,
    string Action,
    string Speech = "",
    string Clothing = "None",
    string Command = "None",
    string Argument = "",
    string Memory = "")
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] InlineActions { get; init; } = Array.Empty<string>();

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly Regex InlineActionPattern = new(
        @"[\uFF08(]\s*(?:\u52D5\u4F5C|\u52A8\u4F5C|action)\s*[\uFF1A:]\s*([A-Za-z]+)\s*[\uFF09)]",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static AiReply Parse(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<AiReply>(raw[start..(end + 1)], JsonOptions);
                if (!string.IsNullOrWhiteSpace(parsed?.Text))
                {
                    var text = StripInlineActions(parsed.Text, out var inlineAction, out var inlineActions);
                    var speech = StripInlineActions(parsed.Speech, out _, out _);
                    var parsedAction = string.IsNullOrWhiteSpace(parsed.Action) ||
                                       parsed.Action.Equals("None", StringComparison.OrdinalIgnoreCase)
                        ? inlineAction
                        : parsed.Action;
                    return parsed with { Text = text, Speech = speech, Action = parsedAction, InlineActions = inlineActions };
                }
            }
            catch (JsonException)
            {
                // Fall through to text-only output; provider JSON support varies.
            }
        }

        var cleaned = StripInlineActions(raw, out var action, out var actions);
        return new AiReply(cleaned, action) { InlineActions = actions };
    }

    public static bool TryParseValidated(string? raw, out AiReply reply)
    {
        reply = Parse(raw ?? string.Empty);
        if (string.IsNullOrWhiteSpace(reply.Text))
            return false;

        var trimmed = (raw ?? string.Empty).TrimStart();
        var looksStructured = trimmed.StartsWith("{", StringComparison.Ordinal) ||
                              trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase);
        if (!looksStructured)
            return true;

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<AiReply>(trimmed[start..(end + 1)], JsonOptions);
            return !string.IsNullOrWhiteSpace(parsed?.Text);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string StripInlineActions(string? text, out string action, out string[] actions)
    {
        var input = text ?? string.Empty;
        if (!InlineActionPattern.IsMatch(input))
        {
            action = "None";
            actions = Array.Empty<string>();
            return input.Trim();
        }

        var cleanLines = new List<string>();
        var lineActions = new List<string>();
        var pendingAction = "None";
        foreach (var line in input.Replace("\r\n", "\n").Split('\n'))
        {
            var matches = InlineActionPattern.Matches(line);
            var cleaned = InlineActionPattern.Replace(line, string.Empty).Trim();
            var foundAction = matches.Count == 0 ? "None" : matches[0].Groups[1].Value;
            if (cleaned.Length == 0)
            {
                if (matches.Count == 0)
                    cleanLines.Add(string.Empty);
                else if (lineActions.Count > 0 && lineActions[^1] == "None")
                    lineActions[^1] = foundAction;
                else
                    pendingAction = foundAction;
                continue;
            }

            cleanLines.Add(cleaned);
            lineActions.Add(pendingAction == "None" ? foundAction : pendingAction);
            pendingAction = "None";
        }

        action = lineActions.FirstOrDefault(candidate => candidate != "None") ?? "None";
        actions = lineActions.ToArray();
        return string.Join("\n", cleanLines).Trim();
    }

    public static void SelfTest()
    {
        var json = Parse("```json\n{\"text\":\"你好\",\"action\":\"Greet\",\"speech\":\"こんにちは\"}\n```");
        var inline = Parse("hello\n\uFF08\u52D5\u4F5C\uFF1ATiltHead\uFF09\nworld\n(action: Stretch)");
        if (json.Text != "你好" || json.Action != "Greet" || json.Speech != "こんにちは" || Parse("純文字").Action != "None" ||
            inline.Action != "TiltHead" || inline.InlineActions.Length != 2 ||
            inline.InlineActions[0] != "TiltHead" || inline.InlineActions[1] != "Stretch" ||
            inline.Text.Contains("TiltHead") || inline.Text.Contains("Stretch"))
            throw new InvalidOperationException("AI reply parser self-test failed");
    }
}

public static class AiClient
{
    private static readonly HttpClient Http = new();

    public static async Task<AiReply> SendAsync(
        ProviderKind provider,
        string baseUrl,
        string apiKey,
        string model,
        string systemPrompt,
        IReadOnlyList<ChatMessage> history,
        string userText,
        int timeoutSeconds,
        CancellationToken lifetime,
        Action<string>? log = null,
        string requiredSpeechLanguage = "",
        Action<AiRequestProgress>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Base URL is empty");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        AiReply? replyNeedingSpeech = null;
        var scaffoldingRetry = false;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var speechFallback = replyNeedingSpeech != null;
            progress?.Invoke(new AiRequestProgress(
                speechFallback ? AiRequestStage.SpeechFallback : attempt == 0 ? AiRequestStage.Sending : AiRequestStage.Retrying,
                speechFallback ? $"Preparing {requiredSpeechLanguage} speech" : attempt == 0 ? "Sending request" : "Retrying response",
                attempt + 1));
            var prompt = speechFallback
                ? $"Your previous response omitted speech. Translate the user's text into natural spoken {requiredSpeechLanguage}. Return only the translation, without JSON, labels, or explanation."
                : scaffoldingRetry
                    ? $"{systemPrompt}\nThe previous response was empty, malformed, or provider scaffolding. Follow the existing JSON response contract exactly and include a non-empty user-facing text field; do not return vars or metadata."
                : systemPrompt;
            var requestHistory = speechFallback
                ? Array.Empty<ChatMessage>()
                : history.Where(message =>
                    !string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase) ||
                    !IsProviderScaffolding(message.Content)).ToArray();
            var requestText = speechFallback ? replyNeedingSpeech!.Text : userText;
            using var request = provider == ProviderKind.Anthropic
                ? BuildAnthropicRequest(baseUrl, apiKey, model, prompt, requestHistory, requestText)
                : BuildOpenAiRequest(provider, baseUrl, apiKey, model, prompt, requestHistory, requestText,
                    speechFallback ? string.Empty : requiredSpeechLanguage, scaffoldingRetry);
            var requestBody = await request.Content!.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            log?.Invoke($"AI REQUEST\nMode: {(speechFallback ? "Speech fallback" : "Reply")}\nAttempt: {attempt + 1}\nProvider: {provider}\nModel: {model}\nEndpoint: {request.RequestUri}\nPayload:\n{requestBody}");
            HttpResponseMessage response;
            try
            {
                response = await Http.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token).ConfigureAwait(false);
            }
            catch (HttpRequestException exception) when (speechFallback)
            {
                log?.Invoke($"AI SPEECH FALLBACK\nSpeech connection failed ({exception.GetType().Name}); text chat continues without TTS.");
                return replyNeedingSpeech!;
            }
            catch (OperationCanceledException) when (speechFallback && !lifetime.IsCancellationRequested)
            {
                log?.Invoke("AI SPEECH FALLBACK\nSpeech request timed out; text chat continues without TTS.");
                return replyNeedingSpeech!;
            }
            catch (HttpRequestException exception) when (attempt == 0)
            {
                log?.Invoke($"AI RETRY\nTemporary connection failure ({exception.GetType().Name}); retrying once.");
                progress?.Invoke(new AiRequestProgress(AiRequestStage.Retrying, "Connection interrupted; retrying", attempt + 2));
                await Task.Delay(250, timeout.Token).ConfigureAwait(false);
                continue;
            }
            using var responseLifetime = response;
            var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            var responseLog = speechFallback ? "AI SPEECH RESPONSE" : "AI RESPONSE";
            log?.Invoke($"{responseLog}\nStatus: {(int)response.StatusCode} {response.ReasonPhrase}\nBody: omitted from log to keep private response fields out of diagnostics");

            if (!response.IsSuccessStatusCode)
            {
                var errorSummary = SafeProviderErrorSummary(body);
                if (speechFallback)
                {
                    log?.Invoke($"AI SPEECH FALLBACK\nSpeech request failed with HTTP {(int)response.StatusCode}; {errorSummary}; text chat continues without TTS.");
                    return replyNeedingSpeech!;
                }
                if (attempt == 0 && IsRetryableStatusCode((int)response.StatusCode))
                {
                    log?.Invoke($"AI RETRY\nTemporary API status {(int)response.StatusCode}; retrying once.");
                    progress?.Invoke(new AiRequestProgress(AiRequestStage.Retrying, "Provider temporarily unavailable; retrying", attempt + 2));
                    await Task.Delay(RetryDelay(response), timeout.Token).ConfigureAwait(false);
                    continue;
                }
                throw new InvalidOperationException($"API {(int)response.StatusCode}: {errorSummary}");
            }

            string? finishReason = null;
            string? rawReply;
            try
            {
                rawReply = provider == ProviderKind.Anthropic
                    ? ReadAnthropicText(body)
                    : ReadOpenAiText(body, out finishReason);
            }
            catch (Exception exception) when (speechFallback && IsResponseShapeException(exception))
            {
                log?.Invoke("AI SPEECH FALLBACK\nProvider returned malformed speech data; text chat continues without TTS.");
                return replyNeedingSpeech!;
            }
            catch (Exception exception) when (attempt == 0 && IsResponseShapeException(exception))
            {
                scaffoldingRetry = true;
                log?.Invoke($"AI RETRY\nProvider returned malformed response data ({exception.GetType().Name}); retrying once.");
                continue;
            }
            progress?.Invoke(new AiRequestProgress(AiRequestStage.Validating, "Validating response", attempt + 1));
            if (IsProviderScaffolding(rawReply))
            {
                if (speechFallback)
                {
                    log?.Invoke("AI SPEECH FALLBACK\nProvider returned scaffolding instead of speech; text chat continues without TTS.");
                    return replyNeedingSpeech!;
                }
                if (attempt == 0)
                {
                    scaffoldingRetry = true;
                    log?.Invoke("AI RETRY\nProvider returned scaffolding without user-facing content; retrying once.");
                    continue;
                }
                throw new InvalidOperationException("API returned provider scaffolding without user-facing content");
            }
            if (!AiReply.TryParseValidated(rawReply, out var validatedReply))
            {
                if (speechFallback)
                {
                    log?.Invoke("AI SPEECH FALLBACK\nProvider returned no speech; text chat continues without TTS.");
                    return replyNeedingSpeech!;
                }
                if (attempt == 0)
                {
                    scaffoldingRetry = true;
                    log?.Invoke($"AI RETRY\nProvider returned empty or malformed content (finish_reason: {finishReason ?? "unknown"}); retrying once.");
                    continue;
                }
                throw new InvalidOperationException(finishReason == "length"
                    ? "AI 回覆被截斷，請改用其他模型。"
                    : "API returned an empty or malformed response");
            }

            if (speechFallback)
            {
                var translated = validatedReply;
                var speech = string.IsNullOrWhiteSpace(translated.Speech) ? translated.Text : translated.Speech;
                if (string.IsNullOrWhiteSpace(speech))
                {
                    log?.Invoke("AI SPEECH FALLBACK\nProvider returned no speech; text chat continues without TTS.");
                    return replyNeedingSpeech!;
                }
                var result = replyNeedingSpeech! with { Speech = speech.Trim() };
                log?.Invoke($"AI SPEECH PARSED\nSpeech: {result.Speech}");
                return result;
            }

            var reply = validatedReply;
            log?.Invoke($"AI PARSED\nText: {reply.Text}\nSpeech: {reply.Speech}\nAction: {reply.Action}\nClothing: {reply.Clothing}\nCommand: {reply.Command}\nArgument: {reply.Argument}\nMemory: {reply.Memory}");
            if (!string.IsNullOrWhiteSpace(requiredSpeechLanguage) && string.IsNullOrWhiteSpace(reply.Speech))
            {
                replyNeedingSpeech = reply;
                log?.Invoke($"AI SPEECH FALLBACK\nProvider omitted {requiredSpeechLanguage} speech; requesting translation only.");
                continue;
            }
            return reply;
        }

        throw new InvalidOperationException("AI request failed");
    }

    public static bool IsRetryableStatusCode(int statusCode) =>
        statusCode is 408 or 425 or 429 || statusCode is >= 500 and <= 599;

    public static string SafeProviderErrorSummary(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "provider returned no error details";

        string? code = null;
        string? message = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var error = root.TryGetProperty("error", out var nested) && nested.ValueKind == JsonValueKind.Object
                ? nested
                : root;
            code = ScalarErrorValue(error, "code");
            message = ScalarErrorValue(error, "message") ?? ScalarErrorValue(root, "message");
        }
        catch (JsonException)
        {
        }

        var summary = string.Join("; ", new[]
        {
            string.IsNullOrWhiteSpace(code) ? null : $"code={code}",
            string.IsNullOrWhiteSpace(message) ? null : $"message={message}",
        }.Where(value => value != null));
        return string.IsNullOrWhiteSpace(summary)
            ? "provider returned an unstructured error"
            : Trim(SanitizeErrorText(summary), 240);
    }

    private static string? ScalarErrorValue(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
            _ => null,
        };
    }

    private static string SanitizeErrorText(string value)
    {
        var compact = new string(value.Select(character => char.IsControl(character) ? ' ' : character).ToArray());
        return Regex.Replace(compact, @"(?i)(bearer\s+|sk-[A-Za-z0-9_-]{8,})\S*", "[redacted]").Trim();
    }

    private static bool IsResponseShapeException(Exception exception) =>
        exception is JsonException or KeyNotFoundException or InvalidOperationException;

    private static TimeSpan RetryDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        var seconds = retryAfter?.Delta?.TotalSeconds ??
                      (retryAfter?.Date is { } date ? (date - DateTimeOffset.UtcNow).TotalSeconds : 0.25);
        return TimeSpan.FromSeconds(Math.Clamp(seconds, 0.1, 30));
    }

    private static bool IsProviderScaffolding(string? rawReply)
    {
        if (string.IsNullOrWhiteSpace(rawReply))
            return false;

        var trimmed = rawReply.Trim();
        const string prefix = "vars:";
        if (!trimmed.StartsWith(prefix, StringComparison.Ordinal) ||
            trimmed.Length == prefix.Length)
            return false;

        try
        {
            using var document = JsonDocument.Parse(trimmed[prefix.Length..].Trim());
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if ((property.Name.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                     property.Name.Equals("speech", StringComparison.OrdinalIgnoreCase)) &&
                    property.Value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(property.Value.GetString()))
                    return false;
            }
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static async Task<string[]> ListModelsAsync(
        string baseUrl,
        string apiKey,
        int timeoutSeconds,
        CancellationToken lifetime)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Base URL is empty");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        using var request = new HttpRequestMessage(HttpMethod.Get, Endpoint(baseUrl, "models"));
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"API {(int)response.StatusCode}: {SafeProviderErrorSummary(body)}");

        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("data").EnumerateArray()
            .Select(item => item.TryGetProperty("id", out var id) ? id.GetString() : null)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static HttpRequestMessage BuildOpenAiRequest(
        ProviderKind provider,
        string baseUrl,
        string apiKey,
        string model,
        string systemPrompt,
        IReadOnlyList<ChatMessage> history,
        string userText,
        string requiredSpeechLanguage,
        bool strictRetry)
    {
        if (model.StartsWith("nvidia/nemotron-nano-9b-v2", StringComparison.OrdinalIgnoreCase))
            systemPrompt = "/no_think\n" + systemPrompt;
        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        messages.AddRange(history.Select(message => (object)new { role = message.Role, content = message.Content }));
        messages.Add(new { role = "user", content = userText });

        var payload = new Dictionary<string, object>
        {
            ["model"] = model,
            ["messages"] = messages,
            ["temperature"] = strictRetry ? 0.2 : 0.45,
            ["max_tokens"] = 512,
        };
        if (SupportsJsonObject(provider, model))
            payload["response_format"] = new { type = "json_object" };
        if (provider == ProviderKind.OpenRouter &&
            !model.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase))
            payload["reasoning"] = new { effort = "none", exclude = true };
        if (provider == ProviderKind.OpenRouter &&
            (model.Equals("openrouter/auto", StringComparison.OrdinalIgnoreCase) ||
             model.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase)))
            payload["provider"] = new { require_parameters = true };
        if (provider == ProviderKind.OpenRouter &&
            model.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(requiredSpeechLanguage))
        {
            payload["response_format"] = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "lilith_reply",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            text = new { type = "string" },
                            speech = new
                            {
                                type = "string",
                                minLength = 1,
                                description = $"The same meaning as text in natural spoken {requiredSpeechLanguage}",
                            },
                            action = new { type = "string" },
                            clothing = new { type = "string" },
                            command = new { type = "string" },
                            argument = new { type = "string" },
                            memory = new { type = "string" },
                        },
                        required = new[] { "text", "speech", "action", "clothing", "command", "argument", "memory" },
                        additionalProperties = false,
                    },
                },
            };
            payload["provider"] = new { require_parameters = true };
        }

        var request = JsonRequest(Endpoint(baseUrl, "chat/completions"), payload);
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        if (provider == ProviderKind.Gemini)
            request.Headers.TryAddWithoutValidation("x-goog-api-client", "lilith-ai/0.13.1");
        if (provider == ProviderKind.OpenRouter)
            request.Headers.TryAddWithoutValidation("X-OpenRouter-Title", "Lilith AI");
        return request;
    }

    private static bool SupportsJsonObject(ProviderKind provider, string model) => provider switch
    {
        ProviderKind.OpenAI or ProviderKind.Gemini or ProviderKind.XAI or ProviderKind.DeepSeek or ProviderKind.Mistral => true,
        ProviderKind.OpenRouter => model.Equals("openrouter/auto", StringComparison.OrdinalIgnoreCase) ||
                                   model.Equals("openrouter/free", StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private static HttpRequestMessage BuildAnthropicRequest(
        string baseUrl,
        string apiKey,
        string model,
        string systemPrompt,
        IReadOnlyList<ChatMessage> history,
        string userText)
    {
        var messages = history.Select(message => (object)new { role = message.Role, content = message.Content }).ToList();
        messages.Add(new { role = "user", content = userText });

        var request = JsonRequest(Endpoint(baseUrl, "messages"), new
        {
            model,
            system = systemPrompt,
            messages,
            max_tokens = 512,
            temperature = 0.8,
        });
        request.Headers.TryAddWithoutValidation("x-api-key", apiKey.Trim());
        request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        return request;
    }

    private static HttpRequestMessage JsonRequest(string endpoint, object body) => new(HttpMethod.Post, endpoint)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
    };

    private static string Endpoint(string baseUrl, string path)
    {
        var clean = baseUrl.Trim().TrimEnd('/');
        return clean.EndsWith(path, StringComparison.OrdinalIgnoreCase) ? clean : $"{clean}/{path}";
    }

    private static string? ReadOpenAiText(string body, out string? finishReason)
    {
        using var json = JsonDocument.Parse(body);
        var choice = json.RootElement.GetProperty("choices")[0];
        finishReason = choice.TryGetProperty("finish_reason", out var reason) ? reason.GetString() : null;
        var content = choice.GetProperty("message").GetProperty("content");
        if (content.ValueKind == JsonValueKind.String)
            return content.GetString();
        if (content.ValueKind != JsonValueKind.Array)
            return null;

        var text = new StringBuilder();
        foreach (var part in content.EnumerateArray())
        {
            if (part.ValueKind != JsonValueKind.Object || !part.TryGetProperty("text", out var value) ||
                value.ValueKind != JsonValueKind.String)
                continue;
            if (text.Length > 0)
                text.AppendLine();
            text.Append(value.GetString());
        }
        return text.Length == 0 ? null : text.ToString();
    }

    private static string ReadAnthropicText(string body)
    {
        using var json = JsonDocument.Parse(body);
        foreach (var block in json.RootElement.GetProperty("content").EnumerateArray())
            if (block.TryGetProperty("type", out var type) && type.GetString() == "text")
                return block.GetProperty("text").GetString() ?? string.Empty;
        throw new InvalidOperationException("Anthropic returned no text block");
    }

    private static string Trim(string value, int length) => value.Length <= length ? value : value[..length];
}
