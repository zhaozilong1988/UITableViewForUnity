using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class NestedCell : UITableViewCell
	{
		[SerializeField] Image _icon; 
		[SerializeField] Text _ranking;
		

		public void UpdateData(bool isApp, Sprite icon)
		{
			_ranking.text = isApp ? string.Empty : (this.index.GetValueOrDefault() + 1).ToString();
			_icon.sprite = icon;
		}
		
		public void UnloadData()
		{
			_icon.sprite = null;
			_ranking.text = string.Empty;
		}
	}
}
