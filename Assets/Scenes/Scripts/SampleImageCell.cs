using UITableViewForUnity;
using UnityEngine;
using UnityEngine.UI;

public class SampleImageCell : UITableViewCell
{
	[SerializeField]
	private Image _image;

	public void UpdateData(Sprite sprite)
	{
		_image.sprite = sprite;
	}
}
