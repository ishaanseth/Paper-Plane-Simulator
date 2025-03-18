using UnityEngine;
using UnityEngine.UI;

public class PlaneScaler : MonoBehaviour
{
    [Header("Reference to armature.002")]
    public Transform armatureTransform; // Assign armature.002 here

    [Header("UI Sliders")]
    public Slider sliderX;
    public Slider sliderY;
    public Slider sliderZ;

    void Start()
    {
        // Initialize sliders with the current scale
        Vector3 initialScale = armatureTransform.localScale;
        sliderX.value = initialScale.x;
        sliderY.value = initialScale.y;
        sliderZ.value = initialScale.z;

        // Add listener to update the scale as slider changes
        sliderX.onValueChanged.AddListener(OnSliderChanged);
        sliderY.onValueChanged.AddListener(OnSliderChanged);
        sliderZ.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnSliderChanged(float value)
    {
        // Only change the localScale of armature.002
        armatureTransform.localScale = new Vector3(
            sliderX.value,
            sliderY.value,
            sliderZ.value
        );
    }
}
