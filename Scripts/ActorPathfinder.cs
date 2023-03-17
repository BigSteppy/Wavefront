using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameAssets.Pathfinding.Scripts;
using UnityEditor;
using UnityEngine;


namespace Pathfinding.Scripts
{
    internal sealed class ActorPathfinder
    {
        //cell data
        private readonly Vector2Int gridSize;
        private readonly float cellRadius;
        internal FlowField FlowField;
        private readonly LayerMask difficultTerrainLayers,impassibleTerrainLayers;

        //positions
        private Vector2Int startPoint;
        private Vector2Int endPoint;
        internal Vector3 EndPointVector3 => new(endPoint.x, 1, endPoint.y);


        internal ActorPathfinder(Vector2Int gridSize, float cellRadius, LayerMask difficultTerrainLayers,
            LayerMask impassibleTerrainLayers)
        {
            this.cellRadius = cellRadius;
            this.gridSize = gridSize;
            this.difficultTerrainLayers = difficultTerrainLayers;
            this.impassibleTerrainLayers = impassibleTerrainLayers;
            
            RecalculateFlowField().Forget();
        }


        //used in live version to check if actors can reach target from intended spawn point, kept as proof of concept
        internal bool IsPathValid(Vector2Int startCoord)
        {
            var path = GetPath(startPoint, endPoint);
            if (path == null) return false;

            foreach (var cell in path)
                if (cell.Cost == byte.MaxValue)
                    return false;
            return true;
        }


        internal void UpdateEndPoint(Vector3 endpoint)
        {
            endPoint = FlowField.GetCellFromWorldPosition(endpoint).GridIndex;
            RecalculateFlowField().Forget();
        }


        //async so we can update position without freezing the entire project
        private async UniTask RecalculateFlowField()
        {
            FlowField ??= new FlowField(cellRadius, gridSize, difficultTerrainLayers, impassibleTerrainLayers);
            if (FlowField.Grid == null) FlowField.CreateGrid();
            else FlowField.ResetBestValues();

            FlowField.CreateCostField();
            FlowField.CreateIntegrationField(FlowField.Grid[endPoint.x, endPoint.y]);
            await FlowField.CreateFlowField();
        }

        private List<Cell> GetPath(Vector2Int startPos, Vector2Int endPos)
        {
            var currentCell = FlowField.Grid[startPos.x, startPos.y];
            var endCell = FlowField.Grid[endPos.x, endPos.y];
            List<Cell> results = new List<Cell>();
            while (currentCell != endCell)
            {
                if (currentCell.Cost == byte.MaxValue) return null;
                results.Add(currentCell);
                var nextBestCell = FindNextCell(currentCell);
                currentCell = FlowField.Grid[nextBestCell.x, nextBestCell.y];
            }

            foreach (var cell in results)
                Debug.DrawLine(cell.WorldPos,
                    cell.WorldPos + new Vector3(cell.BestDirection.Vector.x, 0, cell.BestDirection.Vector.y),
                    Color.blue,
                    10f);
            return results;
        }

        private Vector2Int FindNextCell(Cell currentCell)
        {
            var index = currentCell.GridIndex;
            var grid = FlowField.Grid[index.x, index.y];
            var dir = grid.BestDirection;
            return index + dir;
        }

        internal void DrawGizmos()
        {
            Gizmos.color = Color.yellow;
            foreach (var cell in FlowField.Grid)
            {
                Gizmos.DrawLine(cell.WorldPos,
                    cell.WorldPos + new Vector3(cell.BestDirection.Vector.x, 0, cell.BestDirection.Vector.y));
                Handles.Label(cell.WorldPos, cell.BestCost.ToString());
                Handles.Label(cell.WorldPos + Vector3.forward * (cellRadius / 2),
                    $"{cell.WorldPos.x},{cell.WorldPos.y},{cell.WorldPos.z}");
            }
        }
    }
}