using System;
using System.Collections.Generic;
using UIKit;
using UnityEngine;
using UnityEngine.EventSystems;

public class SampleClickableTableView : UITableView, IUITableViewDataSource, IUITableViewDelegate, IUITableViewClickable, IUITableViewMargin
{
	[SerializeField] SampleClickableCell _clickableCell;
	[SerializeField] SampleScrollToCell _scrollToCell;
	List<Meta> _metaList;

	public Action onClickTableOrGridViewCell;
	public Action<int> onClickScrollToCell;
	public Action onClickPrependCell;
	public Action onClickAppendCell;
	public Action onClickDragCell;
	public Action onClickDeleteCell;
	public Action onClickReverseDirection;

	protected override void Awake()
	{
		base.Awake();
		dataSource = this;
		@delegate = this;
		marginDataSource = this;
		clickable = this;
	}

	public void ReloadDataForGridView()
	{
		Reload(Title.TableOrGrid, Title.ReverseDirection, Title.Drag, Title.Delete);
		ReloadData(0);
	}

	public void ReloadDataForTableView()
	{
		Reload(Title.TableOrGrid, Title.ReverseDirection, Title.Append, Title.Prepend, Title.ScrollTo);
		ReloadData(0);
	}

	void Reload(params Title[] titles)
	{
		_metaList = _metaList ?? new List<Meta>();
		_metaList.Clear();
		foreach (var t in titles) {
			_metaList.Add(new Meta(t));
		}
	}

	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
	{
		if (_metaList[index].title == Title.ScrollTo) {
			return tableView.ReuseOrCreateCell(_scrollToCell);
		}
		return tableView.ReuseOrCreateCell(_clickableCell);
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return _metaList.Count;
	}

	public float LengthForCellInTableView(UITableView tableView, int index)
	{
		return 200f;
	}

	public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
	{
		if (_metaList[index].title != Title.ScrollTo) {
			tableView.GetLoadedCell<SampleClickableCell>(index).UpdateData(_metaList[index]);
		}
	}

	public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
	{
	}

	public Camera TableViewCameraForInteractive(UITableView tableView)
	{
		return null; // Because the render mode of canvas which tableview on is overlay.
	}

	public void TableViewOnPointerDownCellAt(UITableView tableView, int index, PointerEventData eventData)
	{
	}

	public void TableViewOnPointerClickCellAt(UITableView tableView, int index, PointerEventData eventData)
	{
		Debug.Log($"{_metaList[index].title} cell is clicked.");
		switch (_metaList[index].title) {
			case Title.TableOrGrid:
				this.onClickTableOrGridViewCell.Invoke();
				break;
			case Title.ScrollTo:
				var toIndex = tableView.GetLoadedCell<SampleScrollToCell>(index).scrollToCellIndex;
				this.onClickScrollToCell.Invoke(toIndex);
				break;
			case Title.Prepend:
				this.onClickPrependCell.Invoke();
				break;
			case Title.Append:
				this.onClickAppendCell.Invoke();
				break;
			case Title.Drag:
				for (int i = 0; i < _metaList.Count; i++) {
					_metaList[i].selected = i == index && !_metaList[i].selected;
				}
				this.onClickDragCell.Invoke();
				break;
			case Title.Delete:
				for (int i = 0; i < _metaList.Count; i++) {
					_metaList[i].selected = i == index && !_metaList[i].selected;
				}
				this.onClickDeleteCell.Invoke();
				break;
			case Title.ReverseDirection:
				this.onClickReverseDirection.Invoke();
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		ReloadData();
	}

	public void TableViewOnPointerUpCellAt(UITableView tableView, int index, PointerEventData eventData)
	{
	}

	public float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex)
	{
		if (rowIndex == 0) {
			return 10f;
		}
		return 5f;
	}

	public float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex)
	{
		return 5f;
	}

	public enum Title
	{
		TableOrGrid,
		Drag,
		Delete,
		ReverseDirection,
		Append,
		Prepend,
		ScrollTo,
	}
	
	public class Meta
	{
		public Title title;
		public bool selected;

		public Meta(Title t)
		{
			this.title = t;
			this.selected = false;
		}
	}
}