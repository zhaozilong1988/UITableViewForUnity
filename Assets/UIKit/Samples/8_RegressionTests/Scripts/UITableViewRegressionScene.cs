using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIKit.Samples
{
	public class UITableViewRegressionScene : MonoBehaviour
	{
		public const string ScenePath = "Assets/UIKit/Samples/8_RegressionTests/8_RegressionTests.unity";
		public static readonly string[] CaseNames = {
			"1a. Append then scroll to new cell", "1b. Prepend then scroll to shifted cell",
			"2. Short content in infinite loop", "3. Infinite grid with incomplete last row",
			"4. Prepend grid with row margins", "5. Prepend zero cells (retained offscreen cells)",
			"6a. Rearrange empty table", "6b. Magnetic drag on empty table", "7. Content equals viewport"
		};
		UnityEngine.UI.Text _status;
		bool _running;
		public bool isRunning => _running;
		public int passedCount { get; private set; }
		public int failedCount { get; private set; }

		void Start()
		{
			var canvas = new GameObject("Regression Controls", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
			canvas.transform.SetParent(transform, false);
			canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.GetComponent<Canvas>().sortingOrder = 20;
			var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
			scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1280f, 900f);
			var panel = new GameObject("Checks", typeof(RectTransform), typeof(UnityEngine.UI.Image));
			panel.transform.SetParent(canvas.transform, false);
			var rect = panel.GetComponent<RectTransform>();
			rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
			rect.sizeDelta = new Vector2(610f, 890f);
			rect.anchoredPosition = new Vector2(12f, -8f);
			panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.96f);
			Label(panel.transform, "UITableView regression checks\nRun a case or all cases. PASS means the expected behavior holds.", 10f, 75f);
			Button(panel.transform, "Run all cases", 90f, () => StartCoroutine(RunInteractive(-1)));
			for (var i = 0; i < CaseNames.Length; i++) {
				var index = i;
				Button(panel.transform, CaseNames[i], 140f + i * 42f, () => StartCoroutine(RunInteractive(index)));
			}
			_status = Label(panel.transform, "Results will appear here and in the Console.", 535f, 340f);
			if (EventSystem.current == null) {
				var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
				events.transform.SetParent(transform, false);
			}
		}

		public IEnumerator RunCase(int index, UITableViewDirection direction = UITableViewDirection.TopToBottom)
		{
			var viewport = index == 2 ? 1000f : index == 3 || index == 8 ? 100f : 300f;
			using (var fixture = new UITableViewRegressionFixture(direction, viewport, index == 3 || index == 4)) {
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
						Require(fixture.table.TryGetLoadedCell<RegressionCell>(10, out _), "The appended cell must be accessible at index 10.");
						break;
					case 1:
						fixture.count++;
						fixture.table.PrependData();
						fixture.ScrollTo(10);
						Require(fixture.table.TryGetLoadedCell<RegressionCell>(10, out _), "The shifted last cell must be accessible at index 10.");
						break;
					case 2:
						fixture.table.enableInfiniteLoop = true;
						fixture.table.ReloadData(0);
						fixture.Pump();
						var position = fixture.ScrollPosition;
						var maxPosition = fixture.ContentLength - fixture.ViewportLength;
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
						Require(copies.Count >= 3, "At least three copies should be loaded.");
						foreach (var cell in copies) {
							var column = direction.IsVertical() ? cell.rectTransform.anchoredPosition.x : cell.rectTransform.anchoredPosition.y;
							var row = RowPosition(cell, direction);
							if (firstColumn.HasValue) Require(Mathf.Abs(firstColumn.Value - column) < 0.01f, "Logical cell 0 must stay in column 0 in every copy.");
							if (previousRow.HasValue) Require(Mathf.Abs(row - previousRow.Value - 200f) < 0.01f, "Every copy must occupy exactly two rows (200 units).");
							firstColumn = column;
							previousRow = row;
						}
						break;
					case 4:
						fixture.count = 9;
						fixture.table.PrependData();
						Require(Mathf.Abs(fixture.ContentLength - 360f) < 0.01f, $"Three rows with 10-unit upper/lower margins must be 360 units, got {fixture.ContentLength}.");
						break;
					case 5:
						fixture.ScrollTo(0);
						var retained = fixture.table.GetLoadedCell(0);
						Require(retained != null, "Cell 0 must start loaded.");
						fixture.ScrollTo(9);
						Require(fixture.table.GetLoadedCell(0) == retained, "The lifecycle must retain offscreen cell 0 before prepend.");
						fixture.table.PrependData();
						Require(fixture.table.GetLoadedCell(0) == retained, "Prepending zero cells must preserve the retained offscreen cell.");
						break;
					case 6:
						fixture.table.RearrangeData();
						Require(!HasLoadedCells(fixture.table), "An empty table must stay empty.");
						break;
					case 7:
						fixture.table.magnetic = fixture;
						fixture.EndDrag();
						Require(fixture.magneticNotifications == 0, "Empty-table dragging must not start a magnetic animation.");
						break;
					case 8:
						var normalized = fixture.table.GetNormalizedPositionOfCellAt(0, UITableViewAlignment.RightOrTop, false, 0f);
						Require(IsFinite(normalized.x) && IsFinite(normalized.y), $"Normalized position must be finite, got {normalized}.");
						fixture.ScrollTo(0);
						break;
					default: throw new ArgumentOutOfRangeException(nameof(index));
				}
			}
			yield return null;
		}

		IEnumerator RunInteractive(int selected)
		{
			if (_running) yield break;
			_running = true;
			passedCount = failedCount = 0;
			_status.text = "";
			for (var i = 0; i < CaseNames.Length; i++) {
				if (selected >= 0 && selected != i) continue;
				var routine = RunCase(i);
				Exception failure = null;
				while (true) {
					bool next;
					try { next = routine.MoveNext(); }
					catch (Exception e) { failure = e; break; }
					if (!next) break;
					yield return routine.Current;
				}
				(routine as IDisposable)?.Dispose();
				if (failure == null) passedCount++;
				else failedCount++;
				var message = $"{(failure == null ? "PASS" : "FAIL")} {CaseNames[i]}";
				_status.text += message + "\n";
				Debug.Log(message + (failure == null ? "" : "\n" + failure));
			}
			_running = false;
		}

		static bool HasLoadedCells(UITableView table) { foreach (var cell in table.GetAllLoadedCells()) return true; return false; }
		static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
		static float RowPosition(UITableViewCell cell, UITableViewDirection direction) => Mathf.Abs(direction.IsVertical() ? cell.rectTransform.anchoredPosition.y : cell.rectTransform.anchoredPosition.x);
		static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
		static UnityEngine.UI.Text Label(Transform parent, string text, float top, float height)
		{
			var go = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			go.transform.SetParent(parent, false);
			var rect = go.GetComponent<RectTransform>();
			rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
			rect.anchoredPosition = new Vector2(12f, -top);
			rect.sizeDelta = new Vector2(580f, height);
			var label = go.GetComponent<UnityEngine.UI.Text>();
			label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			label.fontSize = 18;
			label.color = Color.white;
			label.text = text;
			label.raycastTarget = false;
			return label;
		}
		static void Button(Transform parent, string text, float top, UnityEngine.Events.UnityAction click)
		{
			var go = new GameObject("Run " + text, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
			go.transform.SetParent(parent, false);
			var rect = go.GetComponent<RectTransform>();
			rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
			rect.anchoredPosition = new Vector2(12f, -top);
			rect.sizeDelta = new Vector2(580f, 36f);
			go.GetComponent<UnityEngine.UI.Image>().color = new Color(0.13f, 0.26f, 0.36f);
			go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(click);
			Label(go.transform, text, 5f, 30f);
		}
	}
}
