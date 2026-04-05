using UnityEngine;

namespace KokTengri.Core
{
    [CreateAssetMenu(fileName = "SpellSlotConfig", menuName = "KokTengri/Data/Spell Slot Config")]
    public sealed class SpellSlotConfigSO : ScriptableObject
    {
        [field: SerializeField, Min(1)] public int MaxSpellSlots { get; private set; } = 6;

        [field: SerializeField, Min(1)] public int MaxSpellLevel { get; private set; } = 5;
    }
}
