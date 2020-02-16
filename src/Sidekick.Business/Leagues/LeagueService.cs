using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Sidekick.Business.Apis.Poe;
using Sidekick.Business.Apis.Poe.Models;
using Sidekick.Core.Initialization;
using Sidekick.Core.Loggers;
using Sidekick.Core.Settings;

namespace Sidekick.Business.Leagues
{
    public class LeagueService : ILeagueService, IOnInit, IDisposable
    {
        private readonly IPoeApiClient poeApiClient;
        private readonly IOptionsMonitor<SidekickSettings> configuration;

        public List<League> Leagues { get; private set; }

        public LeagueService(IPoeApiClient poeApiClient,
            IOptionsMonitor<SidekickSettings> configuration)
        {
            this.poeApiClient = poeApiClient;
            this.configuration = configuration;
        }

        public async Task OnInit()
        {
            Leagues = null;
            Leagues = await poeApiClient.Fetch<League>();
            if (string.IsNullOrEmpty(configuration.CurrentValue.LeagueId) ||
                !Leagues.Exists(x => x.Id == configuration.CurrentValue.LeagueId))
            {
                configuration.CurrentValue.LeagueId = Leagues.First().Id;
                configuration.CurrentValue.Save();
            }
        }

        public void Dispose()
        {
            Leagues = null;
        }
    }
}
