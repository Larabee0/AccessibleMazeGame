using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;

namespace DOTSMaze
{
    public class DOTSGameController : MonoBehaviour
    {
        EntityManager EntityManager;

        [SerializeField] private Material mazeMat1;
        [SerializeField] private Material mazeMat2;

        EntityArchetype mazeRoot;
        Entity maze;

        private void Awake()
        {
            EntityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            AssignMazeMeshSystem.mazeMat1 = mazeMat1;
            AssignMazeMeshSystem.mazeMat2 = mazeMat2;
            mazeRoot = EntityManager.CreateArchetype(typeof(CreateMaze), typeof(Cell),
                typeof(Translation), typeof(LocalToWorld), typeof(Child));
        }

        private void Start()
        {
            maze = EntityManager.CreateEntity(mazeRoot);
            EntityManager.SetComponentData(maze, new CreateMaze { CellsZ = 13, CellsX = 15 });
        }
    }
}

