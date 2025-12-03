using UnityEngine;

public class HandTracker : MonoBehaviour
{
    [Header("Refs")]
    public Transform attachPoint;          // child transform where objects mount
    public bool isLeftHand = true;         // set in inspector

    [Header("Line Settings")]
    public float rayLength = 3f;
    public LayerMask hittableMask = ~0;    // everything by default

    // public for other scripts
    public Vector3 LinearVelocity { get; private set; }
    public Vector3 AngularVelocity { get; private set; }

    Vector3 _prevPos;
    Quaternion _prevRot;

    void Start()
    {
        _prevPos = transform.position;
        _prevRot = transform.rotation;
    }

    void Update()
    {
        // Velocities (simple finite difference)
        var dt = Mathf.Max(Time.deltaTime, 1e-5f);
        LinearVelocity = (transform.position - _prevPos) / dt;

        // Angular velocity approx
        Quaternion dq = transform.rotation * Quaternion.Inverse(_prevRot);
        dq.ToAngleAxis(out float angleDeg, out Vector3 axis);
        if (angleDeg > 180f) angleDeg -= 360f;
        AngularVelocity = axis * (angleDeg * Mathf.Deg2Rad / dt);

        _prevPos = transform.position;
        _prevRot = transform.rotation;

    }

    // Simple ray utility
    public bool Raycast(out RaycastHit hit) =>
        Physics.Raycast(transform.position, transform.forward, out hit, rayLength, hittableMask, QueryTriggerInteraction.Ignore);
}