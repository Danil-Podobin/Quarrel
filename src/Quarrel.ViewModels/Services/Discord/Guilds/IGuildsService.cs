﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordAPI.Models;
using Quarrel.ViewModels.Models.Bindables;

namespace Quarrel.ViewModels.Services.Discord.Guilds
{
    public interface IGuildsService
    {
        ConcurrentDictionary<string, GuildSetting> GuildSettings { get; }
        IDictionary<string, BindableGuild> AllGuilds { get; }
        BindableGuild CurrentGuild { get; }
        BindableGuildMember GetGuildMember(string memberId, string guildId);
        IReadOnlyDictionary<string, BindableGuildMember> GetAndRequestGuildMembers(IEnumerable<string> memberIds, string guildId);
    }
}
