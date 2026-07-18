# Photon Fusion Setup Guide for Developers

Welcome to the Fireboy & Watergirl online multiplayer project! This project uses **Photon Fusion** for networking. Since networking relies on cloud servers for matchmaking and relay, each developer needs to ensure their Unity Editor is properly configured with a Photon App ID to test online features.

Follow these steps to get your local environment ready for online multiplayer testing.

## 1. Create a Photon Account (If you don't have a team App ID)
If the lead developer hasn't provided you with a shared Team App ID, you will need to create your own for local testing:
1. Go to the [Photon Engine Dashboard](https://dashboard.photonengine.com/).
2. Sign in or create a new free account.
3. Click on **Create a New App**.
4. Set the **Photon Type** to **Fusion**.
5. Give your app a name (e.g., `FireboyWatergirl_Dev_YourName`).
6. Click **Create**.
7. Once created, you will see a long alphanumeric string called the **App ID**. Copy this to your clipboard.

## 2. Configure Unity

1. Open the project in Unity.
2. The Fusion SDK is already included in the `Assets/Photon` directory, so you don't need to download any packages.
3. In the top menu bar of Unity, go to **Fusion > Fusion Hub**. 
   *(If the Hub doesn't open, you can manually locate the settings file in the Project window at: `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`)*
4. In the Fusion Hub window, find the **App Id** field.
5. Paste your copied App ID into this field.
6. Unity will automatically save this configuration. 

> [!WARNING]  
> `PhotonAppSettings.asset` is a tracked file in the repository (it holds the team's shared/production App ID). If you are using a personal App ID for testing, Git will still track your changes even though it's in `.gitignore`.
> To prevent accidentally overwriting the team's App ID in source control, run this command in your terminal before changing the App ID:
> ```bash
> git update-index --skip-worktree Assets/Photon/Fusion/Resources/PhotonAppSettings.asset
> ```
> *(If you ever need to commit changes to it again, use `--no-skip-worktree`)*

## 3. Verify Setup
To verify everything is working:
1. Open the main menu or network lobby scene (e.g., `Assets/Scenes/Menu.unity` or wherever the `GameModeManager` is initialized).
2. Press **Play** in the Unity Editor.
3. Select **Online Multiplayer** mode.
4. If you connect successfully and do not see any red errors in the Unity Console regarding "Invalid App ID" or "Connection Timeout", your setup is complete!

## 4. Testing Multiplayer Locally
If you want to test both the Host and the Client simultaneously on your own machine without building the game:
1. Install the **ParrelSync** package (if not already installed) via the Unity Package Manager. 
2. Use ParrelSync to open a clone of your Unity Editor.
3. Press Play in the original editor and Host the game.
4. Press Play in the cloned editor and Join the game. 

Happy coding!
