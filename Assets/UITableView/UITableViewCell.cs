using UnityEngine;

namespace UITableViewForUnity
{
	public class UITableViewCell : MonoBehaviour
	{
		public RectTransform rectTransform { get; private set; }

		public string reuseIdentifier { get; set; }
		public bool isAutoResize { get; set; }

		public bool isReusable => !string.IsNullOrEmpty(reuseIdentifier);

		public bool isReused { get; set; }

		protected virtual void Awake()
		{
			rectTransform = (RectTransform)transform;
		}
	}
}
