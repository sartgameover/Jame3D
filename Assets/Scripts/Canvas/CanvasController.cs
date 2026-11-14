using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Висит на всегда активном объекте (Canvas). Esc открывает/закрывает паузу:
/// камера замирает сразу, игра плавно замедляется, меню плавно проявляется.
/// </summary>
public class CanvasController : MonoBehaviour
{
    public GameObject pause;                 // объект PAUSE (можно назначить в инспекторе)
    public KeyCode pauseKey = KeyCode.Escape;
    [Tooltip("Только в редакторе Unity: запасная клавиша паузы (Esc в редакторе сам отпускает курсор). None — выключить")]
    public KeyCode editorPauseKey = KeyCode.P;

    [Tooltip("За сколько секунд игра замедляется / разгоняется")]
    public float transitionTime = 0.35f;
    [Tooltip("За сколько секунд меню проявляется / исчезает (больше = плавнее)")]
    public float fadeTime = 0.6f;

    GameState gameState;
    CanvasGroup group;
    bool isPause = false;
    float scaleVelocity;
    float fade; // 0..1, прогресс появления меню

    void Awake()
    {
        // Find не находит выключенные объекты, поэтому лучше назначить pause в инспекторе
        if (pause == null) pause = GameObject.Find("PAUSE");
        if (pause == null)
        {
            Debug.LogError("CanvasController: объект PAUSE не найден — назначь его в инспекторе.");
            enabled = false;
            return;
        }

        gameState = pause.GetComponent<GameState>();
        gameState.Init();                    // грузит сенсу и вешает кнопки
        gameState.onContinue += () => SetPause(false);

        group = pause.GetComponent<CanvasGroup>();
        if (group == null) group = pause.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Time.timeScale = 1f;
        gameState.mouseLook.lookLocked = false;
        pause.SetActive(false);
    }

    void Update()
    {
        bool pressed = Input.GetKeyDown(pauseKey);
#if UNITY_EDITOR
        if (editorPauseKey != KeyCode.None && Input.GetKeyDown(editorPauseKey)) pressed = true;
#endif
        if (pressed)
            SetPause(!isPause);

        // замедление / разгон времени (unscaledDeltaTime — иначе замедление замедляет само себя)
        float target = isPause ? 0f : 1f;
        Time.timeScale = Mathf.SmoothDamp(Time.timeScale, target, ref scaleVelocity,
                                          transitionTime, Mathf.Infinity, Time.unscaledDeltaTime);
        if (Mathf.Abs(Time.timeScale - target) < 0.005f)
        {
            Time.timeScale = target;
            scaleVelocity = 0f;
        }

        // меню проявляется отдельно от времени, мягкой кривой (медленный старт и финиш)
        fade = Mathf.MoveTowards(fade, isPause ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
        group.alpha = Mathf.SmoothStep(0f, 1f, fade);

        if (!isPause && fade <= 0f && pause.activeSelf)
            pause.SetActive(false);
    }

    // LateUpdate — чтобы перебить всё, что другие скрипты/редактор сделали с курсором в этом кадре.
    // Пока игра идёт, курсор ВСЕГДА заблокирован и скрыт (проверяем и lockState, и visible).
    // Рывок камеры при возврате курсора гасит сам MouseLook (пропускает первые кадры).
    void LateUpdate()
    {
        if (!isPause && (Cursor.lockState != CursorLockMode.Locked || Cursor.visible))
            SetCursorFree(false);
    }

    void SetPause(bool value)
    {
        if (isPause == value) return;
        isPause = value;

        if (value) pause.SetActive(true);

        // камера замирает сразу при нажатии, а при закрытии разгоняется вместе со временем
        gameState.mouseLook.lookLocked = value;
        if (!value) gameState.mouseLook.SkipLook(); // курсор вернётся в центр — не даём дельте дёрнуть камеру

        group.interactable = value;
        group.blocksRaycasts = value;

        // Сбрасываем выбор в UI: иначе после Esc кнопка/слайдер остаются «в фокусе»
        // и реагируют на стрелки/Enter/пробел прямо во время игры.
        if (!value && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        SetCursorFree(value);
    }

    static void SetCursorFree(bool free)
    {
        Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = free;
    }

    // вернулись в окно игры — снова захватываем курсор, если не на паузе
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !isPause)
        {
            if (gameState != null && gameState.mouseLook != null) gameState.mouseLook.SkipLook();
            SetCursorFree(false);
        }
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}