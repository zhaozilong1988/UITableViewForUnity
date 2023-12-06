using UnityEngine;
using UIKit.Helper;

namespace UIKit
{
	public class UITableViewCell : MonoBehaviour
	{
		public RectTransform rectTransform { get; private set; }
		public string reuseIdentifier { get; internal set; }
		/// <summary> TRUE, Resize the cell when it is loaded or rearranged, or not if FALSE. Then you should be to resize it manually for your needs. </summary>
		public bool isAutoResize { get; internal set; }
		public UITableViewCellLifeCycle lifeCycle { get; internal set; }
		/// <summary> The index of cell in table view, top->bottom:0~n, right->left:0~n. Null when cell is unloaded. </summary>
		public int? index { get; internal set; }
		public Rect worldRect => rectTransform.WorldRect();

		protected virtual void Awake()
		{
			rectTransform = (RectTransform)transform;
		}
	}

	/// <summary> How the cell will be when it disappeared from scroll view's viewport or data is reloaded. </summary>
	public enum UITableViewCellLifeCycle
	{
		/// <summary> The cell will be put into reuse pool when it disappeared from scroll view's viewport. </summary>
		RecycleWhenDisappeared,
		/// <summary> The cell will not be destroyed or put into reuse pool once appeared until UITableView.ReloadData() is called. </summary>
		RecycleWhenReloaded,
		/// <summary> The cell will be destroyed when it disappeared from scroll view's viewport. </summary>
		DestroyWhenDisappeared,
	}
}
