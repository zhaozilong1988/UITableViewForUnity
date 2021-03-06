﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UIKit;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SampleTableViewImplementation : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin
{
	[SerializeField]
	private UITableView _tableView;
	[SerializeField]
	private GameObject _gridView;
	[SerializeField, Header("cell prefabs")]
	private SampleImageCell _imageCellPrefab;
	[SerializeField]
	private SampleTextCell _textCellPrefab;
	[SerializeField]
	private SampleTabCell _tabCellPrefab;
	[SerializeField]
	private SampleChatCell _chatCellPrefab;
	[Space]
	[SerializeField]
	private InputField _cellIndexInput;
	[SerializeField]
	private InputField _chatInput;
	[SerializeField]
	private Text _textSizeCalculator;

	private readonly List<SampleData> _tab1DataList = new List<SampleData>();
	private readonly List<SampleData> _tab2DataList = new List<SampleData>();
	private int _selectedTabIndex;
	private ScreenOrientation _lastOrientation;

	private void Awake()
	{
		// Prepare for data source
		_tab1DataList.Add(CreateSampleForTab());
		_tab1DataList.AddRange(CreateSamples(10));
		_tab2DataList.Add(CreateSampleForTab());
		_tab2DataList.AddRange(CreateSamples(30));

		// Setup table view
		_tableView.dataSource = this;
		_tableView.marginDataSource = this;
		_tableView.@delegate = this;
	}

	private void Start()
	{
		_lastOrientation = Screen.orientation;
		// Reload from 0th
		_tableView.ReloadData(0);
		_tableView.ScrollToCellAt(0, withMargin: true);
	}

	private void Update()
	{
		if (_lastOrientation != Screen.orientation)
		{
			_lastOrientation = Screen.orientation;
			_tableView.ReloadData();
		}
	}

	private static SampleData CreateSampleForTab()
	{
		var data = new SampleData
		{
			sampleType = SampleData.SampleType.Tab,
			scalar = 120f
		};
		return data;
	}

	private IEnumerable<SampleData> CreateSamples(int count)
	{
		for (var i = 0; i < count; i++)
		{
			var data = new SampleData();
			if (Random.Range(i, count) % 2 == 0)
			{
				data.sampleType = SampleData.SampleType.Text;
				data.scalar = 75f + Random.Range(0f, 100f);
				data.text = "https://www.freepik.com/free-photos-vectors/character";
			}
			else
			{
				data.sampleType = SampleData.SampleType.Image;
				data.scalar = 200f;
				data.rarity = Random.Range(1, 5);
				data.spriteIndex = Random.Range(0, 8);
				data.scalarBeforeExpand = data.scalar;
				data.scalarAfterExpand = data.scalar + 300;
				data.onExpand = Expand;
			}

			yield return data;
		}
	}

	private void OnTabClicked(int tabIndex)
	{
		if (_selectedTabIndex == tabIndex) 
			return;
		_tableView.UnloadData();
		_selectedTabIndex = tabIndex;
		_tableView.ReloadData(0);
	}

	public void OnClickScrollTo()
	{
		var index = int.Parse(_cellIndexInput.text);
		_tableView.ScrollToCellAt(index, withMargin: false);
	}

	public void OnClickScrollTo(float time)
	{
		var index = int.Parse(_cellIndexInput.text);
		Debug.Log("Go to cell at index of " + index);
		_tableView.ScrollToCellAt(index, time, withMargin: true, onScrollingFinished: () => {
			Debug.Log("Scrolling has finished");
		});
	}

	public void OnClickPrepend()
	{
		var dataList = _selectedTabIndex == 0 ? _tab1DataList : _tab2DataList;
		dataList.InsertRange(0, CreateSamples(5));
		_tableView.PrependData();
	}

	public void OnClickAppend()
	{
		var dataList = _selectedTabIndex == 0 ? _tab1DataList : _tab2DataList;
		dataList.AddRange(CreateSamples(5));
		_tableView.AppendData();
	}

	private float CalculateTextHeight(string text, float textWidth, int fontSize)
	{
		_textSizeCalculator.fontSize = fontSize;
		var size = _textSizeCalculator.rectTransform.sizeDelta;
		size.x = textWidth;
		_textSizeCalculator.rectTransform.sizeDelta = size;
		_textSizeCalculator.text = text;
		return _textSizeCalculator.preferredHeight;
	}

	public void OnClickSend()
	{
		if (string.IsNullOrEmpty(_chatInput.text))
			return;
		var height = CalculateTextHeight(_chatInput.text, SampleChatCell.MESSAGE_TEXT_WIDTH, SampleChatCell.MESSAGE_FONT_SIZE);
		var data = new SampleData
		{
			sampleType = SampleData.SampleType.Chat,
			scalar = height,
			text = _chatInput.text
		};
		var dataList = _selectedTabIndex == 0 ? _tab1DataList : _tab2DataList;
		dataList.Add(data);
		_tableView.AppendData();
		_tableView.ScrollToCellAt(dataList.Count-1, 0.1f, withMargin: true);
		_chatInput.text = "";
	}

	private void Expand(int index)
	{
		StartCoroutine(CoExpandOrClose(index));
	}

	private IEnumerator CoExpandOrClose(int index)
	{
		var dataList = _selectedTabIndex == 0 ? _tab1DataList : _tab2DataList;
		var sampleData = dataList[index];
		var start = Time.time;

		var progress = 0f;
		while (!Mathf.Approximately(progress, 1f))
		{
			yield return null;
			progress = Mathf.Min((Time.time - start) / 0.1f, 1f);
			dataList[index].scalar = sampleData.isExpanded 
				? Mathf.Lerp(sampleData.scalarBeforeExpand, sampleData.scalarAfterExpand, progress)
				: Mathf.Lerp(sampleData.scalarAfterExpand, sampleData.scalarBeforeExpand, progress);
			_tableView.RearrangeData();
			_tableView.ScrollToCellAt(index, withMargin: false);
		}
	}

	public void OnClickGrid()
	{
		_gridView.SetActive(true);
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
			case SampleData.SampleType.Chat:
				return tableView.ReuseOrCreateCell(_chatCellPrefab);
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
		return _selectedTabIndex == 0 ? _tab1DataList[index].scalar : _tab2DataList[index].scalar;
	}

	#endregion

	#region IUITableViewDelegate
	public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
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
			case SampleData.SampleType.Chat:
				var chatCell = tableView.GetLoadedCell<SampleChatCell>(index);
				chatCell.UpdateData(index, data.text);
				lifeCycle = chatCell.lifeCycle;
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		Debug.Log($"Cell at index:{index} is appeared. UITableViewLifeCycle is <color=green>{lifeCycle}</color>");
	}

	public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
	{
		var data = _selectedTabIndex == 0 ? _tab1DataList[index] : _tab2DataList[index];
		if (data.sampleType == SampleData.SampleType.Image)
		{
			var imageCell = tableView.GetLoadedCell<SampleImageCell>(index);
			imageCell.ClearUp();
		}
		Debug.Log($"cell at index:{index} will disappear.");
	}
	#endregion

	public float ScalarForUpperMarginInTableView(UITableView tableView, int index)
	{
		return index == 0 ? 100f : 0f;
	}

	public float ScalarForLowerMarginInTableView(UITableView tableView, int index)
	{
		var dataList = _selectedTabIndex == 0 ? _tab1DataList : _tab2DataList;
		return index == dataList.Count - 1 ? 100f : 0f;
	}
}
