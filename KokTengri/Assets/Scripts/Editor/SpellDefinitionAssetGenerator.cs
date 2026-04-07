using KokTengri.Core;
using UnityEditor;
using UnityEngine;

namespace KokTengri.Editor
{
    /// <summary>
    /// Unity Editor tool that generates SpellDefinitionSO .asset files
    /// for all 15 spell recipes defined in the game design.
    /// Menu: KokTengri &gt; Generate Spell Definition Assets
    /// </summary>
    public static class SpellDefinitionAssetGenerator
    {
        private const string OutputFolder = "Assets/Data/Spells";
        private const string AssetFilePrefix = "SpellDefinition_";

        [MenuItem("KokTengri/Generate Spell Definition Assets")]
        public static void GenerateAll()
        {
            CreateFolderStructure();

            SpellEntry[] spells = CreateSpellEntries();
            int created = 0;
            int updated = 0;

            foreach (SpellEntry entry in spells)
            {
                string assetPath = $"{OutputFolder}/{AssetFilePrefix}{entry.SpellId}.asset";
                bool exists = System.IO.File.Exists(assetPath);

                SpellDefinitionSO so;
                if (exists)
                {
                    so = AssetDatabase.LoadAssetAtPath<SpellDefinitionSO>(assetPath);
                    updated++;
                }
                else
                {
                    so = ScriptableObject.CreateInstance<SpellDefinitionSO>();
                    created++;
                }

                ApplySpellData(so, entry);

                if (!exists)
                {
                    AssetDatabase.CreateAsset(so, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(so);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[SpellDefinitionAssetGenerator] " +
                $"Created: {created}, Updated: {updated}, Total: {spells.Length}");
        }

        /// <summary>
        /// Applies spell data to a SpellDefinitionSO using SerializedObject
        /// to respect the private setters on auto-properties.
        /// </summary>
        private static void ApplySpellData(SpellDefinitionSO so, SpellEntry entry)
        {
            var serializedObject = new SerializedObject(so);

            SetBackingField(serializedObject, nameof(SpellDefinitionSO.SpellId), entry.SpellId);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.DisplayName), entry.DisplayName);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.Kind), (int)entry.Kind);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.BaseDamage), entry.BaseDamage);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.MaxLevel), entry.MaxLevel);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.ElementA), (int)entry.ElementA);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.ElementB), (int)entry.ElementB);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.DamageScalingPerLevel), entry.DamageScalingPerLevel);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.CooldownSeconds), entry.CooldownSeconds);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.DurationSeconds), entry.DurationSeconds);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.Range), entry.Range);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.AreaRadius), entry.AreaRadius);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.ProjectileCount), entry.ProjectileCount);
            SetBackingField(serializedObject, nameof(SpellDefinitionSO.Speed), entry.Speed);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        // --- SerializedProperty helpers for [field: SerializeField] auto-properties ---

        private static void SetBackingField(SerializedObject obj, string propertyName, string value)
        {
            SerializedProperty prop = obj.FindProperty($"<{propertyName}>k__BackingField");
            if (prop != null) prop.stringValue = value;
        }

        private static void SetBackingField(SerializedObject obj, string propertyName, float value)
        {
            SerializedProperty prop = obj.FindProperty($"<{propertyName}>k__BackingField");
            if (prop != null) prop.floatValue = value;
        }

        private static void SetBackingField(SerializedObject obj, string propertyName, int value)
        {
            SerializedProperty prop = obj.FindProperty($"<{propertyName}>k__BackingField");
            if (prop != null) prop.intValue = value;
        }

        // --- Folder creation ---

        private static void CreateFolderStructure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }

            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "Spells");
            }
        }

        // --- 15 Spell Definitions ---
        // Values derived from design docs:
        //   - design/gdd/spell-effects.md (Alev Halkası, Kılıç Fırtınası, Kaya Kalkanı)
        //   - design/gdd/damage-calculator.md (damage formula, base damage range 5–25)
        //   - design/gdd/spell-crafting.md (recipes, SpellKind per spell)
        //   - design/gdd/spell-slot-manager.md (cooldown scaling rules)
        //
        // DamageScalingPerLevel is 0.25 for all spells (matches design doc level_multiplier formula).
        // MaxLevel is 5 for all spells.

        private static SpellEntry[] CreateSpellEntries()
        {
            return new SpellEntry[]
            {
                // --- Pure element (same + same) ---

                // Alev Halkası — Od+Od, Orbit (MVP-1)
                // Design: tickInterval 0.3s, baseRadius 0.5, orbitSpeed 30–360 deg/s
                new SpellEntry(
                    spellId: "alev_halkasi",
                    displayName: "Alev Halkası",
                    kind: SpellKind.Orbit,
                    baseDamage: 8f,
                    elementA: ElementType.Od,
                    elementB: ElementType.Od,
                    cooldownSeconds: 0.3f,
                    durationSeconds: 0f,
                    range: 2.0f,
                    areaRadius: 0.5f,
                    projectileCount: 1,
                    speed: 180f),

                // Şifa Pınarı — Sub+Sub, Passive
                // Healing fountain: periodic heal pulses
                new SpellEntry(
                    spellId: "sifa_pinari",
                    displayName: "Şifa Pınarı",
                    kind: SpellKind.Passive,
                    baseDamage: 5f,
                    elementA: ElementType.Sub,
                    elementB: ElementType.Sub,
                    cooldownSeconds: 2.0f,
                    durationSeconds: 0f,
                    range: 3.0f,
                    areaRadius: 2.0f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Kaya Kalkanı — Yer+Yer, Orbit (MVP-1)
                // Design: tickInterval 0.3s, baseRadius 0.5, orbitSpeed 20–300 deg/s
                new SpellEntry(
                    spellId: "kaya_kalkani",
                    displayName: "Kaya Kalkanı",
                    kind: SpellKind.Orbit,
                    baseDamage: 10f,
                    elementA: ElementType.Yer,
                    elementB: ElementType.Yer,
                    cooldownSeconds: 0.3f,
                    durationSeconds: 0f,
                    range: 2.0f,
                    areaRadius: 0.5f,
                    projectileCount: 1,
                    speed: 120f),

                // Rüzgar Koşusu — Yel+Yel, Aura
                // Movement speed aura, low damage, wide radius
                new SpellEntry(
                    spellId: "ruzgar_kosusu",
                    displayName: "Rüzgar Koşusu",
                    kind: SpellKind.Aura,
                    baseDamage: 5f,
                    elementA: ElementType.Yel,
                    elementB: ElementType.Yel,
                    cooldownSeconds: 0.5f,
                    durationSeconds: 0f,
                    range: 3.0f,
                    areaRadius: 2.0f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Demir Yağmuru — Temur+Temur, AoE
                // Heavy area bombardment
                new SpellEntry(
                    spellId: "demir_yagmuru",
                    displayName: "Demir Yağmuru",
                    kind: SpellKind.AoE,
                    baseDamage: 15f,
                    elementA: ElementType.Temur,
                    elementB: ElementType.Temur,
                    cooldownSeconds: 3.0f,
                    durationSeconds: 1.5f,
                    range: 6.0f,
                    areaRadius: 3.0f,
                    projectileCount: 1,
                    speed: 1.0f),

                // --- Mixed element combos ---

                // Kılıç Fırtınası — Od+Temur, Projectile (MVP-1)
                // Design: baseDamage 12, swordCountByLevel 1/1/2/3/4
                new SpellEntry(
                    spellId: "kilic_firtinasi",
                    displayName: "Kılıç Fırtınası",
                    kind: SpellKind.Projectile,
                    baseDamage: 12f,
                    elementA: ElementType.Od,
                    elementB: ElementType.Temur,
                    cooldownSeconds: 2.0f,
                    durationSeconds: 0f,
                    range: 6.0f,
                    areaRadius: 0.5f,
                    projectileCount: 3,
                    speed: 8.0f),

                // Buz Rüzgârı — Sub+Yel, AoE
                // Cold wind blast, moderate damage, slows enemies
                new SpellEntry(
                    spellId: "buz_ruzgari",
                    displayName: "Buz Rüzgârı",
                    kind: SpellKind.AoE,
                    baseDamage: 10f,
                    elementA: ElementType.Sub,
                    elementB: ElementType.Yel,
                    cooldownSeconds: 2.5f,
                    durationSeconds: 1.0f,
                    range: 5.0f,
                    areaRadius: 2.5f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Ok Yağmuru — Yel+Temur, AoE
                // Ranged arrow storm, wide area
                new SpellEntry(
                    spellId: "ok_yagmuru",
                    displayName: "Ok Yağmuru",
                    kind: SpellKind.AoE,
                    baseDamage: 12f,
                    elementA: ElementType.Yel,
                    elementB: ElementType.Temur,
                    cooldownSeconds: 3.0f,
                    durationSeconds: 2.0f,
                    range: 7.0f,
                    areaRadius: 4.0f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Buhar Patlaması — Od+Sub, AoE
                // Steam explosion, high burst, short duration
                new SpellEntry(
                    spellId: "buhar_patlamasi",
                    displayName: "Buhar Patlaması",
                    kind: SpellKind.AoE,
                    baseDamage: 18f,
                    elementA: ElementType.Od,
                    elementB: ElementType.Sub,
                    cooldownSeconds: 2.5f,
                    durationSeconds: 0.5f,
                    range: 4.0f,
                    areaRadius: 2.5f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Deprem — Yer+Temur, AoE
                // Earthquake, heavy damage, large area
                new SpellEntry(
                    spellId: "deprem",
                    displayName: "Deprem",
                    kind: SpellKind.AoE,
                    baseDamage: 15f,
                    elementA: ElementType.Yer,
                    elementB: ElementType.Temur,
                    cooldownSeconds: 3.5f,
                    durationSeconds: 1.5f,
                    range: 5.0f,
                    areaRadius: 3.5f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Ateş Kasırgası — Od+Yel, Projectile
                // Fire tornado, dual projectile, fast
                new SpellEntry(
                    spellId: "ates_kasirgasi",
                    displayName: "Ateş Kasırgası",
                    kind: SpellKind.Projectile,
                    baseDamage: 10f,
                    elementA: ElementType.Od,
                    elementB: ElementType.Yel,
                    cooldownSeconds: 1.8f,
                    durationSeconds: 0f,
                    range: 8.0f,
                    areaRadius: 1.0f,
                    projectileCount: 2,
                    speed: 7.0f),

                // Bataklık — Yer+Sub, AoE
                // Swamp zone, low damage, long duration CC area
                new SpellEntry(
                    spellId: "bataklik",
                    displayName: "Bataklık",
                    kind: SpellKind.AoE,
                    baseDamage: 6f,
                    elementA: ElementType.Yer,
                    elementB: ElementType.Sub,
                    cooldownSeconds: 3.0f,
                    durationSeconds: 3.0f,
                    range: 4.0f,
                    areaRadius: 3.0f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Lav Seli — Od+Yer, AoE
                // Lava flow, moderate damage, medium range
                new SpellEntry(
                    spellId: "lav_seli",
                    displayName: "Lav Seli",
                    kind: SpellKind.AoE,
                    baseDamage: 14f,
                    elementA: ElementType.Od,
                    elementB: ElementType.Yer,
                    cooldownSeconds: 2.5f,
                    durationSeconds: 1.0f,
                    range: 6.0f,
                    areaRadius: 2.5f,
                    projectileCount: 1,
                    speed: 1.0f),

                // Buz Kılıcı — Sub+Temur, Projectile
                // Ice blade, high damage dual projectile, fast
                new SpellEntry(
                    spellId: "buz_kilici",
                    displayName: "Buz Kılıcı",
                    kind: SpellKind.Projectile,
                    baseDamage: 14f,
                    elementA: ElementType.Sub,
                    elementB: ElementType.Temur,
                    cooldownSeconds: 1.5f,
                    durationSeconds: 0f,
                    range: 7.0f,
                    areaRadius: 0.5f,
                    projectileCount: 2,
                    speed: 10.0f),

                // Kum Fırtınası — Yer+Yel, AoE
                // Sandstorm, low damage, wide persistent area
                new SpellEntry(
                    spellId: "kum_firtinasi",
                    displayName: "Kum Fırtınası",
                    kind: SpellKind.AoE,
                    baseDamage: 8f,
                    elementA: ElementType.Yer,
                    elementB: ElementType.Yel,
                    cooldownSeconds: 2.5f,
                    durationSeconds: 2.0f,
                    range: 5.0f,
                    areaRadius: 3.0f,
                    projectileCount: 1,
                    speed: 1.0f),
            };
        }

        /// <summary>
        /// Data container for a single spell definition.
        /// Maps 1:1 with SpellDefinitionSO fields.
        /// </summary>
        private readonly struct SpellEntry
        {
            public readonly string SpellId;
            public readonly string DisplayName;
            public readonly SpellKind Kind;
            public readonly float BaseDamage;
            public readonly int MaxLevel;
            public readonly ElementType ElementA;
            public readonly ElementType ElementB;
            public readonly float DamageScalingPerLevel;
            public readonly float CooldownSeconds;
            public readonly float DurationSeconds;
            public readonly float Range;
            public readonly float AreaRadius;
            public readonly int ProjectileCount;
            public readonly float Speed;

            public SpellEntry(
                string spellId,
                string displayName,
                SpellKind kind,
                float baseDamage,
                ElementType elementA,
                ElementType elementB,
                float cooldownSeconds,
                float durationSeconds,
                float range,
                float areaRadius,
                int projectileCount,
                float speed)
            {
                SpellId = spellId;
                DisplayName = displayName;
                Kind = kind;
                BaseDamage = baseDamage;
                MaxLevel = 5;
                ElementA = elementA;
                ElementB = elementB;
                DamageScalingPerLevel = 0.25f;
                CooldownSeconds = cooldownSeconds;
                DurationSeconds = durationSeconds;
                Range = range;
                AreaRadius = areaRadius;
                ProjectileCount = projectileCount;
                Speed = speed;
            }
        }
    }
}
