# Changelog

## [0.5.0] - 2026-11-20

### Added
- `unity-dialog` names the Safe Mode dialog — scripts that don't compile — and the button that opens the
  Editor with the errors listed; README and skill open a project with `unity open . --until-ready`.
- `SetResolution(width, height)`: the game runs at that size from the next frame — `Screen`, cameras,
  UI and captures follow. In the Editor it is a Game view size of its own; the person's size is
  selected again, and that one removed, when the test ends. In a player, `Screen.SetResolution`.
- `PlayInRealTime(seconds)`: frames paced at real time, so unscaled time — a UI fade,
  `WaitForSecondsRealtime`, a UI Toolkit transition, sound — keeps up with game time. Unity can't hold
  `Time.unscaledDeltaTime`; under a held clock, 60 frames moved it 0.04 s.
- `AudioLevel()`: the RMS of what the AudioListener put out last, so a test can tell the game isn't silent.

### Changed
- `Load` starts fresh: what an earlier test kept with `DontDestroyOnLoad` is destroyed first.
  `Load(scene, seed, keepAcrossScenes: true)` keeps it.
- `Capture` draws at the Game view's size by default, so a portrait game is captured as it laid itself
  out; it used to be 1280×720 whatever the game expected.

### Fixed
- A test that throws has its stack in `result.txt`, after the message: it used to say only
  `NullReferenceException`, with nothing to tell which line.
- `unity-test` compiles what changed before it runs, and stops on a compile error. It used to run the
  last compiled code, so a test edited since then ran as it was before, with no sign.
- The game ran frames after a test had restored the Input System, and what it threw then reached the
  Console but not `result.txt`. Teardown now makes every root inactive first, while the log still records.

## [0.4.0] - 2026-11-18

### Changed
- The tools are bash and Unity's CLI only, so they run on macOS, Linux and Windows (Git Bash): no
  zsh, Python or GNU `timeout`. `unity-test`'s work moved into the Editor: `TestRun.Start(filter, mode)`
  saves, checks the filter, runs, and writes the outcome as plain text to `Library/Diceload/test-run.txt`.
  It now finds the results folders of namespaced and parameterized tests.
- `unity-restart` uses `unity close --force` and `unity open`.
- `unity-dialog` leaves out windows with no buttons, which aren't waiting on an answer.

### Added
- `unity-dialog` on Windows, through UI Automation (`unity-dialog.ps1`).
- The agent skill, in `Skills~/unity-game`, and a setup guide for a new machine in the README.

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
