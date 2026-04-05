# OpenCode Game Studios — Game Studio Agent Architecture

<CRITICAL_STARTUP_DIRECTIVE>
**FOR SISYPHUS (MAIN AGENT):**
When you start a new session, your VERY FIRST ACTION must be to use the `Read` tool to read:
`production/session-state/active.md` (or the equivalent status file).
DO NOT answer the user's first prompt until you have read the active session state to restore context.
</CRITICAL_STARTUP_DIRECTIVE>

> Migrated from Claude Code Game Studios. Adapted for OpenCode's agent system,
> Hookify plugin, and PowerShell environment (Windows).
>
> Indie game development managed through 48 coordinated agents.
> Each agent owns a specific domain, enforcing separation of concerns and quality.

## Language Policy

- **User-facing communication** (Sisyphus → User): Turkish (Türkçe)
- **All internal agent communication** (delegation prompts, task descriptions, agent outputs, skill invocations, session state, logs): English
- **Game terminology** (hitbox, cooldown, DPS, loot table, etc.): Keep original English terms — do not translate
- **Code, comments, docstrings, commit messages, design documents, file names**: Always English

This ensures agent tasks are unambiguous while the user receives responses in their preferred language.

## Technology Stack

- **Engine**: Unity 2022.3 LTS (URP 2D)
- **Language**: C#
- **Version Control**: Git with trunk-based development
- **Build System**: Unity Build Pipeline
- **Asset Pipeline**: Unity Asset Import + Addressables (post-MVP)

> **Note**: Engine-specialist agents exist for Godot, Unity, and Unreal with
> dedicated sub-specialists. Use the set matching your engine.

## Project Structure

```text
/
├── AGENTS.md                    # OpenCode master configuration (this file)
├── CLAUDE.md                    # Legacy Claude Code config (kept for compat)
├── .claude/
│   ├── agents/                  # 48 agent definitions
│   ├── skills/                  # 37 slash commands
│   ├── hooks/                   # Hook scripts (bash + PowerShell)
│   ├── rules/                   # Legacy path-scoped rules (reference only)
│   ├── docs/                    # Documentation and templates
│   ├── hookify.*.local.md       # Hookify validation rules (active)
│   └── settings.json            # Legacy Claude Code settings
├── src/                         # Game source code (core, gameplay, ai, networking, ui, tools)
├── assets/                      # Game assets (art, audio, vfx, shaders, data)
├── design/                      # Game design documents (gdd, narrative, levels, balance)
├── docs/                        # Technical documentation (architecture, api, postmortems)
│   └── engine-reference/        # Curated engine API snapshots (version-pinned)
├── tests/                       # Test suites (unit, integration, performance, playtest)
├── tools/                       # Build and pipeline tools (ci, build, asset-pipeline)
├── prototypes/                  # Throwaway prototypes (isolated from src/)
└── production/                  # Production management (sprints, milestones, releases)
    ├── session-state/           # Ephemeral session state (active.md — gitignored)
    └── session-logs/            # Session audit trail (gitignored)
```

## Engine Version Reference

@docs/engine-reference/godot/VERSION.md

## Technical Preferences

@.claude/docs/technical-preferences.md

## Collaboration Protocol

**User-driven collaboration, not autonomous execution.**
Every task follows: **Question -> Options -> Decision -> Draft -> Approval**

- Agents MUST ask "May I write this to [filepath]?" before using Write/Edit tools
- Agents MUST show drafts or summaries before requesting approval
- Multi-file changes require explicit approval for the full changeset
- No commits without user instruction

See `docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` for full protocol and examples.

> **First session?** If the project has no engine configured and no game concept,
> run `/start` to begin the guided onboarding flow.

---

## Agent Coordination Rules

1. **Vertical Delegation**: Leadership agents delegate to department leads, who
   delegate to specialists. Never skip a tier for complex decisions.
2. **Horizontal Consultation**: Agents at the same tier may consult each other
   but must not make binding decisions outside their domain.
3. **Conflict Resolution**: When two agents disagree, escalate to the shared
   parent. If no shared parent, escalate to `creative-director` for design
   conflicts or `technical-director` for technical conflicts.
4. **Change Propagation**: When a design change affects multiple domains, the
   `producer` agent coordinates the propagation.
5. **No Unilateral Cross-Domain Changes**: An agent must never modify files
   outside its designated directories without explicit delegation.

---

## Coding Standards

- All game code must include doc comments on public APIs
- Every system must have a corresponding architecture decision record in `docs/architecture/`
- Gameplay values must be data-driven (external config), never hardcoded
- All public methods must be unit-testable (dependency injection over singletons)
- Commits must reference the relevant design document or task ID
- **Verification-driven development**: Write tests first when adding gameplay systems.
  For UI changes, verify with screenshots. Compare expected output to actual output
  before marking work complete. Every implementation should have a way to prove it works.

