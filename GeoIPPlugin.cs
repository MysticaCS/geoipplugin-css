using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeoIPPlugin
{
    public class GeoIPPlugin : BasePlugin
    {
        public override string ModuleName => "GeoIP Announcer (API)";
        public override string ModuleVersion => "4.0.0";
        public override string ModuleAuthor => "Mystica";

        private static readonly HttpClient httpClient = new HttpClient();

        public override void Load(bool hotReload)
        {
            Server.PrintToConsole("[GeoIP]Using ip-api.com");
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull ev, GameEventInfo info)
        {
            var player = ev.Userid;
            if (player == null || player.IsBot)
                return HookResult.Continue;

            try
            {
                var ip = player.IpAddress;
                if (string.IsNullOrEmpty(ip))
                    return HookResult.Continue;

                if (ip.Contains(":"))
                    ip = ip.Split(':')[0];

                string steamId2 = ToSteamID2(player.SteamID);
                string name = player.PlayerName;

                _ = Task.Run(async () =>
                {
                    string country = await GetCountryFromAPI(ip);
                    Server.NextFrame(() =>
                    {
                        string msg = Localizer["GeoIP_ConnectMsg", name, steamId2, country];
                        Server.PrintToChatAll(msg);
                        Server.PrintToConsole($"[GeoIP] {name} ({steamId2}) from {country}");
                    });
                });
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[GeoIP] Error: {ex.Message}");
            }

            return HookResult.Continue;
        }

        private async Task<string> GetCountryFromAPI(string ip)
        {
            try
            {
                string url = $"http://ip-api.com/json/{ip}?fields=country";
                var response = await httpClient.GetStringAsync(url);

                var json = Newtonsoft.Json.Linq.JObject.Parse(response);
                return json["country"]?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        private string ToSteamID2(ulong steamId64)
        {
            uint accountId = (uint)(steamId64 & 0xFFFFFFFF);
            uint authServer = accountId % 2;
            uint authId = accountId / 2;
            return $"STEAM_1:{authServer}:{authId}";
        }
    }
}
