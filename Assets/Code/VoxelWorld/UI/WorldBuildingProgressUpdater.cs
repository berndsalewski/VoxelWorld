using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// updates the progressbar during world generation
/// </summary>
public class WorldBuildingProgressUpdater : MonoBehaviour
{
    [SerializeField]
    private Slider progressbar;

    public void Show(int maxValue)
    {
        progressbar.value = 0;
        progressbar.maxValue = maxValue;
        gameObject.SetActive(true);
    }

    public void UpdateProgress(int delta)
    {
        progressbar.value += delta;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
