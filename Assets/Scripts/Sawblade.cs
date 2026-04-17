using UnityEngine;

public class UniversalSawblade : MonoBehaviour
{
    public enum MovementAxis { X, Z }

    [Header("Axis Configuration")]
    [SerializeField] private MovementAxis moveAlong = MovementAxis.Z;

    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 500f;
    [SerializeField] private float movementSpeed = 5f;
    
    [Header("Vertical (Y) Settings")]
    [SerializeField] private float upY = 5f;
    [SerializeField] private float downY = 0f;
    
    [Header("Horizontal Settings")]
    [SerializeField] private float startPos = 0f;
    [SerializeField] private float maxPos = 20f;

    private enum BladeState { Dropping, MovingForward, Rising }
    [SerializeField] private BladeState currentState = BladeState.Dropping; // Serialized so you can watch it in Inspector

    void Start()
    {
        ResetToStart();
    }

    void Update()
    {
        RotateBlade();
        HandleStateMachine();
    }

    private void RotateBlade()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    private void HandleStateMachine()
    {
        Vector3 pos = transform.position;
        float step = movementSpeed * Time.deltaTime;

        switch (currentState)
        {
            case BladeState.Dropping:
                pos.y = Mathf.MoveTowards(pos.y, downY, step);
                if (Mathf.Abs(pos.y - downY) < 0.001f)
                {
                    pos.y = downY;
                    currentState = BladeState.MovingForward;
                }
                break;

            case BladeState.MovingForward:
                if (moveAlong == MovementAxis.X)
                    pos.x = Mathf.MoveTowards(pos.x, maxPos, step);
                else
                    pos.z = Mathf.MoveTowards(pos.z, maxPos, step);

                float currentH = (moveAlong == MovementAxis.X) ? pos.x : pos.z;
                if (Mathf.Abs(currentH - maxPos) < 0.001f)
                {
                    // Snap to exact max position
                    if (moveAlong == MovementAxis.X) pos.x = maxPos; else pos.z = maxPos;
                    currentState = BladeState.Rising;
                }
                break;

            case BladeState.Rising:
                pos.y = Mathf.MoveTowards(pos.y, upY, step);
                if (Mathf.Abs(pos.y - upY) < 0.001f)
                {
                    // This is where the reset happens
                    ResetToStart();
                    return; // Exit early to avoid setting position twice
                }
                break;
        }

        transform.position = pos;
    }

    private void ResetToStart()
    {
        Vector3 newPos = transform.position;
        newPos.y = upY;
        
        if (moveAlong == MovementAxis.X) newPos.x = startPos;
        else newPos.z = startPos;
        
        transform.position = newPos;
        currentState = BladeState.Dropping;
    }
}