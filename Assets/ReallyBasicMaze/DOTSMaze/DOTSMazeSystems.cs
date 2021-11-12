using UnityEngine.Rendering;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

namespace DOTSMaze
{
    public struct CreateMaze : IComponentData 
    {
        public int CellsZ;
        public int CellsX;
    }

    public struct MazeData : IComponentData
    {
        public int CellsZ;
        public int CellsX;
    }
    public struct MeshReady : IComponentData { }
    public struct Generated : IComponentData { }
    public struct FloorMesh : IComponentData { }
    public struct WallMesh : IComponentData { }
    public struct MeshVertexInfo : IBufferElementData
    {
        public float3 vertex;
        public float2 uv;

        public MeshVertexInfo(float3 v, float2 uv)
        {
            vertex = v;
            this.uv = uv;
        }
    }
    public struct MeshIndexInfo : IBufferElementData
    {
        public uint Index;

        public MeshIndexInfo(uint i)
        {
            Index = i;
        }
    }

    public class CreateMazeSystem : JobComponentSystem
    {
        EndSimulationEntityCommandBufferSystem ecbEndSystem;
        BeginSimulationEntityCommandBufferSystem ecbBeginSystem;
        private readonly EntityQueryDesc CreateMazeQuery = new EntityQueryDesc { All = new ComponentType[] { typeof(CreateMaze) },  };
        private EntityArchetype mazeMeshArch;

        protected override void OnCreate()
        {
            ecbEndSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            ecbBeginSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
            mazeMeshArch = EntityManager.CreateArchetype(typeof(Translation), typeof(LocalToWorld), 
                typeof(LocalToParent), typeof(Parent), typeof(Generated));
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityQuery CreateMaze = GetEntityQuery(CreateMazeQuery);
            CreateMazeJob mazeJob = new CreateMazeJob()
            {
                mazeMeshArch = mazeMeshArch,
                mazeRootTypeHandle = GetEntityTypeHandle(),
                createMazeTypeHandle = GetComponentTypeHandle<CreateMaze>(true),
                cellBufferTypeHandle = GetBufferTypeHandle<Cell>(),
                rng = new Random((uint)UnityEngine.Time.time + 1),
                ecbEnd = ecbEndSystem.CreateCommandBuffer(),
                ecbBegin = ecbBeginSystem.CreateCommandBuffer()
            };
            JobHandle outputDeps = mazeJob.Schedule(CreateMaze, inputDeps);
            ecbEndSystem.AddJobHandleForProducer(outputDeps);
            ecbBeginSystem.AddJobHandleForProducer(outputDeps);
            return outputDeps;
        }

        [BurstCompile]
        private struct CreateMazeJob : IJobEntityBatch
        {   
            [ReadOnly]
            public EntityArchetype mazeMeshArch;
            [ReadOnly]
            public EntityTypeHandle mazeRootTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<CreateMaze> createMazeTypeHandle;
            public BufferTypeHandle<Cell> cellBufferTypeHandle;

            public Random rng;

