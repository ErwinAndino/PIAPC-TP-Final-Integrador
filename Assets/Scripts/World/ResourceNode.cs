using UnityEngine;

namespace DefendAndCollect.World
{
    public sealed class ResourceNode
    {
        public ResourceNode(Vector2Int cell)
        {
            Cell = cell;
        }

        public Vector2Int Cell { get; }

        public bool IsAvailable { get; private set; } = true;

        public void MarkTaken()
        {
            IsAvailable = false;
        }
    }
}
