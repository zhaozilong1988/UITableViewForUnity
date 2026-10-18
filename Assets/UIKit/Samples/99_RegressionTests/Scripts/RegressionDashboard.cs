using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIKit.Samples
{
	// Authored uGUI; the sample's own UITableView recycles the result rows.
	public sealed class RegressionDashboard : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate
	{
		public static readonly string[] Titles = UITableViewRegressionScene.Combine(new[] {
			"Append a cell", "Prepend a cell", "Short infinite loop", "Incomplete grid row",
			"Grid margins after prepend", "Prepend zero cells", "Rearrange an empty table",
			"Empty magnetic table", "Content fits the viewport"
		}, BasicTableViewChecks.Titles);
		public static readonly string[] Descriptions = UITableViewRegressionScene.Combine(new[] {
			"Add a cell at the end, then scroll to the new last item.",
			"Add a cell at the beginning, then scroll to the shifted last item.",
			"Repeat one 100-unit cell inside a 1000-unit viewport.",
			"Repeat five items in a three-column grid. Each copy must end with two items.",
			"Prepend a row without duplicating the existing row-margin information.",
			"Keep an offscreen cell alive when no items are added.",
			"Rearrange a table that contains no data.",
			"End a drag on an empty table with magnetic snapping enabled.",
			"Scroll when content and viewport are both exactly 100 units long."
		}, BasicTableViewChecks.Descriptions);
		public static readonly string[] Expectations = UITableViewRegressionScene.Combine(new[] {
			"11 items; cell #10 is loaded.", "11 items; cell #10 is loaded.",
			"More than 50 units of scroll room on both sides.",
			"At least 3 copies; column 0 stays aligned; row spacing is 200.",
			"3 rows with margins; total length = 360.",
			"Zero added items; offscreen cell #0 is the same instance.",
			"No loaded cells; no exception.", "Zero magnetic notifications; no exception.",
			"Finite X/Y normalized position; no NaN or Infinity."
		}, BasicTableViewChecks.Expectations);
		[SerializeField] RectTransform _body;
		[SerializeField] RectTransform _listPanel;
		[SerializeField] RectTransform _detailPanel;
		[SerializeField] RectTransform _preview;
		[SerializeField] RectTransform _viewportFrame;
		[SerializeField] UITableView _table;
		[SerializeField] RegressionResultCell _cellPrefab;
		[SerializeField] UnityEngine.UI.Text _summary;
		[SerializeField] UnityEngine.UI.Text _title;
		[SerializeField] UnityEngine.UI.Text _description;
		[SerializeField] UnityEngine.UI.Text _expected;
		[SerializeField] UnityEngine.UI.Text _actual;
		[SerializeField] UnityEngine.UI.Text _status;
		[SerializeField] UnityEngine.UI.Text _previewCaption;
		[SerializeField] UnityEngine.UI.Text _emptyPreview;
		[SerializeField] UnityEngine.UI.Text _failure;
		[SerializeField] UnityEngine.UI.Button _runAll;
		[SerializeField] UnityEngine.UI.Button _direction;
		[SerializeField] UnityEngine.UI.Button _rerun;
		[SerializeField] UnityEngine.UI.Button _errorToggle;
		[SerializeField] GameObject _errorPanel;
		readonly List<int> _ordered = new List<int>();
		UITableViewRegressionScene _runner;
		Vector2 _lastSize;
		bool _showError;
		RegressionInteractivePreview _interaction;
		int _interactionCase = -1;
		int _presentedCase = -1;
		UITableViewRegressionFixture _presentedSource;
		public UITableViewRegressionFixture interactivePreview => _interaction?.fixture;
		public void Initialize(UITableViewRegressionScene runner)
		{
			if (_table == null || _cellPrefab == null || _preview == null || _summary == null || _runAll == null || _direction == null)
				throw new InvalidOperationException("Regression dashboard has missing authored references.");
			_runner = runner;
			_table.dataSource = this;
			_table.@delegate = this;
			_runAll.onClick.AddListener(runner.RunAllCases);
			_direction.onClick.AddListener(runner.CycleDirection);
			_rerun.onClick.AddListener(runner.RunSelectedCase);
			_errorToggle.onClick.AddListener(() => { DisposeInteraction(); _showError = !_showError; RefreshDetails(); });
			Refresh();
			ApplyLayout();
		}
		void StartInteraction()
		{
			_interactionCase = _runner.selectedCase;
			_interaction = new RegressionInteractivePreview(_runner.selectedPreview, message => _previewCaption.text = message);
			AttachFixture(_interaction.fixture, true);
		}
		void DisposeInteraction()
		{
			_interaction?.Dispose();
			_interaction = null;
			_interactionCase = -1;
		}
		void OnDestroy() => DisposeInteraction();
		void LateUpdate()
		{
			if (_runner != null && _lastSize != _body.rect.size) ApplyLayout();
		}
		void ApplyLayout()
		{
			_lastSize = _body.rect.size;
			var stacked = _body.rect.width < 940f;
			Place(_listPanel, new Vector2(0, stacked ? .57f : 0), new Vector2(stacked ? 1 : .37f, 1), new Vector2(0, stacked ? 8 : 0), new Vector2(stacked ? 0 : -10, 0));
			Place(_detailPanel, Vector2.zero, new Vector2(1, stacked ? .57f : 1), new Vector2(stacked ? 0 : _body.rect.width * .37f + 10, 0), new Vector2(0, stacked ? -8 : 0));
			Canvas.ForceUpdateCanvases();
			if (_table.dataSource != null) _table.RearrangeData();
			FitSelectedPreview();
		}
		static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
		{
			rect.anchorMin = min; rect.anchorMax = max;
			rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
		}
		public void Refresh()
		{
			if (_runner == null) return;
			var position = _table.scrollRect.normalizedPosition;
			_ordered.Clear();
			for (var i = 0; i < Titles.Length; i++) _ordered.Add(i);
			_ordered.Sort((a, b) => {
				var aFailed = _runner.results[a].state == RegressionCaseState.Failed;
				var bFailed = _runner.results[b].state == RegressionCaseState.Failed;
				return aFailed != bFailed ? (aFailed ? -1 : 1) : a.CompareTo(b);
			});
			_summary.text = $"PASS {_runner.passedCount}    FAIL {_runner.failedCount}    PENDING {_runner.pendingCount}";
			_runAll.interactable = !_runner.isRunning;
			_direction.interactable = !_runner.isRunning;
			_direction.GetComponentInChildren<UnityEngine.UI.Text>().text = _runner.testDirection.ToString();
			_table.ReloadData(position);
			RefreshDetails();
		}
		void RefreshDetails()
		{
			var index = _runner.selectedCase;
			var result = _runner.results[index];
			var source = _runner.selectedPreview;
			if (_presentedCase != index || _presentedSource != source) {
				_presentedCase = index;
				_presentedSource = source;
				_showError = false;
			}
			if (_interaction != null && (_runner.isRunning || _interactionCase != index || _interaction.source != source)) DisposeInteraction();
			if (!_showError && !_runner.isRunning && source != null && _interaction == null) StartInteraction();
			var live = _interaction != null;
			if (_runner.selectedPreview != null) _runner.selectedPreview.root.SetActive(!live);
			_title.text = $"{index + 1:00}  {Titles[index]}";
			_description.text = Descriptions[index];
			_expected.text = Expectations[index];
			_actual.text = result.actual;
			_status.text = StateLabel(result.state) + (result.state == RegressionCaseState.Passed || result.state == RegressionCaseState.Failed ? $"  /  {result.milliseconds:0} ms" : "");
			_status.color = StateColor(result.state);
			_rerun.interactable = !_runner.isRunning;
			_errorToggle.gameObject.SetActive(result.state == RegressionCaseState.Failed);
			_errorToggle.interactable = !_runner.isRunning;
			_errorPanel.SetActive(_showError && result.state == RegressionCaseState.Failed && !live);
			_failure.text = result.failure;
			_preview.gameObject.SetActive(!_errorPanel.activeSelf);
			_emptyPreview.gameObject.SetActive(_runner.selectedPreview == null || _runner.selectedPreview.count == 0);
			_emptyPreview.text = _runner.selectedPreview == null ? "Run this check to keep its table here." : "EMPTY TABLE / 0 cells";
			_previewCaption.text = _errorPanel.activeSelf ? "FAILURE DETAILS / scroll to read" : "RESULT VIEW / blue frame = tested viewport";
			if (live) _interaction.ShowHint();
			var captionOffset = _previewCaption.rectTransform.offsetMax;
			captionOffset.x = -18;
			_previewCaption.rectTransform.offsetMax = captionOffset;
			_viewportFrame.gameObject.SetActive(_runner.selectedPreview != null);
			FitSelectedPreview();
		}
		public void AttachPreview(UITableViewRegressionFixture fixture, int index)
		{
			AttachFixture(fixture, false);
			fixture.root.SetActive(index == _runner.selectedCase);
			FitPreview(fixture, false);
		}
		void AttachFixture(UITableViewRegressionFixture fixture, bool interactive)
		{
			fixture.root.transform.SetParent(_preview, false);
			fixture.root.transform.SetAsFirstSibling();
			var canvas = fixture.root.GetComponent<Canvas>();
			canvas.overrideSorting = false;
			fixture.root.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = interactive;
			// Keep the result overview visible outside the tested viewport.
			fixture.table.scrollRect.viewport.GetComponent<UnityEngine.UI.RectMask2D>().enabled = false;
			fixture.table.scrollRect.enabled = interactive;
			fixture.table.enabled = interactive;
			var rect = fixture.root.GetComponent<RectTransform>();
			rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
			rect.anchoredPosition = Vector2.zero;
			rect.sizeDelta = fixture.table.GetComponent<RectTransform>().rect.size;
		}

		void FitSelectedPreview()
		{
			var fixture = _interaction?.fixture ?? _runner?.selectedPreview;
			if (fixture != null) FitPreview(fixture, _interaction != null);
		}
		void FitPreview(UITableViewRegressionFixture fixture, bool interactive)
		{
			var available = _preview.rect.size - new Vector2(28, 28);
			if (available.x <= 0 || available.y <= 0) return;
			var corners = new Vector3[4];
			var geometry = interactive ? _interaction.source : fixture;
			geometry.table.scrollRect.viewport.GetWorldCorners(corners);
			var bounds = new Bounds(geometry.root.transform.InverseTransformPoint(corners[0]), Vector3.zero);
			foreach (var corner in corners) bounds.Encapsulate(geometry.root.transform.InverseTransformPoint(corner));
			foreach (var cell in geometry.table.GetAllLoadedCells()) {
				cell.rectTransform.GetWorldCorners(corners);
				foreach (var corner in corners) bounds.Encapsulate(geometry.root.transform.InverseTransformPoint(corner));
			}
			var scale = Mathf.Min(available.x / bounds.size.x, available.y / bounds.size.y);
			fixture.root.transform.localScale = Vector3.one * scale;
			fixture.root.GetComponent<RectTransform>().anchoredPosition = -(Vector2)bounds.center * scale;
			fixture.table.scrollRect.viewport.GetWorldCorners(corners);
			var min = (Vector2)_preview.InverseTransformPoint(corners[0]);
			var max = min;
			foreach (var corner in corners) {
				var point = (Vector2)_preview.InverseTransformPoint(corner);
				min = Vector2.Min(min, point); max = Vector2.Max(max, point);
			}
			_viewportFrame.sizeDelta = max - min;
			_viewportFrame.anchoredPosition = (min + max) * .5f;
			if (interactive) {
				var rootCanvas = fixture.root.GetComponent<Canvas>().rootCanvas;
				_interaction.recorder.camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
				_interaction.SetDisplayScale(scale * rootCanvas.scaleFactor);
			}
			else foreach (var cell in fixture.table.GetAllLoadedCells<RegressionCell>()) {
				cell.label.fontSize = Mathf.RoundToInt(Mathf.Min(22, fixture.cellLength * scale * .6f) / scale);
				cell.GetComponent<UnityEngine.UI.Image>().color = cell.index == 10 ? new Color(.12f, .65f, .44f) : cell.index % 2 == 0 ? new Color(.16f, .36f, .56f) : new Color(.12f, .25f, .39f);
			}
		}
		public static string StateLabel(RegressionCaseState state)
		{
			switch (state) {
				case RegressionCaseState.Passed: return "PASS";
				case RegressionCaseState.Failed: return "FAIL";
				case RegressionCaseState.Running: return "RUNNING...";
				default: return "NOT RUN";
			}
		}
		public static Color StateColor(RegressionCaseState state)
		{
			return state == RegressionCaseState.Passed ? new Color(.3f, .92f, .65f) : state == RegressionCaseState.Failed ? new Color(1, .45f, .48f) : state == RegressionCaseState.Running ? new Color(1, .8f, .35f) : new Color(.65f, .72f, .83f);
		}
		public int NumberOfCellsInTableView(UITableView view) => _ordered.Count;
		public float LengthForCellInTableView(UITableView view, int index) => 116f;
		public UITableViewCell CellAtIndexInTableView(UITableView view, int index) => view.ReuseOrCreateCell(_cellPrefab);
		public void CellAtIndexInTableViewWillAppear(UITableView view, int index)
		{
			var id = _ordered[index];
			view.GetLoadedCell<RegressionResultCell>(index).Bind($"{id + 1:00}  {Titles[id]}", _runner.results[id].state, id == _runner.selectedCase, !_runner.isRunning, () => _runner.SelectCase(id));
		}
		public void CellAtIndexInTableViewDidDisappear(UITableView view, int index) { }
	}
}
