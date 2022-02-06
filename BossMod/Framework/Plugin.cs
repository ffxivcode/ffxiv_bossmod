﻿using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Numerics;

namespace BossMod
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Boss Mod";

        private ConfigRoot _config;
        private CommandManager _commandManager { get; init; }

        private Network _network;
        private WorldStateGame _ws;
        private DebugEventLogger _debugLogger;
        private BossModuleManager _bossmod;
        private Autorotation _autorotation;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface dalamud,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            dalamud.Create<Service>();
            Service.LogHandler = (string msg) => PluginLog.Log(msg);
            //Service.Device = pluginInterface.UiBuilder.Device;
            Camera.Instance = new();

            _config = ConfigRoot.ReadConfig(dalamud);
            var generalCfg = _config.Get<GeneralConfig>();

            _commandManager = commandManager;
            _commandManager.AddHandler("/vbm", new CommandInfo(OnCommand) { HelpMessage = "Show boss mod config UI" });

            _network = new(generalCfg);
            _ws = new(_network);
            _debugLogger = new(_ws, generalCfg);
            _bossmod = new(_ws, _config);
            _autorotation = new(_network, generalCfg);

            dalamud.UiBuilder.Draw += DrawUI;
            dalamud.UiBuilder.OpenConfigUi += OpenConfigUI;
        }

        public void Dispose()
        {
            WindowManager.Reset();
            _debugLogger.Dispose();
            _bossmod.Dispose();
            _network.Dispose();
            _autorotation.Dispose();
            _commandManager.RemoveHandler("/vbm");
        }

        private void OnCommand(string cmd, string args)
        {
            Service.Log($"OnCommand: {cmd} {args}");
            var split = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length == 0)
            {
                OpenConfigUI();
                return;
            }

            switch (split[0])
            {
                case "z":
                    _bossmod.ActivateModuleForZone(split.Length > 1 ? ushort.Parse(split[1]) : _ws.CurrentZone);
                    break;
                case "d":
                    OpenDebugUI();
                    break;
            }
        }

        private void OpenConfigUI()
        {
            var w = WindowManager.CreateWindow("Boss mod config", _config.Draw, () => { });
            w.SizeHint = new Vector2(300, 300);
        }

        private void OpenDebugUI()
        {
            var ui = new DebugUI(_ws, _network, _autorotation);
            var w = WindowManager.CreateWindow("Boss mod debug UI", ui.Draw, ui.Dispose);
            w.SizeHint = new Vector2(300, 200);
        }

        private void DrawUI()
        {
            Camera.Instance?.Update();
            _debugLogger.Update();
            _ws.Update();
            _bossmod.Update();
            _autorotation.Update();

            WindowManager.DrawAll();
        }
    }
}