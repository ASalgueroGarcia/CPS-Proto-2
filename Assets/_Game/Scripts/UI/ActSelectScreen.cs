using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Act select poster (GDD > Interface > HUD > Poster). A full-screen overlay opened from an
/// ActPoster in the lobby or the shop - same screen either way, the room stays visible behind.
/// One choice only: pick an open act and the game goes straight to the run map.
/// Left / Right choose, E / Enter / South button begin, Esc / East button step back.
/// </summary>
public class ActSelectScreen : MonoBehaviour
{
    [SerializeField] private GameObject overlay;
    [Tooltip("One card per act, in act order (I, II, III).")]
    [SerializeField] private ActCard[] cards;

    private const string RunMapScene = "_MapScene";

    private int _cursor;
    private int _openedFrame;

    public static bool IsOpen { get; private set; }

    private void Awake()
    {
        overlay.SetActive(false);
    }

    private void OnDisable()
    {
        if (IsOpen) Close();
    }

    public void Open()
    {
        if (IsOpen) return;

        IsOpen = true;
        _openedFrame = Time.frameCount;
        _cursor = ActProgress.IsUnlocked(ActProgress.SelectedAct) ? ActProgress.SelectedAct : 0;
        Player.IsPaused = true;
        overlay.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        IsOpen = false;
        Player.IsPaused = false;
        overlay.SetActive(false);
    }

    private void Update()
    {
        // The E press that opened the poster must not also confirm it.
        if (!IsOpen || Time.frameCount == _openedFrame) return;

        // The pause menu's Resume clears this flag while the poster is still up.
        Player.IsPaused = true;

        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;

        bool left = (keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame))
                    || (gamepad != null && (gamepad.dpad.left.wasPressedThisFrame || gamepad.leftStick.left.wasPressedThisFrame));
        bool right = (keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame))
                     || (gamepad != null && (gamepad.dpad.right.wasPressedThisFrame || gamepad.leftStick.right.wasPressedThisFrame));
        bool begin = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                     || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
        bool back = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                    || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

        if (back)
        {
            StepBack();
        }
        else if (begin)
        {
            Begin();
        }
        else if (left)
        {
            MoveCursor(-1);
        }
        else if (right)
        {
            MoveCursor(1);
        }
    }

    /// <summary>Moves to the next open act in that direction; locked acts are skipped.</summary>
    private void MoveCursor(int direction)
    {
        for (int act = _cursor + direction; act >= 0 && act < cards.Length; act += direction)
        {
            if (!ActProgress.IsUnlocked(act)) continue;

            _cursor = act;
            Refresh();
            return;
        }
    }

    private void Begin()
    {
        ActProgress.SelectAct(_cursor);
        Close();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.StartGame();
        }
        else
        {
            SceneManager.LoadScene(RunMapScene);
        }
    }

    /// <summary>Closes the poster without starting anything, so walking into it by accident never forces a run.</summary>
    private void StepBack()
    {
        Close();

        // Esc is also UIManager's pause key, and its input callback has already paused the
        // game by the time this Update runs. Undo that so Esc only closes the poster.
        if (Time.timeScale == 0f && UIManager.Instance != null)
        {
            UIManager.Instance.Resume();
        }
    }

    private void Refresh()
    {
        for (int act = 0; act < cards.Length; act++)
        {
            cards[act].Show(StateOf(act), ActProgress.GetScenesCleared(act), ActProgress.ScenesPerAct);
        }
    }

    private ActCard.State StateOf(int act)
    {
        if (!ActProgress.IsUnlocked(act)) return ActCard.State.Locked;
        if (act == _cursor) return ActCard.State.Selected;
        return ActProgress.IsFinished(act) ? ActCard.State.Finished : ActCard.State.Available;
    }
}
