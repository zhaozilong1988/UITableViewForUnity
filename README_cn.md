# 这是什么？

UITableViewForUnity是一个可以方便实现各种列表式UI需求的Unity引擎的组件。<br>

比如
| Chat | Nested Scrolling | Appendable List
| --- | --- | --- |
| ![](sample_chat_list.gif) | ![](sample_netflix_like.gif) | ![](sample_sns.gif) |
| Snapping | Expandable List | Grid |
![](sample_snapping.gif) | ![](sample_expend.gif) | ![](sample_endless_grid.gif) |
| Changeable Grid | Draggable Grid | Deletable Grid |
![](sample_changeable_grid.gif) | ![](sample_grid_drag.gif) | ![](sample_grid_del.gif) |

# 为什么需要?

在应用开发中经常会遇到各种样式的列表式UI需求。<br>

比如，游戏新闻列表，排名列表，消息列表，音乐游戏的话歌曲列表等等，如果用Unity引擎开发的话，一般会使用[UnityEngine.UI.ScrollRect](https://docs.unity3d.com/2018.3/Documentation/ScriptReference/UI.ScrollRect.html)。<br>

但是，ScrollRect只提供了滚动操作功能，列表中的元素（GameObject）的生命周期却没有被管理。<br>
尤其是在开发手游时，硬件的性能参差不齐，如果列表中元素的生命周期没有很好的被管理的话，性能上会出现问题。<br>

当然，如果是开发老手的话，可以信手拈来开发一个可以元素重用的列表组件。但是以个人经验来看，在开发第一线经常会被开发进度追着跑，没有足够的时间完善列表功能，为满足不同的列表功能而开发了一个以上的列表组件这种情况也不少见。<br>

为了在一定程度上解决上述问题，写了一个比较通用的列表逐渐来方便开发。<br>

主要的性能如下。<br>

① 列表（以下称为TableView）中的元素（以下称为Cell）的生命周期将会被管理<br>

Cell的生命周期类型有以下三种。<br>
- 进入可视范围时加载，消失在可视范围之外将被回收。<br>
- 进入可视范围时加载，消失在可视之外也不会被回收，手动重新加载时会被回收。<br>
- 进入可视范围时加载，消失在可视范围之外会被破弃。<br>

比如，类型1的Cell希望被回收，类型2的Cell却因为显示物件比较多，加载比较慢，不想被回收的话，可以分类型来选择Cell的生命周期。<br>
又比如，第一行Cell希望被回收，第二行的Cell不需要被回收的话，也可以通过Cell的行数来自由组合设定。<br>
实际上上诉两种情况本人并没有在实际开发中遇到过，但是有可能在一些少数极端复杂的需求中会被用到。<br>

还有，在不想管理Cell的生命周期的情况下，可以选择在UITableView中开启忽略生命周期，这样可以实现一齐加载。<br>

特徴は主に以下です。<br>

② 不同类型的Cell可以放在同一个TableView中<br>

例如，在聊天室列表，消息（MessageCell），表情（StampCell），位置共享（LocationCell）等不同UI配置的Cell也可以在同一个TableView中使用。这样的话可以避免把所有UI配置都放在同一个Cell中，然后通过SetActive去显示隐藏GameObject的这种操作。<br>

③ 支持实现动画<br>

Cell的高度可以被任意调整（除了0），所以像上边提到的可展开式列表的动画也可以被实现。<br>

④ Cell的增减，指定位置移动等操作也被支持。<br>

朋友列表等，需要通过API分页取得数据的情况，在TableView的表头表尾可以随意增加新加载的数据。<br>

# 安装

1. 打开 Unity Package Manager。
2. 选择 "Add package from git URL".
3. 输入以下 URL：https://github.com/zhaozilong1988/UITableViewForUnity.git?path=Assets/UIKit.

# 使用方法

请查看 [Assets/UIKit/Samples](https://github.com/zhaozilong1988/UITableViewForUnity/tree/master/Assets/UIKit/Samples) 文件夹中的示例，或者在 Unity Package Manager 的 “Samples” 选项卡中查看示例。
![](samples_tab.png)

# 关于设计

UITableViewForUnity基本上是参考了iOS的[UIKit](https://developer.apple.com/documentation/uikit)框架中的[UITableView](https://developer.apple.com/documentation/uikit/uitableview)的设计而实现的。但也有以下几种不同的部分。<br>

・ | UITableView | UITableViewForUnity
--- | --- | ---
Header | 支持 | 不支持
Cell的生命周期 | 只有可回收一种 | ①不可视时回收<br>②手动重新加载时回收<br>③不可视时破弃<br>共三种类型
和ScrollView的关系 | 继承于UIScrollView | 依赖于ScrollRect，但是是作为独立的组件存在
