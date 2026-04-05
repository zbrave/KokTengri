namespace KokTengri.Core
{
    /// <summary>
    /// The five cosmic elements of Turkic cosmology used in spell crafting.
    /// </summary>
    public enum ElementType
    {
        Od = 0,
        Sub = 1,
        Yer = 2,
        Yel = 3,
        Temur = 4,
    }

    /// <summary>
    /// Run lifecycle states managed by Run Manager.
    /// </summary>
    public enum RunLifecycleState
    {
        Uninitialized = 0,
        Starting = 1,
        Active = 2,
        Paused = 3,
        Ending = 4,
        Ended = 5,
    }

    /// <summary>
    /// Player movement states for the locomotion state machine.
    /// </summary>
    public enum MovementState
    {
        Idle = 0,
        Moving = 1,
        Knockback = 2,
        Invincible = 3,
        Frozen = 4,
        AFKAutoMove = 5,
    }

    /// <summary>
    /// Enemy type identifiers for data lookups and affinity tables.
    /// </summary>
    public enum EnemyType
    {
        KaraKurt = 0,
        YekUsagi = 1,
        Albasti = 2,
        Cor = 3,
        DemirciCin = 4,
        GolAynasi = 5,
    }

    /// <summary>
    /// Enemy health lifecycle states per-enemy.
    /// </summary>
    public enum EnemyHealthState
    {
        Alive = 0,
        TakingDamage = 1,
        Dying = 2,
        DeathCleanup = 3,
        ReturnedToPool = 4,
    }

    /// <summary>
    /// Wave Manager pacing states.
    /// </summary>
    public enum WaveState
    {
        Inactive = 0,
        Spawning = 1,
        BossEncounter = 2,
    }

    /// <summary>
    /// Spell activation type categories.
    /// </summary>
    public enum SpellKind
    {
        Orbit = 0,
        Projectile = 1,
        AoE = 2,
        Aura = 3,
        Passive = 4,
    }

    /// <summary>
    /// Result of spell crafting evaluation for tooltip display.
    /// </summary>
    public enum CraftingResultType
    {
        NewSpell = 0,
        UpgradeSpell = 1,
        AddToInventory = 2,
        BlockedByFullSlots = 3,
        InventoryFullNoMatch = 4,
    }

    /// <summary>
    /// Hero class identifiers for class bonus rules.
    /// </summary>
    public enum HeroClass
    {
        None = 0,
        Kam = 1,
        Batur = 2,
        Mergen = 3,
        Otaci = 4,
    }

    /// <summary>
    /// Level-up selection resolution types.
    /// </summary>
    public enum LevelUpResolutionType
    {
        ElementSelected = 0,
        DiscardElement = 1,
        StatBoostHP = 2,
        StatBoostSpeed = 3,
        StatBoostDamage = 4,
    }
}
