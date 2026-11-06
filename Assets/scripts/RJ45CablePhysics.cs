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
        }

        for (int i = 0; i < all.Count; i++)
            for (int j = i + 1; j < all.Count; j++)
                if (all[i] != null && all[j] != null)
                    Physics.IgnoreCollision(all[i], all[j], true);
    }
}
