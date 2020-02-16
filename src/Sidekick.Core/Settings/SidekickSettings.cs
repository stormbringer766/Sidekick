using System.IO;
using System.Text.Json;

namespace Sidekick.Core.Settings
{
    public class SidekickSettings
    {
        public SidekickSettings()
        {
            Language_UI = "en";
            Language_Parser = "English";
            LeagueId = string.Empty;
            Character_Name = string.Empty;
            Wiki_Preferred = WikiSetting.PoeWiki;
            RetainClipboard = true;
            CloseOverlayWithMouse = true;
            EnableCtrlScroll = true;
            Key_CloseWindow = "Space";
            Key_CheckPrices = "Ctrl+D";
            Key_GoToHideout = "F5";
            Key_OpenWiki = "Alt+W";
            Key_FindItems = "Ctrl+F";
            Key_LeaveParty = "F4";
            Key_OpenSearch = "Alt+Q";
            Key_OpenLeagueOverview = "F6";
            Key_ReplyToLatestWhisper = "Ctrl+Shift+R";
        }

        public const string FileName = "Sidekick_settings.json";

        public string Language_UI { get; set; }

        public string Language_Parser { get; set; }

        public string LeagueId { get; set; }

        public WikiSetting Wiki_Preferred { get; set; }

        public string Character_Name { get; set; }

        public bool RetainClipboard { get; set; }

        public bool CloseOverlayWithMouse { get; set; }

        public bool EnableCtrlScroll { get; set; }

        public string Key_CloseWindow { get; set; }

        public string Key_CheckPrices { get; set; }

        public string Key_GoToHideout { get; set; }

        public string Key_OpenWiki { get; set; }

        public string Key_FindItems { get; set; }

        public string Key_LeaveParty { get; set; }

        public string Key_OpenSearch { get; set; }

        public string Key_OpenLeagueOverview { get; set; }

        public string Key_ReplyToLatestWhisper { get; set; }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this);
            var defaults = JsonSerializer.Serialize(new SidekickSettings());
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), FileName);

            // Backup old settings
            if (File.Exists(filePath))
            {
                File.Copy(filePath, filePath.Replace(".json", "_old.json"), true);
            }

            // TODO: Refactor this to use the new using syntax in Csharp 8
            using (var fileStream = File.Create(filePath))
            {
                using (var writer = new Utf8JsonWriter(fileStream, options: new JsonWriterOptions
                {
                    Indented = true
                }))
                {
                    using (var document = JsonDocument.Parse(json, new JsonDocumentOptions
                    {
                        CommentHandling = JsonCommentHandling.Skip
                    }))
                    {
                        using (var defaultsDocument = JsonDocument.Parse(defaults, new JsonDocumentOptions
                        {
                            CommentHandling = JsonCommentHandling.Skip
                        }))
                        {
                            var root = document.RootElement;
                            var defaultsRoot = defaultsDocument.RootElement;

                            if (root.ValueKind == JsonValueKind.Object)
                            {
                                writer.WriteStartObject();
                            }
                            else
                            {
                                return;
                            }

                            foreach (var property in root.EnumerateObject())
                            {
                                if (defaultsRoot.GetProperty(property.Name).ToString() == property.Value.ToString())
                                {
                                    continue;
                                }

                                property.WriteTo(writer);
                            }

                            writer.WriteEndObject();
                            writer.Flush();
                        }
                    }

                    if (writer.BytesCommitted == 0)
                    {
                        File.Delete(filePath);
                    }
                }
            }
        }
    }
}
