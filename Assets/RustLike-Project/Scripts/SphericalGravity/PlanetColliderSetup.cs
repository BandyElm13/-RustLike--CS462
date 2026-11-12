using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Put this on a planet's root. It makes every visible prop on the planet solid:
//   - Any mesh with no collider (rocks, buildings from asset packs) gets a MeshCollider
//     that matches its exact shape.
//   - A SphereCollider on a stretched (non-uniformly scaled) sphere is replaced with a
//     MeshCollider, because SphereColliders always stay perfectly round and would stick
//     out past the visible surface.
//
// It runs automatically when Play starts. To save the colliders into the prefab (needed
// for builds, recommended), open the prefab, right-click this component's header and
// choose "Bake Colliders Now", then save the prefab.
[DisallowMultipleComponent]
public class PlanetColliderSetup : MonoBehaviour
{
    [Tooltip("Add a MeshCollider to every mesh that has no collider.")]
    public bool addMissingColliders = true;

    [Tooltip("Replace SphereColliders on stretched spheres with exact MeshColliders.")]
    public bool fixStretchedSphereColliders = true;

    [Tooltip("Skip meshes smaller than this (world units, largest side) so tiny pebbles don't trip the player.")]
    public float minimumSize = 0.5f;

    [Tooltip("Objects (and their children) listed here are left alone.")]
    public List<Transform> ignore = new List<Transform>();

    private void Awake()
    {
        int changed = Apply(false);
        if (changed > 0)
            Debug.Log($"PlanetColliderSetup: added/fixed {changed} colliders on '{name}' at runtime. " +
                      "Use 'Bake Colliders Now' to save them into the prefab.", this);
    }

    [ContextMenu("Bake Colliders Now")]
    private void BakeNow()
    {
        int changed = Apply(true);
        Debug.Log($"PlanetColliderSetup: added/fixed {changed} colliders on '{name}'. Save the prefab/scene to keep them.", this);
    }

    private int Apply(bool recordUndo)
    {
        int changed = 0;

        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || IsIgnored(filter.transform)) continue;

            // Moving bodies (enemies, pickups) manage their own colliders.
            if (filter.GetComponentInParent<Rigidbody>() != null) continue;

            GameObject go = filter.gameObject;
            Collider existing = go.GetComponent<Collider>();

            if (existing == null)
            {
                if (!addMissingColliders) continue;

                Renderer r = go.GetComponent<Renderer>();
                if (r != null && r.bounds.size.MaxComponent() < minimumSize) continue;

                AddMeshCollider(go, mesh, recordUndo);
                changed++;
            }
            else if (fixStretchedSphereColliders && existing is SphereCollider sphere &&
                     !sphere.isTrigger && IsNonUniform(go.transform.lossyScale))
            {
                RemoveCollider(sphere, recordUndo);
                AddMeshCollider(go, mesh, recordUndo);
                changed++;
            }
        }

        return changed;
    }

    private bool IsIgnored(Transform t)
    {
        foreach (Transform root in ignore)
            if (root != null && (t == root || t.IsChildOf(root)))
                return true;
        return false;
    }

    private static bool IsNonUniform(Vector3 s)
    {
        float max = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        float min = Mathf.Min(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        return max > 0f && (max - min) / max > 0.02f;
    }

    private static void AddMeshCollider(GameObject go, Mesh mesh, bool recordUndo)
    {
        MeshCollider mc;
#if UNITY_EDITOR
        if (recordUndo && !Application.isPlaying)
            mc = Undo.AddComponent<MeshCollider>(go);
        else
#endif
            mc = go.AddComponent<MeshCollider>();

        mc.sharedMesh = mesh;
        mc.convex = false;
    }

    private static void RemoveCollider(Collider c, bool recordUndo)
    {
#if UNITY_EDITOR
        if (recordUndo && !Application.isPlaying)
        {
            Undo.DestroyObjectImmediate(c);
            return;
        }
#endif
        if (Application.isPlaying) Destroy(c);
        else DestroyImmediate(c);
    }
}

internal static class PlanetColliderSetupVectorExtensions
{
    public static float MaxComponent(this Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));
}
