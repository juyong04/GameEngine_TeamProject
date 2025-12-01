using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class RoadGenerator : MonoBehaviour
{
    // === 맵 및 도로 설정 (수정) ===
    [Header("Map and Road Settings")]
    public int mapSize = 200; 
    public int roadWidth = 6; 
    
    // 혼잡도를 높이기 위해 직선 길이와 최대 세그먼트 수 조정
    public int minStraightLength = 30; // 최소 직선 길이 감소 (50 -> 30)
    public int maxStraightLength = 60; // 최대 직선 길이 감소 (100 -> 60)
    public int maxSegments = 60;       // 최대 세그먼트 수 증가 (40 -> 60)
    
    // === 유니티 레퍼런스 (필수 할당!) ===
    [Header("Tilemap References")]
    public Tilemap roadTilemap;
    public TileBase roadTile;           // 일반 6차선 도로 타일
    public TileBase grassTile;          // 도로 외곽 기본 타일
    public TileBase[] cornerTiles = new TileBase[4]; // 코너 4종류 

    // === 신호등 설정 ===
    [Header("Traffic Light Settings")]
    public GameObject trafficLightPrefab; 
    
    // === 내부 사용 변수 ===
    private HashSet<Vector3Int> allRoadPositions = new HashSet<Vector3Int>();
    private List<Vector3Int> majorRoadJunctions = new List<Vector3Int>(); 
    
    private readonly Vector3Int[] cardinalDirections = new Vector3Int[]
    {
        new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
        new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0)
    };

    void Start()
    {
        if (roadTilemap == null || roadTile == null)
        {
            Debug.LogError("🚨 필수 오류: Road Tilemap 또는 Road Tile이 할당되지 않았습니다. 인스펙터를 확인하세요.");
            return;
        }
        GenerateRoadMap();
    }

    public void GenerateRoadMap()
    {
        roadTilemap.ClearAllTiles();
        allRoadPositions.Clear();
        majorRoadJunctions.Clear();
        
        FillMapWithGrass();

        Vector3Int currentPos = new Vector3Int(mapSize / 2, mapSize / 2, 0);
        GeneratePathBasedRoads(currentPos);
        
        CleanStartPoint(currentPos);

        WidenRoads();

        PlaceTrafficLights(); 

        Debug.Log($"맵 생성 완료! 도로 타일 수: {allRoadPositions.Count}, 교차로/코너 수: {majorRoadJunctions.Count}");
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

                // 최소 길이 기준을 낮춤 (50% -> 30%)
                if (Vector3Int.Distance(startJunction, endPos) > minStraightLength * 0.3f)
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
        
        // 혼잡도 증가 핵심 수정: 겹침 방지 이격 거리를 최소한으로 줄임 (7 -> 4)
        // 6차선 (폭 6)이므로 겹치지 않으려면 최소 6타일이 필요. 
        // 1차선 경로가 겹치지 않게 하기 위해 도로 폭의 절반 + 1 정도로 설정
        int overlapCheckRadius = roadWidth / 2 + 1; // 4타일 (3 + 1)

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
            // 50타일 간격 조건을 30타일로 낮춘 후 체크
            if (i > minStraightLength) 
            {
                for (int x = -overlapCheckRadius; x <= overlapCheckRadius; x++)
                {
                    for (int y = -overlapCheckRadius; y <= overlapCheckRadius; y++)
                    {
                        Vector3Int checkPos = current + new Vector3Int(x, y, 0);
                        
                        // 1차선 경로가 너무 가까이 있는지 확인 (12차선 방지)
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
        if (cornerTiles.Length != 4 || cornerTiles.Any(t => t == null)) return;

        int halfCorner = roadWidth / 2;
        
        foreach (Vector3Int junctionPos in majorRoadJunctions)
        {
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
            int offset = roadWidth / 2 + 1; 

            Vector3Int[] lightPositions = new Vector3Int[]
            {
                junctionPos + new Vector3Int(offset, offset, 0), 
                junctionPos + new Vector3Int(-offset, offset, 0),
                junctionPos + new Vector3Int(offset, -offset, 0), 
                junctionPos + new Vector3Int(-offset, -offset, 0)
            };

            foreach (Vector3Int lightPos in lightPositions)
            {
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
    
    // === 기타 로직 (Null 체크 포함) ===
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
                    roadTilemap.SetTile(pos, grassTile != null ? grassTile : null); 
                }
            }
        }
    }

    // === 제외된 함수 (건물, 아이템/장애물 관련) ===
    // 해당 로직은 제외되었습니다.
}