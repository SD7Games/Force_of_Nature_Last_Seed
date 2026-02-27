using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpawnEnemyPoint : MonoBehaviour
{
    [Header("Waypoints (Rails)")]
    [SerializeField] private Transform[] _waypoints;

    [Header("Speed")]
    [SerializeField] private float _moveSpeed = 2f;

    [Header("Prefabs (4 segment types)")]
    [SerializeField] private WormSegment _headPrefab;

    [SerializeField] private WormSegment _bodyPrefab;
    [SerializeField] private WormSegment _cocoonPrefab;
    [SerializeField] private WormSegment _tailPrefab;

    [Header("Prewarm")]
    [Min(0)][SerializeField] private int _prewarmHead = 2;

    [Min(0)][SerializeField] private int _prewarmBody = 10;
    [Min(0)][SerializeField] private int _prewarmCocoon = 2;
    [Min(0)][SerializeField] private int _prewarmTail = 2;

    [Header("Spawn Pattern (order + counts)")]
    [SerializeField]
    private List<SpawnEntry> _pattern = new()
    {
        new SpawnEntry(WormSegmentType.Head, 1),
        new SpawnEntry(WormSegmentType.Body, 6),
        new SpawnEntry(WormSegmentType.Cocoon, 1),
        new SpawnEntry(WormSegmentType.Tail, 1),
    };

    [Header("Spacing")]
    [Tooltip("Delay between segments. If 0, auto-calc from spacing/speed.")]
    [SerializeField] private float _spawnDelayOverride = 0f;

    [Min(0.05f)]
    [SerializeField] private float _segmentSpacing = 0.6f;

    [System.Serializable]
    public struct SpawnEntry
    {
        public WormSegmentType Type;
        [Min(1)] public int Count;

        public SpawnEntry(WormSegmentType type, int count)
        {
            Type = type;
            Count = count;
        }
    }

    private readonly Queue<WormSegment> _headPool = new();
    private readonly Queue<WormSegment> _bodyPool = new();
    private readonly Queue<WormSegment> _cocoonPool = new();
    private readonly Queue<WormSegment> _tailPool = new();

    private void Awake()
    {
        Prewarm(_headPrefab, _prewarmHead, _headPool);
        Prewarm(_bodyPrefab, _prewarmBody, _bodyPool);
        Prewarm(_cocoonPrefab, _prewarmCocoon, _cocoonPool);
        Prewarm(_tailPrefab, _prewarmTail, _tailPool);
    }

    private void Start()
    {
        SpawnWorm();
    }

    public void SpawnWorm()
    {
        if (_waypoints == null || _waypoints.Length == 0)
        {
            Debug.LogWarning($"{name}: No waypoints assigned.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        float delay = _spawnDelayOverride > 0f
            ? _spawnDelayOverride
            : Mathf.Max(0.01f, _segmentSpacing / Mathf.Max(0.01f, _moveSpeed));

        for (int i = 0; i < _pattern.Count; i++)
        {
            SpawnEntry entry = _pattern[i];

            for (int j = 0; j < entry.Count; j++)
            {
                WormSegment segment = GetFromPool(entry.Type);

                segment.SetupReturn(ReturnToPool);
                segment.StartMove(_waypoints, _moveSpeed);

                yield return new WaitForSeconds(delay);
            }
        }
    }

    private WormSegment GetFromPool(WormSegmentType type)
    {
        Queue<WormSegment> pool = GetPool(type);

        if (pool.Count > 0)
            return pool.Dequeue();

        WormSegment prefab = GetPrefab(type);
        WormSegment created = Instantiate(prefab, transform);
        created.gameObject.SetActive(false);

        return created;
    }

    private void ReturnToPool(WormSegment segment)
    {
        segment.gameObject.SetActive(false);

        Queue<WormSegment> pool = GetPool(segment.Type);
        pool.Enqueue(segment);
    }

    private void Prewarm(WormSegment prefab, int count, Queue<WormSegment> pool)
    {
        if (prefab == null || count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            WormSegment created = Instantiate(prefab, transform);
            created.gameObject.SetActive(false);
            pool.Enqueue(created);
        }
    }

    private Queue<WormSegment> GetPool(WormSegmentType type)
    {
        return type switch
        {
            WormSegmentType.Head => _headPool,
            WormSegmentType.Body => _bodyPool,
            WormSegmentType.Cocoon => _cocoonPool,
            WormSegmentType.Tail => _tailPool,
            _ => _bodyPool
        };
    }

    private WormSegment GetPrefab(WormSegmentType type)
    {
        return type switch
        {
            WormSegmentType.Head => _headPrefab,
            WormSegmentType.Body => _bodyPrefab,
            WormSegmentType.Cocoon => _cocoonPrefab,
            WormSegmentType.Tail => _tailPrefab,
            _ => _bodyPrefab
        };
    }
}