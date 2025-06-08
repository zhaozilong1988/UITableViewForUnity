using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class SimpleGridCell : UITableViewCell
	{
		[SerializeField] Text _index;
		[SerializeField] Image _bgImage;
		[SerializeField] Image _icon;
		[SerializeField] Sprite[] _monsterSprites;

		public void UpdateData(int cellIndex)
		{
			_index.text = cellIndex.ToString();
			var sprite = _monsterSprites[cellIndex % _monsterSprites.Length];
			_icon.sprite = sprite;
			_bgImage.color = cellIndex % 2 == 0 ? Color.yellow : Color.green;
		}
	}
}
