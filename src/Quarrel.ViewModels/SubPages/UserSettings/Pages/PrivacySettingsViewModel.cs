﻿using DiscordAPI.API.User.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Quarrel.ViewModels.Services.Discord.CurrentUser;
using Quarrel.ViewModels.Services.Discord.Rest;

namespace Quarrel.ViewModels.SubPages.UserSettings.Pages
{
    public class PrivacySettingsViewModel : ViewModelBase
    {
        public ICurrentUserService CurrentUsersService
        {
            get => SimpleIoc.Default.GetInstance<ICurrentUserService>();
        }

        public bool FilterAll
        {
            get => CurrentUsersService.CurrentUserSettings.ExplicitContentFilter == 2;
            set
            {
                ModifyUserSettings modify = new ModifyUserSettings(CurrentUsersService.CurrentUserSettings);
                modify.ExplicitContentFilter = 2;
                ApplyChanges(modify);
            }
        }

        public bool PublicFilter
        {
            get => CurrentUsersService.CurrentUserSettings.ExplicitContentFilter == 1;
            set
            {
                ModifyUserSettings modify = new ModifyUserSettings(CurrentUsersService.CurrentUserSettings);
                modify.ExplicitContentFilter = 1;
                ApplyChanges(modify);
            }
        }

        public bool NoFilter
        {
            get => CurrentUsersService.CurrentUserSettings.ExplicitContentFilter == 0;
            set
            {
                ModifyUserSettings modify = new ModifyUserSettings(CurrentUsersService.CurrentUserSettings);
                modify.ExplicitContentFilter = 0;
                ApplyChanges(modify);
            }
        }

        public async void ApplyChanges(ModifyUserSettings modify)
        {
            await SimpleIoc.Default.GetInstance<IDiscordService>().UserService.UpdateSettings(modify);
        }
    }
}
