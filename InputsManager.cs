using UnityEngine;

public static class InputsManager 
{
    private static InputActions _actions;

    public static InputActions Actions { 
        get
        {
            Initialize();
            return _actions;
        }
        private set
        {
            _actions = value;
        }
    }

    [RuntimeInitializeOnLoadMethod]
    static void Initialize()
    {
        if (_actions != null)
            return;

        _actions = new InputActions();
        _actions.Enable();
    }
}
