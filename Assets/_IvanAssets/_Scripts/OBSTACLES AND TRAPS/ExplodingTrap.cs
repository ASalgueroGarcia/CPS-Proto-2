using UnityEngine;

public class ExplodingTrap : MonoBehaviour
{
    [Header("STATS SETTINGS")]
    [SerializeField] private float damageToPlayer = 40.0f;
    [SerializeField] private float damageToEnemys = 50.0f;
    [SerializeField] private float explosionRadius = 5.0f;
    [SerializeField] private float delayBetweenTrigger = 1.5f;

    private float explosionTimer = 0f;
    private bool exploded = false;
    private MeshRenderer meshRenderer; // 4 the color change.
    private SphereCollider tCollider;
    private Color originalColor;
    PlayerStatsManager s;
    // only 4 visual explotion.
     [SerializeField]private Color explosionColor = Color.yellow;

    private void Start()
    {
        tCollider = GetComponent<SphereCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null){
            originalColor = meshRenderer.material.color;

        }
        // Configurar el trigger
        if (tCollider != null){
            tCollider.radius = explosionRadius;
        }
    }

    private void Update()
    {
        if (exploded) return;
        if(explosionTimer > 0)
        {
            explosionTimer -= Time.deltaTime;

            if(meshRenderer != null)
            {
                // color change while wait
                float timeWaiting = 1f - (explosionTimer / delayBetweenTrigger);
                meshRenderer.material.color = Color.Lerp(originalColor, explosionColor, timeWaiting);
            }

            // Explotar solo cuando el timer llegue a 0
            if (explosionTimer <= 0)
            {
                Explosion();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // the already trap exploded?
        if (exploded)return;

        if (other.CompareTag("Enemy") || other.CompareTag("Player"))
        {
            explosionTimer = delayBetweenTrigger;
        }
    }
    private void Explosion()
    {
        exploded = true;
        Collider[] collsInRad = Physics.OverlapSphere(transform.position, explosionRadius);

        // FOR LOOP NORMAL
        for (int k = 0; k < collsInRad.Length;k++)
        {
            Collider colDet = collsInRad[k];

            // ENEMY IN RANGE?
            if (colDet.CompareTag("Enemy"))
            {
                Health enemyHealth = colDet.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damageToEnemys);
                }
            }

            if (colDet.CompareTag("Player"))
            {
                Health playerHealth = colDet.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageToPlayer);
                            s.Prints();

                }
            }
        }

        Destroy(gameObject);
    }    
private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}