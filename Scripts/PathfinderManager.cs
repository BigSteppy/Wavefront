using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pathfinding.Scripts
{
    //the only MonoBehaviour,
    //this could easily be broken into several smaller MonoBehaviours as required
    internal sealed class PathfinderManager : MonoBehaviour
    {
        //actor stats
        [SerializeField] private Rigidbody actorPrefab, ghostPrefab;
        [SerializeField] private float actorSpeed = 5f;
        [SerializeField] private int actorsPerKeyPress = 10, ghostActorsPerKeyPress = 1;

        //grid stats
        [SerializeField] private Vector2Int gridSize = new(50, 50);
        [SerializeField] private float cellRadius = 0.5f;

        private float cellDiameter;
        //actor lists
        private readonly List<Rigidbody> actors = new(), actorsToDestroy = new();

        
        // you cannot use LayerMask.GetMask on fields, but usually these would be called DifficultTerrain and ImpassibleTerrain
        private string ghostActorTag;
        [SerializeField] private LayerMask difficultTerrainMask, impassibleTerrainMask;

        //debug
        [SerializeField] private bool showDebug;

        //pathfinder
        private ActorPathfinder pathfinder;

        private Mouse mouse => Mouse.current;
        private Keyboard keyboard => Keyboard.current;

        private void Awake()
        {
            //setup pathfinder
            pathfinder = new ActorPathfinder(gridSize, cellRadius,difficultTerrainMask,impassibleTerrainMask);
            
            //doing it this way prevents issues if we want to make changes to tags later
            //tags don't have anywhere near the support of layers
            ghostActorTag = ghostPrefab.tag;

            //cheaper than calling it as a expression
            cellDiameter = cellRadius * 2;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                for (int i = 0; i < actorsPerKeyPress; i++) InstantiateActor();
                for (int i = 0; i < ghostActorsPerKeyPress; i++) InstantiateActor(true);
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                var mousePos = Mouse.current.position.ReadValue();
                var ray = Camera.main!.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out var hit))
                    pathfinder.UpdateEndPoint(hit.point);
            }
        }

        void OnDrawGizmos()
        {
            //kept behind a bool as drawing this many debugs will lower framerates considerably
            if (!showDebug || pathfinder.FlowField.Grid == null) return;
            pathfinder.DrawGizmos();
        }
#endif

        private void FixedUpdate()
        {
            //keep a list of actors to destroy afterwards, as destroying them during a foreach loop would break it
            actorsToDestroy.Clear();
            foreach (var actor in actors)
            {
                var nodeBelow = pathfinder.FlowField.GetCellFromWorldPosition(actor.transform.position);
                if (nodeBelow.BestCost <= 1)
                {
                    actorsToDestroy.Add(actor);
                    continue;
                }

                Vector3 direction;
                //if ghost actor (which can walk through walls)
                if (actor.CompareTag(ghostActorTag))
                    direction = (pathfinder.EndPointVector3 * cellDiameter - actor.transform.position)
                        .normalized * actorSpeed;
                else
                    direction = new Vector3(nodeBelow.BestDirection.Vector.x, 0, nodeBelow.BestDirection.Vector.y)
                        .normalized * actorSpeed;
                actor.velocity = direction;
                actor.transform.LookAt(actor.transform.position + direction);
            }

            //destroy GameObjects after they reach destination
            foreach (var rb in actorsToDestroy)
            {
                Destroy(rb.gameObject);
                actors.Remove(rb);
            }
        }

        private void InstantiateActor(bool isGhost = false)
        {
            var x = Random.Range(0, gridSize.x);
            var z = Random.Range(0, gridSize.y);
            var prefab = actorPrefab;
            if (isGhost) prefab = ghostPrefab;
            actors.Add(Instantiate(prefab, new Vector3(x, 1, z) * cellDiameter , Quaternion.identity));
        }
    }
}