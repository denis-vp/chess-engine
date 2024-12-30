using System.Text.Json;

namespace chess_engine
{
    public static class Settings
    {
        public static int ScreenWidth { get; private set; } = 800;
        public static int ScreenHeight { get; private set; } = 800;
        public static bool IsPlayerWhite { get; private set; } = true;
        public static int BotThinkTimeSeconds { get; private set; } = 5;
        public static int MaxBookPly { get; private set; } = 16;
        public static bool InvertPerspective { get; private set; } = false;
        public static bool EngineVsEngine { get; private set; } = false;
        public static bool PrintSearch { get; private set; } = false;

        private static readonly string settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        static Settings()
        {
            Load();
        }

        private static void Load()
        {
            if (File.Exists(settingsFilePath))
            {
                string json = File.ReadAllText(settingsFilePath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                ScreenWidth = settings.ScreenWidth;
                ScreenHeight = settings.ScreenHeight;
                IsPlayerWhite = settings.IsPlayerWhite;
                BotThinkTimeSeconds = settings.BotThinkTimeSeconds;
                MaxBookPly = settings.MaxBookPly;
                InvertPerspective = settings.InvertPerspective;
                EngineVsEngine = settings.EngineVsEngine;
                PrintSearch = settings.PrintSearch;
            }
        }

        private class SettingsData
        {
            public int ScreenWidth { get; set; } = 800;
            public int ScreenHeight { get; set; } = 800;
            public bool IsPlayerWhite { get; set; } = true;
            public int BotThinkTimeSeconds { get; set; } = 5;
            public int MaxBookPly { get; set; } = 16;
            public bool InvertPerspective { get; set; } = false;
            public bool EngineVsEngine { get; set; } = false;
            public bool PrintSearch { get; set; } = false;
        }
    }
}
