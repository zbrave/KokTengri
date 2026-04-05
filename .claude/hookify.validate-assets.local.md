---
name: validate-assets
enabled: true
event: file
conditions:
  - field: file_path
    operator: regex_match
    pattern: (^|/)assets/
---

⚠️ **Asset File Validation**

When writing/editing files in `assets/`, verify:

**Naming Convention:**
- [ ] Filename uses lowercase with underscores only: `system_name.ext`
- [ ] No uppercase letters, spaces, or hyphens in filenames
- [ ] Data files follow `[system]_[name].json` pattern

**JSON Data Files** (files in `assets/data/`):
- [ ] Valid JSON syntax — broken JSON blocks the build pipeline
- [ ] camelCase for keys within JSON files
- [ ] No orphaned data entries — every entry must be referenced by code or another data file
- [ ] Schema documented (JSON Schema or in corresponding design doc)

**Shader Files** (files in `assets/shaders/`):
- [ ] File naming: `[type]_[category]_[name].[ext]` (e.g., `spatial_env_water.gdshader`)
- [ ] All uniforms have descriptive names and appropriate hints
- [ ] No magic numbers — use named constants or documented uniform values

See **AGENTS.md > Path-Scoped Rules** for full asset standards.
