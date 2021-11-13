using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MazeConstructorWithBurst : MonoBehaviour
{
    public LineRenderer pathPlotter;

    [SerializeField] private MeshRenderer miniMapRenderer;
    [SerializeField] private MeshFilter miniMap;

    [SerializeField] private MeshFilter floorsAndCeiling;
    [SerializeField] private MeshFilter walls;

    [SerializeField] private MeshCollider floorsAndCeilingCol;
    [SerializeField] private MeshCollider wallsCol;


    [SerializeField] private SpriteRenderer NavigationArrow;
    public Image miniMapBackground;

    [SerializeField] private Transform startObj;
    [SerializeField] private Transform endObj;
    [SerializeField] private Transform startObjMiniMap;
    [SerializeField] private Transform endObjMiniMap;

    public float3 StartPosition;
    public float3 EndPosition;

    [SerializeField] private Color lineStartColour = Color.green;
    [SerializeField] private Color lineEndColour = Color.yellow;
    [SerializeField] private Color miniMapBackgroundColour;
    [SerializeField] private Color miniMapForegroundColour = Color.black;
    [SerializeField] private Color navigationArrowColour = Color.white;
    public Color LineStartColour { get { return pathPlotter.startColor; } set { lineStartColour = pathPlotter.startColor = value; } }
    public Color LineEndColour { get { return pathPlotter.endColor; } set { lineEndColour = pathPlotter.endColor = value; } }
    public Color MiniMapBackgroundColour { get { return miniMapBackground.color; } set { miniMapBackground.color = value; } }
    public Color MiniMapForegroundColour { get { return miniMapRenderer.material.color; } set { miniMapRenderer.material.color = value; } }
    public Color NavigationArrowColour { get { return NavigationArrow.color; } set { NavigationArrow.color = value; } }

    private void Awake()
    {
        LineStartColour = lineStartColour;
        LineEndColour = lineEndColour;
        NavigationArrowColour = navigationArrowColour;
        MiniMapForegroundColour = miniMapForegroundColour;
        MiniMapBackgroundColour = miniMapBackgroundColour;
    }

    public void CyclePathColour()
    {
        Color Save = LineEndColour;
        LineEndColour = LineStartColour;
        LineStartColour = Save;
    }

    public void ResetPathColour()
    {
        LineStartColour = lineStartColour;
        LineEndColour = lineEndColour;
    }

    public void SetColours(ColourChangedEventArgs e)
    {
        LineEndColour = startObj.GetComponent<MeshRenderer>().material.color = startObjMiniMap.GetComponent<MeshRenderer>().material.color = e.background5Current;
        LineStartColour = endObj.GetComponent<MeshRenderer>().material.color = endObjMiniMap.GetComponent<MeshRenderer>().material.color = e.background6Current;
        MiniMapBackgroundColour = e.background7Current;
        MiniMapForegroundColour = e.background8Current;
        NavigationArrowColour = e.textCurrent;
    }
    //ResetPathColour();
    //CyclePathColour();
    public void GenerateNewMaze(int cellsZ, int cellsX, 
        TriggerEventHandler startCallBack = null, TriggerEventHandler endCallBack = null)
    {
        if (cellsZ % 2 == 0 && cellsX % 2 == 0)
        {
            Debug.LogWarning("Odd numbers work better for maze size.");
        }

        NativeList<float3> outputData = new NativeList<float3>(2, Allocator.TempJob);
        Mesh.MeshDataArray meshes = Mesh.AllocateWritableMeshData(2);
        MazeJob createMazeJob = new MazeJob
        {
            cellsX = cellsX,
            cellsZ = cellsZ,
            rng = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(0, int.MaxValue)),
            meshDataArray = meshes,
            endAndStartPositions = outputData
        };

        JobHandle handle = new JobHandle();
        JobHandle job = createMazeJob.Schedule(handle);
        job.Complete();

        Mesh[] mazeMeshes = new Mesh[]
        {
            new Mesh(),
            new Mesh()
        };

        Mesh.ApplyAndDisposeWritableMeshData(meshes, mazeMeshes);
        mazeMeshes[0].RecalculateNormals();
        mazeMeshes[0].RecalculateBounds();
        mazeMeshes[1].RecalculateNormals();
        mazeMeshes[1].RecalculateBounds();

        floorsAndCeilingCol.sharedMesh = floorsAndCeiling.sharedMesh = miniMap.sharedMesh = mazeMeshes[0];
        wallsCol.sharedMesh = walls.sharedMesh = mazeMeshes[1];

        startObj.position = StartPosition = new float3(outputData[0].x, 0.5f, outputData[0].z);
        endObj.position = EndPosition = new float3(outputData[1].x, 0.5f, outputData[1].z);
        startObjMiniMap.position = new Vector3(StartPosition.x, -25, StartPosition.z);
        endObjMiniMap.position = new Vector3(EndPosition.x, -25, EndPosition.z);

        outputData.RemoveAt(0);
        outputData.RemoveAt(0);

        //Debug.Log(outputData.Length);
        pathPlotter.positionCount = outputData.Length;
        pathPlotter.SetPositions(outputData.AsArray().Reinterpret<Vector3>());

        outputData.Dispose();

        startObj.GetComponent<TriggerEventRouter>().callback = startCallBack;
        endObj.GetComponent<TriggerEventRouter>().callback = endCallBack;

        ShowEnd();
    }

    public void HideEnd()
    {
        endObj.gameObject.SetActive(false);
        endObjMiniMap.gameObject.SetActive(false);
    }

    public void ShowEnd()
    {
        endObj.gameObject.SetActive(true);
        endObjMiniMap.gameObject.SetActive(true);
    }

    [BurstCompile]
    public struct MazeJob : IJob
    {
        const float placementThreshold = 0.1f;
        const float width = 3.75f;
        const float height = 3.5f;

        public int cellsZ;
        public int cellsX;

        public Unity.Mathematics.Random rng;

        public Mesh.MeshDataArray meshDataArray;

        public NativeList<float3> endAndStartPositions;
        public void Execute()
        {
            #region MazeData
            NativeArray<Cell> cells = Cell.CreateGird(cellsZ, cellsX, Allocator.Temp);
            for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                Cell cell = cells[cellIndex];
                if (cell.x == 0 || cell.z == 0 || cell.z == cellsZ - 1 || cell.x == cellsX - 1)
                {
                    cell.SetWall = true;
                }
                else if (cell.x % 2 == 0 && cell.z % 2 == 0)
                {
                    if (rng.NextFloat() > placementThreshold)
                    {
                        CellDirection direction = rng.NextFloat() < 0.5f ? (rng.NextFloat() < 0.5f ? CellDirection.S : CellDirection.N) : (rng.NextFloat() < 0.5f ? CellDirection.W : CellDirection.E);
                        Cell randNeighbour = Cell.GetNeighbour(cell, cells, direction);
                        if (!randNeighbour.Equals(Cell.Null))
                        {
                            randNeighbour.SetWall = true;
                            cells[randNeighbour.Index] = randNeighbour;
                        }
                        cell.SetWall = true;
                    }
                }
                cells[cellIndex] = cell;
            }
            #endregion
            #region MazeMeshes
            NativeList<float3> floorCeilingVertex = new NativeList<float3>(Allocator.Temp);
            NativeList<float2> floorCeilingVertexUV = new NativeList<float2>(Allocator.Temp);
            NativeList<float3> wallVertex = new NativeList<float3>(Allocator.Temp);
            NativeList<float2> wallVertexUV = new NativeList<float2>(Allocator.Temp);
            NativeList<uint> floorCeilingTriangles = new NativeList<uint>(Allocator.Temp);
            NativeList<uint> wallTriangles = new NativeList<uint>(Allocator.Temp);
            float halfHeight = height * .5f;

            for (int z = 0; z < cellsZ; z++)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    Cell cell = cells[(z * cellsX) + (x)];
                    if (!cell.SetWall)
                    {
                        // floor
                        float4x4 matrix = float4x4.TRS(new float3(x * width, 0, z * width), quaternion.LookRotationSafe(new float3(0, 1, 0), new float3(0, 0, 1)), new float3(width, width, 1));
                        cell.UsablePosition = AddQuadFloor(matrix, floorCeilingVertex, floorCeilingVertexUV, floorCeilingTriangles);
                        cell.HasFloor = true;
                        // ceiling
                        matrix = float4x4.TRS(new float3(x * width, height, z * width), quaternion.LookRotationSafe(new float3(0, -1, 0), new float3(0, 0, 1)), new float3(width, width, 1));
                        AddQuad(matrix, floorCeilingVertex, floorCeilingVertexUV, floorCeilingTriangles);

                        // walls
                        Cell neighbour = Cell.GetNeighbour(cell, cells, CellDirection.S);

                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = float4x4.TRS(new float3(x * width, halfHeight, (z - .5f) * width), quaternion.LookRotationSafe(new float3(0, 0, 1), new float3(0, 1, 0)), new float3(width, height, 1));
                            AddQuad(matrix, wallVertex, wallVertexUV, wallTriangles);
                        }
                        neighbour = Cell.GetNeighbour(cell, cells, CellDirection.E);
                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = float4x4.TRS(new float3((x + .5f) * width, halfHeight, z * width), quaternion.LookRotationSafe(new float3(-1, 0, 0), new float3(0, 1, 0)), new float3(width, height, 1));
                            AddQuad(matrix, wallVertex, wallVertexUV, wallTriangles);
                        }
                        neighbour = Cell.GetNeighbour(cell, cells, CellDirection.W);
                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = float4x4.TRS(new float3((x - .5f) * width, halfHeight, z * width), quaternion.LookRotationSafe(new float3(1, 0, 0), new float3(0, 1, 0)), new float3(width, height, 1));
                            AddQuad(matrix, wallVertex, wallVertexUV, wallTriangles);
                        }
                        neighbour = Cell.GetNeighbour(cell, cells, CellDirection.N);
                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = float4x4.TRS(new float3(x * width, halfHeight, (z + .5f) * width), quaternion.LookRotationSafe(new float3(0, 0, -1), new float3(0, 1, 0)), new float3(width, height, 1));
                            AddQuad(matrix, wallVertex, wallVertexUV, wallTriangles);
                        }
                    }
                    cells[cell.Index] = cell;
                }
            }

            // common vertex attrubyte descriptor array.
            NativeArray<VertexAttributeDescriptor> VertexDescriptors = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
            VertexDescriptors[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
            VertexDescriptors[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1);
            
            // floor and ceiling mesh
            Mesh.MeshData meshData = meshDataArray[0];
            meshData.SetVertexBufferParams(floorCeilingVertex.Length, VertexDescriptors);
            meshData.SetIndexBufferParams(floorCeilingTriangles.Length, IndexFormat.UInt32);
            meshData.GetVertexData<float3>(0).CopyFrom(floorCeilingVertex);
            meshData.GetVertexData<float2>(1).CopyFrom(floorCeilingVertexUV);
            meshData.GetIndexData<uint>().CopyFrom(floorCeilingTriangles);
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, floorCeilingTriangles.Length, MeshTopology.Triangles));
            
            // wall mesh
            meshData = meshDataArray[1];
            meshData.SetVertexBufferParams(wallVertex.Length, VertexDescriptors);
            meshData.SetIndexBufferParams(wallTriangles.Length, IndexFormat.UInt32);
            meshData.GetVertexData<float3>(0).CopyFrom(wallVertex);
            meshData.GetVertexData<float2>(1).CopyFrom(wallVertexUV);
            meshData.GetIndexData<uint>().CopyFrom(wallTriangles);
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, wallTriangles.Length, MeshTopology.Triangles));
            floorCeilingVertex.Dispose();
            floorCeilingVertexUV.Dispose();
            floorCeilingTriangles.Dispose();
            wallTriangles.Dispose();
            wallVertexUV.Dispose();
            wallVertex.Dispose();
            VertexDescriptors.Dispose();
            #endregion
            #region StartAndEndPositions
            int startIndex = FindStartPosition(cells);
            int endIndex = FindEndPosition(cells);

            if (startIndex == int.MinValue || endIndex == int.MinValue)
            {
                return;
            }
            endAndStartPositions.Add(cells[startIndex].UsablePosition);
            endAndStartPositions.Add(cells[endIndex].UsablePosition);
            
            if (Search(cells, startIndex, endIndex))
            {
                Cell current = cells[endIndex];
                while (current.Index != startIndex)
                {
                    endAndStartPositions.Add(new float3(current.UsablePosition.x, -25, current.UsablePosition.z));
                    current = cells[current.PathFrom];
                }
                current = cells[startIndex];
                endAndStartPositions.Add(new float3(current.UsablePosition.x, -25, current.UsablePosition.z));
            }
            cells.Dispose();
            #endregion
        }

        private bool Search(NativeArray<Cell> cells, int startIndex, int endIndex)
        {
            NativeArray<CellQueueElement> searchCells = new NativeArray<CellQueueElement>(cells.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                searchCells[cellIndex] = new CellQueueElement
                {
                    cellIndex = cellIndex,
                    NextWithSamePriority = int.MinValue,
                    SearchPhase = 0,
                    Distance = 0
                };
            }
            CellPriorityQueue searchFrontier = new CellPriorityQueue(searchCells);
            Cell toCell = cells[endIndex];
            int speed = 24;
            CellQueueElement firstCellElement = searchFrontier.elements[startIndex];
            firstCellElement.Distance = 0;
            int searchFrontierPhase = firstCellElement.SearchPhase = 2;
            searchFrontier.Enqueue(firstCellElement);
            while (searchFrontier.Count > 0)
            {
                int elementIndex = searchFrontier.Dequeue();
                CellQueueElement currentElement = searchFrontier.elements[elementIndex];
                Cell current = cells[elementIndex];
                currentElement.SearchPhase += 1;
                searchFrontier.elements[elementIndex] = currentElement;
                if (current.Equals(toCell))
                {
                    searchFrontier.Dispose();
                    return true;
                }
                int currentTurn = (currentElement.Distance - 1) / speed;
                for (CellDirection d = 0; d < CellDirection.W; d++)
                {
                    Cell neighbour = Cell.GetNeighbour(current, cells, d);
                    if (neighbour.Equals(Cell.Null))
                    {
                        continue;
                    }
                    CellQueueElement neighbourElement = searchFrontier.elements[neighbour.Index];
                    if (!neighbour.HasFloor || neighbourElement.SearchPhase > searchFrontierPhase)
                    {
                        continue;
                    }
                    int distance = currentElement.Distance + 10;
                    int turn = (distance - 1) / speed;
                    if (turn > currentTurn)
                    {
                        distance = turn * speed + 10;
                    }
                    if (neighbourElement.SearchPhase < searchFrontierPhase)
                    {
                        neighbourElement.SearchPhase = searchFrontierPhase;
                        neighbourElement.Distance = distance;
                        neighbour.PathFrom = current.Index;

                        neighbourElement.SearchHeuristic = CalculateDistanceCost(neighbour, toCell);
                        searchFrontier.Enqueue(neighbourElement);
                    }
                    else if (distance < neighbourElement.Distance)
                    {
                        int oldPriority = neighbourElement.SearchPriority;
                        neighbourElement.Distance = distance;
                        neighbour.PathFrom = current.Index;
                        searchFrontier.Change(neighbourElement, oldPriority);
                    }
                    cells[neighbour.Index] = neighbour;
                }
            }
            searchFrontier.Dispose();
            return false;
        }

        private int CalculateDistanceCost(Cell a, Cell b)
        {
            int xDistance = math.abs(a.x - b.x);
            int zDistance = math.abs(a.z - b.z);
            int remaining = (xDistance - zDistance);
            return 14 * math.min(xDistance, zDistance) + 10 * remaining;
        }

        private int FindStartPosition(NativeArray<Cell> cells)
        {
            for (int z = 0; z < cellsZ; z++)
            {
                for (int x = 0; x < cellsX; x++)
                {
                    Cell cell = cells[(z * cellsX) + (x)];
                    if (cell.HasFloor)
                    {
                        return cell.Index;
                    }
                }
            }
            return int.MinValue;
        }

        private int FindEndPosition(NativeArray<Cell> cells)
        {
            for (int z = cellsZ - 1; z >= 0; z--)
            {
                for (int x = cellsX - 1; x >= 0; x--)
                {
                    Cell cell = cells[(z * cellsX) + (x)];
                    if (cell.HasFloor)
                    {
                        return cell.Index;
                    }
                }
            }
            return int.MinValue;
        }

        private void AddQuad(float4x4 matrix, NativeList<float3> newVertices, NativeList<float2> newUVs, NativeList<uint> newTriangles)
        {
            uint index = (uint)newVertices.Length;
            newVertices.Add(math.mul(matrix, new float4(-0.5f, -0.5f, 0, 1f)).xyz);
            newVertices.Add(math.mul(matrix, new float4(-0.5f, 0.5f, 0, 1f)).xyz);
            newVertices.Add(math.mul(matrix, new float4(0.5f, 0.5f, 0, 1f)).xyz);
            newVertices.Add(math.mul(matrix, new float4(0.5f, -0.5f, 0, 1f)).xyz);
            newUVs.Add(new float2(1, 0));
            newUVs.Add(new float2(1, 1));
            newUVs.Add(new float2(0, 1));
            newUVs.Add(new float2(0, 0));


            newTriangles.Add(index + 2);
            newTriangles.Add(index + 1);
            newTriangles.Add(index);

            newTriangles.Add(index + 3);
            newTriangles.Add(index + 2);
            newTriangles.Add(index);
        }

        private float3 AddQuadFloor(float4x4 matrix, NativeList<float3> newVertices, NativeList<float2> newUVs, NativeList<uint> newTriangles)
        {
            uint index = (uint)newVertices.Length;

            float3 v1 = math.mul(matrix, new float4(-0.5f, -0.5f, 0, 1f)).xyz;
            float3 v2 = math.mul(matrix, new float4(-0.5f, 0.5f, 0, 1f)).xyz;
            float3 v3 = math.mul(matrix, new float4(0.5f, 0.5f, 0, 1f)).xyz;
            float3 v4 = math.mul(matrix, new float4(0.5f, -0.5f, 0, 1f)).xyz;
            float3 v5 = (v1 + v2 + v3 + v4) / 4;

            newVertices.Add(v1);
            newVertices.Add(v2);
            newVertices.Add(v3);
            newVertices.Add(v4);

            newUVs.Add(new float2(1, 0));
            newUVs.Add(new float2(1, 1));
            newUVs.Add(new float2(0, 1));
            newUVs.Add(new float2(0, 0));

            newTriangles.Add(index + 2);
            newTriangles.Add(index + 1);
            newTriangles.Add(index);

            newTriangles.Add(index + 3);
            newTriangles.Add(index + 2);
            newTriangles.Add(index);
            return v5;
        }


    }
}
