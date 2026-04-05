# Game Pillars: Kök Tengri

## Document Status
- **Version**: 1.0
- **Last Updated**: 2026-04-04
- **Approved By**: zbrave (creative-director)
- **Status**: Approved

---

## What Are Game Pillars?

Pillars are the 3-5 non-negotiable principles that define this game's identity.
Every design, art, audio, narrative, and technical decision must serve at least
one pillar. If a feature doesn't serve a pillar, it doesn't belong in the game.

---

## Core Fantasy

> You are the last shaman of Tengri, wielding the five cosmic elements — Fire, Water, Earth, Wind, and Iron — to craft ancient spells and hold back the underworld armies of Erlik Han. No one else can do what you do. The elements answer to you alone.

---

## Target MDA Aesthetics

| Rank | Aesthetic | How Our Game Delivers It |
| ---- | ---- | ---- |
| 1 | **Challenge** | Escalating waves, boss mechanics every 5 min, build-dependent difficulty curve |
| 2 | **Fantasy** | Turkic shaman fantasy — cosmic elements, mythological creatures, ancestral magic |
| 3 | **Discovery** | 15 spell recipes to learn through play, hero/class synergy exploration |
| 4 | **Expression** | 15 spells × 6 heroes × 4 classes = infinite build variety |
| 5 | **Sensation** | Pixel art VFX filling the screen, satisfying spell effects, haptic feedback |
| N/A | **Fellowship** | Single-player game; post-MVP: leaderboards for indirect competition |
| N/A | **Narrative** | Mythology as flavor, not driver. No cutscenes, no dialogue trees. |
| N/A | **Submission** | Some meditative quality in the loop, but challenge is the dominant note |

---

## The Pillars

### Pillar 1: Every Element Matters

**One-Sentence Definition**: All 5 elements (Od, Sub, Yer, Yel, Temür) contribute equally to the spell crafting system — no element is universally dominant or useless across all build strategies.

**Target Aesthetics Served**: Challenge, Discovery, Expression

**Design Test**: "If we're debating a new spell recipe that makes Od (Fire) strictly better than all other elements in most situations, this pillar says redesign the spell to balance element value."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | Balance all 15 spells so no element's spells are collectively stronger. Enemy weaknesses must be distributed evenly across elements. | Kara Kurt is weak to Od but resistant to Yel; Yek Uşakları weak to Yel but resistant to Temür — every element has targets |
| **Art** | Each element needs equally compelling visual identity. No element's VFX should look like an afterthought. | Od (red fire), Sub (blue water), Yer (brown earth), Yel (white wind), Temür (grey iron) — all distinct, all visually satisfying |
| **Audio** | Each element needs a distinct sound signature. Spell sounds should make players excited about ANY element, not just fire. | Sub spells sound cool and flowing; Temür spells sound sharp and metallic — both satisfying |
| **Narrative** | In Turkic cosmology, all 5 elements are fundamental forces. No element is "the best" in the lore. | Destan pages celebrate each element's role in creation mythology equally |
| **Engineering** | The spell crafting system must support symmetrical recipe creation. No hardcoded special cases for specific elements. | SpellRecipe ScriptableObject uses element pair → spell mapping, not element-specific logic |

#### Serving This Pillar
- Enemy weakness/resistance table gives every element roughly equal coverage (2 weaknesses, 1 resistance per enemy type)
- Both Basic Spells (same element ×2) and Combined Spells (different elements) are viable paths
- Hero starting elements are distributed across all 5 elements

#### Violating This Pillar
- Adding a spell that makes one element's combined spells universally better (e.g., Od+X always strongest)
- Making one element's Basic Spell clearly dominant (e.g., Alev Halkası is best AoE, best single-target, best everything)
- Creating enemies that are weak to only 1-2 elements, making others feel useless

---

### Pillar 2: Death Teaches, Never Punishes

**One-Sentence Definition**: Every run — win or lose — provides meaningful permanent progression and clear information about what to try differently next time.

**Target Aesthetics Served**: Challenge, Discovery

