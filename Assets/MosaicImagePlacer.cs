using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using Sirenix.OdinInspector;
using MPUIKIT;
using TMPro;

[System.Serializable]
public class MosaicConfig
{
    public string photoFolderPath = "Photos";
    public bool enableSequencer = true;
    public float sequencer3DEffectDuration = 10f;
    public string instructions = "Edit this file to change settings. Restart the application after making changes.";
}

public class MosaicImagePlacer : MonoBehaviour
{
    [Header("References")]
    public MosaicGenerator mosaicGenerator; // Reference to the grid generator
    public Canvas targetCanvas; // Canvas to place UI images on
    public GameObject imagePrefab; // Prefab with Image component for displaying images
    public GameObject logoGameObject; // Logo GameObject to show/hide during fade transitions
    public Image logoImage; // Logo Image component for alpha control
    
    [Header("Image Loading")]
    public bool useSystemFolder = true; // Load from system folder instead of Resources
    public string systemFolderPath = "Photos"; // Path relative to executable or absolute path (overridden by config file)
    public string resourcesFolderPath = "Photos"; // Fallback: Path within Resources folder
    [Space(5)]
    [Header("Config File")]
    public bool useConfigFile = true; // Load photo path from config file
    public string configFileName = "MosaicConfig.json"; // Config file name
    public bool loadOnStart = false;
    public bool shuffleImages = true; // Randomize which image goes where
    public bool useAsyncLoading = true; // Load images on-demand during animation for better performance
    public int maxConcurrentLoads = 3; // Maximum images loading simultaneously (reduces memory spikes)
    
    [Header("Image Settings")]
    [Space(5)]
    [Header("Texture Quality (Memory Impact)")]
    public int squareImageSize = 512; // Texture resolution for quality (higher = better quality, more memory)
    
    [Space(5)]
    [Header("Display Size (Visual Size)")]
    public bool fitToGridCell = true; // Auto-scale display size to fit grid cells
    public float displayScale = 1f; // Manual scale multiplier for display size
    public float scalePadding = 1f; // Padding factor when fitting to cells (1f = 100% of cell size)
    public Vector2 customDisplaySize = new Vector2(100f, 100f); // Custom display size if not fitting to grid
    
    [Header("Size Calculation Info")]
    [ReadOnly] public float calculatedDisplaySize; // Shows the final calculated display size
    [ReadOnly] public string sizeCalculationMethod; // Shows how size was calculated
    [ReadOnly] public int currentlyLoading = 0; // How many images are currently being loaded
    [ReadOnly] public int totalAvailableImages = 0; // Total images found in resources folder
    
    [Header("Fly-in Animation")]
    public bool animateImagePlacement = true;
    public float flyInDuration = 3f; // Duration for each individual fly-in animation
    
    [Header("Burst Animation Settings")]
    public bool useBurstMode = true; // Use burst loading instead of sequential
    public int imagesPerBurst = 8; // How many images per burst (5-10 recommended)
    public float burstInterval = 1f; // Time between bursts
    public float burstSpread = 0.3f; // Time spread within each burst (randomization)
    public bool burstFromSameSide = true; // Each burst comes from same side or random
    
    [Header("Direction & Effects")]
    public float startDistance = 20f; // How far outside canvas to start images
    public AnimationCurve flyInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool randomizeSequence = true; // Randomize the order images appear
    public bool showSequenceProgress = true; // Show progress in console
    
    [Header("Debug Info")]
    [ReadOnly] public int loadedImageCount;
    [ReadOnly] public int placedImageCount;
    [ReadOnly] public bool imagesLoaded = false;
    
    [Header("Status Display")]
    public TMP_Text statusText; // UI Text component to show loading progress
    public bool showDetailedStatus = true; // Show detailed progress messages
    public float statusUpdateInterval = 0.1f; // How often to update status (seconds)
    public float statusFadeDuration = 0.3f; // Duration for status text fade in/out
    [ReadOnly] public string currentStatus = "Ready"; // Current status message
    [ReadOnly] public bool statusTextVisible = false; // Is status text currently visible
    
        [Header("Post-Completion Effects")]
    public bool autoStartEffectsAfterCompletion = true; // Automatically start effects when mosaic is complete
    
    [Header("3D Scatter & Float Effect")]
    public bool enable3DFloating = true; // Enable 3D floating scatter effect
    public float scatterRadius = 8f; // How far images can scatter from their original positions
    public float floatingSpeed = 1f; // Speed of gentle floating movement
    public float floatingRange = 2f; // How far images can drift while floating
    public float scatterTransitionTime = 3f; // Time to transition to scattered positions
    public Vector3 floatingBounds = new Vector3(15f, 10f, 8f); // 3D bounds for floating area
    
    [Header("Fade Out/In Cycle Effect")]
    public bool enableFadeCycles = false; // Enable fade out/in cycles
    public float fadeOutDuration = 2f; // Time to fade out
    public float fadeInDuration = 2f; // Time to fade in  
    public float fadeWaitTime = 5f; // Wait time when faded out
    public int maxFadeCycles = 3; // How many fade cycles to perform (0 = infinite)
    
    [Header("Effect Control")]
    [ReadOnly] public bool effectsActive = false;
    [ReadOnly] public bool isFloating = false;
    [ReadOnly] public bool isFading = false;
    [ReadOnly] public int currentFadeCycle = 0;
    
    [Header("Return to Formation")]
    public bool enableAutoReformation = true; // Automatically return to logo formation
    public float timeInScatteredState = 15f; // How long to stay scattered before reforming
    public float reformationDuration = 4f; // Time to transition back to logo formation
    public bool cycleBetweenStates = true; // Continuously cycle between scattered and formed
    public float timeInFormedState = 10f; // How long to stay in logo formation before scattering again
    
    [Header("Formation Control")]
    [ReadOnly] public bool isReforming = false;
    [ReadOnly] public bool isInFormation = true; // Start in formation
    [ReadOnly] public float stateTimer = 0f; // Timer for current state
    
    [Header("Logo Settings")]
    public bool enableLogoFadeTransition = true; // Enable logo visibility during fade transitions
    public float logoFadeDuration = 1f; // Duration for logo fade in/out
    public bool logoVisibleWhenImagesHidden = true; // Show logo when images are faded out
    
    [Header("Photo Frame Settings")]
    public bool enablePhotoFrame = true; // Enable photo frame during scattered state
    public int photosPerRow = 5; // Number of photos to show simultaneously in a row
    public float photoSpacing = 150f; // Horizontal spacing between photos
    public GameObject photoFrameContainer; // Container for all photo frames
    public GameObject photoFramePrefab; // Prefab for individual photo frames
    [Space(5)]
    [Header("Legacy Single Frame (Deprecated)")]
    public GameObject photoFrameObject; // The complete photo frame GameObject
    public CanvasGroup photoFrameCanvasGroup; // CanvasGroup for controlling frame visibility
    public MPImage photoFrameImage; // Image component for displaying the photo
    public RectTransform photoFrameRect; // RectTransform for positioning and animation
    
    [Header("Photo Frame Animation")]
    public float photoDisplayDuration = 5f; // How long each photo is displayed
    public float photoWaitDuration = 3f; // Wait time between photos
    public float slideInDuration = 0.8f; // Time to slide in from bottom
    public float slideOutDuration = 0.8f; // Time to slide out to top
    public float frameFadeDuration = 0.5f; // Time to fade frame in/out when state changes
    public AnimationCurve slideInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Slide in animation curve
    public AnimationCurve slideOutCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Slide out animation curve
    
    [Header("Photo Frame Debug")]
    [ReadOnly] public bool photoFrameActive = false; // Is photo frame currently active
    [ReadOnly] public bool photoFrameAnimating = false; // Is frame currently animating
    [ReadOnly] public string currentFramePhoto = ""; // Currently displayed photo name
    [ReadOnly] public List<string> currentFramePhotos = new List<string>(); // Currently displayed photo names (for multiple photos)
    
    [Header("Sequencer Control")]
    public bool enableSequencer = true; // Enable automatic sequencing
    public float sequencer3DEffectDuration = 10f; // How long to run 3D effect in sequencer mode
    [ReadOnly] public int currentSequence = 1; // Current sequence (1 or 2)
    [ReadOnly] public int currentStep = 1; // Current step within sequence
    [ReadOnly] public string nextExpectedKey = "1"; // Next key that should be pressed
    [ReadOnly] public string sequenceStatus = "Ready for Sequence 1"; // Current sequence status
    
    [Header("Keyboard Controls")]
    [InfoBox("SEQUENCER MODE:\n" +
             "Sequence 1: 1 → 2 → 3\n" +
             "Sequence 2: 4 → 2 → 3\n" +
             "Press keys in order!\n\n" +
             "Manual Controls (when sequencer disabled):\n" +
             "1 - Start Async Mosaic Formation\n" +
             "2 - Start Fade Cycles Effect\n" +
             "3 - Start 3D Scatter Effect\n" +
             "4 - Return to Formation\n" +
             "5 - Start Both Effects (Scatter + Fade)\n" +
             "6 - Start Cycling Mode\n" +
             "0 - Stop All Effects\n" +
             "R - Reset to Original Positions\n" +
             "C - Clear All Images\n" +
             "L - Test Logo Fade\n" +
             "H - Toggle Logo Visibility\n" +
             "P - Test Photo Frame\n" +
             "` - Toggle Status Text Visibility", InfoMessageType.Info)]
    public bool enableKeyboardControls = true; // Enable/disable keyboard controls
    
    private Texture2D[] availableTextures; // Available texture files (loaded instantly, lightweight) - for Resources mode
    private List<string> availableImagePaths = new List<string>(); // Available image file paths - for system folder mode
    private Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>(); // Cache of loaded sprites
    private Queue<string> imageNameQueue; // Queue of image names to use
    private List<GameObject> placedImages = new List<GameObject>();
    private HashSet<string> currentlyLoadingImages = new HashSet<string>(); // Track what's currently loading
    private List<Vector3> originalPositions = new List<Vector3>(); // Store original mosaic positions
    private List<Vector3> scatteredPositions = new List<Vector3>(); // Scattered 3D positions
    private List<Vector3> floatingDirections = new List<Vector3>(); // Random floating directions
    private Coroutine floatingCoroutine;
    private Coroutine fadeCycleCoroutine;
    private Coroutine reformationCoroutine;
    private Coroutine cyclingCoroutine;
    private Coroutine photoFrameCoroutine;
    private float lastStatusUpdate = 0f;
    private MosaicConfig currentConfig;
    private List<GameObject> activePhotoFrames = new List<GameObject>(); // Active photo frame objects
    private List<RectTransform> activeFrameRects = new List<RectTransform>(); // RectTransforms for animation
    
    // Status update methods
    private void UpdateStatus(string message, bool forceUpdate = false)
    {
        currentStatus = message;
        
        if (statusText != null && (forceUpdate || Time.time - lastStatusUpdate >= statusUpdateInterval))
        {
            statusText.text = message;
            lastStatusUpdate = Time.time;
            
            // Only show text if status is visible
            if (!statusTextVisible)
            {
                SetStatusTextAlpha(0f);
            }
            
            if (showDetailedStatus)
            {
                Debug.Log($"[STATUS] {message}");
            }
        }
    }
    
    private void SetStatusTextAlpha(float alpha)
    {
        if (statusText != null)
        {
            Color textColor = statusText.color;
            textColor.a = alpha;
            statusText.color = textColor;
        }
    }
    
    private void ToggleStatusTextVisibility()
    {
        if (statusText == null) return;
        
        statusTextVisible = !statusTextVisible;
        
        StopCoroutine("FadeStatusText"); // Stop any existing fade
        StartCoroutine(FadeStatusText(statusTextVisible ? 1f : 0f));
        
        Debug.Log($"Status text visibility: {(statusTextVisible ? "ON" : "OFF")}");
    }
    
