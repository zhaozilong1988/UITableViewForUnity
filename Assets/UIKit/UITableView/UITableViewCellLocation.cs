namespace UIKit
{
	/// <summary> Use to locate a point in UITableView(scroll view's content).  </summary>
	public struct UITableViewCellLocation
	{
		/// <summary> Index of the cell. </summary>
		public int index;
		/// <summary> Alignment of the cell on UITableView. </summary>
		public UITableViewCellAlignment alignment;
		/// <summary> If TRUE, calculate margin(IUITableViewMargin) when locate the cell. </summary>
		public bool withMargin;
		/// <summary> The displacement relative to the cell.
		/// Positive number for move up,
		/// and negative number for move down.</summary>
		public float displacement;

		public UITableViewCellLocation(int index, UITableViewCellAlignment alignment, bool withMargin, float displacement)
		{
			this.index = index;
			this.alignment = alignment;
			this.withMargin = withMargin;
			this.displacement = displacement;
		}
	}
}
