﻿using AmongUs.GameOptions;
using TOHE.Roles.AddOns.Common;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor;

internal class Camouflager : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 2900;
    public static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Shapeshifter;
    //==================================================================\\

    private static OptionItem CamouflageCooldownOpt;
    private static OptionItem CamouflageDurationOpt;
    private static OptionItem CanUseCommsSabotagOpt;
    private static OptionItem DisableReportWhenCamouflageIsActiveOpt;
    private static OptionItem ShowShapeshiftAnimationsOpt;

    public static bool AbilityActivated = false;
    private static float CamouflageCooldown;
    private static float CamouflageDuration;
    private static bool CanUseCommsSabotage;
    private static bool DisableReportWhenCamouflageIsActive;

    private static Dictionary<byte, long> Timer = [];

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Camouflager);
        CamouflageCooldownOpt = FloatOptionItem.Create(Id + 2, "CamouflageCooldown", new(1f, 180f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CamouflageDurationOpt = FloatOptionItem.Create(Id + 4, "CamouflageDuration", new(1f, 180f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager])
            .SetValueFormat(OptionFormat.Seconds);
        CanUseCommsSabotagOpt = BooleanOptionItem.Create(Id + 6, "CanUseCommsSabotage", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        DisableReportWhenCamouflageIsActiveOpt = BooleanOptionItem.Create(Id + 8, "DisableReportWhenCamouflageIsActive", false, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);
        ShowShapeshiftAnimationsOpt = BooleanOptionItem.Create(Id + 9, "ShowShapeshiftAnimations", true, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Camouflager]);

    }
    public override void Init()
    {
        Timer.Clear();
        AbilityActivated = false;
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        CamouflageCooldown = CamouflageCooldownOpt.GetFloat();
        CamouflageDuration = CamouflageDurationOpt.GetFloat();
        CanUseCommsSabotage = CanUseCommsSabotagOpt.GetBool();
        DisableReportWhenCamouflageIsActive = DisableReportWhenCamouflageIsActiveOpt.GetBool();

        Playerids.Add(playerId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        AURoleOptions.ShapeshifterCooldown = ShowShapeshiftAnimationsOpt.GetBool() && AbilityActivated ? CamouflageDuration : CamouflageCooldown;
        AURoleOptions.ShapeshifterDuration = CamouflageDuration;
    }
    public override Sprite GetAbilityButtonSprite(PlayerControl player, bool shapeshifting) => CustomButton.Get("Camo");
    public override void SetAbilityButtonText(HudManager hud, byte playerId)
    {
        if (AbilityActivated)
            hud.AbilityButton.OverrideText(GetString("CamouflagerShapeshiftTextAfterDisguise"));
        else
            hud.AbilityButton.OverrideText(GetString("CamouflagerShapeshiftTextBeforeDisguise"));
    }
    public override bool OnCheckShapeshift(PlayerControl camouflager, PlayerControl target, ref bool resetCooldown, ref bool shouldAnimate)
    {
        if (ShowShapeshiftAnimationsOpt.GetBool() || camouflager.PlayerId == target.PlayerId) return true;

        if (AbilityActivated)
        {
            Logger.Info("Rejected bcz ability alredy activated", "Camouflager");
            return false;
        }

        if (!Main.MeetingIsStarted && GameStates.IsInTask)
        {
            AbilityActivated = true;
            camouflager.SyncSettings();

            Camouflage.CheckCamouflage();
            Timer.Add(camouflager.PlayerId, Utils.GetTimeStamp());

            Logger.Info("Camouflager use hidden shapeshift", "Camouflager");
        }

        return false;
    }
    public override void OnShapeshift(PlayerControl shapeshifter, PlayerControl target, bool IsAnimate, bool shapeshifting)
    {
        if (!shapeshifting)
        {
            ClearCamouflage();
            Timer = [];
            return;
        }

        AbilityActivated = true;

        var timer = 1.2f;

        _ = new LateTask(() =>
        {
            if (!Main.MeetingIsStarted && GameStates.IsInTask)
            {
                Camouflage.CheckCamouflage();
            }
        }, timer, "Camouflager Use Shapeshift");
    }
    public override void OnReportDeadBody(PlayerControl reporter, PlayerControl target)
    {
        ClearCamouflage();
        Timer = [];
    }

    public override void OnMurderPlayerAsTarget(PlayerControl killer, PlayerControl target, bool inMeeting, bool isSuicide)
    {
        if (inMeeting || !AbilityActivated) return;

        ClearCamouflage();
    }

    public static bool CantPressCommsSabotageButton(PlayerControl player) => player.Is(CustomRoles.Camouflager) && !CanUseCommsSabotage;

    public override bool OnCheckReportDeadBody(PlayerControl reporter, GameData.PlayerInfo deadBody, PlayerControl killer)
    {
        if (deadBody.Object.Is(CustomRoles.Bait) && Bait.BaitCanBeReportedUnderAllConditions.GetBool()) return true;

        return DisableReportWhenCamouflageIsActive && AbilityActivated && !(Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool());
    }

    private static void ClearCamouflage()
    {
        AbilityActivated = false;
        Camouflage.CheckCamouflage();
    }
    public override void OnFixedUpdate(PlayerControl camouflager)
    {
        if (!ShowShapeshiftAnimationsOpt.GetBool() && !AbilityActivated) return;

        if (camouflager == null || !camouflager.IsAlive())
        {
            Timer.Remove(camouflager.PlayerId);
            ClearCamouflage();
            camouflager.SyncSettings();
            camouflager.RpcResetAbilityCooldown();
            return;
        }
        if (!Timer.TryGetValue(camouflager.PlayerId, out var oldTime)) return;

        var nowTime = Utils.GetTimeStamp();
        if (nowTime - oldTime >= CamouflageDuration)
        {
            Timer.Remove(camouflager.PlayerId);
            ClearCamouflage();
            camouflager.SyncSettings();
            camouflager.RpcResetAbilityCooldown();
        }
    }
}