**Design Test**: "If we're debating whether dying should cost half your gold vs keeping all of it, this pillar says keep all of it — death is a lesson, not a penalty."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | Meta-progression always moves forward. Run rewards scale with performance but never go to zero. Death screen shows useful stats. | Even a 2-minute death run earns gold for permanent upgrades. Run summary shows: "Your Kaya Kalkanı was level 1 — try upgrading Yer spells for more defense." |
| **Art** | Death screen is not demoralizing. It should feel like a warrior's reflection, not a failure screen. | Warm tones, ancestral spirit motif, encouraging text: "The ancestors learned from this battle." |
| **Audio** | Death sound is somber but not punishing. Music should motivate the next run. | Gentle drum beat, not a harsh game-over jingle. Transition to hopeful melody at run summary. |
| **Narrative** | Death is canonically part of the shaman's journey — consulting ancestors, learning from the spirit realm. | "Erlik Han's forces won this battle, but Tengri remembers your courage." |
| **Engineering** | Save system must reliably persist progression. No lost progress due to crashes. | Gold and unlocks saved immediately on earn, not batched at run end. JSON + encryption with backup. |

#### Serving This Pillar
- Gold formula: `(survived_minutes × 10) + (kills × 0.5) + (bosses × 100)` — even short runs earn meaningful gold
- Soul Stones from first-time boss kills persist permanently
- Meta-upgrades provide clear, felt power increase between runs (+5% HP per level, +3% damage, etc.)
- Run summary screen shows build composition and suggests improvements

