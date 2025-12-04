using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    Rigidbody _rb;
    FixedJoint _joint;
    HandTracker _holder;
    MonoBehaviour _networkTransform;
    NetworkObject _networkObject;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _networkTransform = GetComponent("ClientNetworkTransform") as MonoBehaviour;
        _networkObject = GetComponent<NetworkObject>();
    }

    public void OnHover(bool on)
    {
        if (_holder) return;     // dont flash while held
    }

    public void Grab(HandTracker hand)
    {
        if (_holder) return;
        
        if (hand.attachPoint == null)
        {
            Debug.LogError($"[Grabbable] HandTracker on {hand.gameObject.name} has no attach point assigned! Cannot grab {gameObject.name}");
            return;
        }

        Debug.Log($"[Grabbable] Grabbing {gameObject.name} with {hand.gameObject.name}, attach point: {hand.attachPoint.name}");
        Debug.Log($"[Grabbable] Current position: {transform.position}, Target position: {hand.attachPoint.position}");

        _holder = hand;

        if (_networkObject != null && _networkObject.IsSpawned)
        {
            Debug.Log($"[Grabbable] Network object - IsOwner: {_networkObject.IsOwner}, OwnerClientId: {_networkObject.OwnerClientId}");
            
            if (!_networkObject.IsOwner)
            {
                Debug.LogWarning($"[Grabbable] Not owner of {gameObject.name}, ownership transfer should happen via TransferOwnershipOnSelect component");
            }
        }

        if (_networkTransform != null)
        {
            Debug.Log($"[Grabbable] Disabling ClientNetworkTransform on {gameObject.name}");
            _networkTransform.enabled = false;
        }

        StartCoroutine(GrabSequence(hand));
    }

    IEnumerator GrabSequence(HandTracker hand)
    {
        yield return new WaitForFixedUpdate();

        transform.position = hand.attachPoint.position;
        transform.rotation = hand.attachPoint.rotation;

        Debug.Log($"[Grabbable] Snapped to position: {transform.position}");

        _joint = hand.gameObject.AddComponent<FixedJoint>();
        _joint.connectedBody = _rb;
        _joint.breakForce = Mathf.Infinity;
        _joint.breakTorque = Mathf.Infinity;
        _joint.enableCollision = false;
        
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        Debug.Log($"[Grabbable] Created FixedJoint on {hand.gameObject.name} connected to {gameObject.name}");
    }

    public void Release()
    {
        if (!_holder) return;

        if (_holder && _joint)
        {
            Object.Destroy(_joint);
            _joint = null;
        }

        _rb.linearVelocity = _holder.LinearVelocity;
        _rb.angularVelocity = _holder.AngularVelocity;

        _holder = null;

        if (_networkTransform != null)
        {
            _networkTransform.enabled = true;
        }

        _rb.isKinematic = true;
    }

    public bool IsHeld => _holder != null;
}
