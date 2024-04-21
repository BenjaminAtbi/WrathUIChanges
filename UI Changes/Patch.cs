using Kingmaker;
using Kingmaker.PubSubSystem;
using Kingmaker.View;
using Kingmaker.Visual;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.MVVM._VM.ActionBar;
using HarmonyLib;
using UnityEngine;
using Kingmaker.Utility;
using Kingmaker.Blueprints;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System;
using UniRx;


namespace UIChanges
{
    [HarmonyPatch(typeof(UnitEntityView), nameof(UnitEntityView.UpdateHighlight))]
    internal static class UnitEntityView_UpdateHighlight_Patch
    {
        static bool Prefix(UnitEntityView __instance, ref UnitMultiHighlight ___m_Highlighter, bool raiseEvent = true)
        {
            if (!Main.Enabled || __instance.EntityData == null) return true;

            if (!__instance.MouseHighlighted && !__instance.DragBoxHighlighted && !__instance.EntityData.Descriptor.State.IsDead && !__instance.EntityData.IsPlayersEnemy &&
                __instance.EntityData.IsPlayerFaction)
            {
                ___m_Highlighter.BaseColor = Color.clear;

                if (raiseEvent)
                {
                    EventBus.RaiseEvent<IUnitHighlightUIHandler>(delegate (IUnitHighlightUIHandler h)
                    {
                        h.HandleHighlightChange(__instance);
                    });
                }
                return false;
            }
            return true;
        }
    }

    //[HarmonyPatch(typeof(OvertipController), nameof(OvertipController.NeedName))]
    //internal static class OvertipController_NeedName_Patch
    //{
    //    //static bool Prefix(OvertipController __instance, ref UnitEntityData _unit, ref bool __result)
    //    static bool Prefix(OvertipController __instance, ref bool __result)
    //    {
    //        if (!Main.Enabled) return true;


    //        if (__instance.Unit.IsPlayerFaction)
    //        {
    //            bool flag = SettingsRoot.Game.CombatTexts.ShowNamesForParty == ConditionType.Never;
    //            __result = __instance.ObjectIsHovered && !flag;
    //            return false;
    //        }
    //        return true;
    //    }
    //}


    public static class UnitSpellLevels
    {
        public static Dictionary<int, int> saved = new Dictionary<int, int>();

        public static UnitEntityData? SelectedUnit;

    }

    [HarmonyPatch(typeof(ActionBarVM), MethodType.Constructor)]
    public static class ActionBarVM_Patch
    {

        static void Postfix(ActionBarVM __instance)
        {

            __instance.CurrentSpellLevel.Subscribe(delegate (int value)
            {
                if(UnitSpellLevels.SelectedUnit != null)
                {
                    //Main.DebugLog($"lvl change: {UnitSpellLevels.SelectedUnit.CharacterName} {UnitSpellLevels.SelectedUnit.GetHashCode()} {value}");
                    UnitSpellLevels.saved[UnitSpellLevels.SelectedUnit.GetHashCode()] = value;
                }
            });
        }
    }

    [HarmonyPatch(typeof(ActionBarVM), nameof(ActionBarVM.OnUnitChanged))]
    internal static class ActionBarVM_OnUnitChanged_Patch
    {
        static int UnitChange(UnitEntityData unit) 
        {

            if (unit != null)
            {
                UnitSpellLevels.SelectedUnit = unit;
                if (!UnitSpellLevels.saved.ContainsKey(unit.GetHashCode()))
                {
                    UnitSpellLevels.saved.Add(unit.GetHashCode(), 0);
                }
                //Main.DebugLog($"referencing data: {unit.CharacterName}, {UnitSpellLevels.saved.Get(unit.GetHashCode())}");
                return UnitSpellLevels.saved.Get(unit.GetHashCode());
            }
            return 0;
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnUnitChangedTranspile(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            foreach (var instruction in instructions)
            {
                if (!found && instruction.opcode == OpCodes.Ldc_I4_0)
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call,
                        SymbolExtensions.GetMethodInfo((UnitEntityData unit) => UnitChange(unit)));
                } else
                {
                    yield return instruction;
                }
            }
            if (!found) Main.DebugLog("cannot find Ldc_I4_0 in ActionBarVM.OnUnitChanged");
        }
    }
}
