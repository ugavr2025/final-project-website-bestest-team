using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class HandGrabber : MonoBehaviour
{
    [Header("Existing")]
    public HandTracker hand;
    public InputActionProperty gripAction; // bind to LeftGrip/RightGrip from actions
    [Range(0f, 1f)] public float gripThreshold = 0.5f;
    public XRNode xrNode; // assign LeftHand or RightHand in Inspector

    [Header("Impact Haptics")]
    [Tooltip("Layers that should cause impact haptics (Default, Environment, Grabbable).")]
    public LayerMask hapticLayers = ~0;
    [Tooltip("Minimum hand/controller speed to buzz when contacting colliders.")]
    public float minImpactSpeed = 0.25f;
    [Tooltip("Speed to full amplitude.")]
    public float maxImpactSpeed = 3.0f;
    [Tooltip("Cooldown to prevent rapid spam")]
    public float impactCooldown = 0.10f;
    [Tooltip("Base duration for pulse.")]
    public float impactBaseDuration = 0.05f;
    [Range(0.05f, 1f)] public float impactAmplitudeScale = 1f;

    Grabbable _hovered;
    Grabbable _held;

    float _lastImpactTime = -999f;

    void OnEnable() { gripAction.action.Enable(); }
    void OnDisable() { gripAction.action.Disable(); }

    void Update()
    {
        // Hover detection via raycast
        if (!_held)
        {
            if (hand.Raycast(out var hit))
            {
                var g = hit.collider.GetComponentInParent<Grabbable>();
                if (g != _hovered)
                {
                    if (_hovered) _hovered.OnHover(false);
                    _hovered = g;
                    if (_hovered)
                    {
                        _hovered.OnHover(true);
                        Pulse(0.05f, 0.03f); // small tick when highlighting a grabbable
                    }
                }
            }
            else
            {
                if (_hovered) _hovered.OnHover(false);
                _hovered = null;
            }
        }

        // Grip pressed?
        float grip = gripAction.action.ReadValue<float>();
        bool wantGrab = grip > gripThreshold;

        if (wantGrab && _held == null && _hovered != null)
        {
            _held = _hovered;
            _held.Grab(hand);
            Pulse(0.25f, 0.05f); // stronger pulse when grabbing
        }
        else if (!wantGrab && _held != null)
        {
            _held.Release();
            Pulse(0.15f, 0.04f); // short pulse on release
            _held = null;
        }
    }

    // Impact haptics
    void OnTriggerEnter(Collider other)
    {
        if (!IsInHapticLayers(other.gameObject.layer)) return;
        TryImpactPulseFromVelocity();
    }

    void OnCollisionEnter(Collision collision)
    {
        // If using non-trigger colliders, can also map relative velocity here
        if (!IsInHapticLayers(collision.gameObject.layer)) return;

        // Prefer device velocity but fall back to relative velocity
        if (!TryImpactPulseFromVelocity())
        {
            float rel = collision.relativeVelocity.magnitude;
            TryImpactPulse(rel);
        }
    }

    bool TryImpactPulseFromVelocity()
    {
        var dev = InputDevices.GetDeviceAtXRNode(xrNode);
        if (dev.isValid && dev.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 v))
        {
            return TryImpactPulse(v.magnitude);
        }
        return false;
    }

    bool TryImpactPulse(float speed)
    {
        if (Time.time - _lastImpactTime < impactCooldown) return false;
        if (speed < minImpactSpeed) return false;

        float t = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, speed);
        float amp = Mathf.Clamp01(t) * impactAmplitudeScale;
        float dur = Mathf.Lerp(impactBaseDuration * 0.7f, impactBaseDuration * 1.3f, t);
        Pulse(amp, dur);
        _lastImpactTime = Time.time;
        return true;
    }

    bool IsInHapticLayers(int layer) => (hapticLayers.value & (1 << layer)) != 0;

    // Simple local haptic pulse
    void Pulse(float amplitude, float duration)
    {
        var device = InputDevices.GetDeviceAtXRNode(xrNode);
        if (device.isValid &&
            device.TryGetHapticCapabilities(out var caps) &&
            caps.supportsImpulse)
        {
            device.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, duration));
        }
    }
}
