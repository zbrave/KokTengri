# Game Concept: Kök Tengri

*Created: 2026-04-04*
*Status: Approved*
*Source: docs/superpowers/specs/2026-03-26-kok-tengri-game-design.md*

---

## Elevator Pitch

> It's a pixel art survivor-like where you play as a Turkic shaman combining 5 cosmic elements — Fire (Od), Water (Sub), Earth (Yer), Wind (Yel), and Iron (Temür) — to craft 15 unique spells mid-run, fighting waves of mythological creatures from the underworld of Erlik Han.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Survivor-like (bullet heaven) with real-time spell crafting |
| **Platform** | Mobile (iOS + Android) |
| **Target Audience** | Mid-core mobile gamers, 18-35, who enjoy Vampire Survivors-style loops |
| **Player Count** | Single-player |
| **Session Length** | 15-30 minutes |
| **Monetization** | Free-to-Play + IAP (cosmetic + hero unlock, no power advantage) |
| **Estimated Scope** | Medium (3-4 months MVP, solo developer) |
| **Comparable Titles** | Vampire Survivors, Brotato, Halls of Torment, Soulstone Survivors |

---

## Core Fantasy

You are the last shaman protecting Tengri's lands. Erlik Han's underworld armies are rising to the surface. Combine the power of the elements, rediscover the ancient spells of your ancestors, and push back the darkness. Every run is an epic, every death is a lesson, every victory is a legend.

---

## Unique Hook

**Shaman spell crafting with Turkic cosmology elements.** During each run, you collect 5 base elements (Od, Sub, Yer, Yel, Temür) drawn from ancient Turkic cosmology. Combining any 2 elements creates a unique spell — same elements yield Basic Spells (5), different elements yield Combined Spells (10), for a total of 15 discoverable spells. The crafting happens in real-time: every level-up, you pick an element, and if your inventory has a matching pair, a spell is automatically created or upgraded.

This is not "pick from a list of weapons" — it's "learn which elements combine into which spells and strategize your build." The crafting tree is knowledge the player builds over multiple runs.

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Challenge** | 1 (Primary) | Escalating enemy waves, boss every 5 minutes, build-dependent difficulty |
| **Fantasy** | 2 | Playing as a Turkic shaman wielding cosmic elements against mythological creatures |
| **Discovery** | 3 | Learning spell recipes, finding hero/class synergies, exploring element interactions |
| **Expression** | 4 | 15 spells × 6 heroes × 4 classes = massive build variety and personal expression |
| **Sensation** | 5 | Satisfying pixel art VFX, screen-filling spell effects, haptic feedback |
| **Submission** | 6 | Repetitive core loop is meditative, auto-attack with strategic level-up decisions |
| **Narrative** | 7 | Turkic mythology as flavor — Destan pages from bosses, hero backstories |
| **Fellowship** | N/A | Single-player game (post-MVP: leaderboards) |

### Key Dynamics (Emergent player behaviors)

1. **Recipe hunting**: Players will experiment with element combinations to discover all 15 spells, then share findings with the community
2. **Build planning**: Before a run, players choose hero + class to enable specific spell synergies (e.g., Börte + Batur for defensive powerhouse)
3. **Risk assessment**: Elite enemies offer guaranteed element drops but are dangerous — chase or avoid?
4. **Inventory juggling**: Managing 3 element inventory slots creates tension — discard an element to make room for a better one?
5. **Boss preparation**: Knowing a boss comes every 5 minutes creates time-pressure decisions about build completion

### Core Mechanics (Systems we build)

1. **Element Collection & Spell Crafting** — Collect 5 element types during runs; pairs auto-craft into 15 possible spells
2. **Wave-based Survival Combat** — Escalating enemy waves with 6 enemy types, each with unique behavior and element affinities
3. **Hero + Class System** — 6 heroes (each with a starting element) × 4 classes (each buffing specific spell categories) for build variety
4. **Meta-Progression** — Permanent upgrades purchased with gold earned across runs; long-term growth between sessions
5. **Boss Encounters** — 5 bosses every 5 minutes, each testing a different player skill

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | 15 spells, 6 heroes, 4 classes — every run is a player-driven choice of strategy. Level-up offers 3 elements + re-roll, creating meaningful decisions. | Core |
| **Competence** (mastery, skill growth) | Learning spell recipes is knowledge mastery. Boss patterns require mechanical skill. Difficulty scales with time, testing build optimization. | Core |
| **Relatedness** (connection, belonging) | Mythological setting creates cultural connection for Turkic audiences. Collection system (Destan pages) creates shared completion goals. | Supporting |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers** (goal completion, collection, progression) — How: Meta-progression upgrades, Destan collection, hero/class unlocks, maxing all upgrades (~138,000 gold)
- [x] **Explorers** (discovery, understanding systems, finding secrets) — How: 15 spell recipes to discover, hero/class synergy combinations, element interaction depth
- [ ] **Socializers** (relationships, cooperation, community) — How: Post-MVP leaderboards, Destan page sharing
- [ ] **Killers/Competitors** (domination, PvP, leaderboards) — How: Post-MVP competitive features

