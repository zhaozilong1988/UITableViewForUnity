using UIKit;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SampleScene : MonoBehaviour
{
	[SerializeField] InputField _chatInput;
	[SerializeField] Text _textSizeCalculator;
	[SerializeField] SampleClickableTableView _clickableTableView;

	void Awake()
	{
		_clickableTableView.onClickScrollToCell = OnClickScrollToCell;
		_clickableTableView.onClickTableOrGridViewCell = OnClickTableOrGridViewCell;
	}

	void Start()
	{
		OnClickTableOrGridViewCell();
	}

	void OnClickTableOrGridViewCell()
	{
		_clickableTableView.ReloadDataForGridView();
	}

	void OnClickScrollToCell(int index)
	{
		Debug.Log("Scroll to cell at index of " + index);

		// _tableView.ScrollToCellAt(index, withMargin: false); // without animation
	}

}