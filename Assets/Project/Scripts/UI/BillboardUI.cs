using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        transform.forward = mainCamera.transform.forward;
    }
}