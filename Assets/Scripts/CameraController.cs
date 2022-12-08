using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector2 lookSensitivity = Vector2.one;
    [SerializeField] private float speed = 1;

    Vector2 input;
    Vector3 lookAngle = Vector3.zero;

    private void Update()
    {
        input = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));

        float rotationY = Input.GetAxis("Mouse Y") * lookSensitivity.x;
        float rotationX = Input.GetAxis("Mouse X") * lookSensitivity.y;
        
        if(rotationY > 0)
        {
            lookAngle = new Vector3(Mathf.MoveTowards(lookAngle.x, -80, rotationY), lookAngle.y + rotationX, 0);
        }
        else
        {
            lookAngle = new Vector3(Mathf.MoveTowards(lookAngle.x, 80, -rotationY), lookAngle.y + rotationX, 0);
        }

        transform.localEulerAngles = lookAngle;
    }
    
    private void FixedUpdate()
    {
        Vector3 moveAmount = input.x * transform.right + input.y * transform.forward;
        transform.position += moveAmount * speed * Time.deltaTime;
    }
}
