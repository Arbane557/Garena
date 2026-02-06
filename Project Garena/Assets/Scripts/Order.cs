using System;
using System.Collections.Generic;

[Serializable]
public class Order
{
    public ItemSubType subType;
    public List<TraitType> requiredTraits = new List<TraitType>();
    public float timeLeft;
}