### Flow State Design

- **Onboarding curve**: First run teaches movement + auto-attack. Level-up introduces element selection. First spell craft happens within 2 minutes. First boss at 5 minutes.
- **Difficulty scaling**: `enemy_hp = base_hp × (1 + 0.12 × minute)` — gradual, predictable escalation. Bosses introduce new mechanics every 5 minutes.
- **Feedback clarity**: Spell effects are visually distinct. Element colors are consistent (Od=red, Sub=blue, Yer=brown, Yel=white, Temür=grey). Level-up screen shows recipe tooltips.
- **Recovery from failure**: Death always earns gold + soul stones. Meta-upgrades make next run easier. Death is never punitive — it's progression.

---

## Core Loop

### Moment-to-Moment (30 seconds)

Move around the arena with virtual joystick. Auto-attack handles combat. Watch enemies swarm, dodge contact damage, pick up XP gems and element drops. Observe spell effects clearing waves.

### Short-Term (5-15 minutes)

Level up multiple times. Each level-up: choose 1 of 3 elements, watch them combine into spells, decide whether to re-roll for a better element. Boss encounters every 5 minutes require focused positioning and build awareness.

### Session-Level (15-30 minutes)

A complete run from start to death/30-minute completion. Build goes from 0 spells to 6 fully-loaded spell slots. Encounter 3-5 bosses. Earn gold and soul stones. The run ends with a summary screen showing kills, spells crafted, bosses defeated, and currency earned.

### Long-Term Progression

Over days/weeks: unlock permanent upgrades (health, power, speed, magnet, luck, element affinity). Unlock new heroes (Ay Kağan 5K gold, Börte 8K, Ayzıt 12K). Unlock new classes (Mergen, Otacı via meta-progression). Fill the Destan collection. Target: all heroes unlocked in 6-8 weeks F2P.

### Retention Hooks

- **Curiosity**: Undiscovered spell recipes, unplayed hero/class combos, locked Destan pages
- **Investment**: Permanent upgrades that persist, gold accumulation toward next hero
- **Social**: Cultural connection through Turkic mythology (unique in the market)
- **Mastery**: Optimizing builds, learning boss patterns, finding element synergy breakpoints

---

## Game Pillars

> Full pillar document: `design/gdd/game-pillars.md`

**Pillar 1: Every Element Matters** — All 5 elements contribute equally to the 15-spell crafting tree. No element is strictly dominant.

**Pillar 2: Death Teaches, Never Punishes** — Every run earns permanent progression. Failure is information, not loss.

**Pillar 3: Mythology is Gameplay, Not Decoration** — Turkic cosmology elements define game mechanics, not just names.

**Pillar 4: Build Diversity Over Build Power** — Hero × class × spell combinations ensure no single dominant strategy.

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| **Vampire Survivors** | Core survivor-like loop, auto-attack, escalating waves, run structure | Real-time crafting instead of weapon selection; element-based build system | Validates the "minimal input, maximum spectacle" formula |
| **Brotato** | Varied character builds, wave-based structure, item synergies | Crafting-based acquisition instead of shop; mythological theme | Proves character variety drives replayability |
| **Halls of Torment** | Pixel art aesthetic, satisfying VFX, old-school visual style | Turkic mythology instead of Western gothic | Shows pixel art + good VFX = visual impact |

**Non-game inspirations**:
- Turkic mythology (Tengri belief system, Oğuz Kağan epic, Dede Korkut stories)
- Ergenekon legend (wolf ancestry, mountain forging)
- Turkic cosmology (5 elements: fire, water, earth, wind, iron)

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | 18-35 |
| **Gaming experience** | Mid-core — plays mobile games regularly, familiar with survivor-like genre |
| **Time availability** | 15-30 minute sessions, 2-3 times daily (commute, breaks, before bed) |
| **Platform preference** | Mobile-first (iOS/Android) |
| **Current games they play** | Vampire Survivors, Brotato, Soulstone Survivors, Slay the Spire |
| **What they're looking for** | Deeper build crafting than Vampire Survivors, cultural novelty, satisfying progression |
| **What would turn them away** | Pay-to-win mechanics, overly complex UI, sessions over 30 min, heavy story |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity 2022.3 LTS (URP 2D) — strong mobile deployment, ScriptableObjects for data-driven design, proven 2D pipeline |
| **Key Technical Challenges** | 300+ enemies on screen at 60 FPS; object pooling for zero GC; responsive touch controls |
| **Art Style** | Pixel art (2D sprites, URP 2D lighting) |
| **Art Pipeline Complexity** | Medium — custom pixel art (Aseprite → Unity), no 3D assets needed |
| **Audio Needs** | Moderate — Turkic-themed BGM, combat SFX, UI sounds. No adaptive music needed for MVP. |
| **Networking** | None (single-player). Post-MVP: leaderboards, cloud save. |
| **Content Volume** | 6 enemy types, 5 bosses, 15 spells, 6 heroes, 4 classes, ~20 permanent upgrades |
| **Procedural Systems** | Wave composition scales by time; no procedural level generation |

