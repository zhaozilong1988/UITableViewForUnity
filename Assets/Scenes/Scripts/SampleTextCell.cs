using UITableViewForUnity;
using UnityEngine;
using UnityEngine.UI;

public class SampleTextCell : UITableViewCell
{
	[SerializeField]
	private Text _text;

	public void UpdateData(string text)
	{
		_text.text = text;
	}
}
