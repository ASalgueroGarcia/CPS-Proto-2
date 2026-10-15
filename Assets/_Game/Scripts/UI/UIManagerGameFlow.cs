using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene navigation for the menus and the run (IGameFlow), on top of UIManager and the build
/// scenes. Registered with Services before the first scene loads. When the lobby exists,
/// EnterHub is the only thing that changes.
/// </summary>
public class UIManagerGameFlow : IGameFlow
{
    private const string MainMenuScene = "MainMenu";
    private const string RunMapScene = "_MapScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        Services.RegisterFlow(new UIManagerGameFlow());
    }

    /// <summary>
    /// The GDD has the player wake up in the lobby; there is no lobby yet, so the hub is a fresh
    /// run of the current act for now.
    /// </summary>
    public void EnterHub()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartGame();
            return;
        }

        Services.Run?.BeginRun(ActProgress.SelectedAct);
        SceneManager.LoadScene(RunMapScene);
    }

    public void EnterMainMenu()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.Resume();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
