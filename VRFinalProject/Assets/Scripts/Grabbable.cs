using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    Rigidbody _rb;
    FixedJoint _joint;           // created when grabbed
    HandTracker _holder;         // which hand holds me

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void OnHover(bool on)
    {
        if (_holder) return;     // dont flash while held
    }

    public void Grab(HandTracker hand)
    {
        _rb.isKinematic = false;
        if (_holder) return;
        _holder = hand;

        // Snap to attach point
        transform.position = hand.attachPoint.position;
        transform.rotation = hand.attachPoint.rotation;

        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _joint = hand.gameObject.AddComponent<FixedJoint>();
        _joint.connectedBody = _rb;
        _joint.breakForce = Mathf.Infinity;
        _joint.breakTorque = Mathf.Infinity;
        _joint.enableCollision = false;  // avoid weird self-collisions
    }

    public void Release()
    {
        _rb.isKinematic = true;

        if (!_holder) return;

        // Remove joint first
        if (_holder && _joint)
        {
            Object.Destroy(_joint);
            _joint = null;
        }

        // Apply throw based on hand velocity
        _rb.linearVelocity = _holder.LinearVelocity;
        _rb.angularVelocity = _holder.AngularVelocity;

        _holder = null;
    }

    public bool IsHeld => _holder != null;
}
