using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UIKit.Samples
{
	// A recorder around the existing reproduction fixture, not another table implementation.
	public sealed class BasicTableViewFixture : IUITableViewDelegate, IUITableViewMargin,
		IUITableViewClickable, IUITableViewDraggable, IUITableViewFlickableExtra,
		IUITableViewMagneticExtra, IUITableViewReachable, IUITableViewSortable
	{
		public readonly UITableViewRegressionFixture fixture;
		public readonly List<string> events = new List<string>();
		public readonly List<UITableViewMagneticState> magneticStates = new List<UITableViewMagneticState>();
		public readonly List<UITableViewDirection> flickDirections = new List<UITableViewDirection>();
		public readonly HashSet<UITableViewCell> suppliedCells = new HashSet<UITableViewCell>();
		public int cellRequests, appears, disappears, clicks, drags, revision;
		public int reachedTop, reachedBottom, leftTop, leftBottom;
		public bool variableLengths, variableColumns, mixedTypes, movable = true, autoResize = true;
		public float widthRatio = 1, upperMargin = -1, lowerMargin = -1;
		public float flickDuration = .5f, flickLower = 10, flickUpper = 50;
		public float magneticThreshold = 100, magneticSpeed = 10000;
		public UITableViewAlignment alignment;
		public Vector2 calibration = Vector2.one * .5f;
		public Camera camera;
		public UITableViewCell touched;
		public int? draggedIndex;
		readonly RegressionCell _prefab;
		readonly BasicAlternateCell _alternate;

		public BasicTableViewFixture(UITableViewRegressionFixture fixture, bool grid)
		{
			this.fixture = fixture;
			_prefab = fixture.root.transform.Find("Cell Template").GetComponent<RegressionCell>();
			var alternate = new GameObject("Alternate Cell Template", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(BasicAlternateCell));
			alternate.transform.SetParent(fixture.root.transform, false);
			alternate.GetComponent<RectTransform>().anchoredPosition = new Vector2(10000, 10000);
			_alternate = alternate.GetComponent<BasicAlternateCell>();
			var label = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
			label.transform.SetParent(alternate.transform, false);
			var labelRect = label.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.sizeDelta = new Vector2(-8, -8);
			_alternate.label = label.GetComponent<UnityEngine.UI.Text>(); _alternate.label.font = _prefab.label.font; _alternate.label.alignment = TextAnchor.MiddleCenter;
			fixture.table.dataSource = grid ? (IUITableViewDataSource)new GridSource(this) : new ListSource(this);
			fixture.table.@delegate = this;
			fixture.table.marginDataSource = this;
		}
		public float Length(int index) => variableLengths ? 80 + index % 3 * 20 : fixture.cellLength;
		public int Columns(int row) => variableColumns ? (row % 2 == 0 ? 2 : 3) : fixture.columns;
		public float Upper => upperMargin >= 0 ? upperMargin : fixture.margin;
		public float Lower => lowerMargin >= 0 ? lowerMargin : fixture.margin;
		UITableViewCell Cell(UITableView view, int index)
		{
			if (index < 0 || index >= fixture.count) throw new InvalidOperationException("Datasource received an invalid logical index: " + index);
			cellRequests++;
			RegressionCell cell = mixedTypes && index % 2 != 0
				? view.ReuseOrCreateCell("BasicAlternateCell", _alternate, fixture.lifeCycle, autoResize)
				: view.ReuseOrCreateCell("BasicRegressionCell", _prefab, fixture.lifeCycle, autoResize);
			suppliedCells.Add(cell);
			return cell;
		}
		public void CellAtIndexInTableViewWillAppear(UITableView view, int index)
		{
			var cell = view.GetLoadedCell<RegressionCell>(index);
			if (cell == null || cell.index != index) throw new InvalidOperationException("Appearance callback lost its physical cell.");
			cell.label.text = index + " / v" + revision;
			appears++;
		}
		public void CellAtIndexInTableViewDidDisappear(UITableView view, int index)
		{
			var cell = view.GetLoadedCell(index);
			if (cell == null || cell.index != index) throw new InvalidOperationException("Disappearance callback lost its physical cell.");
			disappears++;
		}
		public float LengthForUpperMarginInTableView(UITableView view, int row) => Upper;
		public float LengthForLowerMarginInTableView(UITableView view, int row) => Lower;
		public Camera TableViewCameraForInteractive(UITableView view) => camera;
		void Record(UITableView view, string name, int? index)
		{
			events.Add(name + ":" + (index.HasValue ? index.Value.ToString() : "none"));
			touched = index.HasValue ? view.GetLoadedCell(index.Value) : null;
		}
		public void TableViewOnPointerDownCellAt(UITableView view, int index, PointerEventData data) => Record(view, "down", index);
		public void TableViewOnPointerUpCellAt(UITableView view, int index, PointerEventData data) => Record(view, "up", index);
		public void TableViewOnPointerClickCellAt(UITableView view, int index, PointerEventData data) { clicks++; Record(view, "click", index); }
		public bool TableViewDragCellMovable(UITableView view) => movable;
		public void TableViewOnBeginDrag(UITableView view, int? index, PointerEventData data) { draggedIndex = index; Record(view, "begin", index); }
		public void TableViewOnDrag(UITableView view, int? index, PointerEventData data) { drags++; draggedIndex = index; Record(view, "drag", index); }
		public void TableViewOnEndDrag(UITableView view, int? index, PointerEventData data) { draggedIndex = index; Record(view, "end", index); }
		public void TableViewOnDidFlick(UITableView view, int? index, UITableViewDirection direction) { flickDirections.Add(direction); Record(view, "flick", index); }
		public float FlickWillBeTriggeredIfFlickDurationBelowTime(UITableView view) => flickDuration;
		public (float lower, float upper) FlickWillBeTriggeredIfFlickDistanceFallsWithinRange(UITableView view) => (flickLower, flickUpper);
		public Vector2 MagneticCalibrationPointInTableView(UITableView view) => calibration;
		public void MagneticStateDidChangeInTableView(UITableView view, int index, UITableViewMagneticState state) { magneticStates.Add(state); Record(view, "magnetic", index); }
		public float MagneticWillBeTriggeredWhenScrollingSpeedBelow(UITableView view) => magneticThreshold;
		public float WhenMagneticIsTriggeredScrollingSpeedWillBeChangedTo(UITableView view) => magneticSpeed;
		public void TableViewReachedTopmostOrRightmost(UITableView view) { reachedTop++; }
		public void TableViewReachedBottommostOrLeftmost(UITableView view) { reachedBottom++; }
		public void TableViewLeftTopmostOrRightmost(UITableView view) { leftTop++; }
		public void TableViewLeftBottommostOrLeftmost(UITableView view) { leftBottom++; }
		public float TableViewReachableEdgeTolerance(UITableView view) => 1;
		public int SiblingOrderAtIndexInTableView(UITableView view, int index) => -index;

		class ListSource : IUITableViewDataSource
		{
			protected readonly BasicTableViewFixture owner;
			public ListSource(BasicTableViewFixture owner) { this.owner = owner; }
			public int NumberOfCellsInTableView(UITableView view) => owner.fixture.count;
			public float LengthForCellInTableView(UITableView view, int index) => owner.Length(index);
			public UITableViewCell CellAtIndexInTableView(UITableView view, int index) => owner.Cell(view, index);
		}
		sealed class GridSource : ListSource, IUIGridViewDataSource
		{
			public GridSource(BasicTableViewFixture owner) : base(owner) { }
			public int NumberOfColumnsAtRowInGridView(UITableView view, int row) => owner.Columns(row);
			public UITableViewAlignment AlignmentOfCellsAtRowInGridView(UITableView view, int row) => owner.alignment;
			public float WidthOfCellAtRowInGridView(UITableView view, int row, int column, float average) => average * owner.widthRatio;
		}
	}

	public sealed class BasicAlternateCell : RegressionCell { }

	public sealed class BasicNestedProbe : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public int initialized, began, dragged, ended;
		public void OnInitializePotentialDrag(PointerEventData data) { initialized++; }
		public void OnBeginDrag(PointerEventData data) { began++; }
		public void OnDrag(PointerEventData data) { dragged++; }
		public void OnEndDrag(PointerEventData data) { ended++; }
	}
}
