using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        Vector3 direction = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            direction += transform.forward;        // Forward

        if (Keyboard.current.sKey.isPressed)
            direction += -transform.forward;       // Back

        if (Keyboard.current.aKey.isPressed)
            direction += -transform.right;         // Left

        if (Keyboard.current.dKey.isPressed)
            direction += transform.right;          // Right

        // Normalize to avoid faster diagonal movement
        direction = direction.normalized;

        transform.Translate(direction * moveSpeed * Time.deltaTime, Space.World);
    }
}
