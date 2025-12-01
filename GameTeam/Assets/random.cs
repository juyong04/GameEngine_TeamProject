using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class RoadGenerator : MonoBehaviour
{
    // === 맵 및 도로 설정 ===
    [Header("Map and Road Settings")]
    public int mapSize = 200; 
    public int roadWidth = 6; 
    public int minStraightLength = 50; 
    public int maxStraightLength = 100; 
    public int maxSegments = 40; 
    
    // === 유니티 레퍼런스 (필수 할당!) ===
    [Header("Tilemap References")]
    public Tilemap roadTilemap;
    public TileBase roadTile;           // 일반 6차선 도로 타일
    public TileBase grassTile;          // 도로 외곽 기본 타일
    public TileBase[] cornerTiles = new TileBase[4]; // 코너 4종류 (북동, 북서, 남서, 남동)

    // === 신호등 설정 ===
    [Header("Traffic Light Settings")]
    public GameObject trafficLightPrefab; // 신호등 프리팹
    
    // === 내부 사용 변수 ===
    private HashSet<Vector3Int> allRoadPositions = new HashSet<Vector3Int>();
    private List<Vector3Int> majorRoadJunctions = new List<Vector3Int>(); // 1차선 경로의 교차/코너 지점
    
    private readonly Vector3Int[] cardinalDirections = new Vector3Int[]
    {
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0)
    };

    void Start()
    {
        // 🚨 Null 체크 강화: 필수 레퍼런스 누락 시 오류 메시지 출력 후 즉시 종료
        if (roadTilemap == null || roadTile == null)
        {
            Debug.LogError("🚨 필수 오류: Road Tilemap 또는 Road Tile이 할당되지 않았습니다. 인스펙터를 확인하세요.");
            return;
        }
        if (cornerTiles.Length != 4 || cornerTiles.Any(t => t == null))
        {
            Debug.LogWarning("⚠️ 코너 타일 4종류가 모두 할당되지 않았습니다. 코너 표시는 건너뜁니다.");
        }
        
        GenerateRoadMap();
    }

    public void GenerateRoadMap()
    {
        Debug.Log("맵 생성 시작...");

        // 1. 초기화
        roadTilemap.ClearAllTiles();
        allRoadPositions.Clear();
        majorRoadJunctions.Clear();
        
        // 2. 배경 채우기
        FillMapWithGrass();

        // 3. 경로 기반 도로 생성
        Vector3Int currentPos = new Vector3Int(mapSize / 2, mapSize / 2, 0);
        GeneratePathBasedRoads(currentPos);
        
        // 4. 시작 부분 정리
        CleanStartPoint(currentPos);

        // 5. 6차선으로 확장 및 코너 타일 배치
        WidenRoads();

        // 6. 신호등 배치
        PlaceTrafficLights();

        // 나머지 건물, 아이템/장애물 배치 로직은 제외됨.

        Debug.Log($"맵 생성 완료! 도로 타일 수: {allRoadPositions.Count}, 교차로/코너 수: {majorRoadJunctions.Count}");
    }
    
    // === 배경 채우기 로직 ===
    private void FillMapWithGrass()
    {
        if (grassTile == null) return; 

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                roadTilemap.SetTile(new Vector3Int(x, y, 0), grassTile);
            }
        }
    }
    
    // === 도로 끝 정리 로직 ===
    private void CleanStartPoint(Vector3Int startPos)
    {
        int cleanRadius = roadWidth / 2 + 1;

        for (int x = -cleanRadius; x <= cleanRadius; x++)
        {
            for (int y = -cleanRadius; y <= cleanRadius; y++)
            {
                Vector3Int pos = startPos + new Vector3Int(x, y, 0);
                if (allRoadPositions.Contains(pos))
                {
                    allRoadPositions.Remove(pos);
                    
                    if (grassTile != null)
                    {
                        roadTilemap.SetTile(pos, grassTile); 
                    }
                    else
                    {
                        roadTilemap.SetTile(pos, null); 
                    }
                }
            }
        }
    }

    // === 경로 기반 도로 생성 로직 ===
    private void GeneratePathBasedRoads(Vector3Int startPos)
    {
        majorRoadJunctions.Add(startPos);
        List<Vector3Int> activeJunctions = new List<Vector3Int>(majorRoadJunctions);
        
        int segmentsCreated = 0;
        
        while (activeJunctions.Count > 0 && segmentsCreated < maxSegments)
        {
            Vector3Int startJunction = activeJunctions[Random.Range(0, activeJunctions.Count)];
            Vector3Int[] directions = cardinalDirections.OrderBy(x => Random.value).ToArray();

            bool segmentStarted = false;
            foreach (var currentDirection in directions)
            {
                int segmentLength = Random.Range(minStraightLength, maxStraightLength);
                
                Vector3Int endPos = GenerateStraightSegment(startJunction, currentDirection, segmentLength);

                if (Vector3Int.Distance(startJunction, endPos) > minStraightLength / 2)
                {
                    majorRoadJunctions.Add(endPos);
                    activeJunctions.Add(endPos);
                    segmentsCreated++;
                    segmentStarted = true;
                    break; 
                }
            }
            
            if (!segmentStarted)
            {
                activeJunctions.Remove(startJunction); 
            }
        }
    }

    private Vector3Int GenerateStraightSegment(Vector3Int start, Vector3Int direction, int length)
    {
        Vector3Int current = start;
        int boundaryMargin = roadWidth / 2 + 3;
        int overlapCheckRadius = roadWidth + 1;

        for (int i = 0; i < length; i++)
        {
            current += direction;

            // 1. 맵 경계 체크
            if (current.x < boundaryMargin || current.x >= mapSize - boundaryMargin || 
                current.y < boundaryMargin || current.y >= mapSize - boundaryMargin)
            {
                return current - direction;
            }
            
            // 2. 기존 도로와의 충돌 체크 (겹침 방지)
            if (i > minStraightLength)
            {
                for (int x = -overlapCheckRadius; x <= overlapCheckRadius; x++)
                {
                    for (int y = -overlapCheckRadius; y <= overlapCheckRadius; y++)
                    {
                        Vector3Int checkPos = current + new Vector3Int(x, y, 0);
                        if (allRoadPositions.Contains(checkPos))
                        {
                            return current - direction; 
                        }
                    }
                }
            }
            
            allRoadPositions.Add(current);
            roadTilemap.SetTile(current, roadTile);
        }
        return current;
    }
    
    // === 6차선 확장 및 코너 타일 배치 로직 ===
    private void WidenRoads()
    {
        HashSet<Vector3Int> majorRoads = new HashSet<Vector3Int>(allRoadPositions);
        HashSet<Vector3Int> widenedRoads = new HashSet<Vector3Int>();

        int halfWidth = roadWidth / 2;

        // 1. 6차선 확장 (빈틈 보강된 정방형 확장)
        foreach (Vector3Int roadPos in majorRoads)
        {
            for (int xOffset = -halfWidth; xOffset <= halfWidth; xOffset++)
            {
                for (int yOffset = -halfWidth; yOffset <= halfWidth; yOffset++)
                {
                    Vector3Int widePos = roadPos + new Vector3Int(xOffset, yOffset, 0);
                    
                    if (widePos.x >= 0 && widePos.x < mapSize && widePos.y >= 0 && widePos.y < mapSize)
                    {
                        widenedRoads.Add(widePos);
                    }
                }
            }
        }

        // 2. 최종 확장된 영역에 일반 타일 배치
        foreach (Vector3Int finalPos in widenedRoads)
        {
            roadTilemap.SetTile(finalPos, roadTile);
            allRoadPositions.Add(finalPos); 
        }

        // 3. 코너 타일 배치
        PlaceCornerTiles();
    }
    
    // === 코너 타일 배치 로직 ===
    private void PlaceCornerTiles()
    {
        // 코너 타일이 4개가 아니거나 하나라도 Null이면 종료
        if (cornerTiles.Length != 4 || cornerTiles.Any(t => t == null)) return;

        int halfCorner = roadWidth / 2;
        
        foreach (Vector3Int junctionPos in majorRoadJunctions)
        {
            // 코너 타일의 월드 좌표 기준 중심 위치 (6차선 코너의 외곽 모서리)
            Vector3Int[] cornerCenters = new Vector3Int[]
            {
                junctionPos + new Vector3Int(halfCorner, halfCorner, 0), // 북동 (Index 0)
                junctionPos + new Vector3Int(-halfCorner, halfCorner, 0), // 북서 (Index 1)
                junctionPos + new Vector3Int(-halfCorner, -halfCorner, 0), // 남서 (Index 2)
                junctionPos + new Vector3Int(halfCorner, -halfCorner, 0) // 남동 (Index 3)
            };

            for (int i = 0; i < 4; i++)
            {
                Vector3Int center = cornerCenters[i];
                TileBase cornerTile = cornerTiles[i];

                // 해당 위치가 6차선 도로 영역 내에 있는지 확인하고 배치
                if (allRoadPositions.Contains(center)) 
                {
                    roadTilemap.SetTile(center, cornerTile);
                }
            }
        }
    }

    // === 신호등 배치 로직 ===
    private void PlaceTrafficLights()
    {
        if (trafficLightPrefab == null || majorRoadJunctions.Count == 0) return;

        foreach (Vector3Int junctionPos in majorRoadJunctions)
        {
            int offset = roadWidth / 2 + 1; // 도로 외곽에 배치하기 위한 오프셋
            
            // 신호등이 배치될 4개의 위치
            Vector3Int[] lightPositions = new Vector3Int[]
            {
                junctionPos + new Vector3Int(offset, offset, 0), 
                junctionPos + new Vector3Int(-offset, offset, 0),
                junctionPos + new Vector3Int(offset, -offset, 0), 
                junctionPos + new Vector3Int(-offset, -offset, 0)
            };

            foreach (Vector3Int lightPos in lightPositions)
            {
                // 도로가 아닌 곳에 신호등 배치
                if (!allRoadPositions.Contains(lightPos) && 
                    lightPos.x >= 0 && lightPos.x < mapSize && 
                    lightPos.y >= 0 && lightPos.y < mapSize)
                {
                    Vector3 worldPos = roadTilemap.CellToWorld(lightPos) + roadTilemap.cellSize / 2;
                    Instantiate(trafficLightPrefab, worldPos, Quaternion.identity, this.transform);
                }
            }
        }
    }

    // === 제외된 함수 (건물, 아이템/장애물 관련) ===
    // 이 함수들은 제거되었으므로, 해당 기능을 다시 추가하려면 이전 버전 코드를 참고해야 합니다.
}