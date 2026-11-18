using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Diceload
{
    /// <summary>
    /// A play session an agent scripts end to end, the way a Playwright spec drives a page: a
    /// PlayMode <c>[UnityTest]</c> coroutine that loads the game, plays it with input as the
    /// player's, reads and changes its state with plain C#, takes screenshots, and asserts.
    /// </summary>
    /// <remarks>
    /// The runner is the Unity Test Framework and the input is the Input System's own
    /// <see cref="InputTestFixture"/>. This class only puts the session together: a held clock
    /// (<see cref="SimulationClock"/>), frames that aren't drawn (<see cref="FrameDrawing"/>), a
    /// results folder and log (<see cref="ResultsFolder"/>, <see cref="TestLog"/>), and the three
    /// calls a test makes: <see cref="Load"/>, <see cref="Capture"/>, <see cref="Pause"/>.
    /// </remarks>
    public abstract class GameSimulation : InputTestFixture
    {
        /// <summary>One frame of game time.</summary>
        public const float FrameSeconds = SimulationClock.FrameSeconds;

        /// <summary>The keyboard the simulation types on (<c>Keyboard.current</c> in the game).</summary>
        protected Keyboard keyboard;

        /// <summary>The mouse the simulation points with (<c>Mouse.current</c> in the game).</summary>
        protected Mouse mouse;

        readonly TestLog log = new();

        /// <summary>This test's folder, <c>SimulationResults/&lt;Fixture&gt;/&lt;Test&gt;/</c>: its screenshots and <c>result.txt</c>.</summary>
        protected static string ResultsDirectory => ResultsFolder.Current;

        public override void Setup()
        {
            base.Setup();
            ResultsFolder.Empty();
            log.Start();
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            SimulationClock.Hold();
            FrameDrawing.Stop();
        }

        public override void TearDown()
        {
            SimulationClock.Release();
            FrameDrawing.Resume();
            log.Stop();
            ResultsFolder.WriteResult(log.Text);
            base.TearDown();
        }

        /// <summary>
        /// Load <paramref name="scenePath"/> fresh, with <c>UnityEngine.Random</c> started from
        /// <paramref name="seed"/>, and return once its objects have run <c>Start</c>. The same seed and
        /// the same input play out the same way, so loading again replays, or tries another value.
        /// </summary>
        protected IEnumerator Load(string scenePath, int seed = 0) => SceneLoader.Load(scenePath, seed);

        /// <summary>
        /// Draw the game as it stands — cameras, then uGUI and UI Toolkit on top — to
        /// <c>SimulationResults/&lt;Fixture&gt;/&lt;Test&gt;/&lt;name&gt;.png</c>, and return the path.
        /// </summary>
        protected string Capture(string name, int width = 1280, int height = 720)
        {
            var path = ResultsFolder.File(name + ".png");
            Screenshot.Take(path, width, height);
            Debug.Log($"capture {name}: {path}");
            return path;
        }

        /// <summary>
        /// Stop on this frame, drawn, for a person at the Editor (Playwright's <c>page.pause()</c>); it goes on
        /// when they resume. Does nothing in batch mode — an agent looks with <see cref="Capture"/>.
        /// </summary>
        protected IEnumerator Pause() => FrameDrawing.PauseOnDrawnFrame();

        /// <summary>
        /// Press a button as a player does: one already down stays down and nothing is sent, so a loop
        /// can say "hold right" every frame. (InputTestFixture's own sends an event each call, and a
        /// held key pressed again every frame reads as up.)
        /// </summary>
        public new void Press(ButtonControl button, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
        {
            if (!button.isPressed) base.Press(button, time, timeOffset, queueEventOnly);
        }

        /// <summary>Release a button as a player does: one that isn't down is left alone.</summary>
        public new void Release(ButtonControl button, double time = -1, double timeOffset = 0, bool queueEventOnly = false)
        {
            if (button.isPressed) base.Release(button, time, timeOffset, queueEventOnly);
        }
    }
}
