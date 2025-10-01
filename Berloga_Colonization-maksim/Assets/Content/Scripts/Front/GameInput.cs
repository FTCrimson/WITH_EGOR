using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnInventoryAction;
    public event EventHandler OnPauseAction;

    private PlayerInputAction playerInputAction;

    private void Awake()
    {
        Instance = this;

        playerInputAction = new PlayerInputAction();
        playerInputAction.UI.Enable();

        playerInputAction.UI.Inventory.performed += Inventory_performed;
        playerInputAction.UI.Pause.performed += Pause_performed;


    }

    private void Inventory_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnInventoryAction?.Invoke(this, EventArgs.Empty);
    }

    private void OnDestroy()
    {
        playerInputAction.UI.Inventory.performed -= Inventory_performed;
        playerInputAction.UI.Pause.performed -= Pause_performed;

        playerInputAction.Dispose();
    }

    
    private void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }
}
