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
			"5_AdvancedGrid/5_AdvancedGridScene", "6_AdvancedTable/6_AdvancedTableScene", "7_SnappingTable/7_SnappingTableScene", "8_InfiniteLoop/8_InfiniteLoopScene"
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
					foreach (var root in scene.GetRootGameObjects()) {
						var simple = root.GetComponentInChildren<UIKit.Samples.SimpleTableScene>();
						if (simple != null) {
							var table = simple.GetComponentInChildren<UITableView>();
							Assert.That(table.enableInfiniteLoop, Is.False, "SimpleTable must retain its finite behavior.");
							Assert.That(simple.NumberOfCellsInTableView(table), Is.EqualTo(200));
						}
						var loop = root.GetComponentInChildren<UIKit.Samples.InfiniteLoopTableScene>();
						if (loop != null) {
							var table = loop.GetComponentInChildren<UITableView>();
							Assert.That(table.enableInfiniteLoop, Is.True);
							Assert.That(loop.NumberOfCellsInTableView(table), Is.EqualTo(7));
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
