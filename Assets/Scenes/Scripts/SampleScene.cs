using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleScene : MonoBehaviour
{
	[SerializeField] InputField _chatInput;
	[SerializeField] Text _textSizeCalculator;
	[SerializeField] SampleTableView _tableView;
	[SerializeField] SampleGridView _gridView;
	[SerializeField] SampleClickableTableView _clickableTableView;

	void Awake()
	{
		_clickableTableView.onClickAppendCell = OnClickAppendCell;
		_clickableTableView.onClickPrependCell = OnClickPrependCell;
		_clickableTableView.onClickScrollToCell = OnClickScrollToCell;
		_clickableTableView.onClickTableOrGridViewCell = OnClickTableOrGridViewCell;
		_clickableTableView.onClickDragCell = OnClickDragCell;
		_clickableTableView.onClickDeleteCell = OnClickDeleteCell;
		_clickableTableView.onClickReverseDirection = () => {
			if (_gridView.gameObject.activeSelf) {
				_gridView.direction = _gridView.direction == UITableViewDirection.TopToBottom
					? UITableViewDirection.BottomToTop
					: UITableViewDirection.TopToBottom;
			} else {
				_tableView.direction = _tableView.direction == UITableViewDirection.TopToBottom
					? UITableViewDirection.BottomToTop
					: UITableViewDirection.TopToBottom;
			}
		};
	}

	void Start()
	{
		OnClickTableOrGridViewCell();
	}

	void OnClickTableOrGridViewCell()
	{
		if (_gridView.gameObject.activeSelf) {
			_gridView.UnloadData();
			_gridView.gameObject.SetActive(false);

			_tableView.gameObject.SetActive(true);
			_tableView.ReloadData(0);
			
			_clickableTableView.ReloadDataForTableView();
		} else {
			_gridView.gameObject.SetActive(true);
			_gridView.ReloadData(0);

			_tableView.UnloadData();
			_tableView.gameObject.SetActive(false);

			_clickableTableView.ReloadDataForGridView();
		}
	}

	void OnClickDragCell()
	{
		if (_gridView.gameObject.activeSelf) {
			_gridView.SwitchDragMode();
		}
	}

	void OnClickDeleteCell()
	{
		if (_gridView.gameObject.activeSelf) {
			_gridView.SwitchDeleteMode();
		}
	}

	void OnClickScrollToCell(int index)
	{
		Debug.Log("Scroll to cell at index of " + index);
		_tableView.ScrollToCellAt(index, 0.3f, withMargin: true, onScrollingFinished: () => {
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

	float CalculateTextHeight(string text, float textWidth, int fontSize)
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
		_tableView.PrependData(data);
		_chatInput.text = "";
	}
}