# Plan: ASMDef Test Runner Hotfix

## Objective
Fix the "duplicate references" compiler error in the Unity Test Runner configuration caused by referencing both explicit test runner DLLs and implicit `TestAssemblies` simultaneously.

## Scope
- **IN**: Remove `UnityEngine.TestRunner` and `UnityEditor.TestRunner` explicit references from `KokTengri.Tests.asmdef` because the `optionalUnityReferences` -> `TestAssemblies` block already auto-includes them.
- **OUT**: Any other script or test modifications.

## Tasks
- [x] 1. **Remove Duplicate References**
  - **What to do**: Edit `KokTengri/Assets/Tests/KokTengri.Tests.asmdef`. Remove the elements `"UnityEngine.TestRunner"` and `"UnityEditor.TestRunner"` from the `"references"` array. Leave only `"KokTengri.Runtime"`.
  - **Recommended Agent Profile**: `quick` (Simple file edit).
  - **Acceptance Criteria**:
    - [x] `KokTengri.Tests.asmdef` contains exactly 1 explicit reference (`KokTengri.Runtime`).
    - [x] The `optionalUnityReferences` block remains intact with `"TestAssemblies"`.

## Final Verification
- [x] F1. **Code Quality Review**: Unity assembly file is valid JSON.
- [ ] F2. **Manual QA**: Ask the user to return to Unity Editor and confirm the "duplicate references" error is gone and the Test Runner correctly lists 19 tests in the EditMode tab.
