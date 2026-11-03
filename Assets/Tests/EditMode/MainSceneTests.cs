using System.Linq;
using DefendAndCollect.Grid;
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
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
