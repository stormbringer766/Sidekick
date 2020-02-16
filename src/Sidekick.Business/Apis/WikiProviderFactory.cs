using Microsoft.Extensions.Options;
using Sidekick.Business.Apis.PoeDb;
using Sidekick.Business.Apis.PoeWiki;
using Sidekick.Business.Parsers.Models;
using Sidekick.Core.Settings;

namespace Sidekick.Business.Apis
{
    public class WikiProviderFactory: IWikiProvider
    {
        private readonly IOptionsMonitor<SidekickSettings> settings;
        private readonly IPoeWikiClient poeWikiClient;
        private readonly IPoeDbClient poeDbClient;

        public WikiProviderFactory(IOptionsMonitor<SidekickSettings> settings, IPoeWikiClient poeWikiClient, IPoeDbClient poeDbClient)
        {
            this.settings = settings;
            this.poeWikiClient = poeWikiClient;
            this.poeDbClient = poeDbClient;
        }

        public void Open(Item item)
        {
            GetCurrentProvider().Open(item);
        }

        private IWikiProvider GetCurrentProvider()
        {
            return settings.CurrentValue.Wiki_Preferred == WikiSetting.PoeDb
                ? (IWikiProvider) poeDbClient
                : poeWikiClient;
        }
    }
}
