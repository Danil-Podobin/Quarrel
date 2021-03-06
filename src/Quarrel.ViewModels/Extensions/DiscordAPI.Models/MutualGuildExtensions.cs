﻿using GalaSoft.MvvmLight.Ioc;
using Quarrel.ViewModels.Models.Bindables;
using Quarrel.ViewModels.Services.Discord.Guilds;

namespace DiscordAPI.Models
{
    internal static class MutualGuildExtentions
    {
        public static BindableGuild Guild(this MutualGuild mg) => SimpleIoc.Default.GetInstance<IGuildsService>().AllGuilds.TryGetValue(mg.Id, out var value) ? value : null;

        public static string GetName(this MutualGuild mg) => Guild(mg)?.Model?.Name;

        public static string GetIconUrl(this MutualGuild mg)
        {
            return Guild(mg)?.IconUrl;
        }
    }
}