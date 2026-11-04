using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefendAndCollect.Agents
{
    public static class Perception
    {
        public static float Distance(Vector2Int from, Vector2Int to)
        {
            int deltaX = to.x - from.x;
            int deltaY = to.y - from.y;
            return Mathf.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        public static bool IsWithinRadius(Vector2Int from, Vector2Int to, float radius)
        {
            return Distance(from, to) <= radius;
        }

        public static T FindNearest<T>(
            Vector2Int origin,
            IReadOnlyList<T> candidates,
            Func<T, Vector2Int> cellSelector,
            float radius,
            Func<T, bool> isEligible = null)
            where T : class
        {
            if (candidates == null)
            {
                return null;
            }

            T nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                T candidate = candidates[i];

                if (candidate == null)
                {
                    continue;
                }

                if (isEligible != null && !isEligible(candidate))
                {
                    continue;
                }

                float distance = Distance(origin, cellSelector(candidate));

                if (distance > radius || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest;
        }
    }
}
