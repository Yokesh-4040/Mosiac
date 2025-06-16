using UnityEngine;

public static class TextureUtils
{
    public static Texture2D ResizeTextureToSquare(Texture2D originalTexture, int targetSize)
    {
        // Create a new square texture
        Texture2D resizedTexture = new Texture2D(targetSize, targetSize, TextureFormat.RGB24, false);
        
        // Make the original texture readable if it isn't already
        Texture2D readableTexture = MakeTextureReadable(originalTexture);
        
        // Resize the texture using bilinear filtering
        Color[] originalPixels = readableTexture.GetPixels();
        Color[] resizedPixels = new Color[targetSize * targetSize];
        
        float xRatio = (float)readableTexture.width / targetSize;
        float yRatio = (float)readableTexture.height / targetSize;
        
        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                int originalX = Mathf.FloorToInt(x * xRatio);
                int originalY = Mathf.FloorToInt(y * yRatio);
                
                // Clamp to avoid out of bounds
                originalX = Mathf.Clamp(originalX, 0, readableTexture.width - 1);
                originalY = Mathf.Clamp(originalY, 0, readableTexture.height - 1);
                
                resizedPixels[y * targetSize + x] = originalPixels[originalY * readableTexture.width + originalX];
            }
        }
        
        resizedTexture.SetPixels(resizedPixels);
        resizedTexture.Apply();
        
        return resizedTexture;
    }
    
    public static Texture2D MakeTextureReadable(Texture2D originalTexture)
    {
        // Create a temporary RenderTexture
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            originalTexture.width,
            originalTexture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );
        
        // Copy the original texture to the RenderTexture
        Graphics.Blit(originalTexture, renderTexture);
        
        // Create a new readable texture
        Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height);
        
        // Read pixels from RenderTexture
        RenderTexture.active = renderTexture;
        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTexture.Apply();
        
        // Clean up
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexture);
        
        return readableTexture;
    }
} 