using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Pool Reference")]
    public EnemyPool pool;   // Drag your EnemyPool object in here

    [Header("Spawn Settings")]
    public float spawnInterval = 1f;
    public int maxAlive = 10;        // Max enemies alive at once
    public int totalToSpawn = 100;   // Total enemies to spawn overall

    [Header("Spawn Points (Optional)")]
    public Transform[] spawnPoints;

    private int currentAlive = 0;
    private int totalSpawned = 0;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (totalSpawned < totalToSpawn)
        {
            if (currentAlive < maxAlive)
            {
                SpawnEnemy();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnEnemy()
    {
        Transform spawnPoint = transform;

        // Pick random spawn point if available
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        // Get enemy from pool (no Instantiate)
        GameObject enemy = pool.GetEnemy();

        // Reset transform
        enemy.transform.position = spawnPoint.position;
        enemy.transform.rotation = spawnPoint.rotation;

        // Make sure it's active
        enemy.SetActive(true);

        currentAlive++;
        totalSpawned++;

        // Tell the enemy where it came from
      /* EnemyLife life = enemy.GetComponent<EnemyLife>();
        if (life != null)
        {
            life.spawner = this;
            life.pool = pool; // so it can return itself
            life.ResetEnemy(); // optional safety reset
        }
      */
    }

    // Called by enemies when they "die"
    public void OnEnemyKilled()
    {
        currentAlive--;
    }
}
