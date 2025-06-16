// 14-06-2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class PhotoMosaic : MonoBehaviour
{
    public GameObject photoPrefab; // Prefab with SpriteRenderer
    public Texture2D logoTexture; // The logo image to recreate
    public GameObject logoDisplayPrefab; // Prefab to display the reference logo (should have SpriteRenderer)
    public float animationDuration = 2f;
    [Header("Photo Settings")]
    public int photoSize = 256; // Square size for all photos
    public float minPhotoDistance = 1f; // Minimum distance between photos to prevent overlap
    [Header("Logo Settings")]
    public float logoScale = 5f; // How big the final logo should be
    public int samplingResolution = 50; // How densely to sample the logo (higher = more photos)
    public float mosaicFormationTime = 180f; // How long photos stay in mosaic formation (3 minutes)
    [Header("Mask Settings")]
    public bool useMask = true; // Enable mask functionality
    public float maskThreshold = 0.1f; // Alpha threshold for mask detection
    public bool optimizePositions = true; // Optimize logo positions to prevent overlaps
    [Header("Mosaic Reset Settings")]
    public float mosaicDisplayTime = 10f; // How long to show completed mosaic before fading
    public float fadeDuration = 2f; // How long the fade out/in takes
    public float resetInterval = 3f; // Time between fade out and reformation
    public bool showLogoReference = true; // Whether to show the logo reference during fade
    [Header("Idle Mode Settings")]
    public float scatterRadius = 15f; // How far photos can be scattered from center
    public float floatSpeed = 1f; // Speed of gentle floating movement
    public float floatRange = 2f; // How far photos can drift from their base position
    public float showcaseInterval = 5f; // How often a photo comes forward (seconds)
    public float showcaseDuration = 5f; // How long photo stays in front (5 seconds)
    public Vector3 showcasePosition = new Vector3(0, 0, -3f); // Position for showcased photo
    public Vector3 showcaseEntryPosition = new Vector3(0, -8f, -3f); // Where photos enter from (bottom of screen)

    private Queue<Sprite> photoQueue;
    private Sprite[] loadedSprites;
    private List<Vector3> logoPositions;
    private List<Vector3> optimizedLogoPositions; // Positions after overlap prevention
    private List<GameObject> spawnedPhotos;
    private List<Vector3> basePositions; // Base positions for scattered photos
    private List<Vector3> floatDirections; // Random directions for floating
    private List<Vector3> mosaicTargetPositions; // Assigned mosaic positions for each photo
    private bool isIdleMode = false;
    private bool isMosaicMode = false;
    private bool isTransitioning = false;
    private Coroutine showcaseCoroutine;
    private Coroutine floatCoroutine;
    private Coroutine mosaicResetCoroutine;
    private Coroutine mosaicFormationCoroutine;
    private GameObject currentShowcasePhoto; // Track which photo is currently being showcased
    private GameObject logoReferenceDisplay; // The logo reference display object

    private void Start()
    {
        LoadPhotosIntoQueue();
        SampleLogoPositions();
        CreateLogoReference();
        spawnedPhotos = new List<GameObject>();
        basePositions = new List<Vector3>();
        floatDirections = new List<Vector3>();
        mosaicTargetPositions = new List<Vector3>();
        currentShowcasePhoto = null;
    }

    private void CreateLogoReference()
    {
        if (logoTexture == null || logoDisplayPrefab == null)
        {
            Debug.LogWarning("Logo texture or logo display prefab not assigned. Logo reference will not be shown.");
            return;
        }

        // Create the logo reference display
        logoReferenceDisplay = Instantiate(logoDisplayPrefab, Vector3.zero, Quaternion.identity, this.transform);
        
        // Create sprite from logo texture
        Sprite logoSprite = Sprite.Create(
            logoTexture,
            new Rect(0, 0, logoTexture.width, logoTexture.height),
            new Vector2(0.5f, 0.5f)
        );

        // Set up the sprite renderer
        SpriteRenderer sr = logoReferenceDisplay.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = logoSprite;
            
            // Calculate proper scale to match mosaic size
            float aspectRatio = (float)logoTexture.width / logoTexture.height;
            float logoWidth, logoHeight;
            
            if (aspectRatio >= 1f) // Logo is wider than tall
            {
                logoWidth = logoScale;
                logoHeight = logoScale / aspectRatio;
            }
            else // Logo is taller than wide
            {
                logoWidth = logoScale * aspectRatio;
                logoHeight = logoScale;
            }
            
            // Scale the logo reference to match the mosaic size
            float scaleX = logoWidth / (logoTexture.width / 100f); // Adjust scaling factor as needed
            float scaleY = logoHeight / (logoTexture.height / 100f);
            logoReferenceDisplay.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            
            // Start invisible
            Color color = sr.color;
            color.a = 0f;
            sr.color = color;
            
            // Position slightly behind the mosaic
            logoReferenceDisplay.transform.position = new Vector3(0, 0, 0.1f);
        }
        else
        {
            Debug.LogError("Logo display prefab must have a SpriteRenderer component!");
        }
    }

    // Public function to trigger mosaic mode (can be called by button)
    [Button("Start Mosaic Mode", ButtonSizes.Large)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void StartMosaicMode()
    {
        Debug.Log("Starting Mosaic Mode");
        if (!isIdleMode)
        {
            // If not in idle mode, start idle mode first
            StartIdleMode();
            // Wait a moment then transition to mosaic
            StartCoroutine(DelayedMosaicTransition());
        }
        else
        {
            TransitionToMosaic();
        }
    }

    private IEnumerator DelayedMosaicTransition()
        {
        yield return new WaitForSeconds(1f); // Wait for idle mode to initialize
        TransitionToMosaic();
    }

    private void TransitionToMosaic()
    {
        if (isTransitioning) return;
        
        StopIdleMode();
        isIdleMode = false;
        isMosaicMode = true;
        isTransitioning = true;
        
        // Assign each floating photo to a mosaic position
        AssignMosaicPositions();
        
        // Start transition animation
        mosaicFormationCoroutine = StartCoroutine(TransitionToMosaicPositions());
    }

    private void AssignMosaicPositions()
    {
        mosaicTargetPositions.Clear();
        
        if (optimizedLogoPositions.Count == 0)
        {
            Debug.LogWarning("No optimized logo positions available!");
            return;
        }

        // Assign positions to photos, cycling through available positions if needed
        for (int i = 0; i < spawnedPhotos.Count; i++)
        {
            int positionIndex = i % optimizedLogoPositions.Count;
            mosaicTargetPositions.Add(optimizedLogoPositions[positionIndex]);
        }
        
        Debug.Log($"Assigned {mosaicTargetPositions.Count} photos to {optimizedLogoPositions.Count} logo positions");
        }

    private IEnumerator TransitionToMosaicPositions()
    {
        Debug.Log("Starting transition to mosaic positions");
        
        // Calculate photo scale for mosaic
        float photoScale = logoScale / samplingResolution * 0.8f;
        
        // Animate all photos to their assigned mosaic positions
        List<Coroutine> animationCoroutines = new List<Coroutine>();
        
        for (int i = 0; i < spawnedPhotos.Count && i < mosaicTargetPositions.Count; i++)
                {
            if (spawnedPhotos[i] != null)
                    {
                Coroutine anim = StartCoroutine(AnimatePhotoToMosaic(spawnedPhotos[i], mosaicTargetPositions[i], photoScale));
                animationCoroutines.Add(anim);
            }
        }
        
        // Wait for all animations to complete
        foreach (Coroutine anim in animationCoroutines)
        {
            yield return anim;
        }
        
        isTransitioning = false;
        Debug.Log("Mosaic formation complete! Starting formation timer.");
        
        // Start the mosaic formation timer
        StartCoroutine(MosaicFormationTimer());
    }

    private IEnumerator AnimatePhotoToMosaic(GameObject photo, Vector3 targetPosition, float targetScale)
    {
        Vector3 startPosition = photo.transform.position;
        Vector3 startScale = photo.transform.localScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            photo.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            photo.transform.localScale = Vector3.Lerp(startScale, Vector3.one * targetScale, smoothT);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        photo.transform.position = targetPosition;
        photo.transform.localScale = Vector3.one * targetScale;
    }

    private IEnumerator MosaicFormationTimer()
    {
        Debug.Log($"Mosaic will stay formed for {mosaicFormationTime} seconds ({mosaicFormationTime/60f:F1} minutes)");
        
        yield return new WaitForSeconds(mosaicFormationTime);
        
        Debug.Log("Mosaic formation time expired. Transitioning back to idle mode.");
        TransitionBackToIdle();
    }

    private void TransitionBackToIdle()
    {
        if (mosaicFormationCoroutine != null)
        {
            StopCoroutine(mosaicFormationCoroutine);
            mosaicFormationCoroutine = null;
        }
        
        isMosaicMode = false;
        isTransitioning = true;
        
        StartCoroutine(TransitionToIdlePositions());
    }

    private IEnumerator TransitionToIdlePositions()
    {
        Debug.Log("Transitioning back to idle mode");
        
        // Generate new scattered positions
        GenerateNewScatteredPositions();
        
        // Animate all photos back to scattered positions
        List<Coroutine> animationCoroutines = new List<Coroutine>();
        
        for (int i = 0; i < spawnedPhotos.Count; i++)
        {
            if (spawnedPhotos[i] != null && i < basePositions.Count)
            {
                Coroutine anim = StartCoroutine(AnimatePhotoToIdle(spawnedPhotos[i], basePositions[i]));
                animationCoroutines.Add(anim);
            }
        }
        
        // Wait for all animations to complete
        foreach (Coroutine anim in animationCoroutines)
        {
            yield return anim;
        }
        
        isTransitioning = false;
        isIdleMode = true;
        
        // Restart idle mode behaviors
        floatCoroutine = StartCoroutine(FloatPhotos());
        showcaseCoroutine = StartCoroutine(ShowcasePhotos());
        
        Debug.Log("Transition to idle mode complete!");
    }

    private IEnumerator AnimatePhotoToIdle(GameObject photo, Vector3 targetPosition)
    {
        Vector3 startPosition = photo.transform.position;
        Vector3 startScale = photo.transform.localScale;
        Vector3 targetScale = Vector3.one * 0.2f; // Back to idle scale
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            photo.transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);
            photo.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        photo.transform.position = targetPosition;
        photo.transform.localScale = targetScale;
    }

    private void GenerateNewScatteredPositions()
    {
        basePositions.Clear();
        floatDirections.Clear();
        
        for (int i = 0; i < spawnedPhotos.Count; i++)
        {
            Vector3 scatteredPosition = GetRandomScatteredPosition();
            basePositions.Add(scatteredPosition);
            floatDirections.Add(GetRandomFloatDirection());
        }
    }

    private void SampleLogoPositions()
    {
        if (logoTexture == null)
        {
            Debug.LogError("Logo texture is not assigned!");
            return;
        }

        logoPositions = new List<Vector3>();
        
        // Make logo texture readable
        Texture2D readableLogo = MakeTextureReadable(logoTexture);
        
        // Calculate aspect ratio to preserve logo proportions
        float aspectRatio = (float)readableLogo.width / readableLogo.height;
        float logoWidth, logoHeight;
        
        if (aspectRatio >= 1f) // Logo is wider than tall
        {
            logoWidth = logoScale;
            logoHeight = logoScale / aspectRatio;
        }
        else // Logo is taller than wide
        {
            logoWidth = logoScale * aspectRatio;
            logoHeight = logoScale;
        }
        
        Debug.Log($"Logo aspect ratio: {aspectRatio:F2}, Final size: {logoWidth:F2} x {logoHeight:F2}");
        
        // Sample the logo at regular intervals
        int stepX = Mathf.Max(1, readableLogo.width / samplingResolution);
        int stepY = Mathf.Max(1, readableLogo.height / samplingResolution);
        
        Debug.Log($"Sampling logo: {readableLogo.width}x{readableLogo.height}, step: {stepX}x{stepY}");
        
        for (int x = 0; x < readableLogo.width; x += stepX)
        {
            for (int y = 0; y < readableLogo.height; y += stepY)
            {
                Color pixelColor = readableLogo.GetPixel(x, y);
                
                // Use mask threshold for better detection
                if (useMask && pixelColor.a > maskThreshold)
                {
                    // Convert pixel position to world position with correct aspect ratio
                    float worldX = (x - readableLogo.width * 0.5f) / readableLogo.width * logoWidth;
                    float worldY = (y - readableLogo.height * 0.5f) / readableLogo.height * logoHeight;
                    
                    logoPositions.Add(new Vector3(worldX, worldY, 0));
                }
                else if (!useMask && pixelColor.a > 0.1f)
                {
                    // Original logic for non-mask mode
                    float worldX = (x - readableLogo.width * 0.5f) / readableLogo.width * logoWidth;
                    float worldY = (y - readableLogo.height * 0.5f) / readableLogo.height * logoHeight;
                    
                    logoPositions.Add(new Vector3(worldX, worldY, 0));
                }
            }
        }
        
        Debug.Log($"Found {logoPositions.Count} positions to place photos for logo recreation");
        
        // Optimize positions to prevent overlapping
        if (optimizePositions)
        {
            OptimizeLogoPositions();
        }
        else
        {
            optimizedLogoPositions = new List<Vector3>(logoPositions);
        }
        
        // If we don't have enough positions, duplicate some
        if (optimizedLogoPositions.Count == 0)
        {
            Debug.LogWarning("No valid positions found in logo! Using center position.");
            optimizedLogoPositions = new List<Vector3> { Vector3.zero };
        }
    }

    private void OptimizeLogoPositions()
    {
        optimizedLogoPositions = new List<Vector3>();
        
        foreach (Vector3 position in logoPositions)
        {
            bool canPlace = true;
            
            // Check distance to all already placed positions
            foreach (Vector3 placedPosition in optimizedLogoPositions)
            {
                if (Vector3.Distance(position, placedPosition) < minPhotoDistance)
                {
                    canPlace = false;
                    break;
        }
            }
            
            if (canPlace)
            {
                optimizedLogoPositions.Add(position);
            }
        }
        
        Debug.Log($"Optimized from {logoPositions.Count} to {optimizedLogoPositions.Count} positions to prevent overlaps");
    }

    private void LoadPhotosIntoQueue()
    {
        // Load all textures from Resources/Photos
        Texture2D[] photoTextures = Resources.LoadAll<Texture2D>("Photos");
        Debug.Log($"Found {photoTextures.Length} textures in Resources/Photos");

        if (photoTextures.Length == 0)
        {
            Debug.LogError("No textures found in Resources/Photos! Make sure your images are in Assets/Resources/Photos/");
            return;
        }

        // Create sprites from textures
        loadedSprites = new Sprite[photoTextures.Length];
        photoQueue = new Queue<Sprite>();

        for (int i = 0; i < photoTextures.Length; i++)
        {
            Texture2D originalTex = photoTextures[i];
            Debug.Log($"Loading texture: {originalTex.name} - Original Size: {originalTex.width}x{originalTex.height}");
            
            // Resize texture to square
            Texture2D resizedTex = ResizeTextureToSquare(originalTex, photoSize);
            Debug.Log($"Resized to: {resizedTex.width}x{resizedTex.height}");
            
            Sprite sprite = Sprite.Create(
                resizedTex,
                new Rect(0, 0, resizedTex.width, resizedTex.height),
                new Vector2(0.5f, 0.5f)
            );
            
            loadedSprites[i] = sprite;
            photoQueue.Enqueue(sprite);
        }

        Debug.Log($"Successfully loaded {loadedSprites.Length} sprites into queue");
    }

    private Texture2D ResizeTextureToSquare(Texture2D originalTexture, int targetSize)
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

    private Sprite GetNextSprite()
    {
        if (photoQueue.Count == 0)
        {
            // Refill queue when empty
            foreach (Sprite sprite in loadedSprites)
            {
                photoQueue.Enqueue(sprite);
            }
            Debug.Log("Queue was empty, refilled with all sprites");
        }

        return photoQueue.Dequeue();
    }

    private void CreateMosaic()
    {
        // This method is now deprecated in favor of TransitionToMosaic()
        // Keeping for backward compatibility but redirecting to new system
        Debug.Log("CreateMosaic called - redirecting to transition system");
        StartMosaicMode();
    }

    private Vector3 GetRandomScatteredPosition()
    {
        // Create random 3D positions in a sphere around the center
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
        float distance = UnityEngine.Random.Range(scatterRadius * 0.5f, scatterRadius);
        return randomDirection * distance;
    }

    private Quaternion GetRandomRotation()
    {
        // No rotation - keep images upright
        return Quaternion.identity;
    }

    private Vector3 GetRandomFloatDirection()
    {
        // Random direction for gentle floating
        return UnityEngine.Random.insideUnitSphere.normalized;
    }

    private IEnumerator FloatPhotos()
    {
        while (isIdleMode && !isTransitioning)
        {
            for (int i = 0; i < spawnedPhotos.Count; i++)
            {
                if (spawnedPhotos[i] != null && i < basePositions.Count && i < floatDirections.Count)
                {
                    // Skip floating movement if this photo is currently being showcased
                    if (spawnedPhotos[i] == currentShowcasePhoto)
        {
                        continue; // Don't move the showcased photo
                    }

                    // Calculate gentle floating movement for background photos only
                    float time = Time.time * floatSpeed;
                    Vector3 floatOffset = floatDirections[i] * Mathf.Sin(time + i) * floatRange;
                    
                    Vector3 targetPosition = basePositions[i] + floatOffset;
                    spawnedPhotos[i].transform.position = Vector3.Lerp(
                        spawnedPhotos[i].transform.position, 
                        targetPosition, 
                        Time.deltaTime * 2f
                    );
                    
                    // No rotation - keep images upright
                }
            }
            yield return null;
        }
    }

    private IEnumerator ShowcasePhotos()
    {
        while (isIdleMode && !isTransitioning)
        {
            yield return new WaitForSeconds(showcaseInterval);
            
            if (spawnedPhotos.Count > 0)
            {
                // Pick a random photo to showcase
                int randomIndex = UnityEngine.Random.Range(0, spawnedPhotos.Count);
                GameObject showcasePhoto = spawnedPhotos[randomIndex];
                
                if (showcasePhoto != null)
                {
                    yield return StartCoroutine(ShowcasePhoto(showcasePhoto, randomIndex));
                }
            }
        }
    }

    private IEnumerator ShowcasePhoto(GameObject photo, int originalIndex)
    {
        // Mark this photo as currently being showcased
        currentShowcasePhoto = photo;
        
        Vector3 originalPosition = photo.transform.position;
        Vector3 originalScale = photo.transform.localScale; // This will be 0.2f (1/5 size)
        
        // Phase 1: Move to entry position (bottom of screen) instantly and scale up
        photo.transform.position = showcaseEntryPosition;
        photo.transform.localScale = Vector3.one * 1f; // Full size
        photo.transform.rotation = Quaternion.identity; // Keep upright
        
        // Phase 2: Smoothly animate from bottom to center showcase position
        float elapsedTime = 0f;
        float moveTime = 1.5f; // Slightly longer for smoother animation
        
        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;
            // Use smooth curve for more elegant movement
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            photo.transform.position = Vector3.Lerp(showcaseEntryPosition, showcasePosition, smoothT);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Phase 3: Stay in showcase position for 5 seconds (completely still)
        photo.transform.position = showcasePosition;
        photo.transform.localScale = Vector3.one * 1f; // Normal size when showcased
        photo.transform.rotation = Quaternion.identity; // Keep upright
        
        yield return new WaitForSeconds(showcaseDuration); // 5 seconds - photo stays perfectly still
        
        // Phase 4: Smoothly animate back to original scattered position
        Vector3 targetScatteredPosition = basePositions[originalIndex];
        elapsedTime = 0f;
        moveTime = 2f; // Longer time for smooth return
        
        while (elapsedTime < moveTime)
        {
            float t = elapsedTime / moveTime;
            // Use smooth curve for elegant return
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            photo.transform.position = Vector3.Lerp(showcasePosition, targetScatteredPosition, smoothT);
            photo.transform.localScale = Vector3.Lerp(Vector3.one * 1f, originalScale, smoothT); // Back to 1/5 size
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Phase 5: Resume normal floating behavior
        photo.transform.localScale = originalScale; // Back to 1/5 size (0.2f)
        photo.transform.rotation = Quaternion.identity; // Keep upright
        
        // Clear the showcase tracking - photo can now float again
        currentShowcasePhoto = null;
    }

    // Public function to trigger idle mode (can be called by button)
    [Button("Start Idle Mode", ButtonSizes.Large)]
    [GUIColor(1f, 0.6f, 0.4f)]
    public void StartIdleMode()
        {
        Debug.Log("Starting Idle Mode");
        StopIdleMode();
        StopMosaicMode();
        ClearAllPhotos();
        isIdleMode = true;
        isMosaicMode = false;
        CreateIdleMode();
    }

    private void StopIdleMode()
    {
        if (showcaseCoroutine != null)
        {
            StopCoroutine(showcaseCoroutine);
            showcaseCoroutine = null;
        }
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
            floatCoroutine = null;
        }
    }

    private void StopMosaicMode()
    {
        if (mosaicResetCoroutine != null)
        {
            StopCoroutine(mosaicResetCoroutine);
            mosaicResetCoroutine = null;
        }
        if (mosaicFormationCoroutine != null)
        {
            StopCoroutine(mosaicFormationCoroutine);
            mosaicFormationCoroutine = null;
        }
    }

    [Button("Clear All Photos", ButtonSizes.Medium)]
    [GUIColor(1f, 0.4f, 0.4f)]
    private void ClearAllPhotos()
    {
        foreach (GameObject photo in spawnedPhotos)
        {
            if (photo != null)
                DestroyImmediate(photo);
        }
        spawnedPhotos.Clear();
        
        // Also hide logo reference
        if (logoReferenceDisplay != null)
        {
            SpriteRenderer sr = logoReferenceDisplay.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color color = sr.color;
                color.a = 0f;
                sr.color = color;
            }
        }
    }

    private void CreateIdleMode()
    {
        if (loadedSprites == null || loadedSprites.Length == 0)
        {
            Debug.LogError("No sprites loaded! Cannot create idle mode.");
            return;
        }

        Debug.Log($"Creating idle mode with {loadedSprites.Length} photos scattered like stars");

        basePositions.Clear();
        floatDirections.Clear();

        // Create photos in scattered positions
        for (int i = 0; i < loadedSprites.Length; i++)
        {
            Vector3 scatteredPosition = GetRandomScatteredPosition();
            GameObject photo = Instantiate(photoPrefab, scatteredPosition, Quaternion.identity);
            photo.transform.localScale = Vector3.one * 0.2f; // 1/5 size (0.2f = 1/5)

            SpriteRenderer sr = photo.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = loadedSprites[i];
                // Keep full opacity - no transparency variation
            }
            
            spawnedPhotos.Add(photo);
            basePositions.Add(scatteredPosition);
            floatDirections.Add(GetRandomFloatDirection());
        }

        // Start floating animation and showcase system
        floatCoroutine = StartCoroutine(FloatPhotos());
        showcaseCoroutine = StartCoroutine(ShowcasePhotos());
    }

    private Vector3 RandomPosition()
    {
        return new Vector3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0);
    }

    private System.Collections.IEnumerator AnimateToPosition(Transform obj, Vector3 targetPosition)
    {
        Vector3 startPosition = obj.position;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            obj.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obj.position = targetPosition;
    }

    private IEnumerator MosaicResetCycle()
    {
        while (isMosaicMode)
        {
            // Wait for mosaic to be displayed
            yield return new WaitForSeconds(mosaicDisplayTime);
            
            // Simultaneously fade out photos and fade in logo reference
            if (showLogoReference && logoReferenceDisplay != null)
            {
                StartCoroutine(FadeLogoReference(0f, 1f, fadeDuration)); // Fade in logo
            }
            yield return StartCoroutine(FadeMosaicPhotos(1f, 0f, fadeDuration)); // Fade out photos
            
            // Wait before resetting (logo reference stays visible)
            yield return new WaitForSeconds(resetInterval);
            
            // Move photos back to random positions
            foreach (GameObject photo in spawnedPhotos)
            {
                if (photo != null)
                {
                    photo.transform.position = RandomPosition();
                }
            }
            
            // Simultaneously fade in photos and fade out logo reference
            if (showLogoReference && logoReferenceDisplay != null)
            {
                StartCoroutine(FadeLogoReference(1f, 0f, fadeDuration)); // Fade out logo
            }
            yield return StartCoroutine(FadeMosaicPhotos(0f, 1f, fadeDuration)); // Fade in photos
            
            // Animate photos back to mosaic positions
            for (int i = 0; i < spawnedPhotos.Count && i < optimizedLogoPositions.Count; i++)
            {
                if (spawnedPhotos[i] != null)
                {
                    StartCoroutine(AnimateToPosition(spawnedPhotos[i].transform, optimizedLogoPositions[i]));
                }
            }
        }
    }

    private IEnumerator FadeMosaicPhotos(float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        // Get all sprite renderers
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        foreach (GameObject photo in spawnedPhotos)
        {
            if (photo != null)
            {
                SpriteRenderer sr = photo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    renderers.Add(sr);
                }
            }
        }
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            
            // Apply alpha to all photos
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null)
                {
                    Color color = sr.color;
                    color.a = currentAlpha;
                    sr.color = color;
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final alpha is set
        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
            {
                Color color = sr.color;
                color.a = toAlpha;
                sr.color = color;
            }
        }
    }

    private IEnumerator FadeLogoReference(float fromAlpha, float toAlpha, float duration)
    {
        if (logoReferenceDisplay == null) yield break;
        
        SpriteRenderer sr = logoReferenceDisplay.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            
            Color color = sr.color;
            color.a = currentAlpha;
            sr.color = color;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final alpha is set
        Color finalColor = sr.color;
        finalColor.a = toAlpha;
        sr.color = finalColor;
    }
}

