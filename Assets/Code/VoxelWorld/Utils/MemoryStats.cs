using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

public class MemoryStats : MonoBehaviour
{
    const int BYTES_TO_MB = 1000 * 1000;
    string statsText;
    ProfilerRecorder totalReservedMemoryRecorder;
    ProfilerRecorder gcReservedMemoryRecorder;
    ProfilerRecorder systemUsedMemoryRecorder;

    GUIStyle style;

    private void Start()
    {
        style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.white;
        style.normal.background = Texture2D.grayTexture;
        style.padding.left = 5;
        style.padding.top = 5;
    }

    void OnEnable()
    {
        totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
        gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
        systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
    }

    void OnDisable()
    {
        totalReservedMemoryRecorder.Dispose();
        gcReservedMemoryRecorder.Dispose();
        systemUsedMemoryRecorder.Dispose();
    }

    void Update()
    {
        var sb = new StringBuilder(500);
        if (totalReservedMemoryRecorder.Valid)
            sb.AppendLine($"Total Reserved Memory: {(totalReservedMemoryRecorder.LastValue / BYTES_TO_MB).ToString("F2")} MB");
        if (gcReservedMemoryRecorder.Valid)
            sb.AppendLine($"GC Reserved Memory: {(gcReservedMemoryRecorder.LastValue / BYTES_TO_MB).ToString("F2")} MB");
        if (systemUsedMemoryRecorder.Valid)
            sb.AppendLine($"System Used Memory: {(systemUsedMemoryRecorder.LastValue / BYTES_TO_MB).ToString("F2")} MB");
        statsText = sb.ToString();
    }

    void OnGUI()
    {

        GUI.TextArea(new Rect(10, 180, 250, 50), statsText, style);

        GUI.Box(new Rect(10, 230, 250, 24),
    $"Total Used Memory:  {(Profiler.GetTotalAllocatedMemoryLong() / BYTES_TO_MB).ToString("F2")} MB", style);
    }
}

