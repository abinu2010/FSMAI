using UnityEngine;

public class PlayerLook3D : MonoBehaviour
{
    public Transform cameraTransform;
    public float mouseSensitivity = 150f;
    public float maxPitch = 80f;
    float pitch;
    bool cursorLocked;
    void Awake()
    {
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
        }
        LockCursor(true);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(false);
        }

        if (!cursorLocked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                LockCursor(true);
            }
            return;
        }
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;
        if (Mathf.Abs(mouseX) > 0.0001f)
        {
            transform.Rotate(Vector3.up * mouseX);
        }
        if (cameraTransform != null)
        {
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
    void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
