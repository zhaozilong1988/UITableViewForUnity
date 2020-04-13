using UITableViewForUnity;
using UnityEngine;
using UnityEngine.UI;

public class SampleImageCell : UITableViewCell
{
	[SerializeField]
	private Text _indexText;
	[SerializeField]
	private Image[] _deepColorImages;
	[SerializeField]
	private Image[] _paleColorImages;
	[SerializeField]
	private Color _deepGreen;
	[SerializeField]
	private Color _paleGreen;
	[SerializeField]
	private Color _deepOrange;
	[SerializeField]
	private Color _paleOrange;

	public void UpdateData(int cellIndex, int selectedTabIndex)
	{
		_indexText.text = cellIndex.ToString();
		foreach (var img in _deepColorImages)
		{
			img.color = selectedTabIndex == 0 ? _deepGreen : _deepOrange;
		}
		
		foreach (var img in _paleColorImages)
		{
			img.color = selectedTabIndex == 0 ? _paleGreen : _paleOrange;
		}
	}

	public void OnClickButton()
	{
		
	}
}
