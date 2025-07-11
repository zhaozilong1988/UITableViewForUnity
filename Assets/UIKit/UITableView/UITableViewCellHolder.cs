namespace UIKit
{
	internal class UITableViewCellHolder
	{
		public UITableViewCell loadedCell { get; set; }

		/// <summary> The width of the cell in the column of its row. </summary>
		public float columnWidth { get; set; }

		/// <summary> The length of the cell in the scrolling direction. </summary>
		public float rowLength { get; set; }

		/// <summary> Upper margin (top or right) for cell. </summary>
		public float upperRowMargin { get; set; }

		/// <summary> Lower margin (bottom or left) for cell. </summary>
		public float lowerRowMargin { get; set; }

		/// <summary> The position relative to scroll view's content without considering anchor. </summary>
		public float rowPosition { get; set; }

		/// <summary> The position of the cell in the column of its row. </summary>
		public float columnPosition { get; set; }

		/// <summary> If the direction is Top ⇔ Bottom, the row is horizontal direction, or vertical direction in Right to Left. </summary>
		public int rowIndex { get; set; }

		/// <summary> If the direction is Top ⇔ Bottom, the column is vertical direction, or horizontal direction in Right to Left. </summary>
		public int columnIndex { get; set; }

		/// <summary> Larger sibling orders correspond to older sibling indexes in the table view’s content. </summary>
		public int siblingOrder { get; set; }
	}
}
