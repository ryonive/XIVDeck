﻿using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVPlugin.helpers;
using FFXIVPlugin.Server.Messages.Inbound;
using Newtonsoft.Json;

namespace FFXIVPlugin.Utils {
    public class HotbarWatcher {
        private XIVDeckPlugin _plugin;
        private HotBars _hotbar_cache;
        
        public HotbarWatcher(XIVDeckPlugin plugin) {
            Injections.Framework.Update += onGameUpdate;
            this._plugin = plugin;
        }

        public unsafe void onGameUpdate(Framework framework) {
            var hotbarModule = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetRaptureHotbarModule();

            if (checkHotbarEquality(hotbarModule->HotBar, _hotbar_cache)) {
                // no-op
            } else {
                PluginLog.Information($"HOTBAR UPDATE!" );
                _hotbar_cache = hotbarModule->HotBar;

                var wsServer = this._plugin.WSServer;
                var serverObj = wsServer.server;
                var socketService = serverObj.WebSocketServices["/xivdeck"];
                var sessionManager = socketService.Sessions;
                sessionManager.Broadcast(JsonConvert.SerializeObject(new WsHotbarUpdateMessage(hotbarModule->HotBar)));
            }
        }

        private unsafe bool checkHotbarEquality(HotBars left, HotBars right) {
            for (int i = 0; i < 11; i++) {
                var hotbar = left[i];
                for (int j = 0; j < 12; j++) {
                    var slot = hotbar->Slot[j];
                    
                    if (slot->CommandId != right[i]->Slot[j]->CommandId) {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Dispose() {
            Injections.Framework.Update -= onGameUpdate;
        }
    }
}