            public EntityCommandBuffer ecbEnd;
            public EntityCommandBuffer ecbBegin;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                float placementThreshold = 0.1f;
                float width = 3.75f;
                float height = 3.5f;
                NativeArray<Entity> entities = batchInChunk.GetNativeArray(mazeRootTypeHandle);
                NativeArray<CreateMaze> createMazes = batchInChunk.GetNativeArray(createMazeTypeHandle);
                BufferAccessor<Cell> cellBufferAccessor = batchInChunk.GetBufferAccessor(cellBufferTypeHandle);
                for (int i = 0; i < entities.Length; i++)
                {
                    #region MazeData
                    CreateMaze mazeData = createMazes[i];

                    NativeArray<Cell> maze = Cell.CreateGird(mazeData.CellsZ, mazeData.CellsX, Allocator.Temp);

                    for (int cellIndex = 0; cellIndex < maze.Length; cellIndex++)
                    {
                        Cell cell = maze[cellIndex];
                        if (cell.x == 0 || cell.z == 0 || cell.z == mazeData.CellsZ - 1 || cell.x == mazeData.CellsX - 1)
                        {
                            cell.SetWall = true;
                        }
                        else if (cell.x % 2 == 0 && cell.z % 2 == 0)
                        {
                            if (rng.NextFloat() > placementThreshold)
                            {
                                CellDirection direction = rng.NextFloat() < 0.5f ? 
                                    (rng.NextFloat() < 0.5f ? CellDirection.S : CellDirection.N) 
                                    : (rng.NextFloat() < 0.5f ? CellDirection.W : CellDirection.E);
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

                    #endregion

                    #region MazeMeshData
                    NativeList<float3> colliderVertices = new NativeList<float3>(Allocator.Temp);
                    NativeList<MeshVertexInfo> floorCeilingVertexData = new NativeList<MeshVertexInfo>(Allocator.Temp);
                    NativeList<MeshVertexInfo> wallVertexData = new NativeList<MeshVertexInfo>(Allocator.Temp);
                    NativeList<int3> colliderTriangles = new NativeList<int3>(Allocator.Temp);
                    NativeList<MeshIndexInfo> floorCeilingTriangles = new NativeList<MeshIndexInfo>(Allocator.Temp);
                    NativeList<MeshIndexInfo> wallTriangles = new NativeList<MeshIndexInfo>(Allocator.Temp);
                    float halfHeight = height * .5f;

                    for (int z = 0; z < mazeData.CellsZ; z++)
                    {
                        for (int x = 0; x < mazeData.CellsX; x++)
                        {
                            Cell cell = maze[(z * mazeData.CellsX) + (x)];
                            if (!cell.SetWall)
                            {
                                // floor
                                float4x4 matrix = float4x4.TRS(new float3(x * width, 0, z * width), quaternion.LookRotationSafe(new float3(0, 1, 0), new float3(0, 0, 1)), new float3(width, width, 1));
                                cell.UsablePosition = AddQuadFloor(matrix, floorCeilingVertexData, floorCeilingTriangles, colliderVertices, colliderTriangles);
                                cell.HasFloor = true;
                                // ceiling
                                matrix = float4x4.TRS(new float3(x * width, height, z * width), quaternion.LookRotationSafe(new float3(0, -1, 0), new float3(0, 0, 1)), new float3(width, width, 1));
                                AddQuad(matrix, floorCeilingVertexData, floorCeilingTriangles, colliderVertices, colliderTriangles);

                                // walls
                                Cell neighbour = Cell.GetNeighbour(cell, maze, CellDirection.S);

                                if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                                {
                                    matrix = float4x4.TRS(new float3(x * width, halfHeight, (z - .5f) * width), quaternion.LookRotationSafe(new float3(0, 0, 1), new float3(0, 1, 0)), new float3(width, height, 1));
                                    AddQuad(matrix, wallVertexData, wallTriangles, colliderVertices, colliderTriangles);
                                }
                                neighbour = Cell.GetNeighbour(cell, maze, CellDirection.E);
                                if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                                {
                                    matrix = float4x4.TRS(new float3((x + .5f) * width, halfHeight, z * width), quaternion.LookRotationSafe(new float3(-1, 0, 0), new float3(0, 1, 0)), new float3(width, height, 1));
                                    AddQuad(matrix, wallVertexData, wallTriangles, colliderVertices, colliderTriangles);
                                }
                                neighbour = Cell.GetNeighbour(cell, maze, CellDirection.W);
                                if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                                {
                                    matrix = float4x4.TRS(new float3((x - .5f) * width, halfHeight, z * width), quaternion.LookRotationSafe(new float3(1, 0, 0), new float3(0, 1, 0)), new float3(width, height, 1));
                                    AddQuad(matrix, wallVertexData, wallTriangles, colliderVertices, colliderTriangles);
                                }
                                neighbour = Cell.GetNeighbour(cell, maze, CellDirection.N);
                                if (neighbour.Equals(Cell.Null) || neighbour.SetWall)
                                {
                                    matrix = float4x4.TRS(new float3(x * width, halfHeight, (z + .5f) * width), quaternion.LookRotationSafe(new float3(0, 0, -1), new float3(0, 1, 0)), new float3(width, height, 1));
                                    AddQuad(matrix, wallVertexData, wallTriangles, colliderVertices, colliderTriangles);
                                }
                            }
                            maze[cell.Index] = cell;
                        }
                    }


                    cellBufferAccessor[i].CopyFrom(maze);
                    maze.Dispose();

                    PhysicsCollider collider = new PhysicsCollider()
                    {
                        Value = MeshCollider.Create(colliderVertices, colliderTriangles)
                    };
                    colliderVertices.Dispose();
                    colliderTriangles.Dispose();

                    Parent mazeParent = new Parent { Value = entities[i] };

                    MazeData data = new MazeData { CellsX = mazeData.CellsX, CellsZ = mazeData.CellsZ };

                    ecbBegin.AddComponent(mazeParent.Value, data);
                    ecbBegin.AddComponent(mazeParent.Value, collider);
                    ecbBegin.AddComponent<MeshReady>(mazeParent.Value);
                    ecbEnd.RemoveComponent<CreateMaze>(mazeParent.Value);


                    Entity newMesh = ecbBegin.CreateEntity(mazeMeshArch);
                    ecbBegin.AddBuffer<MeshVertexInfo>(newMesh).CopyFrom(floorCeilingVertexData);
                    ecbBegin.AddBuffer<MeshIndexInfo>(newMesh).CopyFrom(floorCeilingTriangles);
                    floorCeilingVertexData.Dispose();
                    floorCeilingTriangles.Dispose();
                    ecbBegin.AddComponent<FloorMesh>(newMesh);
                    ecbBegin.AddComponent(newMesh, data);
                    ecbBegin.SetComponent(newMesh, mazeParent);

                    newMesh = ecbBegin.CreateEntity(mazeMeshArch);
                    ecbBegin.AddBuffer<MeshVertexInfo>(newMesh).CopyFrom(wallVertexData);
                    ecbBegin.AddBuffer<MeshIndexInfo>(newMesh).CopyFrom(wallTriangles);
                    wallTriangles.Dispose();
                    wallVertexData.Dispose();
                    ecbBegin.AddComponent<WallMesh>(newMesh);
                    ecbBegin.AddComponent(newMesh, data);
                    ecbBegin.SetComponent(newMesh, mazeParent);

                    #endregion
                }
            }

            private void AddQuad(float4x4 matrix, NativeList<MeshVertexInfo> newVertices, 
                NativeList<MeshIndexInfo> newTriangles, NativeList<float3> colliderVerts, NativeList<int3> colliderTriangles)
            {
                uint index = (uint)newVertices.Length;
                float3 v1 = math.mul(matrix, new float4(-0.5f, -0.5f, 0, 1f)).xyz;
                float3 v2 = math.mul(matrix, new float4(-0.5f, 0.5f, 0, 1f)).xyz;
                float3 v3 = math.mul(matrix, new float4(0.5f, 0.5f, 0, 1f)).xyz;
                float3 v4 = math.mul(matrix, new float4(0.5f, -0.5f, 0, 1f)).xyz;
                newVertices.Add(new MeshVertexInfo(v1, new float2(1, 0)));
                newVertices.Add(new MeshVertexInfo(v2, new float2(1, 1)));
                newVertices.Add(new MeshVertexInfo(v3, new float2(0, 1)));
                newVertices.Add(new MeshVertexInfo(v4, new float2(0, 0)));

                int colliderIndex = colliderVerts.Length;
                colliderVerts.Add(v1);
                colliderVerts.Add(v2);
                colliderVerts.Add(v3);
                colliderVerts.Add(v4);

                newTriangles.Add(new MeshIndexInfo(index + 2));
                newTriangles.Add(new MeshIndexInfo(index + 1));
                newTriangles.Add(new MeshIndexInfo(index));

                colliderTriangles.Add(new int3( colliderIndex + 2, colliderIndex + 1, colliderIndex));
                colliderTriangles.Add(new int3(colliderIndex + 3, colliderIndex + 2, colliderIndex));

                newTriangles.Add(new MeshIndexInfo(index + 3));
                newTriangles.Add(new MeshIndexInfo(index + 2));
                newTriangles.Add(new MeshIndexInfo(index));
            }

            private float3 AddQuadFloor(float4x4 matrix, NativeList<MeshVertexInfo> newVertices, 
                NativeList<MeshIndexInfo> newTriangles, NativeList<float3> colliderVerts, NativeList<int3> colliderTriangles)
            {
                uint index = (uint)newVertices.Length;
                int colliderIndex = colliderVerts.Length;


                float3 v1 = math.mul(matrix, new float4(-0.5f, -0.5f, 0, 1f)).xyz;
                float3 v2 = math.mul(matrix, new float4(-0.5f, 0.5f, 0, 1f)).xyz;
                float3 v3 = math.mul(matrix, new float4(0.5f, 0.5f, 0, 1f)).xyz;
                float3 v4 = math.mul(matrix, new float4(0.5f, -0.5f, 0, 1f)).xyz;
                float3 v5 = (v1 + v2 + v3 + v4) / 4;

                newVertices.Add(new MeshVertexInfo(v1, new float2(1, 0)));
                newVertices.Add(new MeshVertexInfo(v2, new float2(1, 1)));
                newVertices.Add(new MeshVertexInfo(v3, new float2(0, 1)));
                newVertices.Add(new MeshVertexInfo(v4, new float2(0, 0)));

                colliderVerts.Add(v1);
                colliderVerts.Add(v2);
                colliderVerts.Add(v3);
                colliderVerts.Add(v4);

                newTriangles.Add(new MeshIndexInfo(index + 2));
                newTriangles.Add(new MeshIndexInfo(index + 1));
                newTriangles.Add(new MeshIndexInfo(index));

                colliderTriangles.Add(new int3(colliderIndex + 2, colliderIndex + 1, colliderIndex));
                colliderTriangles.Add(new int3(colliderIndex + 3, colliderIndex + 2, colliderIndex));

                newTriangles.Add(new MeshIndexInfo(index + 3));
                newTriangles.Add(new MeshIndexInfo(index + 2));
                newTriangles.Add(new MeshIndexInfo(index));
                return v5;
            }
        }
    }

