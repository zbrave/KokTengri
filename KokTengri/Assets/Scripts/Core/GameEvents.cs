using System;
using UnityEngine;

namespace KokTengri.Core
{
    // ─────────────────────────────────────────────────────────
    //  EXISTING EVENTS (preserved)
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct PlayerDamagedEvent
    {
        public PlayerDamagedEvent(int damageAmount, int currentHp, int maxHp, int sourceId, float runTime)
        {
            DamageAmount = damageAmount;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            SourceId = sourceId;
            RunTime = runTime;
        }

        public int DamageAmount;
        public int CurrentHp;
        public int MaxHp;
        public int SourceId;
        public float RunTime;
    }

    [Serializable]
    public struct XPCollectedEvent
    {
        public XPCollectedEvent(int amount, int collectorId, Vector3 position, float runTime)
        {
            Amount = amount;
            CollectorId = collectorId;
            Position = position;
            RunTime = runTime;
        }

        public int Amount;
        public int CollectorId;
        public Vector3 Position;
        public float RunTime;
    }

    public enum RunEndResultType
    {
        Unknown = 0,
        Victory = 1,
        Defeat = 2,
        Quit = 3,
    }

    [Serializable]
    public struct RunEndEvent
    {
        public RunEndEvent(int runId, RunEndResultType result, float survivedSeconds, int kills, int bossesDefeated)
        {
            RunId = runId;
            Result = result;
            SurvivedSeconds = survivedSeconds;
            Kills = kills;
            BossesDefeated = bossesDefeated;
        }

        public int RunId;
        public RunEndResultType Result;
        public float SurvivedSeconds;
        public int Kills;
        public int BossesDefeated;
    }

    // ─────────────────────────────────────────────────────────
    //  RUN LIFECYCLE EVENTS
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct RunStartEvent
    {
        public RunStartEvent(int runId, string heroId, string classId, int seed)
        {
            RunId = runId;
            HeroId = heroId;
            ClassId = classId;
            Seed = seed;
        }

        public int RunId;
        public string HeroId;
        public string ClassId;
        public int Seed;
    }

    [Serializable]
    public struct RunPauseEvent
    {
        public RunPauseEvent(bool isPaused, float runTime)
        {
            IsPaused = isPaused;
            RunTime = runTime;
        }

        public bool IsPaused;
        public float RunTime;
    }

    [Serializable]
    public struct RunTimerTickEvent
    {
        public RunTimerTickEvent(float elapsedSeconds)
        {
            ElapsedSeconds = elapsedSeconds;
        }

        public float ElapsedSeconds;
    }

    [Serializable]
    public struct HeroModeActivatedEvent
    {
        public HeroModeActivatedEvent(float runTime)
        {
            RunTime = runTime;
        }

        public float RunTime;
    }

    // ─────────────────────────────────────────────────────────
    //  PLAYER EVENTS
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct PlayerPositionEvent
    {
        public PlayerPositionEvent(Vector2 position, float runTime)
        {
            Position = position;
            RunTime = runTime;
        }

        public Vector2 Position;
        public float RunTime;
    }

    [Serializable]
    public struct PlayerMovementStateChangedEvent
    {
        public PlayerMovementStateChangedEvent(MovementState previousState, MovementState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }

        public MovementState PreviousState;
        public MovementState NewState;
    }

    // ─────────────────────────────────────────────────────────
    //  ENEMY EVENTS
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct EnemyDeathEvent
    {
        public EnemyDeathEvent(int enemyId, EnemyType enemyType, Vector3 position, bool isElite, float runTime)
        {
            EnemyId = enemyId;
            EnemyType = enemyType;
            Position = position;
            IsElite = isElite;
            RunTime = runTime;
        }

        public int EnemyId;
        public EnemyType EnemyType;
        public Vector3 Position;
        public bool IsElite;
        public float RunTime;
    }

    [Serializable]
    public struct BossSpawnedEvent
    {
        public BossSpawnedEvent(string bossId, Vector3 position, float runTime)
        {
            BossId = bossId;
            Position = position;
            RunTime = runTime;
        }

        public string BossId;
        public Vector3 Position;
        public float RunTime;
    }

    [Serializable]
    public struct BossDefeatedEvent
    {
        public BossDefeatedEvent(string bossId, float runTime)
        {
            BossId = bossId;
            RunTime = runTime;
        }

        public string BossId;
        public float RunTime;
    }

    // ─────────────────────────────────────────────────────────
    //  INVENTORY & CRAFTING EVENTS
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct ElementAddedEvent
    {
        public ElementAddedEvent(ElementType elementType, int slotIndex)
        {
            ElementType = elementType;
            SlotIndex = slotIndex;
        }

        public ElementType ElementType;
        public int SlotIndex;
    }

    [Serializable]
    public struct ElementRemovedEvent
    {
        public ElementRemovedEvent(ElementType elementType, int slotIndex)
        {
            ElementType = elementType;
            SlotIndex = slotIndex;
        }

        public ElementType ElementType;
        public int SlotIndex;
    }

    [Serializable]
    public struct InventoryFullEvent
    {
        public InventoryFullEvent(ElementType rejectedElement)
        {
            RejectedElement = rejectedElement;
        }

