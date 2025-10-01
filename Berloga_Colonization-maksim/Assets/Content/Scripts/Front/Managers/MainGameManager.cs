using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameManager : MonoBehaviour
{
    public static MainGameManager Instance { get; private set; }

    public event EventHandler OnGamePaused;
    public event EventHandler OnGameUnpaused;
    public event EventHandler OnInventoryOpend;
    public event EventHandler OnInventoryClosed;

    private bool isGamePaused = false;
    private bool isInventoryOpen = false; 

    private void Awake()
    {
        Instance = this;
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInventoryAction += GameInput_OnInventoryAction;
    }

    private void Start()
    {
        
    }

    private void GameInput_OnInventoryAction(object sender, EventArgs e)
    {
        ToggleInventory();
    }

    private void GameInput_OnPauseAction(object sender, EventArgs e)
    {
        TogglePauseGame();
    }

    public void TogglePauseGame()
    {
        isGamePaused = !isGamePaused;
        if (isGamePaused && !isInventoryOpen)
        {
            Time.timeScale = 0f;

            OnGamePaused?.Invoke(this, EventArgs.Empty);
        }
        else if (!isGamePaused && isInventoryOpen)
        {
            Time.timeScale = 1f;

            OnGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        if (isInventoryOpen)
        {
            Time.timeScale = 0f;

            OnInventoryOpend?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1f;

            OnInventoryClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
