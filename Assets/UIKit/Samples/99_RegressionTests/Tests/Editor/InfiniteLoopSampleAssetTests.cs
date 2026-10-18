using NUnit.Framework;
using UIKit.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UIKit.Tests
{
    public class InfiniteLoopSampleAssetTests
    {
        [Test]
        public void IndependentLoopSceneHasCompleteAuthoredReferences()
        {
            const string folder = "Assets/UIKit/Samples/8_InfiniteLoop";
            var scenePath = folder + "/8_InfiniteLoopScene.unity";
            foreach (var path in AssetDatabase.GetDependencies(scenePath, true))
                Assert.That(path, Does.Not.StartWith("Assets/UIKit/Samples/0_SimpleTable/"), "The loop example must work independently of the SimpleTable sample.");
            var scene = EditorSceneManager.OpenPreviewScene(scenePath);
            try {
                InfiniteLoopTableScene controller = null;
                foreach (var root in scene.GetRootGameObjects()) {
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
                    var found = root.GetComponentInChildren<InfiniteLoopTableScene>(true);
                    if (found != null) controller = found;
                }
                Assert.That(controller, Is.Not.Null);
                var data = new SerializedObject(controller);
                Assert.That(data.FindProperty("_numberOfCells").intValue, Is.EqualTo(7));
                var table = (UITableView)data.FindProperty("_tableView").objectReferenceValue;
                Assert.That(table, Is.Not.Null);
                Assert.That(table.enableInfiniteLoop, Is.True, "The authored Inspector state must match runtime intent.");
                Assert.That(table.scrollRect.viewport, Is.Not.Null);
                Assert.That(table.scrollRect.content.parent, Is.SameAs(table.scrollRect.viewport));
                var prefab = (InfiniteLoopCell)data.FindProperty("_cellPrefab").objectReferenceValue;
                Assert.That(prefab, Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(prefab), Is.EqualTo(folder + "/Prefabs/InfiniteLoopCell.prefab"));
                Assert.That(prefab.text != null && prefab.icon != null && prefab.background != null, Is.True);
                var icons = data.FindProperty("_iconSprites"); Assert.That(icons.arraySize, Is.EqualTo(4));
                for (var i = 0; i < icons.arraySize; i++) {
                    var icon = icons.GetArrayElementAtIndex(i).objectReferenceValue;
                    Assert.That(icon, Is.Not.Null);
                    Assert.That(AssetDatabase.GetAssetPath(icon), Does.StartWith(folder + "/Images/"));
                }
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
