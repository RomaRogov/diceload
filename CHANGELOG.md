# Changelog

## [0.3.1] - 2026-11-18

### Fixed
- Installed from git, the `Diceload` assembly wasn't compiled: Unity skips a package's test
  assemblies (those constrained to `UNITY_INCLUDE_TESTS`) unless the package is in `testables`. It is
  now constrained to the Test Framework being installed, as the Input System's test fixture is.

## [0.3.0] - 2026-11-18

### Changed
- One class per job: `GameSimulation` only puts a session together; `SimulationClock`, `SceneLoader`,
  `FrameDrawing`, `Screenshot`, `OverlayCanvases`, `ToolkitPanels`, `ResultsFolder` and `TestLog` do the
  work. Its public surface is unchanged.
- Editor-only code moved to its own assembly, `Diceload.Editor`: `Simulations.Prepare()` is now
  `UnsavedWork.Save()`, `Simulations.WhyNoTests()` is `TestFilter.WhyNothingRan()`.
- `unity-test` checks the filter against `test list` before a run and says why nothing matches (exit 5),
  instead of printing the previous run's status.

### Added
- `Capture` draws UI Toolkit panels (`UIDocument`) over the cameras, as it draws uGUI overlay canvases.

Measured on the sample game, 1,800 frames: drawn every frame 566–887 fps, undrawn ~2,000 fps (with 4,000
lit cubes: 301–350 vs ~1,700–2,050). Disabling cameras instead was slower and nulls `Camera.main`.

## [0.2.0] - 2026-11-09

Rebuilt on Unity's own testing stack. A simulation is a PlayMode `[UnityTest]` coroutine — one
script per pass, like a Playwright spec — run in the open Editor through the Pipeline's
`test run` / `test status`, or headless with `unity test`.

### Added
- `GameSimulation` (derives from the Input System's `InputTestFixture`): a 1/60 s
  `Time.captureDeltaTime` clock, script-only `keyboard` / `mouse`, `Load(scene, seed)` (fixed-step
  aligned, reseeded, fresh scene; replays frame-identically), `Capture(name)` (camera render,
  works without a focused Game view), and a per-test `SimulationResults/<Fixture>/<Test>/` folder
  with screenshots and `result.txt` (outcome + frame-stamped logs).

- Rendering is off while a simulation runs (`OnDemandRendering.renderFrameInterval`), measured at
  2–6× the frame rate of drawing every frame, with cameras and `Camera.main` intact. `Capture`
  draws on demand; `Pause()` (like Playwright's `page.pause()`) draws the frame it stops the
  Editor on, and frames are undrawn again once it resumes.
- `SimulationThroughput` (explicit) and `RenderingChecks` samples: the measurement, and tests that
  nothing is drawn while simulating, that captures are real frames, and that a pause shows its frame.

### Removed
- The `sim.*` CLI verbs, the PlayerLoop/pause stepping host, the reflection read/override layer,
  the custom input queue, and the `com.unity.pipeline` dependency. Their jobs are done by UTF
  coroutines, plain C#, `InputTestFixture` and the Pipeline's own test commands. The 0.1.0 code is
  kept in `.backup/com.roro.diceload-0.1.0` at the project root.
