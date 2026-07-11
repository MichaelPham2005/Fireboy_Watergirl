using Fusion;

namespace Network
{
    /// <summary>
    /// Holds input data for the local player to be sent over the network via Photon Fusion.
    /// </summary>
    public struct PlayerInputData : INetworkInput
    {
        /// <summary>
        /// Horizontal movement input (-1, 0, or 1).
        /// </summary>
        public float Horizontal;

        /// <summary>
        /// True if the jump button was pressed this tick.
        /// </summary>
        public NetworkBool JumpPressed;
    }
}