        public ElementType RejectedElement;
    }

    [Serializable]
    public struct SpellCraftedEvent
    {
        public SpellCraftedEvent(string spellId, int level, SpellKind kind)
        {
            SpellId = spellId;
            Level = level;
            Kind = kind;
        }

        public string SpellId;
        public int Level;
        public SpellKind Kind;
    }

    [Serializable]
    public struct SpellUpgradedEvent
    {
        public SpellUpgradedEvent(string spellId, int newLevel)
        {
            SpellId = spellId;
            NewLevel = newLevel;
        }

        public string SpellId;
        public int NewLevel;
    }

    // ─────────────────────────────────────────────────────────
    //  WAVE EVENTS
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct WaveCompletedEvent
    {
        public WaveCompletedEvent(int waveIndex, int remainingEnemies, float runTime)
        {
            WaveIndex = waveIndex;
            RemainingEnemies = remainingEnemies;
            RunTime = runTime;
        }

        public int WaveIndex;
        public int RemainingEnemies;
        public float RunTime;
    }

    // ─────────────────────────────────────────────────────────
    //  XP & LEVELING EVENTS
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct LevelUpEvent
    {
        public LevelUpEvent(int newLevel, float overflowXp, float runTime)
        {
            NewLevel = newLevel;
            OverflowXp = overflowXp;
            RunTime = runTime;
        }

        public int NewLevel;
        public float OverflowXp;
        public float RunTime;
    }

    // ─────────────────────────────────────────────────────────
    //  SPELL EFFECT EVENTS
    // ─────────────────────────────────────────────────────────

    [Serializable]
    public struct SpellEffectActivatedEvent
    {
        public SpellEffectActivatedEvent(string spellId, Vector3 position)
        {
            SpellId = spellId;
            Position = position;
        }

        public string SpellId;
        public Vector3 Position;
    }

    [Serializable]
    public struct SpellEffectHitEvent
    {
        public SpellEffectHitEvent(string spellId, int targetId, int damage)
        {
            SpellId = spellId;
            TargetId = targetId;
            Damage = damage;
        }

        public string SpellId;
        public int TargetId;
        public int Damage;
    }

    // ─────────────────────────────────────────────────────────
    //  EVENT CHANNEL SCRIPTABLE OBJECTS
    // ─────────────────────────────────────────────────────────

    [CreateAssetMenu(fileName = "PlayerDamagedEventChannel", menuName = "KokTengri/Events/Player Damaged Channel")]
    public sealed class PlayerDamagedEventChannelSO : EventChannelSO<PlayerDamagedEvent>
    {
    }

    [CreateAssetMenu(fileName = "XPCollectedEventChannel", menuName = "KokTengri/Events/XP Collected Channel")]
    public sealed class XPCollectedEventChannelSO : EventChannelSO<XPCollectedEvent>
    {
    }

    [CreateAssetMenu(fileName = "RunEndEventChannel", menuName = "KokTengri/Events/Run End Channel")]
    public sealed class RunEndEventChannelSO : EventChannelSO<RunEndEvent>
    {
    }

    [CreateAssetMenu(fileName = "RunStartEventChannel", menuName = "KokTengri/Events/Run Start Channel")]
    public sealed class RunStartEventChannelSO : EventChannelSO<RunStartEvent>
    {
    }

    [CreateAssetMenu(fileName = "RunPauseEventChannel", menuName = "KokTengri/Events/Run Pause Channel")]
    public sealed class RunPauseEventChannelSO : EventChannelSO<RunPauseEvent>
    {
    }

    [CreateAssetMenu(fileName = "EnemyDeathEventChannel", menuName = "KokTengri/Events/Enemy Death Channel")]
    public sealed class EnemyDeathEventChannelSO : EventChannelSO<EnemyDeathEvent>
    {
    }

    [CreateAssetMenu(fileName = "LevelUpEventChannel", menuName = "KokTengri/Events/Level Up Channel")]
    public sealed class LevelUpEventChannelSO : EventChannelSO<LevelUpEvent>
    {
    }

    [CreateAssetMenu(fileName = "SpellCraftedEventChannel", menuName = "KokTengri/Events/Spell Crafted Channel")]
    public sealed class SpellCraftedEventChannelSO : EventChannelSO<SpellCraftedEvent>
    {
    }

    [CreateAssetMenu(fileName = "SpellUpgradedEventChannel", menuName = "KokTengri/Events/Spell Upgraded Channel")]
    public sealed class SpellUpgradedEventChannelSO : EventChannelSO<SpellUpgradedEvent>
    {
    }

    [CreateAssetMenu(fileName = "WaveCompletedEventChannel", menuName = "KokTengri/Events/Wave Completed Channel")]
    public sealed class WaveCompletedEventChannelSO : EventChannelSO<WaveCompletedEvent>
    {
    }

    [CreateAssetMenu(fileName = "BossDefeatedEventChannel", menuName = "KokTengri/Events/Boss Defeated Channel")]
    public sealed class BossDefeatedEventChannelSO : EventChannelSO<BossDefeatedEvent>
    {
    }
}
