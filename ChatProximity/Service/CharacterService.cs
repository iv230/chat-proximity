using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace ChatProximity.Service;

public static class CharacterService
{
    /// <summary>
    /// Retrieves the local player's BattleChara pointer.
    /// </summary>
    /// <returns>
    /// A pointer to the local player's <c>BattleChara</c> structure, or null if the local player is not available.
    /// </returns>
    public static unsafe BattleChara* GetLocalPlayer()
    {
        return (BattleChara*)(ChatProximity.ClientState.LocalPlayer?.Address ?? 0);
    }

    /// <summary>
    /// Retrieves the BattleChara pointer of the currently focused target, if it is a player character.
    /// </summary>
    /// <returns>
    /// A pointer to the focused target's <c>BattleChara</c> structure if the target is a player character,
    /// or null if no valid focus target exists or the focus target is not a player character.
    /// </returns>
    public static unsafe BattleChara* GetFocusedBattleChara()
    {
        var focused = TargetSystem.Instance()->FocusTarget;
        if (focused == null)
            return null;

        if (focused->ObjectKind is ObjectKind.Pc)
            return (BattleChara*)focused;

        return null;
    }
}
