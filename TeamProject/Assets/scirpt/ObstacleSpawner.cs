using UnityEngine;
using UnityEngine.Tilemaps;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Tilemap Settings")]
    public Tilemap targetTilemap;       // 타겟 Tilemap (예: Road 타일맵)
    public TileBase targetTile;         // 이 타일 위에만 방해물 생성

    [Header("Obstacle Settings")]
    public GameObject obstaclePrefab;   // 생성할 방해물 프리팹 (CarObstacle, FixedObstacle 등)

    [Tooltip("X 방향 간격 (타일 단위). 1이면 모든 타일, 3이면 3칸마다 1개")]
    public int stepX = 3;

    [Tooltip("Y 방향 간격 (타일 단위). 1이면 모든 타일, 2이면 2칸마다 1개")]
    public int stepY = 1;

    [Header("Spawn Offset")]
    [Tooltip("타일 중앙에서의 추가 오프셋 (월드 좌표)")]
    public Vector3 worldOffset = Vector3.zero;

    private void Start()
    {
        if (targetTilemap == null || obstaclePrefab == null || targetTile == null)
        {
            Debug.LogWarning("ObstacleSpawner: 설정이 부족합니다. Tilemap / Prefab / TargetTile을 확인하세요.");
            return;
        }

        SpawnObstaclesOnTiles();
    }

    private void SpawnObstaclesOnTiles()
    {
        // 타일맵의 전체 범위
        BoundsInt bounds = targetTilemap.cellBounds;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                // 간격 조건: stepX, stepY에 맞게 필터링
                if (stepX > 1 && Mathf.Abs(x) % stepX != 0) continue;
                if (stepY > 1 && Mathf.Abs(y) % stepY != 0) continue;

                Vector3Int cellPos = new Vector3Int(x, y, 0);
                TileBase tile = targetTilemap.GetTile(cellPos);

                // 해당 칸이 targetTile일 때만 생성
                if (tile == targetTile)
                {
                    Vector3 worldPos = targetTilemap.GetCellCenterWorld(cellPos) + worldOffset;
                    Instantiate(obstaclePrefab, worldPos, Quaternion.identity);
                }
            }
        }
    }
}
