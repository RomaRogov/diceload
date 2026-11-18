using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Diceload
{
    /// <summary>
    /// uGUI's Screen Space – Overlay canvases draw to the screen, not to a camera. For a screenshot they're
    /// drawn by a camera — Screen Space – Camera, just in front of its near plane — and put back after.
    /// </summary>
    public sealed class OverlayCanvases : IDisposable
    {
        readonly List<Canvas> moved = new();

        /// <summary>Draw the overlay canvases with <paramref name="camera"/> until disposed.</summary>
        public static OverlayCanvases OnCamera(Camera camera)
        {
            var overlays = new OverlayCanvases();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                overlays.moved.Add(canvas);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.01f;
            }
            if (overlays.moved.Count > 0) Canvas.ForceUpdateCanvases();
            return overlays;
        }

        public void Dispose()
        {
            foreach (var canvas in moved)
            {
                if (canvas == null) continue;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
        }
    }
}
