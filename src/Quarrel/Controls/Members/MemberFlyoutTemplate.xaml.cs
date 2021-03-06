﻿using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Quarrel.ViewModels.Messages.Gateway;
using Quarrel.ViewModels.Models.Bindables;
using Quarrel.ViewModels.Services.Discord.Rest;
using Quarrel.ViewModels.Services.Navigation;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;

namespace Quarrel.Controls.Members
{
    public sealed partial class MemberFlyoutTemplate : UserControl
    {   
        public MemberFlyoutTemplate()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) =>
            {
                this.Bindings.Update();
            };
            Messenger.Default.Register<GatewayNoteUpdatedMessage>(this, async m =>
            {
                await DispatcherHelper.RunAsync(() =>
                {
                    if (ViewModel != null && m.UserId == ViewModel.Model.User.Id)
                        this.Bindings.Update();
                });
            });
        }

        public BindableGuildMember ViewModel => DataContext as BindableGuildMember;

        private void NoteBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SimpleIoc.Default.GetInstance<IDiscordService>().UserService.AddNote(ViewModel.Model.User.Id, new DiscordAPI.API.User.Models.Note() { Content = (sender as TextBox).Text });
        }

        private void AvatarButton_Click(object sender, RoutedEventArgs e)
        {
            // Connected Animation
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(ViewModels.Helpers.Constants.ConnectedAnimationKeys.MemberFlyoutAnimation, FullAvatar);

            // Navigate
            SimpleIoc.Default.GetInstance<ISubFrameNavigationService>().NavigateTo("UserProfilePage", ViewModel);

            // Close popup
            var presenter = Parent.FindParent<FlyoutPresenter>();
            if (presenter != null)
            {
                (presenter.Parent as Popup).IsOpen = false;
            }
        }
    }
}
