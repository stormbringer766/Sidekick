using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Sidekick.Business.Leagues;
using Sidekick.Core.Natives;
using Sidekick.Core.Settings;
using Sidekick.Localization;
using Sidekick.UI.Helpers;

namespace Sidekick.UI.Settings
{
    public class SettingsViewModel : ISettingsViewModel, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly IUILanguageProvider uiLanguageProvider;
        private readonly IOptionsMonitor<SidekickSettings> sidekickSettings;
        private readonly INativeKeyboard nativeKeyboard;

        public SettingsViewModel(IUILanguageProvider uiLanguageProvider,
            IOptionsMonitor<SidekickSettings> sidekickSettings,
            INativeKeyboard nativeKeyboard,
            ILeagueService leagueService)
        {
            this.uiLanguageProvider = uiLanguageProvider;
            this.sidekickSettings = sidekickSettings;
            this.nativeKeyboard = nativeKeyboard;

            Settings = new SidekickSettings();
            AssignValues(sidekickSettings.CurrentValue, Settings);

            Keybinds.Clear();
            Settings.GetType()
                .GetProperties()
                .Where(x => x.Name.StartsWith("Key"))
                .ToList()
                .ForEach(x => Keybinds.Add(x.Name, x.GetValue(Settings).ToString()));

            WikiOptions.Add("POE Wiki", WikiSetting.PoeWiki.ToString());
            WikiOptions.Add("POE Db", WikiSetting.PoeDb.ToString());

            leagueService.Leagues.ForEach(x => LeagueOptions.Add(x.Id, x.Text));
            uiLanguageProvider.AvailableLanguages.ForEach(x => UILanguageOptions.Add(x.NativeName.First().ToString().ToUpper() + x.NativeName.Substring(1), x.Name));

            nativeKeyboard.OnKeyDown += NativeKeyboard_OnKeyDown;
        }

        public SidekickSettings Settings { get; private set; }

        public ObservableDictionary<string, string> Keybinds { get; private set; } = new ObservableDictionary<string, string>();

        public string CurrentKey { get; set; }

        public ObservableDictionary<string, string> WikiOptions { get; private set; } = new ObservableDictionary<string, string>();

        public ObservableDictionary<string, string> LeagueOptions { get; private set; } = new ObservableDictionary<string, string>();

        public ObservableDictionary<string, string> UILanguageOptions { get; private set; } = new ObservableDictionary<string, string>();

        public void Save()
        {
            var keybindProperties = Settings.GetType().GetProperties();

            foreach (var keybind in Keybinds)
            {
                keybindProperties.First(x => x.Name == keybind.Key).SetValue(Settings, keybind.Value);
            };

            AssignValues(Settings, sidekickSettings.CurrentValue);
            uiLanguageProvider.SetLanguage(Settings.Language_UI);
            sidekickSettings.CurrentValue.Save();
        }

        public bool IsKeybindUsed(string keybind, string ignoreKey = null)
        {
            return Keybinds.ToCollection().Any(x => x.Value == keybind && x.Key != ignoreKey);
        }

        private static void AssignValues(SidekickSettings src, SidekickSettings dest)
        {
            // iterates through src Settings (properties) and copies them to dest settings (properties)
            // If there ever comes a time, where some properties do not have to be copied, we can add attributes to exclude them
            src.GetType().GetProperties().ToList().ForEach(x => x.SetValue(dest, x.GetValue(src)));
        }

        private bool NativeKeyboard_OnKeyDown(string input)
        {
            if (string.IsNullOrEmpty(CurrentKey))
            {
                return false;
            }

            if (input == "Escape")
            {
                CurrentKey = null;
                return true;
            }

            if (!IsKeybindUsed(input, CurrentKey))
            {
                var currentKeybind = Keybinds.ToCollection().FirstOrDefault(x => x.Key == CurrentKey);
                currentKeybind.Value = input;
            }

            CurrentKey = null;
            return true;
        }

        public void Clear(string key)
        {
            Keybinds.ToCollection().FirstOrDefault(x => x.Key == key).Value = string.Empty;
        }

        public void Dispose()
        {
            nativeKeyboard.OnKeyDown -= NativeKeyboard_OnKeyDown;
        }
    }
}
