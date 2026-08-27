using UnityEngine; 
using MoreMountains.NiceVibrations;

public class VibrationsManager : MonoBehaviour
{
    public static VibrationsManager Instance;

    protected virtual void Awake()
    {
        Instance = this;
        MMVibrationManager.iOSInitializeHaptics();
    }

    protected virtual void OnDisable()
    {
        MMVibrationManager.iOSReleaseHaptics();
    }

    public void TriggerDefault()
    {
        if (!GlobalValues.HapticEnable) return;
#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }

    public void TriggerVibrate()
    {
        if (!GlobalValues.HapticEnable) return;
        MMVibrationManager.Vibrate();
    }

    public void TriggerType(HapticTypes hapticTypes, long milliseconds = 20)
    {
        if (!GlobalValues.HapticEnable) return;
        MMVibrationManager.Haptic(hapticTypes, milliseconds);
    }

    public void TriggerLightImpact()
    {
        TriggerType(HapticTypes.LightImpact);
    }

    public void TriggerMediumImpact()
    {
        TriggerType(HapticTypes.MediumImpact);
    }

    public void TriggerSuccess()
    {
        TriggerType(HapticTypes.Success);
    }

    public void TriggerFailure()
    {
        TriggerType(HapticTypes.Failure);
    }
}
