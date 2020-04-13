using System.Collections.Generic;
using UnityEngine;
using UITableViewForUnity;

public class SampleTableView : MonoBehaviour, IUITableViewDataSource, IUITableViewLifecycle
{
	[SerializeField]
	private UITableView _tableView;
	[SerializeField, Header("cell prefabs")]
	private SampleImageCell _imageCellPrefab;
	[SerializeField]
	private SampleTextCell _textCellPrefab;
	[SerializeField]
	private SampleTabCell _tabCellPrefab;

	private List<int> _indexes = new List<int>();
	private int _selectedTabIndex = 0;

	void Start()
	{
		for (int i = 0; i < 50; i++)
		{
			_indexes.Add(i);
		}

		_tableView.dataSource = this;
		_tableView.lifecycle = this;
		_tableView.ReloadData();

		// _tableView.MoveToCellAtIndex(6);
	}

	public void OnClickAppendButton()
	{
		var startIndex = _indexes.Count - 1;
		var count = _indexes.Count + 10;
		for (int i = startIndex; i < count; i++)
		{
			_indexes.Add(i);
		}
		_tableView.AppendData();
	}

	public void OnClickPrependButton()
	{
		var startIndex = _indexes.Count - 1;
		var count = _indexes.Count + 10;
		for (int i = startIndex; i < count; i++)
		{
			_indexes.Insert(0, i);
		}
		_tableView.PrependData();
	}

	private void OnTabClicked(int tabIndex)
	{
		if (_selectedTabIndex != tabIndex)
		{
			_selectedTabIndex = tabIndex;
			_tableView.ReloadData();
		}
	}

	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
	{
		if (index == 0)
			return tableView.DequeueOrCreateCell(_tabCellPrefab, "TabCell", isAutoResize: true);

		if (index % 2 == 0)
			return tableView.DequeueOrCreateCell(_textCellPrefab, "TextCell", true);

		return tableView.DequeueOrCreateCell(_imageCellPrefab, "ImageCell", true);
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return _indexes.Count;
	}

	public float LengthForCellInTableView(UITableView tableView, int index)
	{
		if (index == 0)
			return 100f;

		if (index % 2 == 0)
			return 50f;

		return 200f;
	}

	public void CellAtIndexInTableViewDidAppear(UITableView tableView, int index, bool isReused)
	{
		if (index == 0)
		{
			var tabCell = tableView.GetAppearingCell<SampleTabCell>(index);
			tabCell.UpdateData(_selectedTabIndex, OnTabClicked);
		}
		else if (index % 2 == 0)
		{
			var textCell = tableView.GetAppearingCell<SampleTextCell>(index);
			textCell.UpdateData(index, _selectedTabIndex, "UITableView");
		}
		else
		{
			var imageCell = tableView.GetAppearingCell<SampleImageCell>(index);
			imageCell.UpdateData(index, _selectedTabIndex);
		}

		Debug.Log($"index:{index} of cell is <color=green>reused: {isReused}</color>");

		// if (index >= _indexes.Count - 1)
		// {
		// 	OnClickAppendButton();
		// }
	}

	public void CellAtIndexInTableViewWillDisappear(UITableView tableView, int index, bool willBeRecycled)
	{
		Debug.Log($"cell at index:{index} will disappear. <color=red>recycle: {willBeRecycled}</color>");
	}
}
