using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiningProgressBarController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MiningProgressBar _miningBarPrefab;
    [SerializeField] private Transform _barsContainer;

    private readonly Dictionary<IMineableTarget, MiningProgressBar> _barsByTarget = new();
    private readonly List<IMineableTarget> _targetsToRemoveBuffer = new();

    private void Update()
    {
        if (_barsByTarget.Count == 0)
            return;

        _targetsToRemoveBuffer.Clear();
        foreach (var pair in _barsByTarget)
        {
            var target = pair.Key;
            if (target == null || target.IsDepleted || !target.HasDamage)
                _targetsToRemoveBuffer.Add(target);
        }

        for (var i = 0; i < _targetsToRemoveBuffer.Count; i++)
            RemoveBar(_targetsToRemoveBuffer[i]);
    }

    public void ShowProgress(IMineableTarget target)
    {
        if (target == null)
            return;

        ShowProgress(target, target.MiningProgressNormalized);
    }

    public void ShowProgress(IMineableTarget target, float progress)
    {
        if (target == null)
            return;

        var bar = EnsureBar(target);
        if (bar == null)
            return;

        bar.SetWorldPosition(target.WorldPosition);
        bar.SetProgressValue(progress);
    }

    public void HandleMiningStopped(IMineableTarget target)
    {
        if (target == null || !_barsByTarget.TryGetValue(target, out var bar) || bar == null)
            return;

        if (target.HasDamage && !target.IsDepleted)
            ShowProgress(target);
        else
            RemoveBar(target);
    }

    public void ClearAll()
    {
        foreach (var pair in _barsByTarget)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        _barsByTarget.Clear();
        _targetsToRemoveBuffer.Clear();
    }

    private MiningProgressBar EnsureBar(IMineableTarget target)
    {
        if (_barsByTarget.TryGetValue(target, out var existingBar) && existingBar != null)
            return existingBar;

        if (_miningBarPrefab == null)
            return null;

        var parent = _barsContainer != null ? _barsContainer : transform;
        var bar = Instantiate(_miningBarPrefab, parent);
        bar.SetIdle();
        _barsByTarget[target] = bar;
        return bar;
    }

    private void RemoveBar(IMineableTarget target)
    {
        if (!_barsByTarget.TryGetValue(target, out var bar))
            return;

        if (bar != null)
            Destroy(bar.gameObject);

        _barsByTarget.Remove(target);
    }
}
