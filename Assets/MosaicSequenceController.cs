using System;
using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public class MosaicSequenceController : MonoBehaviour
{
    [Header("Mosaic Reference")]
    [Required("Please assign the MosaicImagePlacer component")]
    public MosaicImagePlacer mosaicImagePlacer;
    
    [Header("Sequence Information")]
    [ReadOnly, ShowInInspector] 
    private string CurrentSequenceStatus => mosaicImagePlacer != null ? mosaicImagePlacer.GetSequencerStatus() : "No Mosaic Assigned";
    
    [ReadOnly, ShowInInspector]
    private string CurrentStatus => mosaicImagePlacer != null ? mosaicImagePlacer.currentStatus : "No Status";
    
    [ReadOnly, ShowInInspector]
    private bool SequencerEnabled => mosaicImagePlacer != null ? mosaicImagePlacer.enableSequencer : false;
    
    [ReadOnly, ShowInInspector]
    private bool KeyboardControlsEnabled => mosaicImagePlacer != null ? mosaicImagePlacer.enableKeyboardControls : false;
    
 
    [InfoBox("SEQUENCER MODE: Follow the sequence order!\nSequence 1: 1 → 2 → 3\nSequence 2: 4 → 2 → 3", InfoMessageType.Info)]
    
    [Button("🚀 Key 1: Start Mosaic Formation", ButtonSizes.Large)]
    [GUIColor("@GetButtonColor(1)")]
    public void PressKey1()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKey1Press();
    }

    public float delay1, delay2;
    IEnumerator RunSequence1()
    {
        if (isAlreadyStarted)
        {
            PressKey4();
        }else
        {
            PressKey1();
        }
        isAlreadyStarted = true;
        while (!mosaicImagePlacer.isPlaced)
        {
            yield return null;
            Debug.Log("Loading");
        }

        mosaicImagePlacer.isPlaced = false;
        yield return new WaitForSeconds(delay1);
        PressKey2();
        
        while (mosaicImagePlacer.isFading)
        {
            yield return null;
            Debug.Log("Fading");
        }
        
        yield return new WaitForSeconds(delay2);
        
        PressKey3();
    }
    
    [Button("✨ Key 2: Start Fade Cycles", ButtonSizes.Large)]
    [GUIColor("@GetButtonColor(2)")]
    public void PressKey2()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKey2Press();
    }
    
    [Button("🌪️ Key 3: Start 3D Scatter Effect", ButtonSizes.Large)]
    [GUIColor("@GetButtonColor(3)")]
    public void PressKey3()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKey3Press();
    }
    
    [Button("🏠 Key 4: Return to Formation", ButtonSizes.Large)]
    [GUIColor("@GetButtonColor(4)")]
    public void PressKey4()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKey4Press();
    }
    

    [InfoBox("These buttons only work when Sequencer is DISABLED", InfoMessageType.Warning)]
    
    [Button("🎭 Key 5: Start Both Effects", ButtonSizes.Medium)]
    [GUIColor(1f, 0.8f, 0.4f)]
    [EnableIf("@!SequencerEnabled")]
    public void PressKey5()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKey5Press();
    }
    
    [Button("🔄 Key 6: Start Cycling Mode", ButtonSizes.Medium)]
    [GUIColor(1f, 0.4f, 0.8f)]
    [EnableIf("@!SequencerEnabled")]
    public void PressKey6()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKey6Press();
    }
    

    [InfoBox("These controls work in both Sequencer and Manual modes", InfoMessageType.Info)]
    
    [Button("⏹️ Key 0: Stop All Effects", ButtonSizes.Medium)]
    [GUIColor(1f, 0.6f, 0.6f)]
    public void PressKey0()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKey0Press();
    }
    
    [HorizontalGroup("Utility Row 1")]
    [Button("🔄 Key R: Reset Positions", ButtonSizes.Medium)]
    [GUIColor(0.7f, 0.7f, 1f)]
    public void PressKeyR()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKeyRPress();
    }
    
    [HorizontalGroup("Utility Row 1")]
    [Button("🧹 Key C: Clear Images", ButtonSizes.Medium)]
    [GUIColor(1f, 0.4f, 0.4f)]
    public void PressKeyC()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKeyCPress();
    }
    
    [HorizontalGroup("Utility Row 2")]
    [Button("👁️ Key `: Toggle Status Text", ButtonSizes.Medium)]
    [GUIColor(0.8f, 1f, 0.4f)]
    public void PressBackQuote()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateBackQuotePress();
    }
    
    [HorizontalGroup("Utility Row 2")]
    [Button("🏷️ Key H: Toggle Logo", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 1f)]
    public void PressKeyH()
    {
        if (ValidateReference()) mosaicImagePlacer.SimulateKeyHPress();
    }
    
  
    [HorizontalGroup("Quick Actions")]
    [Button("📋 Show Image Info", ButtonSizes.Small)]
    [GUIColor(0.8f, 0.8f, 1f)]
    public void ShowImageInfo()
    {
        if (ValidateReference()) mosaicImagePlacer.ShowImageInfo();
    }
    
    [HorizontalGroup("Quick Actions")]
    [Button("🔍 Rediscover Images", ButtonSizes.Small)]
    [GUIColor(0.8f, 0.8f, 1f)]
    public void RediscoverImages()
    {
        if (ValidateReference()) mosaicImagePlacer.RediscoverImages();
    }
    
    [HorizontalGroup("Quick Actions")]
    [Button("📷 Test Photo Frame", ButtonSizes.Small)]
    [GUIColor(1f, 0.4f, 0.8f)]
    public void TestPhotoFrame()
    {
        if (ValidateReference()) mosaicImagePlacer.TestPhotoFrame();
    }
    
    // ===== HELPER METHODS =====
    
    private bool ValidateReference()
    {
        if (mosaicImagePlacer == null)
        {
            Debug.LogError("[MosaicSequenceController] MosaicImagePlacer reference is not assigned!");
            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Returns the appropriate button color based on whether the key is valid in the current sequence
    /// </summary>
    private Color GetButtonColor(int keyNumber)
    {
        if (mosaicImagePlacer == null) return Color.gray;
        
        if (!mosaicImagePlacer.enableSequencer)
        {
            // Manual mode - all keys are valid
            switch (keyNumber)
            {
                case 1: return new Color(0.2f, 1f, 0.2f, 1f); // Green
                case 2: return new Color(0.8f, 0.4f, 1f, 1f); // Purple
                case 3: return new Color(0.4f, 1f, 0.8f, 1f); // Cyan
                case 4: return new Color(0.2f, 0.8f, 1f, 1f); // Blue
                default: return Color.white;
            }
        }
        else
        {
            // Sequencer mode - highlight the expected key
            if (mosaicImagePlacer.IsKeyValidForCurrentSequence(keyNumber))
            {
                return new Color(0.2f, 1f, 0.2f, 1f); // Bright green for valid key
            }
            else
            {
                return new Color(0.5f, 0.5f, 0.5f, 1f); // Gray for invalid keys
            }
        }
    }
    

    [InfoBox("The sequencer status above updates automatically to show which key should be pressed next.", InfoMessageType.Info)]
    
    // Auto-refresh the inspector to show current status
    private void OnValidate()
    {
        // This helps keep the inspector updated with current status
    }

    private void Update()
    {
#if UNITY_EDITOR

        // Force inspector refresh in edit mode to show real-time status
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }

#endif
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(RunSequence1());
        }
    }

    private void Start()
    {
        StartCoroutine(RunSequence1());
    }

    public bool isAlreadyStarted;
} 