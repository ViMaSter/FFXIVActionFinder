using System;
using System.Collections.Generic;
using System.Threading;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using SkillFinder.Extensions;
using SkillFinder.Windows;

namespace SkillFinder;

#nullable disable

// ReSharper disable once UnusedType.Global - Dalamud plugin entry point
public sealed class Plugin : IDalamudPlugin
{
    private IDalamudPluginInterface PluginInterface { get; init; }
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SkillFinder");
    private MainWindow MainWindow { get; init; }
    
    private readonly CancellationTokenSource tokenSource = new();

    private readonly List<Tuple<int, int>> lastHoveredKeyboardActions = [];
    private readonly List<Tuple<AddonActionCrossExtensions.CrossBars, int>> lastHoveredActionsCross = [];
    private bool showHighlight;

    private readonly Dictionary<string, AddonActionCrossExtensions.CrossBars> mappings = new()
    {
        {"_ActionCross", AddonActionCrossExtensions.CrossBars.Cross},
        {"_ActionDoubleCrossL", AddonActionCrossExtensions.CrossBars.DoubleCrossL},
        {"_ActionDoubleCrossR", AddonActionCrossExtensions.CrossBars.DoubleCrossR}
    };
    
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IPluginLog pluginLog,
        IClientState clientState,
        IAddonLifecycle addonLifecycle,
        IGameGui gameGUI
            )
    {
        PluginInterface = pluginInterface;
        PluginLog = pluginLog;
        ClientState = clientState;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        PluginLog.Info("Restored state: ");
        
        MainWindow = new MainWindow();

        WindowSystem.AddWindow(MainWindow);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += () => MainWindow.Toggle();
        PluginInterface.UiBuilder.OpenConfigUi += () => {};
        
        addonLifecycle.RegisterListener(AddonEvent.PreSetup, "ActionMenu", (_, _) =>
        {
            showHighlight = true;
        });
        addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ActionMenu", (_, _) =>
        {
            showHighlight = false;
            
            AddonActionCrossExtensions.CleanupHighlights(gameGUI, lastHoveredKeyboardActions, lastHoveredActionsCross);
        });
        
        gameGUI.HoveredActionChanged += (_, action) =>
        {
            if (!showHighlight)
            {
                return;
            }
            
            AddonActionCrossExtensions.CleanupHighlights(gameGUI, lastHoveredKeyboardActions, lastHoveredActionsCross);
            
            // handle keyboard cross bar
            pluginLog.Warning(action.ActionID.ToString());
            unsafe
            {
                for (var i = 0; i < 10; i++)
                {
                    var indexString = i == 0 ? "" : i.ToString("D2");
                    var actionbarPointer = gameGUI.GetAddonByName($"_ActionBar{indexString}");
                    var actionBar = (AddonActionBarBase*)actionbarPointer;
                    var result = actionBar->HandleActionBar(i, action.ActionID);
                    if (result == null)
                    {
                        continue;
                    }
                    foreach (var (row, column) in result)
                    {
                        pluginLog.Warning($"Found action in row {row} column {column}");
                    }

                    lastHoveredKeyboardActions.AddRange(result);
                }

                // handle action cross bars
                foreach (var (addonName, location) in mappings)
                {
                    var actionCrossPointer = gameGUI.GetAddonByName(addonName);
                    var actionBar = (AddonActionCross*)actionCrossPointer;
                    var result = actionBar->AddonActionBarBase.HandleActionBar(location, action.ActionID);
                    if (result == null)
                    {
                        continue;
                    }
                    foreach (var (row, column) in result)
                    {
                        pluginLog.Warning($"Found action in row {row} column {column}");
                    }

                    lastHoveredActionsCross.AddRange(result);
                }
            }
        };
    }

    public IClientState ClientState { get; set; }
    public IPluginLog PluginLog { get; set; }
    
    public void Dispose()
    {
        tokenSource.Cancel();
    }

    private void DrawUI() => WindowSystem.Draw();
}
