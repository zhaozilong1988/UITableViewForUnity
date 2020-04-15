using System;
using System.Collections.Generic;
using UnityEngine;
using UITableViewForUnity;
using UnityEngine.UI;

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
	[SerializeField]
	private InputField _cellIndexInput;

	private readonly List<SampleData> _tab1DataList = new List<SampleData>();
	private readonly List<SampleData> _tab2DataList = new List<SampleData>();
	private int _selectedTabIndex = 0;

	void Start()
	{
		AppendSampleData(20, _tab1DataList);
		AppendSampleData(30, _tab2DataList);

		_tableView.dataSource = this;
		_tableView.lifecycle = this;
		_tableView.ReloadData();
		_tableView.MoveToCellAtIndex(0);
	}

	public void AppendSampleData(int delta, List<SampleData> sampleDataList)
	{
		var startIndex = sampleDataList.Count;
		var count = sampleDataList.Count + delta;
		for (int i = startIndex; i < count; i++)
		{
			var data = new SampleData();
			if (i == 0)
				data.sampleType = SampleData.SampleType.Tab;
			else if (i % 2 == 0)
				data.sampleType = SampleData.SampleType.Text;
			else
				data.sampleType = SampleData.SampleType.Image;

			data.text = $"https://www.freepik.com/free-photos-vectors/character";
			data.rarity = i % 5 + 1;
			sampleDataList.Add(data);
		}
	}

	public void PrependSampleData(int count)
	{
		// var startIndex = _indexes.Count - 1;
		// var count = _indexes.Count + 10;
		// for (int i = startIndex; i < count; i++)
		// {
		// 	_indexes.Insert(0, i);
		// }
		// _tableView.PrependData();
	}

	private void OnTabClicked(int tabIndex)
	{
		if (_selectedTabIndex != tabIndex)
		{
			_selectedTabIndex = tabIndex;
			_tableView.ReloadData();
		}
	}

	public void OnGoToCellAtIndexButtonClick()
	{
		var index = int.Parse(_cellIndexInput.text);
		Debug.Log("Go to cell at index of " + index);
		_tableView.MoveToCellAtIndex(index);
	}

	#region IUITableViewDataSource
	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
	{
		var data = _selectedTabIndex == 0 ? _tab1DataList[index] : _tab2DataList[index];
		switch (data.sampleType)
		{
			case SampleData.SampleType.Text:
				return tableView.DequeueOrCreateCell(_textCellPrefab, "TextCell", true);
			case SampleData.SampleType.Image:
				return tableView.DequeueOrCreateCell(_imageCellPrefab, "ImageCell", true);
			case SampleData.SampleType.Tab:
				return tableView.DequeueOrCreateCell(_tabCellPrefab, "TabCell", isAutoResize: true);
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return _selectedTabIndex == 0 ? _tab1DataList.Count : _tab2DataList.Count;
	}

	public float LengthForCellInTableView(UITableView tableView, int index)
	{
		if (_selectedTabIndex == 0)
		{
			switch (_tab1DataList[index].sampleType)
			{
				case SampleData.SampleType.Text:
					return 150f;
				case SampleData.SampleType.Image:
					return 200f;
				case SampleData.SampleType.Tab:
					return 120f;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		switch (_tab2DataList[index].sampleType)
		{
			case SampleData.SampleType.Text:
				return 75f;
			case SampleData.SampleType.Image:
				return 250f;
			case SampleData.SampleType.Tab:
				return 120f;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
	#endregion

	#region IUITableViewLifecycle
	public void CellAtIndexInTableViewDidAppear(UITableView tableView, int index, bool isReused)
	{
		var data = _selectedTabIndex == 0 ? _tab1DataList[index] : _tab2DataList[index];
		switch (data.sampleType)
		{
			case SampleData.SampleType.Text:
				var textCell = tableView.GetAppearingCell<SampleTextCell>(index);
				textCell.UpdateData(index, _selectedTabIndex, data.text);
				break;
			case SampleData.SampleType.Image:
				var imageCell = tableView.GetAppearingCell<SampleImageCell>(index);
				imageCell.UpdateData(index, _selectedTabIndex, data);
				break;
			case SampleData.SampleType.Tab:
				var tabCell = tableView.GetAppearingCell<SampleTabCell>(index);
				tabCell.UpdateData(_selectedTabIndex, OnTabClicked);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		if (_selectedTabIndex == 0 && index >= _tab1DataList.Count - 1)
		{
			AppendSampleData(10, _tab1DataList);
			_tableView.AppendData();
		}

		// Debug.Log($"index:{index} of cell is <color=green>reused: {isReused}</color>");
	}

	public void CellAtIndexInTableViewWillDisappear(UITableView tableView, int index, bool willBeRecycled)
	{
		// Debug.Log($"cell at index:{index} will disappear. <color=red>recycle: {willBeRecycled}</color>");
	}
	#endregion
}
