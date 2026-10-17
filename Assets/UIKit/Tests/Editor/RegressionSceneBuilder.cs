using UIKit.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIKit.Tests
{
	public static class RegressionSceneBuilder
	{
		[MenuItem("UIKit/Tests/Create Regression Scene")]
		public static void CreateScene()
		{
			var previous = SceneManager.GetActiveScene();
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
			var root = new GameObject("UITableView Regression Checks");
			SceneManager.MoveGameObjectToScene(root, scene);
			root.AddComponent<UITableViewRegressionScene>();
			EditorSceneManager.SaveScene(scene, UITableViewRegressionScene.ScenePath);
			if (!Application.isBatchMode) {
				EditorSceneManager.CloseScene(scene, true);
				if (previous.IsValid()) SceneManager.SetActiveScene(previous);
			}
			AssetDatabase.SaveAssets();
			Debug.Log("Created " + UITableViewRegressionScene.ScenePath);
		}
	}
}
