
using System;

public class SampleData
{
	public enum SampleType
	{
		Text,
		Image,
		Tab
	}

	public SampleType sampleType;
	public string text;
	public int rarity;
	public bool isFavorite;
	public float scalarBeforeExpend;
	public float scalarAfterExpend;
	public float scalar;
	public int spriteIndex;
	public bool isExpended = false;
	public Action<int> onExpend;
}
