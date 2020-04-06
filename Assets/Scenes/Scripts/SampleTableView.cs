using UnityEngine;
using UITableViewForUnity;

public class SampleTableView : MonoBehaviour, IUITableViewDataSource, IUITableViewLifecycle
{
	[SerializeField]
	private UITableView _tableViewV;
	[SerializeField]
	private UITableView _tableViewH;
	[SerializeField]
	private SampleTextCell _textCellPrefab;
	[SerializeField]
	private SampleImageCell _imageCellPrefab;
	[SerializeField]
	private Sprite[] _sprites;

	private const float TEXT_CELL_LENGTH = 50f;
	private const float IMAGE_CELL_LENGTH = 100f;

	void Start()
	{
		_tableViewV.dataSource = this;
		_tableViewV.lifecycle = this;
		_tableViewV.ReloadData();
		
		_tableViewH.dataSource = this;
		_tableViewH.lifecycle = this;
		_tableViewH.ReloadData();
	}

	public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
	{
		if (index % 2 == 0)
		{
			return tableView.DequeueOrCreateCell(_textCellPrefab, "TextCell", true);
		}
		return tableView.DequeueOrCreateCell(_imageCellPrefab, "ImageCell", true);
	}

	public int NumberOfCellsInTableView(UITableView tableView)
	{
		return 100;
	}

	public float LengthForCellInTableView(UITableView tableView, int index)
	{
		if (index % 2 == 0)
		{
			return TEXT_CELL_LENGTH + 100;
		}
		return IMAGE_CELL_LENGTH;
	}

	public void CellAtIndexInTableViewDidAppear(UITableView tableView, int index, bool isReused)
	{
		if (index % 2 == 0)
		{
			var textCell = tableView.GetAppearingCell<SampleTextCell>(index);
			textCell.UpdateData($"index:{index}");
		}
		else
		{
			var imageCell = tableView.GetAppearingCell<SampleImageCell>(index);
			imageCell.UpdateData(_sprites[Random.Range(0, _sprites.Length)]);
		}

		Debug.Log($"index:{index} of cell is <color=green>reused: {isReused}</color>");
	}

	public void CellAtIndexInTableViewWillDisappear(UITableView tableView, int index, bool willBeRecycled)
	{
		Debug.Log($"cell at index:{index} will disappear. <color=red>recycle: {willBeRecycled}</color>");
	}
}
