using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Висит на объекте PAUSE (панель меню): кнопки и слайдер сенсы.
/// Паузой (Esc, время, курсор) управляет CanvasController, а сенсу отдаёт в MouseLook этот скрипт.
/// </summary>
public class GameState : MonoBehaviour
{
    public Button btnContinue;
    public Slider sldrSensitivity;
    public MouseLook mouseLook;
    public Button btnQuitInMenu;
    public int mainMenuSceneIndex = 0;

    // CanvasController подписывается и закрывает паузу
    public event Action onContinue;

    const string PrefKey = "MouseSensitivity";
    bool inited;

    void Awake()
    {
        Init();
    }

    // Можно вызывать сколько угодно раз — выполнится один раз.
    // CanvasController вызывает явно, т.к. PAUSE у него выключен и Start бы не сработал вовремя.
    public void Init()
    {
        if (inited) return;
        inited = true;

        if (mouseLook == null)
            mouseLook = FindObjectOfType<MouseLook>(true);
        if (mouseLook == null)
        {
            Debug.LogError("GameState: MouseLook не назначен и не найден на сцене.");
            return;
        }

        float saved = PlayerPrefs.GetFloat(PrefKey, mouseLook.mouseSensitivity);
        mouseLook.SetSensitivity(saved);

        if (sldrSensitivity != null)
        {
            sldrSensitivity.SetValueWithoutNotify(saved);
            sldrSensitivity.onValueChanged.AddListener(OnSensitivityChanged);
        }

        if (btnContinue != null) btnContinue.onClick.AddListener(OnContinueClicked);
        if (btnQuitInMenu != null) btnQuitInMenu.onClick.AddListener(QuitInMenu);
    }

    void OnContinueClicked()
    {
        onContinue?.Invoke();
    }

    void OnSensitivityChanged(float value)
    {
        mouseLook.SetSensitivity(value);
        PlayerPrefs.SetFloat(PrefKey, value);
    }

    // сохраняем один раз, когда меню закрывается, а не на каждое движение слайдера
    void OnDisable()
    {
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        if (sldrSensitivity != null) sldrSensitivity.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (btnContinue != null) btnContinue.onClick.RemoveListener(OnContinueClicked);
        if (btnQuitInMenu != null) btnQuitInMenu.onClick.RemoveListener(QuitInMenu);
    }

    void QuitInMenu()
    {
        // ВАЖНО: вернуть время и курсор, иначе главное меню останется «замороженным»
        Time.timeScale = 1f;
        PlayerPrefs.Save();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}