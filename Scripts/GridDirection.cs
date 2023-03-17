using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace GameAssets.Pathfinding.Scripts
{
    internal sealed class GridDirection
    {
        //Remember that y is z in most cases
        internal readonly Vector2Int Vector;

        private GridDirection(int x, int y) => Vector = new Vector2Int(x, y);

        //GridDirection to Vector2
        public static implicit operator Vector2Int([NotNull] GridDirection direction) => direction.Vector;


        //Vector2 to GridDirection 
        [NotNull]
        internal static GridDirection GetDirectionFromVector2Int(Vector2Int vector) => CardinalAndInterCardinalDirections.DefaultIfEmpty(None).FirstOrDefault(direction => direction == vector)!;

    

        internal static readonly GridDirection None = new(0, 0);
        internal static readonly GridDirection North = new(0, 1);
        internal static readonly GridDirection South = new(0, -1);
        internal static readonly GridDirection East = new(1, 0);
        internal static readonly GridDirection West = new(-1, 0);
        internal static readonly GridDirection NorthEast = new(1, 1);
        internal static readonly GridDirection NorthWest = new(-1, 1);
        internal static readonly GridDirection SouthEast = new(1, -1);
        internal static readonly GridDirection SouthWest = new(-1, -1);

        internal static readonly List<GridDirection> CardinalDirections = new()
        {
            North, East, South, West
        };

        internal static readonly List<GridDirection> CardinalAndInterCardinalDirections = new()
        {
            North,East, South,West, NorthEast,SouthEast, SouthWest,NorthWest
        };
    }
}
