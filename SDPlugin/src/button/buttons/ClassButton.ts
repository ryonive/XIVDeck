﻿import {BaseButton} from "../BaseButton";
import AbstractStateEvent from "@rweich/streamdeck-events/dist/Events/Received/Plugin/AbstractStateEvent";
import {KeyDownEvent} from "@rweich/streamdeck-events/dist/Events/Received/Plugin";
import {
    GameClassMessage,
    GetClassOpcode,
    SwitchClassOpcode
} from "../../link/ffxivplugin/messages/ClassMessages";
import plugin from "../../plugin";

export type ClassButtonSettings = {
    classId: number
}

export class ClassButton extends BaseButton {
    classId: number;
    useGameIcon: boolean = true;

    constructor(event: AbstractStateEvent) {
        super(event.context);

        let settings = event.settings as ClassButtonSettings;
        this.classId = settings.classId;
        
        // try to render now, and schedule defer
        this.render();
        this._xivEventListeners.add(plugin.xivPluginLink.on("initReply", this.render.bind(this)));
    }
    
    async execute(event: KeyDownEvent): Promise<void> {
        if (this.classId == null) {
            throw new Error("No class specified for this button");
        }

        await this._sendExec(new SwitchClassOpcode(this.classId));
    }
    
    async render(): Promise<void> {
        if (!this.useGameIcon) {
            this.setImage("");
            return
        }

        if (!plugin.xivPluginLink.isReady()) {
            return;
        }

        if (this.classId == null) {
            return
        }
        
        let myClass = await plugin.xivPluginLink.send(new GetClassOpcode(this.classId)) as GameClassMessage;
        
        this.setImage(myClass.class.iconData);
    }
}