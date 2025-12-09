using TMPro;
using UnityEngine;

public class GuardStateBillboard3D : MonoBehaviour
{
    public TextMeshPro text;
    public Transform cameraTransform;

    void LateUpdate()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            return;
        }

        Vector3 toCamera = cameraTransform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(toCamera);
    }

    public void SetText(string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
