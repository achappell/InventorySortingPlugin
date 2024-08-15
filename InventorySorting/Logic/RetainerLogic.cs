using CriticalCommonLib;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace InventorySorting.Logic
{
    internal class RetainerLogic
    {
        internal static bool GenericThrottle => EzThrottler.Throttle("RetainerInfoThrottler", 100);
        private static IPlayerCharacter? LocalPlayer = Service.ClientState.LocalPlayer;
        internal static IGameObject? GetReachableRetainerBell()
        {
            foreach (var x in Service.Objects)
            {
                if ((x.ObjectKind == ObjectKind.Housing || x.ObjectKind == ObjectKind.EventObj) && x.Name.ToString().EqualsIgnoreCaseAny(BellName, "リテイナーベル") && LocalPlayer != null)
                {
                    if (Vector3.Distance(x.Position, LocalPlayer.Position) < GetValidInteractionDistance(x) && x.IsTargetable)
                    {
                        return x;
                    }
                }
            }
            return null;
        }

        internal static string? BellName
        {
            get => Service.Data.GetExcelSheet<EObjName>()?.GetRow(2000401)?.Singular.ToString();
        }

        internal static float GetValidInteractionDistance(IGameObject bell)
        {
            if (bell.ObjectKind == ObjectKind.Housing)
            {
                return 6.5f;
            }
            else if (Inns.List.Contains(Service.ClientState.TerritoryType))
            {
                return 4.75f;
            }
            else
            {
                return 4.6f;
            }
        }
        internal unsafe static bool? InteractWithTargetedBell()
        {
            if (Service.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedSummoningBell]) return true;
            var x = Service.Targets.Target;
            if (x != null && (x.ObjectKind == ObjectKind.Housing || x.ObjectKind == ObjectKind.EventObj) && x.Name.ToString().EqualsIgnoreCaseAny(BellName, "リテイナーベル") && LocalPlayer != null)
            {
                if (Vector3.Distance(x.Position, LocalPlayer.Position) < GetValidInteractionDistance(x) && x.IsTargetable)
                {
                    if (GenericThrottle && EzThrottler.Throttle("InteractWithBell", 5000))
                    {
                        TargetSystem.Instance()->InteractWithObject((GameObject*)x.Address, false);
                        Service.Log.Debug($"Interacted with {x}");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
