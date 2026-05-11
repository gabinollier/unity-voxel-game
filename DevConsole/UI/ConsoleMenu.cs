using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Events;
using System.Linq;
using Lean.Gui;

public class ConsoleMenu : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] Key enterKey = Key.Enter;
    [SerializeField] Key autoCompleteKey = Key.Tab;

    [Header("Events")]
    [SerializeField, Tooltip("Invoked when the console is opened")] UnityEvent OnOpen;
    [SerializeField, Tooltip("Invoked when a command is entered")] UnityEvent OnClose;

    [Header("Components")]
    [SerializeField] LeanToggle leanToggle;
    [SerializeField] TextMeshProUGUI outputTextComponent;
    [SerializeField] Button closeButton;

    TMP_InputField _inputField;
    string _outputText = "";
    StringBuilder _autoCompletionTexts;
    string _autoCompletionText;

    private void OnDestroy()
    {
        CommandConsole.Clear();
    }

    private void Awake()
    {
        _autoCompletionTexts = new StringBuilder();

        _inputField = GetComponentInChildren<TMP_InputField>(includeInactive:true);

        _inputField.onValueChanged.AddListener((string text) => ChangeAutocompletion(text));

        _inputField.onEndEdit.AddListener((string text) =>
        {
            if (Keyboard.current[enterKey].wasPressedThisFrame)
                OnCommandEnter(text);
        });

        CommandConsole.OnConsoleTextChanged.AddListener((string text) =>
        {
            _outputText = text;
            UpdateText();
        });

        closeButton.onClick.AddListener((UnityAction)(() =>
        {
            _inputField.text = "";
            leanToggle.TurnOff();
        }));
    }

    private void Start()
    {
        leanToggle.TurnOff();
    }

    private void LateUpdate()
    {
        if (Keyboard.current[autoCompleteKey].wasPressedThisFrame)
        {
            if (_autoCompletionText == "")
                return;

            _inputField.text = _autoCompletionText;
            _inputField.caretPosition = _inputField.text.Length;
            _inputField.ForceLabelUpdate();
        }
    }

    void OnCommandEnter(string text)
    {
        _inputField.text = "";
        CommandConsole.Log(">> " + text);
        CommandConsole.TryExecuteCommand(text);
        _inputField.Select();
        _inputField.ActivateInputField();
    }

    [Command("exit", "Closes this console", false)]
    void ExitCommand()
    {
        leanToggle.TurnOff();
    }

    public void DisableInput()
    {
        _inputField.DeactivateInputField();
    }

    public void EnableInput()
    {
        _inputField.Select();
        _inputField.ActivateInputField();
    }

    void UpdateText()
    {
        outputTextComponent.text = $"{_outputText}\n<color=#AAAAAA><i>{_autoCompletionTexts}</i>";
    }

    void ChangeAutocompletion(string text)
    {
        _autoCompletionTexts.Clear();

        var possibilities = CommandAutoCompleter.GetAutoCompletion(text).ToList();

        for (int i = 0; i < possibilities.Count; i++)
        {
            _autoCompletionTexts.AppendLine(possibilities[i]);

            if (i == possibilities.Count - 1)
                _autoCompletionText = possibilities[i];
        }

        if (possibilities.Count == 0)
            _autoCompletionText = "";

        UpdateText();
    }
}
