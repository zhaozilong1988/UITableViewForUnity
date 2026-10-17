using System.Collections;
using NUnit.Framework;
using UIKit.Samples;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UIKit.Tests
{
	public class UITableViewRegressionTests
	{
		Scene _scene;
		UITableViewRegressionScene _runner;
		static readonly UITableViewDirection[] Directions = {
			UITableViewDirection.TopToBottom, UITableViewDirection.BottomToTop,
			UITableViewDirection.RightToLeft, UITableViewDirection.LeftToRight
		};

		[UnitySetUp]
		public IEnumerator LoadReproductionScene()
		{
#if UNITY_EDITOR
			yield return EditorSceneManager.LoadSceneAsyncInPlayMode(UITableViewRegressionScene.ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
			yield return SceneManager.LoadSceneAsync(UITableViewRegressionScene.ScenePath, LoadSceneMode.Additive);
#endif
			_scene = SceneManager.GetSceneByPath(UITableViewRegressionScene.ScenePath);
			foreach (var root in _scene.GetRootGameObjects()) {
				_runner = root.GetComponent<UITableViewRegressionScene>();
				if (_runner != null) break;
			}
			Assert.That(_runner, Is.Not.Null);
			yield return null;
		}

		[UnityTearDown]
		public IEnumerator UnloadReproductionScene()
		{
			if (_scene.IsValid() && _scene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scene);
			yield return null;
		}

		IEnumerator CheckAllDirections(int index)
		{
			foreach (var direction in Directions) {
				Debug.Log($"Checking {UITableViewRegressionScene.CaseNames[index]} / {direction}");
				yield return _runner.RunCase(index, direction);
			}
		}

		[UnityTest] public IEnumerator AppendMakesNewIndexAccessible() => CheckAllDirections(0);
		[UnityTest] public IEnumerator PrependMakesShiftedIndexAccessible() => CheckAllDirections(1);
		[UnityTest] public IEnumerator ShortInfiniteLoopKeepsRoomOnBothSides() => CheckAllDirections(2);
		[UnityTest] public IEnumerator InfiniteGridPreservesIncompleteRows() => CheckAllDirections(3);
		[UnityTest] public IEnumerator PrependGridDoesNotDuplicateRowMargins() => CheckAllDirections(4);
		[UnityTest] public IEnumerator ZeroPrependPreservesRetainedOffscreenCells() => CheckAllDirections(5);
		[UnityTest] public IEnumerator EmptyTableCanBeRearranged() => CheckAllDirections(6);
		[UnityTest] public IEnumerator EmptyTableDoesNotStartMagneticScroll() => CheckAllDirections(7);
		[UnityTest] public IEnumerator EqualContentAndViewportReturnFinitePosition() => CheckAllDirections(8);

		[UnityTest]
		public IEnumerator RunAllButtonExecutesTheSceneChecks()
		{
			var buttons = _runner.GetComponentsInChildren<UnityEngine.UI.Button>();
			Assert.That(buttons.Length, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length + 1));
			buttons[0].onClick.Invoke();
			while (_runner.isRunning) yield return null;
			Assert.That(_runner.failedCount, Is.Zero);
			Assert.That(_runner.passedCount, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length));
		}
	}
}
