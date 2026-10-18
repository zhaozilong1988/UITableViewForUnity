using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIKit.Samples
{
	// Uses the same ScrollRect / Viewport / Content and datasource pattern as the samples.
	public sealed class UITableViewRegressionFixture : IDisposable, IUIGridViewDataSource, IUITableViewDelegate, IUITableViewMargin, IUITableViewMagnetic
	{
		public readonly GameObject root;
		public readonly RegressionTableView table;
		public readonly RectTransform content;
		public int count;
		public int columns = 1;
		public float cellLength = 100f;
		public float margin;
		public int magneticNotifications;
		public UITableViewCellLifeCycle lifeCycle;
		readonly RegressionCell _prefab;
		readonly bool _vertical;

		public UITableViewRegressionFixture(UITableViewDirection direction, float viewportLength = 300f, bool grid = false)
		{
			_vertical = direction.IsVertical();
			root = new GameObject("Regression Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
			root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
			root.GetComponent<Canvas>().sortingOrder = 10;
			var tableObject = Rect("Table", root.transform);
			tableObject.gameObject.SetActive(false);
			tableObject.sizeDelta = _vertical ? new Vector2(400f, viewportLength) : new Vector2(viewportLength, 400f);
			tableObject.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.12f, 0.16f);
			var viewport = Rect("Viewport", tableObject);
			viewport.anchorMin = Vector2.zero;
			viewport.anchorMax = Vector2.one;
			viewport.sizeDelta = Vector2.zero;
			viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
			content = Rect("Content", viewport);
			// Stretch only across the scrolling axis's perpendicular direction.
			content.anchorMin = _vertical ? Vector2.up : Vector2.zero;
			content.anchorMax = _vertical ? Vector2.one : Vector2.up;
			content.pivot = new Vector2(0.5f, 0.5f);
			content.sizeDelta = _vertical ? new Vector2(0f, 100f) : new Vector2(100f, 0f);
			var scroll = tableObject.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
			scroll.viewport = viewport;
			scroll.content = content;
			scroll.vertical = _vertical;
			scroll.horizontal = !_vertical;
			scroll.inertia = false;
			scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
			table = tableObject.gameObject.AddComponent<RegressionTableView>();
			table.dataSource = grid ? (IUITableViewDataSource)this : new TableDataSource(this);
			table.@delegate = this;
			table.marginDataSource = this;
			tableObject.gameObject.SetActive(true);
			var template = Rect("Cell Template", root.transform);
			template.anchoredPosition = new Vector2(10000f, 10000f);
			template.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.45f, 0.65f);
			_prefab = template.gameObject.AddComponent<RegressionCell>();
			var label = Rect("Label", template);
			label.anchorMin = Vector2.zero;
			label.anchorMax = Vector2.one;
			label.sizeDelta = new Vector2(-8f, -8f);
			_prefab.label = label.gameObject.AddComponent<UnityEngine.UI.Text>();
			_prefab.label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			_prefab.label.alignment = TextAnchor.MiddleCenter;
			_prefab.label.fontSize = 22;
			_prefab.label.raycastTarget = false;
			Canvas.ForceUpdateCanvases();
			table.direction = direction;
		}

		public float ContentLength => _vertical ? content.rect.height : content.rect.width;
		public float ScrollPosition => (_vertical
			? (table.direction == UITableViewDirection.TopToBottom ? 1f - table.scrollRect.normalizedPosition.y : table.scrollRect.normalizedPosition.y)
			: (table.direction == UITableViewDirection.RightToLeft ? 1f - table.scrollRect.normalizedPosition.x : table.scrollRect.normalizedPosition.x))
			* (ContentLength - ViewportLength);
		public float ViewportLength => _vertical ? table.scrollRect.viewport.rect.height : table.scrollRect.viewport.rect.width;
		public void Pump() => table.scrollRect.onValueChanged.Invoke(table.scrollRect.normalizedPosition);
		public void ScrollTo(int index) { table.ScrollToCellAt(index); table.Tick(); Pump(); }
		public void EndDrag() { table.OnEndDrag(new PointerEventData(EventSystem.current)); table.Tick(); }

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index) => tableView.ReuseOrCreateCell(_prefab, lifeCycle);
		public int NumberOfCellsInTableView(UITableView tableView) => count;
		public float LengthForCellInTableView(UITableView tableView, int index) => cellLength;
		public int NumberOfColumnsAtRowInGridView(UITableView gridView, int rowIndex) => columns;
		public UITableViewAlignment AlignmentOfCellsAtRowInGridView(UITableView gridView, int rowIndex) => UITableViewAlignment.LeftOrBottom;
		public float WidthOfCellAtRowInGridView(UITableView gridView, int rowIndex, int columnIndex, float averageWidthAtRow) => averageWidthAtRow;
		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index) => tableView.GetLoadedCell<RegressionCell>(index).label.text = index.ToString();
		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index) { }
		public float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex) => margin;
		public float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex) => margin;
		public Vector2 MagneticCalibrationPointInTableView(UITableView tableView) => Vector2.one * 0.5f;
		public void MagneticStateDidChangeInTableView(UITableView tableView, int index, UITableViewMagneticState state) => magneticNotifications++;

		public void Dispose() { if (root == null) return; root.SetActive(false); UnityEngine.Object.Destroy(root); }
		static RectTransform Rect(string name, Transform parent)
		{
			var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
			rect.SetParent(parent, false);
			return rect;
		}

		sealed class TableDataSource : IUITableViewDataSource
		{
			readonly UITableViewRegressionFixture _fixture;
			public TableDataSource(UITableViewRegressionFixture fixture) => _fixture = fixture;
			public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index) => _fixture.CellAtIndexInTableView(tableView, index);
			public int NumberOfCellsInTableView(UITableView tableView) => _fixture.count;
			public float LengthForCellInTableView(UITableView tableView, int index) => _fixture.cellLength;
		}
	}
}
