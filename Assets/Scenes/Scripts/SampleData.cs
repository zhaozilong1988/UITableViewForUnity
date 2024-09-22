
using System;

public class SampleData
{
	public enum SampleType
	{
		Text,
		Image,
		Tab,
		Chat,
		NestedScrollRect,
	}

	public SampleType sampleType;
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
