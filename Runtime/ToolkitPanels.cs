using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Diceload
{
    /// <summary>
    /// UI Toolkit's panels (<c>UIDocument</c>) draw to the screen, not to a camera, and repaint only on
    /// frames that are drawn — a simulation's aren't. For a screenshot each screen panel is pointed at the
    /// image once (<c>PanelSettings.targetTexture</c>, not cleared) and drawn by UI Toolkit's own per-frame
    /// calls: <c>UpdatePanels</c>, <c>RepaintPanels</c> and <c>RenderOffscreenPanels</c> — public methods of
    /// the internal <c>UIElementsRuntimeUtility</c>, so found by name.
    /// </summary>
    public static class ToolkitPanels
    {
        static readonly Type Runtime = typeof(PanelSettings).Assembly.GetType("UnityEngine.UIElements.UIElementsRuntimeUtility");
        static readonly MethodInfo Update = Runtime?.GetMethod("UpdatePanels", Type.EmptyTypes);
        static readonly MethodInfo Repaint = Runtime?.GetMethod("RepaintPanels", new[] { typeof(bool) });
        static readonly MethodInfo Render = Runtime?.GetMethod("RenderOffscreenPanels", Type.EmptyTypes);

        public static void DrawInto(RenderTexture target)
        {
            var panels = new List<PanelSettings>();
            foreach (var doc in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
                if (doc.panelSettings != null && doc.panelSettings.targetTexture == null && !panels.Contains(doc.panelSettings))
                    panels.Add(doc.panelSettings);
            if (panels.Count == 0) return;
            if (Update == null || Repaint == null || Render == null)
            {
                Debug.LogWarning("Capture: this Unity version has no UIElementsRuntimeUtility.UpdatePanels/RepaintPanels/RenderOffscreenPanels — UI Toolkit panels aren't in the image");
                return;
            }

            var clears = new List<bool>();
            foreach (var p in panels)
            {
                clears.Add(p.clearColor);
                p.clearColor = false;
                p.targetTexture = target;
            }
            try
            {
                Update.Invoke(null, null);
                Repaint.Invoke(null, new object[] { true });       // only offscreen panels: the ones pointed at the image
                Render.Invoke(null, null);
            }
            finally
            {
                for (var i = 0; i < panels.Count; i++)
                {
                    panels[i].targetTexture = null;
                    panels[i].clearColor = clears[i];
                }
            }
        }
    }
}
