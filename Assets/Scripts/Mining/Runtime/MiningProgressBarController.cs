using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiningProgressBarController : MonoBehaviour
{
    private sealed class MiningBarRuntimeHandle
    {
        public MiningProgressBar MiningProgressBar;
        public bool IsOwnedInstance;
    }

    [Header("Refs")]
    [SerializeField] private MiningProgressBar _miningBarPrefab;
    [SerializeField] private Transform _barsContainer;

    private readonly Dictionary<IMineableTarget, MiningBarRuntimeHandle> _barsByTarget = new();
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
            if (pair.Value == null || pair.Value.MiningProgressBar == null)
                continue;

            if (pair.Value.IsOwnedInstance)
                Destroy(pair.Value.MiningProgressBar.gameObject);
            else
                pair.Value.MiningProgressBar.SetIdle();
        }

        _barsByTarget.Clear();
        _targetsToRemoveBuffer.Clear();
    }

    private MiningProgressBar EnsureBar(IMineableTarget target)
    {
        if (_barsByTarget.TryGetValue(target, out var existingHandle) && existingHandle?.MiningProgressBar != null)
            return existingHandle.MiningProgressBar;

        if (TryFindChildBar(target, out var childBar))
        {
            _barsByTarget[target] = new MiningBarRuntimeHandle
            {
                MiningProgressBar = childBar,
                IsOwnedInstance = false
            };
            return childBar;
        }

        if (_miningBarPrefab == null)
            return null;

        var parent = _barsContainer != null ? _barsContainer : transform;
        var bar = Instantiate(_miningBarPrefab, parent);
        bar.SetIdle();
        _barsByTarget[target] = new MiningBarRuntimeHandle
        {
            MiningProgressBar = bar,
            IsOwnedInstance = true
        };
        return bar;
    }

    private bool TryFindChildBar(IMineableTarget target, out MiningProgressBar miningProgressBar)
    {
        miningProgressBar = null;

        if (target is not Component targetComponent)
            return false;

        miningProgressBar = targetComponent.GetComponent<MiningProgressBar>();
        if (miningProgressBar != null)
            return true;

        miningProgressBar = targetComponent.GetComponentInChildren<MiningProgressBar>(true);
        if (miningProgressBar != null)
            return true;

        miningProgressBar = targetComponent.GetComponentInParent<MiningProgressBar>(true);
        return miningProgressBar != null;
    }

    private void RemoveBar(IMineableTarget target)
    {
        if (!_barsByTarget.TryGetValue(target, out var barRuntimeHandle) || barRuntimeHandle == null)
            return;

        var bar = barRuntimeHandle.MiningProgressBar;
        if (bar != null)
        {
            if (barRuntimeHandle.IsOwnedInstance)
                Destroy(bar.gameObject);
            else
                bar.SetIdle();
        }

        _barsByTarget.Remove(target);
    }
}
