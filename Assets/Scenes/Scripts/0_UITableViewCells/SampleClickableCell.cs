using System;
using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleClickableCell : UITableViewCell
{
	[SerializeField] Text _label;
	[SerializeField] Image _bg;

	public void UpdateData(SampleClickableTableView.Meta meta)
	{
		_bg.color = meta.selected ? Color.red : Color.white;

		switch (meta.title) {
			case SampleClickableTableView.Title.TableOrGrid:
				_label.text = "Table/Grid";
				break;
			case SampleClickableTableView.Title.Drag:
				_label.text = "Enter Draggable Mode";
				break;
			case SampleClickableTableView.Title.Delete:
				_label.text = "Enter Deletable Mode";
				break;
			case SampleClickableTableView.Title.Append:
				_label.text = "Append 5 cells";
				break;
			case SampleClickableTableView.Title.Prepend:
				_label.text = "Prepend 5 cells";
				break;
			case SampleClickableTableView.Title.ReverseDirection:
				_label.text = "Reverse\nDirection";
				break;
			case SampleClickableTableView.Title.ScrollTo:
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}