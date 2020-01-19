using UnityEngine;
using UnityEditor;

public class WriteEXRflie : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Texture2D texture = new Texture2D(3, 1, TextureFormat.RGBAHalf, false, true);

        texture.SetPixel(0, 0, new Color(5, -5, 0, 0));
        texture.SetPixel(1, 0, new Color(25, 0, 0, 0));
        texture.SetPixel(2, 0, new Color(-15, 10, 0, 0));

        texture.Apply();

        texture.anisoLevel = 0;
        texture.filterMode = FilterMode.Point;

        RenderTexture renderTexture = new RenderTexture(3, 1, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

        //renderTexture.height = 1;
        //renderTexture.width = 3;
        //renderTexture.format = RenderTextureFormat.ARGBHalf;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.anisoLevel = 0;
        renderTexture.depth = 0;

        renderTexture.Create();

        Graphics.Blit(texture, renderTexture);

        AssetDatabase.CreateAsset(texture, "Assets/pivotPos.renderTexture");

        renderTexture.Release();

        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(texture);
    }
}
