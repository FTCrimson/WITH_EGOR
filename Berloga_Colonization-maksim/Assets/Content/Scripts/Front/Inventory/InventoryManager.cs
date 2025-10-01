using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public Transform inventoryPanel;
    public List<InventorySlots> inventorySlots = new List<InventorySlots>();
    public GameObject self;

    private void Awake()
    {
        if (self == null) self = gameObject;
        if (inventoryPanel == null)
        {
            var t = transform.Find("InventoryPanel");
            if (t != null) inventoryPanel = t;
        }
    }

    private void Start()
    {
        Hide();

        var mgm = MainGameManager.Instance;
        if (mgm != null)
        {
            mgm.OnInventoryClosed += MainGameManager_OnInventoryClosed;
            mgm.OnInventoryOpend += MainGameManager_OnInventoryOpend;
        }

        if (inventoryPanel != null)
        {
            for (int i = 0; i < inventoryPanel.childCount; i++)
            {
                var slot = inventoryPanel.GetChild(i).GetComponent<InventorySlots>();
                if (slot != null)
                {
                    inventorySlots.Add(slot);
                }
            }
        }
    }

    private void MainGameManager_OnInventoryOpend(object sender, System.EventArgs e)
    {
        Show();
    }

    private void MainGameManager_OnInventoryClosed(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void Show()
    {
        if (self != null) self.SetActive(true);
    }

    private void Hide()
    {
        if (self != null) self.SetActive(false);
    }
}
