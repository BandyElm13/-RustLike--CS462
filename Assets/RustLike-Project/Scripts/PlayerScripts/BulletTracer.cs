using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    public float lifetime = 0.08f;

    private LineRenderer line;
    private float startWidth;
    private float age;

    public void Fire(Vector3 from, Vector3 to)
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        startWidth = line.widthMultiplier;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        age += Time.deltaTime;
        line.widthMultiplier = startWidth * (1f - age / lifetime);
    }
}
