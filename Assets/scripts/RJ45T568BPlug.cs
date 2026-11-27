using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// A grabbable RJ45 plug that only snaps onto the cable when the exposed wires
/// are placed left-to-right in the T568B sequence.
/// </summary>
public class RJ45T568BPlug : MonoBehaviour
{
    [SerializeField] private RJ45CablePhysics cable;
    [SerializeField] private BoxCollider insertionZone;
    [SerializeField] private Renderer statusLight;
    [SerializeField] private GameObject snapLatch;
    [SerializeField] private float slotPitch = 0.008f;
    [SerializeField] private float slotTolerance = 0.0035f;
    [SerializeField] private float verticalTolerance = 0.012f;
    [SerializeField] private float depthTolerance = 0.025f;

    private XRGrabInteractable grabInteractable;
    private bool installed;
    private Material statusMaterial;
    [SerializeField] private TextMesh statusMessage;

    private Vector3 installedLocalPosition;
    private Quaternion installedLocalRotation;

    
private string lastStatus = "Ułóż żyły w gnieździe";

    private static readonly string[] T568B =
    {
        "Bialo-Pomaranczowa",
        "Pomaranczowa",
        "Bialo-Zielona",
        "Niebieska",
        "Bialo-Niebieska",
        "Zielona",
        "Bialo-Brazowa",
        "Brazowa"
    };

    public bool IsInstalled => installed;
    public string LastStatus => lastStatus;

private void Awake()
{
    grabInteractable = GetComponent<XRGrabInteractable>();
    if (statusLight != null) statusMaterial = statusLight.material;
    if (statusMessage == null)
        statusMessage = GetComponentInChildren<TextMesh>(true);
    if (snapLatch != null) snapLatch.SetActive(false);
    SetStatus(Color.yellow, "Ułóż żyły w gnieździe");
}

private void LateUpdate()
    {
        if (!installed || cable == null)
            return;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = cable.transform.TransformPoint(installedLocalPosition);
            body.rotation = cable.transform.rotation * installedLocalRotation;
        }
    }


private void OnTriggerStay(Collider other)
{
    if (installed || cable == null || cable.wiresRoot == null) return;
    if (!other.transform.IsChildOf(cable.wiresRoot)) return;

    if (EvaluateOrder(out string message))
        Install();
    else
        SetStatus(message == "Ułóż wszystkie końcówki w gnieździe"
            ? new Color(1f, 0.65f, 0.1f, 1f) : Color.red,
            message == "Zła kolejność — T568B" ? "Źle poukładane — T568B" : message);
}

    /// <summary>Checks the current wire positions without installing the plug.</summary>
    public bool CheckWiringOrder()
    {
        bool valid = EvaluateOrder(out string message);
        SetStatus(valid ? Color.green : Color.red, message);
        return valid;
    }

private bool EvaluateOrder(out string message)
{
    message = "Ułóż wszystkie końcówki w gnieździe";
    if (insertionZone == null || cable == null || cable.wiresRoot == null) return false;

    List<Transform> wires = cable.wiresRoot.Cast<Transform>()
        .Where(w => w.name.StartsWith("Chwyt_") && w.childCount > 0).ToList();
    if (wires.Count != 8) { message = "W gnieździe musi być 8 żył"; return false; }

    var positioned = wires.Select(w =>
    {
        Transform tip = w.GetChild(w.childCount - 1);
        Vector3 local = insertionZone.transform.InverseTransformPoint(tip.position) - insertionZone.center;
        return new { wire = w, local };
    }).ToList();

    float halfWidth = insertionZone.size.x * .5f;
    float halfHeight = insertionZone.size.y * .5f;
    float halfDepth = insertionZone.size.z * .5f;
    if (positioned.Any(p => Mathf.Abs(p.local.x) > halfWidth ||
                            Mathf.Abs(p.local.y) > halfHeight ||
                            Mathf.Abs(p.local.z) > halfDepth))
        return false;

    var leftToRight = positioned.OrderBy(p => p.local.x).ToArray();
    for (int i = 0; i < leftToRight.Length; i++)
    {
        if (GetWireColor(leftToRight[i].wire.name) != T568B[i])
        { message = "Zła kolejność — T568B"; return false; }

        float expectedX = (i - 3.5f) * slotPitch;
        if (Mathf.Abs(leftToRight[i].local.x - expectedX) > slotTolerance ||
            Mathf.Abs(leftToRight[i].local.y) > verticalTolerance ||
            Mathf.Abs(leftToRight[i].local.z) > depthTolerance)
        { message = "Wyrównaj żyły do 8 pól"; return false; }
    }
    message = "Dobrze poukładane — T568B";
    return true;
}

    private static string GetWireColor(string wireName)
    {
        if (wireName.Contains("Bialo-Pomaranczowa")) return "Bialo-Pomaranczowa";
        if (wireName.Contains("Bialo-Zielona")) return "Bialo-Zielona";
        if (wireName.Contains("Bialo-Niebieska")) return "Bialo-Niebieska";
        if (wireName.Contains("Bialo-Brazowa")) return "Bialo-Brazowa";
        if (wireName.Contains("Pomaranczowa")) return "Pomaranczowa";
        if (wireName.Contains("Niebieska")) return "Niebieska";
        if (wireName.Contains("Zielona")) return "Zielona";
        if (wireName.Contains("Brazowa")) return "Brazowa";
        return string.Empty;
    }

private void Install()
{
    installed = true;
    SetStatus(Color.green, "Dobrze poukładane — RJ45 założony");

    Vector3 tipCenter = Vector3.zero;
    int count = 0;
    foreach (Transform wire in cable.wiresRoot)
    {
        if (!wire.name.StartsWith("Chwyt_") || wire.childCount == 0) continue;
        tipCenter += wire.GetChild(wire.childCount - 1).position;
        count++;
    }
    if (count > 0) transform.position = tipCenter / count - transform.forward * .015f;
    transform.SetParent(cable.transform, true);
    installedLocalPosition = cable.transform.InverseTransformPoint(transform.position);
    installedLocalRotation = Quaternion.Inverse(cable.transform.rotation) * transform.rotation;

    if (snapLatch != null) snapLatch.SetActive(true);
    if (grabInteractable != null) grabInteractable.enabled = false;
    foreach (Collider col in GetComponentsInChildren<Collider>(true)) col.enabled = false;

    Rigidbody plugBody = GetComponent<Rigidbody>();
    if (plugBody != null)
    {
        plugBody.isKinematic = true;
        plugBody.useGravity = false;
        plugBody.constraints = RigidbodyConstraints.FreezeAll;
    }

    // Po zaciśnięciu wtyku żyły przestają być osobnymi obiektami do chwytania.
    foreach (Transform wire in cable.wiresRoot)
    {
        var wireGrab = wire.GetComponent<XRGrabInteractable>();
        if (wireGrab != null) wireGrab.enabled = false;
        var wireBody = wire.GetComponent<Rigidbody>();
        if (wireBody != null)
        {
            wireBody.isKinematic = true;
            wireBody.useGravity = false;
            wireBody.linearVelocity = Vector3.zero;
            wireBody.angularVelocity = Vector3.zero;
        }
        foreach (Collider col in wire.GetComponentsInChildren<Collider>(true)) col.enabled = false;
    }
}

private void SetStatus(Color color, string message)
{
    lastStatus = message;
    if (statusMessage != null) statusMessage.text = message;
    if (statusMaterial != null)
    {
        statusMaterial.color = color;
        if (statusMaterial.HasProperty("_EmissionColor"))
            statusMaterial.SetColor("_EmissionColor", color * 1.5f);
    }
}
}