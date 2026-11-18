---
name: unity-game
description: Make, change, or test a Unity game with Diceload and Scenoco, in the open Unity Editor and without a person at it — scenes and prefabs as text (.uscene/.uprefab), C# behaviours, and PlayMode simulations that play the game with real input, read its state and take screenshots. Use when asked to build a Unity game or feature, edit a Unity scene or prefab, or check that a Unity game plays right.
---

# Making a Unity game with Diceload and Scenoco

Work as a Unity developer does, with the Editor open: what you'd build in the Editor — objects,
meshes, materials, prefabs, the level — goes into scenes and prefabs; what you'd write in an IDE —
behaviour — goes into C#. Scenoco gives every scene and prefab a text twin you edit instead of the
Editor, and Diceload plays the game the way a Playwright spec plays a page. You drive the Editor
through the `unity` CLI. Write from what you know of Unity; the two cards say where this differs.

## Read, as you need it

Each package is in `Packages/<name>/`, or, installed from git, in `Library/PackageCache/<name>@…/`.

- **Before writing a scene or prefab:** "Every scene" in `com.roro.scenoco/AGENTS.md`, and
  a paragraph of its "When the game needs it" the first time the game needs that thing.
- **Before writing a simulation:** "Writing a simulation" in `com.roro.diceload/README.md`.
- **For an Editor task:** `unity command --query <term>` lists the CLI's commands, and
  `unity command script eval --code '…'` runs any C# in the Editor and returns its value.

## Start

- **The project needs** both packages (`com.roro.scenoco`, `com.roro.diceload`; from git,
  `https://github.com/RomaRogov/scenoco-unity.git` and `…/diceload.git`), the Input System as the active
  input handling, and the Test Framework.
- **Game code goes in its own assembly** (an `.asmdef` in `Assets/Scripts`) so simulations can
  reference it; simulations go in a test assembly in `Assets/Simulations` (the README has it).
- **Keep a saved scene open** (`unity command scene open Assets/…/X.unity`) and change scenes through
  their text, which saves itself. An untitled scene with changes makes the next test run ask "save
  changes?", and that modal dialog stops the Editor.
- **The tools** are in the Diceload package's `Tools~/`: `unity-test`, `unity-dialog`,
  `unity-restart`. They're bash scripts: run them from the project folder (on Windows, in Git Bash,
  the shell Claude Code uses there). Setting up a project from nothing: Diceload's README, "A project
  for an agent".

## Build

Plan it as a Unity project before writing code: the scenes and what's in each, the prefabs (anything
placed more than once, or spawned), the materials, the UI (a UI Toolkit document; never `OnGUI`),
and the scripts on them — with one component that owns the game's state (phase, score, lives),
which every simulation reads. Then go round:

1. **Change** scenes and prefabs (`.uscene`/`.uprefab`), UI (`.uxml`/`.uss`), materials and other
   assets (C# through `script eval`, once), and scripts (`.cs`) for what moves, reacts and spawns.
2. **Check:** `unity recompile`, then `unity command script eval --code 'return Scenoco.Files.Sync();'`.
   Fix each error at its line: the message says what is accepted there.
3. **Play it:** a `GameSimulation` test loads the scene, plays a stretch with `Press`/`Release`,
   reads the state component, logs what matters, `Capture`s screenshots and asserts. Run it with
   `Tools~/unity-test <Fixture or Test>` — one call that waits and prints each failure and results
   folder — then read `SimulationResults/<Fixture>/<Test>/result.txt` and look at its PNGs. Readings
   say whether it works; screenshots, how it looks. Tune a value by setting it after `Load` (or
   sweep it with `[ValueSource]`), then write the one that works into the text.

Keep one simulation per phase of the game: they're its tests from then on. Done is a simulation that
plays a round through with real input — start, win or lose, restart — asserting on the state, with
screenshots that look right and no errors logged.

## When something is off

- **A capture is black or empty:** no camera, or it looks away. `Scenoco.Files.Describe("Assets/…/X.unity")`
  shows what the scene holds.
- **Input does nothing:** the game reads `UnityEngine.Input`; it must read the Input System.
- **A replay differs:** state in statics, or real time (`Time.unscaledTime`, `WaitForSecondsRealtime`).
  A simulation's clock is game time.
- **A `unity command` times out, or "did not start within" its timeout:** a modal dialog blocks the
  Editor. Run `Tools~/unity-dialog` to read it, then `Tools~/unity-dialog press "Cancel"` — the button
  that loses nothing. "No dialog" means it's compiling or importing: wait a minute. Still blocked:
  `Tools~/unity-restart` (unsaved Editor state is lost; texts are in their files). Say what blocked it.
- **A text stays `Conflict`:** the scene had unsaved changes when its file changed —
  `Scenoco.Files.Resolve(path, "file")` takes the file.
- **A value doesn't land:** an attribute is that property's setter, as in a script; the error lists
  what the component takes, and the card's "When something is off" has the rest.
- **Unity or the format seems wrong:** narrow it to the smallest case, work around it, and say so in
  your report.
