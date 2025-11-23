using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int poolSize = 50;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(enemyPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject GetEnemy()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // Optional: expand pool
        GameObject newObj = Instantiate(enemyPrefab);
        return newObj;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
        pool.Enqueue(enemy);
    }
}
