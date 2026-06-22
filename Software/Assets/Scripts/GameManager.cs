using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// [ExecuteInEditMode]
public class GameManager : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown levelDropdown;
    [SerializeField] private TMP_Dropdown modeDropdown;
    [SerializeField] private TMP_Dropdown typeDropdown;

    [Header("References")]
    [SerializeField] private InstantiateBalls levelManager;

    void Start() {
        levelManager = this.GetComponent<InstantiateBalls>();
    // Activate second display
	    if (Display.displays.Length > 1) {
	        Display.displays[1].Activate();
	    }

        SetupLevelDropdown();
        SetupModeDropdown();
        SetupTypeDropdown();

	}

    // // ── Level ────────────────────────────────────────────────
    void SetupLevelDropdown()
    {
        levelDropdown.ClearOptions();
        levelDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Easy", "Medium", "Hard"
        });

        // Set to current level
        levelDropdown.value = (int)levelManager.chosenLevel;
        levelDropdown.onValueChanged.AddListener(OnLevelChanged);
    }

    void OnLevelChanged(int index)
    {
        levelManager.chosenLevel = (InstantiateBalls.Level)index;
        levelManager.LoadConfig(); // make LoadConfig() public in LevelManager
        Debug.Log($"[Master] Level changed to {levelManager.chosenLevel} "); // +$"→ {levelManager.nivel.lines}x{levelManager.nivel.columns}");
    }

    // ── Stimulus Mode ────────────────────────────────────────
    void SetupModeDropdown()
    {
        modeDropdown.ClearOptions();
        modeDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Haptic", "Audio", "AudioHaptic", "None"
        });

        // Set to current mode
        modeDropdown.value = (int)levelManager.chosenStimulusType;
        modeDropdown.onValueChanged.AddListener(OnTypeChanged);
    }

    void OnTypeChanged(int index)
    {
        levelManager.chosenStimulusType = (InstantiateBalls.StimulusType)index;
        Debug.Log($"[Master] Mode changed to {levelManager.chosenStimulusType}");
    }

    // ── Stimulus Mode ────────────────────────────────────────
    void SetupTypeDropdown()
    {
        modeDropdown.ClearOptions();
        modeDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Reward", "ArmID", "DirectionStatic", "DirectionDynamic"
        });

        // Set to current mode
        modeDropdown.value = (int)levelManager.chosenStimulusMode;
        modeDropdown.onValueChanged.AddListener(OnModeChanged);
    }

    void OnModeChanged(int index)
    {
        levelManager.chosenStimulusMode = (InstantiateBalls.StimulusMode)index;
        Debug.Log($"[Master] Mode changed to {levelManager.chosenStimulusMode}");
    }


}
