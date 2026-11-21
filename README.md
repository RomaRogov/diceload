# Diceload Simulation (`com.roro.diceload`)

An agent plays and checks a Unity game the way a Playwright spec drives a page: one PlayMode test
loads the game, plays it with real player input on a held clock, reads its state with plain C#,
takes screenshots and asserts. It's the Unity Test Framework and the Input System's
`InputTestFixture`; Diceload adds a base class, `GameSimulation`, and a few tools.

| Playwright | Diceload |
|---|---|
| `page.goto(url)` | `yield return Load("Assets/Scenes/Level1.unity", seed)` |
| `page.keyboard.press` | `Press(keyboard.spaceKey)`, `Release(…)`, `Move(mouse.position, p)` |
| `page.evaluate(() => state)` | plain C#: `FindAnyObjectByType<Game>().score` |
| `page.screenshot({ path })` | `Capture("name")` → `SimulationResults/<Fixture>/<Test>/name.png` |
| `page.pause()` | `yield return Pause()` — for a person at the Editor |
| `npx playwright test -g name` | `Tools~/unity-test Name` |

## Install

In the Package Manager, **+ ▸ Install package from git URL…**:

```
https://github.com/RomaRogov/diceload.git
```

or in `Packages/manifest.json`, `"com.roro.diceload": "https://github.com/RomaRogov/diceload.git#v0.5.0"`.
It brings the Test Framework and the Input System; set the Input System as the active input handling.
Installed from git, the tools below are in `Library/PackageCache/com.roro.diceload@…/Tools~/`.

### A project for an agent, on a new machine (macOS or Windows)

1. **Unity 6** (6000.0 or later) and **Unity's CLI**, `unity`: `curl -fsSL https://unity.com/install.sh | bash`
   on macOS and Linux, `irm https://unity.com/install.ps1 | iex` in PowerShell on Windows.
2. **The project:** the Input System as the active input handling (Project Settings ▸ Player), then both
   packages, in `Packages/manifest.json`:
   ```json
   "com.roro.scenoco": "https://github.com/RomaRogov/scenoco-unity.git#v0.2.0",
   "com.roro.diceload": "https://github.com/RomaRogov/diceload.git#v0.5.0"
   ```
   Private repositories install with the machine's git login (a credential helper, or SSH URLs).
