using TMPro;
using UnityEngine;

/// <summary>
/// Simple UI presenter that shows "Day N — HH:MM".
/// </summary>
[DisallowMultipleComponent]
public sealed class TimeOfDayText : MonoBehaviour
{
    [SerializeField] private DayNightSystem _time;
    [SerializeField] private TMP_Text _text;

    private void Awake()
    {
        if (_time == null)
            _time = FindFirstObjectByType<DayNightSystem>();
        if (_text == null)
            _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (_time != null)
        {
            _time.OnMinuteChanged += HandleMinuteChanged;
            HandleMinuteChanged(_time.Hour, _time.Minute);
        }
    }

    private void OnDisable()
    {
        if (_time != null)
        {
            _time.OnMinuteChanged -= HandleMinuteChanged;
        }
    }

    private void HandleMinuteChanged(int hour, int minute)
    {
        if (_text == null || _time == null) 
            return;
        
        _text.text = $"Day {_time.CurrentDay} — {hour:00}:{minute:00}";
    }
}
