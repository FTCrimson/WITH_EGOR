using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Food Item", menuName = "Inventory/Items/New Food Item")]
public class FoodItem : ItemScripttableObject
{
    public float healAmount;
    private void Awake()
    {
        itemType = ItemType.Food;
    }
}
