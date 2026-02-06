using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Entity
{
    public string id;
    public EntityType type;

    // For boxes
    public ItemSubType? subType;
    public HashSet<TraitType> traits = new HashSet<TraitType>();

    // For trait tiles
    public TraitType? trait;

    public Vector2Int velocity;
    public bool isMoving;

    public Entity(EntityType t)
    {
        id = Guid.NewGuid().ToString();
        type = t;
        velocity = Vector2Int.zero;
        isMoving = false;
    }

    public bool HasTrait(TraitType tt)
    {
        if (trait.HasValue && trait.Value == tt) return true;
        return traits != null && traits.Contains(tt);
    }
}
