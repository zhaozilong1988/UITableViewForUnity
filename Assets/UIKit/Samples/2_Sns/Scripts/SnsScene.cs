using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UIKit.Samples
{
	public class SnsScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate, IUITableViewMargin, IUITableViewReachable
	{
		[SerializeField] Text _textSizeCalculator;
		[SerializeField] UITableView _tableView;
		[SerializeField] SnsCell _cellPrefab;
		[SerializeField] Image _indicator;
		[SerializeField] Sprite[] _postedPhotos;
		[SerializeField] Sprite[] _userIcons;

		const float IndicatorRotationSpeed = 100f;
		const int FontSize = 30;
		const float MsgTextWidth = 520f;

		bool _indicatorVisible;
		readonly List<CellData> _chats = new List<CellData>();
		readonly IReadOnlyList<string> _replies = new List<string>() {
			"Dream big, start small, and take the first step today. #Motivation",
			"Every day is a new opportunity to grow and improve. #PositiveVibes",
			"Technology is reshaping the world. Let’s use it to create a better future. #Innovation",
			"Great ideas become great projects when you start building. #TechTalk",
			"Take a moment to appreciate how much you’ve learned and achieved this year. #Mindfulness",
			"Failure is not the opposite of success; it’s part of the process. #LifeLessons",
			"I told myself I’d only watch one episode. Now it’s 3 AM, and I’ve finished the season. #Relatable",
			"Why do weekends feel like 10 minutes, but Mondays feel like 10 years? #JustSaying",
			"What’s one book that changed your life? Share your favorites below. #BookLovers",
			"To everyone working hard today—you’re doing great. Keep going. #Support",
		};

		readonly IReadOnlyList<string> _usernames = new List<string>() {
			"Wonder Tracks", "Bright Horizon", "Quiet Nomad", "Daily Echoes", "Sky Wanderer", 
			"Alex In Motion", "The Real Jamie B", "Chris Journals", "Taylor Writes", "Mia Unfolded",
			"Coffee And Dreams", "Midnight Ramblings", "Cloud Chaser"
		};

		void Start()
		{
			// Tell the table view that this class will provide that the data it needs
			_tableView.dataSource = this;
			// Tell the table view that this class will respond to its delegate methods
			_tableView.@delegate = this;
			// Tell the table view that this class will provide margin between cells
			_tableView.margin = this;
			_tableView.reachable = this;

			for (int i = 0; i < 5; i++) {
				var msg = _replies[Random.Range(0, _replies.Count)];
				var size = CalculateTextSize(msg, FontSize);
				// _chats.Add(new CellData(msg, MsgTextWidth, size.y));
				_chats.Add(new CellData(
					_usernames[Random.Range(0, _usernames.Count)], msg, MsgTextWidth, size.y, 
					_userIcons[Random.Range(0, _userIcons.Length)], 
					_postedPhotos[Random.Range(0, _postedPhotos.Length)]));
			}

			_tableView.scrollRect.onValueChanged.AddListener(OnNormalizedPositionChanged);
			// Reload the table view to refresh UI
			_tableView.ReloadData();
		}

		void OnDestroy()
		{
			_tableView.scrollRect.onValueChanged.RemoveListener(OnNormalizedPositionChanged);
		}

		void OnNormalizedPositionChanged(Vector2 normalizedPos)
		{
			if (!_indicatorVisible && normalizedPos.y < 0f) {
				_indicatorVisible = true;
			} else if (_indicatorVisible && normalizedPos.y >= 0f) {
				_indicatorVisible = false;
			}

			_indicator.enabled = _indicatorVisible;
		}

		void Update()
		{
			if (_indicatorVisible) {
				_indicator.rectTransform.Rotate(0, 0, IndicatorRotationSpeed * Time.deltaTime);
			}
		}
		
		Vector2 CalculateTextSize(string text, int fontSize)
		{
			_textSizeCalculator.fontSize = fontSize;
			var size = _textSizeCalculator.rectTransform.sizeDelta;
			size.x = MsgTextWidth;
			_textSizeCalculator.rectTransform.sizeDelta = size;
			_textSizeCalculator.text = text;
			return new Vector2(Mathf.Min(size.x, _textSizeCalculator.preferredWidth), _textSizeCalculator.preferredHeight);
		}

		public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
		{
			return tableView.ReuseOrCreateCell(_cellPrefab);
		}

		public int NumberOfCellsInTableView(UITableView tableView)
		{
			return _chats.Count;
		}

		public float LengthOfCellAtIndexInTableView(UITableView tableView, int index)
		{
			return _chats[index].cellHeight;
		}

		public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
		{
			var cellData = _chats[index];
			_tableView.GetLoadedCell<SnsCell>(index).UpdateData(
				cellData.userIcon, cellData.postedPhoto, cellData.username, cellData.message, cellData.size, FontSize);
		}

		public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
		{
			_tableView.GetLoadedCell<SnsCell>(index).UnloadData();
		}

		public float LengthForUpperMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 0f;
		}

		public float LengthForLowerMarginInTableView(UITableView tableView, int rowIndex)
		{
			return 0f;
		}

		public void TableViewReachedTopmostOrRightmost(UITableView tableView)
		{
			
		}

		public void TableViewReachedBottommostOrLeftmost(UITableView tableView)
		{
		}

		public void TableViewLeftTopmostOrRightmost(UITableView tableView)
		{
			
		}

		public void TableViewLeftBottommostOrLeftmost(UITableView tableView)
		{
			// Add a new message when the table view reaches the bottom
			var msg = _replies[Random.Range(0, _replies.Count)];
			var size = CalculateTextSize(msg, FontSize);
			_chats.Add(new CellData(
				_usernames[Random.Range(0, _usernames.Count)], msg, MsgTextWidth, size.y, 
				_userIcons[Random.Range(0, _userIcons.Length)], 
				_postedPhotos[Random.Range(0, _postedPhotos.Length)]));
			_tableView.AppendData();
			_tableView.ScrollToCellAt(_chats.Count - 2, alignment: UITableViewAlignment.LeftOrBottom, withMargin:true);
		}

		public float TableViewReachableEdgeTolerance(UITableView tableView)
		{
			return -100f;
		}

		class CellData
		{
			public string username;
			public string message;
			public Sprite userIcon;
			public Sprite postedPhoto;
			public float cellHeight;
			public Vector2 size;

			public CellData(string username, string message, float textWidth, float textHeight, Sprite userIcon = null, Sprite postedPhoto = null)
			{
				this.username = username;
				this.message = message;
				this.userIcon = userIcon;
				this.postedPhoto = postedPhoto;
				cellHeight = textHeight + 565f;
				size = new Vector2(textWidth, cellHeight);
			}
		}
	}
}