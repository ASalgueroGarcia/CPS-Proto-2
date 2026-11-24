using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

// the enemy spawner of the cards.
public class DeckOfCardsAttack : MonoBehaviour, IEnemyAttackStrategy
{
    public int AliveSoldiers { get{ CleanDeadSoldiers(); return spawnedEnemys.Count; }} // how many soldiers are left.
    private const int gizS = 48; // this is for now.

    [Header("SPAWNER SETTINGS")]
    [SerializeField] private GameObject cardSoldierPref; // the soldiers - spawns.
    [SerializeField] private float cardSpawnRadius = 2f;
    [SerializeField] private int maxCardSoldiersAvile = 15; // max TOTAL - 15 soldiers per deck spawner.
    [SerializeField] private int maxCardSoldiersAvileST = 4;
    [SerializeField] private bool soldierStartAttacking = true;
    [SerializeField] private float checkHeight = 1.5f;

    [Header("VALIDATION OF THE SPAWN POINT SETTINGS")]
    [SerializeField] private LayerMask obstaclesMask;
    [SerializeField] private int maxPoints = 12;
    [SerializeField] private int totalSoldiersSpawned;
    [SerializeField] private float checkRad = 0.3f; // obstacles radius validator sphere.

    ///////////////////////////////

    // just 4 now - delete this later.
    [Header("Gizmos (radius visualss)")]
    [SerializeField] private bool showG = true;
    [SerializeField] private Color radiusV = new Color(1f, 0.2f, 0.3f, 1f);
    [SerializeField] private Color lastPointGizCol = new Color(1f, 0.2f, 0.3f, 1f);
    ///////////////////////////////

    // list of the enemys.
    private List<GameObject> spawnedEnemys = new List<GameObject>();
 
    // for the gizmos.
    private Vector3 lastSpawnPoint; // where did the last enemy spawns.
    private bool hasLastSpawn;
    public float ApproachDuration => 0f;
    public float ExecutingDuration => 0f;
    public bool IsExecutionComplete => false;
    public bool BeginsWindupAtAnyDistance => true;
    private bool SoldierCanSpawn()
    {
        if (totalSoldiersSpawned >= maxCardSoldiersAvile) {
            return false; //15
        }

        if (AliveSoldiers >= maxCardSoldiersAvileST) {
            return false; // wait -> 4 already on the scene.
        }
         
        // can spawn.
        return true;
    }
    public void OnApproachTarget(Enemy owner, float distanceToPlayer){}

    public void OnWindup(Enemy owner)
    {   
        if (!SoldierCanSpawn()) {
            return;
        }
        owner.FlashColor(Color.red, 0.20f);

    }
    public void OnExecute(Enemy owner, float distanceToPlayer)
    {
        if(cardSoldierPref==null){
            return;
        }
        if (!SoldierCanSpawn()) {
            return;
        }
        // validate the point.
        if(!SpawnPointValid(out Vector3 pToSpawn))
        {
            return;
        }
        SpawnSoldier(owner, pToSpawn);

    }
    public void OnCooldown(Enemy owner, float distanceToPlayer){}

    // to garantee the 4 max soldiers alive at the same time on the scene.

    private bool SpawnPointValid(out Vector3 pToSpawn)
    {
        pToSpawn = transform.position;

        for (int i = 0; i < maxPoints; i++)
        {
            //randoom point.
            Vector2 offsetP = Random.insideUnitCircle * cardSpawnRadius; 
            Vector3 validP = transform.position + new Vector3(offsetP.x, 0f, offsetP.y);
            
            Vector3 bottom = validP + Vector3.up * checkRad;
            Vector3 top = validP + Vector3.up * Mathf.Max(checkHeight - checkRad, checkRad);

            if (Physics.CheckCapsule(bottom, top, checkRad, obstaclesMask, QueryTriggerInteraction.Ignore)){
                continue;
            }

            pToSpawn = validP;  // free point.
            return true;
        }
        return false;
    }

    // To create the soldiers.
    private void SpawnSoldier(Enemy owner, Vector3 point)
    {
        GameObject soldier = Instantiate(cardSoldierPref, point, Quaternion.identity, owner.transform.parent);

        spawnedEnemys.Add(soldier);    // alive soldier
        totalSoldiersSpawned++;
        lastSpawnPoint = point;
        hasLastSpawn = true;

        if (soldierStartAttacking && soldier.TryGetComponent(out Enemy soldierEnemy)){
            soldierEnemy.ChangeState(Enemy.EnemyState.Attack); // the soldier MUST start chasing the player.
        }
    }

    private void CleanDeadSoldiers()
    {
        for (int i = spawnedEnemys.Count - 1; i >= 0; i--){
            if (spawnedEnemys[i] == null)
                spawnedEnemys.RemoveAt(i);
        }
    }

    //GIZZ - TO DELETE.
    private void OnDrawGizmos()
    {
        if (!showG) return;

        Gizmos.color = radiusV;
        DrawCircle(transform.position, cardSpawnRadius);

        if (!hasLastSpawn) return;
        Gizmos.color = lastPointGizCol;
        Gizmos.DrawWireSphere(lastSpawnPoint, checkRad);
    }

    private static void DrawCircle(Vector3 center, float radius)
    {
        Vector3 previous = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= gizS; i++)
        {
            float angle = (float)i / gizS * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