#### Violating This Pillar
- Losing gold or items on death
- Runs that earn zero progression (e.g., "must survive 10 minutes to earn anything")
- Power caps that make early deaths feel like wasted time
- Ambiguous death causes (player doesn't understand why they died)

---

### Pillar 3: Mythology is Gameplay, Not Decoration

**One-Sentence Definition**: Turkic mythology and cosmology define game mechanics — element types, enemy behaviors, boss designs, spell names, and hero backstories are all rooted in the mythology and affect how the game plays.

**Target Aesthetics Served**: Fantasy, Discovery

**Design Test**: "If a new enemy type is proposed that has no connection to Turkic mythology, this pillar says find the myth connection or redesign it to fit the cosmological framework."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | All game systems should be explainable through Turkic cosmology. The 5 elements ARE the crafting system. Enemy behaviors reflect their mythological nature. | Çor'lar (corrupted earth spirits) split in half when killed — because they are fragmented earth, not just "a splitting enemy." Demirci Cinleri (underworld smiths) are armored because they forge iron. |
| **Art** | Visual design draws from Turkic nomadic art, petroglyphs, and folklore — not generic fantasy. | Enemy designs reference Turkic mythological descriptions: Tepegöz has one eye, Kara Kurt have shadow-wolf features, Albastılar match the folklore description of malevolent female spirits |
| **Audio** | Sound design incorporates Turkic instruments and musical traditions. Not European orchestral fantasy. | Bağlama, ney, davul in BGM. Spell sounds use metallic anvil strikes for Temür, wind instruments for Yel. |
| **Narrative** | Every entity in the game has a mythological source. Destan collection pages provide real folklore context. | Boss descriptions reference actual Turkic epics: Tepegöz from Dede Korkut, Erlik Han from Tengriism underworld mythology |
| **Engineering** | Data structures must support mythology-linked metadata (enemy lore, spell origins, element cosmology). | EnemyDefinitionSO includes `mythologyReference` field. Destan pages are unlockable lore entries. |

#### Serving This Pillar
- 5 elements map directly to Turkic cosmological elements (not the Western 4-element system)
- Each hero is inspired by a specific Turkic epic figure (Korkut from Dede Korkut, Ay Kağan from Oğuz Kağan, Börte from Ergenekon wolf legend)
- Boss mechanics reflect mythological nature: Tepegöz's eye is his weak point (one-eyed giant from Dede Korkut)
- Spell names use Turkic language (Alev Halkası, Kılıç Fırtınası, Buz Rüzgarı)

#### Violating This Pillar
- Adding a "generic dragon" boss that has no Turkic mythological source
- Using Western element systems (earth/air/fire/water) instead of Turkic 5 elements
- Spell names that don't connect to the mythology (e.g., "Fireball" instead of "Alev Halkası")
- Enemy designs that look like generic fantasy creatures without Turkic cultural reference

---

### Pillar 4: Build Diversity Over Build Power

**One-Sentence Definition**: The hero × class × spell system should create many viable strategies, not one optimal build — balance changes should nerf dominant builds rather than power-creep everything else.

**Target Aesthetics Served**: Expression, Challenge, Discovery

**Design Test**: "If analytics show one hero/class/spell combination has 60%+ win rate while others have 20%, this pillar says nerf the dominant build, don't buff the weak ones."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | Class bonuses are designed to make DIFFERENT builds optimal, not stack one element. Each class buffs cross-element categories to encourage diverse spell selection. | Batur buffs Sub/Yer spells (defensive), but if you pick Börte (Temür hero), your Temür spells aren't buffed — you're pushed toward cross-element play |
| **Art** | Visual feedback should celebrate ALL build types equally. No build should look boring or unfinished. | Defensive builds (Kaya Kalkanı + Şifa Pınarı) should look as visually spectacular as offensive builds (Alev Halkası + Kılıç Fırtınası) |
| **Audio** | Sound design should make every build type feel powerful. Support builds should sound satisfying too. | Healing spells have warm, resonant sounds. Defensive spells have deep, impactful tones. Not just "cool explosion sounds for DPS." |
| **Narrative** | Each hero's story implies a viable playstyle, not a power level. No hero is "the strongest" in lore. | Korkut is a young apprentice (accessible), Ay Kağan is a legendary leader (different strategy, not stronger), Börte is a wolf ancestor (aggressive but not dominant) |
| **Engineering** | Analytics must track build diversity metrics: win rate by hero, class, spell combination, element composition. | Telemetry events: hero_selected, class_selected, spells_crafted, run_duration, death_cause. Dashboard: build win rates over time. |

#### Serving This Pillar
- Class bonuses are designed to create OPPOSITE synergies: Börte (Temür) + Batur (Sub/Yer bonus) = cross-element synergy
- 15 spells ensure no single element pair dominates all scenarios
- Hero starting elements bias early crafting but don't lock out any spell
- Enemy weakness/resistance table rotates which elements are most effective by time

#### Violating This Pillar
- One class being strictly better for all heroes (e.g., Kam's extra choice always outperforms Batur's defense)
- Spell balance where 3-4 spells are clearly optimal and the rest are "trap choices"
- Hero power levels that make one hero the "correct" pick for progression
- Meta-upgrades that make one strategy permanently dominant

---

## Anti-Pillars (What This Game Is NOT)

- **NOT an open-world game**: The survivor-like format is arena-based. Open world would compromise the focused, escalating tension of the core loop and inflate scope beyond solo-dev capacity.
- **NOT pay-to-win**: Per the Fair Balance Rule (spec Section 7.5), IAP content offers different experiences, not power advantages. Heroes are different, not stronger. This is a non-negotiable trust contract with players.
- **NOT a story-heavy RPG**: Turkic mythology is flavor, theme, and mechanical inspiration — not a narrative driver. No dialogue trees, no branching story, no cutscenes. The "story" is the Destan collection you build by playing.
- **NOT a competitive PvP game**: The core loop is solo survival. Adding PvP would compromise the power fantasy and balancing (PvE balance ≠ PvP balance).

---

## Pillar Conflict Resolution

When two pillars conflict, use this priority order:

| Priority | Pillar | Rationale |
| ---- | ---- | ---- |
| 1 | **Death Teaches, Never Punishes** | Player retention is existential for a F2P mobile game. If players feel punished, they churn. This pillar protects the entire business model. |
| 2 | **Every Element Matters** | The crafting system IS the game's identity. If one element dominates, the discovery and expression aesthetics collapse. |
| 3 | **Build Diversity Over Build Power** | Balance is important but can be iterated post-launch. Imperfect balance is tolerable; broken retention is fatal. |
| 4 | **Mythology is Gameplay, Not Decoration** | Mythology adds depth and uniqueness but should never block a good gameplay decision. If a mechanic needs to break mythology to be fun, fun wins. |

**Resolution Process**:
1. Identify which pillars are in tension
2. Consult the priority ranking above
3. If the lower-priority pillar can be served partially without compromising the higher-priority one, do so
4. If not, the higher-priority pillar wins
5. Document the decision and rationale in the relevant design document

---

## Player Motivation Alignment

| Need | Which Pillar Serves It | How |
| ---- | ---- | ---- |
| **Autonomy** (meaningful choice) | Build Diversity Over Build Power | Many viable builds = real choice, not illusion of choice. Level-up element selection = meaningful decision point every 30-60 seconds. |
| **Competence** (mastery, growth) | Death Teaches, Never Punishes | Death provides clear feedback. Meta-progression is visible power growth. Learning spell recipes is knowledge mastery. |
| **Relatedness** (connection) | Mythology is Gameplay, Not Decoration | Cultural connection through Turkic mythology — unique in the market. Destan collection creates shared cultural experience. |

---

## Emotional Arc

### Session Emotional Arc

| Phase | Duration | Target Emotion | Pillar(s) Driving It | Mechanics Delivering It |
| ---- | ---- | ---- | ---- | ---- |
| Opening | 0-2 min | Anticipation, discovery | Every Element Matters | First element pick, first spell craft within 2 minutes, enemies are manageable |
| Rising | 2-10 min | Focus, flow, tension | Every Element Matters, Build Diversity | Spell crafting decisions, element inventory juggling, first boss at 5 min |
| Climax | 10-25 min | Intensity, excitement | Build Diversity, Mythology | Full spell build online, elite enemies, bosses 2-4, screen-filling VFX |
| Resolution | 25-30 min | Satisfaction or defiance | Death Teaches | Run summary, gold/rewards earned, "one more run" feeling |
| Hook | End of session | Curiosity, unfinished business | Every Element, Discovery | Undiscovered recipes, unplayed heroes, locked Destan pages |

### Long-Term Emotional Progression

- **Week 1**: Discovery — learning recipes, first hero unlock, first Destan page
- **Weeks 2-4**: Mastery — optimizing builds, clearing bosses consistently, filling meta-upgrades
- **Months 2-3**: Expression — trying every hero/class combo, finding personal playstyle
- **Month 4+**: Completion — maxing upgrades, filling Destan collection, community sharing

---

## Reference Games

| Reference | What We Take From It | What We Do Differently | Which Pillar It Validates |
| ---- | ---- | ---- | ---- |
| **Vampire Survivors** | Core loop structure, auto-attack, escalating waves, run duration | Real-time element crafting instead of weapon evolution; deeper build decisions at level-up | Death Teaches (accessible loop), Build Diversity (crafting > weapon select) |
| **Brotato** | Character variety driving replayability, item synergies | Crafting-based acquisition instead of shop; cultural theme vs. random potato characters | Build Diversity (hero × class × spell), Every Element Matters (all paths viable) |
| **Halls of Torment** | Pixel art + satisfying VFX, retro visual identity | Turkic mythology instead of Western gothic; spell crafting instead of ability selection | Mythology is Gameplay (visual design from culture, not Western tropes) |

**Non-game inspirations**:
- Dede Korkut stories (hero archetypes, enemy designs)
- Oğuz Kağan epic (hero inspiration for Ay Kağan)
- Tengri belief system (5-element cosmology, spirit world mechanics)
- Ergenekon legend (wolf ancestry → Börte hero, mountain forging → Temür element)
- Turkic petroglyphs and nomadic art (visual style reference)

---

## Pillar Validation Checklist

- [x] **Count**: 4 pillars (within 3-5 range)
- [x] **Falsifiable**: Each pillar makes a claim that could be wrong (e.g., "all elements matter" could be proven wrong by balance data)
- [x] **Constraining**: Each pillar forces saying "no" (no element dominance, no death penalties, no generic fantasy, no dominant builds)
- [x] **Cross-departmental**: Each pillar has implications for design, art, audio, narrative, AND engineering
- [x] **Design-tested**: Each pillar has a concrete design test that resolves a real decision
- [x] **Anti-pillars defined**: 4 explicit "this game is NOT" statements
- [x] **Priority-ranked**: Clear conflict resolution order (Retention > Balance > Diversity > Theme)
- [x] **MDA-aligned**: Pillars collectively deliver Challenge (primary), Fantasy, Discovery, Expression
- [x] **SDT coverage**: Autonomy (Build Diversity), Competence (Death Teaches), Relatedness (Mythology)
- [x] **Memorable**: "Every Element Matters / Death Teaches / Mythology is Gameplay / Diversity Over Power"
- [x] **Core fantasy served**: Every pillar traces back to the shaman-with-cosmic-elements fantasy

---

## Next Steps

- [x] Get pillar approval from creative-director (zbrave)
- [ ] Distribute to all department leads for sign-off
- [ ] Create design tests for each pillar using real upcoming decisions
- [ ] Schedule first pillar review (after Phase 1 prototype)
- [ ] Add pillars to the game-concept document (already referenced)
- [ ] Use pillars to guide `/map-systems` decomposition

---

*This document is the creative north star. It lives in `design/gdd/game-pillars.md`
and is referenced by every design, art, audio, and narrative document in the project.
Review quarterly or after major milestone pivots.*
