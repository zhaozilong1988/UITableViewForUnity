using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleChatCell : UITableViewCell
{
	[SerializeField]
	private Text _text;
	[SerializeField]
	private Text _indexText;

	public void UpdateData(int cellIndex, string text)
	{
		_text.text = text;
		_indexText.text = cellIndex.ToString();
	}
}
