using System;

namespace UIKit
{
	internal enum UITableViewMagneticInternalState
	{
		Scrolling,
		Attracting,
		Stopped,
	}
	public enum UITableViewMagneticState
	{
		Start,
		Completed,
		Interrupted,
	}

	public enum UITableViewAlignment
	{
		/// <summary> Right alignment at row on UITableViewDirection.TopToBottom, or top alignment at column on UITableViewDirection.RightToLeft. </summary>
		RightOrTop = 0,
		/// <summary> Centering </summary>
		Center = 1,
		/// <summary> Left alignment at row on UITableViewDirection.TopToBottom, or bottom alignment at column on UITableViewDirection.RightToLeft. </summary>
		LeftOrBottom = 2,
	}

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