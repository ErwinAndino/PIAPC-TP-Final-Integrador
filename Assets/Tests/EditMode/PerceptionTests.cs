using DefendAndCollect.Agents;
using DefendAndCollect.World;
using NUnit.Framework;
using UnityEngine;

namespace DefendAndCollect.Tests.EditMode
{
    public sealed class PerceptionTests
    {
        [Test]
        public void Distance_UsesEuclideanMetric()
        {
            Assert.That(Perception.Distance(new Vector2Int(0, 0), new Vector2Int(3, 4)), Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void IsWithinRadius_IncludesCandidatesOnTheEdge()
        {
            Assert.That(Perception.IsWithinRadius(new Vector2Int(0, 0), new Vector2Int(3, 4), 5f), Is.True);
            Assert.That(Perception.IsWithinRadius(new Vector2Int(0, 0), new Vector2Int(3, 4), 4.99f), Is.False);
        }

        [Test]
        public void FindNearest_ReturnsClosestEligibleCandidate()
        {
            ResourceNode farther = new ResourceNode(new Vector2Int(5, 0));
            ResourceNode taken = new ResourceNode(new Vector2Int(1, 0));
            ResourceNode nearest = new ResourceNode(new Vector2Int(2, 1));
            taken.MarkTaken();

            ResourceNode result = Perception.FindNearest(
                new Vector2Int(0, 0),
                new[] { farther, taken, nearest },
                node => node.Cell,
                6f,
                node => node.IsAvailable);

            Assert.That(result, Is.SameAs(nearest));
        }

        [Test]
        public void FindNearest_IgnoresCandidatesOutsideRadius()
        {
            ResourceNode node = new ResourceNode(new Vector2Int(5, 0));

            ResourceNode result = Perception.FindNearest(
                new Vector2Int(0, 0),
                new[] { node },
                candidate => candidate.Cell,
                4.99f);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindNearest_WithoutCandidates_ReturnsNull()
        {
            ResourceNode result = Perception.FindNearest(
                new Vector2Int(0, 0),
                new ResourceNode[0],
                node => node.Cell,
                10f);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindNearest_WithNullList_ReturnsNull()
        {
            ResourceNode result = Perception.FindNearest<ResourceNode>(
                new Vector2Int(0, 0),
                null,
                node => node.Cell,
                10f);

            Assert.That(result, Is.Null);
        }
    }
}
