using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Network;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Button hostButton;
    public Button joinButton;
    public TMP_InputField roomCodeInput;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        // Hook up the buttons via code so you don't have to do it in the Inspector
        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);

        // Listen for status updates from the NetworkRunnerController
        if (NetworkRunnerController.Instance != null)
        {
            NetworkRunnerController.Instance.OnStatusChanged += UpdateStatusText;
            NetworkRunnerController.Instance.OnRoomCodeGenerated += DisplayRoomCode;
            NetworkRunnerController.Instance.OnError += DisplayError;
            NetworkRunnerController.Instance.OnDisconnectMessage += DisplayError;
        }
    }

    private void OnDestroy()
    {
        if (NetworkRunnerController.Instance != null)
        {
            NetworkRunnerController.Instance.OnStatusChanged -= UpdateStatusText;
            NetworkRunnerController.Instance.OnRoomCodeGenerated -= DisplayRoomCode;
            NetworkRunnerController.Instance.OnError -= DisplayError;
            NetworkRunnerController.Instance.OnDisconnectMessage -= DisplayError;
        }
    }

    private void OnHostClicked()
    {
        if (NetworkRunnerController.Instance != null)
        {
            NetworkRunnerController.Instance.StartHost();
        }
    }

    private void OnJoinClicked()
    {
        if (NetworkRunnerController.Instance != null && roomCodeInput != null)
        {
            // Read the text from the input field and try to join!
            string code = roomCodeInput.text.Trim();
            if (!string.IsNullOrEmpty(code))
            {
                NetworkRunnerController.Instance.StartClient(code);
            }
            else
            {
                if (statusText != null) statusText.text = "Please enter a Room Code!";
            }
        }
    }

    private void UpdateStatusText(ConnectionStatus status)
    {
        if (statusText != null)
        {
            if (status == ConnectionStatus.InLobby)
            {
                if (NetworkRunnerController.Instance.Runner != null && NetworkRunnerController.Instance.Runner.IsServer)
                {
                    statusText.text = "Room Code: " + NetworkRunnerController.Instance.CurrentRoomCode + "\nWaiting for player...";
                }
                else
                {
                    statusText.text = "Connected! Waiting for host to start...";
                }
            }
            else
            {
                statusText.text = "Status: " + status.ToString();
            }
        }
    }

    private void DisplayRoomCode(string code)
    {
        if (statusText != null)
        {
            statusText.text = "Room Code: " + code + "\nWaiting for player...";
        }
    }

    private void DisplayError(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
