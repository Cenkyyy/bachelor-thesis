using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followSpeed = 10f;

    private void LateUpdate()
    {
        if (!target) 
            return;

        var pos = target.position;
        pos.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, pos, followSpeed * Time.deltaTime);
    }
}