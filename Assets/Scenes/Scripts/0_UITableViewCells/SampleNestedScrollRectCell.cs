using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleNestedScrollRectCell : UITableViewCell, IUITableViewDataSource
{
	[SerializeField] Text _indexText;
	[SerializeField] UITableView _nestedTable;
	[SerializeField] SampleGridCell _cell;

	int _nestedCellCount;

	public void UpdateData(int cellIndex)
	{
		_indexText.text = cellIndex.ToString();
		_nestedCellCount = 10;
		_nestedTable.dataSource = this;
		_nestedTable.ReloadData();
	}

	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int cellIndex)
	{
		var cell = tableView.ReuseOrCreateCell(_cell);
		cell.UpdateData(cellIndex, SampleGridView.Mode.Normal);
		return cell;
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return _nestedCellCount;
	}

	public float LengthForCellInTableView(UITableView tableView, int cellIndex)
	{
		return cellIndex % 2 == 0 ? 200f : 250f;
	}
}
