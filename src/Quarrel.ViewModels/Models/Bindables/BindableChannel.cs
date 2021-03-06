﻿// Special thanks to Sergio Pedri for the basis of this design

using DiscordAPI.Models;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using JetBrains.Annotations;
using Quarrel.ViewModels.Messages.Gateway;
using Quarrel.ViewModels.Messages.Navigation;
using Quarrel.ViewModels.Messages.Services.Settings;
using Quarrel.ViewModels.Models.Bindables.Abstract;
using Quarrel.ViewModels.Services.Clipboard;
using Quarrel.ViewModels.Services.Discord.Channels;
using Quarrel.ViewModels.Services.Discord.CurrentUser;
using Quarrel.ViewModels.Services.Discord.Friends;
using Quarrel.ViewModels.Services.Discord.Guilds;
using Quarrel.ViewModels.Services.Discord.Rest;
using Quarrel.ViewModels.Services.DispatcherHelper;
using Quarrel.ViewModels.Services.Navigation;
using Quarrel.ViewModels.Services.Settings;
using Quarrel.ViewModels.Services.Settings.Enums;
using Quarrel.ViewModels.Services.Voice;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Quarrel.ViewModels.Models.Bindables
{
    public class BindableChannel : BindableModelBase<Channel>
    {
        #region Constructors

        /// <summary>
        /// Creates a Channel object that can be bound to the UI
        /// </summary>
        /// <param name="model">API Channel Model</param>
        /// <param name="guildId">Id of Channel's guild</param>
        /// <param name="states">List of VoiceStates for users in a voice channel</param>
        public BindableChannel([NotNull] Channel model, [NotNull] string guildId, [CanBeNull] IEnumerable<VoiceState> states = null) : base(model)
        {
            this.guildId = guildId;

            #region Messenger

            MessengerInstance.Register<GatewayVoiceStateUpdateMessage>(this, e =>
            {
                DispatcherHelper.CheckBeginInvokeOnUi(() =>
                {
                    if (e.VoiceState.ChannelId == Model.Id)
                    {
                        // User joined this Voice Channel
                        if (!ConnectedUsers.ContainsKey(e.VoiceState.UserId))
                        {
                            ConnectedUsers.Add(e.VoiceState.UserId, new BindableVoiceUser(e.VoiceState));
                        }
                    }
                    else if (ConnectedUsers.ContainsKey(e.VoiceState.UserId))
                    {
                        // User joined a different Voice Channel, so they must have left this one
                        ConnectedUsers.Remove(e.VoiceState.UserId);
                    }
                });
            });

            MessengerInstance.Register<GatewayUserGuildSettingsUpdatedMessage>(this, m =>
            {
                if ((m.Settings.GuildId ?? "DM") == GuildId)
                    DispatcherHelper.CheckBeginInvokeOnUi(() =>
                    {
                        // Updated channel settings
                        ChannelOverride channelOverride;
                        if(_ChannelsService.ChannelSettings.TryGetValue(Model.Id, out channelOverride))
                            Muted = channelOverride.Muted;
                    });
            });

            MessengerInstance.Register<GatewayMessageAckMessage>(this, m =>
            {
                if (Model.Id == m.ChannelId)
                {
                    UpdateLRMID(m.MessageId);
                    DispatcherHelper.CheckBeginInvokeOnUi(() =>
                    {
                        RaisePropertyChanged(nameof(MentionCount));
                        RaisePropertyChanged(nameof(IsUnread));
                        RaisePropertyChanged(nameof(ShowUnread));
                    });
                }
            });

            MessengerInstance.Register<ChannelNavigateMessage>(this, m =>
            {
                DispatcherHelper.CheckBeginInvokeOnUi(() =>
                {
                    Selected = m.Channel == this;
                });
            });

            MessengerInstance.Register<SettingChangedMessage<bool>>(this, m =>
            {
                if (m.Key == SettingKeys.ShowNoPermssions)
                {
                    RaisePropertyChanged(nameof(Hidden));
                }
            });

            MessengerInstance.Register<SettingChangedMessage<CollapseOverride>>(this, m =>
            {
                if (m.Key == SettingKeys.CollapseOverride)
                {
                    RaisePropertyChanged(nameof(Hidden));
                }
            });

            #endregion

            if (states != null)
            {
                foreach (var state in states)
                {
                    if (state.ChannelId == Model.Id)
                    {
                        ConnectedUsers.Add(state.UserId, new BindableVoiceUser(state));
                    }
                }
            }
        }

        #endregion

        #region Properties

        #region Services

        private ICurrentUserService _CurrentUsersService { get; } = SimpleIoc.Default.GetInstance<ICurrentUserService>();
        private IChannelsService _ChannelsService { get; } = SimpleIoc.Default.GetInstance<IChannelsService>();
        private IDiscordService _DiscordService { get; } = SimpleIoc.Default.GetInstance<IDiscordService>();
        private ISettingsService _SettingsService { get; } = SimpleIoc.Default.GetInstance<ISettingsService>();
        private IFriendsService _FriendsService { get; } = SimpleIoc.Default.GetInstance<IFriendsService>();
        public IVoiceService VoiceService { get; } = SimpleIoc.Default.GetInstance<IVoiceService>();
        public IGuildsService GuildsService { get; } = SimpleIoc.Default.GetInstance<IGuildsService>();
        public IDispatcherHelper DispatcherHelper { get; } = SimpleIoc.Default.GetInstance<IDispatcherHelper>();

        #endregion

        public BindableGuild Guild
        {
            get => GuildsService.AllGuilds[guildId];
        }

        #region ChannelType

        public bool IsTextChannel => Model.Type == 0;
        public bool IsDirectChannel => Model.Type == 1;
        public bool IsVoiceChannel => Model.Type == 2;
        public bool IsGroupChannel => Model.Type == 3;
        public bool IsCategory => Model.Type == 4;
        public bool IsGuildChannel => !IsPrivateChannel;
        public GuildChannel AsGuildChannel => Model as GuildChannel;
        public bool IsPrivateChannel => IsDirectChannel || IsGroupChannel;
        public DirectMessageChannel AsDMChannel => Model as DirectMessageChannel;
        public bool IsTypingChannel => IsCategory || IsTextChannel || IsDirectChannel || IsGroupChannel;

        #endregion

        #region Settings

        private bool _Muted;
        public bool Muted
        {
            get => _Muted;
            set
            {
                if (Set(ref _Muted, value))
                {
                    RaisePropertyChanged(nameof(TextOpacity));
                    RaisePropertyChanged(nameof(ShowUnread));
                }
            }
        }

        #endregion

        // Order:
        //  Guild Permsissions
        //  Denies of @everyone
        //  Allows of @everyone
        //  All Role Denies
        //  All Role Allows
        //  Member denies
        //  Member allows
        private Permissions permissions = null;
        public Permissions Permissions
        {
            get
            {
                if (permissions != null)
                    return permissions;

                if (Model is GuildChannel)
                {
                    Permissions perms = Guild.Permissions.Clone();

                    var user = Guild.Model.Members.FirstOrDefault(x => x.User.Id == _DiscordService.CurrentUser.Id);

                    GuildPermission roleDenies = 0;
                    GuildPermission roleAllows = 0;
                    GuildPermission memberDenies = 0;
                    GuildPermission memberAllows = 0;
                    foreach (Overwrite overwrite in (Model as GuildChannel).PermissionOverwrites)
                        if (overwrite.Type == "role" && overwrite.Id == guildId) // @everyone Id is equal to GuildId
                        {
                            perms.AddDenies((GuildPermission)overwrite.Deny);
                            perms.AddAllows((GuildPermission)overwrite.Allow);
                        }
                        else if (overwrite.Type == "role" && user.Roles.Contains(overwrite.Id))
                        {
                            roleDenies |= (GuildPermission)overwrite.Deny;
                            roleAllows |= (GuildPermission)overwrite.Allow;
                        }
                        else if (overwrite.Type == "member" && overwrite.Id == user.User.Id)
                        {
                            memberDenies |= (GuildPermission)overwrite.Deny;
                            memberAllows |= (GuildPermission)overwrite.Allow;
                        }

                    perms.AddDenies(roleDenies);
                    perms.AddAllows(roleAllows);
                    perms.AddDenies(memberDenies);
                    perms.AddAllows(memberAllows);

                    // If owner add admin
                    if (Guild.Model.OwnerId == user.User.Id)
                    {
                        perms.AddAllows(GuildPermission.Administrator);
                    }

                    permissions = perms;
                    return perms;
                }
                return new Permissions(int.MaxValue);
            }
        }

        #region Sorting

        private int _ParentPostion;
        public int ParentPostion
        {
            get => _ParentPostion;
            set => Set(ref _ParentPostion, value);
        }
        
        public ulong AbsolutePostion
        {
            get
            {
                if (IsCategory)
                    return ((ulong)Position + 1) << 32;
                else
                    return
                        ((ulong)ParentPostion + 1) << 32 |
                        ((uint)(IsVoiceChannel ? 1 : 0) << 31) |
                        (uint)(Position + 1);
            }
        }

        #endregion

        #region Misc

        private string guildId;
        public string GuildId { get => guildId; }
        public string ParentId => Model is GuildChannel gcModel ? (IsCategory ? gcModel.Id : gcModel.ParentId) : null;
        public int Position => Model is GuildChannel gcModel ? gcModel.Position : 0;
        public string FormattedName
        {
            get
            {
                switch (Model.Type)
                {
                    case 0:
                        return Model.Name.ToLower();
                    case 4:
                        return Model.Name.ToUpper();
                }
                return Model.Name;
            }
        }
        public IEnumerable<BindableGuildMember> GuildMembers
        {
            get
            {
                if (!IsDirectChannel && !IsGroupChannel)
                    return null;

                if (Model is DirectMessageChannel dmChannel)
                {
                    return dmChannel.Users.Select(x => _FriendsService.DMUsers[x.Id]);
                }

                return null;
            }
        }

        #endregion

        #region Display

        private bool _Selected;
        public bool Selected
        {
            get => _Selected;
            set
            {
                if (Set(ref _Selected, value))
                    RaisePropertyChanged(nameof(Hidden));
            }
        }

        private bool _Collapsed;
        public bool Collapsed
        {
            get => _Collapsed;
            set
            {
                if (Set(ref _Collapsed, value))
                    RaisePropertyChanged(nameof(Hidden));
            }
        }

        public bool Hidden
        {
            get
            {
                bool hidden = false;
                if (_Collapsed && !IsCategory)
                {
                    hidden = true;
                    switch (_SettingsService.Roaming.GetValue<CollapseOverride>(SettingKeys.CollapseOverride))
                    {
                        case CollapseOverride.Mention:
                            if (MentionCount > 0)
                                hidden = false;
                            break;
                        case CollapseOverride.Unread:
                            if (ShowUnread)
                                hidden = false;
                            break;
                    }
                }
                else if (!Permissions.ReadMessages && !_SettingsService.Roaming.GetValue<bool>(SettingKeys.ShowNoPermssions))
                {
                    hidden = true;
                }
                return hidden && !Selected;
            }
        }

        public bool HasIcon
        {
            get
            {
                if (Model is DirectMessageChannel dmModel)
                {
                    if (IsDirectChannel)
                    {
                        return !string.IsNullOrEmpty(dmModel.Users[0].Avatar);
                    }
                    else if (IsGroupChannel)
                    {
                        return dmModel.IconUri(false) == null;
                    }
                }

                return false;
            }
        }

        public bool ShowUnread
        {
            get => IsUnread && !Muted && Permissions.ReadMessages;
        }

        public double TextOpacity
        {
            get
            {
                if (!Permissions.ReadMessages)
                    return 0.25;
                else if (Muted)
                    return 0.35;
                else if (IsUnread)
                    return 1;
                else
                    return 0.55;
            }
        }

        public string ImageUrl
        {
            get
            {
                if (Model is DirectMessageChannel dmModel)
                {
                    if (IsDirectChannel)
                    {
                        return dmModel.Users[0].AvatarUrl;
                    }
                    else if (IsGroupChannel)
                    {
                        //TODO: detect theme
                        return dmModel.IconUrl(true, true);
                    }
                }

                return null;
            }
        }

        public int FirstUserDiscriminator
        {
            get => Model is DirectMessageChannel dmModel ? (dmModel.Users.Count > 0 ? Convert.ToInt32(dmModel.Users[0].Discriminator) : 0) : 0;
        }

        public bool ShowIconBackdrop
        {
            get => IsDirectChannel && !HasIcon;
        }

        public ObservableHashedCollection<string, BindableVoiceUser> ConnectedUsers = new ObservableHashedCollection<string, BindableVoiceUser>();

        #endregion

        #region ReadState

        public bool IsUnread
        {
            get
            {
                if ((IsTextChannel || IsPrivateChannel || IsGroupChannel) && !string.IsNullOrEmpty(Model.LastMessageId))
                {
                    if (ReadState != null)
                    {
                        return ReadState.LastMessageId != Model.LastMessageId;
                    }
                    return true;
                }
                return false;
            }
        }

        public int MentionCount
        {
            get => ReadState == null ? 0 : ReadState.MentionCount;
        }

        private ReadState _ReadState;
        public ReadState ReadState
        {
            get => _ReadState;
            set
            {
                if (Set(ref _ReadState, value))
                {
                    DispatcherHelper.CheckBeginInvokeOnUi(() =>
                    {
                        RaisePropertyChanged(nameof(IsUnread));
                        RaisePropertyChanged(nameof(ShowUnread));
                        RaisePropertyChanged(nameof(TextOpacity));
                    });
                }
            }
        }

        public void UpdateLRMID(string id)
        {
            if (ReadState == null)
                ReadState = new ReadState();

            ReadState.LastMessageId = id;
            ReadState.MentionCount = 0;
            DispatcherHelper.CheckBeginInvokeOnUi(() =>
            {
                RaisePropertyChanged(nameof(IsUnread));
                RaisePropertyChanged(nameof(ShowUnread));
                RaisePropertyChanged(nameof(Hidden));
                RaisePropertyChanged(nameof(TextOpacity));
                RaisePropertyChanged(nameof(MentionCount));
            });
        }

        public void UpdateLMID(string id)
        {
            Model.UpdateLMID(id);
            DispatcherHelper.CheckBeginInvokeOnUi(() =>
            {
                RaisePropertyChanged(nameof(IsUnread));
                RaisePropertyChanged(nameof(ShowUnread));
                RaisePropertyChanged(nameof(Hidden));
                RaisePropertyChanged(nameof(TextOpacity));
                RaisePropertyChanged(nameof(MentionCount));
            });
        }

        #endregion

        #region Typing

        public bool IsTyping
        {
            get => Typers.Count > 0;
        }

        public List<string> GetNames()
        {
            List<string> names = new List<string>();
            var keys = Typers.Keys.ToList();

            foreach (var id in keys)
            {
                var user = GuildsService.GetGuildMember(id, GuildId);
                if (user != null)
                {
                    names.Add(user.DisplayName);
                }
            }

            return names;
        }

        public string TypingText
        {
            get
            {
                var names = GetNames();
                string typeText = "";
                for (int i = 0; i < names.Count; i++)
                {
                    if (i != 0)
                    {
                        typeText += ", ";
                    }
                    else if (i != 0 && i == names.Count - 1)
                    {
                        typeText += " and ";
                    }
                    typeText += names[i];
                }

                if (names.Count > 1)
                {
                    typeText += " are typing";
                }
                else
                {
                    typeText += " is typing";
                }
                return typeText;
            }
        }

        public ConcurrentDictionary<string, Timer> Typers = new ConcurrentDictionary<string, Timer>();

        #endregion


        #endregion

        #region Commands

        private RelayCommand openProfile;
        public RelayCommand OpenProfile => openProfile = new RelayCommand(() =>
        {
            SimpleIoc.Default.GetInstance<ISubFrameNavigationService>().NavigateTo("UserProfilePage", GuildMembers.FirstOrDefault());
        });

        private RelayCommand markAsRead;
        public RelayCommand MarkAsRead => markAsRead = new RelayCommand(async () =>
        {
            if (Model.LastMessageId != null)
                await _DiscordService.ChannelService.AckMessage(Model.Id, Model.LastMessageId);
        });

        private RelayCommand mute;
        public RelayCommand Mute => mute = new RelayCommand(async () =>
        {
            // Build basic Settings Modifier
            GuildSettingModify guildSettingModify = new GuildSettingModify();
            guildSettingModify.GuildId = GuildId == "DM" ? "@me" : GuildId;
            guildSettingModify.ChannelOverrides = new Dictionary<string, ChannelOverride>();

            ChannelOverride channelOverride;
            if(_ChannelsService.ChannelSettings.TryGetValue(Model.Id, out channelOverride))
            {
                // No pre-exisitng channeloverride, create a default
                channelOverride = new ChannelOverride();
                channelOverride.ChannelId = Model.Id;
                channelOverride.Muted = false; // Will be swapped later
            }

            channelOverride.Muted = !channelOverride.Muted;

            // Finish Settings Modifer and send request
            guildSettingModify.ChannelOverrides.Add(Model.Id, channelOverride);
            await _DiscordService.UserService.ModifyGuildSettings(guildSettingModify.GuildId, guildSettingModify);
        });

        private RelayCommand leaveGroup;
        public RelayCommand LeaveGroup => leaveGroup = new RelayCommand(async () =>
        {
            await _DiscordService.ChannelService.DeleteChannel(Model.Id);
        });

        private RelayCommand copyId;
        public RelayCommand CopyId => copyId = new RelayCommand(() =>
        {
            SimpleIoc.Default.GetInstance<IClipboardService>().CopyToClipboard(Model.Id);
        });

        #endregion
    }
}
