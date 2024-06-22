using System;
using System.Collections.Generic;
using System.Threading;
using SkillFinder.Windows;
using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SkillFinder;

#nullable disable

// ReSharper disable once UnusedType.Global - Dalamud plugin entry point
public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/gil";

    private DalamudPluginInterface PluginInterface { get; init; }
    private ICommandManager CommandManager { get; init; }
    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SkillFinder");
    private MainWindow MainWindow { get; init; }
    
    private readonly CancellationTokenSource tokenSource = new();

    enum CrossBars
    {
        Main,
        DoubleL,
        DoubleR
    }
    
    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] ICommandManager commandManager,
        [RequiredVersion("1.0")] IPluginLog pluginLog,
        [RequiredVersion("1.0")] IFramework framework,
        [RequiredVersion("1.0")] IClientState clientState,
        [RequiredVersion("1.0")] IAddonLifecycle addonLifecycle,
        [RequiredVersion("1.0")] ISigScanner sigScanner,
        [RequiredVersion("1.0")] IGameGui gameGUI
            )
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
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

        var lastHoveredActions = new List<Tuple<int, int>>();
        var lastHoveredActionsCross = new List<Tuple<CrossBars, int>>();
        var showHighlight = false;
        
        addonLifecycle.RegisterListener(AddonEvent.PreSetup, "ActionMenu", (type, args) =>
        {
            showHighlight = true;
        });
        addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "ActionMenu", (type, args) =>
        {
            showHighlight = false;
            
            foreach (var (lastHoveredActionColumn, lastHoveredActionRow) in lastHoveredActions)
            {
                var indexString = lastHoveredActionRow == 0 ? "" : lastHoveredActionRow.ToString("D2");
                var actionbarPointer = gameGUI.GetAddonByName($"_ActionBar{indexString}");
                unsafe
                {
                    var actionBar = (AddonActionBarBase*) actionbarPointer;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                }
            }
            foreach (var (lastHoveredActionCrossBar, lastHoveredActionColumn) in lastHoveredActionsCross)
            {
                var actioncrossPointer = gameGUI.GetAddonByName($"_ActionCross");
                var actiondoublecrosslPointer = gameGUI.GetAddonByName($"_ActionDoubleCrossL");
                var actiondoublecrossrPointer = gameGUI.GetAddonByName($"_ActionDoubleCrossR");
                unsafe
                {
                    if (lastHoveredActionCrossBar == CrossBars.Main)
                    {
                        var actionBar = (AddonActionCross*) actioncrossPointer;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                    }
                    else if (lastHoveredActionCrossBar == CrossBars.DoubleL)
                    {
                        var actionBar = (AddonActionCross*) actiondoublecrosslPointer;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                    }
                    else if (lastHoveredActionCrossBar == CrossBars.DoubleR)
                    {
                        var actionBar = (AddonActionCross*) actiondoublecrossrPointer;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                    }
                }
            }
            lastHoveredActions.Clear();             
            lastHoveredActionsCross.Clear();
        });
        
        gameGUI.HoveredActionChanged += (sender, action) =>
        {
            if (!showHighlight)
            {
                return;
            }
            
            foreach (var (lastHoveredActionColumn, lastHoveredActionRow) in lastHoveredActions)
            {
                var indexString = lastHoveredActionRow == 0 ? "" : lastHoveredActionRow.ToString("D2");
                var actionbarPointer = gameGUI.GetAddonByName($"_ActionBar{indexString}");
                unsafe
                {
                    var actionBar = (AddonActionBarBase*) actionbarPointer;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                    actionBar->ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                }
            }
            foreach (var (lastHoveredActionCrossBar, lastHoveredActionColumn) in lastHoveredActionsCross)
            {
                var actioncrossPointer = gameGUI.GetAddonByName($"_ActionCross");
                var actiondoublecrosslPointer = gameGUI.GetAddonByName($"_ActionDoubleCrossL");
                var actiondoublecrossrPointer = gameGUI.GetAddonByName($"_ActionDoubleCrossR");
                unsafe
                {
                    if (lastHoveredActionCrossBar == CrossBars.Main)
                    {
                        var actionBar = (AddonActionCross*) actioncrossPointer;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                    }
                    else if (lastHoveredActionCrossBar == CrossBars.DoubleL)
                    {
                        var actionBar = (AddonActionCross*) actiondoublecrosslPointer;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                    }
                    else if (lastHoveredActionCrossBar == CrossBars.DoubleR)
                    {
                        var actionBar = (AddonActionCross*) actiondoublecrossrPointer;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddRed_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddGreen_2 = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue = 0;
                        actionBar->ActionBarBase.ActionBarSlotVector.Get((ulong) lastHoveredActionColumn).Icon->AtkResNode.AddBlue_2 = 0;
                    }
                }
            }
            lastHoveredActions.Clear();             
            lastHoveredActionsCross.Clear();
            
            // handle keyboard cross bar
            pluginLog.Warning(action.ActionID.ToString());
            for (var i = 0; i < 10; i++)
            {
                var indexString = i == 0 ? "" : i.ToString("D2");
                var actionbarPointer = gameGUI.GetAddonByName($"_ActionBar{indexString}");
                unsafe
                {
                    var actionBar = (AddonActionBarBase*) actionbarPointer;
                    for (ulong j = 0; j < actionBar->ActionBarSlotVector.Size(); j++)
                    {
                        var actionId = actionBar->ActionBarSlotVector.Get(j).ActionId;
                        if (actionId == action.ActionID)
                        {
                            lastHoveredActions.Add(new Tuple<int, int>((int) j, i));
                            actionBar->ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed = 128;
                            actionBar->ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed_2 = 128;
                            actionBar->ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen = 128;
                            actionBar->ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen_2 = 128;
                            actionBar->ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue = 128;
                            actionBar->ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue_2 = 128;
                            pluginLog.Warning($"Found action {actionId} in actionbar {i}, slot {j}");
                        }
                    }
                }
            }
            
            // handle controller cross bar
            {
                // _ActionCross
                // _ActionDoubleCrossL
                // _ActionDoubleCrossR
                
                var actioncrossPointer = gameGUI.GetAddonByName($"_ActionCross");
                unsafe
                {
                    var actionBar = (AddonActionCross*) actioncrossPointer;
                    
                    for (ulong j = 0; j < actionBar->ActionBarBase.ActionBarSlotVector.Size(); j++)
                    {
                        var actionId = actionBar->ActionBarBase.ActionBarSlotVector.Get(j).ActionId;
                        if (actionId == action.ActionID)
                        {
                            lastHoveredActionsCross.Add(new Tuple<CrossBars, int>(CrossBars.Main, (int) j));
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed_2 = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen_2 = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue_2 = 128;
                            pluginLog.Warning($"Found action {actionId} in actionbar 10, slot {j}");
                        }
                    }
                }
                
                var actiondoublecrosslPointer = gameGUI.GetAddonByName($"_ActionDoubleCrossL");
                unsafe
                {
                    var actionBar = (AddonActionCross*) actiondoublecrosslPointer;
                    
                    for (ulong j = 0; j < actionBar->ActionBarBase.ActionBarSlotVector.Size(); j++)
                    {
                        var actionId = actionBar->ActionBarBase.ActionBarSlotVector.Get(j).ActionId;
                        if (actionId == action.ActionID)
                        {
                            lastHoveredActionsCross.Add(new Tuple<CrossBars, int>(CrossBars.DoubleL, (int) j));
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed_2 = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen_2 = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue_2 = 128;
                            pluginLog.Warning($"Found action {actionId} in actionbar 11, slot {j}");
                        }
                    }
                }
                
                var actiondoublecrossrPointer = gameGUI.GetAddonByName($"_ActionDoubleCrossR");
                unsafe
                {
                    var actionBar = (AddonActionCross*) actiondoublecrossrPointer;
                    
                    for (ulong j = 0; j < actionBar->ActionBarBase.ActionBarSlotVector.Size(); j++)
                    {
                        var actionId = actionBar->ActionBarBase.ActionBarSlotVector.Get(j).ActionId;
                        if (actionId == action.ActionID)
                        {
                            lastHoveredActionsCross.Add(new Tuple<CrossBars, int>(CrossBars.DoubleR, (int) j));
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddRed_2 = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddGreen_2 = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue = 128;
                            actionBar->ActionBarBase.ActionBarSlotVector.Get(j).Icon->AtkResNode.AddBlue_2 = 128;
                            pluginLog.Warning($"Found action {actionId} in actionbar 12, slot {j}");
                        }
                    }
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
