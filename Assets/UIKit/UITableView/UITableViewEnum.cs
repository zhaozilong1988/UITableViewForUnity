using System;

namespace UIKit
{
	public enum UITableViewDirection
	{
		/// <summary> Index of cell at the top is zero. </summary>
		TopToBottom = 0,
		/// <summary> Index of cell at the rightmost is zero. </summary>
		RightToLeft = 1,
		/// <summary> Index of cell at the bottom is zero. </summary>
		BottomToTop = 2,
		/// <summary> Index of cell at the leftmost is zero. </summary>
		LeftToRight = 3,
	}

	public static class UITableViewDirectionExtension
	{
		public static bool IsVertical(this UITableViewDirection self)
		{
			switch (self) {
				case UITableViewDirection.TopToBottom:
				case UITableViewDirection.BottomToTop: return true;
				case UITableViewDirection.RightToLeft:
				case UITableViewDirection.LeftToRight: return false;
				default: throw new ArgumentOutOfRangeException(nameof(self), self, null);
			}
		}

		/// <summary> Is same direction to normalized position of ScrollRect. </summary>
		public static bool IsTopToBottomOrRightToLeft(this UITableViewDirection self)
		{
			switch (self) {
				case UITableViewDirection.TopToBottom:
				case UITableViewDirection.RightToLeft: return true;
				case UITableViewDirection.BottomToTop: 
				case UITableViewDirection.LeftToRight: return false;
				default: throw new ArgumentOutOfRangeException(nameof(self), self, null);
			}
		}
	}
}