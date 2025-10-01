using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType {None, Resource, Food, Weapon, Instrument}

public class ItemScripttableObject : ScriptableObject
{
    public ItemType itemType;
    public string itemName;
    public GameObject itemPefab;
    public int maxAmount;
    public string itemDescription;
}
