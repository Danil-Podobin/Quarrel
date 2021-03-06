﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;

namespace Quarrel.Xaml.Behaviors
{
    class OpenListViewItemFlyoutAction : DependencyObject, IAction
    {
        public object Execute(object sender, object parameter)
        {
            if(((sender as ListView)?.ContainerFromItem((parameter as ItemClickEventArgs)?.ClickedItem) as ListViewItem
                )?.ContentTemplateRoot is FrameworkElement flyout && FlyoutBase.GetAttachedFlyout(flyout) != null)
                FlyoutBase.ShowAttachedFlyout(flyout);

            return null;
        }

    }
}