### Design Document Standards

- All design docs use Markdown
- Each mechanic has a dedicated document in `design/gdd/`
- Documents must include these 8 required sections:
  1. **Overview** -- one-paragraph summary
  2. **Player Fantasy** -- intended feeling and experience
  3. **Detailed Rules** -- unambiguous mechanics
  4. **Formulas** -- all math defined with variables
  5. **Edge Cases** -- unusual situations handled
  6. **Dependencies** -- other systems listed
  7. **Tuning Knobs** -- configurable values identified
  8. **Acceptance Criteria** -- testable success conditions
- Balance values must link to their source formula or rationale
- Design documents MUST be written incrementally: create skeleton first, then fill
  each section one at a time with user approval between sections.

---

## Path-Scoped Rules (Auto-Enforced via Instructions)

> These rules are enforced when editing files in the specified paths.
> Hookify rules provide additional automated validation.

### src/gameplay/** — Gameplay Code

- ALL gameplay values MUST come from external config/data files, NEVER hardcoded
- Use delta time for ALL time-dependent calculations (frame-rate independence)
- NO direct references to UI code — use events/signals for cross-system communication
- Every gameplay system must implement a clear interface
- State machines must have explicit transition tables with documented states
- Write unit tests for all gameplay logic — separate logic from presentation
- Document which design doc each feature implements in code comments
- No static singletons for game state — use dependency injection

### src/core/** — Engine Code

- ZERO allocations in hot paths (update loops, rendering, physics) — pre-allocate, pool, reuse
- All engine APIs must be thread-safe OR explicitly documented as single-thread-only
- Profile before AND after every optimization — document the measured numbers
- Engine code must NEVER depend on gameplay code (strict dependency direction: engine <- gameplay)
- Every public API must have usage examples in its doc comment
- Changes to public interfaces require a deprecation period and migration guide
- Use RAII / deterministic cleanup for all resources
- All engine systems must support graceful degradation
- Before writing engine API code, consult `docs/engine-reference/` for the current engine version

### src/ai/** — AI Code

- AI update budget: 2ms per frame maximum — profile to verify
- All AI parameters must be tunable from data files (behavior tree weights, perception ranges, timers)
- AI must be debuggable: implement visualization hooks for all AI state
- AI should telegraph intentions — players need time to read and react
- Prefer utility-based or behavior tree approaches over hard-coded if/else chains
- Group AI must support formation, flanking, and role assignment from data
- All AI state machines must log transitions for debugging
- Never trust AI input from the network without validation

### src/networking/** — Network Code

- Server is AUTHORITATIVE for all gameplay-critical state — never trust the client
- All network messages must be versioned for forward/backward compatibility
- Client predicts locally, reconciles with server — implement rollback for mispredictions
- Handle disconnection, reconnection, and host migration gracefully
- Rate-limit all network logging to prevent log flooding
- All networked values must specify replication strategy: reliable/unreliable, frequency, interpolation
- Bandwidth budget: define and track per-message-type bandwidth usage
- Security: validate all incoming packet sizes and field ranges

### src/ui/** — UI Code

- UI must NEVER own or directly modify game state — display only, use commands/events
- All UI text must go through the localization system — no hardcoded user-facing strings
- Support both keyboard/mouse AND gamepad input for all interactive elements
- All animations must be skippable and respect user motion/accessibility preferences
- UI sounds trigger through the audio event system, not directly
- UI must never block the game thread
- Scalable text and colorblind modes are mandatory, not optional
- Test all screens at minimum and maximum supported resolutions

### assets/data/** — Data Files

- All JSON files must be valid JSON — broken JSON blocks the entire build pipeline
- File naming: lowercase with underscores only, following `[system]_[name].json` pattern
- Every data file must have a documented schema (JSON Schema or in the corresponding design doc)
- Numeric values must include comments or companion docs explaining what the numbers mean
- Use consistent key naming: camelCase for keys within JSON files
- No orphaned data entries — every entry must be referenced by code or another data file
- Version data files when making breaking schema changes
- Include sensible defaults for all optional fields

### assets/shaders/** — Shader Code

- File naming: `[type]_[category]_[name].[ext]` (e.g., `spatial_env_water.gdshader`)
- All uniforms/parameters must have descriptive names and appropriate hints
- Comment non-obvious calculations (especially math-heavy sections)
- No magic numbers — use named constants or documented uniform values
- Document the target platform and complexity budget for each shader
- Minimize texture samples in fragment shaders
- Avoid dynamic branching in fragment shaders — use `step()`, `mix()`, `smoothstep()`
- Two-pass approach for blur effects (horizontal then vertical)
- Test shaders on minimum spec target hardware

### design/gdd/** — Design Documents

