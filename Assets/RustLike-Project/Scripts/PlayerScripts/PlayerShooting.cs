using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float shootRange = 100f;
    [SerializeField] private int damage = 25;

    [Header("Timing Variables")]
    private float timestamp;
    public static float energy;
    private float chargeBuffer;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float chargeRate = 1f;
    [SerializeField] private float chargeDelay = 2f;
    [SerializeField] private float energyMax = 10f;
    [SerializeField] private float energyCost = 1f;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Shooting
        if (Input.GetMouseButtonDown(0))
        {
            if (canShoot()) Shoot();
        }

        // Energy recharge
        chargeBuffer -= Time.deltaTime;
        if ((energy < energyMax) && (chargeBuffer <= 0f)) energy += chargeRate * Time.deltaTime;
    }

    private bool canShoot()
    {
        if ((Time.time < timestamp) || (energy <= energyCost)) return false;
        timestamp = Time.time + fireRate;
        return true;
    }

    private void Shoot()
    {
        energy -= energyCost;
        chargeBuffer = chargeDelay;

        Ray ray = playerCamera.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        if (Physics.Raycast(ray, out RaycastHit hit, shootRange))
        {
            EnemyStats enemy = hit.collider.GetComponentInParent<EnemyStats>();

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