using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleGridCell : UITableViewCell
{
	[SerializeField]
	private Text _indexText;

	public void UpdateData(int cellIndex)
	{
		_indexText.text = cellIndex.ToString();
	}
}
