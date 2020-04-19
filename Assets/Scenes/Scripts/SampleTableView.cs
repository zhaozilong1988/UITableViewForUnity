using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UITableViewForUnity;
using UnityEngine.UI;

public class SampleTableView : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate
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
	private int _selectedTabIndex;

	void Start()
	{
		AppendSampleData(20, _tab1DataList);
		AppendSampleData(30, _tab2DataList);

		_tableView.dataSource = this;
		_tableView.@delegate = this;
		_tableView.ReloadData(0);
	}

	private void AppendSampleData(int delta, List<SampleData> sampleDataList)
	{
		var startIndex = sampleDataList.Count;
		var count = sampleDataList.Count + delta;
		for (int i = startIndex; i < count; i++)
		{
			var data = new SampleData();
			if (i == 0)
			{
				data.sampleType = SampleData.SampleType.Tab;
				data.scalar = 120f;
			}
			else if (i % 2 == 0)
			{
				data.sampleType = SampleData.SampleType.Text;
				data.scalar = 75f;
			}
			else
			{
				data.sampleType = SampleData.SampleType.Image;
				data.scalar = 200f;
				data.scalarBeforeExpend = 200f;
				data.scalarAfterExpend = 1000f;
			}

			data.onExpend = Expend;
			data.spriteIndex = i % 9;
			data.text = "https://www.freepik.com/free-photos-vectors/character";
			data.rarity = i % 5 + 1;
			sampleDataList.Add(data);
		}
	}

	private void PrependSampleData(int delta, List<SampleData> sampleDataList)
	{
		for (int i = 0; i < delta; i++)
		{
			var data = new SampleData();
			if (i % 2 == 0)
			{
				data.sampleType = SampleData.SampleType.Text;
				data.scalar = 75f;
			}
			else
			{
				data.sampleType = SampleData.SampleType.Image;
				data.scalar = 200f;
				data.scalarBeforeExpend = 200f;
				data.scalarAfterExpend = 500f;
			}

			data.onExpend = Expend;
			data.spriteIndex = i % 9;
			data.text = "https://www.freepik.com/free-photos-vectors/character";
			data.rarity = i % 5 + 1;
			sampleDataList.Insert(i, data);
		}
	}

	private void OnTabClicked(int tabIndex)
	{
		if (_selectedTabIndex != tabIndex)
		{
			_selectedTabIndex = tabIndex;
			_tableView.ReloadData(0);
			// _tableView.ScrollToCellAtIndex(0);
		}
	}

	public void OnGoToCellAtIndexButtonClick()
	{
		var index = int.Parse(_cellIndexInput.text);
		Debug.Log("Go to cell at index of " + index);
		_tableView.ScrollToCellAtIndex(index, 0.5f, () =>
		{
			Debug.Log("Scrolling has finished");
		});
	}

	public void OnClickPrepend()
	{
		PrependSampleData(10, _tab1DataList);
		_tableView.PrependData();
	}

	public void OnClickAppend()
	{
		AppendSampleData(10, _tab1DataList);
		_tableView.AppendData();
	}

	private void Expend(int index)
	{
		StartCoroutine(CoExpendOrClose(index));
	}

	private IEnumerator CoExpendOrClose(int index)
	{
		var dataList = _selectedTabIndex == 0 ? _tab1DataList : _tab2DataList;
		var sampleData = dataList[index];
		var start = Time.time;

		var progress = 0f;
		while (!Mathf.Approximately(progress, 1f))
		{
			yield return null;
			progress = Mathf.Min((Time.time - start) / 0.1f, 1f);
			dataList[index].scalar = sampleData.isExpended 
				? Mathf.Lerp(sampleData.scalarBeforeExpend, sampleData.scalarAfterExpend, progress)
				: Mathf.Lerp(sampleData.scalarAfterExpend, sampleData.scalarBeforeExpend, progress);
			_tableView.RearrangeData();
			_tableView.ScrollToCellAtIndex(index);
		}
	}

	#region IUITableViewDataSource
	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
	{
		var data = _selectedTabIndex == 0 ? _tab1DataList[index] : _tab2DataList[index];
		switch (data.sampleType)
		{
			case SampleData.SampleType.Text:
				return tableView.ReuseOrCreateCell(_textCellPrefab);
			case SampleData.SampleType.Image:
				return tableView.ReuseOrCreateCell(_imageCellPrefab);
			case SampleData.SampleType.Tab:
				return tableView.ReuseOrCreateCell(_tabCellPrefab, UITableViewCellLifeCycle.RecycleWhenReloaded);
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return _selectedTabIndex == 0 ? _tab1DataList.Count : _tab2DataList.Count;
	}

	public float ScalarForCellInTableView(UITableView tableView, int index)
	{
		if (_selectedTabIndex == 0)
		{
			return _tab1DataList[index].scalar;
		}
		return _tab2DataList[index].scalar;
	}
	#endregion

	#region IUITableViewDelegate
	public void CellAtIndexInTableViewDidAppear(UITableView tableView, int index)
	{
		UITableViewCellLifeCycle lifeCycle;
		var data = _selectedTabIndex == 0 ? _tab1DataList[index] : _tab2DataList[index];
		switch (data.sampleType)
		{
			case SampleData.SampleType.Text:
				var textCell = tableView.GetLoadedCell<SampleTextCell>(index);
				textCell.UpdateData(index, _selectedTabIndex, data.text);
				lifeCycle = textCell.lifeCycle;
				break;
			case SampleData.SampleType.Image:
				var imageCell = tableView.GetLoadedCell<SampleImageCell>(index);
				imageCell.UpdateData(index, _selectedTabIndex, data);
				lifeCycle = imageCell.lifeCycle;
				break;
			case SampleData.SampleType.Tab:
				var tabCell = tableView.GetLoadedCell<SampleTabCell>(index);
				tabCell.UpdateData(_selectedTabIndex, OnTabClicked);
				lifeCycle = tabCell.lifeCycle;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		Debug.Log($"Cell at index:{index} is appeared. UITableViewLifeCycle is <color=green>{lifeCycle}</color>");
	}

	public void CellAtIndexInTableViewWillDisappear(UITableView tableView, int index)
	{
		// Debug.Log($"cell at index:{index} will disappear. <color=red>recycle: {willBeRecycled}</color>");
	}
	#endregion
}
