using System.Linq;
using DefendAndCollect.Agents;
using DefendAndCollect.Build;
using DefendAndCollect.Grid;
using DefendAndCollect.Pathfinding;
using DefendAndCollect.Scenarios;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefendAndCollect.Tests.EditMode
{
    public sealed class MainSceneTests
    {
        [Test]
        public void MainScene_ContainsGridBootstrapOnGameRoot()
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);

            try
            {
                GameObject gameRoot = scene.GetRootGameObjects().Single(go => go.name == "GameRoot");

                Assert.That(gameRoot.GetComponent<GridBootstrap>(), Is.Not.Null);
                Assert.That(gameRoot.GetComponent<PathfindingDebugController>(), Is.Not.Null);
                Assert.That(gameRoot.GetComponent<FsmDemoBootstrap>(), Is.Not.Null);
                Assert.That(gameRoot.GetComponent<BuildController>(), Is.Not.Null);
                Assert.That(gameRoot.GetComponent<ScenarioController>(), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
