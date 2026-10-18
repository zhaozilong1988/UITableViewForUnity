using System.Collections;
using System.Reflection;
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
	public partial class UITableViewRegressionTests
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

		public static System.Collections.Generic.IEnumerable<string> BasicCases {
			get { for (var i = UITableViewRegressionScene.RegressionCaseCount; i < UITableViewRegressionScene.CaseNames.Length; i++) yield return UITableViewRegressionScene.CaseNames[i]; }
		}
		[UnityTest]
		public IEnumerator BasicFeatureWorksInAllDirections([ValueSource(nameof(BasicCases))] string caseName) => CheckAllDirections(System.Array.IndexOf(UITableViewRegressionScene.CaseNames, caseName));

		[UnityTest]
		public IEnumerator DirectionButtonResetsResultsAndRunsTheSelectedDirection()
		{
			_runner.RunSelectedCase();
			yield return WaitForRun();
			Assert.That(_runner.passedCount, Is.EqualTo(1));
			var dashboard = _runner.GetComponentInChildren<RegressionDashboard>();
			var button = Field<UnityEngine.UI.Button>(dashboard, "_direction");
			var seen = new System.Collections.Generic.HashSet<UITableViewDirection> { _runner.testDirection };
			for (var i = 0; i < 3; i++) {
				button.onClick.Invoke();
				Assert.That(seen.Add(_runner.testDirection), Is.True);
				Assert.That(_runner.pendingCount, Is.EqualTo(_runner.results.Count));
				Assert.That(_runner.selectedPreview, Is.Null);
				_runner.RunSelectedCase();
				Assert.That(button.interactable, Is.False);
				yield return WaitForRun();
				Assert.That(_runner.failedCount, Is.Zero);
				Assert.That(_runner.selectedPreview.table.direction, Is.EqualTo(_runner.testDirection));
			}
			button.onClick.Invoke();
			Assert.That(_runner.testDirection, Is.EqualTo(UITableViewDirection.TopToBottom));
		}

		[UnityTest]
		public IEnumerator DashboardStartsWithAllPendingChecksAndNoPreview()
		{
			Assert.That(_runner.results.Count, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length));
			Assert.That(_runner.pendingCount, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length));
			Assert.That(_runner.passedCount, Is.Zero);
			Assert.That(_runner.failedCount, Is.Zero);
			Assert.That(_runner.selectedPreview, Is.Null);
			var dashboard = _runner.GetComponentInChildren<RegressionDashboard>();
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_summary").text, Does.Contain($"PENDING {UITableViewRegressionScene.CaseNames.Length}"));
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_emptyPreview").gameObject.activeInHierarchy, Is.True);
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_emptyPreview").text, Does.Contain("Run this check"));
			foreach (var name in new[] { "_runAll", "_direction", "_rerun", "_interactionToggle", "_fit", "_zoom" }) {
				var label = Field<UnityEngine.UI.Button>(dashboard, name).GetComponentInChildren<UnityEngine.UI.Text>();
				Assert.That(label.text, Is.Not.Empty, name);
				Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height), name + " label must fit vertically");
			}
			yield return null;
		}

		[UnityTest]
		public IEnumerator ResultRowsRemainSelectableAfterScrollingAndRecycling()
		{
			var lastIndex = UITableViewRegressionScene.CaseNames.Length - 1;
			var dashboard = _runner.GetComponentInChildren<RegressionDashboard>();
			var table = Field<UITableView>(dashboard, "_table");
			var lastResult = _runner.results[lastIndex];
			table.ScrollToCellAt(lastIndex);
			yield return WaitForVisibleRow(table, lastIndex);
			var last = table.GetLoadedCell<RegressionResultCell>(lastIndex);

			Assert.That(last, Is.Not.Null, "The final check must be reachable by scrolling.");
			Assert.That(Field<UnityEngine.UI.Text>(last, "_title").text, Does.Contain(RegressionDashboard.Titles[lastIndex]));
			Field<UnityEngine.UI.Button>(last, "_select").onClick.Invoke();
			Assert.That(_runner.selectedCase, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length - 1));
			table.ScrollToCellAt(0);
			yield return WaitForVisibleRow(table, 0);
			var first = table.GetLoadedCell<RegressionResultCell>(0);
			Assert.That(first, Is.Not.Null);
			Assert.That(Field<UnityEngine.UI.Text>(first, "_title").text, Does.Contain(RegressionDashboard.Titles[0]));
			Field<UnityEngine.UI.Button>(first, "_select").onClick.Invoke();
			Assert.That(_runner.selectedCase, Is.Zero);
			table.ScrollToCellAt(lastIndex);
			yield return WaitForVisibleRow(table, lastIndex);
			last = table.GetLoadedCell<RegressionResultCell>(lastIndex);
			Assert.That(last, Is.Not.Null);
			Field<UnityEngine.UI.Button>(last, "_select").onClick.Invoke();
			Assert.That(_runner.selectedCase, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length - 1), "Recycled row buttons must bind the displayed case.");
			Assert.That(_runner.results[lastIndex], Is.SameAs(lastResult));
			Assert.That(_runner.pendingCount, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length));
			Assert.That(_runner.isRunning, Is.False);
			Assert.That(_runner.selectedPreview, Is.Null, "Selecting rows must not execute their checks.");
		}

		[UnityTest]
		public IEnumerator RunAllButtonExecutesTheSceneChecks([ValueSource(nameof(Directions))] UITableViewDirection direction)
		{
			while (_runner.testDirection != direction) _runner.CycleDirection();
			UnityEngine.UI.Button runAll = null;
			foreach (var button in _runner.GetComponentsInChildren<UnityEngine.UI.Button>())
				if (button.name == "RunAll") runAll = button;
			Assert.That(runAll, Is.Not.Null, "The authored RunAll button must be wired.");
			runAll.onClick.Invoke();
			yield return WaitForRun();
			Assert.That(_runner.failedCount, Is.Zero);
			Assert.That(_runner.passedCount, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length));
			Assert.That(_runner.pendingCount, Is.Zero);
			var lastPreview = _runner.selectedPreview;
			var firstResult = _runner.results[0];
			Assert.That(firstResult.state, Is.EqualTo(RegressionCaseState.Passed));
			Assert.That(firstResult.actual, Does.Contain("#10 loaded: True"));
			_runner.SelectCase(0);
			var firstPreview = _runner.selectedPreview;
			Assert.That(firstPreview, Is.Not.Null);
			Assert.That(firstPreview.root.activeSelf, Is.True);
			Assert.That(lastPreview.root.activeSelf, Is.False);
			_runner.SelectCase(UITableViewRegressionScene.CaseNames.Length - 1);
			Assert.That(_runner.selectedPreview, Is.SameAs(lastPreview));
			_runner.SelectCase(0);
			yield return null;
			Assert.That(_runner.selectedPreview, Is.SameAs(firstPreview), "Selecting a completed check must reuse its retained table.");
			Assert.That(_runner.results[0], Is.SameAs(firstResult), "Selection must not rerun the check.");
			Assert.That(_runner.isRunning, Is.False);
		}

		[UnityTest]
		public IEnumerator RerunSelectedPreservesOtherResultsAndCounts()
		{
			_runner.SelectCase(0);
			_runner.RunSelectedCase();
			yield return WaitForRun();
			var firstPreview = _runner.selectedPreview;
			var firstResult = _runner.results[0];
			_runner.SelectCase(1);
			_runner.RunSelectedCase();
			yield return WaitForRun();
			var otherPreview = _runner.selectedPreview;
			var otherResult = _runner.results[1];
			var pendingResult = _runner.results[8];
			_runner.SelectCase(0);
			var dashboard = _runner.GetComponentInChildren<RegressionDashboard>();
			Field<UnityEngine.UI.Button>(dashboard, "_rerun").onClick.Invoke();
			yield return WaitForRun();
			Assert.That(_runner.passedCount, Is.EqualTo(2));
			Assert.That(_runner.failedCount, Is.Zero);
			Assert.That(_runner.pendingCount, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length - 2));
			Assert.That(_runner.results[0], Is.Not.SameAs(firstResult));
			Assert.That(_runner.selectedPreview, Is.Not.SameAs(firstPreview));
			Assert.That(_runner.results[1], Is.SameAs(otherResult));
			Assert.That(_runner.results[8], Is.SameAs(pendingResult));
			_runner.SelectCase(1);
			Assert.That(_runner.selectedPreview, Is.SameAs(otherPreview));
		}

		[UnityTest]
		public IEnumerator FailedCheckShowsMeasuredAndExpectedValuesAndSortsFirst()
		{
			_runner.SelectCase(4);
			_runner.RunSelectedCase();
			Assert.That(_runner.isRunning, Is.True);
			UITableViewRegressionFixture fixture = null;
			foreach (var table in Object.FindObjectsOfType<RegressionTableView>())
				if (table.dataSource is UITableViewRegressionFixture candidate) fixture = candidate;
			Assert.That(fixture, Is.Not.Null, "The running grid fixture must exist before its first yielded frame finishes.");
			fixture.margin = 30f;
			yield return WaitForRun();
			Assert.That(_runner.failedCount, Is.EqualTo(1));
			Assert.That(_runner.passedCount, Is.Zero);
			Assert.That(_runner.pendingCount, Is.EqualTo(UITableViewRegressionScene.CaseNames.Length - 1));
			Assert.That(_runner.results[4].state, Is.EqualTo(RegressionCaseState.Failed));
			Assert.That(_runner.results[4].actual, Does.Contain("Content length: 480"));
			Assert.That(_runner.results[4].failure, Does.Contain("360").And.Contain("480"));
			Assert.That(_runner.selectedPreview, Is.SameAs(fixture));
			var dashboard = _runner.GetComponentInChildren<RegressionDashboard>();
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_expected").text, Does.Contain("360"));
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_actual").text, Does.Contain("480"));
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_status").text, Does.StartWith("FAIL"));
			var row = Field<UITableView>(dashboard, "_table").GetLoadedCell<RegressionResultCell>(0);
			Assert.That(row, Is.Not.Null);
			Assert.That(Field<UnityEngine.UI.Text>(row, "_title").text, Does.Contain(RegressionDashboard.Titles[4]));
			Assert.That(Field<UnityEngine.UI.Text>(row, "_status").text, Does.StartWith("FAIL"));
			Field<UnityEngine.UI.Button>(dashboard, "_errorToggle").onClick.Invoke();
			Assert.That(Field<GameObject>(dashboard, "_errorPanel").activeInHierarchy, Is.True);
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_failure").text, Is.EqualTo(_runner.results[4].failure));
		}

		[UnityTest]
		public IEnumerator NestedBasicFailureIsCapturedInTheDashboard()
		{
			var index = UITableViewRegressionScene.RegressionCaseCount + 1;
			_runner.SelectCase(index);
			_runner.RunSelectedCase();
			BasicTableViewFixture fixture = null;
			foreach (var table in Object.FindObjectsOfType<RegressionTableView>())
				if (table.@delegate is BasicTableViewFixture candidate) fixture = candidate;
			Assert.That(fixture, Is.Not.Null);
			fixture.autoResize = false;
			yield return WaitForRun();
			Assert.That(_runner.failedCount, Is.EqualTo(1));
			Assert.That(_runner.results[index].state, Is.EqualTo(RegressionCaseState.Failed));
			Assert.That(_runner.results[index].failure, Is.Not.Empty);
			var dashboard = _runner.GetComponentInChildren<RegressionDashboard>();
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_status").text, Does.StartWith("FAIL"));
			Assert.That(Field<UnityEngine.UI.Text>(dashboard, "_actual").text, Is.EqualTo(_runner.results[index].actual));
			Assert.That(_runner.isRunning, Is.False);
		}

		static IEnumerator WaitForVisibleRow(UITableView table, int index)
		{
			var deadline = Time.realtimeSinceStartup + 3f;
			var viewportCorners = new Vector3[4];
			var cellCorners = new Vector3[4];
			while (Time.realtimeSinceStartup < deadline) {
				if (table.TryGetLoadedCell<RegressionResultCell>(index, out var cell)) {
					table.scrollRect.viewport.GetWorldCorners(viewportCorners);
					cell.rectTransform.GetWorldCorners(cellCorners);
					if (cellCorners[0].x < viewportCorners[2].x && cellCorners[2].x > viewportCorners[0].x &&
						cellCorners[0].y < viewportCorners[2].y && cellCorners[2].y > viewportCorners[0].y) yield break;
				}
				yield return null;
			}
			Assert.Fail($"Row {index} did not become visible. Position={table.scrollRect.normalizedPosition}, viewport={table.scrollRect.viewport.rect}, content={table.scrollRect.content.rect}");
		}

		IEnumerator WaitForRun()
		{
			var deadline = Time.realtimeSinceStartup + 45f;
			while (_runner.isRunning && Time.realtimeSinceStartup < deadline) yield return null;
			Assert.That(_runner.isRunning, Is.False, "The regression dashboard run did not complete within 45 seconds.");
		}

		static T Field<T>(object target, string name) =>
			(T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
	}
}
