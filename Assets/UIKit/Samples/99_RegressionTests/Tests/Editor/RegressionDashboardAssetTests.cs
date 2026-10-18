using System.Reflection;
using NUnit.Framework;
using UIKit;
using UIKit.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UIKit.Tests
{
	public class RegressionDashboardAssetTests
	{
		[Test]
		public void AuthoredSceneAndCellRetainCompleteReferences()
		{
			Assert.That(AssetDatabase.AssetPathToGUID(UITableViewRegressionScene.ScenePath), Is.EqualTo("d4a779a5adc63da49997aab0c310accb"));
			var scene = EditorSceneManager.OpenPreviewScene(UITableViewRegressionScene.ScenePath);
			try {
				UITableViewRegressionScene runner = null;
				foreach (var root in scene.GetRootGameObjects()) {
					foreach (var transform in root.GetComponentsInChildren<Transform>(true))
						Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
					runner = root.GetComponent<UITableViewRegressionScene>();
					if (runner != null) break;
				}
				Assert.That(runner, Is.Not.Null);
				var dashboard = runner.GetComponentInChildren<RegressionDashboard>(true);
				Assert.That(dashboard, Is.Not.Null);
				CheckReferences(runner);
				CheckReferences(dashboard);
				foreach (var button in dashboard.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
					Assert.That(button.name, Is.Not.EqualTo("ToggleInteraction"));
					Assert.That(button.name, Is.Not.EqualTo("PreviewFit"));
					Assert.That(button.name, Is.Not.EqualTo("PreviewZoom"));
				}
				Assert.That(RegressionDashboard.Titles.Length, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length));
				Assert.That(RegressionDashboard.Descriptions.Length, Is.EqualTo(RegressionDashboard.Titles.Length));
				Assert.That(RegressionDashboard.Expectations.Length, Is.EqualTo(RegressionDashboard.Titles.Length));
				var data = new SerializedObject(dashboard);
				var summary = (UnityEngine.UI.Text)data.FindProperty("_summary").objectReferenceValue;
				Assert.That(summary.text, Does.Contain("PENDING " + UITableViewRegressionScene.CaseNames.Length), "The saved scene must match the current case catalog.");
				var table = (UITableView)data.FindProperty("_table").objectReferenceValue;
				var scroll = table.GetComponent<UnityEngine.UI.ScrollRect>();
				Assert.That(scroll.viewport, Is.Not.Null);
				Assert.That(scroll.content.parent, Is.EqualTo(scroll.viewport));
				Assert.That(scroll.horizontal, Is.False);
				var cell = (RegressionResultCell)data.FindProperty("_cellPrefab").objectReferenceValue;
				Assert.That(AssetDatabase.GetAssetPath(cell), Is.EqualTo("Assets/UIKit/Samples/99_RegressionTests/Prefabs/RegressionResultCell.prefab"));
				CheckReferences(cell);
				var rowObject = (GameObject)PrefabUtility.InstantiatePrefab(cell.gameObject, scene);
				try {
					var row = rowObject.GetComponent<RegressionResultCell>();
					rowObject.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 116);
					var label = (UnityEngine.UI.Text)new SerializedObject(row).FindProperty("_title").objectReferenceValue;
					for (var i = 0; i < RegressionDashboard.Titles.Length; i++) {
						row.Bind($"{i + 1:00}  {RegressionDashboard.Titles[i]}", RegressionCaseState.Passed, false, true, () => { });
						Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height), "The authored result row must show its full name: " + label.text);
					}
				} finally { Object.DestroyImmediate(rowObject); }
				var frame = (RectTransform)data.FindProperty("_viewportFrame").objectReferenceValue;
				Assert.That(frame.GetComponentsInChildren<UnityEngine.UI.Image>().Length, Is.EqualTo(4), "Viewport annotation needs a hollow frame, so it does not cover the table or empty-state text.");
				foreach (var image in frame.GetComponentsInChildren<UnityEngine.UI.Image>()) Assert.That(image.raycastTarget, Is.False);
			} finally { EditorSceneManager.ClosePreviewScene(scene); }
		}

		static void CheckReferences(Object target)
		{
			var serialized = new SerializedObject(target);
			foreach (var field in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
				if (!field.IsDefined(typeof(SerializeField), false) || !typeof(Object).IsAssignableFrom(field.FieldType)) continue;
				Assert.That(serialized.FindProperty(field.Name).objectReferenceValue, Is.Not.Null, target.name + "." + field.Name);
			}
		}
	}
}
