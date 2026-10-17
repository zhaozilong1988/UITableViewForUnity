using System;
using UIKit.Samples;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace UIKit.Tests
{
	public static class RegressionSceneBuilder
	{
		const string Folder = "Assets/UIKit/Samples/8_RegressionTests";
		const string CellPath = Folder + "/Prefabs/RegressionResultCell.prefab";
		static readonly Color Panel = new Color(.055f, .085f, .13f);
		static Font Font => Resources.GetBuiltinResource<Font>(
#if UNITY_2022_2_OR_NEWER
			"LegacyRuntime.ttf"
#else
			"Arial.ttf"
#endif
		);

		[MenuItem("UIKit/Tests/Create Regression Scene")]
		public static void CreateScene()
		{
			if (!AssetDatabase.IsValidFolder(Folder + "/Prefabs")) AssetDatabase.CreateFolder(Folder, "Prefabs");
			var previous = SceneManager.GetActiveScene();
			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
			var prefab = CreateCell();
			var root = new GameObject("UITableView Regression Checks");
			SceneManager.MoveGameObjectToScene(root, scene);
			var runner = root.AddComponent<UITableViewRegressionScene>();
			var canvasRect = Rect("Regression Dashboard", root.transform);
			var canvas = canvasRect.gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 20;
			var scaler = canvasRect.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
			scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1280, 900);
			scaler.matchWidthOrHeight = .5f;
			canvasRect.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
			var dashboard = canvasRect.gameObject.AddComponent<RegressionDashboard>();
			Surface(canvasRect, new Color(.025f, .04f, .07f));
			var heading = Text("Heading", canvasRect, "UITableView / Regression checks", 32);
			Top(heading.rectTransform, 24, 18, 0, 44, 24);
			var intro = Text("Scope", canvasRect, "9 scene checks. Automated test results are reported separately in Test Runner.", 20);
			Top(intro.rectTransform, 24, 68, 0, 32, 24);
			var summary = Text("Summary", canvasRect, "PASS 0    FAIL 0    PENDING 9", 26);
			Top(summary.rectTransform, 24, 108, 0, 60, 276);
			var runAll = Button("RunAll", canvasRect, "Run all 9 checks", new Color(.12f, .44f, .68f));
			Top(runAll.GetComponent<RectTransform>(), -256, 108, 232, 48);
			runAll.GetComponent<RectTransform>().anchorMin = runAll.GetComponent<RectTransform>().anchorMax = Vector2.one;
			var body = Rect("Body", canvasRect);
			Fill(body, 24, 176, 24, 24);
			var list = Rect("Results", body); list.anchorMin = Vector2.zero; list.anchorMax = new Vector2(.37f, 1); list.offsetMin = Vector2.zero; list.offsetMax = new Vector2(-10, 0); Surface(list, Panel);
			var listTitle = Text("ResultsHeading", list, "CHECKS  /  failures first", 22);
			Top(listTitle.rectTransform, 16, 14, 0, 30, 16);
			var listHint = Text("ResultsHint", list, "Select a row to inspect its result. Scroll for more.", 18);
			Top(listHint.rectTransform, 16, 50, 0, 48, 16);
			var tableRect = Rect("ResultsTable", list);
			Fill(tableRect, 12, 108, 12, 12);
			var table = Table(tableRect);
			var detail = Rect("Detail", body); detail.anchorMin = new Vector2(.37f, 0); detail.anchorMax = Vector2.one; detail.offsetMin = new Vector2(10, 0); detail.offsetMax = Vector2.zero; Surface(detail, Panel);
			var title = Text("CaseTitle", detail, "", 28); Top(title.rectTransform, 18, 18, 0, 40, 18);
			var description = Text("Description", detail, "", 21); Top(description.rectTransform, 18, 64, 0, 48, 18);
			var expectedPanel = Rect("ExpectedCard", detail);
			expectedPanel.anchorMin = new Vector2(0, 1); expectedPanel.anchorMax = new Vector2(.5f, 1);
			expectedPanel.pivot = Vector2.up; expectedPanel.offsetMin = new Vector2(18, -274); expectedPanel.offsetMax = new Vector2(-6, -122);
			Surface(expectedPanel, new Color(.085f, .14f, .21f));
			var expectedHeading = Text("ExpectedHeading", expectedPanel, "EXPECTED", 17); Top(expectedHeading.rectTransform, 12, 10, 0, 24, 12);
			var expected = Text("Expected", expectedPanel, "", 21); Fill(expected.rectTransform, 12, 42, 12, 10);
			var actualPanel = Rect("MeasuredCard", detail);
			actualPanel.anchorMin = new Vector2(.5f, 1); actualPanel.anchorMax = Vector2.one;
			actualPanel.pivot = Vector2.up; actualPanel.offsetMin = new Vector2(6, -274); actualPanel.offsetMax = new Vector2(-18, -122);
			Surface(actualPanel, new Color(.08f, .18f, .19f));
			var actualHeading = Text("ActualHeading", actualPanel, "MEASURED AT COMPLETION", 17); Top(actualHeading.rectTransform, 12, 10, 0, 24, 12);
			var actual = Text("Actual", actualPanel, "", 21); Fill(actual.rectTransform, 12, 42, 12, 10);
			var status = Text("Status", detail, "NOT RUN", 23); Top(status.rectTransform, 18, 286, 0, 30, 18);
			var rerun = Button("RunSelected", detail, "Run selected check", new Color(.13f, .3f, .48f)); Top(rerun.GetComponent<RectTransform>(), 18, 328, 254, 48);
			var errors = Button("ToggleFailure", detail, "Failure details", new Color(.4f, .15f, .2f)); Top(errors.GetComponent<RectTransform>(), 288, 328, 220, 48);
			var caption = Text("PreviewCaption", detail, "TABLE AT COMPLETION", 17); Top(caption.rectTransform, 18, 392, 0, 24, 186);
			var fit = Button("PreviewFit", detail, "Fit", new Color(.12f, .22f, .34f)); Top(fit.GetComponent<RectTransform>(), -182, 386, 70, 36); fit.GetComponent<RectTransform>().anchorMin = fit.GetComponent<RectTransform>().anchorMax = Vector2.one; fit.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 18;
			var zoom = Button("PreviewZoom", detail, "Zoom +", new Color(.12f, .22f, .34f)); Top(zoom.GetComponent<RectTransform>(), -104, 386, 86, 36); zoom.GetComponent<RectTransform>().anchorMin = zoom.GetComponent<RectTransform>().anchorMax = Vector2.one; zoom.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 18;
			var preview = Rect("Preview", detail); Fill(preview, 18, 430, 18, 18); Surface(preview, new Color(.025f, .045f, .075f)); preview.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
			var empty = Text("PreviewEmpty", preview, "Run this check to keep its table here.", 24); Fill(empty.rectTransform, 20, 20, 20, 20); empty.alignment = TextAnchor.MiddleCenter;
			var viewportFrame = Rect("TestedViewportFrame", preview); viewportFrame.anchorMin = viewportFrame.anchorMax = viewportFrame.pivot = new Vector2(.5f, .5f);
			FrameEdge(viewportFrame, "Top", Vector2.up, Vector2.one, new Vector2(0, 2), new Vector2(.5f, 1));
			FrameEdge(viewportFrame, "Bottom", Vector2.zero, Vector2.right, new Vector2(0, 2), new Vector2(.5f, 0));
			FrameEdge(viewportFrame, "Left", Vector2.zero, Vector2.up, new Vector2(2, 0), new Vector2(0, .5f));
			FrameEdge(viewportFrame, "Right", Vector2.right, Vector2.one, new Vector2(2, 0), new Vector2(1, .5f));
			var failurePanel = Rect("FailureDetails", detail); Fill(failurePanel, 18, 430, 18, 18); Surface(failurePanel, new Color(.12f, .05f, .08f)).raycastTarget = true;
			var failureViewport = Rect("Viewport", failurePanel); Fill(failureViewport, 12, 12, 12, 12); failureViewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
			var failure = Text("FailureText", failureViewport, "", 20); Top(failure.rectTransform, 0, 0, 0, 0, 0);
			failure.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
			var failureScroll = failurePanel.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
			failureScroll.viewport = failureViewport; failureScroll.content = failure.rectTransform; failureScroll.horizontal = false; failureScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
			failurePanel.gameObject.SetActive(false);
			var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); events.transform.SetParent(root.transform, false);
			Assign(runner, "_dashboard", dashboard);
			Assign(dashboard, "_body", body, "_listPanel", list, "_detailPanel", detail, "_preview", preview, "_viewportFrame", viewportFrame, "_table", table, "_cellPrefab", prefab,
				"_summary", summary, "_title", title, "_description", description, "_expected", expected, "_actual", actual, "_status", status,
				"_previewCaption", caption, "_emptyPreview", empty, "_failure", failure, "_runAll", runAll, "_rerun", rerun, "_errorToggle", errors, "_fit", fit, "_zoom", zoom, "_errorPanel", failurePanel.gameObject);
			if (new SerializedObject(dashboard).FindProperty("_cellPrefab").objectReferenceValue == null)
				throw new InvalidOperationException("The authored dashboard must retain its result-cell prefab reference.");
			EditorSceneManager.SaveScene(scene, UITableViewRegressionScene.ScenePath);
			if (!Application.isBatchMode) { EditorSceneManager.CloseScene(scene, true); if (previous.IsValid()) SceneManager.SetActiveScene(previous); }
			AssetDatabase.SaveAssets();
			Debug.Log("Authored regression dashboard and result-cell prefab.");
		}
		static RegressionResultCell CreateCell()
		{
			var rect = Rect("RegressionResultCell", null); rect.sizeDelta = new Vector2(440, 102);
			var image = Surface(rect, Panel); image.raycastTarget = true;
			var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
			var cell = rect.gameObject.AddComponent<RegressionResultCell>();
			var stripeRect = Rect("StateStripe", rect); stripeRect.anchorMin = Vector2.zero; stripeRect.anchorMax = Vector2.up; stripeRect.sizeDelta = new Vector2(4, -10); stripeRect.anchoredPosition = new Vector2(4, 0);
			var stripe = Surface(stripeRect, Color.gray);
			var title = Text("Title", rect, "01  Append a cell", 24); Top(title.rectTransform, 18, 10, 0, 48, 14);
			var status = Text("State", rect, "NOT RUN  >", 20); Top(status.rectTransform, 18, 62, 0, 28, 14);
			Assign(cell, "_title", title, "_status", status, "_background", image, "_stripe", stripe, "_select", button);
			PrefabUtility.SaveAsPrefabAsset(rect.gameObject, CellPath);
			UnityEngine.Object.DestroyImmediate(rect.gameObject);
			AssetDatabase.ImportAsset(CellPath, ImportAssetOptions.ForceSynchronousImport);
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CellPath).GetComponent<RegressionResultCell>();
			if (prefab == null) throw new InvalidOperationException("The result-cell prefab did not import correctly.");
			return prefab;
		}
		static void FrameEdge(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size, Vector2 pivot)
		{
			var edge = Rect(name, parent); edge.anchorMin = min; edge.anchorMax = max; edge.pivot = pivot;
			edge.sizeDelta = size; edge.anchoredPosition = Vector2.zero;
			Surface(edge, new Color(.2f, .85f, .95f));
		}
		static UITableView Table(RectTransform rect)
		{
			var viewport = Rect("Viewport", rect); Fill(viewport, 0, 0, 0, 0); viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
			var content = Rect("Content", viewport); Top(content, 0, 0, 0, 0, 0);
			var scroll = rect.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
			var background = Surface(rect, new Color(.04f, .065f, .1f)); background.raycastTarget = true;
			return rect.gameObject.AddComponent<UITableView>();
		}
		static RectTransform Rect(string name, Transform parent)
		{
			var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); if (parent != null) rect.SetParent(parent, false); return rect;
		}
		static UnityEngine.UI.Image Surface(RectTransform rect, Color color)
		{
			var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image;
		}
		static UnityEngine.UI.Text Text(string name, Transform parent, string text, int size)
		{
			var label = Rect(name, parent).gameObject.AddComponent<UnityEngine.UI.Text>(); label.font = Font; label.text = text; label.fontSize = size; label.color = new Color(.9f, .94f, 1); label.raycastTarget = false; label.alignment = TextAnchor.UpperLeft; return label;
		}
		static UnityEngine.UI.Button Button(string name, Transform parent, string text, Color color)
		{
			var rect = Rect(name, parent); var image = Surface(rect, color); image.raycastTarget = true;
			var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
			var label = Text("Label", rect, text, 22); Fill(label.rectTransform, 8, 6, 8, 6); label.alignment = TextAnchor.MiddleCenter; return button;
		}
		static void Fill(RectTransform rect, float left, float top, float right, float bottom)
		{
			rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
		}
		static void Top(RectTransform rect, float x, float y, float width, float height, float right = 0)
		{
			rect.anchorMin = Vector2.up; rect.anchorMax = width == 0 ? Vector2.one : Vector2.up; rect.pivot = Vector2.up;
			rect.sizeDelta = new Vector2(width == 0 ? -x - right : width, height); rect.anchoredPosition = new Vector2(x, -y);
		}
		static void Assign(UnityEngine.Object target, params object[] pairs)
		{
			var serialized = new SerializedObject(target);
			for (var i = 0; i < pairs.Length; i += 2) serialized.FindProperty((string)pairs[i]).objectReferenceValue = (UnityEngine.Object)pairs[i + 1];
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}
	}
}
