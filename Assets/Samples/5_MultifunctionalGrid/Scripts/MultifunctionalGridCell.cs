﻿using System;
using UIKit;
using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class MultifunctionalGridCell : UITableViewCell
	{
		[SerializeField] Text _indexText;
		[SerializeField] Image _bgImage;
		[SerializeField] Image _icon;
		[SerializeField] Image _editIcon;

		[SerializeField] Sprite _draggableSprite;
		[SerializeField] Sprite _deletableSprite;

		[SerializeField] Sprite[] _monsterSprites;

		Color _initialColor;

		protected override void Awake()
		{
			base.Awake();
			_initialColor = _bgImage.color;
		}

		public void UpdateData(int cellIndex, MultifunctionalGrid.Mode mode)
		{
			_editIcon.enabled = mode != MultifunctionalGrid.Mode.Normal;
			_indexText.text = cellIndex.ToString();
			var sprite = _monsterSprites[cellIndex % _monsterSprites.Length];
			_icon.sprite = sprite;
		
			switch (mode) {
				case MultifunctionalGrid.Mode.Normal:
					break;
				case MultifunctionalGrid.Mode.Draggable:
					_editIcon.sprite = _draggableSprite;
					break;
				case MultifunctionalGrid.Mode.Deletable:
					_editIcon.sprite = _deletableSprite;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}
		}

		public void SetMergeable(bool mergeable)
		{
			_bgImage.color = mergeable ? Color.red : _initialColor;
		}
	}
}