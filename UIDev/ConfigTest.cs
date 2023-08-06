﻿using BossMod;
using ImGuiNET;
using System;

namespace UIDev
{
    class ConfigTest : TestWindow
    {
        private string _command = "";
        private ConfigUI _ui;

        public ConfigTest() : base("Config", ImGuiWindowFlags.None)
        {
            _ui = new(Service.Config, new(TimeSpan.TicksPerSecond));
        }

        public override void Draw()
        {
            ImGui.InputText("##console", ref _command, 1024);
            ImGui.SameLine();
            if (ImGui.Button("Execute"))
            {
                var output = Service.Config.ConsoleCommand(_command.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                foreach (var msg in output)
                {
                    Service.Log(msg);
                }
            }

            _ui.Draw();
        }
    }
}
