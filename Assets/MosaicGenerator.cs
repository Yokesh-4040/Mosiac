using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class MosaicGenerator : MonoBehaviour
{
    [Header("Logo Settings")]
    public Texture2D logoTexture; // The logo image to recreate
    public float logoScale = 10f; // How big the final mosaic should be
    public int gridResolution = 32; // Grid resolution (32x32 = 1024 cells max)
    public float alphaThreshold = 0.1f; // Minimum alpha to consider a pixel valid
    
    [Header("Grid Visualization")]
    public bool showGrid = true; // Show grid in scene view
    public bool showValidPositions = true; // Show only valid positions (non-transparent)
    public Color gridColor = Color.green;
    public Color validPositionColor = Color.red;
    public float cellVisualizationSize = 0.8f; // Size multiplier for visualization cubes
    
    [Header("Scene Visualization (Alternative to Gizmos)")]
    public bool createVisualObjects = false; // Create actual GameObjects for visualization
    public Material previewMaterial; // Material for preview objects
    public bool showConnectingLines = false; // Draw lines connecting grid points
    
    [Header("Grid Generation")]
    [SerializeField] private List<Vector3> validGridPositions = new List<Vector3>();
    [SerializeField] private List<Color> averageColors = new List<Color>(); // Average color for each grid position
    [SerializeField] private Vector2Int gridDimensions; // Actual grid dimensions
    [SerializeField] private float cellSize; // Size of each grid cell
    
    [Header("Debug Info")]
    [ReadOnly] public int totalValidPositions;
    [ReadOnly] public Vector2 logoAspectRatio;
    
    private List<GameObject> visualizationObjects = new List<GameObject>();
    private LineRenderer gridLineRenderer;
    
    private void Start()
    {
        if (logoTexture != null)
        {
            GenerateGridFromLogo();
        }
    }
    
    [Button("Generate Grid from Logo", ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void GenerateGridFromLogo()
    {
        if (logoTexture == null)
        {
            Debug.LogError("Logo texture is not assigned!");
            return;
        }
        
        Debug.Log("Starting grid generation from logo...");
        
        // Clear previous data
        validGridPositions.Clear();
        averageColors.Clear();
        ClearVisualizationObjects();
        
        // Make logo texture readable
        Texture2D readableLogo = MakeTextureReadable(logoTexture);
        
        // Calculate grid dimensions based on logo aspect ratio
        CalculateGridDimensions(readableLogo);
        
        // Calculate cell size
        cellSize = logoScale / Mathf.Max(gridDimensions.x, gridDimensions.y);
        
        Debug.Log($"Grid dimensions: {gridDimensions.x}x{gridDimensions.y}, Cell size: {cellSize:F2}");
        
        // Generate grid positions and sample colors
        SampleLogoGrid(readableLogo);
        
        // Update debug info
        totalValidPositions = validGridPositions.Count;
        logoAspectRatio = new Vector2((float)readableLogo.width / readableLogo.height, 1f);
        
        Debug.Log($"Grid generation complete! Found {totalValidPositions} valid positions out of {gridDimensions.x * gridDimensions.y} total cells");
        
        // Create visual objects if requested
        if (createVisualObjects)
        {
            CreateVisualizationObjects();
        }
        
        // Create connecting lines if requested
        if (showConnectingLines)
        {
            CreateConnectingLines();
        }
    }
    
    [Button("Create Visual Objects", ButtonSizes.Medium)]
    [GUIColor(0.2f, 1f, 0.2f)]
    public void CreateVisualizationObjects()
    {
        ClearVisualizationObjects();
        
        if (validGridPositions.Count == 0)
        {
            Debug.LogWarning("No grid positions available! Generate grid first.");
            return;
        }
        
        Debug.Log($"Creating {validGridPositions.Count} visualization objects...");
        
        for (int i = 0; i < validGridPositions.Count; i++)
        {
            // Create a cube for each grid position
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = this.transform;
            cube.transform.localPosition = validGridPositions[i];
            cube.transform.localScale = Vector3.one * cellSize * cellVisualizationSize;
            cube.name = $"GridPosition_{i}";
            
            // Set color based on average color
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (previewMaterial != null)
                {
                    Material instanceMaterial = new Material(previewMaterial);
                    instanceMaterial.color = averageColors[i];
                    renderer.material = instanceMaterial;
                }
                else
                {
                    // Create a basic material with the color
                    Material newMaterial = new Material(Shader.Find("Standard"));
                    newMaterial.color = averageColors[i];
                    renderer.material = newMaterial;
                }
            }
            
            visualizationObjects.Add(cube);
        }
        
        Debug.Log($"Created {visualizationObjects.Count} visualization objects!");
    }
    
    [Button("Create Connecting Lines", ButtonSizes.Medium)]
    [GUIColor(1f, 1f, 0.2f)]
    public void CreateConnectingLines()
    {
        if (validGridPositions.Count == 0)
        {
            Debug.LogWarning("No grid positions available! Generate grid first.");
            return;
        }
        
        // Remove existing line renderer
        if (gridLineRenderer != null)
        {
            DestroyImmediate(gridLineRenderer.gameObject);
        }
        
        // Create line renderer
        GameObject lineObject = new GameObject("GridLines");
        lineObject.transform.parent = this.transform;
        gridLineRenderer = lineObject.AddComponent<LineRenderer>();
        
        // Configure line renderer
        gridLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        gridLineRenderer.startColor = gridColor;
        gridLineRenderer.endColor = gridColor;
        gridLineRenderer.startWidth = 0.05f;
        gridLineRenderer.endWidth = 0.05f;
        gridLineRenderer.positionCount = validGridPositions.Count;
        
        // Set positions
        for (int i = 0; i < validGridPositions.Count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(validGridPositions[i]);
            gridLineRenderer.SetPosition(i, worldPos);
        }
        
        Debug.Log("Created connecting lines between grid positions!");
    }
    
    [Button("Clear All Visualizations", ButtonSizes.Medium)]
    [GUIColor(1f, 0.4f, 0.4f)]
    public void ClearVisualizationObjects()
    {
        // Clear visual objects
        foreach (GameObject obj in visualizationObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        visualizationObjects.Clear();
        
        // Clear line renderer
        if (gridLineRenderer != null)
        {
            DestroyImmediate(gridLineRenderer.gameObject);
            gridLineRenderer = null;
        }
        
        Debug.Log("Cleared all visualization objects!");
    }
    
    private void CalculateGridDimensions(Texture2D logo)
    {
        float aspectRatio = (float)logo.width / logo.height;
        
        if (aspectRatio >= 1f) // Logo is wider than tall
        {
            gridDimensions.x = gridResolution;
            gridDimensions.y = Mathf.RoundToInt(gridResolution / aspectRatio);
        }
        else // Logo is taller than wide
        {
            gridDimensions.x = Mathf.RoundToInt(gridResolution * aspectRatio);
            gridDimensions.y = gridResolution;
        }
        
        // Ensure minimum dimensions
        gridDimensions.x = Mathf.Max(1, gridDimensions.x);
        gridDimensions.y = Mathf.Max(1, gridDimensions.y);
    }
    
    private void SampleLogoGrid(Texture2D logo)
    {
        // Calculate how many pixels per grid cell
        float pixelsPerCellX = (float)logo.width / gridDimensions.x;
        float pixelsPerCellY = (float)logo.height / gridDimensions.y;
        
        Debug.Log($"Pixels per cell: {pixelsPerCellX:F2} x {pixelsPerCellY:F2}");
        
        // Sample each grid cell
        for (int gridY = 0; gridY < gridDimensions.y; gridY++)
        {
            for (int gridX = 0; gridX < gridDimensions.x; gridX++)
            {
                // Calculate the pixel region for this grid cell
                int startX = Mathf.FloorToInt(gridX * pixelsPerCellX);
                int endX = Mathf.FloorToInt((gridX + 1) * pixelsPerCellX);
                int startY = Mathf.FloorToInt(gridY * pixelsPerCellY);
                int endY = Mathf.FloorToInt((gridY + 1) * pixelsPerCellY);
                
                // Clamp to texture bounds
                startX = Mathf.Clamp(startX, 0, logo.width - 1);
                endX = Mathf.Clamp(endX, 0, logo.width);
                startY = Mathf.Clamp(startY, 0, logo.height - 1);
                endY = Mathf.Clamp(endY, 0, logo.height);
                
                // Sample the average color and alpha for this region
                Color averageColor = SampleRegionAverage(logo, startX, startY, endX, endY);
                
                // Check if this position is valid (not transparent)
                if (averageColor.a > alphaThreshold)
                {
                    // Convert grid position to world position
                    Vector3 worldPosition = GridToWorldPosition(gridX, gridY);
                    
                    validGridPositions.Add(worldPosition);
                    averageColors.Add(averageColor);
                }
            }
        }
    }
    
    private Color SampleRegionAverage(Texture2D texture, int startX, int startY, int endX, int endY)
    {
        Color totalColor = Color.clear;
        int pixelCount = 0;
        
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                Color pixelColor = texture.GetPixel(x, y);
                totalColor += pixelColor;
                pixelCount++;
            }
        }
        
        if (pixelCount > 0)
        {
            totalColor /= pixelCount;
        }
        
        return totalColor;
    }
    
    private Vector3 GridToWorldPosition(int gridX, int gridY)
    {
        // Center the grid around origin
        float offsetX = (gridDimensions.x - 1) * cellSize * 0.5f;
        float offsetY = (gridDimensions.y - 1) * cellSize * 0.5f;
        
        float worldX = (gridX * cellSize) - offsetX;
        float worldY = (gridY * cellSize) - offsetY;
        
        return new Vector3(worldX, worldY, 0f);
    }
    
    private Texture2D MakeTextureReadable(Texture2D originalTexture)
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
    
    [Button("Clear Grid Data", ButtonSizes.Medium)]
    [GUIColor(1f, 0.4f, 0.4f)]
    public void ClearGridData()
    {
        validGridPositions.Clear();
        averageColors.Clear();
        totalValidPositions = 0;
        ClearVisualizationObjects();
        Debug.Log("Grid data cleared!");
    }
    
    // Getters for other scripts to access the generated data
    public List<Vector3> GetValidGridPositions()
    {
        return new List<Vector3>(validGridPositions);
    }
    
    public List<Color> GetAverageColors()
    {
        return new List<Color>(averageColors);
    }
    
    public float GetCellSize()
    {
        return cellSize;
    }
    
    public Vector2Int GetGridDimensions()
    {
        return gridDimensions;
    }
    
    // Visualization in Scene View (Gizmos)
    private void OnDrawGizmos()
    {
        if (!showGrid || validGridPositions.Count == 0) return;
        
        // Draw grid positions
        for (int i = 0; i < validGridPositions.Count; i++)
        {
            Vector3 position = transform.TransformPoint(validGridPositions[i]);
            
            if (showValidPositions)
            {
                // Use the average color for visualization, but make it visible
                Color gizmoColor = averageColors[i];
                gizmoColor.a = 1f; // Make sure it's visible
                
                // If the color is too dark, brighten it for visibility
                if (gizmoColor.r + gizmoColor.g + gizmoColor.b < 0.5f)
                {
                    gizmoColor = Color.Lerp(gizmoColor, Color.white, 0.5f);
                }
                
                Gizmos.color = gizmoColor;
            }
            else
            {
                Gizmos.color = validPositionColor;
            }
            
            // Draw a cube to represent where an image will be placed
            Gizmos.DrawCube(position, Vector3.one * cellSize * cellVisualizationSize);
            
            // Draw wireframe for better visibility
            Gizmos.color = gridColor;
            Gizmos.DrawWireCube(position, Vector3.one * cellSize);
        }
        
        // Draw the overall bounding box
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 size = new Vector3(
            gridDimensions.x * cellSize,
            gridDimensions.y * cellSize,
            0.1f
        );
        Gizmos.DrawWireCube(center, size);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Draw more detailed info when selected
        OnDrawGizmos();
        
        if (validGridPositions.Count > 0)
        {
            // Draw text labels for debugging
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            Vector3 labelPos = transform.position + Vector3.up * (logoScale * 0.6f);
            UnityEditor.Handles.Label(labelPos, $"Grid: {gridDimensions.x}x{gridDimensions.y}\nValid Positions: {totalValidPositions}\nCell Size: {cellSize:F2}");
            #endif
        }
    }
} 