using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

public class MemoryStats : MonoBehaviour
{
    public const float BYTES_TO_MB = 1000 * 1000;
    private string _statsText;
    private ProfilerRecorder _totalReservedMemoryRecorder;
    private ProfilerRecorder _gcReservedMemoryRecorder;
    private ProfilerRecorder _systemUsedMemoryRecorder;

    void OnEnable()
    {
        _totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
        _gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
        _systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
    }

    void OnDisable()
    {
        _totalReservedMemoryRecorder.Dispose();
        _gcReservedMemoryRecorder.Dispose();
        _systemUsedMemoryRecorder.Dispose();
    }

    void Update()
    {
        var sb = new StringBuilder(500);
        if (_totalReservedMemoryRecorder.Valid)
            sb.AppendLine($"Total Reserved Memory: {(_totalReservedMemoryRecorder.LastValue / BYTES_TO_MB).ToString("F2")} MB");
        if (_gcReservedMemoryRecorder.Valid)
            sb.AppendLine($"GC Reserved Memory: {(_gcReservedMemoryRecorder.LastValue / BYTES_TO_MB).ToString("F2")} MB");
        if (_systemUsedMemoryRecorder.Valid)
            sb.AppendLine($"System Used Memory: {(_systemUsedMemoryRecorder.LastValue / BYTES_TO_MB).ToString("F2")} MB");

        sb.AppendLine($"Total Used Memory: {(Profiler.GetTotalAllocatedMemoryLong() / BYTES_TO_MB).ToString("F2")} MB");

        _statsText = sb.ToString();
    }

    void OnGUI()
    {
        GUI.TextArea(UIScaler.GetScaledRect(10, 180, 250, 70), _statsText, UIScaler.scaledStyle);
    }
}

