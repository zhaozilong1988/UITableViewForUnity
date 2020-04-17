using UnityEngine;

namespace UITableViewForUnity
{
	public enum UITableViewCellUsageType
	{
		Reuse,
		NeverUnload,
		DestroyWhenDisappeared,
	}

	public class UITableViewCell : MonoBehaviour
	{
		public RectTransform rectTransform { get; private set; }

		public string reuseIdentifier { get; set; }
		public bool isAutoResize { get; set; }

		public UITableViewCellUsageType usageType { get; set; }

		protected virtual void Awake()
		{
			rectTransform = (RectTransform)transform;
		}
	}
}
