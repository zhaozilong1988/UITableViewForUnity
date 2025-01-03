using System;

namespace UIKit.Samples
{
	public class AdvancedCellData
	{
		public enum CellType
		{
			Text,
			Monster,
		}

		public CellType cellType;
		public string text;
		public int rarity;
		public bool isFavorite;
		public float scalar;
		public int spriteIndex;

		// expend
		public float scalarBeforeExpand;
		public float scalarAfterExpand;
		public bool isExpanded = false;
		public Action<int> onExpand;
	}
}
