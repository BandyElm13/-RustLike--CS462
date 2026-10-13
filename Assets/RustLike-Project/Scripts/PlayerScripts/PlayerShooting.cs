using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float shootRange = 100f;
    [SerializeField] private int damage = 25;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            EnemyStats enemy = hit.collider.GetComponent<EnemyStats>();

            if (enemy != null)
            {
                enemy.TakeDamage(damage);

                Debug.Log(
                    "Hit enemy for " +
                    damage +
                    " damage."
                );
            }
        }
    }
}