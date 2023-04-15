﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using XIVDeck.FFXIVPlugin.ActionExecutor.Payloads;
using XIVDeck.FFXIVPlugin.Base;
using XIVDeck.FFXIVPlugin.Game;
using XIVDeck.FFXIVPlugin.Game.Chat;
using XIVDeck.FFXIVPlugin.Resources.Localization;

namespace XIVDeck.FFXIVPlugin.ActionExecutor.Strategies;

[ActionStrategy(HotbarSlotType.GearSet)]
public class GearsetStrategy : IActionStrategy {
    private static readonly GameStateCache GameStateCache = XIVDeckPlugin.Instance.GameStateCache;

    private static ExecutableAction GetExecutableAction(GameStateCache.Gearset gearset) {
        return new ExecutableAction {
            ActionId = gearset.Slot,
            ActionName = gearset.Name,
            IconId = 062800 + (int) gearset.ClassJob,
            HotbarSlotType = HotbarSlotType.GearSet
        };
    }

    private static unsafe GameStateCache.Gearset? GetGearsetBySlot(uint slot) {
        // we want to intentionally bypass the cache here as the cache is a bit messy and, well, not always up to
        // date. this can get called quite a bit, so we don't want to force updating everything each time (which
        // (invalidates the entire point of the cache).

        var gsEntry = RaptureGearsetModule.Instance()->Gearset[(int) slot - 1];

        if (gsEntry == null || !gsEntry->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
            return null;

        return new GameStateCache.Gearset {
            Slot = gsEntry->ID,
            ClassJob = gsEntry->ClassJob,
            Name = MemoryHelper.ReadString(new nint(gsEntry->Name), 47)
        };
    }

    public ExecutableAction? GetExecutableActionById(uint slotId) {
        var gearset = GetGearsetBySlot(slotId);

        return gearset == null ? null : GetExecutableAction(gearset.Value);
    }

    public List<ExecutableAction> GetAllowedItems() {
        GameStateCache.Refresh();

        return GameStateCache.Gearsets!.Select(GetExecutableAction).ToList();
    }

    public void Execute(uint actionSlot, ActionPayload? payload) {
        var gearset = GetGearsetBySlot(actionSlot);

        if (gearset == null)
            throw new ArgumentException(string.Format(UIStrings.GearsetStrategy_GearsetNotFoundError, actionSlot));

        var command = $"/gearset change {gearset.Value.Slot + 1}";

        switch (payload) {
            case GearsetPayload {GlamourPlateId: >= 1 and <= 20} p:
                command += $" {p.GlamourPlateId}";
                break;
            case GearsetPayload {GlamourPlateId: not null}:
                throw new ArgumentException("Glamour Plate ID must be between 1 and 20.");
        }

        PluginLog.Debug($"Executing command: {command}");
        Injections.Framework.RunOnFrameworkThread(delegate {
            ChatHelper.GetInstance().SendSanitizedChatMessage(command);
        });
    }

    public int GetIconId(uint slot) {
        var gearset = GetGearsetBySlot(slot);

        if (gearset == null) return 0;

        return 062800 + (int) gearset.Value.ClassJob;
    }

    public Type GetPayloadType() => typeof(GearsetPayload);
}