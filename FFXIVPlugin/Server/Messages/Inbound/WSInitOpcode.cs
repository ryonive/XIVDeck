﻿using System.Reflection;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using EmbedIO.WebSockets;
using Newtonsoft.Json;
using XIVDeck.FFXIVPlugin.Base;
using XIVDeck.FFXIVPlugin.Game;
using XIVDeck.FFXIVPlugin.Server.Helpers;
using XIVDeck.FFXIVPlugin.Server.Messages.Outbound;
using XIVDeck.FFXIVPlugin.UI;
using XIVDeck.FFXIVPlugin.UI.Windows;
using XIVDeck.FFXIVPlugin.UI.Windows.Nags;
using XIVDeck.FFXIVPlugin.Utils;

namespace XIVDeck.FFXIVPlugin.Server.Messages.Inbound;

public enum PluginMode {
    Plugin,
    Inspector,
    Developer
}

[WSOpcode("init")]
public class WSInitOpcode : BaseInboundMessage {
    public string Version { get; set; } = default!;
    [JsonProperty("mode")]
    public PluginMode? Mode { get; set; }
    
    public override async Task Process(IWebSocketContext context) {
        // hide all nags
        NagWindow.CloseAllNags();

        var sdPluginVersion = System.Version.Parse(this.Version);
        
        if (sdPluginVersion < System.Version.Parse(Constants.MinimumSDPluginVersion)) {
            await context.WebSocket.CloseAsync(CloseStatusCode.ProtocolError,
                "The version of the Stream Deck plugin is too old.", context.CancellationToken);
            
            PluginLog.Warning($"The currently-installed version of the XIVDeck Stream Deck plugin " +
                              $"is {this.Version}, but version {Constants.MinimumSDPluginVersion} is needed.");
            ForcedUpdateNag.Show();

            return;
        }

        var xivPluginVersion = Assembly.GetExecutingAssembly().GetName().Version!;
        var reply = new WSInitReplyMessage(xivPluginVersion.ToString(), AuthHelper.Instance.Secret);
        await WebUtils.SendMessage(context, reply);
        PluginLog.Information($"XIVDeck Stream Deck Plugin version {this.Version} has connected!");
        PluginLog.Debug($"Mode: {this.Mode}");

        // version check
        if (sdPluginVersion != xivPluginVersion && this.Mode is null or PluginMode.Plugin) {
            var updateMessage = new SeStringBuilder()
                .AddUiForeground(514)
                .AddText($"[XIVDeck] Your version of the XIVDeck Stream Deck Plugin is out of date. Please " +
                                 "consider installing ")
                .Add(ChatLinkWiring.GetPayload(LinkCode.GetGithubReleaseLink))
                .AddUiForeground($"\xE0BB version {StringUtils.GetMajMinBuild()}", 32)
                .Add(RawPayload.LinkTerminator)
                .AddText(" from GitHub!")
                .AddUiForegroundOff()
                .Build();
            
            DeferredChat.SendOrDeferMessage(updateMessage);
        }

        // check for first-run
        if (!XIVDeckPlugin.Instance.Configuration.HasLinkedStreamDeckPlugin) {
            Injections.Chat.Print("[XIVDeck] Thank you for installing the Stream Deck plugin. XIVDeck is " +
                                  "now ready to go!");

            XIVDeckPlugin.Instance.Configuration.HasLinkedStreamDeckPlugin = true;
            XIVDeckPlugin.Instance.Configuration.Save();
        }
    }
}