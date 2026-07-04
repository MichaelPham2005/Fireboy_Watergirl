using UnityEngine;

/// <summary>
/// Identifies the type of liquid pool this object represents.
/// Attach this component to liquid GameObjects (Lava, Water, Goo) in the scene.
/// PlayerHealth reads this to determine whether a specific player should die on contact.
/// </summary>
public enum LiquidType
{
    RedLava  = 0,
    BlueWater = 1,
    GreenGoo  = 2
}

public class PoolElement : MonoBehaviour
{
    [Header("Liquid Settings")]
    [Tooltip("The type of liquid this pool contains. Determines which player character dies on contact.")]
    public LiquidType liquidType = LiquidType.RedLava;
}
