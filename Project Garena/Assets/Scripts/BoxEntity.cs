using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BoxEntity
{
    public string id;
    public ItemSubType subType;

    // Traits currently applied to the box
    public HashSet<TraitType> traits = new HashSet<TraitType>();

    // Trait tile data
    public bool isTraitTile = false;
    public TraitType tileTrait;

    // Grid footprint
    public Vector2Int anchor;
    public Vector2Int size = Vector2Int.one;

    // Fire expires after 5 seconds
    public float fireTimer = 0f;

    public ItemKind kind = ItemKind.Box;
    public ElementMask elements = ElementMask.None;
    public float activeCooldown = 0f;
    public float activeCooldownMax = 1.0f;
    public float potency = 1f;

    public BoxEntity(ItemSubType subType)
    {
        this.subType = subType;
        id = Guid.NewGuid().ToString();
        kind = subType switch
        {
            ItemSubType.Knife => ItemKind.Sword,
            ItemSubType.WaterBottle => ItemKind.Potion,
            ItemSubType.Bread => ItemKind.Bread,
            ItemSubType.Ghost => ItemKind.Ghost,
            ItemSubType.TraitTile => ItemKind.TraitTile,
            _ => ItemKind.Box
        };
    }

    public static BoxEntity CreateTraitTile(TraitType t)
    {
        var e = new BoxEntity(ItemSubType.TraitTile);
        e.isTraitTile = true;
        e.tileTrait = t;
        e.kind = ItemKind.TraitTile;
        return e;
    }

    public bool Has(TraitType t) => traits != null && traits.Contains(t);

    public void AddTrait(TraitType t)
    {
        if (traits == null) traits = new HashSet<TraitType>();
        if (t == TraitType.Fire) traits.Remove(TraitType.Ice);
        if (t == TraitType.Ice) traits.Remove(TraitType.Fire);
        traits.Add(t);

        if (t == TraitType.Fire)
        {
            fireTimer = 5f;
        }
    }
}
