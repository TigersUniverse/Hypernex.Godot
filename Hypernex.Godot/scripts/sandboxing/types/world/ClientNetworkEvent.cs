using System;
using System.Collections.Generic;
using System.Linq;
using Hypernex.Networking.Messages;
using Hypernex.Tools;
using Nexport;

namespace Hypernex.Sandboxing.SandboxedTypes.World
{
    public class ClientNetworkEvent
    {
        private GameInstance gameInstance;

        public ClientNetworkEvent()
        {
            throw new Exception("Cannot instantiate ClientNetworkEvent!");
        }

        internal ClientNetworkEvent(GameInstance gameInstance) => this.gameInstance = gameInstance;

        public void SendToServer(string eventName, object[] data = null, 
            MessageChannel messageChannel = MessageChannel.Reliable)
        {
            NetworkedEvent networkedEvent = new NetworkedEvent
            {
                Auth = new JoinAuth
                {
                    UserId = APITools.CurrentUser.Id,
                    TempToken = gameInstance?.userIdToken
                },
                EventName = eventName,
                Data = new List<object> {data?.ToArray() ?? Array.Empty<object>()}
            };
            gameInstance?.SendMessage(networkedEvent, messageChannel);
        }
    }
}