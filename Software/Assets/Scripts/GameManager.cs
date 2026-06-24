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

    public HapticUDPController leftController;
    public HapticUDPController rightController;

    public IMUReceiver leftIMU;
    public IMUReceiver rightIMU;

    private void Awake()
    {
        this.leftController = this.gameObject.AddComponent<HapticUDPController>();
        leftController.armID = HapticUDPController.ArmID.Left;
        this.leftIMU = this.gameObject.AddComponent<IMUReceiver>();
        leftIMU.armID = leftController.armID;

        this.rightController = this.gameObject.AddComponent<HapticUDPController>();
        rightController.armID = HapticUDPController.ArmID.Right;
        this.rightIMU = this.gameObject.AddComponent<IMUReceiver>();
        rightIMU.armID = rightController.armID;
    }
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

    private void Update()
    {
        leftController.realArmID = leftIMU.realArmID;
        rightController.realArmID = rightIMU.realArmID;

        leftController.motorOrder = leftIMU.motorOrder;
        rightController.motorOrder = rightIMU.motorOrder;

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

    // ── Stimulus Type ────────────────────────────────────────
    void SetupTypeDropdown()
    {
        typeDropdown.ClearOptions();
        typeDropdown.AddOptions(new System.Collections.Generic.List<string> {
            "Haptic", "Audio", "AudioHaptic", "None"
        });

        // Set to current mode
        typeDropdown.value = (int)levelManager.chosenStimulusType;
        typeDropdown.onValueChanged.AddListener(OnTypeChanged);
    }

    void OnTypeChanged(int index)
    {
        levelManager.chosenStimulusType = (InstantiateBalls.StimulusType)index;
        Debug.Log($"[Master] Mode changed to {levelManager.chosenStimulusType}");
    }

    // ── Stimulus Mode ────────────────────────────────────────
    void SetupModeDropdown()
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
