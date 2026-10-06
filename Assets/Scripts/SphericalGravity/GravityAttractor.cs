using UnityEngine;

// Put this on your giant sphere. It's the thing everything falls toward.
// Any object with a GravityBody will be pulled toward this sphere's center.
public class GravityAttractor : MonoBehaviour
{
    [Tooltip("Acceleration (units/sec^2) pulling bodies toward this sphere's center. ~9.81 is Earth-like; higher feels snappier.")]
    public float gravity = 20f;

    // Convenience default so a GravityBody with no attractor assigned can still
    // find one: the first attractor to wake up becomes the fallback.
    public static GravityAttractor Main { get; private set; }

    public Vector3 Center => transform.position;

    private void Awake()
    {
        if (Main == null) Main = this;
    }

    private void OnDestroy()
    {
        if (Main == this) Main = null;
    }
}
