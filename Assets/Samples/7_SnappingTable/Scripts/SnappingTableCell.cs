using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class SnappingTableCell : UITableViewCell
	{
		[SerializeField] Image _bg;
		[SerializeField] Image _monster;
		[SerializeField] Text _number;
		[SerializeField] Sprite[] _monsterSprites;

		public void UpdateData()
		{
			var idx = this.index.GetValueOrDefault();
			_number.text = idx.ToString();
			_monster.sprite = _monsterSprites[idx % _monsterSprites.Length];
			_bg.color = Color.white;
		}
	}
}