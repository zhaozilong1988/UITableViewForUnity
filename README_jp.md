# これは何物？

UITableViewForUnityは、アプリ開発中によくあるリスト系のUI(2D)仕様を、楽に実現できるUnityエンジンのコンポーネントです。

例
| Chat | Nested Scrolling | Appendable List
| --- | --- | --- |
| ![](sample_chat_list.gif) | ![](sample_netflix_like.gif) | ![](sample_sns.gif) |
| Snapping | Expandable List | Grid |
![](sample_snapping.gif) | ![](sample_expend.gif) | ![](sample_endless_grid.gif) |
| Changeable Grid | Draggable Grid | Deletable Grid |
![](sample_changeable_grid.gif) | ![](sample_grid_drag.gif) | ![](sample_grid_del.gif) |

# なぜ必要?

アプリの開発でリスト式のUI表示がよく使われています。<br>

例えば、お知らせリスト、ランキングリスト、チャットリスト、音ゲーなら曲リストなど、Unityエンジンなら、[UnityEngine.UI.ScrollRect](https://docs.unity3d.com/2018.3/Documentation/ScriptReference/UI.ScrollRect.html)(以下ScrollRectで表記)を使うのが多いでしょう。<br>

ただ、SrollRectはネーミングの通り、スクロールできる以外、リストに入っている一つ一つアイテム(GameObject)のライフサイクルを管理してくれていません。<br>

特にスマホゲームの場合、端末のスペックもさまざまなので、ライフサイクルが管理されてないと、アイテムは画面外に飛んでいってもDrawCallが増え続くとか、リストが長いほどスクロールが遅くなるとかのパーフォマンス問題が出てきます。<br>

もちろん、ScrollRectを使いこなして、アイテムの再利用仕組みを容易に作ることができますが、個人経験だと、開発現場で、スケジュールに迫られてしまい、その仕様だけに適用な仕組みしか作れない場合も少なくなく、新しい仕様が出てくると、また仕組みを作り直さなければなりません。<br>

一つのプロジェクトでそれぞれの画面仕様の要求を満たすため、リストコンポーネントを複数に作られているのも経験したことがあります。<br>

こういった問題を可能な限り減らしたいと思うので、比較的に汎用なものを作りました。<br>

特徴は主に以下です。<br>

① リスト(TableViewと呼ぶ)でアイテム(Cellと呼ぶ)のライフサイクルを管理できる<br>

Cellのライフサイクルは下記３つのタイプと設計しています。<br>
- 見えるようになったらロードし、見えなくなるとリサイクル<br>
- 見えるようになったらロードし、見えなくなってもリサイクルせず、手動リロード時にリサイクル<br>
- 見えるようになったらロードし、見えなくなると廃棄<br>

例えば、タイプ１のCellはリサイクル、タイプ２のCellのロードが重いからリサイクルさせたいという選択もできます。<br>
または、0行目のCellはリサイクル、1行目のCellリサイクルさせないという設定もできます。<br>
実際違うCellのライフサイクルを別々で管理したいという仕様にあったことはないですが、少数極端のケースで使いたい場合があるかもしれません。<br>

また、Cellのライフサイクルを管理したくない場合、TableViewでオフにすることもできます。そうすると、全てのCellを一括ロードができます。<br>

② 違うタイプのCellを一つのTableViewに共存できる<br>

例えば、チャットTableViewの場合、メッセージ(MessageCell)、スタンプ(StampCell)、位置共有(LocationCell)などレイアウト配置の違うタイプのCellでも一つのTableViewに入れられます。すると、一つGameObjectでいらない物をSetActiveオンしたりオフしたりするという実装が避けられます。

③ 演出の実装ができる<br>

Cellの高さを自由(0不可)に調整できるので、ジャバラ式みたいな伸縮演出も簡単に実現できます。

④ Cellに対する増減、指定番号(Index)まで移動という操作もできる<br>

フレンドリストなど、データをページ式で少しずつAPIで取る形なら、TableViewの先頭と後尾からCellをアペンドすることもできます。

# インストール

## UPM
1. Unity Package Manager を開きます。
2. 「Add package from git URL」を選択します。
3. 次の URL を入力します：https://github.com/zhaozilong1988/UITableViewForUnity.git?path=Assets/UIKit.

## .unitypackage
[Releases](https://github.com/zhaozilong1988/UITableViewForUnity/releases)からunitypackageファイルをダウンロードして、プロジェクトにインポートします。

# クイックスタートガイド

## ステップ1：Cellクラスの作成

`UITableViewCell`を継承したクラスを作成します：

```csharp
using UnityEngine.UI;

namespace UIKit.Samples
{
    public class SimpleCell : UITableViewCell
    { 
        public Text text; 
        public Image icon; 
        public Image background;
    }
}
```

## ステップ2：DataSourceとDelegateの実装

`IUITableViewDataSource`と`IUITableViewDelegate`を実装するMonoBehaviourを作成します：

```csharp
using UnityEngine;

namespace UIKit.Samples
{
    public class SimpleTableScene : MonoBehaviour, IUITableViewDataSource, IUITableViewDelegate
    {
        [SerializeField] UITableView _tableView;
        [SerializeField] SimpleCell _cellPrefab;

        void Start()
        {
            // DataSourceとDelegateを設定
            _tableView.dataSource = this;
            _tableView.@delegate = this;

            // TableViewをリロードしてUIを更新
            _tableView.ReloadData();
        }

        #region IUITableViewDataSource
        // 指定されたインデックスのCellを返す
        public UITableViewCell CellAtIndexInTableView(UITableView tableView, int index)
        {
            return _tableView.ReuseOrCreateCell(_cellPrefab);
        }

        // Cellの総数を返す
        public int NumberOfCellsInTableView(UITableView tableView)
        {
            return 200;
        }

        // 各Cellの高さ（横向きの場合は幅）を返す
        public float LengthForCellInTableView(UITableView tableView, int index)
        {
            return index % 2 == 0 ? 150 : 200; // 可変高さ
        }
        #endregion

        #region IUITableViewDelegate
        // Cellが表示される直前に呼ばれる
        public void CellAtIndexInTableViewWillAppear(UITableView tableView, int index)
        {
            var cell = tableView.GetLoadedCell<SimpleCell>(index);
            cell.text.text = $"Cell Index: {index}";
        }

        // Cellが非表示になった時に呼ばれる
        public void CellAtIndexInTableViewDidDisappear(UITableView tableView, int index)
        {
            var cell = tableView.GetLoadedCell<SimpleCell>(index);
            cell.text.text = string.Empty;
        }
        #endregion
    }
}
```

## ステップ3：シーンの設定

1. ScrollRectを含むCanvasを作成
2. ScrollRectにUITableViewコンポーネントを追加
3. SimpleCellスクリプトを持つCellプレハブを作成
4. プレハブとUITableViewをシーンスクリプトに割り当て

# サンプル一覧

[Assets/UIKit/Samples](https://github.com/zhaozilong1988/UITableViewForUnity/tree/master/Assets/UIKit/Samples)フォルダー内のサンプル、または Unity Package Manager の「Samples」タブをご確認ください。

![](samples_tab.png)

| サンプル | 説明 |
| --- | --- |
| 0_SimpleTable | 可変高さのセルを持つ基本的なテーブルビュー |
| 1_Chat | 複数のセルタイプを持つチャット形式のリスト |
| 2_Sns | 追加/先頭追加をサポートするSNS形式のフィード |
| 3_SimpleGrid | 基本的なグリッドレイアウト |
| 4_NetflixLike | ネストされたスクロールリスト |
| 5_AdvancedGrid | ドラッグ＆削除機能付きの高度なグリッド |
| 6_AdvancedTable | 展開/折りたたみ可能なセル |
| 7_SnappingTable | セルにスナップするスクロール |

# 設計について

UITableViewForUnityは、基本iOSの[UIKit](https://developer.apple.com/documentation/uikit)フレームワークの[UITableView](https://developer.apple.com/documentation/uikit/uitableview)の設計を参考して実装していますが、いくつか違うところもあります。

・ | UITableView | UITableViewForUnity
--- | --- | ---
Headerという概念 | あり | ない
Cellのライフサイクル | 回収再利用式のみ | ①見えなくなったら回収<br>②リロード時に回収<br>③見えなくなったら廃棄<br>の3種類がある
ScrollViewとの関係 | UIScrollViewを継承 | ScrollRectとは別で独立
