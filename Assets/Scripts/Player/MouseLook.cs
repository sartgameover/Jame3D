using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 1000;
    [HideInInspector] public bool lookLocked; // true — камера не крутится (ставит CanvasController при паузе)

    [Tooltip("Сколько кадров игнорировать мышь после возврата курсора в игру (убирает рывок камеры)")]
    public int skipFramesAfterUnlock = 3;

    GameObject player;
    PlayerController playerController;
    float xRotation;
    int skipFrames;
    bool wasBlocked = true;

    // БОБИКИ! Тряска камеры
    float bobikTimer;
    Vector3 startLocalPosition;
    public float bobikSpeed;
    public float bobikPlusSpeed; // он прибавляет а не заменяет!!!
    public float AmplitudeX = 0.015f;
    public float AmplitudeY = 0.05f;
    public float AmplitudeXRun = 0.017f;
    public float AmplitudeYRun = 0.07f;
    public float bobikSmoothing;

    void Start()
    {
        SetCursorLocked(true);

        player = GameObject.Find("Player");
        playerController = player.GetComponent<PlayerController>();

        startLocalPosition = transform.localPosition;
        xRotation = NormalizeAngle(transform.localEulerAngles.x); // не сбрасываем наклон камеры в 0
        SkipLook();
    }

    void Update()
    {
        // Мышь «заблокирована», если пауза ИЛИ курсор не захвачен (Esc в редакторе, alt-tab, меню).
        // В момент, когда блок снимается, курсор прыгает в центр экрана и даёт огромную дельту —
        // из-за неё камера дёргалась. Поэтому после разблокировки пропускаем несколько кадров.
        bool blocked = lookLocked || Cursor.lockState != CursorLockMode.Locked;
        if (wasBlocked && !blocked) skipFrames = skipFramesAfterUnlock;
        wasBlocked = blocked;

        float mouseX = 0f, mouseY = 0f;
        if (blocked)
        {
            // ничего не крутим
        }
        else if (skipFrames > 0)
        {
            skipFrames--; // глотаем «прыжок» дельты
        }
        else
        {
            // Time.timeScale как множитель: когда игра плавно замедляется, камера тоже плавно тормозит.
            // unscaledDeltaTime — чтобы сенса не зависела от замедления.
            float k = Time.timeScale * Time.unscaledDeltaTime * mouseSensitivity;
            mouseX = Input.GetAxisRaw("Mouse X") * k;
            mouseY = Input.GetAxisRaw("Mouse Y") * k;
        }

        player.transform.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -89, 89);

        transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        if (playerController.isMoving)
        {
            float bobikX = 0;
            float bobikY = 0;
            if (playerController.cc.isGrounded)
            {
                // TODO: это условие никогда не true (speed > speed + shift) — подставь свою проверку бега
                if (playerController.speed > playerController.speed + playerController.speedShift)
                {
                    bobikTimer += Time.deltaTime * (bobikSpeed + bobikPlusSpeed);
                    bobikX = Mathf.Cos(bobikTimer) * AmplitudeXRun;
                    bobikY = Mathf.Sin(bobikTimer) * AmplitudeYRun;
                }
                else
                {
                    bobikTimer += Time.deltaTime * bobikSpeed;
                    bobikX = Mathf.Cos(bobikTimer) * AmplitudeX;
                    bobikY = Mathf.Sin(bobikTimer) * AmplitudeY;
                }
            }
            transform.localPosition = startLocalPosition + new Vector3(bobikX, bobikY, 0);
        }
        else
        {
            bobikTimer = 0;
            transform.localPosition = Vector3.Lerp(transform.localPosition, startLocalPosition, Time.deltaTime * bobikSmoothing);
        }
    }

    // Можно вызвать из любого скрипта перед возвратом курсора в игру
    public void SkipLook()
    {
        skipFrames = skipFramesAfterUnlock;
    }

    // Вызывается из GameState
    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    static float NormalizeAngle(float a)
    {
        return a > 180f ? a - 360f : a;
    }
}