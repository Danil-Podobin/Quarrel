﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Discord_UWP.LocalModels;
using Discord_UWP.Managers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Discord_UWP.SubPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DeleteChannel : Page
    {
        public DeleteChannel()
        {
            this.InitializeComponent();
            App.SubpageCloseHandler += App_SubpageCloseHandler;
        }

        private void App_SubpageCloseHandler(object sender, EventArgs e)
        {
            CloseButton_Click(null, null);
            App.SubpageCloseHandler -= App_SubpageCloseHandler;
        }

        string chnId = "";
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            chnId = e.Parameter.ToString();
            if (App.CurrentGuildIsDM)
            {
                Message.Text = "Are you sure you want to Close your DM with " + LocalState.DMs[chnId].Users.FirstOrDefault().Username; //TODO: Translate
            } else
            {
                Message.Text = App.GetString("/Dialogs/VerifyDelete") + " " + LocalState.Guilds[App.CurrentGuildId].channels[chnId].raw.Name + "?";
            }
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            CloseButton_Click(null, null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            scale.CenterY = this.ActualHeight / 2;
            scale.CenterX = this.ActualWidth / 2;
            NavAway.Begin();
            App.SubpageClosed();
        }

        private void NavAway_Completed(object sender, object e)
        {
            Frame.Visibility = Visibility.Collapsed;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await RESTCalls.DeleteChannel(chnId); //TODO: Rig to App.Events
            CloseButton_Click(null, null);
        }
    }
}
