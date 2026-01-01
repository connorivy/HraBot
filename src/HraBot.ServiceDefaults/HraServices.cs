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
    public const string MIGRATION_SERVICE = "migrationService";
    public const string MARK_IT_DOWN = "markitdown";
    public const string ALL_SERVICES = nameof(ALL_SERVICES);
    public const string PG_ADMIN = "pgadmin";
    public const string db_hraBot = "hraBotDb";
    public const string geminiKey = "gemini";
}

public static class AppOptions
{
    public const string MockChatClient_bool = nameof(MockChatClient_bool);
    public const string ENV_NAME_IsEphemeralDb = nameof(ENV_NAME_IsEphemeralDb);
    public static bool IsEphemeralDb
    {
        get => IsEnvVarTrue(ENV_NAME_IsEphemeralDb);
        set => Environment.SetEnvironmentVariable(ENV_NAME_IsEphemeralDb, value.ToString());
    }

    private static bool IsEnvVarTrue(string envVar) =>
        Environment.GetEnvironmentVariable(envVar) is string mockChatClientString
        && bool.TryParse(mockChatClientString, out var mockChatClient)
        && mockChatClient;
}
