using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloudRenderer : MonoBehaviour
{
    public GameObject Point;
    public VisualEffect vfx;
    public float ParticleSize;

    private bool _toUpdate = false;
    private uint _particleCount = 0;
    private Texture2D _texColor;
    private Texture2D _texPosScale;
    private uint _resolution = 2048;


    /// <summary>
    /// Creates a list of points and colors to render
    /// </summary>
    /// <param name="pointsLists">The list of points to render</param>
    public void RenderPoints(List<List<IPoint<float>>> pointsLists)
    {
        RenderSettings.ambientLight = Color.black;
        
        List<Vector3> positions = new List<Vector3>();
        List<Color> colors = new List<Color>();

        foreach (List<IPoint<float>> points in pointsLists)
        {

            foreach (IPoint<float> point in points)
            {
                positions.Add(new Vector3(point.X, point.Y, point.Z));
                colors.Add(new Color(point.Color[0] / 255f, point.Color[1] / 255f, point.Color[2] / 255f));
            }
        }
        setPoints(positions.ToArray(), colors.ToArray());
        Debug.Log("Render End: " + DateTime.Now);
    }

    /// <summary>
    /// If _toUpdate is true renders all the created points with the vfx graph
    /// </summary>
    private void Update()
    {
        if (_toUpdate)
        {
            _toUpdate = false;

            vfx.Reinit();
            vfx.SetUInt(Shader.PropertyToID("ParticleCount"), _particleCount);
            vfx.SetTexture(Shader.PropertyToID("TexColor"), _texColor);
            vfx.SetTexture(Shader.PropertyToID("TexPosScale"), _texPosScale);
            vfx.SetUInt(Shader.PropertyToID("Resolution"), _resolution);
            vfx.SetFloat(Shader.PropertyToID("ParticleSize"), ParticleSize);
        }
    }

    /// <summary>
    /// Creates the points in form of a texture2d from the positions and colors
    /// </summary>
    /// <param name="positions">The positions of the points</param>
    /// <param name="colors">The colors of the points</param>
    private void setPoints(Vector3[] positions, Color[] colors)
    {
        _texColor = new Texture2D(positions.Length > (int)_resolution ? (int)_resolution : positions.Length, Mathf.Clamp(positions.Length / (int)_resolution, 1, (int)_resolution), TextureFormat.RGBAFloat, false);
        _texPosScale = new Texture2D(positions.Length > (int)_resolution ? (int)_resolution : positions.Length, Mathf.Clamp(positions.Length / (int)_resolution, 1, (int)_resolution), TextureFormat.RGBAFloat, false);
        int texWidth = _texColor.width;
        int texHeight = _texColor.height;

        for (int y = 0; y < texHeight; y++)
        {
            for (int x = 0; x < texWidth; x++)
            {
                int index = x + y * texWidth;
                _texColor.SetPixel(x, y, colors[index]);
                var data = new Color(positions[index].x, positions[index].y, positions[index].z, ParticleSize);
                _texPosScale.SetPixel(x, y, data);
            }
        }

        _texColor.Apply();
        _texPosScale.Apply();
        _particleCount = (uint)positions.Length;
        _toUpdate = true;
    }
}
