using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIKit.Samples
{
	public class UITableViewRegressionScene : MonoBehaviour
	{
		public const string ScenePath = "Assets/UIKit/Samples/99_RegressionTests/99_RegressionTests.unity";
		public const int RegressionCaseCount = 9;
		public static readonly string[] CaseNames = Combine(new[] {
			"1a. Append then scroll to new cell", "1b. Prepend then scroll to shifted cell",
			"2. Short content in infinite loop", "3. Infinite grid with incomplete last row",
			"4. Prepend grid with row margins", "5. Prepend zero cells (retained offscreen cells)",
			"6a. Rearrange empty table", "6b. Magnetic drag on empty table", "7. Content equals viewport"
		}, BasicTableViewChecks.Names);
		[SerializeField] RegressionDashboard _dashboard;
		readonly RegressionCaseResult[] _results = CreateResults();
		readonly UITableViewRegressionFixture[] _previews = new UITableViewRegressionFixture[CaseNames.Length];
		bool _running;
		bool _destroying;
		int _selectedCase;
		UITableViewDirection _direction = UITableViewDirection.TopToBottom;
		public UITableViewDirection testDirection => _direction;
		public bool isRunning => _running;
		public int passedCount => Count(RegressionCaseState.Passed);
		public int failedCount => Count(RegressionCaseState.Failed);
		public int pendingCount => Count(RegressionCaseState.NotRun);
		public int selectedCase => _selectedCase;
		public IReadOnlyList<RegressionCaseResult> results => _results;
		public UITableViewRegressionFixture selectedPreview => _previews[_selectedCase];

		internal static string[] Combine(string[] first, string[] second)
		{
			var result = new string[first.Length + second.Length];
			Array.Copy(first, result, first.Length);
			Array.Copy(second, 0, result, first.Length, second.Length);
			return result;
		}
		public void CycleDirection()
		{
			if (_running) return;
			var directions = new[] { UITableViewDirection.TopToBottom, UITableViewDirection.BottomToTop, UITableViewDirection.LeftToRight, UITableViewDirection.RightToLeft };
			_direction = directions[(Array.IndexOf(directions, _direction) + 1) % directions.Length];
			ResetResults();
			RefreshDashboard();
		}
		void ResetResults()
		{
			for (var i = 0; i < _results.Length; i++) {
				_results[i] = new RegressionCaseResult();
				_previews[i]?.Dispose();
				_previews[i] = null;
			}
		}
		static RegressionCaseResult[] CreateResults()
		{
			var results = new RegressionCaseResult[CaseNames.Length];
			for (var i = 0; i < results.Length; i++) results[i] = new RegressionCaseResult();
			return results;
		}
		int Count(RegressionCaseState state)
		{
			var count = 0;
			foreach (var result in _results) if (result.state == state) count++;
			return count;
		}
		void Start()
		{
			if (_dashboard == null) throw new InvalidOperationException("Regression Dashboard reference is missing. Rebuild the sample using UIKit/Tests/Create Regression Scene.");
			_dashboard.Initialize(this);
		}
		void OnDestroy()
		{
			_destroying = true;
			StopAllCoroutines();
			foreach (var preview in _previews) preview?.Dispose();
		}
		public void RunAllCases() { if (!_running) StartCoroutine(RunInteractive(-1)); }
		public void RunSelectedCase() { if (!_running) StartCoroutine(RunInteractive(_selectedCase)); }
		public void SelectCase(int index)
		{
			if (_running || index < 0 || index >= CaseNames.Length) return;
			_selectedCase = index;
			RefreshDashboard();
		}
		void RefreshDashboard()
		{
			for (var i = 0; i < _previews.Length; i++)
				if (_previews[i] != null) _previews[i].root.SetActive(i == _selectedCase);
			if (_dashboard != null) _dashboard.Refresh();
		}

		public IEnumerator RunCase(int index, UITableViewDirection direction = UITableViewDirection.TopToBottom)
		{
			return ExecuteCase(index, direction, false);
		}

		IEnumerator ExecuteCase(int index, UITableViewDirection direction, bool retainPreview)
		{
			var viewport = index == 2 ? 1000f : index == 3 || index == 8 ? 100f : 300f;
			var fixture = index < RegressionCaseCount ? new UITableViewRegressionFixture(direction, viewport, index == 3 || index == 4)
				: BasicTableViewChecks.CreateFixture(index - RegressionCaseCount, direction);
			try {
				if (index >= RegressionCaseCount) {
					yield return BasicTableViewChecks.Run(index - RegressionCaseCount, fixture, actual => _results[index].actual = actual);
					yield break;
				}
				fixture.count = index == 2 || index == 8 ? 1 : index == 3 ? 5 : index == 4 ? 6 : index == 6 || index == 7 ? 0 : 10;
				fixture.columns = index == 3 || index == 4 ? 3 : 1;
				fixture.margin = index == 4 ? 10f : 0f;
				fixture.lifeCycle = index == 5 ? UITableViewCellLifeCycle.RecycleWhenReloaded : UITableViewCellLifeCycle.RecycleWhenDisappeared;
				yield return null;
				fixture.table.ReloadData();
				switch (index) {
					case 0:
						fixture.count++;
						fixture.table.AppendData();
						fixture.ScrollTo(10);
						_results[index].actual = $"Cell count: {fixture.count}\nCell #10 loaded: {fixture.table.TryGetLoadedCell<RegressionCell>(10, out _)}";
						Require(fixture.table.TryGetLoadedCell<RegressionCell>(10, out _), "The appended cell must be accessible at index 10.");
						break;
					case 1:
						fixture.count++;
						fixture.table.PrependData();
						fixture.ScrollTo(10);
						_results[index].actual = $"Cell count: {fixture.count}\nCell #10 loaded: {fixture.table.TryGetLoadedCell<RegressionCell>(10, out _)}";
						Require(fixture.table.TryGetLoadedCell<RegressionCell>(10, out _), "The shifted last cell must be accessible at index 10.");
						break;
					case 2:
						fixture.table.enableInfiniteLoop = true;
						fixture.table.ReloadData(0);
						fixture.Pump();
						var position = fixture.ScrollPosition;
						var maxPosition = fixture.ContentLength - fixture.ViewportLength;
						_results[index].actual = $"Position: {position:0.##} / {maxPosition:0.##}\nRoom after viewport: {maxPosition - position:0.##}";
						Require(position > 50f && position < maxPosition - 50f,
							$"The loop must leave room on both sides: position={position}, scrollable={maxPosition}.");
						break;
					case 3:
						fixture.table.enableInfiniteLoop = true;
						fixture.table.ignoreCellLifeCycle = true;
						float? firstColumn = null, previousRow = null;
						var copies = new List<UITableViewCell>();
						foreach (var cell in fixture.table.GetAllLoadedCells()) {
							if (cell.index == 0) copies.Add(cell);
						}
						copies.Sort((a, b) => RowPosition(a, direction).CompareTo(RowPosition(b, direction)));
						_results[index].actual = $"Copies of cell #0: {copies.Count}";
						Require(copies.Count >= 3, "At least three copies should be loaded.");
						foreach (var cell in copies) {
							var column = direction.IsVertical() ? cell.rectTransform.anchoredPosition.x : cell.rectTransform.anchoredPosition.y;
							var row = RowPosition(cell, direction);
							_results[index].actual = $"Copies of #0: {copies.Count}; column-0 offset: {column:0.##}\nMeasured row spacing: {(previousRow.HasValue ? (row - previousRow.Value).ToString("0.##") : "first copy")}";
							if (firstColumn.HasValue) Require(Mathf.Abs(firstColumn.Value - column) < 0.01f, "Logical cell 0 must stay in column 0 in every copy.");
							if (previousRow.HasValue) Require(Mathf.Abs(row - previousRow.Value - 200f) < 0.01f, "Every copy must occupy exactly two rows (200 units).");
							firstColumn = column;
							previousRow = row;
						}
						break;
					case 4:
						fixture.count = 9;
						fixture.table.PrependData();
						_results[index].actual = $"Cells: {fixture.count}; columns: {fixture.columns}\nContent length: {fixture.ContentLength:0.##}";
						Require(Mathf.Abs(fixture.ContentLength - 360f) < 0.01f, $"Three rows with 10-unit upper/lower margins must be 360 units, got {fixture.ContentLength}.");
						break;
					case 5:
						fixture.ScrollTo(0);
						var retained = fixture.table.GetLoadedCell(0);
						Require(retained != null, "Cell 0 must start loaded.");
						fixture.ScrollTo(9);
						Require(fixture.table.GetLoadedCell(0) == retained, "The lifecycle must retain offscreen cell 0 before prepend.");
						fixture.table.PrependData();
						_results[index].actual = $"Added cells: 0\nOffscreen cell #0 retained: {fixture.table.GetLoadedCell(0) == retained}";
						Require(fixture.table.GetLoadedCell(0) == retained, "Prepending zero cells must preserve the retained offscreen cell.");
						break;
					case 6:
						fixture.table.RearrangeData();
						_results[index].actual = $"Cell count: {fixture.count}\nLoaded cells: {(HasLoadedCells(fixture.table) ? "present" : "none")}";
						Require(!HasLoadedCells(fixture.table), "An empty table must stay empty.");
						break;
					case 7:
						fixture.table.magnetic = fixture;
						fixture.EndDrag();
						_results[index].actual = $"Cell count: 0\nMagnetic notifications: {fixture.magneticNotifications}";
						Require(fixture.magneticNotifications == 0, "Empty-table dragging must not start a magnetic animation.");
						break;
					case 8:
						var normalized = fixture.table.GetNormalizedPositionOfCellAt(0, UITableViewAlignment.RightOrTop, false, 0f);
						_results[index].actual = $"Content / viewport: {fixture.ContentLength:0.##} / {fixture.ViewportLength:0.##}\nNormalized position: {normalized}";
						Require(IsFinite(normalized.x) && IsFinite(normalized.y), $"Normalized position must be finite, got {normalized}.");
						fixture.ScrollTo(0);
						break;
					default: throw new ArgumentOutOfRangeException(nameof(index));
				}
			} finally {
				if (retainPreview && !_destroying && _dashboard != null) {
					_previews[index]?.Dispose();
					_previews[index] = fixture;
					_dashboard.AttachPreview(fixture, index);
				} else fixture.Dispose();
			}
			yield return null;
		}

		IEnumerator RunInteractive(int selected)
		{
			if (_running) yield break;
			_running = true;
			try {
				if (selected < 0) ResetResults();
				for (var i = 0; i < CaseNames.Length; i++) {
					if (selected >= 0 && selected != i) continue;
					_selectedCase = i;
					_results[i] = new RegressionCaseResult { state = RegressionCaseState.Running };
					RefreshDashboard();
					var started = Time.realtimeSinceStartup;
					var routine = ExecuteCase(i, _direction, true);
					Exception failure = null;
					// Flatten nested case coroutines so every assertion reaches the result UI.
					var stack = new Stack<IEnumerator>();
					stack.Push(routine);
					try {
						while (stack.Count > 0) {
							var current = stack.Peek();
							bool next;
							try { next = current.MoveNext(); }
							catch (Exception e) { failure = e; break; }
							if (!next) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
							if (current.Current is IEnumerator nested) stack.Push(nested);
							else yield return current.Current;
						}
					} finally {
						while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
					}
					_results[i].state = failure == null ? RegressionCaseState.Passed : RegressionCaseState.Failed;
					_results[i].failure = failure?.ToString() ?? "";
					if (failure != null && _results[i].actual.StartsWith("Not measured"))
						_results[i].actual = "Stopped before measurement. Open failure details.";
					_results[i].milliseconds = (Time.realtimeSinceStartup - started) * 1000f;
					Debug.Log($"{(failure == null ? "PASS" : "FAIL")} {CaseNames[i]}" + (failure == null ? "" : "\n" + failure));
					RefreshDashboard();
				}
				if (selected < 0) {
					for (var i = 0; i < _results.Length; i++)
						if (_results[i].state == RegressionCaseState.Failed) { _selectedCase = i; break; }
				}
			} finally {
				_running = false;
				RefreshDashboard();
			}
		}

		static bool HasLoadedCells(UITableView table) { foreach (var cell in table.GetAllLoadedCells()) return true; return false; }
		static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
		static float RowPosition(UITableViewCell cell, UITableViewDirection direction) => Mathf.Abs(direction.IsVertical() ? cell.rectTransform.anchoredPosition.y : cell.rectTransform.anchoredPosition.x);
		static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
	}

	public enum RegressionCaseState { NotRun, Running, Passed, Failed }

	public sealed class RegressionCaseResult
	{
		public RegressionCaseState state;
		public string actual = "Not measured yet. Run this case to inspect its table.";
		public string failure = "";
		public float milliseconds;
	}
}
