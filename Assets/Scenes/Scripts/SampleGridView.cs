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
		draggable = this;
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
		scrollRect.vertical = _mode != Mode.Draggable;
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
		return _columnNumber;
	}

	public UITableViewCellAlignment AlignmentOfCellsAtLastRow(UITableView grid)
	{
		return UITableViewCellAlignment.Center;
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

	public bool TableViewOnBeginDragCellAt(UITableView tableView, int draggedIndex, PointerEventData eventData)
	{
		var isDraggable = _dataList[draggedIndex] >= 0 && _mode == Mode.Draggable;
		if (isDraggable) {
			tableView.GetLoadedCell(draggedIndex).rectTransform.SetAsLastSibling();
		}
		return isDraggable;
	}

	public void TableViewOnDragCellAt(UITableView tableView, int draggedIndex, PointerEventData eventData)
	{
		foreach (var cell in GetAllLoadedCells<SampleGridCell>()) {
			cell.SetMergeable(false);
		}
		if (tableView.TryFindMostIntersectedCell<SampleGridCell>(draggedIndex, out var mostIntersectedCellIndex, out var maxArea) && maxArea > 150f) {
			tableView.GetLoadedCell<SampleGridCell>(mostIntersectedCellIndex).SetMergeable(true);
		}
	}

	public void TableViewOnEndDragCellAt(UITableView tableView, int draggedIndex, PointerEventData eventData)
	{
		foreach (var cell in GetAllLoadedCells<SampleGridCell>()) {
			cell.SetMergeable(false);
		}
		if (tableView.TryFindMostIntersectedCell<SampleGridCell>(draggedIndex, out var mostIntersectedCellIndex, out var maxArea) && maxArea > 150f) {
			var swap = _dataList[mostIntersectedCellIndex];
			_dataList[mostIntersectedCellIndex] = _dataList[draggedIndex];
			_dataList[draggedIndex] = swap;
			ReloadDataAt(mostIntersectedCellIndex);
			ReloadDataAt(draggedIndex);
		}
		RearrangeData();
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
