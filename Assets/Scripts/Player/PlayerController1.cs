using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public CharacterController cc;
    Camera mainCamera;

    public bool isMoving;
    public int speed;
    public int speedShift; // мы плюсуем, а не заново добовляем!!!
    public int speedZoomRun;

    public float jumpHeight = 1.5f; // это в метры (;
    public float gravity = -9.81f;
    public float fallMultiplier;

    private float velocityY;
    void Start()
    {
        cc = GetComponent<CharacterController>();
        mainCamera = Camera.main;
    }
    void Update()
    {
        // ПРЫЖОК
        bool isGrounded = cc.isGrounded;
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            velocityY = Mathf.Sqrt(jumpHeight * -2 * gravity);


        // ХОДЬБА
        if (isGrounded && velocityY < 0)
        {
            velocityY = -2; // небольшой минус чтобы контроллер плотно прижимался к земле
        }
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        isMoving = moveX != 0 || moveZ != 0;

        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;
        cc.Move(move * speed * Time.deltaTime);


        // ГРАВИТАЦИЯ
        if (velocityY < 0)
            velocityY += gravity * fallMultiplier * Time.deltaTime;
        else
            velocityY += gravity * Time.deltaTime;
        cc.Move(Vector3.up * velocityY * Time.deltaTime);


        // БЕГ
        float targetFOV = (Input.GetKey(KeyCode.LeftShift) && (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)) ? 70 : 65;
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.deltaTime * speedZoomRun);
        
        if (Input.GetKeyDown(KeyCode.LeftShift))
            speed += speedShift;
        else if (Input.GetKeyUp(KeyCode.LeftShift))
            speed -= speedShift;
    }
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb.isKinematic)
            return;
        
        if (hit.moveDirection.y < -0.3f) // НЕ толкаем предмет вниз
            return;

        Vector3 pushDirection = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        rb.AddForce(pushDirection * 2, ForceMode.Impulse);
    }
}
