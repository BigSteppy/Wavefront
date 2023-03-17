using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Pathfinding.Scripts;
using UnityEngine;

namespace GameAssets.Pathfinding.Scripts
{
    internal sealed class FlowField
    {
        internal Cell[,] Grid { get; private set; }
        private Vector2Int GridSize { get; }
        private float CellRadius { get; }

        private readonly float cellDiameter;

        private LayerMask difficultTerrainLayers, impassibleTerrainLayers;
        
       
        internal FlowField(float cellRadius, Vector2Int gridSize, LayerMask difficultTerrainLayers,LayerMask impassibleTerrainLayers)
        {
            CellRadius = cellRadius;
            cellDiameter = cellRadius * 2;
            GridSize = gridSize;
            this.difficultTerrainLayers = difficultTerrainLayers;
            this.impassibleTerrainLayers = impassibleTerrainLayers;
        }

        internal void CreateGrid()
        {
            Grid = new Cell[GridSize.x, GridSize.y];

            for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
            {
                var worldPos = new Vector3(cellDiameter * x + CellRadius, 0, cellDiameter * y + CellRadius);
                Grid[x, y] = new Cell(worldPos, new Vector2Int(x, y));
            }
        }

        internal void CreateCostField()
        {
            //Multiply by point 9 since the models sometime overlap
            var cellHalfExtents = Vector3.one * (CellRadius * .9f);
            

            foreach (var cell in Grid)
            {
                var obstacles = Physics.OverlapBox(cell.WorldPos, cellHalfExtents, Quaternion.identity);
                var hasIncreasedCost = false;
                foreach (var obstacle in obstacles)
                {
                    //block solid walls
                    if (!hasIncreasedCost && CompareLayerMask(obstacle.gameObject, impassibleTerrainLayers))
                    {
                        cell.AdjustCost(255);
                        continue;
                    }

                    //make difficult terrain harder
                    if (!hasIncreasedCost && CompareLayerMask(obstacle.gameObject, difficultTerrainLayers))
                    {
                        cell.AdjustCost(3);
                        hasIncreasedCost = true;
                    }
                }
            }
        }

        //async so the game doesn't freeze every time we do this
        internal async UniTask CreateFlowField()
        {
            foreach (var cell in Grid)
            {
                var neighbours = GetNeighborCells(cell.GridIndex);
                var bestCost = cell.BestCost;
                
                foreach (var neighbour in neighbours)
                {
                    //update cost if neighbour is cheaper
                    if (neighbour.BestCost < bestCost)
                    {
                        bestCost = neighbour.BestCost;
                        cell.BestDirection = GridDirection.GetDirectionFromVector2Int(neighbour.GridIndex - cell.GridIndex);
                    }
                }
            }
        }

        internal void CreateIntegrationField(Cell destination)
        {
            destination.Cost = 0;
            destination.BestCost = 0;
            var cellsToCheck = new Queue<Cell>();
            cellsToCheck.Enqueue(destination);
            while (cellsToCheck.Count > 0)
            {
                var cell = cellsToCheck.Dequeue();

                var neighbours = GetNeighborCells(cell.GridIndex);
                foreach (var neighbour in neighbours)
                {
                    if (neighbour.Cost == byte.MaxValue) continue;
                    if (neighbour.Cost + cell.BestCost < neighbour.BestCost)
                    {
                        neighbour.BestCost = (ushort) (neighbour.Cost + cell.BestCost);
                        cellsToCheck.Enqueue(neighbour);
                    }
                }
            }
        }

        
        [NotNull]
        private List<Cell> GetNeighborCells(Vector2Int index)
        {
            //Replace cardinal directions with CardinalAndInterCardinalDirections to make diagonals possible
            var directions = CheckDiagonalBlocking(index);
            var results = new List<Cell>();
            //check if cell isn't null
            //TODO: check if this is faster than simple != null check
            foreach (var direction in directions)
            {
                var result = GetCellAtRelativePosition(index, direction);
                if (result != null) results.Add(result);
            }

            return results;
        }

    
        //used to prevent paths from going through diagonal spots when both cardinal directions are blocked
        //can be removed if this behaviour is expected
        [NotNull]
        private List<GridDirection> CheckDiagonalBlocking(Vector2Int index)
        {
            //Replace cardinal directions with CardinalAndInterCardinalDirections to make diagonals possible
            var unblockedDirections = new List<GridDirection>();
            foreach (var direction in GridDirection.CardinalDirections)
            {
                var cell = GetCellAtRelativePosition(index, direction);
                if (cell == null) continue;
                if (cell.Cost != byte.MaxValue) unblockedDirections.Add(direction);
            }

            var north = unblockedDirections.Contains(GridDirection.North);
            var south = unblockedDirections.Contains(GridDirection.South);
            var east = unblockedDirections.Contains(GridDirection.East);
            var west = unblockedDirections.Contains(GridDirection.West);
            if (north && east) unblockedDirections.Add(GridDirection.NorthEast);
            if (north && west) unblockedDirections.Add(GridDirection.NorthWest);
            if (south && east) unblockedDirections.Add(GridDirection.SouthEast);
            if (south && west) unblockedDirections.Add(GridDirection.SouthWest);
            return unblockedDirections;
        }

        private Cell GetCellAtRelativePosition(Vector2Int index, Vector2Int direction)
        {
            var finalIndex = index + direction;
            
            //if out of bounds
            if (finalIndex.x < 0 || finalIndex.x > GridSize.x - 1 || finalIndex.y < 0 || finalIndex.y > GridSize.y - 1)
                return null;
            return Grid[finalIndex.x, finalIndex.y];
        }

        internal Cell GetCellFromWorldPosition(Vector3 worldPosition)
        {
            var percentX = worldPosition.x / GridSize.x / cellDiameter;
            var percentY = worldPosition.z / GridSize.y / cellDiameter;

            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            var x = Mathf.Clamp(Mathf.FloorToInt((GridSize.x) * percentX), 0, GridSize.x - 1);
            var y = Mathf.Clamp(Mathf.FloorToInt((GridSize.y) * percentY), 0, GridSize.y - 1);
            return Grid[x, y];
        }

        internal void ResetBestValues()
        {
            foreach (var cell in Grid)
            {
                cell.Cost = 1;
                cell.BestCost = ushort.MaxValue;
            }
        }

        private bool CompareLayerMask(GameObject obj, LayerMask layerMask)
        {
            return ((layerMask.value & (1 << obj.layer)) > 0);
        }
    }
}