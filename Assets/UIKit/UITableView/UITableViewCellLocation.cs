namespace UIKit
{
	/// <summary> Use to locate a point in UITableView(scroll view's content).  </summary>
	public struct UITableViewCellLocation
	{
		/// <summary> Index of the cell. </summary>
		public int index;
		/// <summary> Alignment of the cell on UITableView. </summary>
		public UITableViewAlignment alignment;
		/// <summary> If TRUE, calculate margin(IUITableViewMargin) when locate the cell. </summary>
		public bool withMargin;
		/// <summary> The displacement relative to the cell.
		/// Positive value for move up or right,
		/// and negative value for move down or left.</summary>
		public float displacement;

		public UITableViewCellLocation(int index, UITableViewAlignment alignment, bool withMargin, float displacement)
		{
			this.index = index;
			this.alignment = alignment;
			this.withMargin = withMargin;
			this.displacement = displacement;
		}
	}
}
