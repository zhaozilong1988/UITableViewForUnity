using System;
using UnityEngine;
using UnityEngine.UI;

namespace UIKit.Samples
{
	public class AdvancedMonsterCell : UITableViewCell
	{
		[SerializeField] Text _indexText;
		[SerializeField] Image[] _deepColorImages;
		[SerializeField] Image[] _paleColorImages;
		[SerializeField] Color _deepGreen;
		[SerializeField] Color _paleGreen;
		[SerializeField] Color _deepRed;
		[SerializeField] Color _paleRed;
		[SerializeField] Image _frameImage;
		[SerializeField] Sprite[] _monsterSprites;
		[SerializeField] Image[] _rarityImages;

		Action<bool> _onAddToFavoriteClicked;
		AdvancedCellData _advancedCellData;
		int _cellIndex;

		public void UpdateData(int cellIndex, AdvancedCellData advancedCellData)
		{
			_advancedCellData = advancedCellData;
			_indexText.text = cellIndex.ToString();
			_cellIndex = cellIndex;

			UpdateBackgroundColor();

			_frameImage.sprite = _monsterSprites[advancedCellData.spriteIndex];
			_frameImage.SetNativeSize();
			var frameRect = _frameImage.rectTransform;
			var scale = 100f / frameRect.sizeDelta.x;
			frameRect.localScale = new Vector3(scale, scale);
			foreach (var rarityImage in _rarityImages) {
				rarityImage.enabled = false;
			}

			for (int i = 0; i < advancedCellData.rarity; i++) {
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
			if (_advancedCellData.isFavorite) {
				deepColor = _deepRed;
				paleColor = _paleRed;
			}
			else {
				deepColor = _deepGreen;
				paleColor = _paleGreen;
			}

			foreach (var img in _deepColorImages) {
				img.color = deepColor;
			}

			foreach (var img in _paleColorImages) {
				img.color = paleColor;
			}
		}

		public void OnClickAddToFavoriteButton()
		{
			_advancedCellData.isFavorite = !_advancedCellData.isFavorite;
			UpdateBackgroundColor();
		}

		public void OnClickExpandButton()
		{
			_advancedCellData.isExpanded = !_advancedCellData.isExpanded;
			_advancedCellData.onExpand?.Invoke(_cellIndex);
		}
	}
}