    private IEnumerator FadeStatusText(float targetAlpha)
    {
        if (statusText == null) yield break;
        
        float startAlpha = statusText.color.a;
        float elapsedTime = 0f;
        
        while (elapsedTime < statusFadeDuration)
        {
            float t = elapsedTime / statusFadeDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetStatusTextAlpha(currentAlpha);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        SetStatusTextAlpha(targetAlpha);
    }
    
    private void UpdateStatusWithProgress(string baseMessage, int current, int total, string additionalInfo = "")
    {
        float percentage = total > 0 ? (float)current / total * 100f : 0f;
        string progressBar = GenerateProgressBar(percentage);
        string message = $"{baseMessage}\n{progressBar} {current}/{total} ({percentage:F1}%)";
        
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            message += $"\n{additionalInfo}";
        }
        
        UpdateStatus(message);
    }
    
    private string GenerateProgressBar(float percentage, int length = 20)
    {
        int filledLength = Mathf.RoundToInt(percentage / 100f * length);
        string bar = "[";
        
        for (int i = 0; i < length; i++)
        {
            if (i < filledLength)
                bar += "█";
            else
                bar += "░";
        }
        
        bar += "]";
        return bar;
    }
    
    // ===== SEQUENCER METHODS =====
    
    private void InitializeSequencer()
    {
        currentSequence = 1;
        currentStep = 1;
        nextExpectedKey = "1";
        sequenceStatus = "Ready for Sequence 1 - Press '1' to start";
        UpdateStatus("Sequencer ready!\nPress '1' to begin Sequence 1", true);
    }
    
    private bool IsValidKeyForSequence(KeyCode keyCode)
    {
        if (!enableSequencer) return true; // Allow all keys if sequencer disabled
        
        string keyPressed = GetKeyCodeString(keyCode);
        return keyPressed == nextExpectedKey;
    }
    
    private string GetKeyCodeString(KeyCode keyCode)
    {
        switch (keyCode)
        {
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            default: return "";
        }
    }
    
    private void AdvanceSequence()
    {
        if (!enableSequencer) return;
        
        currentStep++;
        
        // Sequence 1: 1 → 2 → 3
        if (currentSequence == 1)
        {
            switch (currentStep)
            {
                case 2:
                    nextExpectedKey = "2";
                    sequenceStatus = "Sequence 1, Step 2 - Press '2' for fade cycles";
                    UpdateStatus("Images placed! Next: Press '2' for fade cycles", true);
                    break;
                case 3:
                    nextExpectedKey = "3";
                    sequenceStatus = "Sequence 1, Step 3 - Press '3' for 3D scatter";
                    UpdateStatus("Fade cycles ready! Next: Press '3' for 3D effects", true);
                    break;
                case 4:
                    // Sequence 1 complete, move to Sequence 2
                    currentSequence = 2;
                    currentStep = 1;
                    nextExpectedKey = "4";
                    sequenceStatus = "Sequence 2, Step 1 - Press '4' to return to formation";
                    UpdateStatus("Sequence 1 complete! Next: Press '4' to return to formation", true);
                    break;
            }
        }
        // Sequence 2: 4 → 2 → 3
        else if (currentSequence == 2)
        {
            switch (currentStep)
            {
                case 2:
                    nextExpectedKey = "2";
                    sequenceStatus = "Sequence 2, Step 2 - Press '2' for fade cycles";
                    UpdateStatus("Back in formation! Next: Press '2' for fade cycles", true);
                    break;
                case 3:
                    nextExpectedKey = "3";
                    sequenceStatus = "Sequence 2, Step 3 - Press '3' for 3D scatter";
                    UpdateStatus("Fade cycles ready! Next: Press '3' for 3D effects", true);
                    break;
                case 4:
                    // Sequence 2 complete, loop back to Sequence 2
                    currentStep = 1;
                    nextExpectedKey = "4";
                    sequenceStatus = "Sequence 2 complete - Press '4' to restart cycle";
                    UpdateStatus("Sequence 2 complete! Press '4' to restart cycle", true);
                    break;
            }
        }
        
        Debug.Log($"[SEQUENCER] Advanced to Sequence {currentSequence}, Step {currentStep}. Next key: {nextExpectedKey}");
    }
    
