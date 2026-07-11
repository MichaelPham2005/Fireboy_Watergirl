using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        InLobby,
        InGame
    }

    /// <summary>
    /// Manages the lifecycle of the Photon Fusion NetworkRunner.
    /// Handles hosting, joining, room code generation, and network callbacks.
    /// </summary>
    public class NetworkRunnerController : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static NetworkRunnerController Instance { get; private set; }
        
        public NetworkRunner Runner { get; private set; }
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
        public string CurrentRoomCode { get; private set; } = string.Empty;

        public event Action<ConnectionStatus> OnStatusChanged;
        public event Action<string> OnRoomCodeGenerated;
        public event Action<string> OnError;
        public event Action<string> OnDisconnectMessage;
        public event Action<PlayerRef> OnPlayerJoinedEvent;
        public event Action<PlayerRef> OnPlayerLeftEvent;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null); // Fix DontDestroyOnLoad warning if it's a child
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Starts the game as a Host. Generates a random 4-character room code.
        /// </summary>
        public async void StartHost()
        {
            if (Runner != null) Shutdown();

            SetStatus(ConnectionStatus.Connecting);
            GameModeManager.CurrentMode = GameModeManager.GameMode.OnlineMultiplayer;

            CurrentRoomCode = GenerateRoomCode();
            OnRoomCodeGenerated?.Invoke(CurrentRoomCode);

            var oldRunner = gameObject.GetComponent<NetworkRunner>();
            if (oldRunner != null) Destroy(oldRunner);
            
            GameObject runnerGO = new GameObject("SessionRunner");
            DontDestroyOnLoad(runnerGO);
            Runner = runnerGO.AddComponent<NetworkRunner>();
            Runner.ProvideInput = true;
            Runner.AddCallbacks(this);

            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = CurrentRoomCode,
                SceneManager = runnerGO.AddComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                SetStatus(ConnectionStatus.InLobby);
                Debug.Log($"Started Host with session: {CurrentRoomCode}");
            }
            else
            {
                SetStatus(ConnectionStatus.Disconnected);
                OnError?.Invoke($"Failed to start host: {result.ShutdownReason}");
                Debug.LogError($"Failed to start host: {result.ShutdownReason}");
            }
        }

        /// <summary>
        /// Starts the game as a Client joining a specific room code.
        /// </summary>
        public async void StartClient(string roomCode)
        {
            if (Runner != null) Shutdown();
            if (string.IsNullOrEmpty(roomCode))
            {
                OnError?.Invoke("Room code cannot be empty.");
                return;
            }

            SetStatus(ConnectionStatus.Connecting);
            GameModeManager.CurrentMode = GameModeManager.GameMode.OnlineMultiplayer;
            CurrentRoomCode = roomCode.ToUpper();

            var oldRunner = gameObject.GetComponent<NetworkRunner>();
            if (oldRunner != null) Destroy(oldRunner);
            
            GameObject runnerGO = new GameObject("SessionRunner");
            DontDestroyOnLoad(runnerGO);
            Runner = runnerGO.AddComponent<NetworkRunner>();
            Runner.ProvideInput = true;
            Runner.AddCallbacks(this);

            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = CurrentRoomCode,
                SceneManager = runnerGO.AddComponent<NetworkSceneManagerDefault>()
            });

            if (result.Ok)
            {
                SetStatus(ConnectionStatus.InLobby);
                Debug.Log($"Joined session: {CurrentRoomCode}");
            }
            else
            {
                SetStatus(ConnectionStatus.Disconnected);
                OnError?.Invoke($"Failed to join session: {result.ShutdownReason}");
                Debug.LogError($"Failed to join session: {result.ShutdownReason}");
            }
        }

        /// <summary>
        /// Shuts down the current network runner.
        /// </summary>
        public void Shutdown()
        {
            if (Runner != null)
            {
                Runner.Shutdown();
                Destroy(Runner.gameObject);
                Runner = null;
            }
            SetStatus(ConnectionStatus.Disconnected);
            CurrentRoomCode = string.Empty;
        }

        private void SetStatus(ConnectionStatus newStatus)
        {
            Status = newStatus;
            OnStatusChanged?.Invoke(Status);
        }

        /// <summary>
        /// Generates a random 4-character alphanumeric uppercase code.
        /// </summary>
        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] code = new char[4];
            for (int i = 0; i < 4; i++)
            {
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            return new string(code);
        }
        
        // --- INetworkRunnerCallbacks ---

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player.PlayerId} joined.");
            
            // If we are the Host, assign input authority to the characters already in the scene
            if (runner.IsServer)
            {
                var players = FindObjectsByType<StandardPlayerMovement>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if (p.Object == null) continue; // Safety check
                    
                    if (p.playerType == StandardPlayerMovement.PlayerType.Fireboy && player == runner.LocalPlayer)
                    {
                        p.Object.AssignInputAuthority(player);
                        Debug.Log($"Assigned Fireboy Input Authority to Host (Player {player.PlayerId})");
                    }
                    else if (p.playerType == StandardPlayerMovement.PlayerType.Watergirl && player != runner.LocalPlayer)
                    {
                        p.Object.AssignInputAuthority(player);
                        Debug.Log($"Assigned Watergirl Input Authority to Client (Player {player.PlayerId})");
                    }
                }
            }

            OnPlayerJoinedEvent?.Invoke(player);
            
            // Once we have 2 players, the host loads the game level!
            if (runner.IsServer && player != runner.LocalPlayer)
            {
                int levelIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Level_01.unity");
                if (levelIndex >= 0)
                {
                    runner.LoadScene(SceneRef.FromIndex(levelIndex));
                }
                else
                {
                    Debug.LogError("Level_01 not found in Build Settings! Make sure it's added. (Expected path: Assets/Scenes/Level_01.unity)");
                }
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player.PlayerId} left.");
            OnPlayerLeftEvent?.Invoke(player);

            // Mid-game disconnect: if the other player leaves during gameplay, return to menu
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "Home" && currentScene != "LobbyScene")
            {
                OnDisconnectMessage?.Invoke("The other player has disconnected.");
                // Give a moment for the message to show, then clean up
                StartCoroutine(ReturnToHomeAfterDelay(2f));
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) 
        {
            if (GameModeManager.CurrentMode != GameModeManager.GameMode.OnlineMultiplayer) return;

            PlayerInputData data = new PlayerInputData();
            
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) data.Horizontal = -1f;
                else if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) data.Horizontal = 1f;

                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) data.JumpPressed = true;
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) data.Horizontal = -1f;
                else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) data.Horizontal = 1f;

                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) data.JumpPressed = true;
            }

            input.Set(data);
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"Runner Shutdown: {shutdownReason}");
            SetStatus(ConnectionStatus.Disconnected);
            
            // If we disconnect mid-game, load the main menu
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene != "Home" && currentScene != "LobbyScene")
            {
                SceneManager.LoadScene("Home");
            }
        }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.Log($"Disconnected from Server: {reason}");
            OnDisconnectMessage?.Invoke("Host disconnected. Returning to menu...");
            StartCoroutine(ReturnToHomeAfterDelay(2f));
        }

        private System.Collections.IEnumerator ReturnToHomeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Shutdown();
            GameModeManager.CurrentMode = GameModeManager.GameMode.LocalCoop;
            SceneManager.LoadScene("Home");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    }
}
