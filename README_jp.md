# これは何物？

UITableViewForUnityは、アプリ開発中によく会うリスト系のUI(2D)仕様を、楽に実現できるUnityエンジンのコンポーネントです。

例
チャット式 | ジャバラ式
--- | ---
![](sample_chat.gif) | ![](sample_expend.gif)

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

タイプ１のCellはリサイクル、タイプ２のCellのロードが重いからリサイクルしないという選択もできています。<br>
全部タイプ１のCellけど、0行目のCellはリサイクル、1行目のCellは見えなくなったら廃棄という設定もできます。<br>
実際違うCellのライフサイクルを別々で管理したいという仕様を経験したことがないですが、少数極端のケースで使いたい場合があるかもしれません。<br>

また、Cellのライフサイクルを管理しなくない場合、TableViewでオフにすることもできます。そうすると、全てのCellを一括ロードもできます。<br>

② 違うタイプのCellを一つのTableViewに共存できる<br>

例えば、チャットTableViewの場合、メッセージ(MessageCell)、スタンプ(StampCell)、位置共有(LocationCell)などレイアウト配置の違うタイプのCellでも一つのTableViewに入れられます。すると、一つGameObjectでいらない物をSetActiveオンしたりオフしたりする実装も避けられます。

③ 演出の実装もできる<br>

Cellの高さを自由(0不可)に調整できるので、ジャバラ式みたいな伸縮演出を実現できます。

④ Cellに対する増減、指定番号(Index)まで移動という操作もできる<br>

フレンドリストなど、データの取得はページ式で少しずつAPIで取っている場合、TableViewの先頭と後尾からCellを新規追加することもできます。


# どう使う?

[SampleScene.unity](https://github.com/zhaozilong1988/UITableViewForUnity/blob/master/Assets/Scenes/SampleScene.unity)と[SampleTableViewImplementation.cs](https://github.com/zhaozilong1988/UITableViewForUnity/blob/master/Assets/Scenes/Scripts/SampleTableViewImplementation.cs)の実装をご参考ください。

# 設計について

UITableViewForUnityは、基本iOSの[UIKit](https://developer.apple.com/documentation/uikit)フレームワークの[UITableView](https://developer.apple.com/documentation/uikit/uitableview)の設計を参考して実装していますが、いくつか違うところもあります。

・ | UITableView | UITableViewForUnity
--- | --- | ---
Headerという概念 | あり | ない
Cellのライフサイクル | 回収再利用式のみ | ①見えなくなったら回収<br>②リロード時に回収<br>③見えなくなったら廃棄<br>の3種類がある
ScrollViewとの関係 | UIScrollViewを継承 | ScrollRectとは別で独立
