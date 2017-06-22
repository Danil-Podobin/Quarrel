﻿using Discord_UWP.SharedModels;
using Microsoft.Advertising.WinRT.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp.UI.Animations;
using static Discord_UWP.Common;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Discord_UWP
{
    public sealed partial class MessageControl : UserControl
    {

        /// <summary>
        /// Fired when a link element in the markdown was tapped.
        /// </summary>
        public event EventHandler<MarkdownTextBlock.LinkClickedEventArgs> LinkClicked;

        private bool _isadvert = false;
        public bool IsAdvert
        {
            get { return _isadvert; }
            set { _isadvert = value; Notify("IsAdvert"); }
        }
        public static readonly DependencyProperty IsAdvertProperty =
        DependencyProperty.Register("IsAdvert", typeof(bool), typeof(MessageControl),
        new PropertyMetadata(false, OnIsAdvertPropertyChanged));
        private static void OnIsAdvertPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MessageControl)d)._isadvert = (bool)e.NewValue;
        }

        public Visibility MoreButtonVisibility
        {
            get { return moreButton.Visibility; }
            set { moreButton.Visibility = value; Notify("MoreButtonVisibility"); }
        }
        public static readonly DependencyProperty MoreButtonVisibilityProperty =
        DependencyProperty.Register("MoreButtonVisibility", typeof(Visibility), typeof(MessageControl),
        new PropertyMetadata(Visibility.Visible, OnMoreButtonVisibilityPropertyChanged));
        private static void OnMoreButtonVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MessageControl).moreButton.Visibility = (Visibility)e.NewValue;
        }

        private bool _iscontinuation;
        public bool IsContinuation
        {
            get { return _iscontinuation; }
            set { _iscontinuation = value; Notify("IsContinuation"); }
        }
        public static readonly DependencyProperty IsContinuationProperty =
            DependencyProperty.Register("IsContinuation", typeof(bool), typeof(MessageControl),
                new PropertyMetadata(false, OnIsContinuationPropertyChanged));
        private static void OnIsContinuationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MessageControl)d)._iscontinuation = (bool)e.NewValue;
            if ((bool)e.NewValue)
            {
                Debug.WriteLine("IS CONTINUATION");
                VisualStateManager.GoToState(((MessageControl)d), "Continuation", false);
            }
        }

        private string _header;
        public string Header
        {
            get { return _header; }
            set { _header = value; Notify("Header"); }
        }
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(MessageControl),
                new PropertyMetadata(string.Empty, OnHeaderPropertyChanged));
        private static void OnHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MessageControl)d)._header = (string)e.NewValue;
        }

        Message? _message;
        public Message? Message
        {
            get { return _message; }
            set { _message = value; Notify("message"); }
        }
        public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("Message", typeof(Message?), typeof(MessageControl),
        new PropertyMetadata(null, OnmessagePropertyChanged));
        private static void OnmessagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((Message?) e.NewValue == null) return;
            Debug.WriteLine("New message");
            ((MessageControl)d)._message = (Message?)e.NewValue;
            ((MessageControl)d).UpdateControl();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            if (_isadvert == true && (_message == null))
            {
                VisualStateManager.GoToState(this, "Advert", false);
                AdControl ad = new AdControl();
                ad.HorizontalAlignment = HorizontalAlignment.Center;
                ad.Width = 300;
                ad.Height = 50;
                ad.ApplicationId = "d9818ea9-2456-4e67-ae3d-01083db564ee";
                ad.AdUnitId = "336795";
                ad.Margin = new Thickness(6);
                ad.Background = new SolidColorBrush(Colors.Red);
                Grid.SetColumnSpan(ad, 10);
                Grid.SetRowSpan(ad, 10);
                rootGrid.Children.Add(ad);
                return;
            }
        }

        public void UpdateControl()
        {
            if (!Message.HasValue) return;
                username.Text = _message.Value.User.Username;
                SharedModels.GuildMember member;
                if (App.CurrentId != null && Storage.Cache.Guilds[App.CurrentId].Members.ContainsKey(Message.Value.User.Id))
                {
                    member = Storage.Cache.Guilds[App.CurrentId].Members[Message.Value.User.Id].Raw;
                }
                else
                {
                    member = new SharedModels.GuildMember();
                }
                if (member.Nick != null)
                {
                    username.Text = member.Nick;
                }

            if (member.Roles != null && member.Roles.Count() > 0)
            {
                foreach (SharedModels.Role role in Storage.Cache.Guilds[App.CurrentId].RawGuild.Roles)
                {
                    if (role.Id == member.Roles.First<string>())
                    {
                        username.Foreground = IntToColor(role.Color);
                    }
                }
            }

            if (_message.Value.User.Bot == true)
                BotIndicator.Visibility = Visibility.Visible;

            if (!string.IsNullOrEmpty(_message.Value.User.Avatar))
            {
                avatar.Fill = new ImageBrush() { ImageSource = new BitmapImage(new Uri("https://cdn.discordapp.com/avatars/" + _message.Value.User.Id + "/" + _message.Value.User.Avatar + ".jpg")) };
            }
            else
            {
                avatar.Fill = new ImageBrush() { ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/Square150x150Logo.scale-200.png")) };
            }

            timestamp.Text = Common.HumanizeDate(_message.Value.Timestamp, null);
            if (_message.Value.EditedTimestamp.HasValue)
                timestamp.Text += " (Edited " + Common.HumanizeDate(_message.Value.EditedTimestamp.Value, _message.Value.Timestamp) + ")";

            if (Message.Value.Reactions != null)
            {
                GridView gridview = new GridView
                {
                    SelectionMode = ListViewSelectionMode.None,
                    Margin = new Thickness(0),
                    Padding = new Thickness(0)
                };
                foreach (Reactions reaction in Message.Value.Reactions.Where(x => x.Count>0))
                {
                    
                    ToggleButton reactionToggle = new ToggleButton();
                    reactionToggle.IsChecked = reaction.Me;
                    reactionToggle.Tag = new Tuple<string, string, SharedModels.Reactions>(Message.Value.ChannelId, Message.Value.Id, reaction);
                    reactionToggle.Click += ToggleReaction;
                    if (reaction.Me)
                    {
                        reactionToggle.IsChecked = true;
                    }

                    reactionToggle.Content = reaction.Emoji.Name + " " + reaction.Count.ToString(); ;
                    reactionToggle.Style = (Style)App.Current.Resources["EmojiButton"];
                    reactionToggle.MinHeight = 0;
                   
                    GridViewItem gridViewItem = new GridViewItem(){Content=reactionToggle};
                    
                    gridViewItem.MinHeight = 0;
                    gridview.Margin = new Thickness(6,0,0,0);
                    gridview.Items.Add(gridViewItem);
                }
                Grid.SetRow(gridview, 3);
                Grid.SetColumn(gridview, 1);
                rootGrid.Children.Add(gridview);
            }

            LoadAttachements(true);
            content.Users = _message.Value.Mentions;
            if (Message?.Content == "")
            {
                content.Visibility = Visibility.Collapsed;
                Grid.SetRow(moreButton, 4);
            }
            content.Text = _message.Value.Content;
        }

        readonly string[] ImageFiletypes = { ".jpg", ".jpeg", ".gif", ".tif", ".tiff", ".png", ".bmp", ".gif", ".ico" };
        private void LoadAttachements(bool EnableImages)
        {
            if (Message.Value.Attachments.Any())
            {
                var attachement = Message.Value.Attachments.First();
                bool IsImage = false;
                if (EnableImages)
                {
                    foreach (string ext in ImageFiletypes)
                        if (attachement.Filename.ToLower().EndsWith(ext))
                        {
                            IsImage = true;
                            if (attachement.Filename.EndsWith(".svg"))
                                AttachedImageViewer.Source = new SvgImageSource(new Uri(attachement.Url));
                            else
                            {
                                AttachedImageViewer.Source = new BitmapImage(new Uri(attachement.Url));
                            }
                            break;
                        }
                }
                if (IsImage)
                {
                    AttachedImageViewer.Visibility = Visibility.Visible;
                    LoadingImage.Visibility = Visibility.Visible;
                    LoadingImage.IsActive = true;
                    AttachedImageViewer.ImageOpened += AttachedImageViewer_ImageLoaded;
                    AttachedImageViewer.ImageFailed += AttachementImageViewer_ImageFailed;
                }
                else
                {
                    FileName.NavigateUri = new Uri(attachement.Url);
                    FileName.Content = attachement.Filename;
                    FileSize.Text = HumanizeFileSize(attachement.Size);
                    AttachedFileViewer.Visibility = Visibility.Visible;
                }
            }
        }
        private void AttachedImageViewer_ImageLoaded(object sender, RoutedEventArgs e)
        {
            AttachedImageViewer.ImageOpened -= AttachedImageViewer_ImageLoaded;
            AttachedImageViewer.ImageFailed -= AttachementImageViewer_ImageFailed;
            LoadingImage.IsActive = false;
            LoadingImage.Visibility=Visibility.Collapsed;
        }

        private void AttachementImageViewer_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            AttachedImageViewer.ImageOpened -= AttachedImageViewer_ImageLoaded;
            AttachedImageViewer.ImageFailed -= AttachementImageViewer_ImageFailed;
            LoadingImage.IsActive = false;
            LoadingImage.Visibility = Visibility.Collapsed;
            //Reload attachements but with images disabled
            LoadAttachements(false);
        }

        public MessageControl()
        {
            this.InitializeComponent();
        }

        private void moreButton_Click(object sender, RoutedEventArgs e)
        {
            if (perms == null)
            {
                Permissions perms = new Permissions();
                if (App.CurrentId != App.DMid)
                {
                    foreach (SharedModels.Role role in Storage.Cache.Guilds[App.CurrentId].RawGuild.Roles)
                    {
                        if (!Storage.Cache.Guilds[App.CurrentId].Members.ContainsKey(Storage.Cache.CurrentUser.Raw.Id))
                        {
                            Storage.Cache.Guilds[App.CurrentId].Members.Add(Storage.Cache.CurrentUser.Raw.Id, new CacheModels.Member(Session.GetGuildMember(App.CurrentId, Storage.Cache.CurrentUser.Raw.Id)));
                        }

                        if (Storage.Cache.Guilds[App.CurrentId].Members[Storage.Cache.CurrentUser.Raw.Id].Raw.Roles.Count() != 0 && Storage.Cache.Guilds[App.CurrentId].Members[Storage.Cache.CurrentUser.Raw.Id].Raw.Roles.First().ToString() == role.Id)
                        {
                            perms.GetPermissions(role, Storage.Cache.Guilds[App.CurrentId].RawGuild.Roles);
                        }
                        else
                        {
                            perms.GetPermissions(0);
                        }
                    }
                }
            }
            if (!perms.EffectivePerms.ManageMessages)
            {
                MoreDelete.Visibility = Visibility.Collapsed;
            }
            else
            if (_message?.User.Id != Storage.Cache.CurrentUser.Raw.Id)
            {
                MoreEdit.Visibility = Visibility.Collapsed;
            }
            FlyoutBase.ShowAttachedFlyout(sender as Button);
        }

        private void UserControl_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(moreButton);
        }

        private void UserControl_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(moreButton);
        }

        private void ToggleReaction(object sender, RoutedEventArgs e)
        {
            if ((sender as ToggleButton)?.IsChecked == false) //Inverted since it changed
            {
                Session.DeleteReaction(((sender as ToggleButton).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item1, ((sender as ToggleButton).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item2, ((Tuple<string, string, Reactions>) (sender as ToggleButton).Tag).Item3.Emoji);
                if (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Me)
                {
                    ((ToggleButton) sender).Content = (((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count - 1).ToString();
                }
                else
                {
                    ((ToggleButton) sender).Content = (((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count).ToString();
                }
            }
            else
            {
                Session.CreateReaction((((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item1, ((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item2, ((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Emoji);

                if (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Me)
                {
                    ((ToggleButton) sender).Content = (((ToggleButton) sender).Tag as Tuple<string, string, SharedModels.Reactions>)?.Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count).ToString();
                }
                else
                {
                    ((ToggleButton) sender).Content = ((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Emoji.Name + " " + (((Tuple<string, string, Reactions>) ((ToggleButton) sender).Tag).Item3.Count + 1).ToString();
                }
            }
        }
        private async void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
                            

            string val = "";
            MessageBox.Document.GetText(TextGetOptions.None, out val);
            if (val.Trim() == "")
                MessageBox.Document.SetText(TextSetOptions.None, content.Text);
            EditBox.Visibility = Visibility.Visible;
        }

        private void CreateMessage(object sender, RoutedEventArgs e)
        {
            string editedText = "";
            MessageBox.Document.GetText(TextGetOptions.None, out editedText);
            Session.EditMessage(_message.Value.ChannelId, _message.Value.Id, editedText);
        }

        private void content_LinkClicked(object sender, MarkdownTextBlock.LinkClickedEventArgs e)
        {
            LinkClicked(sender, e);
        }

        private void MessageBox_TextChanged(object sender, RoutedEventArgs e)
        {
            string text = "";
            MessageBox.Document.GetText(TextGetOptions.None, out text);
            if (text != "")
                SendBox.IsEnabled = true;
            else
                SendBox.IsEnabled = false;
        }
        Permissions perms;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            EditBox.Visibility = Visibility.Collapsed;
        }

        private void MenuFlyoutItem_Click_1(object sender, RoutedEventArgs e)
        {
            Session.DeleteMessage(_message.Value.ChannelId, _message.Value.Id);
        }

        private void MoreCopyId_Click(object sender, RoutedEventArgs e)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(_message.Value.Id);
            Clipboard.SetContent(dataPackage);
        }
    }
}