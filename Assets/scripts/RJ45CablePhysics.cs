using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fizyka kabla RJ45: żyły są łańcuchami Rigidbody poza hierarchią kabla (wiresRoot).
/// W Awake wyłącza kolizje między elementami samego kabla (płaszcz i żyły), żeby się nie blokowały,
/// a dla segmentów ustawia parametry solvera stabilizujące cienkie łańcuchy.
/// </summary>
[DefaultExecutionOrder(-50)]
public class RJ45CablePhysics : MonoBehaviour
{
    [Tooltip("Obiekt nadrzędny segmentów żył (Rigidbody + Collider + ConfigurableJoint).")]
    public Transform wiresRoot;

    [Header("Parametry segmentów")]
    public float linearDamping = 0.8f;
    public float angularDamping = 4f;
    public int solverIterations = 20;
    public int solverVelocityIterations = 6;
    public float maxDepenetrationVelocity = 1.5f;

    private readonly Dictionary<Transform, Vector3> wireLocalPositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Quaternion> wireLocalRotations = new Dictionary<Transform, Quaternion>();

    
void Awake()
    {
        var all = new List<Collider>(GetComponentsInChildren<Collider>(true));
        if (wiresRoot != null)
        {
            all.AddRange(wiresRoot.GetComponentsInChildren<Collider>(true));
            foreach (var rb in wiresRoot.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.linearDamping = linearDamping;
                rb.angularDamping = angularDamping;
                rb.solverIterations = solverIterations;
                rb.solverVelocityIterations = solverVelocityIterations;
                rb.maxDepenetrationVelocity = maxDepenetrationVelocity;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            foreach (Transform wire in wiresRoot)
            {
                wireLocalPositions[wire] = transform.InverseTransformPoint(wire.position);
                wireLocalRotations[wire] = Quaternion.Inverse(transform.rotation) * wire.rotation;
            }
        }

        for (int i = 0; i < all.Count; i++)
            for (int j = i + 1; j < all.Count; j++)
                if (all[i] != null && all[j] != null)
                    Physics.IgnoreCollision(all[i], all[j], true);
    }

void LateUpdate()
    {
        if (wiresRoot == null)
            return;

        foreach (Transform wire in wiresRoot)
        {
            var grab = wire.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var body = wire.GetComponent<Rigidbody>();
            if (body == null)
                continue;

            if (grab != null && grab.isSelected)
            {
                // Store the moved wire relative to the jacket so it stays where the hand leaves it.
                wireLocalPositions[wire] = transform.InverseTransformPoint(wire.position);
                wireLocalRotations[wire] = Quaternion.Inverse(transform.rotation) * wire.rotation;
                continue;
            }

            if (!wireLocalPositions.TryGetValue(wire, out var localPosition))
            {
                localPosition = transform.InverseTransformPoint(wire.position);
                wireLocalPositions[wire] = localPosition;
            }
            if (!wireLocalRotations.TryGetValue(wire, out var localRotation))
            {
                localRotation = Quaternion.Inverse(transform.rotation) * wire.rotation;
                wireLocalRotations[wire] = localRotation;
            }

            // Keep each unheld kinematic wire Rigidbody synchronized with the moving jacket Rigidbody.
            body.position = transform.TransformPoint(localPosition);
            body.rotation = transform.rotation * localRotation;
        }
    }

}