    public class AssignMazeMeshSystem : ComponentSystem
    {
        EndSimulationEntityCommandBufferSystem ecbEndSystem;
        BeginSimulationEntityCommandBufferSystem ecbBeginSystem;

        private readonly EntityQueryDesc CreateMazeQuery = new EntityQueryDesc { All = new ComponentType[] { typeof(MeshReady) } };

        private readonly EntityQueryDesc CreateMeshQuery = new EntityQueryDesc { All = new ComponentType[] { typeof(MazeData), 
            typeof(MeshVertexInfo),typeof(MeshIndexInfo) } };

        public static UnityEngine.Material mazeMat1;
        public static UnityEngine.Material mazeMat2;

        protected override void OnCreate()
        {
            ecbEndSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            ecbBeginSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (GetEntityQuery(CreateMazeQuery).IsEmpty)
            {
                return;
            }

            EntityQuery meshQuery = GetEntityQuery(CreateMeshQuery);
            NativeArray<ArchetypeChunk> archetypes = meshQuery.CreateArchetypeChunkArray(Allocator.Temp);
            Entity floor = Entity.Null;
            Entity walls = Entity.Null;
            for (int i = 0; i < archetypes.Length; i++)
            {
                if (archetypes[i].Has(GetComponentTypeHandle<FloorMesh>()))
                {
                    floor = archetypes[i].GetNativeArray(GetEntityTypeHandle())[0];
                }
                else
                {
                    walls = archetypes[i].GetNativeArray(GetEntityTypeHandle())[0];
                }
            }
            
            UnityEngine.Mesh.MeshDataArray meshes = UnityEngine.Mesh.AllocateWritableMeshData(2);
            CreateMesh meshJob = new CreateMesh
            {
                entityTypeHandle = GetEntityTypeHandle(),
                floorTypeHandle = GetComponentTypeHandle<FloorMesh>(true),
                vertexBufferTypeHandle = GetBufferTypeHandle<MeshVertexInfo>(true),
                indexbufferTypeHandle = GetBufferTypeHandle<MeshIndexInfo>(true),
                meshDataArray = meshes,
                ecbEnd = ecbEndSystem.CreateCommandBuffer(),
            };
            JobHandle handle = new JobHandle();
            JobHandle job = meshJob.Schedule(meshQuery, handle);
            ecbEndSystem.AddJobHandleForProducer(job);
            job.Complete();

            UnityEngine.Mesh[] updatedMeshes = new UnityEngine.Mesh[2];
            for (int i = 0; i < 2; i++)
            {
                updatedMeshes[i] = new UnityEngine.Mesh();
            }
            UnityEngine.Mesh.ApplyAndDisposeWritableMeshData(meshes, updatedMeshes);

            for (int i = 0; i < 2; i++)
            {
                updatedMeshes[i].RecalculateNormals();
                updatedMeshes[i].RecalculateBounds();
            }

            RenderMeshDescription wallDesc = new RenderMeshDescription(updatedMeshes[0],  mazeMat2, ShadowCastingMode.On,true);
            EntityCommandBuffer commandBufferBegin = ecbBeginSystem.CreateCommandBuffer();
            RenderMeshUtility.AddComponents(walls, commandBufferBegin, wallDesc);
            RenderMeshDescription  floorDesc = new RenderMeshDescription(updatedMeshes[1], mazeMat1, ShadowCastingMode.On, true);
            RenderMeshUtility.AddComponents(floor, commandBufferBegin, floorDesc);
            commandBufferBegin.RemoveComponent<MeshReady>(EntityManager.GetComponentData<Parent>(floor).Value);
        }

