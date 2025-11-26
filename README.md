[日本語版](https://github.com/zhaozilong1988/UITableViewForUnity/blob/master/README_jp.md) 👈 [中文版](https://github.com/zhaozilong1988/UITableViewForUnity/blob/master/README_cn.md)

# UITableViewForUnity

Lightweight, flexible list UI component for Unity inspired by UIKit's UITableView. Efficiently renders long or complex lists by reusing item GameObjects and supports many common patterns (chat, nested scrolling, grid layouts, drag/drop, deletions, etc.).

Demo (examples):
| Chat | Nested Scrolling | Appendable List |
| --- | --- | --- |
| ![](sample_chat_list.gif) | ![](sample_netflix_like.gif) | ![](sample_sns.gif) |

| Snapping | Expandable List | Grid |
| --- | --- | --- |
| ![](sample_snapping.gif) | ![](sample_expend.gif) | ![](sample_endless_grid.gif) |

| Changeable Grid | Draggable Grid | Deletable Grid |
| --- | --- | --- |
| ![](sample_changeable_grid.gif) | ![](sample_grid_drag.gif) | ![](sample_grid_del.gif) |

## Key features
- Virtualized list: reuses item GameObjects to keep memory and CPU usage low.
- Supports vertical/horizontal lists and grids.
- Snap-to-item, expandable/collapsible sections, infinite/appendable lists.
- Drag-and-drop and item deletion support.
- Simple API inspired by UITableView for familiarity.

## Installation

### Unity Package Manager (recommended)
1. Open Unity Package Manager.
2. Select "Add package from git URL..."
3. Enter:
   https://github.com/zhaozilong1988/UITableViewForUnity.git?path=Assets/UIKit

### .unitypackage
Download the latest .unitypackage from Releases and import it into your project:
https://github.com/zhaozilong1988/UITableViewForUnity/releases

## Usage
- Open the samples: Assets/UIKit/Samples or use the Package Manager’s Samples tab.
- See sample scenes for common usage patterns and starter code.
- Typical workflow:
  1. Add a UITableView component to a GameObject with a ScrollRect.
  2. Implement a data source/provider to supply cell data and cell creation.
  3. Configure cell reuse and layout settings.

(See the sample scenes and comments in the code for concrete examples.)

![Samples tab](samples_tab.png)

## Design concept
The API and design are inspired by Apple’s UIKit UITableView. The goal is to provide a familiar, performant list component while staying idiomatic to Unity.

## Contributing
Contributions, bug reports, and suggestions are welcome. Please open issues for bugs or feature requests. If you'd like to contribute code, fork the repo and create a pull request with a clear description and tests or sample demonstrating the change.

## License
Specify license here (e.g. MIT). If no license is present in the repo, consider adding one to clarify reuse terms.

## Links
- Samples: Assets/UIKit/Samples
- Releases: https://github.com/zhaozilong1988/UITableViewForUnity/releases
- Japanese: README_jp.md
- Chinese: README_cn.md