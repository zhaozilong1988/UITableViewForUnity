using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class ChatCell : UITableViewCell
{
	[SerializeField] Image _bg;
	[SerializeField] Text _text;

	public void UpdateData(string text, int fontSize, float bubbleWidth, float bubbleHeight)
	{
		_text.text = text;
		_text.fontSize = fontSize;
		const float minimumWidth = 150f;
		_bg.rectTransform.sizeDelta = new Vector2(Mathf.Max(bubbleWidth, minimumWidth), bubbleHeight);
	}

	public void UnloadData()
	{
		_text.text = string.Empty;
	}
}
