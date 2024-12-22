using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleScene : MonoBehaviour
{
	[SerializeField] InputField _chatInput;
	[SerializeField] Text _textSizeCalculator;
	[SerializeField] SampleTableView _tableView;
	[SerializeField] SampleClickableTableView _clickableTableView;

	void Awake()
	{
		_clickableTableView.onClickAppendCell = OnClickAppendCell;
		_clickableTableView.onClickPrependCell = OnClickPrependCell;
		_clickableTableView.onClickScrollToCell = OnClickScrollToCell;
		_clickableTableView.onClickTableOrGridViewCell = OnClickTableOrGridViewCell;
		_clickableTableView.onClickReverseDirection = () => {
			_tableView.direction = _tableView.direction == UITableViewDirection.TopToBottom
				? UITableViewDirection.BottomToTop
				: UITableViewDirection.TopToBottom;
		};
	}

	void Start()
	{
		OnClickTableOrGridViewCell();
	}

	void OnClickTableOrGridViewCell()
	{
		_tableView.UnloadData();
		_tableView.gameObject.SetActive(false);

		_clickableTableView.ReloadDataForGridView();
	}

	void OnClickScrollToCell(int index)
	{
		Debug.Log("Scroll to cell at index of " + index);
		_tableView.ScrollToCellAt(index, 0.3f, withMargin: true, onScrollingStopped: interrupted => {
			Debug.Log("Scrolling has finished");
		});

		// _tableView.ScrollToCellAt(index, withMargin: false); // without animation
	}

	void OnClickPrependCell()
	{
		if (_tableView.gameObject.activeSelf) {
			_tableView.OnClickPrepend();
		}
	}

	void OnClickAppendCell()
	{
		if (_tableView.gameObject.activeSelf) {
			_tableView.OnClickAppend();
		}
	}
}