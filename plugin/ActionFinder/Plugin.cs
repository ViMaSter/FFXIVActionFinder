using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using ActionFinder.Extensions;

namespace ActionFinder;

#nullable disable

// ReSharper disable once UnusedType.Global - Dalamud plugin entry point
public sealed class Plugin : IDalamudPlugin
{
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
        IAddonLifecycle addonLifecycle,
        IGameGui gameGUI
            )
    {
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
            
            unsafe
            {
                // handle keyboard cross bar
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

                    lastHoveredActionsCross.AddRange(result);
                }
            }
        };
    }

    public void Dispose()
    {
        // nothing to dispose
    }
}
