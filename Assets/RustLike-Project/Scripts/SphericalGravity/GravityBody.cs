using UnityEngine;

// Put this on anything that should fall toward the sphere - the player, props,
// pickups, enemies. It replaces Unity's built-in gravity with a pull toward the
// attractor's center, and (optionally) rotates the object so its feet point at
// the surface.
[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    [Tooltip("The sphere to fall toward. Leave empty to auto-use the first GravityAttractor in the scene.")]
    public GravityAttractor attractor;

    [Tooltip("Auto-rotate so this object's up points away from the sphere center. Turn OFF for the player - FirstPersonController handles its own rotation.")]
    public bool alignToSurface = true;

    [Tooltip("How fast the object rotates to match the surface (only used when Align To Surface is on).")]
    public float alignSpeed = 50f;

    public Rigidbody Body { get; private set; }

    // The attractor actually in use (the assigned one, or the scene default).
    public GravityAttractor ActiveAttractor => attractor != null ? attractor : GravityAttractor.Main;

    // Direction considered "up" for this body right now (straight away from the center).
    public Vector3 UpDirection
    {
        get
        {
            GravityAttractor a = ActiveAttractor;
            return a != null ? (transform.position - a.Center).normalized : transform.up;
        }
    }

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Body.constraints = RigidbodyConstraints.FreezeRotation; // no tumbling from collisions; rotation is set manually
    }

    private void FixedUpdate()
    {
        GravityAttractor a = ActiveAttractor;
        if (a == null) return;

        Vector3 up = UpDirection;

        // Pull toward the center. Acceleration mode is mass-independent, like real gravity.
        Body.AddForce(-up * a.gravity, ForceMode.Acceleration);

        if (alignToSurface)
        {
            Quaternion target = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, target, alignSpeed * Time.fixedDeltaTime);
        }
    }
}
