using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleChatCell : UITableViewCell
{
	[SerializeField]
	private Text _text;
	[SerializeField]
	private Text _indexText;

	public const float MESSAGE_TEXT_WIDTH = 300f;
	public const int MESSAGE_FONT_SIZE = 30;

	protected override void Awake()
	{
		base.Awake();

		var size = _text.rectTransform.sizeDelta;
		size.x = MESSAGE_TEXT_WIDTH;
		_text.rectTransform.sizeDelta = size;
		_text.fontSize = MESSAGE_FONT_SIZE;
	}

	public void UpdateData(int cellIndex, string text)
	{
		_text.text = text;
		_indexText.text = cellIndex.ToString();
	}
}
