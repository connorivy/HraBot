using System;
using System.Reflection.Metadata;

namespace HraBot.ServiceDefaults;

public static class AppServices
{
    // public const string openai = "openai";
    public const string openai = "openai";

    public const string openai2 = "openai2";

    public const string openai3 = "openai3";

    public const string API = "api";
    public const string WEB = "web";

    // public const string vectorDb = "qdrantLocal";
    public const string vectorDb = "qdrantCloud";
    public const string postgres = "postgres";
    public const string db_hraBot = "hraBotDb";
    public const string geminiKey = "gemini";
}

public static class AppOptions
{
    public const string MockChatClient_bool = nameof(MockChatClient_bool);
}
