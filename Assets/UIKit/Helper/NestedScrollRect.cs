using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIKit.Helper
{
	/// <summary>
	/// This script is used to implement a Netflix-like interface,
	/// which scrollrect that be able to scroll up and down,
	/// but also be able to scroll rows of buttons left and right.
	/// ref: https://discussions.unity.com/t/nested-scrollrect/550540/9
	/// </summary>
	public class NestedScrollRect : ScrollRect
	{
		bool _routeToParent;

		/// <summary> Do action for all parents </summary>
		void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
		{
			var parent = transform.parent;
			while (parent) {
				foreach (var component in parent.GetComponents<Component>()) {
					if (component is T)
						action((T)(IEventSystemHandler)component);
				}

				parent = parent.parent;
			}
		}

		/// <summary> Always route initialize potential drag event to parents </summary>
		public override void OnInitializePotentialDrag(PointerEventData eventData)
		{
			DoForParents<IInitializePotentialDragHandler>(parent => { parent.OnInitializePotentialDrag(eventData); });
			base.OnInitializePotentialDrag(eventData);
		}

		public override void OnDrag(PointerEventData eventData)
		{
			if (_routeToParent)
				DoForParents<IDragHandler>(parent => { parent.OnDrag(eventData); });
			else
				base.OnDrag(eventData);
		}

		public override void OnBeginDrag(PointerEventData eventData)
		{
			if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
				_routeToParent = true;
			else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
				_routeToParent = true;
			else
				_routeToParent = false;

			if (_routeToParent)
				DoForParents<IBeginDragHandler>(parent => { parent.OnBeginDrag(eventData); });
			else
				base.OnBeginDrag(eventData);
		}

		public override void OnEndDrag(PointerEventData eventData)
		{
			if (_routeToParent)
				DoForParents<IEndDragHandler>(parent => { parent.OnEndDrag(eventData); });
			else
				base.OnEndDrag(eventData);
			_routeToParent = false;
		}

#if UNITY_EDITOR
		public static void ExchangeBetweenScrollRectAndNestedScrollRect(ScrollRect scrollRect)
		{
			if (scrollRect is null) return;
			var temp = scrollRect is NestedScrollRect 
				? new GameObject().AddComponent<ScrollRect>() 
				: new GameObject().AddComponent<NestedScrollRect>();
			var scriptReference = new SerializedObject(temp).FindProperty("m_Script").objectReferenceValue;
			DestroyImmediate(temp.gameObject);
			var serializedObject = new SerializedObject(scrollRect);
			serializedObject.FindProperty("m_Script").objectReferenceValue = scriptReference;
			serializedObject.ApplyModifiedProperties();
		}
#endif
	}
}
