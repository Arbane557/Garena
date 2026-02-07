public enum GhostType
{
    Base,
    FireGhost,
    IceFast
}

public enum EntityType { Box, Trait }
public enum ItemSubType { Bread, Knife, WaterBottle, Ghost, TraitTile }
public enum TraitType { Fire, Ice, Sentient, Haunted }

[System.Flags]
public enum ElementMask
{
    None = 0,
    Fire = 1 << 0,
    Ice = 1 << 1
}

public enum ItemKind
{
    Box,
    Sword,
    Potion,
    Bread,
    Ghost,
    TraitTile
}
