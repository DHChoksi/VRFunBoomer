using System;
using UnityEngine;

public static class BombEvents
{
    
    public static event Action<GameObject, Vector3> OnBombHitEnemy;

    public static void BombHitEnemy(GameObject enemy, Vector3 explosionPosition)
    {
        OnBombHitEnemy?.Invoke(enemy, explosionPosition);
    }
}
