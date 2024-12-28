using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class AdvancedTextCell : UITableViewCell
	{
		[SerializeField] Text _text;
		[SerializeField] Text _indexText;
		[SerializeField] Image[] _deepColorImages;
		[SerializeField] Image[] _paleColorImages;
		[SerializeField] Color _deepGreen;
		[SerializeField] Color _paleGreen;
		[SerializeField] Color _deepOrange;
		[SerializeField] Color _paleOrange;

		public void UpdateData(int cellIndex, string text)
		{
			_text.text = text;
			_indexText.text = cellIndex.ToString();
			foreach (var img in _deepColorImages) {
				img.color = _deepGreen;
			}

			foreach (var img in _paleColorImages) {
				img.color = _paleGreen;
			}
		}
	}
}
