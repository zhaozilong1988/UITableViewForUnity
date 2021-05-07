using System.Collections.Generic;
using UnityEngine;
using UIKit;

public class SampleGridImplementation : MonoBehaviour, IUIGridViewDataSource, IUITableViewDelegate
{
	[SerializeField]
	private UITableView _tableView;
	[SerializeField]
	private SampleGridCell _gridCellPrefab;
	[SerializeField]
	private GameObject _parent;

	private readonly List<int> _dataList = new List<int>();

	private void Awake()
	{
		// Prepare for data source
		for (var i = 0; i < 100; i++) {
			_dataList.Add(i);
		}

		// Setup table view
		_tableView.dataSource = this;
		_tableView.@delegate = this;
	}

	private void Start()
	{
		_tableView.ReloadData(0);
		_tableView.ScrollToCellAt(0, withMargin: true);
	}

	public void OnClickClose()
	{
		_parent.SetActive(false);
	}

	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
	{
		return tableView.ReuseOrCreateCell(_gridCellPrefab);
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return _dataList.Count;
	}

	public float ScalarForCellInTableView(UITableView tableView, int index)
	{
		return 200f;
	}

	public int NumberOfCellsAtRowOrColumnInGrid(UITableView tableView)
	{
		return 3;
	}

	public UITableViewCellAlignment AlignmentOfCellsAtRowOrColumnInGrid(UITableView grid)
	{
		return UITableViewCellAlignment.Center;
	}

	public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
	{
		tableView.GetLoadedCell<SampleGridCell>(index).UpdateData(index);
	}

	public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
	{
		// throw new NotImplementedException();
	}
}
