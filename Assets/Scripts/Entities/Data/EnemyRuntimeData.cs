using System;
using UnityEngine;

[Serializable]
public sealed class EnemyRuntimeData : EntityRuntimeData
{
    public void InitializeFrom(EnemyData data)
    {
        base.InitializeFrom(data);
    }
}
