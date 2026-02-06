using System;
using System.Collections.Generic;

[Serializable]
public class BoxEntity
{
    public string id;
    public ItemSubType subType;

    // Traits currently applied to the box
    public HashSet<TraitType> traits = new HashSet<TraitType>();

    // Fire expires after 5 seconds
    public float fireTimer = 0f;

    public BoxEntity(ItemSubType subType)
    {
        this.subType = subType;
        id = Guid.NewGuid().ToString();
    }

    public bool Has(TraitType t) => traits != null && traits.Contains(t);

    public void AddTrait(TraitType t)
    {
        if (traits == null) traits = new HashSet<TraitType>();
        traits.Add(t);

        if (t == TraitType.Fire)
        {
            fireTimer = 5f;
        }
    }
}
