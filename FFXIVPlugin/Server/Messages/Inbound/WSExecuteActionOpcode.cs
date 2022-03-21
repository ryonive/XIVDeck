﻿using System;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;
using XIVDeck.FFXIVPlugin.ActionExecutor;
using XIVDeck.FFXIVPlugin.Base;
using XIVDeck.FFXIVPlugin.Server.Messages.Outbound;

namespace XIVDeck.FFXIVPlugin.Server.Messages.Inbound {
    public class WSExecuteActionOpcode : BaseInboundMessage {
        [JsonRequired] public ExecutableAction Action { get; set; } = default!;
        
        /**
         * Options to pass to the execution strategy (if any)
         */
        public dynamic? Options { get; set; } = default!;

        public override void Process(XIVDeckRoute session) {
            HotbarSlotType actionType = this.Action.HotbarSlotType;
            
            if (!Injections.ClientState.IsLoggedIn)
                throw new InvalidOperationException("A player is not logged in to the game!");
            
            ActionDispatcher.GetStrategyForSlotType(actionType).Execute((uint) this.Action.ActionId, this.Options);
            
            session.SendMessage(new WSReplyMessage());
        }

        public WSExecuteActionOpcode() : base("execAction") { }
    }
}