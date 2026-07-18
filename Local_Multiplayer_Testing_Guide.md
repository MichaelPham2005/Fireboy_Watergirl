# 🎮 How to Test Multiplayer Locally (Editor + Build)

This guide provides the standard workflow for testing Host/Client network interactions in Photon Fusion 2 by running a Standalone Build alongside the Unity Editor.

## Step 1: Verify Build Settings
1. In Unity, go to **File > Build Settings**.
2. Make sure all necessary scenes are in the "Scenes in Build" list (and checked). They must be in the correct order:
   - `Assets/Scenes/Home.unity` (Usually Index 0)
   - `Assets/Scenes/LobbyScene.unity`
   - `Assets/Scenes/Level_01.unity`
   - *(and any other levels you wish to test)*
3. Ensure your Platform is set to **Windows, Mac, Linux** (Standalone).

## Step 2: Create the Standalone Build
1. Still in the Build Settings window, click **Build** (or hit `Ctrl+B`).
2. Create a new folder outside of your `Assets` folder (e.g., call it `Builds` in your project root) and select it.
3. Wait for Unity to compile and build the executable (`.exe` on Windows) file.

## Step 3: Start the Host in the Unity Editor
1. Open your starting scene (e.g., `Home.unity`) in the Unity Editor.
2. Press the **Play** button at the top of the Editor.
3. Navigate through the UI to play online and select **Host Game**.
4. The game will generate a **4-character Room Code** on the screen. Leave the editor running!
   > **Note:** Because you are the Host, you will automatically be assigned State Authority over the game and you will control **Fireboy**.

## Step 4: Start the Client in the Standalone App
1. Open the `.exe` file you built in Step 2.
2. Navigate through the UI to play online and select **Join Game**.
3. When prompted, type in the exact **4-character Room Code** that is showing in your Unity Editor.
4. Click Join.
   > **Note:** The network will automatically detect that Fireboy is taken, and will assign this standalone window control over **Watergirl**.

## Step 5: Test the Synchronization
- Put the Unity Editor and the Standalone game side-by-side on your monitor.
- Press the arrow keys in the Unity Editor to move Fireboy. You should see him move perfectly in the standalone window.
- Click into the standalone window and press the WASD keys (or arrow keys) to move Watergirl. You should see her move perfectly in the Unity Editor.
