using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class SnsCell : UITableViewCell
	{
		[SerializeField] Image _userIcon;
		[SerializeField] Image _photo;
		[SerializeField] Text _name;
		[SerializeField] Text _msg;

		public float upperMsgMargin = 65f;
		public float lowerMsgMargin = 385f;

		public void UpdateData(Sprite userIcon, Sprite photo, string username, string msg, Vector2 textSize, int msgFontSize)
		{
			_userIcon.sprite = userIcon;
			_photo.sprite = photo;
			_name.text = username;
			_msg.text = msg;
			var offsetMax = _msg.rectTransform.offsetMax;
			var offsetMin = _msg.rectTransform.offsetMin;
			offsetMax.y = -upperMsgMargin;
			offsetMin.y = lowerMsgMargin;
			_msg.rectTransform.offsetMax = offsetMax;
			_msg.rectTransform.offsetMin = offsetMin;
			var size = _msg.rectTransform.sizeDelta;
			size.x = textSize.x;
			_msg.rectTransform.sizeDelta = size;
			_msg.fontSize = msgFontSize;
		}

		public void UnloadData()
		{
			_name.text = string.Empty;
			_msg.text = string.Empty;
		}
	}
}
