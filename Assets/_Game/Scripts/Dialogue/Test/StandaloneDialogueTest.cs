using System.Collections;
using UnityEngine;

public class StandaloneDialogueTest : MonoBehaviour
{
    [Header("Configuración de Prueba")]
    [SerializeField] private string testConversationStartId = "CONV_LOBBY_01";
    [SerializeField] private float simulatedPrintDuration = 0.8f;

    [Header("Opcional (Para probar Bark -> Conversation)")]
    [SerializeField] private DialogueSpeaker optionalSpeaker;

    private string _currentSpeaker = "-";
    private string _currentFullText = "";
    private string _visibleText = "";
    private float _skipProgress;
    private Coroutine _printSimulationCoroutine;

    private void Start()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[StandaloneTest] Falta DialogueManager en la escena.");
            return;
        }

        DialogueManager.Instance.OnLineStarted += HandleLineStarted;
        DialogueManager.Instance.OnCompleteLineInstantly += HandleCompleteInstant;
        DialogueManager.Instance.OnSkipProgressChanged += progress => _skipProgress = progress;
        DialogueManager.Instance.OnConversationEnding += () => Debug.Log("[TEST] Subiendo carteles (END)...");
        DialogueManager.Instance.OnConversationEnded += HandleConversationEnded;
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance == null) return;

        DialogueManager.Instance.OnLineStarted -= HandleLineStarted;
        DialogueManager.Instance.OnCompleteLineInstantly -= HandleCompleteInstant;
        DialogueManager.Instance.OnConversationEnded -= HandleConversationEnded;
    }

    private void Update()
    {
        if (DialogueManager.Instance == null || GameManager.Instance == null) return;

        // P: Alternar Pausa (comprueba que el diálogo y el Skip se congelan)
        if (Input.GetKeyDown(KeyCode.P))
        {
            bool shouldPause = GameManager.Instance.CurrentGameState != GameState.Paused;
            GameManager.Instance.SetPauseState(shouldPause);
            Debug.Log($"[TEST] Pausa: {shouldPause}");
        }

        if (GameManager.Instance.CurrentGameState == GameState.Paused) return;

        // E o Clic Izquierdo: Iniciar conversación o Avanzar
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            if (DialogueManager.Instance.IsConversationActive)
            {
                DialogueManager.Instance.OnAdvanceInput();
                DialogueManager.Instance.OnConfirmChoiceInput();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                StartTestConversation();
            }
        }

        // Tab (Mantener 1s): Skip de toda la conversación
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            DialogueManager.Instance.OnSkipInputStarted();
        }
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            DialogueManager.Instance.OnSkipInputCanceled();
        }

        // B: Forzar un Bark en el NPC opcional
        if (Input.GetKeyDown(KeyCode.B) && optionalSpeaker != null)
        {
            optionalSpeaker.TriggerBark();
        }

        // R: Simular salida de habitación (corta cualquier Bark activo)
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[TEST] Simulando salida de sala...");
            GameManager.Instance.NotifyRoomExited();
        }

        if (DialogueManager.Instance.IsConversationActive)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                DialogueManager.Instance.OnNavigationChoiceInput(-1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                DialogueManager.Instance.OnNavigationChoiceInput(1);
        }
    }

    private void StartTestConversation()
    {
        if (optionalSpeaker != null)
        {
            optionalSpeaker.InterruptBark();
        }

        DialogueManager.Instance.StartConversation(testConversationStartId);
    }

    private void HandleLineStarted(DialogueEntry entry, string text)
    {
        _currentSpeaker = entry.speaker.ToString();
        _currentFullText = text;
        _visibleText = "";

        Debug.Log($"[TEST] Nueva línea ({entry.id}) - {_currentSpeaker}: \"{text}\"");
    }

    /*
    private IEnumerator SimulatePrintingRoutine()
    {
        // Esperamos a que termine el Estado A (Dropping) y entre en Estado B (Printing)
        yield return new WaitUntil(() =>
            DialogueManager.Instance.CurrentState == DialogueManager.ConversationState.Printing ||
            !DialogueManager.Instance.IsConversationActive);

        if (!DialogueManager.Instance.IsConversationActive) yield break;

        float elapsed = 0f;
        while (elapsed < simulatedPrintDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / simulatedPrintDuration);
            int charCount = Mathf.FloorToInt(t * _currentFullText.Length);
            _visibleText = _currentFullText.Substring(0, charCount);
            yield return null;
        }

        _visibleText = _currentFullText;
        DialogueManager.Instance.NotifyTextFinishedPrinting();
    }*/

    private void HandleCompleteInstant()
    {
        if (_printSimulationCoroutine != null) StopCoroutine(_printSimulationCoroutine);
        _visibleText = _currentFullText;
        Debug.Log("[TEST] Texto completado al instante (Estado B -> C).");
    }

    private void HandleConversationEnded()
    {
        if (_printSimulationCoroutine != null) StopCoroutine(_printSimulationCoroutine);
        _currentSpeaker = "-";
        _currentFullText = "";
        _visibleText = "";
        _skipProgress = 0f;
        Debug.Log("[TEST] Conversación terminada. Estado global vuelto a Gameplay.");
    }

    // Panel visual minimalista en esquina superior izquierda (cero dependencias de UI)
    private void OnGUI()
    {
        if (DialogueManager.Instance == null || GameManager.Instance == null) return;

        GUILayout.BeginArea(new Rect(15, 15, 460, 210), GUI.skin.box);
        GUILayout.Label($"<b>GameState:</b> {GameManager.Instance.CurrentGameState} | <b>ConvState:</b> {DialogueManager.Instance.CurrentState}");
        GUILayout.Label($"<b>Speaker:</b> {_currentSpeaker}");
        GUILayout.Label($"<b>Texto:</b> {_visibleText}");
        GUILayout.Label($"<b>Skip Hold:</b> {Mathf.RoundToInt(_skipProgress * 100)}%");
        GUILayout.Space(8);
        GUILayout.Label("<b>Controles:</b> [E] Iniciar/Avanzar | [Click] Avanzar | [Mantener Tab] Skip");
        GUILayout.Label("[B] Disparar Bark | [R] Salir de sala | [P] Pausa");
        GUILayout.EndArea();
    }
}