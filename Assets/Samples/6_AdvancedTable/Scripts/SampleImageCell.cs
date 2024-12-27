using System;
using UIKit;
using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class SampleImageCell : UITableViewCell
	{
		[SerializeField] Text _indexText;
		[SerializeField] Image[] _deepColorImages;
		[SerializeField] Image[] _paleColorImages;
		[SerializeField] Color _deepGreen;
		[SerializeField] Color _paleGreen;
		[SerializeField] Color _deepOrange;
		[SerializeField] Color _paleOrange;
		[SerializeField] Color _deepRed;
		[SerializeField] Color _paleRed;
		[SerializeField] Image _frameImage;
		[SerializeField] Sprite[] _monsterSprites;
		[SerializeField] Image[] _rarityImages;

		Action<bool> _onAddToFavoriteClicked;
		SampleData _sampleData;
		int _cellIndex;

		public void UpdateData(int cellIndex, SampleData sampleData)
		{
			_sampleData = sampleData;
			_indexText.text = cellIndex.ToString();
			_cellIndex = cellIndex;

			UpdateBackgroundColor();
		
			_frameImage.sprite = _monsterSprites[sampleData.spriteIndex];
			_frameImage.SetNativeSize();
			var frameRect = _frameImage.rectTransform;
			var scale = 100f / frameRect.sizeDelta.x;
			frameRect.localScale = new Vector3(scale, scale);
			foreach (var rarityImage in _rarityImages)
			{
				rarityImage.enabled = false;
			}
			for (int i = 0; i < sampleData.rarity; i++)
			{
				_rarityImages[i].enabled = true;
			}
		}

		public void ClearUp()
		{
			_frameImage.sprite = null;
			_frameImage.rectTransform.localScale = Vector2.one;
		}

		void UpdateBackgroundColor()
		{
			Color deepColor, paleColor;
			if (_sampleData.isFavorite)
			{
				deepColor = _deepRed;
				paleColor = _paleRed;
			}
			else
			{
				deepColor = _deepOrange;
				paleColor = _paleOrange;
			}

			foreach (var img in _deepColorImages)
			{
				img.color = deepColor;
			}
			foreach (var img in _paleColorImages)
			{
				img.color = paleColor;
			}
		}

		public void OnClickAddToFavoriteButton()
		{
			_sampleData.isFavorite = !_sampleData.isFavorite;
			UpdateBackgroundColor();
		}

		public void OnClickExpandButton()
		{
			_sampleData.isExpanded = !_sampleData.isExpanded;
			_sampleData.onExpand?.Invoke(_cellIndex);
		}
	}
}
