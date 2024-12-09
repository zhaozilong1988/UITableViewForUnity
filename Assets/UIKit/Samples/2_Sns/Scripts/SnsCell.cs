using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class SnsCell : UITableViewCell
	{
		[SerializeField] Image _photo;
		[SerializeField] Text _name;
		[SerializeField] Text _msg;

		public void UpdateData(string username, string msg, int msgFontSize)
		{
			_name.text = username;
			_msg.fontSize = msgFontSize;
			_msg.text = msg;
		}

		public void UnloadData()
		{
			_name.text = string.Empty;
			_msg.text = string.Empty;
		}
	}
}
