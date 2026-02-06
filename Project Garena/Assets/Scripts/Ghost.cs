using System;
using UnityEngine;


[Serializable]
public class Ghost
{
    public GhostType type;
    public Vector2Int pos;

    public float moveInterval = 1f;
    public float moveTimer = 0f;

    public Ghost(GhostType type, Vector2Int pos, float moveInterval)
    {
        this.type = type;
        this.pos = pos;
        this.moveInterval = moveInterval;
        this.moveTimer = 0f;
    }
}
