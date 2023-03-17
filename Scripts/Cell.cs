using GameAssets.Pathfinding.Scripts;
using UnityEngine;

namespace Pathfinding.Scripts
{
    internal sealed class Cell
    {
        //Position in game space
        internal Vector3 WorldPos;
        //position in the array, almost always the same as WorldPos
        internal Vector2Int GridIndex;
        //cost to move through this tile
        internal byte Cost;
        //best cost 
        internal ushort BestCost;
        //direction to move if you end up here
        internal GridDirection BestDirection;

        internal Cell(Vector3 worldPos, Vector2Int gridIndex)
        {
            WorldPos = worldPos;
            GridIndex = gridIndex;
            Cost = 1;
            BestCost = ushort.MaxValue;
            BestDirection = GridDirection.None;
        }

        internal void AdjustCost(int amount)
        {
            if (Cost == byte.MaxValue) return;
            if (Cost + amount >= 255) Cost = byte.MaxValue;
            else Cost += (byte)amount;
        }
    }
}