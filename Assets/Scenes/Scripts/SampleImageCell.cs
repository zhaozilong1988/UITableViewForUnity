using System;
using UITableViewForUnity;
using UnityEngine;
using UnityEngine.UI;

public class SampleImageCell : UITableViewCell
{
	[SerializeField]
	private Text _indexText;
	[SerializeField]
	private Image[] _deepColorImages;
	[SerializeField]
	private Image[] _paleColorImages;
	[SerializeField]
	private Color _deepGreen;
	[SerializeField]
	private Color _paleGreen;
	[SerializeField]
	private Color _deepOrange;
	[SerializeField]
	private Color _paleOrange;
	[SerializeField]
	private Color _deepRed;
	[SerializeField]
	private Color _paleRed;
	[SerializeField]
	private Image _frameImage;
	[SerializeField]
	private Sprite[] _monsterSprites;
	[SerializeField]
	private Image[] _rarityImages;

	private Action<bool> _onAddToFavoriteClicked;
	private SampleData _sampleData;
	private int _selectedTabIndex;
	private int _cellIndex;

	public void UpdateData(int cellIndex, int selectedTabIndex, SampleData sampleData)
	{
		_selectedTabIndex = selectedTabIndex;
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

	private void UpdateBackgroundColor()
	{
		Color deepColor, paleColor;
		if (_sampleData.isFavorite)
		{
			deepColor = _deepRed;
			paleColor = _paleRed;
		}
		else if (_selectedTabIndex == 0)
		{
			deepColor = _deepGreen;
			paleColor = _paleGreen;
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

	public void OnClickExpendButton()
	{
		_sampleData.isExpended = !_sampleData.isExpended;
		_sampleData.onExpend?.Invoke(_cellIndex);
	}
}
