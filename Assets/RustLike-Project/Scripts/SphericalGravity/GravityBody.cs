
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GravityBody : MonoBehaviour
{
    [Header("Gravity")]
    [Tooltip("The sphere to fall toward. Leave empty to auto-use the first GravityAttractor in the scene.")]
    public GravityAttractor attractor;

    [Tooltip("Use ordinary world gravity instead of gravity toward a sphere. Enable this in flat dungeon scenes.")]
    public bool useWorldGravity = false;

    [Header("Surface Alignment")]
    [Tooltip("Auto-rotate so this object's up points away from the sphere center.")]
    public bool alignToSurface = true;

    [Tooltip("How fast the object rotates to match the surface.")]
    public float alignSpeed = 50f;

    public Rigidbody Body { get; private set; }

    public GravityAttractor ActiveAttractor =>
        attractor != null ? attractor : GravityAttractor.Main;

    // World gravity uses the world's Y axis as up.
    // Spherical gravity uses the direction away from the sphere's center.
    public Vector3 UpDirection
    {
        get
        {
            GravityAttractor a = ActiveAttractor;

            if (useWorldGravity || a == null)
                return Vector3.up;

            return (transform.position - a.Center).normalized;
        }
    }

    // Gravity strength used by the player jump calculation.
    public float CurrentGravity
    {
        get
        {
            GravityAttractor a = ActiveAttractor;

            if (!useWorldGravity && a != null)
                return a.gravity;

            return Physics.gravity.magnitude;
        }
    }

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();

        // Gravity is applied manually in FixedUpdate.
        Body.useGravity = false;

        Body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        GravityAttractor a = ActiveAttractor;

        if (!useWorldGravity && a != null)
        {
            // Spherical gravity.
            Vector3 up = UpDirection;
            Body.AddForce(-up * a.gravity, ForceMode.Acceleration);

            if (alignToSurface)
            {
                Quaternion target =
                    Quaternion.FromToRotation(transform.up, up) * transform.rotation;

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    target,
                    alignSpeed * Time.fixedDeltaTime
                );
            }
        }
        else
        {
            // Ordinary world gravity for flat environments.
            Body.AddForce(Physics.gravity, ForceMode.Acceleration);
        }
    }
}