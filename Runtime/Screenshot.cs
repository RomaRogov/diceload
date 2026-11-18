using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Diceload
{
    /// <summary>
    /// The game as it stands, as a PNG: every enabled camera drawn in depth order into one image, with
    /// the UI on top — uGUI's overlay canvases (<see cref="OverlayCanvases"/>) and UI Toolkit's panels
    /// (<see cref="ToolkitPanels"/>). Needs no Game view, so it works in batch mode too. IMGUI
    /// (<c>OnGUI</c>) is never in it.
    /// </summary>
    public static class Screenshot
    {
        public static void Take(string path, int width, int height)
        {
            var cameras = new List<Camera>(Camera.allCameras);
            if (cameras.Count == 0) Assert.Fail("Capture: no enabled camera to draw the game with.");
            cameras.Sort((a, b) => a.depth.CompareTo(b.depth));

            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            using (OverlayCanvases.OnCamera(cameras[^1]))
                try
                {
                    foreach (var camera in cameras)
                    {
                        var previous = camera.targetTexture;
                        camera.targetTexture = target;
                        camera.Render();
                        camera.targetTexture = previous;
                    }
                    ToolkitPanels.DrawInto(target);
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    image.Apply();
                    File.WriteAllBytes(path, image.EncodeToPNG());
                }
                finally
                {
                    RenderTexture.active = active;
                    RenderTexture.ReleaseTemporary(target);
                    Object.Destroy(image);
                }
        }
    }
}
