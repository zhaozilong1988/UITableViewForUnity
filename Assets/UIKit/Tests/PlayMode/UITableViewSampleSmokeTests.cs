using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UIKit.Tests
{
	public class UITableViewSampleSmokeTests
	{
		static readonly string[] SampleScenes = {
			"0_SimpleTable/0_SimpleTableScene", "1_Chat/1_ChatScene", "2_Sns/2_SnsScene",
			"3_SimpleGrid/3_SimpleGridScene", "4_NetflixLike/4_NetflixLikeScene",
			"5_AdvancedGrid/5_AdvancedGridScene", "6_AdvancedTable/6_AdvancedTableScene", "7_SnappingTable/7_SnappingTableScene"
		};

		[UnityTest]
		public IEnumerator ExistingSamplesInitializeAndLoadCells()
		{
			foreach (var sample in SampleScenes) {
				var path = "Assets/UIKit/Samples/" + sample + ".unity";
				Debug.Log("Loading existing sample: " + path);
#if UNITY_EDITOR
				yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
#else
				yield return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
#endif
				var scene = SceneManager.GetSceneByPath(path);
				try {
					yield return null;
					yield return null;
					var hasLoadedCell = false;
					foreach (var root in scene.GetRootGameObjects()) {
						foreach (var table in root.GetComponentsInChildren<UITableView>()) {
							foreach (var cell in table.GetAllLoadedCells()) {
								hasLoadedCell |= cell != null;
							}
						}
					}
					Assert.That(hasLoadedCell, Is.True, "Sample must initialize its table and display cells: " + path);
				} finally {
					SceneManager.UnloadSceneAsync(scene);
				}
				yield return null;
			}
		}
	}
}
