## 2026-04-05 — S1-04 Input System wrapper

- Added `KokTengri.Core.IInputProvider` as the DI-friendly movement abstraction with `MoveDirection` and `OnMove`.
- Added `KokTengri.Gameplay.InputManager` as a `MonoBehaviour` wrapper that forwards move updates through `UpdateMoveDirection` and conditionally binds Unity's New Input System when `ENABLE_INPUT_SYSTEM` is available.
- Created `KokTengri/Assets/InputActions/PlayerInputActions.inputactions` with `Gameplay/Move` as a `Vector2` action supporting gamepad left stick, WASD composite, and touch via an on-screen-stick-compatible left-stick binding.
- Wrote tests first in `KokTengri/Assets/Tests/Foundation/InputManagerTests.cs` to cover wrapper delegation, DI injection with a mock provider, and graceful null-provider handling.
- Local verification succeeded for file creation and `.inputactions` JSON structure; full Unity compile/test verification is currently blocked by the missing local C# language server and the absence of `com.unity.inputsystem` in `KokTengri/Packages/manifest.json`.
