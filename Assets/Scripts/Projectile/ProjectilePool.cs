using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int initialSize = 20;

    private readonly Queue<GameObject> pool = new();

    private void Awake()
    {
        Instance = this;

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = Instantiate(projectilePrefab, transform);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // 풀이 부족하면 새로 생성
        return Instantiate(projectilePrefab, transform);
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
