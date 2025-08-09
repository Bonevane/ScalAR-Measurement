using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class DepthRaycaster : MonoBehaviour
{
    public AROcclusionManager occlusionManager;
    public Camera arCamera;

    /// <summary>
    /// Returns a world position from depth texture at given screen point.
    /// </summary>
    public bool DepthRaycast(Vector2 screenPoint, out Vector3 worldPos)
    {
        worldPos = Vector3.zero;

        // Get the current environment depth texture
        Texture depthTexture;
        occlusionManager.TryGetEnvironmentDepthTexture(out depthTexture);
        if (depthTexture == null) return false;

        // Convert screen point to texture UV (0-1)
        Vector2 uv = new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);

        // Convert UV to depth texture pixel coordinates
        int px = Mathf.RoundToInt(uv.x * depthTexture.width);
        int py = Mathf.RoundToInt(uv.y * depthTexture.height);

        // Read the depth value from the GPU texture into CPU
        // Note: This is slow if done every frame — can be optimized with async GPU readback
        RenderTexture rt = RenderTexture.GetTemporary(depthTexture.width, depthTexture.height, 0, RenderTextureFormat.RFloat);
        Graphics.Blit(depthTexture, rt);
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        float depthMeters = tex.GetPixel(px, py).r;

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        Destroy(tex);

        if (depthMeters <= 0f) return false;

        // Convert depth to world position
        Vector3 screenPos = new Vector3(screenPoint.x, screenPoint.y, depthMeters);
        worldPos = arCamera.ScreenToWorldPoint(screenPos);
        return true;
    }
}
