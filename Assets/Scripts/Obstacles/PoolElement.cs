using UnityEngine;

// Define the types of liquids in the game
public enum LiquidType 
{ 
    RedLava, 
    BlueWater, 
    GreenGoo 
}

public class PoolElement : MonoBehaviour
{
    // You will select the liquid type in the Unity Inspector
    public LiquidType liquidType;
}