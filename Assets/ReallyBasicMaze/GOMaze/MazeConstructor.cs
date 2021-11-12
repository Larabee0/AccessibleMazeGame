using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace GameObjectMaze
{
    public class MazeConstructor : MonoBehaviour
    {
        private MazeDataGenerator dataGenerator;
        private MazeMeshGenerator meshGenerator;

        [SerializeField] private LineRenderer pathPlotter;
        public bool showDebug;
        [SerializeField] private Material lineMateral;
        [SerializeField] private Material mazeMat1;
        [SerializeField] private Material mazeMat2;
        [SerializeField] private Material startMat;
        [SerializeField] private Material treasureMat;

        public NativeArray<Cell> DataNative;

        public int StartIndex, GoalIndex;
        public Vector3 StartPosition;
        public Vector3 GoalPosition;
        private void Awake()
        {
            dataGenerator = new MazeDataGenerator();
            meshGenerator = new MazeMeshGenerator();
        }

        public void GenerateNewMaze(int sizeRows, int sizeCols,TriggerEventHandler startCallBack = null,TriggerEventHandler goalCallBack = null)
        {
            if (sizeRows % 2 == 0 && sizeCols % 2 == 0)
            {
                Debug.LogWarning("Odd numbers work better for maze size.");
            }
            DisposeOldMaze();
            DataNative = dataGenerator.FromDimensionsNative(sizeRows, sizeCols);
            DisplayMaze(sizeRows, sizeCols);
            FindStartPosition(sizeRows, sizeCols);
            FindGoalPosition(sizeRows, sizeCols);
            PlaceStartTrigger(startCallBack);
            PlaceGoalTrigger(goalCallBack);
        }

        public void DisplayMaze(int sizeRows, int sizeCols)
        {
            GameObject mazeWallsAndCeiling = new GameObject
            {
                name = "Procedural maze",
                tag = "Generated"
            };
            mazeWallsAndCeiling.transform.position = Vector3.zero;
            MeshFilter mWACmf = mazeWallsAndCeiling.AddComponent<MeshFilter>();

            GameObject mazeFloor = new GameObject
            {
                name = "Procedural maze floor",
                tag = "Generated"
            };
            mazeFloor.transform.position = Vector3.zero;
            mazeFloor.transform.SetParent(mazeWallsAndCeiling.transform);
            MeshFilter mFmf = mazeFloor.AddComponent<MeshFilter>();

            (Mesh maze, Mesh floor) = meshGenerator.FromData(DataNative, sizeRows, sizeCols);
            // meshes
            mWACmf.mesh = maze;
            mFmf.mesh = floor;
            // colliders
            MeshCollider mWACmc = mazeWallsAndCeiling.AddComponent<MeshCollider>();
            mWACmc.sharedMesh = mWACmf.mesh;

            MeshCollider mFmc = mazeFloor.AddComponent<MeshCollider>();
            mFmc.sharedMesh = mFmf.mesh;
            // renderers
            MeshRenderer mWACmr = mazeWallsAndCeiling.AddComponent<MeshRenderer>();
            mWACmr.materials = new Material[2] { mazeMat1, mazeMat2 };

            MeshRenderer mFmr = mazeFloor.AddComponent<MeshRenderer>();
            mFmr.material = mazeMat1;

            GameObject miniMapFloor = Instantiate(mazeFloor);
            miniMapFloor.transform.position = new Vector3(0, -30);



            GameObject pathDisplay = new GameObject();
            pathPlotter = pathDisplay.AddComponent<LineRenderer>();
            pathDisplay.transform.position = new Vector3(0, -25);
            pathDisplay.transform.SetParent(miniMapFloor.transform);
        }

        private void FindStartPosition(int sizeRows, int sizeCols)
        {
            for (int z = 0; z < sizeRows; z++)
            {
                for (int x = 0; x < sizeCols; x++)
                {
                    Cell cell = DataNative[(z * sizeCols) + x];
                    if (cell.HasFloor)
                    {
                        StartPosition = cell.UsablePosition;
                        StartIndex = cell.Index;
                        return;
                    }
                }
            }
        }

        private void FindGoalPosition(int sizeRows, int sizeCols)
        {
            for (int z = sizeRows - 1; z >= 0; z--)
            {
                for (int x = sizeCols - 1; x >= 0; x--)
                {
                    Cell cell = DataNative[(z * sizeCols) + x];
                    if (cell.HasFloor)
                    {
                        GoalPosition = cell.UsablePosition;
                        GoalIndex = cell.Index;
                        return;
                    }
                }
            }
        }

        private void PlaceStartTrigger(TriggerEventHandler callBack)
        {
            GameObject mazeStart = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mazeStart.transform.position = new Vector3(StartPosition.x, 0.5f, StartPosition.z);
            mazeStart.name = "Start Trigger";
            mazeStart.tag = "Generated";

            mazeStart.GetComponent<BoxCollider>().isTrigger = true;
            mazeStart.GetComponent<MeshRenderer>().sharedMaterial = startMat;

            TriggerEventRouter tc = mazeStart.AddComponent<TriggerEventRouter>();
            tc.callback = callBack;

            GameObject miniMapStart = Instantiate(mazeStart);
            miniMapStart.transform.SetParent(mazeStart.transform);
            miniMapStart.transform.Translate(new Vector3(0, -25));
        }

        private void PlaceGoalTrigger(TriggerEventHandler callBack)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = new Vector3(GoalPosition.x, 0.5f, GoalPosition.z);
            go.name = "Treasure";
            go.tag = "Generated";

            go.GetComponent<BoxCollider>().isTrigger = true;
            go.GetComponent<MeshRenderer>().sharedMaterial = treasureMat;
            TriggerEventRouter tc = go.AddComponent<TriggerEventRouter>();
            tc.callback = callBack;
            GameObject go2 = Instantiate(go);
            go2.transform.SetParent(go.transform);
            go2.transform.Translate(new Vector3(0, -25));

            pathPlotter.startColor = Color.green;
            pathPlotter.endColor = Color.yellow;
            pathPlotter.material = lineMateral;
            
            List<Vector3> lineNodes = new List<Vector3>();

            if (Search())
            {
                Cell current = DataNative[GoalIndex];
                while (current.Index!=StartIndex)
                {
                    lineNodes.Add(new Vector3(current.UsablePosition.x, -25, current.UsablePosition.z));
                    current = DataNative[current.PathFrom];
                }
                current = DataNative[StartIndex];
                lineNodes.Add(new Vector3(current.UsablePosition.x, -25, current.UsablePosition.z));
                lineNodes.Reverse();
                pathPlotter.positionCount = lineNodes.Count;
                pathPlotter.SetPositions(lineNodes.ToArray());
            }
        }

        private bool Search()
        {
            Cell toCell = DataNative[GoalIndex];
            int speed = 24;
            int searchFrontierPhase = 2;
            NativeArray<CellQueueElement> searchCells = 
                new NativeArray<CellQueueElement>(DataNative.Length, 
                Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int cellIndex = 0; cellIndex < DataNative.Length; cellIndex++)
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

            CellQueueElement firstCellElement = searchFrontier.elements[StartIndex];
            firstCellElement.Distance = 0;
            firstCellElement.SearchPhase = searchFrontierPhase;
            searchFrontier.Enqueue(firstCellElement);

            while (searchFrontier.Count > 0)
            {
                int elementIndex = searchFrontier.Dequeue();
                CellQueueElement currentElement = searchFrontier.elements[elementIndex];
                Cell current = DataNative[elementIndex];
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
                    Cell neighbour = Cell.GetNeighbour(current, DataNative, d);
                    if (neighbour.Equals(Cell.Null))
                    {
                        continue;
                    }
                    if (!neighbour.HasFloor)
                    {
                        continue;
                    }

                    int distance = currentElement.Distance + 1;
                    int turn = (distance - 1) / speed;
                    if (turn > currentTurn)
                    {
                        distance = turn * speed + 1;
                    }

                    CellQueueElement neighbourElement = searchFrontier.elements[neighbour.Index];
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
                    DataNative[neighbour.Index] = neighbour;
                }
            }
            searchFrontier.Dispose();
            return false;
        }

        private int CalculateDistanceCost(Cell a, Cell b)
        {
            int xDistance = Mathf.Abs(a.x - b.x);
            int zDistance = Mathf.Abs(a.z - b.z);
            int remaining = Mathf.Abs(xDistance - zDistance);
            return 14 * Mathf.Min(xDistance, zDistance) + 10 * remaining;
        }

        public void DisposeOldMaze()
        {
            List<GameObject> objects = new List<GameObject>(GameObject.FindGameObjectsWithTag("Generated"));
            objects.ForEach(obj => Destroy(obj));
            try
            {
                DataNative.Dispose();
            }
            catch { }
        }

        private void OnDestroy()
        {
            try
            {
                DataNative.Dispose();
            }
            catch { }
        }
    }

    public class MazeMeshGenerator
    {
        public const float width = 3.75f;
        public const float height = 3.5f;

        public (Mesh, Mesh) FromData(NativeArray<Cell> data, int sizeRows, int sizeCols)
        {
            Mesh maze = new Mesh
            {
                subMeshCount = 2
            };
            Mesh mazeFloor = new Mesh
            {
                subMeshCount = 1
            };

            List<Vector3> floorVertices = new List<Vector3>();
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<Vector2> floorUVs = new List<Vector2>();
            List<int> floorTriangles = new List<int>();
            List<int> ceilingTriangles = new List<int>();
            List<int> wallTriangles = new List<int>();

            float halfHeight = height * .5f;

            for (int z = 0; z < sizeRows; z++)
            {
                for (int x = 0; x < sizeCols; x++)
                {
                    Cell cell = data[(z * sizeCols) + (x)];
                    if (!cell.SetWall)
                    {
                        // floor
                        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(x * width, 0, z * width), Quaternion.LookRotation(Vector3.up), new Vector3(width, width, 1));
                        cell.UsablePosition = AddQuadFloor(matrix, floorVertices, floorUVs, floorTriangles);
                        cell.HasFloor = true;

                        // ceiling
                        matrix = Matrix4x4.TRS(new Vector3(x * width, height, z * width), Quaternion.LookRotation(Vector3.down), new Vector3(width, width, 1));
                        AddQuad(matrix, newVertices, newUVs, ceilingTriangles);
                        
                        // walls
                        Cell neighbour = Cell.GetNeighbour(cell, data, CellDirection.S);
                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = Matrix4x4.TRS(new Vector3(x * width, halfHeight, (z - .5f) * width), Quaternion.LookRotation(Vector3.forward), new Vector3(width, height, 1));
                            AddQuad(matrix, newVertices, newUVs, wallTriangles);
                        }
                        
                        neighbour = Cell.GetNeighbour(cell, data, CellDirection.E);
                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = Matrix4x4.TRS(new Vector3((x + .5f) * width, halfHeight, z * width), Quaternion.LookRotation(Vector3.left), new Vector3(width, height, 1));
                            AddQuad(matrix, newVertices, newUVs, wallTriangles);
                        }
                        
                        neighbour = Cell.GetNeighbour(cell, data, CellDirection.W);
                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = Matrix4x4.TRS(new Vector3((x - .5f) * width, halfHeight, z * width), Quaternion.LookRotation(Vector3.right), new Vector3(width, height, 1));
                            AddQuad(matrix, newVertices, newUVs, wallTriangles);
                        }
                        
                        neighbour = Cell.GetNeighbour(cell, data, CellDirection.N);
                        if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                        {
                            matrix = Matrix4x4.TRS(new Vector3(x * width, halfHeight, (z + .5f) * width), Quaternion.LookRotation(Vector3.back), new Vector3(width, height, 1));
                            AddQuad(matrix, newVertices, newUVs, wallTriangles);
                        }
                    }
                    data[cell.Index] = cell;
                }
            }

            maze.SetVertices(newVertices);
            maze.SetUVs(0, newUVs);
            maze.SetTriangles(ceilingTriangles, 0);
            maze.SetTriangles(wallTriangles, 1);
            maze.RecalculateNormals();

            mazeFloor.SetVertices(floorVertices);
            mazeFloor.SetUVs(0, floorUVs);
            mazeFloor.SetTriangles(floorTriangles, 0);
            mazeFloor.RecalculateNormals();
            return (maze, mazeFloor);
        }

        private void AddQuad(Matrix4x4 matrix,List<Vector3> newVertices,List<Vector2> newUVs, List<int> newTriangles)
        {
            int index = newVertices.Count;
            Vector3 v1 = new Vector3(-.5f, -.5f, 0);
            Vector3 v2 = new Vector3(-.5f, .5f, 0);
            Vector3 v3 = new Vector3(.5f, .5f, 0);
            Vector3 v4 = new Vector3(.5f, -.5f, 0);
            newVertices.Add(matrix.MultiplyPoint3x4(v1));
            newVertices.Add(matrix.MultiplyPoint3x4(v2));
            newVertices.Add(matrix.MultiplyPoint3x4(v3));
            newVertices.Add(matrix.MultiplyPoint3x4(v4));
            newUVs.Add(new Vector2(1, 0));
            newUVs.Add(new Vector2(1, 1));
            newUVs.Add(new Vector2(0, 1));
            newUVs.Add(new Vector2(0, 0));

            newTriangles.Add(index + 2);
            newTriangles.Add(index + 1);
            newTriangles.Add(index);

            newTriangles.Add(index + 3);
            newTriangles.Add(index + 2);
            newTriangles.Add(index);
        }

        private Vector3 AddQuadFloor(Matrix4x4 matrix, List<Vector3> newVertices, List<Vector2> newUVs, List<int> newTriangles)
        {
            int index = newVertices.Count;

            Vector3 v1 = matrix.MultiplyPoint3x4(new Vector3(-.5f, -.5f, 0));
            Vector3 v2 = matrix.MultiplyPoint3x4(new Vector3(-.5f, .5f, 0));
            Vector3 v3 = matrix.MultiplyPoint3x4(new Vector3(.5f, .5f, 0));
            Vector3 v4 = matrix.MultiplyPoint3x4(new Vector3(.5f, -.5f, 0));
            Vector3 v5 = (v1 + v2 + v3 + v4) / 4;
            newVertices.Add(v1);
            newVertices.Add(v2);
            newVertices.Add(v3);
            newVertices.Add(v4);
            newUVs.Add(new Vector2(1, 0));
            newUVs.Add(new Vector2(1, 1));
            newUVs.Add(new Vector2(0, 1));
            newUVs.Add(new Vector2(0, 0));

            newTriangles.Add(index + 2);
            newTriangles.Add(index + 1);
            newTriangles.Add(index);

            newTriangles.Add(index + 3);
            newTriangles.Add(index + 2);
            newTriangles.Add(index);
            return v5;
        }
    }

    public class MazeDataGenerator
    {
        public const float  placementThreshold = 0.1f;

        public NativeArray<Cell> FromDimensionsNative(int CellsZ, int CellsX)
        {
            NativeArray<Cell> maze = Cell.CreateGird(CellsZ, CellsX);
            for (int cellIndex = 0; cellIndex < maze.Length; cellIndex++)
            {
                Cell cell = maze[cellIndex];
                if (cell.x == 0 || cell.z == 0 || cell.z == CellsZ - 1 || cell.x == CellsX - 1)
                {
                    cell.SetWall = true;
                }
                else if (cell.x % 2 == 0 && cell.z % 2 == 0)
                {
                    if (Random.value > placementThreshold)
                    {
                        CellDirection direction = Random.value < 0.5f ? (Random.value < 0.5f ? CellDirection.S : CellDirection.N) : (Random.value < 0.5f ? CellDirection.W : CellDirection.E);
                        Cell randNeighbour = Cell.GetNeighbour(cell, maze, direction);
                        if (!randNeighbour.Equals(Cell.Null))
                        {
                            randNeighbour.SetWall = true;
                            maze[randNeighbour.Index] = randNeighbour;
                        }
                        cell.SetWall = true;
                    }
                }
                maze[cellIndex] = cell;
            }
            return maze;
        }
    }
}