using UnityEngine;
using UnityEngine.InputSystem;
using NaughtyAttributes;
using Lean.Gui;

public class UIManager : MonoBehaviour
{

    [SerializeField] LeanToggle noMenu, pauseMenu, consoleMenu, inventoryMenu;

    private void Awake()
    {
        noMenu.OnOn.AddListener(LockCursor);
        noMenu.OnOff.AddListener(UnlockCursor);

        InputsManager.Actions.UI.Enable();
        InputsManager.Actions.UI.OpenConsole.performed += OpenConsole;
        InputsManager.Actions.UI.OpenInventory.performed += OpenInventory;

        noMenu.TurnOn();
    }

    private void OpenInventory(InputAction.CallbackContext obj)
    {
        if (noMenu.On)
        {
            inventoryMenu.TurnOn();
        }
    }

    private void OpenConsole(InputAction.CallbackContext obj)
    {
        if (noMenu.On)
            consoleMenu.TurnOn();
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            if (noMenu.On)
            {
                pauseMenu.TurnOn();
            }
            else
            {
                noMenu.TurnOn();
            }
        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        InputsManager.Actions.Player.Enable();
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        InputsManager.Actions.Player.Disable();
    }
}