        [BurstCompile]
        private struct CreateMesh : IJobEntityBatch
        {
            [ReadOnly]
            public EntityTypeHandle entityTypeHandle;
            [ReadOnly]
            public ComponentTypeHandle<FloorMesh> floorTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<MeshVertexInfo> vertexBufferTypeHandle;
            [ReadOnly]
            public BufferTypeHandle<MeshIndexInfo> indexbufferTypeHandle;

            public UnityEngine.Mesh.MeshDataArray meshDataArray;

            public EntityCommandBuffer ecbEnd;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                int meshIndex = 0;
                if (batchInChunk.Has(floorTypeHandle))
                {
                    meshIndex = 1;
                }

                NativeArray<VertexAttributeDescriptor> VertexDescriptors = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp);
                VertexDescriptors[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0);
                VertexDescriptors[1] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 1);

                BufferAccessor<MeshVertexInfo> vertexInfoAccessors = batchInChunk.GetBufferAccessor(vertexBufferTypeHandle);
                BufferAccessor<MeshIndexInfo> indexInfoAccessors = batchInChunk.GetBufferAccessor(indexbufferTypeHandle);
                NativeArray<Entity> meshEntities = batchInChunk.GetNativeArray(entityTypeHandle);

                for (int i = 0; i < vertexInfoAccessors.Length; i++)
                {
                    NativeArray<MeshVertexInfo> verticesUVs = vertexInfoAccessors[i].AsNativeArray();
                    NativeArray<MeshIndexInfo> indices = indexInfoAccessors[i].AsNativeArray();
                    NativeArray<float3> vertices = new NativeArray<float3>(verticesUVs.Length, 
                        Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    NativeArray<float2> UVs = new NativeArray<float2>(verticesUVs.Length, 
                        Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                    for (int v = 0; v < vertices.Length; v++)
                    {
                        vertices[v] = verticesUVs[v].vertex;
                        UVs[v] = verticesUVs[v].uv;
                    }

                    UnityEngine.Mesh.MeshData meshData = meshDataArray[meshIndex];

                    meshData.SetVertexBufferParams(vertices.Length, VertexDescriptors);
                    meshData.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);

                    meshData.GetVertexData<float3>(0).CopyFrom(vertices);
                    meshData.GetVertexData<float2>(1).CopyFrom(UVs);

                    meshData.GetIndexData<uint>().CopyFrom(indices.Reinterpret<uint>());
                    
                    meshData.subMeshCount = 1;
                    meshData.SetSubMesh(0, new SubMeshDescriptor(0, indices.Length, UnityEngine.MeshTopology.Triangles));

                    vertices.Dispose();
                    UVs.Dispose();
                    ecbEnd.SetBuffer<MeshIndexInfo>(meshEntities[i]).Clear();
                    ecbEnd.SetBuffer<MeshVertexInfo>(meshEntities[i]).Clear();
                }
                VertexDescriptors.Dispose();
            }
        }
    }
}