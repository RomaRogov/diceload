using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Diceload
{
    /// <summary>
    /// The resolution the game runs at — what <c>Screen.width</c> and <c>Screen.height</c> read, what cameras
    /// and UI lay themselves out for. In the Editor it is the Game view's size: a size of its own, "Diceload",
    /// selected for the test, and the person's own size selected again and the "Diceload" one removed after.
    /// In a player it is <c>Screen.SetResolution</c>.
    /// </summary>
    public static class ScreenResolution
    {
        const string SizeName = "Diceload";
#if UNITY_EDITOR
        // The Game view's size before the first change, by its name in the view's list — an index moves when
        // the list changes; null when unchanged.
        static string selectedBefore;
#endif

        public static IEnumerator Set(int width, int height)
        {
            if (width <= 0 || height <= 0) Assert.Fail($"SetResolution: {width}x{height} is no resolution; both must be above 0.");
#if UNITY_EDITOR
            selectedBefore ??= GameView.Selected ?? "";
            UnityEditor.PlayModeWindow.SetCustomRenderingResolution((uint)width, (uint)height, SizeName);
#else
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
#endif
            // The game sees it from the next frame on.
            for (var i = 0; Screen.width != width || Screen.height != height; i++)
            {
                if (i > 30) Assert.Fail($"SetResolution: the game still runs at {Screen.width}x{Screen.height}, not {width}x{height}, 30 frames on. In batch mode there is no Game view to size.");
                yield return null;
            }
        }

        /// <summary>The Game view as it was before the test changed it.</summary>
        public static void Restore()
        {
#if UNITY_EDITOR
            if (selectedBefore == null) return;
            var size = selectedBefore;
            selectedBefore = null;
            try
            {
                // Removed first, then the person's size found by name in the list as it is now.
                GameView.RemoveSize(SizeName);
                if (size.Length > 0 && !GameView.Select(size)) throw new InvalidOperationException($"no size called \"{size}\" in the list");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Diceload could not put the Game view back ({e.InnerException?.Message ?? e.Message}): pick its size again in the Game view's toolbar, and remove \"{SizeName}\" from the list.");
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// The Game view's size list, which Unity keeps internal: reached by reflection, and only to undo
        /// what <c>PlayModeWindow.SetCustomRenderingResolution</c> did.
        /// </summary>
        static class GameView
        {
            const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            static readonly Assembly Editor = typeof(UnityEditor.EditorWindow).Assembly;
            static Type ViewType => Editor.GetType("UnityEditor.GameView");

            static Object View => Resources.FindObjectsOfTypeAll(ViewType).FirstOrDefault();

            static Type Sizes => Editor.GetType("UnityEditor.GameViewSizes");
            static object Instance => Sizes.BaseType.GetProperty("instance").GetValue(null);
            static object Group => Sizes.GetProperty("currentGroup").GetValue(Instance);
            static string[] Names => (string[])Group.GetType().GetMethod("GetDisplayTexts").Invoke(Group, null);

            /// <summary>The selected size's name, as the Game view's menu shows it; null without a Game view.</summary>
            public static string Selected
            {
                get
                {
                    try
                    {
                        var view = View;
                        return view == null ? null : Names[(int)ViewType.GetProperty("selectedSizeIndex", Any).GetValue(view)];
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            public static bool Select(string name)
            {
                var view = View;
                var index = Array.IndexOf(Names, name);
                if (view == null || index < 0) return false;
                // What the view's own size menu calls: it resizes the game too.
                ViewType.GetMethod("SizeSelectionCallback", Any).Invoke(view, new object[] { index, null });
                return true;
            }

            /// <summary>Every custom size whose name starts with <paramref name="name"/>, and only those.</summary>
            public static void RemoveSize(string name)
            {
                var group = Group;
                var type = group.GetType();
                var names = Names;
                for (var i = names.Length - 1; i >= 0; i--)
                {
                    // Both take the index in the whole list, built-in sizes first.
                    if (!names[i].StartsWith(name + " (") || !(bool)type.GetMethod("IsCustomSize").Invoke(group, new object[] { i })) continue;
                    type.GetMethod("RemoveCustomSize").Invoke(group, new object[] { i });
                    if (Names.Length != names.Length - 1 || Array.IndexOf(Names, names[i]) >= 0)
                        throw new InvalidOperationException($"removing \"{names[i]}\" changed the list unexpectedly");
                    names = Names;
                }
                Sizes.GetMethod("SaveToHDD", Any).Invoke(Instance, null);
            }
        }
#endif
    }
}
