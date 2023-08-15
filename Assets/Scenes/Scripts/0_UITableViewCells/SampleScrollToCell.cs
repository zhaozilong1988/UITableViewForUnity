using UIKit;
using UnityEngine;
using UnityEngine.UI;

public class SampleScrollToCell : UITableViewCell
{
	[SerializeField] InputField _cellIndexInput;

	public int scrollToCellIndex => int.Parse(_cellIndexInput.text);
}