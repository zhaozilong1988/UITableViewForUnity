
using System;

public class SampleData
{
	public enum SampleType
	{
		Text,
		Image,
		Tab,
		Chat,
	}

	public SampleType sampleType;
	public string text;
	public int rarity;
	public bool isFavorite;
	public float scalar;
	public int spriteIndex;

	// expend
	public float scalarBeforeExpend;
	public float scalarAfterExpend;
	public bool isExpended = false;
	public Action<int> onExpend;
}
