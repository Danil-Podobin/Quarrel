﻿using DiscordAPI.Models;

namespace Quarrel.ViewModels.Messages.Gateway
{
    public sealed class GatewayChannelDeletedMessage
    {
        public GatewayChannelDeletedMessage(Channel channel)
        {
            Channel = channel;
        }

        public Channel Channel { get; }
    }
}
