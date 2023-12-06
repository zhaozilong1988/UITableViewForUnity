using System.Collections.Generic;
using UnityEngine;
using UIKit;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Slider = UnityEngine.UI.Slider;

public class SampleGridView : UITableView, IUIGridViewDataSource, IUITableViewDelegate, IUITableViewDraggable, IUITableViewClickable
{
	[SerializeField] UITableViewCell _gridCell;
	[SerializeField] UITableViewCell _emptyCell;
	[SerializeField] Slider _slider;
	[SerializeField] Text _label;
	

	readonly List<int> _dataList = new List<int>();
	int _columnNumber;
	Mode _mode;

	protected override void Awake()
	{
		base.Awake();
		// Prepare for data source
		for (var i = 0; i < 100; i++) {
			_dataList.Add(i);
		}

		_columnNumber = 3;
		_label.text = $"Column Number: {_columnNumber}";

		// Setup table view
		dataSource = this;
		@delegate = this;
		clickable = this;
	}

	protected override void Update()
	{
		base.Update();
		var newColumnNumber = Mathf.FloorToInt(_slider.value);
		if (newColumnNumber == _columnNumber)
			return;
		_columnNumber = newColumnNumber;
		_label.text = $"Column Number: {_columnNumber}";
		ReloadData();
	}

	public void SwitchDragMode()
	{
		_mode = _mode != Mode.Draggable ? Mode.Draggable : Mode.Normal;
		var undraggable = _mode != Mode.Draggable;
		scrollRect.vertical = undraggable;
		draggable = undraggable ? null : this;
		ReloadData();
	}

	public void SwitchDeleteMode()
	{
		_mode = _mode != Mode.Deletable ? Mode.Deletable : Mode.Normal;
		scrollRect.vertical = true;
		ReloadData();
	}

	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
	{
		if (_dataList[index] < 0) {
			return tableView.ReuseOrCreateCell(_emptyCell);
		}
		return tableView.ReuseOrCreateCell(_gridCell);
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return _dataList.Count;
	}

	public float LengthForCellInTableView(UITableView tableView, int index)
	{
		return 200f;
	}

	public int NumberOfColumnPerRow(UITableView tableView, int rowIndex)
	{
		if (rowIndex % 2 == 0) {
			return Mathf.Max(_columnNumber / 2, 1) ;
		}
		return _columnNumber;
	}

	public UITableViewAlignment AlignmentOfCellsAtLastRow(UITableView grid)
	{
		return UITableViewAlignment.Center;
	}

	public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
	{
		if (_dataList[index] >= 0) {
			tableView.GetLoadedCell<SampleGridCell>(index).UpdateData(_dataList[index], _mode);
		}
	}

	public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
	{
	}

	public Camera TableViewCameraForInteractive(UITableView tableView)
	{
		return null;
	}

	public bool TableViewDragCellMovable(UITableView tableView)
	{
		return true;
	}

	public void TableViewOnBeginDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData)
	{
		if (!draggedIndex.HasValue)
			return;
		if (_dataList[draggedIndex.Value] >= 0 && _mode == Mode.Draggable)
			tableView.GetLoadedCell(draggedIndex.Value).rectTransform.SetAsLastSibling();
	}

	public void TableViewOnDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData)
	{
		if (!draggedIndex.HasValue)
			return;
		foreach (var cell in GetAllLoadedCells<SampleGridCell>())
			cell.SetMergeable(false);
		if (tableView.TryFindMostIntersectedCell<SampleGridCell>(draggedIndex.Value, out var mostIntersectedCellIndex, out var maxArea) && maxArea > 150f)
			tableView.GetLoadedCell<SampleGridCell>(mostIntersectedCellIndex).SetMergeable(true);
	}

	public void TableViewOnEndDrag(UITableView tableView, int? draggedIndex, PointerEventData eventData)
	{
		if (!draggedIndex.HasValue) {
			return;
		}
		foreach (var cell in GetAllLoadedCells<SampleGridCell>()) {
			cell.SetMergeable(false);
		}
		if (tableView.TryFindMostIntersectedCell<SampleGridCell>(draggedIndex.Value, out var mostIntersectedCellIndex, out var maxArea) && maxArea > 150f) {
			var swap = _dataList[mostIntersectedCellIndex];
			_dataList[mostIntersectedCellIndex] = _dataList[draggedIndex.Value];
			_dataList[draggedIndex.Value] = swap;
			ReloadDataAt(mostIntersectedCellIndex);
			ReloadDataAt(draggedIndex.Value);
		}
		RefreshAllLoadedCells();
	}

	public void TableViewOnPointerDownCellAt(UITableView tableView, int index, PointerEventData eventData)
	{
	}

	public void TableViewOnPointerClickCellAt(UITableView tableView, int index, PointerEventData eventData)
	{
		if (_mode != Mode.Deletable || _dataList[index] < 0)
			return;
		_dataList[index] = -1;
		ReloadDataAt(index);
	}

	public void TableViewOnPointerUpCellAt(UITableView tableView, int index, PointerEventData eventData)
	{
	}

	public enum Mode
	{
		Normal,
		Draggable,
		Deletable,
	}
}