    private void Start()
    {
        // Auto-find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogWarning("No Canvas found! Creating one...");
                CreateCanvas();
            }
        }
        
        // Load config file first (this may override systemFolderPath)
        LoadConfigFile();
        
        // Discover available images (lightweight operation)
        DiscoverAvailableImages();
        
        // Initialize sequencer
        if (enableSequencer)
        {
            InitializeSequencer();
        }
        
        // Initialize status text as invisible
        if (statusText != null)
        {
            SetStatusTextAlpha(0f);
            statusTextVisible = false;
        }
        
        if (loadOnStart)
        {
            LoadAndPlaceImages();
        }
    }
    
    private void Update()
    {
        if (!enableKeyboardControls) return;
        
        // Handle sequenced keyboard input
        if (enableSequencer)
        {
            HandleSequencedInput();
        }
        else
        {
            HandleManualInput();
        }
        
        // Additional utility controls (always available)
        HandleUtilityControls();
    }
    
    private void HandleSequencedInput()
    {
        // Only allow the expected key in sequence
        if (Input.GetKeyDown(KeyCode.Alpha1) && IsValidKeyForSequence(KeyCode.Alpha1))
        {
            Debug.Log($"[SEQUENCER] Key 1 pressed - Starting Mosaic Formation (Sequence {currentSequence}, Step {currentStep})");
            LoadAndPlaceImages();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && IsValidKeyForSequence(KeyCode.Alpha2))
        {
            Debug.Log($"[SEQUENCER] Key 2 pressed - Starting Fade Cycles (Sequence {currentSequence}, Step {currentStep})");
            StartFadeEffect();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && IsValidKeyForSequence(KeyCode.Alpha3))
        {
            Debug.Log($"[SEQUENCER] Key 3 pressed - Starting 3D Scatter (Sequence {currentSequence}, Step {currentStep})");
            StartFloatingEffect();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) && IsValidKeyForSequence(KeyCode.Alpha4))
        {
            Debug.Log($"[SEQUENCER] Key 4 pressed - Returning to Formation (Sequence {currentSequence}, Step {currentStep})");
            ManualReturnToFormation();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || 
                 Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4))
        {
            // Wrong key pressed
            UpdateStatus($"Wrong key! Expected: '{nextExpectedKey}'\n{sequenceStatus}", true);
            Debug.LogWarning($"[SEQUENCER] Wrong key pressed! Expected: {nextExpectedKey}, Current: {sequenceStatus}");
        }
    }
    
    private void HandleManualInput()
    {
        // Original manual controls
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Keyboard: Starting Async Mosaic Formation (Key 1)");
            LoadAndPlaceImages();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Keyboard: Starting Fade Cycles Effect (Key 2)");
            StartFadeEffect();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Keyboard: Starting 3D Scatter Effect (Key 3)");
            StartFloatingEffect();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Keyboard: Returning to Formation (Key 4)");
            ManualReturnToFormation();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Debug.Log("Keyboard: Starting Both Effects - Scatter + Fade (Key 5)");
            StartBothEffects();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Debug.Log("Keyboard: Starting Cycling Mode (Key 6)");
            StartCyclingMode();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log("Keyboard: Stopping All Effects (Key 0)");
            StopEffects();
        }
    }
    
    private void HandleUtilityControls()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Keyboard: Resetting to Original Positions (Key R)");
            ResetPositions();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("Keyboard: Clearing All Images (Key C)");
            ClearPlacedImages();
        }
        else if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Keyboard: Testing Logo Fade (Key L)");
            TestLogoFade();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Keyboard: Toggling Logo Visibility (Key H)");
            if (logoGameObject != null && logoGameObject.activeSelf)
            {
                HideLogo();
            }
            else
            {
                ShowLogo();
            }
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Keyboard: Testing Photo Frame (Key P)");
            TestPhotoFrame();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0) && enableSequencer)
        {
            Debug.Log("Keyboard: Emergency Stop All Effects (Key 0)");
            StopEffects();
        }
        else if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            Debug.Log("Keyboard: Toggling Status Text Visibility (Key `)");
            ToggleStatusTextVisibility();
        }
    }
    
    private void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("MosaicCanvas");
        targetCanvas = canvasGO.AddComponent<Canvas>();
        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        Debug.Log("Created new Canvas for mosaic images");
    }
    
    private void LoadConfigFile()
    {
        if (!useConfigFile) return;
        
        string configPath = GetConfigFilePath();
        
        try
        {
            if (File.Exists(configPath))
            {
                string jsonContent = File.ReadAllText(configPath);
                currentConfig = JsonUtility.FromJson<MosaicConfig>(jsonContent);
                
                // Apply config settings
                systemFolderPath = currentConfig.photoFolderPath;
                enableSequencer = currentConfig.enableSequencer;
                sequencer3DEffectDuration = currentConfig.sequencer3DEffectDuration;
                
                UpdateStatus($"Config loaded!\nPhoto path: {systemFolderPath}", true);
                Debug.Log($"Config loaded from: {configPath}");
                Debug.Log($"Photo folder path set to: {systemFolderPath}");
            }
            else
            {
                CreateDefaultConfig();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load config file: {e.Message}");
            CreateDefaultConfig();
        }
    }
    
    private void CreateDefaultConfig()
    {
        currentConfig = new MosaicConfig();
        currentConfig.photoFolderPath = systemFolderPath;
        currentConfig.enableSequencer = enableSequencer;
        currentConfig.sequencer3DEffectDuration = sequencer3DEffectDuration;
        
        SaveConfigFile();
    }
    
    private void SaveConfigFile()
    {
        if (!useConfigFile) return;
        
        string configPath = GetConfigFilePath();
        
        try
        {
            string jsonContent = JsonUtility.ToJson(currentConfig, true);
            File.WriteAllText(configPath, jsonContent);
            
            UpdateStatus("Config file created!\nEdit to change photo path", true);
            Debug.Log($"Config saved to: {configPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save config file: {e.Message}");
        }
    }
    
    private string GetConfigFilePath()
    {
        string basePath = Application.dataPath;
        
        // In builds, go up one level from Data folder to be next to executable
        if (!Application.isEditor)
        {
            basePath = Directory.GetParent(Application.dataPath).FullName;
        }
        else
        {
            // In editor, go up to project root
            basePath = Directory.GetParent(Application.dataPath).FullName;
        }
        
        return Path.Combine(basePath, configFileName);
    }
    
    [Button("Reload Config File", ButtonSizes.Medium)]
    [GUIColor(0.6f, 1f, 0.6f)]
    public void ReloadConfigFile()
    {
        if (useConfigFile)
        {
            LoadConfigFile();
            if (useSystemFolder)
            {
                DiscoverAvailableImages();
            }
        }
        else
        {
            Debug.LogWarning("Config file usage is disabled!");
        }
    }
    
    [Button("Open Config Folder", ButtonSizes.Medium)]
    [GUIColor(0.6f, 0.8f, 1f)]
    public void OpenConfigFolder()
    {
        string configPath = GetConfigFilePath();
        string folderPath = Path.GetDirectoryName(configPath);
        
        // Open folder in system explorer
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", folderPath);
        #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            System.Diagnostics.Process.Start("xdg-open", folderPath);
        #endif
        
        Debug.Log($"Config folder: {folderPath}");
    }
    
    private void DiscoverAvailableImages()
    {
        UpdateStatus("Scanning for employee photos...", true);
        
        if (useSystemFolder)
        {
            DiscoverSystemFolderImages();
        }
        else
        {
            DiscoverResourcesImages();
        }
    }
    
    private void DiscoverSystemFolderImages()
    {
        string fullPath = GetSystemFolderPath();
        Debug.Log($"Discovering available images in system folder: {fullPath}");
        
        availableImagePaths.Clear();
        
        if (!Directory.Exists(fullPath))
        {
            UpdateStatus($"Photo folder not found!\nCreating: {fullPath}", true);
            Debug.LogWarning($"System folder does not exist: {fullPath}. Creating it...");
            
            try
            {
                Directory.CreateDirectory(fullPath);
                UpdateStatus("Photo folder created!\nPlease add images and restart.", true);
                Debug.Log($"Created directory: {fullPath}");
            }
            catch (System.Exception e)
            {
                UpdateStatus("Failed to create photo folder!\nCheck permissions.", true);
                Debug.LogError($"Failed to create directory: {e.Message}");
            }
            return;
        }
        
        // Supported image extensions
        string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tga" };
        
        foreach (string extension in extensions)
        {
            string[] files = Directory.GetFiles(fullPath, "*" + extension, SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                availableImagePaths.Add(file);
            }
            
            // Also check uppercase
            string[] filesUpper = Directory.GetFiles(fullPath, "*" + extension.ToUpper(), SearchOption.TopDirectoryOnly);
            foreach (string file in filesUpper)
            {
                if (!availableImagePaths.Contains(file))
                {
                    availableImagePaths.Add(file);
                }
            }
        }
        
        if (availableImagePaths.Count == 0)
        {
            UpdateStatus($"No photos found in folder!\nAdd images to: {fullPath}", true);
            Debug.LogError($"No images found in system folder: {fullPath}");
            return;
        }
        
        totalAvailableImages = availableImagePaths.Count;
        UpdateStatus($"Found {totalAvailableImages} employee photos!\nPreparing for mosaic creation...", true);
        Debug.Log($"Found {totalAvailableImages} images in system folder (will load on demand)");
        isLoaded = true;
        CreateImageNameQueue();
    }
    
    private void DiscoverResourcesImages()
    {
        Debug.Log($"Discovering available images in Resources/{resourcesFolderPath}...");
        
        // Load texture references (lightweight - just file names and metadata)
        availableTextures = Resources.LoadAll<Texture2D>(resourcesFolderPath);
        
        if (availableTextures.Length == 0)
        {
            UpdateStatus("No employee photos found!\nPlease check the photo folder.", true);
            Debug.LogError($"No images found in Resources/{resourcesFolderPath}! Make sure your images are in Assets/Resources/{resourcesFolderPath}/");
            return;
        }
        
        totalAvailableImages = availableTextures.Length;
        UpdateStatus($"Found {totalAvailableImages} employee photos!\nPreparing for mosaic creation...", true);
        Debug.Log($"Found {totalAvailableImages} available images (not loaded yet - will load on demand)");
        isLoaded = true;
        CreateImageNameQueue();
    }
    
    private string GetSystemFolderPath()
    {
        // If it's an absolute path, use it directly
        if (Path.IsPathRooted(systemFolderPath))
        {
            return systemFolderPath;
        }
        
        // Otherwise, make it relative to the executable
        string basePath = Application.dataPath;
        
        // In builds, Application.dataPath points to the Data folder
        // We want to go up one level to be next to the executable
        if (!Application.isEditor)
        {
            basePath = Directory.GetParent(Application.dataPath).FullName;
        }
        else
        {
            // In editor, go up to the project root
            basePath = Directory.GetParent(Application.dataPath).FullName;
        }
        
        return Path.Combine(basePath, systemFolderPath);
    }
    
    private void CreateImageNameQueue()
    {
        UpdateStatus("Organizing employee photos...");
        
        imageNameQueue = new Queue<string>();
        List<string> imageNames = new List<string>();
        
        if (useSystemFolder)
        {
            // Collect all image file paths
            foreach (string filePath in availableImagePaths)
            {
                imageNames.Add(filePath);
            }
        }
        else
        {
            // Collect all image names from Resources
            foreach (Texture2D texture in availableTextures)
            {
                imageNames.Add(texture.name);
            }
        }
        
        // Shuffle if requested
        if (shuffleImages)
        {
            UpdateStatus("Randomizing photo arrangement...");
            for (int i = 0; i < imageNames.Count; i++)
            {
                string temp = imageNames[i];
                int randomIndex = Random.Range(i, imageNames.Count);
                imageNames[i] = imageNames[randomIndex];
                imageNames[randomIndex] = temp;
            }
            Debug.Log("Shuffled image order for random placement");
        }
        
        // Add to queue
        foreach (string imageName in imageNames)
        {
            imageNameQueue.Enqueue(imageName);
        }
        
        UpdateStatus($"Photo queue ready!\n{imageNameQueue.Count} employee photos organized");
        Debug.Log($"Created image queue with {imageNameQueue.Count} images");
    }
    
    private IEnumerator LoadImageAsync(string imageName)
    {
        // Check if already loaded
        if (loadedSprites.ContainsKey(imageName))
        {
            yield break; // Already loaded
        }
        
        // Check if currently loading
        if (currentlyLoadingImages.Contains(imageName))
        {
            // Wait for it to finish loading
            while (currentlyLoadingImages.Contains(imageName) && !loadedSprites.ContainsKey(imageName))
            {
                yield return null;
            }
            yield break;
        }
        
        // Start loading
        currentlyLoadingImages.Add(imageName);
        currentlyLoading = currentlyLoadingImages.Count;
        
        Texture2D originalTexture = null;
        
        if (useSystemFolder)
        {
            // Load from system file
            yield return StartCoroutine(LoadTextureFromFile(imageName, (result) => {
                originalTexture = result;
            }));
        }
        else
        {
            // Load from Resources
            foreach (Texture2D texture in availableTextures)
            {
                if (texture.name == imageName)
                {
                    originalTexture = texture;
                    break;
                }
            }
        }
        
        if (originalTexture == null)
        {
            Debug.LogError($"Could not load texture: {imageName}");
            currentlyLoadingImages.Remove(imageName);
            currentlyLoading = currentlyLoadingImages.Count;
            yield break;
        }
        
        // Create square texture (spread over multiple frames to avoid frame drops)
        Texture2D squareTexture = null;
        yield return StartCoroutine(CreateSquareTextureAsync(originalTexture, squareImageSize, (result) => {
            squareTexture = result;
        }));
        
        if (squareTexture != null)
        {
            // Create sprite
            string spriteName = useSystemFolder ? Path.GetFileNameWithoutExtension(imageName) : imageName;
            Sprite sprite = Sprite.Create(
                squareTexture,
                new Rect(0, 0, squareTexture.width, squareTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            sprite.name = spriteName;
            
            // Add to cache
            loadedSprites[imageName] = sprite;
            loadedImageCount = loadedSprites.Count;
            
            // Update loading progress
            string displayName = useSystemFolder ? Path.GetFileName(imageName) : imageName;
            UpdateStatusWithProgress("📸 Loading employee photos", loadedImageCount, totalAvailableImages, 
                $"Processing: {displayName}");
            
            Debug.Log($"Async loaded: {displayName} ({loadedImageCount}/{totalAvailableImages} total loaded)");
        }
        
        // Finished loading
        currentlyLoadingImages.Remove(imageName);
        currentlyLoading = currentlyLoadingImages.Count;
    }
    
    private IEnumerator LoadTextureFromFile(string filePath, System.Action<Texture2D> callback)
    {
        string url = "file://" + filePath;
        
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            callback(texture);
        }
        else
        {
            Debug.LogError($"Failed to load texture from file: {filePath}\nError: {request.error}");
            callback(null);
        }
        
        request.Dispose();
    }
    
    private IEnumerator CreateSquareTextureAsync(Texture2D originalTexture, int targetSize, System.Action<Texture2D> callback)
    {
        // Make the original texture readable
        Texture2D readableTexture = MakeTextureReadable(originalTexture);
        yield return null; // Give frame break
        
        // Create new square texture
        Texture2D squareTexture = new Texture2D(targetSize, targetSize, TextureFormat.RGB24, false);
        
        // Get original pixels
        Color[] originalPixels = readableTexture.GetPixels();
        Color[] squarePixels = new Color[targetSize * targetSize];
        
        // Calculate scaling ratios
        float xRatio = (float)readableTexture.width / targetSize;
        float yRatio = (float)readableTexture.height / targetSize;
        
        int pixelsProcessed = 0;
        int pixelsPerFrame = targetSize * 4; // Process 4 rows per frame to avoid stuttering
        
        // Resample pixels to create square image (spread across multiple frames)
        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                // Map square coordinates back to original image coordinates
                int originalX = Mathf.FloorToInt(x * xRatio);
                int originalY = Mathf.FloorToInt(y * yRatio);
                
                // Clamp to avoid out of bounds
                originalX = Mathf.Clamp(originalX, 0, readableTexture.width - 1);
                originalY = Mathf.Clamp(originalY, 0, readableTexture.height - 1);
                
                // Copy pixel from original to square texture
                squarePixels[y * targetSize + x] = originalPixels[originalY * readableTexture.width + originalX];
                
                pixelsProcessed++;
                
                // Give frame break every few rows to maintain smooth framerate
                if (pixelsProcessed >= pixelsPerFrame)
                {
                    pixelsProcessed = 0;
                    yield return null;
                }
            }
        }
        
        // Apply pixels and return
        squareTexture.SetPixels(squarePixels);
        squareTexture.Apply();
        
        callback(squareTexture);
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
    
    [Button("Place Images at Grid Positions", ButtonSizes.Large)]
    [GUIColor(0.2f, 1f, 0.2f)]
    public void PlaceImagesAtGridPositions()
    {
        if (mosaicGenerator == null)
        {
            Debug.LogError("MosaicGenerator reference is not assigned!");
            return;
        }
        
        if (targetCanvas == null)
        {
            Debug.LogError("Target Canvas is not assigned!");
            return;
        }
        
        if (totalAvailableImages == 0)
        {
            Debug.LogWarning("No images discovered! Discovering images first...");
            DiscoverAvailableImages();
            if (totalAvailableImages == 0) return;
        }
        
        List<Vector3> gridPositions = mosaicGenerator.GetValidGridPositions();
        if (gridPositions.Count == 0)
        {
            Debug.LogError("No grid positions available! Generate grid in MosaicGenerator first.");
            return;
        }
        
        UpdateStatus($"Preparing mosaic formation...\n{gridPositions.Count} positions ready");
        Debug.Log($"Placing images at {gridPositions.Count} grid positions using async loading...");
        
        // Clear existing placed images
        ClearPlacedImages();
        
        // Calculate image size
        float finalSize = CalculateDisplaySize();
        UpdateStatus($"Calculating optimal image size...\nSize: {finalSize:F0}px");
        
        // Place images at grid positions
        if (animateImagePlacement && useAsyncLoading)
        {
            StartCoroutine(PlaceImagesWithFlyIn(gridPositions, finalSize));
        }
        else if (animateImagePlacement)
        {
            // Fallback to sync loading if async disabled
            Debug.LogWarning("Async loading disabled - this may cause frame drops with many images!");
            StartCoroutine(PlaceImagesWithFlyIn(gridPositions, finalSize));
        }
        else
        {
            PlaceImagesInstantly(gridPositions, finalSize);
        }
    }
    
    private float CalculateDisplaySize()
    {
        float finalSize;
        
        if (fitToGridCell)
        {
            // Calculate size based on grid cells
            float gridCellSize = mosaicGenerator.GetCellSize();
            finalSize = gridCellSize * scalePadding * 100f; // Convert to UI units
            sizeCalculationMethod = $"Grid-based: {gridCellSize:F2} * {scalePadding:F2} * 100 = {finalSize:F1}px";
        }
        else
        {
            // Use custom display size
            finalSize = Mathf.Max(customDisplaySize.x, customDisplaySize.y) * displayScale;
            sizeCalculationMethod = $"Custom: {customDisplaySize} * {displayScale:F2} = {finalSize:F1}px";
        }
        
        calculatedDisplaySize = finalSize;
        
        Debug.Log($"Display Size Calculation: {sizeCalculationMethod}");
        Debug.Log($"Texture Resolution: {squareImageSize}x{squareImageSize} pixels");
        Debug.Log($"Display Size: {finalSize:F1}x{finalSize:F1} UI units");
        
        return finalSize;
    }
    
    private void PlaceImagesInstantly(List<Vector3> gridPositions, float size)
    {
        for (int i = 0; i < gridPositions.Count; i++)
        {
            GameObject imageObject = CreateImageAtPosition(gridPositions[i], size, i);
            if (imageObject != null)
            {
                placedImages.Add(imageObject);
            }
        }
        
        placedImageCount = placedImages.Count;
        Debug.Log($"Instantly placed {placedImageCount} images!");
    }
    
    private IEnumerator PlaceImagesWithFlyIn(List<Vector3> gridPositions, float size)
    {
        placedImages.Clear();
        isPlaced = false;
        UpdateStatus("🎬 Preparing animation sequence...");
        
        // Create sequence order
        List<int> sequence = new List<int>();
        for (int i = 0; i < gridPositions.Count; i++)
        {
            sequence.Add(i);
        }
        
        // Randomize sequence if requested
        if (randomizeSequence)
        {
            UpdateStatus("Randomizing animation order...");
            for (int i = 0; i < sequence.Count; i++)
            {
                int temp = sequence[i];
                int randomIndex = Random.Range(i, sequence.Count);
                sequence[i] = sequence[randomIndex];
                sequence[randomIndex] = temp;
            }
            Debug.Log("Randomized image sequence for more organic appearance");
        }
        
        if (useBurstMode)
        {
            yield return StartCoroutine(PlaceImagesInBursts(sequence, gridPositions, size));
        }
        else
        {
            yield return StartCoroutine(PlaceImagesSequentially(sequence, gridPositions, size));
        }
        
        placedImageCount = placedImages.Count;
        isPlaced = true;
        UpdateStatus($"Mosaic complete!\n{placedImageCount} employee photos successfully arranged", true);
        Debug.Log($"Animation complete! All {placedImageCount} images have flown in!");
        
        // Store original positions for effects
        StoreOriginalPositions();
        
        // Advance sequencer for mosaic completion
        if (enableSequencer)
        {
            yield return new WaitForSeconds(1f); // Brief pause to show completion
            AdvanceSequence();
        }
        
        // Start post-completion effects if enabled (only in manual mode)
        if (autoStartEffectsAfterCompletion && !enableSequencer)
        {
            UpdateStatus("✨ Initializing special effects...");
            yield return new WaitForSeconds(1f); // Brief pause to admire completed mosaic
            StartPostCompletionEffects();
        }
    }

    public bool isLoaded;
    public bool isPlaced;
    private IEnumerator PlaceImagesInBursts(List<int> sequence, List<Vector3> gridPositions, float size)
    {
        int totalBursts = Mathf.CeilToInt((float)sequence.Count / imagesPerBurst);
        
        UpdateStatus($"Starting burst animation!\n{totalBursts} bursts • {imagesPerBurst} photos per burst");
        Debug.Log($"Starting burst animation: {totalBursts} bursts of {imagesPerBurst} images each");
        
        // Pre-load first burst
        UpdateStatus("Pre-loading first batch of photos...");
        int preloadCount = Mathf.Min(imagesPerBurst * 2, sequence.Count);
        yield return StartCoroutine(PreloadImageBatch(preloadCount));
        
        for (int burstIndex = 0; burstIndex < totalBursts; burstIndex++)
        {
            int startIdx = burstIndex * imagesPerBurst;
            int endIdx = Mathf.Min(startIdx + imagesPerBurst, sequence.Count);
            int burstSize = endIdx - startIdx;
            
            // Choose direction for this entire burst
            int burstDirection = burstFromSameSide ? Random.Range(0, 4) : -1;
            
            UpdateStatusWithProgress("Launching photo bursts", burstIndex + 1, totalBursts, 
                $"Burst from {GetDirectionName(burstDirection)} • {burstSize} photos");
            Debug.Log($"Launching burst {burstIndex + 1}/{totalBursts} ({burstSize} images) from {GetDirectionName(burstDirection)}");
            
            // Launch all images in this burst simultaneously
            List<Coroutine> burstCoroutines = new List<Coroutine>();
            
            for (int i = startIdx; i < endIdx; i++)
            {
                int imageIndex = sequence[i];
                Vector3 targetPosition = gridPositions[imageIndex];
                
                // Get image and ensure it's loaded
                string imageName = GetNextImageName();
                if (imageName == null) break;
                
                yield return StartCoroutine(EnsureImageLoaded(imageName));
                
                // Generate position for this burst
                Vector3 startPosition = GetBurstStartPosition(burstDirection);
                
                // Create image object
                GameObject imageObject = CreateImageAtPositionWithSprite(startPosition, size, imageIndex, loadedSprites[imageName]);
                if (imageObject != null)
                {
                    placedImages.Add(imageObject);
                    
                    // Add random delay within burst for natural feel
                    float burstDelay = Random.Range(0f, burstSpread);
                    Coroutine flyIn = StartCoroutine(AnimateImageFlyIn(imageObject, startPosition, targetPosition, burstDelay));
                    burstCoroutines.Add(flyIn);
                }
            }
            
            // Start loading next burst in background
            if (burstIndex + 1 < totalBursts)
            {
                int nextBatchStart = endIdx;
                int nextBatchSize = Mathf.Min(imagesPerBurst, sequence.Count - nextBatchStart);
                StartCoroutine(PreloadImageBatch(nextBatchSize));
            }
            
            // Show progress
            if (showSequenceProgress)
            {
                float progress = ((float)(burstIndex + 1) / totalBursts) * 100f;
                Debug.Log($"Burst {burstIndex + 1}/{totalBursts} launched ({progress:F1}% complete)");
            }
            
            // Wait before next burst
            if (burstIndex < totalBursts - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }
        
        // Wait for all animations to complete
        yield return new WaitForSeconds(flyInDuration + burstSpread);
    }
    
    private IEnumerator PlaceImagesSequentially(List<int> sequence, List<Vector3> gridPositions, float size)
    {
        // Fallback to old sequential method
        float sequenceDelay = 0.15f;
        
        Debug.Log("Using sequential loading (fallback mode)");
        
        for (int sequenceIndex = 0; sequenceIndex < sequence.Count; sequenceIndex++)
        {
            int imageIndex = sequence[sequenceIndex];
            Vector3 targetPosition = gridPositions[imageIndex];
            
            string imageName = GetNextImageName();
            if (imageName == null) break;
            
            yield return StartCoroutine(EnsureImageLoaded(imageName));
            
            Vector3 startPosition = GetRandomOffCanvasPosition();
            GameObject imageObject = CreateImageAtPositionWithSprite(startPosition, size, imageIndex, loadedSprites[imageName]);
            
            if (imageObject != null)
            {
                placedImages.Add(imageObject);
                StartCoroutine(AnimateImageFlyIn(imageObject, startPosition, targetPosition, 0f));
            }
            
            yield return new WaitForSeconds(sequenceDelay);
        }
    }
    
    private Vector3 GetBurstStartPosition(int direction)
    {
        if (direction == -1)
        {
            // Random direction (not burst mode)
            return GetRandomOffCanvasPosition();
        }
        
        // Get canvas bounds
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        
        float canvasLeft = canvasCorners[0].x;
        float canvasRight = canvasCorners[2].x;
        float canvasBottom = canvasCorners[0].y;
        float canvasTop = canvasCorners[2].y;
        
        // Expand bounds
        float expandedLeft = canvasLeft - startDistance;
        float expandedRight = canvasRight + startDistance;
        float expandedBottom = canvasBottom - startDistance;
        float expandedTop = canvasTop + startDistance;
        
        Vector3 startPos = Vector3.zero;
        
        switch (direction)
        {
            case 0: // Top
                startPos = new Vector3(
                    Random.Range(expandedLeft, expandedRight), 
                    expandedTop, 
                    0
                );
                break;
            case 1: // Right
                startPos = new Vector3(
                    expandedRight, 
                    Random.Range(expandedBottom, expandedTop), 
                    0
                );
                break;
            case 2: // Bottom
                startPos = new Vector3(
                    Random.Range(expandedLeft, expandedRight), 
                    expandedBottom, 
                    0
                );
                break;
            case 3: // Left
                startPos = new Vector3(
                    expandedLeft, 
                    Random.Range(expandedBottom, expandedTop), 
                    0
                );
                break;
        }
        
        // Convert to local canvas position
        Camera cam = targetCanvas.worldCamera ?? Camera.main;
        Vector3 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            cam.WorldToScreenPoint(startPos), 
            cam, 
            out Vector2 localPoint
        );
        localPos = localPoint;
        
        return localPos;
    }
    
    private string GetDirectionName(int direction)
    {
        switch (direction)
        {
            case 0: return "Top";
            case 1: return "Right";
            case 2: return "Bottom";
            case 3: return "Left";
            default: return "Random";
        }
    }
    
    private IEnumerator PreloadImageBatch(int count)
    {
        List<string> imagesToLoad = new List<string>();
        
        // Get image names for next batch
        for (int i = 0; i < count && imageNameQueue.Count > 0; i++)
        {
            string imageName = PeekNextImageName(i);
            if (imageName != null && !loadedSprites.ContainsKey(imageName))
            {
                imagesToLoad.Add(imageName);
            }
        }
        
        // Start loading all images in parallel (limited by maxConcurrentLoads)
        List<Coroutine> loadingCoroutines = new List<Coroutine>();
        int concurrentCount = 0;
        
        foreach (string imageName in imagesToLoad)
        {
            if (concurrentCount < maxConcurrentLoads)
            {
                loadingCoroutines.Add(StartCoroutine(LoadImageAsync(imageName)));
                concurrentCount++;
            }
            else
            {
                // Wait for one to finish before starting next
                yield return loadingCoroutines[0];
                loadingCoroutines.RemoveAt(0);
                loadingCoroutines.Add(StartCoroutine(LoadImageAsync(imageName)));
            }
        }
        
        // Wait for all remaining to finish
        foreach (Coroutine loadCoroutine in loadingCoroutines)
        {
            yield return loadCoroutine;
        }
        
        Debug.Log($"Pre-loaded batch of {imagesToLoad.Count} images");
    }
    
    private string GetNextImageName()
    {
        if (imageNameQueue.Count == 0)
        {
            CreateImageNameQueue(); // Refill if empty
        }
        
        return imageNameQueue.Count > 0 ? imageNameQueue.Dequeue() : null;
    }
    
    private string PeekNextImageName(int offset)
    {
        if (imageNameQueue.Count <= offset) return null;
        
        // Convert queue to array to peek ahead
        string[] queueArray = imageNameQueue.ToArray();
        return queueArray[offset];
    }
    
    private IEnumerator EnsureImageLoaded(string imageName)
    {
        // Start loading if not already loaded or loading
        if (!loadedSprites.ContainsKey(imageName))
        {
            yield return StartCoroutine(LoadImageAsync(imageName));
        }
        
        // Wait until actually loaded
        while (!loadedSprites.ContainsKey(imageName))
        {
            yield return null;
        }
    }
    
    private Vector3 GetRandomOffCanvasPosition()
    {
        // Get canvas bounds in world space
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        Camera cam = targetCanvas.worldCamera ?? Camera.main;
        
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        
        // Calculate canvas bounds
        float canvasLeft = canvasCorners[0].x;
        float canvasRight = canvasCorners[2].x;
        float canvasBottom = canvasCorners[0].y;
        float canvasTop = canvasCorners[2].y;
        
        // Expand bounds by start distance
        float expandedLeft = canvasLeft - startDistance;
        float expandedRight = canvasRight + startDistance;
        float expandedBottom = canvasBottom - startDistance;
        float expandedTop = canvasTop + startDistance;
        
        // Choose random side (0=top, 1=right, 2=bottom, 3=left)
        int side = Random.Range(0, 4);
        Vector3 startPos = Vector3.zero;
        
        switch (side)
        {
            case 0: // Top
                startPos = new Vector3(
                    Random.Range(expandedLeft, expandedRight), 
                    expandedTop, 
                    0
                );
                break;
            case 1: // Right
                startPos = new Vector3(
                    expandedRight, 
                    Random.Range(expandedBottom, expandedTop), 
                    0
                );
                break;
            case 2: // Bottom
                startPos = new Vector3(
                    Random.Range(expandedLeft, expandedRight), 
                    expandedBottom, 
                    0
                );
                break;
            case 3: // Left
                startPos = new Vector3(
                    expandedLeft, 
                    Random.Range(expandedBottom, expandedTop), 
                    0
                );
                break;
        }
        
        // Convert world position to local canvas position
        Vector3 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            cam.WorldToScreenPoint(startPos), 
            cam, 
            out Vector2 localPoint
        );
        localPos = localPoint;
        
        return localPos;
    }
    
    private IEnumerator AnimateImageFlyIn(GameObject imageObject, Vector3 startPos, Vector3 targetPos, float delay)
    {
        // Wait for random delay
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }
        
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        if (rectTransform == null) yield break;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < flyInDuration)
        {
            float t = elapsedTime / flyInDuration;
            float curveValue = flyInCurve.Evaluate(t);
            
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, curveValue);
            rectTransform.localPosition = currentPos;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        rectTransform.localPosition = targetPos;
    }
    
    private GameObject CreateImageAtPosition(Vector3 position, float size, int index)
    {
        // Get next sprite from queue
        Sprite sprite = GetNextSprite();
        if (sprite == null)
        {
            Debug.LogWarning($"No sprite available for position {index}");
            return null;
        }
        
        GameObject imageObject;
        Image imageComponent = null; // Declare once here
        
        // Create image object
        if (imagePrefab != null)
        {
            imageObject = Instantiate(imagePrefab, targetCanvas.transform);
            imageComponent = imageObject.GetComponent<Image>(); // Reuse the variable
        }
        else
        {
            // Create default UI image object
            imageObject = new GameObject($"MosaicImage_{index}");
            imageObject.transform.SetParent(targetCanvas.transform, false);
            
            // Add RectTransform and Image components
            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            imageComponent = imageObject.AddComponent<Image>(); // Reuse the variable
            
            // Set up RectTransform
            rectTransform.sizeDelta = new Vector2(size, size);
        }
        
        // Set position
        RectTransform rt = imageObject.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = position;
            rt.sizeDelta = new Vector2(size, size);
        }
        
        imageObject.name = $"MosaicImage_{index}_{sprite.name}";
        
        // Set sprite
        if (imageComponent != null)
        {
            imageComponent.sprite = sprite;
            imageComponent.preserveAspect = false; // Allow stretching to fit square
        }
        else
        {
            Debug.LogError($"Image object at position {index} doesn't have an Image component!");
            return null;
        }
        
        return imageObject;
    }
    
    private Sprite GetNextSprite()
    {
        // This method is now deprecated in favor of GetNextImageName()
        string imageName = GetNextImageName();
        if (imageName != null && loadedSprites.ContainsKey(imageName))
        {
            return loadedSprites[imageName];
        }
        return null;
    }
    
    [Button("Start Async Mosaic Animation", ButtonSizes.Large)]
    [GUIColor(1f, 0.6f, 0.4f)]
    public void LoadAndPlaceImages()
    {
        PlaceImagesAtGridPositions();
    }
    
    [Button("Discover Available Images", ButtonSizes.Medium)]
    [GUIColor(0.8f, 0.8f, 1f)]
    public void RediscoverImages()
    {
        DiscoverAvailableImages();
    }
    
    [Button("Clear Placed Images", ButtonSizes.Medium)]
    [GUIColor(1f, 0.4f, 0.4f)]
    public void ClearPlacedImages()
    {
        foreach (GameObject imageObj in placedImages)
        {
            if (imageObj != null)
            {
                DestroyImmediate(imageObj);
            }
        }
        placedImages.Clear();
        placedImageCount = 0;
        Debug.Log("Cleared all placed images!");
    }
    
    [Button("Refresh Image Queue", ButtonSizes.Small)]
    public void RefreshImageQueue()
    {
        if (imagesLoaded)
        {
            CreateImageNameQueue();
            Debug.Log("Image queue refreshed!");
        }
        else
        {
            Debug.LogWarning("No images loaded to refresh queue!");
        }
    }
    
    // Utility method to get image info
    [Button("Show Image Info", ButtonSizes.Small)]
    public void ShowImageInfo()
    {
        if (imagesLoaded)
        {
            Debug.Log($"=== IMAGE INFO ===");
            Debug.Log($"Loaded Images: {loadedImageCount}");
            Debug.Log($"Placed Images: {placedImageCount}");
            Debug.Log($"Queue Remaining: {imageNameQueue?.Count ?? 0}");
            Debug.Log($"Grid Positions Available: {mosaicGenerator?.GetValidGridPositions().Count ?? 0}");
        }
        else
        {
            Debug.Log("No images loaded yet!");
        }
    }
    
    private GameObject CreateImageAtPositionWithSprite(Vector3 position, float size, int index, Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"No sprite available for position {index}");
            return null;
        }
        
        GameObject imageObject;
        Image imageComponent = null; // Declare once here
        
        // Create image object
        if (imagePrefab != null)
        {
            imageObject = Instantiate(imagePrefab, targetCanvas.transform);
            imageComponent = imageObject.GetComponent<Image>(); // Reuse the variable
        }
        else
        {
            // Create default UI image object
            imageObject = new GameObject($"MosaicImage_{index}");
            imageObject.transform.SetParent(targetCanvas.transform, false);
            
            // Add RectTransform and Image components
            RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
            imageComponent = imageObject.AddComponent<Image>(); // Reuse the variable
            
            // Set up RectTransform
            rectTransform.sizeDelta = new Vector2(size, size);
        }
        
        // Set position
        RectTransform rt = imageObject.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = position;
            rt.sizeDelta = new Vector2(size, size);
        }
        
        imageObject.name = $"MosaicImage_{index}_{sprite.name}";
        
        // Set sprite
        if (imageComponent != null)
        {
            imageComponent.sprite = sprite;
            imageComponent.preserveAspect = false; // Allow stretching to fit square
        }
        else
        {
            Debug.LogError($"Image object at position {index} doesn't have an Image component!");
            return null;
        }
        
        return imageObject;
    }
    
    private void StoreOriginalPositions()
    {
        originalPositions.Clear();
        foreach (GameObject image in placedImages)
        {
            if (image != null)
            {
                originalPositions.Add(image.transform.localPosition);
            }
        }
        Debug.Log($"Stored {originalPositions.Count} original positions for effects");
    }
    
    private void StartPostCompletionEffects()
    {
        if (effectsActive)
        {
            StopAllEffects();
        }
        
        effectsActive = true;
       // UpdateStatus("Activating dynamic effects...");
        Debug.Log("Starting post-completion effects...");
        
        if (enable3DFloating)
        {
            StartScatterAndFloat();
        }
        
        if (enableFadeCycles)
        {
            StartFadeCycles();
        }
    }
    
    private void StartScatterAndFloat()
    {
        if (isFloating) return;
        
        isFloating = true;
        UpdateStatus("Activating 3D scatter effect...\nPhotos will float in space!");
        Debug.Log("Starting 3D scatter and float effect...");
        
        // Generate scattered positions and floating directions
        GenerateScatteredPositions();
        
        // Start the scatter and float coroutine (different behavior for sequencer vs manual)
        if (enableSequencer)
        {
            floatingCoroutine = StartCoroutine(ScatterAndFloatEffectSequencer());
        }
        else
        {
            floatingCoroutine = StartCoroutine(ScatterAndFloatEffect());
        }
        
        // Start photo frame effect if enabled (only in manual mode)
        if (enablePhotoFrame && !enableSequencer)
        {
            UpdateStatus("Photo frame display activated!");
            StartPhotoFrameEffect();
        }
    }
    
    private void GenerateScatteredPositions()
    {
        scatteredPositions.Clear();
        floatingDirections.Clear();
        
        for (int i = 0; i < placedImages.Count; i++)
        {
            // Generate random 3D scattered position within bounds
            Vector3 scatteredPos = new Vector3(
                Random.Range(-floatingBounds.x, floatingBounds.x),
                Random.Range(-floatingBounds.y, floatingBounds.y),
                Random.Range(-floatingBounds.z, floatingBounds.z)
            );
            
            scatteredPositions.Add(scatteredPos);
            
            // Generate random floating direction
            Vector3 floatDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ).normalized;
            
            floatingDirections.Add(floatDirection);
        }
        
        Debug.Log($"Generated {scatteredPositions.Count} scattered positions in 3D space");
    }
    
    private IEnumerator ScatterAndFloatEffect()
    {
        // Phase 1: Transition to scattered positions
        UpdateStatus("🌪️ Phase 1: Scattering photos...\nMoving to 3D positions");
        Debug.Log("Phase 1: Scattering images to 3D positions...");
        isInFormation = false;
        yield return StartCoroutine(TransitionToScatteredPositions());
        
        // Phase 2: Continuous floating motion with optional reformation
        UpdateStatus("🎈 Phase 2: Floating mode active\nEmployee photos gently drifting");
        Debug.Log("Phase 2: Starting continuous floating motion...");
        stateTimer = 0f;
        
        while (isFloating && effectsActive)
        {
            UpdateFloatingPositions();
            
            // Check for auto-reformation
            if (enableAutoReformation && !isInFormation && !isReforming)
            {
                stateTimer += Time.deltaTime;
                
                if (stateTimer >= timeInScatteredState)
                {
                    yield return StartCoroutine(ReturnToFormation());
                    
                    if (cycleBetweenStates && effectsActive)
                    {
                        // Wait in formation, then scatter again
                        stateTimer = 0f;
                        while (stateTimer < timeInFormedState && effectsActive && isInFormation)
                        {
                            stateTimer += Time.deltaTime;
                            yield return null;
                        }
                        
                        if (effectsActive)
                        {
                            // Scatter again
                            UpdateStatus("🔄 Cycling: Scattering again...\nNew 3D positions generated");
                            Debug.Log("Cycling: Scattering again...");
                            GenerateScatteredPositions(); // Generate new random positions
                            isInFormation = false;
                            yield return StartCoroutine(TransitionToScatteredPositions());
                            stateTimer = 0f;
                        }
                    }
                }
            }
            
            yield return null;
        }
    }
    
    private IEnumerator ScatterAndFloatEffectSequencer()
    {
        // Phase 1: Transition to scattered positions
        UpdateStatus("Phase 1: Scattering photos...\nMoving to 3D positions");
        Debug.Log("Phase 1: Scattering images to 3D positions...");
        isInFormation = false;
        yield return StartCoroutine(TransitionToScatteredPositions());
        
        // Phase 2: Limited floating motion (no auto-reformation in sequencer mode)
        UpdateStatus($"Phase 2: 3D floating active\nDuration: {sequencer3DEffectDuration:F0} seconds");
        Debug.Log($"Phase 2: Starting floating motion for {sequencer3DEffectDuration} seconds...");
        
        float sequencerTimer = 0f;
        while (isFloating && effectsActive && sequencerTimer < sequencer3DEffectDuration)
        {
            UpdateFloatingPositions();
            sequencerTimer += Time.deltaTime;
            
            // Update progress
            float remainingTime = sequencer3DEffectDuration - sequencerTimer;
            if (remainingTime > 0)
            {
                UpdateStatus($"3D floating in progress...\nTime remaining: {remainingTime:F0}s");
            }
            
            yield return null;
        }
        
        // Phase 3: Complete the 3D effect and advance sequencer
        UpdateStatus("3D scatter effect complete!\nReady for next step");
        Debug.Log("3D scatter effect completed for sequencer!");
        isFloating = false;
        
        // Advance sequencer
        if (enableSequencer)
        {
            AdvanceSequence();
        }
    }
    
    private IEnumerator TransitionToScatteredPositions()
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < scatterTransitionTime)
        {
            float t = elapsedTime / scatterTransitionTime;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            for (int i = 0; i < placedImages.Count && i < originalPositions.Count && i < scatteredPositions.Count; i++)
            {
                if (placedImages[i] != null)
                {
                    Vector3 currentPos = Vector3.Lerp(originalPositions[i], scatteredPositions[i], smoothT);
                    placedImages[i].transform.localPosition = currentPos;
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final positions are set
        for (int i = 0; i < placedImages.Count && i < scatteredPositions.Count; i++)
        {
            if (placedImages[i] != null)
            {
                placedImages[i].transform.localPosition = scatteredPositions[i];
            }
        }
    }
    
    private void UpdateFloatingPositions()
    {
        float time = Time.time * floatingSpeed;
        
        for (int i = 0; i < placedImages.Count && i < scatteredPositions.Count && i < floatingDirections.Count; i++)
        {
            if (placedImages[i] != null)
            {
                // Calculate floating offset
                Vector3 floatOffset = floatingDirections[i] * Mathf.Sin(time + i) * floatingRange;
                
                // Apply floating to scattered position
                Vector3 targetPosition = scatteredPositions[i] + floatOffset;
                
                // Smooth transition to floating position
                Vector3 currentPos = placedImages[i].transform.localPosition;
                Vector3 newPos = Vector3.Lerp(currentPos, targetPosition, Time.deltaTime * 2f);
                
                placedImages[i].transform.localPosition = newPos;
            }
        }
    }
    
    private void StartFadeCycles()
    {
        if (isFading) return;
        
        isFading = true;
        currentFadeCycle = 0;
        UpdateStatus("✨ Starting fade animation cycles...\nPhotos will appear and disappear!");
        Debug.Log("Starting fade out/in cycles...");
        
        fadeCycleCoroutine = StartCoroutine(FadeCycleEffect());
    }
    
    private IEnumerator FadeCycleEffect()
    {
        while (isFading && effectsActive && (maxFadeCycles == 0 || currentFadeCycle < maxFadeCycles))
        {
            currentFadeCycle++;
            string cycleInfo = maxFadeCycles > 0 ? $"{currentFadeCycle}/{maxFadeCycles}" : $"{currentFadeCycle}";
            Debug.Log($"Starting fade cycle {currentFadeCycle}" + (maxFadeCycles > 0 ? $"/{maxFadeCycles}" : ""));
            
            // Phase 1: Fade out
            UpdateStatus($"🌙 Fade cycle {cycleInfo}\nFading out employee photos...");
            Debug.Log("Fading out all images...");
            yield return StartCoroutine(FadeImages(1f, 0f, fadeOutDuration));
            
            // Phase 2: Wait while invisible
            UpdateStatus($"⏳ Cycle {cycleInfo}\nPhotos hidden - waiting {fadeWaitTime:F0}s...");
            Debug.Log($"Waiting {fadeWaitTime} seconds while images are invisible...");
            yield return new WaitForSeconds(fadeWaitTime);
            
            // Phase 3: Fade in
            UpdateStatus($"☀️ Cycle {cycleInfo}\nFading in employee photos...");
            Debug.Log("Fading in all images...");
            yield return StartCoroutine(FadeImages(0f, 1f, fadeInDuration));
            
            // Brief pause between cycles
            if (maxFadeCycles == 0 || currentFadeCycle < maxFadeCycles)
            {
                yield return new WaitForSeconds(2f);
            }
        }
        
        UpdateStatus("Fade cycles complete!\nAll employee photos visible");
        Debug.Log("Fade cycle effect completed!");
        isFading = false;
        
        // Advance sequencer for fade completion
        if (enableSequencer)
        {
            AdvanceSequence();
        }
    }
    
    private IEnumerator FadeImages(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        // Get all Image components
        List<Image> imageComponents = new List<Image>();
        foreach (GameObject imageObj in placedImages)
        {
            if (imageObj != null)
            {
                Image img = imageObj.GetComponent<Image>();
                if (img != null)
                {
                    imageComponents.Add(img);
                }
            }
        }
        
        // Determine logo behavior based on fade direction
        bool showingLogo = enableLogoFadeTransition && (endAlpha < startAlpha && logoVisibleWhenImagesHidden);
        bool hidingLogo = enableLogoFadeTransition && (endAlpha > startAlpha && logoVisibleWhenImagesHidden);
        
        // Start logo fade transition if needed
        if (showingLogo)
        {
            StartCoroutine(FadeLogo(0f, 1f, logoFadeDuration * 0.7f)); // Start logo fade slightly before images finish fading
        }
        else if (hidingLogo)
        {
            StartCoroutine(FadeLogo(1f, 0f, logoFadeDuration * 0.7f)); // Start logo fade as images begin to appear
        }
        
        // Animate alpha over time
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            // Apply alpha to all images
            foreach (Image img in imageComponents)
            {
                if (img != null)
                {
                    Color color = img.color;
                    color.a = currentAlpha;
                    img.color = color;
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final alpha is set
        foreach (Image img in imageComponents)
        {
            if (img != null)
            {
                Color color = img.color;
                color.a = endAlpha;
                img.color = color;
            }
        }
    }
    
    private IEnumerator FadeLogo(float startAlpha, float endAlpha, float duration)
    {
        if (!enableLogoFadeTransition || logoImage == null) yield break;
        
        float elapsedTime = 0f;
        
        // Make sure logo GameObject is active when we want to show it
        if (endAlpha > 0f && logoGameObject != null)
        {
            logoGameObject.SetActive(true);
        }
        
        // Animate logo alpha over time
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            // Apply alpha to logo
            if (logoImage != null)
            {
                Color logoColor = logoImage.color;
                logoColor.a = currentAlpha;
                logoImage.color = logoColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final alpha is set
        if (logoImage != null)
        {
            Color logoColor = logoImage.color;
            logoColor.a = endAlpha;
            logoImage.color = logoColor;
        }
        
        // Hide logo GameObject if fully transparent
        if (endAlpha <= 0f && logoGameObject != null)
        {
            logoGameObject.SetActive(false);
        }
    }
    
    [Button("Show Logo", ButtonSizes.Small)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void ShowLogo()
    {
        if (logoGameObject != null && logoImage != null)
        {
            logoGameObject.SetActive(true);
            Color logoColor = logoImage.color;
            logoColor.a = 1f;
            logoImage.color = logoColor;
            Debug.Log("Logo shown");
        }
        else
        {
            Debug.LogWarning("Logo GameObject or Image component not assigned!");
        }
    }
    
    [Button("Hide Logo", ButtonSizes.Small)]
    [GUIColor(1f, 0.8f, 0.4f)]
    public void HideLogo()
    {
        if (logoGameObject != null)
        {
            logoGameObject.SetActive(false);
            Debug.Log("Logo hidden");
        }
        else
        {
            Debug.LogWarning("Logo GameObject not assigned!");
        }
    }
    
    [Button("Test Logo Fade", ButtonSizes.Small)]
    [GUIColor(0.8f, 1f, 0.4f)]
    public void TestLogoFade()
    {
        if (logoImage != null)
        {
            StartCoroutine(TestLogoFadeSequence());
        }
        else
        {
            Debug.LogWarning("Logo Image component not assigned!");
        }
    }
    
    private IEnumerator TestLogoFadeSequence()
    {
        Debug.Log("Testing logo fade sequence...");
        
        // Fade out logo
        yield return StartCoroutine(FadeLogo(1f, 0f, logoFadeDuration));
        yield return new WaitForSeconds(1f);
        
        // Fade in logo
        yield return StartCoroutine(FadeLogo(0f, 1f, logoFadeDuration));
        
        Debug.Log("Logo fade test complete");
    }
    
    private void StopAllEffects()
    {
        effectsActive = false;
        
        // Stop floating effect
        if (floatingCoroutine != null)
        {
            StopCoroutine(floatingCoroutine);
            floatingCoroutine = null;
        }
        isFloating = false;
        
        // Stop fade cycle effect
        if (fadeCycleCoroutine != null)
        {
            StopCoroutine(fadeCycleCoroutine);
            fadeCycleCoroutine = null;
        }
        isFading = false;
        
        // Stop reformation effect
        if (reformationCoroutine != null)
        {
            StopCoroutine(reformationCoroutine);
            reformationCoroutine = null;
        }
        isReforming = false;
        
        // Stop cycling effect
        if (cyclingCoroutine != null)
        {
            StopCoroutine(cyclingCoroutine);
            cyclingCoroutine = null;
        }
        
        // Stop photo frame effect
        if (photoFrameActive)
        {
            StopPhotoFrameEffect(true); // Fade out gracefully
        }
        
        Debug.Log("All post-completion effects stopped");
    }
    
    private void ResetToOriginalPositions()
    {
        for (int i = 0; i < placedImages.Count && i < originalPositions.Count; i++)
        {
            if (placedImages[i] != null)
            {
                placedImages[i].transform.localPosition = originalPositions[i];
                
                // Reset alpha to full if fading was active
                Image img = placedImages[i].GetComponent<Image>();
                if (img != null)
                {
                    Color color = img.color;
                    color.a = 1f;
                    img.color = color;
                }
            }
        }
        Debug.Log("Reset all images to original mosaic positions");
    }
    
    [Button("Start 3D Floating Effect", ButtonSizes.Large)]
    [GUIColor(0.4f, 1f, 0.8f)]
    public void StartFloatingEffect()
    {
        if (placedImages.Count == 0)
        {
            Debug.LogWarning("No images placed yet! Place images first.");
            return;
        }
        
        if (originalPositions.Count == 0)
        {
            StoreOriginalPositions();
        }
        
        StopAllEffects();
        enable3DFloating = true;
        enableFadeCycles = false;
        StartPostCompletionEffects();
    }
    
    [Button("Start Fade Cycle Effect", ButtonSizes.Large)]
    [GUIColor(0.8f, 0.4f, 1f)]
    public void StartFadeEffect()
    {
        if (placedImages.Count == 0)
        {
            Debug.LogWarning("No images placed yet! Place images first.");
            return;
        }
        
        if (originalPositions.Count == 0)
        {
            StoreOriginalPositions();
        }
        
        StopAllEffects();
        enable3DFloating = false;
        enableFadeCycles = true;
        StartPostCompletionEffects();
    }
    
    [Button("Start Both Effects", ButtonSizes.Large)]
    [GUIColor(1f, 0.8f, 0.4f)]
    public void StartBothEffects()
    {
        if (placedImages.Count == 0)
        {
            Debug.LogWarning("No images placed yet! Place images first.");
            return;
        }
        
        if (originalPositions.Count == 0)
        {
            StoreOriginalPositions();
        }
        
        StopAllEffects();
        enable3DFloating = true;
        enableFadeCycles = true;
        StartPostCompletionEffects();
    }
    
    [Button("Stop All Effects", ButtonSizes.Medium)]
    [GUIColor(1f, 0.6f, 0.6f)]
    public void StopEffects()
    {
        StopAllEffects();
    }
    
    [Button("Reset to Original Positions", ButtonSizes.Medium)]
    [GUIColor(0.7f, 0.7f, 1f)]
    public void ResetPositions()
    {
        StopAllEffects();
        ResetToOriginalPositions();
    }
    
    [Button("Return to Formation", ButtonSizes.Large)]
    [GUIColor(0.2f, 0.8f, 1f)]
    public void ManualReturnToFormation()
    {
        if (placedImages.Count == 0)
        {
            Debug.LogWarning("No images placed yet! Place images first.");
            return;
        }
        
        if (isInFormation)
        {
            Debug.Log("Images are already in formation!");
            return;
        }
        
        // Stop all effects to prevent cycling behavior from interfering
        Debug.Log("Manual return to formation: Stopping all background effects...");
        StopAllEffects();
        
        // Start manual reformation
        reformationCoroutine = StartCoroutine(ReturnToFormation());
    }
    
    [Button("Start Cycling Mode", ButtonSizes.Large)]
    [GUIColor(1f, 0.4f, 0.8f)]
    public void StartCyclingMode()
    {
        if (placedImages.Count == 0)
        {
            Debug.LogWarning("No images placed yet! Place images first.");
            return;
        }
        
        if (originalPositions.Count == 0)
        {
            StoreOriginalPositions();
        }
        
        StopAllEffects();
        enable3DFloating = true;
        enableFadeCycles = false;
        enableAutoReformation = true;
        cycleBetweenStates = true;
        StartPostCompletionEffects();
    }
    
    private IEnumerator ReturnToFormation()
    {
        if (isReforming || isInFormation) yield break;
        
        isReforming = true;
        UpdateStatus("🏠 Returning to formation...\nReassembling logo pattern");
        Debug.Log("Returning to logo formation...");
        
        // Stop photo frame effect when returning to formation
        if (photoFrameActive)
        {
            StopPhotoFrameEffect(true); // Fade out gracefully
        }
        
        float elapsedTime = 0f;
        List<Vector3> startPositions = new List<Vector3>();
        
        // Capture current positions as starting points
        foreach (GameObject image in placedImages)
        {
            if (image != null)
            {
                startPositions.Add(image.transform.localPosition);
            }
        }
        
        // Animate back to original positions
        while (elapsedTime < reformationDuration)
        {
            float t = elapsedTime / reformationDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            for (int i = 0; i < placedImages.Count && i < originalPositions.Count && i < startPositions.Count; i++)
            {
                if (placedImages[i] != null)
                {
                    Vector3 currentPos = Vector3.Lerp(startPositions[i], originalPositions[i], smoothT);
                    placedImages[i].transform.localPosition = currentPos;
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final positions are exactly the original positions
        for (int i = 0; i < placedImages.Count && i < originalPositions.Count; i++)
        {
            if (placedImages[i] != null)
            {
                placedImages[i].transform.localPosition = originalPositions[i];
            }
        }
        
        isReforming = false;
        isInFormation = true;
        UpdateStatus("Logo formation complete!\nEmployee photos back in position");
        Debug.Log("Logo formation complete!");
        isPlaced = true;
        // Advance sequencer for formation completion
        if (enableSequencer)
        {
            AdvanceSequence();
        }
    }
    
    // ===== PHOTO FRAME METHODS =====
    
    private void StartPhotoFrameEffect()
    {
        // Check if we have the new multi-frame setup
        if (photoFrameContainer != null && photoFramePrefab != null)
        {
            StartMultiPhotoFrameEffect();
            return;
        }
        
        // Fallback to legacy single frame setup
        if (!enablePhotoFrame || photoFrameObject == null || photoFrameCanvasGroup == null || photoFrameImage == null || photoFrameRect == null)
        {
            Debug.LogWarning("Photo frame not properly configured - missing references!");
            return;
        }
        
        if (photoFrameActive) return; // Already running
        
        photoFrameActive = true;
        Debug.Log("Starting photo frame effect during scattered state...");
        
        // Initialize frame state
        photoFrameObject.SetActive(true);
        photoFrameCanvasGroup.alpha = 1f;
        photoFrameCanvasGroup.interactable = false;
        photoFrameCanvasGroup.blocksRaycasts = false;
        
        // Start the photo frame cycle
        photoFrameCoroutine = StartCoroutine(PhotoFrameCycleEffect());
    }

    private void StartMultiPhotoFrameEffect()
    {
        if (!enablePhotoFrame || photoFrameContainer == null || photoFramePrefab == null)
        {
            Debug.LogWarning("Multi-photo frame not properly configured - missing references!");
            return;
        }
        
        if (photoFrameActive) return; // Already running
        
        photoFrameActive = true;
        Debug.Log($"Starting multi-photo frame effect with {photosPerRow} photos per row...");
        
        // Initialize container
        photoFrameContainer.SetActive(true);
        
        // Clear any existing frames
        ClearActivePhotoFrames();
        
        // Start the multi-photo frame cycle
        photoFrameCoroutine = StartCoroutine(MultiPhotoFrameCycleEffect());
    }
    
    private void StopPhotoFrameEffect(bool fadeOut = true)
    {
        if (!photoFrameActive) return;
        
        photoFrameActive = false;
        Debug.Log("Stopping photo frame effect...");
        
        if (photoFrameCoroutine != null)
        {
            StopCoroutine(photoFrameCoroutine);
            photoFrameCoroutine = null;
        }
        
        // Handle multi-photo frame cleanup
        if (photoFrameContainer != null && activePhotoFrames.Count > 0)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutMultiPhotoFrames());
            }
            else
            {
                ClearActivePhotoFrames();
                photoFrameContainer.SetActive(false);
            }
        }
        // Handle legacy single frame cleanup
        else if (fadeOut && photoFrameCanvasGroup != null)
        {
            StartCoroutine(FadeOutPhotoFrame());
        }
        else if (photoFrameObject != null)
        {
            photoFrameObject.SetActive(false);
        }
    }

    private void ClearActivePhotoFrames()
    {
        foreach (GameObject frame in activePhotoFrames)
        {
            if (frame != null)
            {
                DestroyImmediate(frame);
            }
        }
        activePhotoFrames.Clear();
        activeFrameRects.Clear();
        currentFramePhotos.Clear();
    }
    
    private IEnumerator PhotoFrameCycleEffect()
    {
        while (photoFrameActive && effectsActive && !isInFormation)
        {
            // Get random photo
            string randomPhotoName = GetRandomPhotoName();
            if (randomPhotoName == null)
            {
                Debug.LogWarning("No photos available for photo frame!");
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            // Ensure photo is loaded
            yield return StartCoroutine(EnsureImageLoaded(randomPhotoName));
            
            if (!loadedSprites.ContainsKey(randomPhotoName))
            {
                Debug.LogWarning($"Failed to load photo: {randomPhotoName}");
                yield return new WaitForSeconds(1f);
                continue;
            }
            
            currentFramePhoto = randomPhotoName;
            
            // Set the photo in the frame
            photoFrameImage.sprite = loadedSprites[randomPhotoName];
            
            // Slide in from bottom
            yield return StartCoroutine(SlidePhotoFrameIn());
            
            // Display for specified duration
            float displayTimer = 0f;
            while (displayTimer < photoDisplayDuration && photoFrameActive && !isInFormation)
            {
                displayTimer += Time.deltaTime;
                yield return null;
            }
            
            // Check if we should continue (state might have changed)
            if (!photoFrameActive || isInFormation) break;
            
            // Slide out to top
            yield return StartCoroutine(SlidePhotoFrameOut());
            
            // Wait between photos
            float waitTimer = 0f;
            while (waitTimer < photoWaitDuration && photoFrameActive && !isInFormation)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
        
        // Clean up when cycle ends
        if (photoFrameObject != null && photoFrameObject.activeSelf)
        {
            photoFrameObject.SetActive(false);
        }
        
        Debug.Log("Photo frame cycle ended");
    }

    private IEnumerator MultiPhotoFrameCycleEffect()
    {
        while (photoFrameActive && effectsActive && !isInFormation)
        {
            // Create 5 photo frames for this cycle
            yield return StartCoroutine(CreateMultiPhotoFrames());
            
            // Slide all frames in simultaneously
            yield return StartCoroutine(SlideMultiPhotoFramesIn());
            
            // Display for specified duration
            float displayTimer = 0f;
            while (displayTimer < photoDisplayDuration && photoFrameActive && !isInFormation)
            {
                displayTimer += Time.deltaTime;
                yield return null;
            }
            
            // Check if we should continue (state might have changed)
            if (!photoFrameActive || isInFormation) break;
            
            // Slide all frames out simultaneously
            yield return StartCoroutine(SlideMultiPhotoFramesOut());
            
            // Clean up current frames
            ClearActivePhotoFrames();
            
            // Wait between photo sets
            float waitTimer = 0f;
            while (waitTimer < photoWaitDuration && photoFrameActive && !isInFormation)
            {
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }
        
        // Clean up when cycle ends
        ClearActivePhotoFrames();
        if (photoFrameContainer != null)
        {
            photoFrameContainer.SetActive(false);
        }
        
        Debug.Log("Multi-photo frame cycle ended");
    }
    
    private IEnumerator CreateMultiPhotoFrames()
    {
        // Clear any existing frames first
        ClearActivePhotoFrames();
        
        // Get random photo names
        List<string> photoNames = new List<string>();
        for (int i = 0; i < photosPerRow; i++)
        {
            string randomPhotoName = GetRandomPhotoName();
            if (randomPhotoName == null)
            {
                Debug.LogWarning($"No photos available for frame {i}!");
                continue;
            }
            
            // Ensure photo is loaded
            yield return StartCoroutine(EnsureImageLoaded(randomPhotoName));
            
            if (loadedSprites.ContainsKey(randomPhotoName))
            {
                photoNames.Add(randomPhotoName);
            }
        }
        
        // Create frames for each photo
        float totalWidth = (photosPerRow - 1) * photoSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < photoNames.Count; i++)
        {
            // Instantiate frame
            GameObject frame = Instantiate(photoFramePrefab, photoFrameContainer.transform);
            
            // Get components
            MPImage frameImage = frame.GetComponent<MPImage>();
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            
            if (frameImage == null || frameRect == null)
            {
                Debug.LogWarning($"Photo frame prefab missing required components! Need MPImage and RectTransform.");
                Destroy(frame);
                continue;
            }
            
            // Set photo
            frameImage.sprite = loadedSprites[photoNames[i]];
            
            // Position frame
            Vector3 targetPosition = new Vector3(startX + (i * photoSpacing), 0, 0);
            frameRect.localPosition = targetPosition;
            
            // Store references
            activePhotoFrames.Add(frame);
            activeFrameRects.Add(frameRect);
            currentFramePhotos.Add(photoNames[i]);
        }
        
        Debug.Log($"Created {activePhotoFrames.Count} photo frames for multi-photo display");
    }

    private IEnumerator SlideMultiPhotoFramesIn()
    {
        if (activeFrameRects.Count == 0) yield break;
        
        photoFrameAnimating = true;
        
        // Get canvas bounds for positioning
        Canvas canvas = photoFrameContainer.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasHeight = canvasRect.rect.height;
        
        // Store target positions and set start positions
        Vector3[] targetPositions = new Vector3[activeFrameRects.Count];
        for (int i = 0; i < activeFrameRects.Count; i++)
        {
            targetPositions[i] = activeFrameRects[i].localPosition;
            Vector3 bottomPosition = new Vector3(targetPositions[i].x, -canvasHeight, targetPositions[i].z);
            activeFrameRects[i].localPosition = bottomPosition;
        }
        
        // Animate all frames sliding in simultaneously
        float elapsedTime = 0f;
        while (elapsedTime < slideInDuration)
        {
            float t = elapsedTime / slideInDuration;
            float curveValue = slideInCurve.Evaluate(t);
            
            for (int i = 0; i < activeFrameRects.Count; i++)
            {
                Vector3 bottomPosition = new Vector3(targetPositions[i].x, -canvasHeight, targetPositions[i].z);
                Vector3 currentPos = Vector3.Lerp(bottomPosition, targetPositions[i], curveValue);
                activeFrameRects[i].localPosition = currentPos;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final positions
        for (int i = 0; i < activeFrameRects.Count; i++)
        {
            activeFrameRects[i].localPosition = targetPositions[i];
        }
        
        photoFrameAnimating = false;
        Debug.Log($"All {activeFrameRects.Count} photo frames slid in simultaneously");
    }

    private IEnumerator SlideMultiPhotoFramesOut()
    {
        if (activeFrameRects.Count == 0) yield break;
        
        photoFrameAnimating = true;
        
        // Get canvas bounds for positioning
        Canvas canvas = photoFrameContainer.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        float canvasHeight = canvasRect.rect.height;
        
        // Store start positions
        Vector3[] startPositions = new Vector3[activeFrameRects.Count];
        for (int i = 0; i < activeFrameRects.Count; i++)
        {
            startPositions[i] = activeFrameRects[i].localPosition;
        }
        
        // Animate all frames sliding out simultaneously
        float elapsedTime = 0f;
        while (elapsedTime < slideOutDuration)
        {
            float t = elapsedTime / slideOutDuration;
            float curveValue = slideOutCurve.Evaluate(t);
            
            for (int i = 0; i < activeFrameRects.Count; i++)
            {
                Vector3 topPosition = new Vector3(startPositions[i].x, canvasHeight, startPositions[i].z);
                Vector3 currentPos = Vector3.Lerp(startPositions[i], topPosition, curveValue);
                activeFrameRects[i].localPosition = currentPos;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        photoFrameAnimating = false;
        Debug.Log($"All {activeFrameRects.Count} photo frames slid out simultaneously");
    }

    private IEnumerator FadeOutMultiPhotoFrames()
    {
        if (activePhotoFrames.Count == 0) yield break;
        
        // Get all CanvasGroup components
        List<CanvasGroup> canvasGroups = new List<CanvasGroup>();
        foreach (GameObject frame in activePhotoFrames)
        {
            CanvasGroup canvasGroup = frame.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroups.Add(canvasGroup);
            }
        }
        
        if (canvasGroups.Count == 0)
        {
            // No canvas groups, just deactivate
            ClearActivePhotoFrames();
            photoFrameContainer.SetActive(false);
            yield break;
        }
        
        float elapsedTime = 0f;
        while (elapsedTime < frameFadeDuration)
        {
            float t = elapsedTime / frameFadeDuration;
            float alpha = Mathf.Lerp(1f, 0f, t);
            
            foreach (CanvasGroup canvasGroup in canvasGroups)
            {
                canvasGroup.alpha = alpha;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final alpha
        foreach (CanvasGroup canvasGroup in canvasGroups)
        {
            canvasGroup.alpha = 0f;
        }
        
        ClearActivePhotoFrames();
        photoFrameContainer.SetActive(false);
    }

    private IEnumerator SlidePhotoFrameIn()
    {
        if (photoFrameRect == null) yield break;
        
        photoFrameAnimating = true;
        
        // Get canvas bounds for positioning
        Canvas canvas = photoFrameObject.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        float canvasHeight = canvasRect.rect.height;
        Vector3 centerPosition = Vector3.zero; // Center of screen
        Vector3 bottomPosition = new Vector3(0, -canvasHeight, 0); // Bottom off-screen
        
        // Start from bottom
        photoFrameRect.localPosition = bottomPosition;
        
        float elapsedTime = 0f;
        while (elapsedTime < slideInDuration)
        {
            float t = elapsedTime / slideInDuration;
            float curveValue = slideInCurve.Evaluate(t);
            
            Vector3 currentPos = Vector3.Lerp(bottomPosition, centerPosition, curveValue);
            photoFrameRect.localPosition = currentPos;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        photoFrameRect.localPosition = centerPosition;
        photoFrameAnimating = false;
        
        Debug.Log($"Photo frame slid in with photo: {currentFramePhoto}");
    }
    
    private IEnumerator SlidePhotoFrameOut()
    {
        if (photoFrameRect == null) yield break;
        
        photoFrameAnimating = true;
        
        // Get canvas bounds for positioning
        Canvas canvas = photoFrameObject.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        float canvasHeight = canvasRect.rect.height;
        Vector3 centerPosition = photoFrameRect.localPosition; // Current position
        Vector3 topPosition = new Vector3(0, canvasHeight, 0); // Top off-screen
        
        float elapsedTime = 0f;
        while (elapsedTime < slideOutDuration)
        {
            float t = elapsedTime / slideOutDuration;
            float curveValue = slideOutCurve.Evaluate(t);
            
            Vector3 currentPos = Vector3.Lerp(centerPosition, topPosition, curveValue);
            photoFrameRect.localPosition = currentPos;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        photoFrameRect.localPosition = topPosition;
        photoFrameAnimating = false;
        
        Debug.Log($"Photo frame slid out with photo: {currentFramePhoto}");
    }
    
    private IEnumerator FadeOutPhotoFrame()
    {
        if (photoFrameCanvasGroup == null) yield break;
        
        float elapsedTime = 0f;
        float startAlpha = photoFrameCanvasGroup.alpha;
        
        while (elapsedTime < frameFadeDuration)
        {
            float t = elapsedTime / frameFadeDuration;
            photoFrameCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        photoFrameCanvasGroup.alpha = 0f;
        
        if (photoFrameObject != null)
        {
            photoFrameObject.SetActive(false);
        }
        
        Debug.Log("Photo frame faded out");
    }
    
    private string GetRandomPhotoName()
    {
        if (useSystemFolder)
        {
            if (availableImagePaths == null || availableImagePaths.Count == 0) return null;
            
            // Get random image file path
            int randomIndex = Random.Range(0, availableImagePaths.Count);
            return availableImagePaths[randomIndex];
        }
        else
        {
            if (availableTextures == null || availableTextures.Length == 0) return null;
            
            // Get random texture name
            int randomIndex = Random.Range(0, availableTextures.Length);
            return availableTextures[randomIndex].name;
        }
    }
    
    [Button("Test Photo Frame", ButtonSizes.Small)]
    [GUIColor(1f, 0.4f, 0.8f)]
    public void TestPhotoFrame()
    {
        if (photoFrameObject == null || photoFrameCanvasGroup == null || photoFrameImage == null || photoFrameRect == null)
        {
            Debug.LogWarning("Photo frame not properly configured - assign all references first!");
            return;
        }
        
        if (photoFrameActive)
        {
            StopPhotoFrameEffect();
        }
        else
        {
            StartPhotoFrameEffect();
        }
    }
    
    // ===== PUBLIC METHODS FOR EXTERNAL CONTROL =====
    
    /// <summary>
    /// Public method to simulate Key 1 press - Start Mosaic Formation
    /// </summary>
    public void SimulateKey1Press()
    {
        if (!enableKeyboardControls) return;
        
        if (enableSequencer)
        {
            if (IsValidKeyForSequence(KeyCode.Alpha1))
            {
                Debug.Log("[EXTERNAL] Key 1 simulated - Starting Mosaic Formation");
                LoadAndPlaceImages();
            }
            else
            {
                UpdateStatus($"Wrong key! Expected: '{nextExpectedKey}'\n{sequenceStatus}", true);
                Debug.LogWarning($"[EXTERNAL] Wrong key simulated! Expected: {nextExpectedKey}");
            }
        }
        else
        {
            Debug.Log("[EXTERNAL] Key 1 simulated - Starting Mosaic Formation");
            LoadAndPlaceImages();
        }
    }
    
    /// <summary>
    /// Public method to simulate Key 2 press - Start Fade Cycles
    /// </summary>
    public void SimulateKey2Press()
    {
        if (!enableKeyboardControls) return;
        
        if (enableSequencer)
        {
            if (IsValidKeyForSequence(KeyCode.Alpha2))
            {
                Debug.Log("[EXTERNAL] Key 2 simulated - Starting Fade Cycles");
                StartFadeEffect();
            }
            else
            {
                UpdateStatus($"Wrong key! Expected: '{nextExpectedKey}'\n{sequenceStatus}", true);
                Debug.LogWarning($"[EXTERNAL] Wrong key simulated! Expected: {nextExpectedKey}");
            }
        }
        else
        {
            Debug.Log("[EXTERNAL] Key 2 simulated - Starting Fade Cycles");
            StartFadeEffect();
        }
    }
    
    /// <summary>
    /// Public method to simulate Key 3 press - Start 3D Scatter Effect
    /// </summary>
    public void SimulateKey3Press()
    {
        if (!enableKeyboardControls) return;
        
        if (enableSequencer)
        {
            if (IsValidKeyForSequence(KeyCode.Alpha3))
            {
                Debug.Log("[EXTERNAL] Key 3 simulated - Starting 3D Scatter Effect");
                StartFloatingEffect();
            }
            else
            {
                UpdateStatus($"Wrong key! Expected: '{nextExpectedKey}'\n{sequenceStatus}", true);
                Debug.LogWarning($"[EXTERNAL] Wrong key simulated! Expected: {nextExpectedKey}");
            }
        }
        else
        {
            Debug.Log("[EXTERNAL] Key 3 simulated - Starting 3D Scatter Effect");
            StartFloatingEffect();
        }
    }
    
    /// <summary>
    /// Public method to simulate Key 4 press - Return to Formation
    /// </summary>
    public void SimulateKey4Press()
    {
        if (!enableKeyboardControls) return;
        
        if (enableSequencer)
        {
            if (IsValidKeyForSequence(KeyCode.Alpha4))
            {
                Debug.Log("[EXTERNAL] Key 4 simulated - Returning to Formation");
                ManualReturnToFormation();
            }
            else
            {
                UpdateStatus($"Wrong key! Expected: '{nextExpectedKey}'\n{sequenceStatus}", true);
                Debug.LogWarning($"[EXTERNAL] Wrong key simulated! Expected: {nextExpectedKey}");
            }
        }
        else
        {
            Debug.Log("[EXTERNAL] Key 4 simulated - Returning to Formation");
            ManualReturnToFormation();
        }
    }
    
    /// <summary>
    /// Public method to simulate Key 5 press - Start Both Effects (Manual mode only)
    /// </summary>
    public void SimulateKey5Press()
    {
        if (!enableKeyboardControls || enableSequencer) return;
        
        Debug.Log("[EXTERNAL] Key 5 simulated - Starting Both Effects");
        StartBothEffects();
    }
    
    /// <summary>
    /// Public method to simulate Key 6 press - Start Cycling Mode (Manual mode only)
    /// </summary>
    public void SimulateKey6Press()
    {
        if (!enableKeyboardControls || enableSequencer) return;
        
        Debug.Log("[EXTERNAL] Key 6 simulated - Starting Cycling Mode");
        StartCyclingMode();
    }
    
    /// <summary>
    /// Public method to simulate Key 0 press - Stop All Effects
    /// </summary>
    public void SimulateKey0Press()
    {
        if (!enableKeyboardControls) return;
        
        Debug.Log("[EXTERNAL] Key 0 simulated - Stopping All Effects");
        StopEffects();
    }
    
    /// <summary>
    /// Public method to simulate R key press - Reset Positions
    /// </summary>
    public void SimulateKeyRPress()
    {
        if (!enableKeyboardControls) return;
        
        Debug.Log("[EXTERNAL] Key R simulated - Resetting to Original Positions");
        ResetPositions();
    }
    
    /// <summary>
    /// Public method to simulate C key press - Clear Images
    /// </summary>
    public void SimulateKeyCPress()
    {
        if (!enableKeyboardControls) return;
        
        Debug.Log("[EXTERNAL] Key C simulated - Clearing All Images");
        ClearPlacedImages();
    }
    
    /// <summary>
    /// Public method to simulate ` key press - Toggle Status Text
    /// </summary>
    public void SimulateBackQuotePress()
    {
        if (!enableKeyboardControls) return;
        
        Debug.Log("[EXTERNAL] Key ` simulated - Toggling Status Text Visibility");
        ToggleStatusTextVisibility();
    }
    
    /// <summary>
    /// Public method to simulate H key press - Toggle Logo Visibility
    /// </summary>
    public void SimulateKeyHPress()
    {
        if (!enableKeyboardControls) return;
        
        Debug.Log("[EXTERNAL] Key H simulated - Toggling Logo Visibility");
        if (logoGameObject != null && logoGameObject.activeSelf)
        {
            HideLogo();
        }
        else
        {
            ShowLogo();
        }
    }
    
    /// <summary>
    /// Get current sequencer information for external display
    /// </summary>
    public string GetSequencerStatus()
    {
        if (!enableSequencer) return "Manual Mode";
        return $"Seq {currentSequence}, Step {currentStep} - Next: '{nextExpectedKey}'";
    }
    
    /// <summary>
    /// Check if a specific key is valid for the current sequence
    /// </summary>
    public bool IsKeyValidForCurrentSequence(int keyNumber)
    {
        if (!enableSequencer) return true;
        return nextExpectedKey == keyNumber.ToString();
    }
} 