﻿
using AmongUs.GameOptions;

namespace TOHE.Roles.Impostor;

internal class Parasite : RoleBase
{
    //===========================SETUP================================\\
    private const int Id = 5900;
    private static readonly HashSet<byte> Playerids = [];
    public static bool HasEnabled => Playerids.Any();
    public override bool IsEnable => HasEnabled;
    public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
    //==================================================================\\

    private static OptionItem ParasiteCD;

    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Parasite, zeroOne: false);
        ParasiteCD = FloatOptionItem.Create(Id + 2, "KillCooldown", new(0f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Parasite])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void Init()
    {
        Playerids.Clear();
    }
    public override void Add(byte playerId)
    {
        Playerids.Add(playerId);
    }

    public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(true);
    public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = ParasiteCD.GetFloat();

    public override bool CanUseKillButton(PlayerControl pc) => true;
    public override bool CanUseImpostorVentButton(PlayerControl pc) => true;
    public override bool CanUseSabotage(PlayerControl pc) => true;
}
