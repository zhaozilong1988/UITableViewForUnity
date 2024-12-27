using System;
using System.Collections.Generic;
using UIKit;
using UnityEngine;
using UnityEngine.EventSystems;

public class SampleClickableTableView : UITableView, IUITableViewDataSource, IUITableViewDelegate, IUITableViewClickable, IUITableViewMargin, IUITableViewFlickable, IUITableViewMagnetic
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
	
	// b: 545,600
	// 8 ~ 12: 2, 795, 000
	// 1~7: 3, 848, 000
	// c: 160, 000
	// total: 6, 403, 600

	protected override void Awake()
	{
		base.Awake();
		dataSource = this;
		@delegate = this;
		marginDataSource = this;
		clickable = this;

		magnetic = this;
		flickable = this;
	}

	public void ReloadDataForGridView()
	{
		Reload(Title.TableOrGrid, Title.ReverseDirection, Title.Drag, Title.Delete);
		ReloadData(0);
	}

	public void ReloadDataForTableView()
	{
		Reload(Title.TableOrGrid, Title.ReverseDirection, Title.Append, Title.Prepend);
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
		return 300f;
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
		Debug.Log($"{index} cell is clicked.");

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

	public void TableViewOnDidFlick(UITableView tableView, int? indexOfFlickedCell, UITableViewDirection direction)
	{
		if (indexOfFlickedCell.HasValue) {
			var toIndex = direction.IsTopToBottomOrRightToLeft() ? indexOfFlickedCell.Value + 1 : indexOfFlickedCell.Value - 1;
			toIndex = Mathf.Clamp(toIndex, 0, _metaList.Count - 1);
			ScrollToCellAt(toIndex, 0.5f);
		}
	}

	public Vector2 MagneticCalibrationPointInTableView(UITableView tableView)
	{
		// セルの基準点は中央なので、左寄せの場合、「セルの長さ * 0.5 / viewportの幅」はページングの照準点になる。
		return new Vector2(LengthForCellInTableView(this, 0) * 0.5f / scrollRect.viewport.rect.width, 0.5f);
	}

	public void MagneticStateDidChangeInTableView(UITableView tableView, int ofCellIndex, UITableViewMagneticState state)
	{
		// Debug.Log("MagneticStateDidChangeInTableView: " + ofCellIndex);
	}
}