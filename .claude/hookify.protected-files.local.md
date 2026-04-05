---
name: protected-files
enabled: true
event: file
conditions:
  - field: file_path
    operator: regex_match
    pattern: \.env$|\.env\.|credentials\.|\.pem$|\.key$|secret[_\-]|
action: block
---

⛔ **Sensitive/Protected File Detected**

This file appears to contain sensitive data (secrets, credentials, keys, or environment variables).

**Why this is blocked:**
- `.env` files often contain API keys, database passwords, and tokens
- Credential files should never be in version control
- Secret files can leak through git history even if later deleted
- Key files (`.pem`, `.key`) are cryptographic secrets

**What to do instead:**
1. Use environment variables injected at runtime
2. Store secrets in a secure vault (e.g., HashiCorp Vault, AWS Secrets Manager)
3. Add sensitive file patterns to `.gitignore`
4. If you need to create a template, use `.env.example` with placeholder values

See **AGENTS.md > Security & Safety Rules > Forbidden Commands** for details.
