using UnityEngine;

// Anything the AI enemy can watch (your real Player, or the fake training player)
// must give us these 4 simple facts.
public interface ICombatant
{
    // Where the fighter is standing.
    Transform CombatTransform { get; }

    // 0 = not attacking, 1-3 = sword attack number, 4 = kick.
    int AttackType { get; }

    bool IsBlocking { get; }

    bool IsDodging { get; }
}

// Types of actions we remember in the short history.
public enum CombatActionType
{
    None = 0,
    Attack1 = 1,
    Attack2 = 2,
    Attack3 = 3,
    Kick = 4,
    Block = 5,
    Dodge = 6
}