3. **The Editor's command server:** `unity pipeline install` in the project folder (`com.unity.pipeline`).
   Open the project with `unity open . --until-ready`: it returns once the Editor takes commands, and fails
   if a dialog stops it — then `Tools~/unity-dialog` reads it (scripts that don't compile ask about Safe Mode).
4. **The skill:** copy `Skills~/unity-game` from the package into the project's `.claude/skills/`
   (or `~/.claude/skills/` for every project):
   ```bash
   mkdir -p .claude/skills && cp -R Library/PackageCache/com.roro.diceload@*/Skills~/unity-game .claude/skills/
   ```
5. **The agent's shell:** the tools are bash scripts. Claude Code on Windows runs commands in Git Bash
   (Git for Windows), which is all they need there. On macOS, `unity-dialog` builds its helper once
   with Xcode's command line tools (`xcode-select --install`), and a person allows it once (below).

## Writing a simulation

Simulations go in a test assembly that references the game's own (game code needs an `.asmdef`):

```json
{
  "name": "Simulations",
  "references": ["Diceload", "MyGame", "Unity.InputSystem", "Unity.InputSystem.TestFramework",
                 "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

```csharp
public class LevelOne : GameSimulation
{
    [UnityTest]
    public IEnumerator PlaysThroughToTheEnd()
    {
        yield return Load("Assets/Scenes/Level1.unity", seed: 0);
        var game = Object.FindAnyObjectByType<Game>();
        Capture("start");
        for (var f = 0; f < 60 * 60 && game.phase != Phase.Over; f++)
        {
            /* decide input from the game's state: Press / Release */
            yield return null;
        }
        Capture("end");
        Debug.Log($"score={game.score} t={Time.timeSinceLevelLoad:F1}s");
        Assert.That(game.score, Is.GreaterThan(0));
    }
}
```

- **A frame is `yield return null`, and it's exactly 1/60 s of game time.** Frames run as fast as the
  machine allows and aren't drawn (2–6× faster; cameras stay enabled), so a minute of play takes seconds.
  Wait with `WaitForSeconds` or `WaitUntil(cond, TimeSpan.FromSeconds(60), onTimeout, WaitTimeoutMode.InGameTime)`
  (the timeout is a `TimeSpan`).
- **`Load(scene, seed)` is a replayable start:** what the last test kept with `DontDestroyOnLoad` gone
  (`keepAcrossScenes: true` keeps it), fixed clock aligned, `Random` seeded, scene fresh, every `Start`
  run. Same seed and same input play out the same; load again to replay or to try a value.
- **Unscaled time is real time.** Unity can't hold `Time.unscaledDeltaTime`, so while frames run fast,
  a fade on unscaled time, `WaitForSecondsRealtime`, a UI Toolkit transition or a sound barely moves.
  `yield return PlayInRealTime(seconds)` plays that stretch paced at real time, both clocks together.
- **Input comes only from the test.** `keyboard` and `mouse` are `Keyboard.current` / `Mouse.current` in
  the game; a key pressed in frame N is seen in frame N+1. `Press` on a held key does nothing, so a loop
  can say "hold right" every frame. Other devices: `InputSystem.AddDevice<Gamepad>()`.
- **Read and change the game with plain C#.** A value set after `Load` is tuning; `[ValueSource]` sweeps it.
- **Look with `Capture`:** cameras in depth order, then uGUI overlay canvases and UI Toolkit panels on
  top, at the size the game runs at. `OnGUI` is never in it.
- **Pick the screen with `SetResolution(width, height)`:** `yield return SetResolution(1080, 1920)` runs
  the game at that size — `Screen`, cameras, UI and captures follow — so one test can play a phone held
  upright and another a wide screen. Before `Load` for a game that lays itself out as it starts. In the
  Editor it is the Game view's size, and the person's own is selected again when the test ends.
  (`Capture(name, width, height)` only scales the picture: the layout stays the game's.)
- **Listen with `AudioLevel()`:** the RMS of what the AudioListener heard last, 0 for silence.
- **Report with `Debug.Log`.** Every line logged during the test, the game's too, goes frame-stamped into
  `result.txt` beside the screenshots, with the outcome. A logged error or exception fails the test.
  When the test ends, the game stops (every root made inactive) before its input is taken away.

## Running it

```bash
unity recompile                                   # reports compile errors
Packages/com.roro.diceload/Tools~/unity-test LevelOne
```

`unity-test` runs in the open Editor and waits. The Editor does the work (`TestRun`): it saves unsaved
work first (`UnsavedWork`, so the Test Framework doesn't ask), says why when no test matches the filter
(`TestFilter`), runs, and writes the summary, each failure and each test's results folder to
`Library/Diceload/test-run.txt`, which `unity-test` prints. Headless, in CI: `unity test . --mode PlayMode --filter LevelOne`.

## When the Editor stops answering

A modal dialog stops the Editor, and every `unity command` times out. `Tools~/unity-dialog` reads it —
title, text, buttons — and `unity-dialog press "Cancel"` answers it, through the system's accessibility API:
- **macOS:** a small helper app, `~/.diceload/Unity Dialog.app`, which a person allows once in System
  Settings ▸ Privacy & Security ▸ Accessibility ("Unity Dialog"; a rebuilt helper needs allowing again).
- **Windows:** UI Automation from PowerShell (`unity-dialog.ps1`); nothing to allow.
- **Linux:** not supported.

`Tools~/unity-restart` is the last resort: it closes and reopens the project with Unity's CLI, losing what
only the Editor held. Under Claude Code's sandbox (macOS, Linux) both run outside it (`sandbox.excludedCommands`).

## Limits

- The game must read the Input System, not `UnityEngine.Input`.
- `Load` resets `Random` and the scene, not statics or `DontDestroyOnLoad` objects.
- Game code that uses real time (`Time.unscaledTime`, `WaitForSecondsRealtime`) runs at machine speed.
- Compare physics state (`Rigidbody2D.position`) in replays, not an interpolated `transform.position`.
- A camera drawn only through a URP camera stack's overlay is captured on its own. Audio isn't checked.

Unity 6, `com.unity.test-framework` 1.4+, `com.unity.inputsystem` 1.7+ as the active input handling.

## Code

| Class | Does |
|---|---|
| `GameSimulation` | the test fixture: puts the rest together; `Load`, `Capture`, `Pause`, `Press`, `Release` |
| `SimulationClock` | holds game time at 1/60 s a frame; aligns a load to the fixed step |
| `SceneLoader` | a replayable start: aligned, seeded, fresh scene, `Start` run |
| `FrameDrawing` | frames undrawn while simulating; a pause on a drawn frame |
| `Screenshot` | cameras into one PNG, with the UI on top |
| `OverlayCanvases`, `ToolkitPanels` | uGUI's and UI Toolkit's screen UI drawn into a screenshot |
| `ResultsFolder`, `TestLog` | the test's folder and `result.txt`; its frame-stamped log |
| `TestRun` (Editor) | a run a shell waits for: start it, and its outcome as plain text |
| `UnsavedWork`, `TestFilter` (Editor) | save before a run without a dialog; why a filter matched no test |