- Every design document MUST contain the 8 required sections (listed above)
- Formulas must include variable definitions, expected value ranges, and example calculations
- Edge cases must explicitly state what happens, not just "handle gracefully"
- Dependencies must be bidirectional — if system A depends on B, B's doc must mention A
- Tuning knobs must specify safe ranges and what gameplay aspect they affect
- Acceptance criteria must be testable — a QA tester must be able to verify pass/fail
- No hand-waving: "the system should feel good" is not a valid specification
- Balance values must link to their source formula or rationale

### design/narrative/** — Narrative Content

- All new lore must be cross-referenced against existing lore for contradictions
- Every lore entry must specify canon level: Established / Provisional / Under Review
- Character dialogue must match the voice profile defined for that character
- World rules (what is possible/impossible) must be explicitly documented and consistent
- Mysteries must have documented "true answers" even if players never learn them
- Faction motivations, relationships, and power structures must be internally logical
- All narrative text must be localization-ready: no idioms that don't translate
- No line of dialogue should exceed 120 characters for dialogue box constraints

### tests/** — Test Standards

- Test naming: `test_[system]_[scenario]_[expected_result]` pattern
- Every test must have a clear arrange/act/assert structure
- Unit tests must not depend on external state (filesystem, network, database)
- Integration tests must clean up after themselves
- Performance tests must specify acceptable thresholds and fail if exceeded
- Test data must be defined in the test or in dedicated fixtures, never shared mutable state
- Mock external dependencies — tests should be fast and deterministic
- Every bug fix must have a regression test that would have caught the original bug

### prototypes/** — Prototype Code (Relaxed Standards)

- Hardcoded values allowed (no need for data-driven config)
- Minimal or no doc comments required
- Simple architecture (no dependency injection required)
- Each prototype lives in its own subdirectory: `prototypes/[name]/`
- Every prototype MUST have a `README.md` with: hypothesis, how to run, status, findings
- No production code may reference or import from `prototypes/`
- Prototypes must not modify files outside `prototypes/`
- When a prototype succeeds, it is NOT migrated directly — rewritten to production standards
- Never let prototype code grow into production code through incremental "cleanup"

---

## Session Lifecycle Instructions

### At Session Start

When beginning a new session, proactively check:

1. **Current branch**: Run `git rev-parse --abbrev-ref HEAD`
2. **Recent commits**: Run `git log --oneline -5` to see recent activity
3. **Active sprint**: Check for `production/sprints/sprint-*.md`
4. **Active milestone**: Check for `production/milestones/*.md`
5. **Open bugs**: Scan `tests/playtest/` and `production/` for `BUG-*.md` files
6. **Session state recovery**: If `production/session-state/active.md` exists, read it to recover context from a previous session

### Gap Detection (At Session Start)

Check for common documentation gaps:

1. **Fresh project**: If no engine configured, no game concept, no source code → suggest `/start`
2. **Substantial codebase but sparse docs**: If `src/` has 50+ files but `design/gdd/` has <5 files → suggest `/reverse-document`
3. **Undocumented prototypes**: Check `prototypes/` for directories without `README.md`
4. **Missing architecture docs**: If `src/core/` or `src/engine/` exists but `docs/architecture/` is empty
5. **Gameplay systems without design docs**: Check `src/gameplay/` subdirectories with 5+ files
6. **No production planning**: If 100+ source files but no `production/sprints/` or `production/milestones/`

### Before Context Compaction

Before context is compacted, ensure:

1. Update `production/session-state/active.md` with:
   - Current task and progress
   - Key decisions made this session
   - Files being actively worked on
   - Open questions
2. Note all files modified in this session
3. Log compaction event to `production/session-logs/compaction-log.txt`

### At Session End

When wrapping up a session:

1. Archive `production/session-state/active.md` to `production/session-logs/session-log.md`
2. Log session summary (commits, uncommitted changes) to session log
3. Clean up active session state file

---

## Context Management

Context is the most critical resource. Manage it actively.

### File-Backed State (Primary Strategy)

**The file is the memory, not the conversation.** Conversations are ephemeral.
Files on disk persist across compactions and session crashes.

Maintain `production/session-state/active.md` as a living checkpoint. Update it
after each significant milestone (design section approved, architecture decision,
implementation milestone, test results).

### Status Block (Production+ Only)

When the project is in Production, Polish, or Release stage, include a structured
status block in `active.md`:

```markdown
<!-- STATUS -->
Epic: Combat System
Feature: Melee Combat
Task: Implement hitbox detection
<!-- /STATUS -->
```

### Incremental File Writing

When creating multi-section documents:
1. Create the file immediately with a skeleton (all section headers, empty bodies)
2. Discuss and draft one section at a time in conversation
3. Write each section to the file as soon as it's approved
4. Update the session state file after each section
5. Previous discussion about completed sections can be safely compacted

