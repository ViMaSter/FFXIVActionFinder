#nullable disable
using System;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SkillFinder.Extensions;

public static class AddonActionCrossExtensions
{
    public enum CrossBars
    {
        Cross,
        DoubleCrossL,
        DoubleCrossR
    }
    
    public enum State : byte
    {
        None = 0,
        Highlighted = 128,
    }
    
    private static unsafe void SetActionBarSlot(AtkComponentNode* icon, State state)
    {
        icon->AtkResNode.AddRed = (byte) state;
        icon->AtkResNode.AddRed_2 = (byte) state;
        icon->AtkResNode.AddGreen = (byte) state;
        icon->AtkResNode.AddGreen_2 = (byte) state;
        icon->AtkResNode.AddBlue = (byte) state;
        icon->AtkResNode.AddBlue_2 = (byte) state;
    }

    private static unsafe void CleanupHighlights(AddonActionBarBase actionBar, int column)
    {
        var icon = actionBar.ActionBarSlotVector[column].Icon;
        SetActionBarSlot(icon, State.None);
    }
    

    public static void CleanupHighlights(IGameGui gameGUI, List<Tuple<int, int>> lastHoveredKeyboardActions, List<Tuple<CrossBars, int>> lastHoveredActionsCross)
    {
        foreach (var (lastHoveredActionColumn, lastHoveredActionRow) in lastHoveredKeyboardActions)
        {
            var indexString = lastHoveredActionRow == 0 ? "" : lastHoveredActionRow.ToString("D2");
            var actionbarPointer = gameGUI.GetAddonByName($"_ActionBar{indexString}");
            unsafe
            {
                var actionBar = (AddonActionBarBase*) actionbarPointer;
                CleanupHighlights(*actionBar, lastHoveredActionColumn);
            }
        }
        foreach (var (lastHoveredActionCrossBar, lastHoveredActionColumn) in lastHoveredActionsCross)
        {
            var actionCrossPointer = gameGUI.GetAddonByName("_Action"+lastHoveredActionCrossBar);
            unsafe
            {
                var actionBar = (AddonActionCross*) actionCrossPointer;
                CleanupHighlights(actionBar->AddonActionBarBase, lastHoveredActionColumn);
            }
        }
        lastHoveredKeyboardActions.Clear();             
        lastHoveredActionsCross.Clear();
    }
    
    public static unsafe List<Tuple<CrossBars, int>> HandleActionBar(this AddonActionBarBase actionBar, CrossBars crossBars, uint actionID)
    {
        var result = new List<Tuple<CrossBars, int>>();
        for (long j = 0; j < actionBar.ActionBarSlotVector.LongCount; j++)
        {
            var actionId = actionBar.ActionBarSlotVector[j].ActionId;
            if (actionId == actionID)
            {
                SetActionBarSlot(actionBar.ActionBarSlotVector[j].Icon, State.Highlighted);
                result.Add(new Tuple<CrossBars, int>(crossBars, (int)j));
            }
        }
    
        return result;
    }
    
    public static unsafe List<Tuple<int, int>> HandleActionBar(this AddonActionBarBase actionBar, int row, uint actionID)
    {
        var result = new List<Tuple<int, int>>();
        for (long j = 0; j < actionBar.ActionBarSlotVector.LongCount; j++)
        {
            var actionId = actionBar.ActionBarSlotVector[j].ActionId;
            if (actionId == actionID)
            {
                SetActionBarSlot(actionBar.ActionBarSlotVector[j].Icon, State.Highlighted);
                result.Add(new Tuple<int, int>((int)j, row));
            }
        }
    
        return result;
    }
}
