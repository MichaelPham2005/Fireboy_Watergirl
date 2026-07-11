namespace Network
{
    /// <summary>
    /// Manages the current game mode for the application.
    /// Determines if systems should use Photon Fusion (OnlineMultiplayer) or run locally (LocalCoop).
    /// </summary>
    public static class GameModeManager
    {
        public enum GameMode
        {
            LocalCoop,
            OnlineMultiplayer
        }

        /// <summary>
        /// The current game mode. Defaults to LocalCoop.
        /// </summary>
        public static GameMode CurrentMode { get; set; } = GameMode.LocalCoop;
    }
}