### Context Budget Rule

When context usage approaches **110k tokens**, Sisyphus MUST:
1. **Stop taking new work** — no new tasks, no new explorations
2. **Finish current in-progress work** — complete the active todo item only
3. **Commit immediately** — `git add -A && git commit` with checkpoint message
4. **Do NOT continue** — end the response and let the session compact or end

This is a hard rule. Running out of context mid-task causes broken state and lost work.
Small, committed increments are always better than large unfinished blocks.

### Task Sizing

- Keep tasks **small and atomic** — one file change or one focused feature per task
- A single task should not exceed ~200 lines of changes
- If a task feels large, decompose it into smaller pieces first
- Each piece gets its own checkpoint commit

### Subagent Delegation

Use subagents for research and exploration to keep the main session clean:
- **Use subagents** when investigating across multiple files or consuming >5k tokens
- **Use direct reads** when you know exactly which 1-2 files to check
- Subagents do not inherit conversation history — provide full context in the prompt

### Recovery After Disruption

If a session dies or you start a new session to continue work:
1. Read `production/session-state/active.md` for context
2. Read partially-completed file(s) listed in the state
3. Continue from the next incomplete section or task

---

## Security & Safety Rules

### Forbidden Commands

These commands MUST NEVER be executed:
- `rm -rf` (recursive force delete)
- `git push --force` / `git push -f` (force push)
- `git reset --hard` (hard reset)
- `git clean -f` (force clean)
- `sudo` (privilege escalation)
- `chmod 777` (insecure permissions)
- Reading or writing `.env` files

### Protected Branches

Pushing to `main`, `master`, or `develop` requires:
- Build passes
- Unit tests pass
- No S1/S2 bugs exist

---

## Checkpoint Commit Protocol

**Every completed task MUST produce a checkpoint commit.** This is non-negotiable.
Checkpoint commits create a recoverable history and let you revert any individual step.

### When to Checkpoint

Checkpoint commits are mandatory at these points:

1. **After marking any TODO item `completed`** — the primary checkpoint trigger
2. **After a design document section is approved and written to file**
3. **After a significant file edit or creation** (new component, system, config)
4. **Before delegating to a subagent** — so subagent work is on a clean base
5. **Before session ends** — enforced by the `checkpoint-commit` hookify rule (blocks stop)

### How to Checkpoint

```
1. git add -A
2. git commit -m "checkpoint: [area]: [brief description]"
```

### Commit Message Format

```
checkpoint: [area/system]: [what was completed]

- Specific change 1
- Specific change 2
```

**Area tags:** `combat`, `ui`, `ai`, `core`, `audio`, `narrative`, `level`, `data`, `docs`, `tools`, `production`, `config`

**Examples:**
```
checkpoint: combat: implement hitbox detection

- Add HitboxDetector component
- Add HitboxResponder interface
- Wire up collision events

checkpoint: ui: add health bar component

- Create HealthBarView with animated fill
- Hook up damage events
- Test at min/max resolutions

checkpoint: data: balance melee weapon stats

- Adjust sword damage formula values
- Add dagger critical hit multiplier
- Update weapon range data
```

### What NOT to Checkpoint

- **Do not commit broken code.** Run diagnostics first (`lsp_diagnostics` on changed files).
- **Do not commit secrets.** Check for `.env`, API keys, credentials.
- **Do not commit prototype code outside `prototypes/`.**

### Hookify Enforcement

Two hookify rules enforce this protocol:

| Rule | Event | Action |
|------|-------|--------|
| `hookify.checkpoint-commit.local.md` | Stop | **Blocks** session stop if no commit was made (checks transcript for `git add`/`git commit`) |
| `hookify.todo-checkpoint.local.md` | Bash (todowrite) | **Warns** to commit after TODO updates |

---

## Hookify Rules

Automated validation rules are configured in `.claude/hookify.*.local.md` files.
These rules enforce commit validation, asset naming, protected files, session
completion checks, and checkpoint commits automatically when relevant tool calls are made.

Active rules:
- `hookify.dangerous-commands.local.md` — Blocks dangerous bash commands
- `hookify.validate-commit.local.md` — Warns on commit issues
- `hookify.validate-push.local.md` — Warns on push to protected branches
- `hookify.validate-assets.local.md` — Validates asset file naming
- `hookify.protected-files.local.md` — Blocks access to sensitive files
- `hookify.session-stop-checklist.local.md` — Completion checklist on session stop
- `hookify.gameplay-hardcoded-values.local.md` — Warns about hardcoded gameplay values
- `hookify.design-doc-sections.local.md` — Warns about missing design doc sections
- `hookify.checkpoint-commit.local.md` — **Blocks stop if no checkpoint commit made**
- `hookify.todo-checkpoint.local.md` — **Warns to commit after TODO item completed**
