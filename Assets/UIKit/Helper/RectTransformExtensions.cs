using System.Drawing;
using UnityEngine;

namespace UIKit.Helper
{
	public static class RectTransformExtensions
	{
		/// <summary> Ref. https://www.appsloveworld.com/csharp/100/234/check-if-ui-elements-recttransform-are-overlapping</summary>
		public static Rect WorldRect(this RectTransform rectTransform)
		{
			var sizeDelta = rectTransform.sizeDelta;
			var lossyScale = rectTransform.lossyScale;
			var rectTransformWidth = sizeDelta.x * lossyScale.x;
			var rectTransformHeight = sizeDelta.y * lossyScale.y;
			// With this it works even if the pivot is not at the center
			var position = rectTransform.TransformPoint(rectTransform.rect.center);
			var x = position.x - rectTransformWidth * 0.5f;
			var y = position.y - rectTransformHeight * 0.5f;
			return new Rect(x, y, rectTransformWidth, rectTransformHeight);
		}

		public static float CalculateAreaOfIntersection(this RectTransform rectTransform, Rect otherWorldRect)
		{
			var thisWorldRect = rectTransform.WorldRect();
			if (!thisWorldRect.Overlaps(otherWorldRect))
				return float.MinValue;
			var rectangle1 = new RectangleF(thisWorldRect.x, thisWorldRect.y, thisWorldRect.width, thisWorldRect.height);
			var rectangle2 = new RectangleF(otherWorldRect.x, otherWorldRect.y, otherWorldRect.width, otherWorldRect.height);
			var intersect = RectangleF.Intersect(rectangle1, rectangle2);
			var area = intersect.Width * intersect.Height;
			return area;
		}
	}

}