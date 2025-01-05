[日本語版](https://github.com/zhaozilong1988/UITableViewForUnity/blob/master/README_jp.md) 👈 [中文版](https://github.com/zhaozilong1988/UITableViewForUnity/blob/master/README_cn.md) 👈

# What is this?

UITableViewForUnity is a componet which can be used for implementing various of list UI views with Unity engine.

For example
| Chat | Nested Scrolling | Appendable List
| --- | --- | --- |
| ![](sample_chat_list.gif) | ![](sample_netflix_like.gif) | ![](sample_sns.gif) |
| Snapping | Expandable List | Grid |
![](sample_snapping.gif) | ![](sample_expend.gif) | ![](sample_endless_grid.gif) |
| Changeable Grid | Draggable Grid | Deletable Grid |
![](sample_changeable_grid.gif) | ![](sample_grid_drag.gif) | ![](sample_grid_del.gif) |


# Why I need this?

UITableViewForUnity will help you to develop various of list views effectively without care for the number of items(gameobject), because them will be reused when disappeared from viewport of ScrollRect. Also, you can turn off the reuse feature for any cell which you do not want to reuse.

# Installation

## UMP
1. Open the Unity Package Manager.
2. Select "Add package from git URL".
3. Enter https://github.com/zhaozilong1988/UITableViewForUnity.git?path=Assets/UIKit.

## .unitypackge
Download the unitypackge from the [Releases](https://github.com/zhaozilong1988/UITableViewForUnity/releases), then import it to your project.

# How to use?

Check the samples in the [Assets/UIKit/Samples](https://github.com/zhaozilong1988/UITableViewForUnity/tree/master/Assets/UIKit/Samples) folder or in the Unity Package Manager’s Samples tab
![](samples_tab.png)

# The concept of design

The design of UITableViewForUnity referred to the [UITableView](https://developer.apple.com/documentation/uikit/uitableview) of [UIKit](https://developer.apple.com/documentation/uikit) framework on iOS.
