using UnityEngine;

namespace UIKit
{
	/// <summary>
	/// How the cell will be when it disappeared from scroll view's viewport or data is reloaded. 
	/// </summary>
	public enum UITableViewCellLifeCycle
	{
		/// <summary>
		/// The cell will be put into reuse pool when it disappeared from scroll view's viewport.
		/// </summary>
		RecycleWhenDisappeared,
		/// <summary>
		/// The cell will not be destroyed or put into reuse pool once appeared until UITableView.ReloadData() is called. 
		/// </summary>
		RecycleWhenReloaded,
		/// <summary>
		/// The cell will be destroyed when it disappeared from scroll view's viewport.
		/// </summary>
		DestroyWhenDisappeared,
	}

	public class UITableViewCell : MonoBehaviour
	{
		public RectTransform rectTransform { get; private set; }
		public string reuseIdentifier { get; internal set; }
		public bool isAutoResize { get; internal set; }
		public UITableViewCellLifeCycle lifeCycle { get; internal set; }

		protected virtual void Awake()
		{
			rectTransform = (RectTransform)transform;
		}
	}
}
