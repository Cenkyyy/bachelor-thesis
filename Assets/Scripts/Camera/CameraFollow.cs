using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _followSpeed = 10f;

    private void LateUpdate()
    {
        if (!_target) 
            return;

        var pos = _target.position;
        pos.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, pos, _followSpeed * Time.deltaTime);
    }
}