---

## Risks and Open Questions

### Design Risks

- **Crafting complexity may alienate casual players**: 15 recipes × element inventory management could feel overwhelming. Mitigation: recipe tooltips on level-up screen, first spell crafted within 2 minutes.
- **Build diversity may be theoretical**: Some hero/class/spell combos may be strictly better despite design intent. Mitigation: analytics tracking build win rates post-launch.

### Technical Risks

- **300+ enemies at 60 FPS on mid-range mobile**: Object pooling and efficient rendering required. Mitigation: early performance testing, URP 2D is lightweight.
- **Touch joystick feel**: Virtual joystick must feel responsive for a dodge-heavy game. Mitigation: Unity New Input System + early playtesting.

### Market Risks

- **Survivor-like genre saturation**: Market is crowded post-Vampire Survivors. Mitigation: Turkic mythology theme is genuinely unique in the space.
- **Mobile F2P monetization perception**: Players may assume pay-to-win. Mitigation: explicit "no power advantage" design rule (Section 7.5 of spec).

### Scope Risks

- **Solo developer timeline**: 3-4 month MVP is ambitious. Mitigation: clear MVP scope with explicit post-MVP deferrals.
- **Content volume**: 15 spells, 6 enemies, 5 bosses is significant for solo dev. Mitigation: Phase 1 delivers 3 spells and 2 enemy types first.

### Open Questions

- Should element drops come from elites only (current design) or also from regular enemies? Prototype needed.
- What is the ideal enemy density curve? Needs playtesting to find the "spectacle vs readability" sweet spot.

---

## MVP Definition

**Core hypothesis**: Players find the element-crafting combat loop engaging for 30+ minute sessions, and the crafting discovery motivates repeated runs.

**Required for MVP**:
1. Player movement with touch joystick
2. 2 enemy types (Kara Kurt, Yek Uşağı) with object pooling
3. Wave spawner with time-based difficulty scaling
4. Element inventory (max 3) + full spell crafting (all 15 recipes)
5. 3 working spell effects (Alev Halkası, Kılıç Fırtınası, Kaya Kalkanı)
6. XP system + level-up element selection popup
7. Basic HUD (HP bar, spell slots, timer)
8. Run manager (start → play → death → summary)
9. 4 heroes (Korkut, Ay Kağan, Börte, Ayzıt)
10. 2 classes (Kam, Batur)
11. 3 bosses (Tepegöz, Yer Tanrısı, Erlik Han'ın Elçisi)
12. Meta-progression (permanent upgrades, hero unlock)
13. 60 FPS on mid-range mobile

**Explicitly NOT in MVP** (defer to post-launch):
- Boz Ejderha and Erlik Han bosses (4th and 5th)
- Alp Er Tunga and Umay heroes
- Mergen and Otacı classes
- Evolution system (Section 4.7 of spec)
- Göl Aynası and Demirci Cin enemies (last 2 types)
- Cloud save
- Season Pass IAP
- Daily Quests system (post-MVP priority #1)

### Scope Tiers (if budget/time shrinks)

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **Minimum Viable** | 2 enemies, 3 spells, 1 boss | Core loop only | 6 weeks |
| **MVP** | 4 enemies, 15 spells, 3 bosses, 4 heroes, 2 classes | Core + meta | 3-4 months |
| **Full Vision** | 6 enemies, 5 bosses, 6 heroes, 4 classes, evolutions | All features, polished | 6+ months |

---

## Next Steps

- [x] Get concept approval from creative-director
- [x] Fill in CLAUDE.md technology stack (Unity 2022.3 LTS)
- [x] Create game pillars document (design/gdd/game-pillars.md)
- [ ] Decompose concept into systems (`/map-systems`)
- [ ] Create first architecture decision record (`/architecture-decision`)
- [ ] Prototype core loop (`/prototype core-loop`)
- [ ] Validate core loop with playtest (`/playtest-report`)
- [ ] Plan first sprint (`/sprint-plan new`)
