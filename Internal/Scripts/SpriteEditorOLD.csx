#load "..\..\WaddleTools\Scripts\WaddleSprite.csx"

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows;

public (float, float) EditorWorldToScreen(float x, float y) {
	float translatedX = x + editorCamX;
	float translatedY = y + editorCamY;
	
	float zoomedX = translatedX * editorZoom;
	float zoomedY = translatedY * editorZoom;
	
	return (zoomedX + (((float)SpriteCanvas.Width) / 2.0f), zoomedY + (((float)SpriteCanvas.Height) / 2.0f));
}

public (float, float) EditorScreenToWorld(float x, float y) 
{
	float adjustedX = x - (((float)SpriteCanvas.Width) / 2.0f);
	float adjustedY = y - (((float)SpriteCanvas.Height) / 2.0f);
	
	float unzoomedX = adjustedX / editorZoom;
	float unzoomedY = adjustedY / editorZoom;
	
	return (unzoomedX - editorCamX, unzoomedY - editorCamY);
}

public void UpdateSpriteView() {
	int roundedFrame = (int)Math.Floor(editorCurFrame);
	editorSpriteBrush.ImageSource = SpriteBitmaps[roundedFrame];
}

public void UpdateEditorView() {
	Rect editorViewport = checkerBGBrush.Viewport;
	(editorViewport.X, editorViewport.Y) = EditorWorldToScreen(0, 0);
	editorViewport.Width = (checkerBGGridSize * 2) * editorZoom;
	editorViewport.Height = (checkerBGGridSize * 2) * editorZoom;
	checkerBGBrush.Viewport = editorViewport;
	
	float x, y;
	(x, y) = EditorWorldToScreen(pointX, pointY);
	editorSpriteBrush.Viewport = new(x, y, 
		(float)editorSpriteBrush.ImageSource.Width * editorZoom, 
		(float)editorSpriteBrush.ImageSource.Height * editorZoom
	);
	
	(x, y) = EditorWorldToScreen(0, 0);
	OffsetPointerBrush.Viewport = new(x - 6, y - 6, 12, 12);
}

public void UpdateEditorOffsets(float x, float y) 
{
	(float rX, float rY) = EditorScreenToWorld(x, y);
	float dragSpdX = rX - dragXStart;
	float dragSpdY = rY - dragYStart;

	float finalX = pointXStart + dragSpdX;
	float finalY = pointYStart + dragSpdY;

	(pointX, pointY) = (
		(float)Math.Round(pointXStart + dragSpdX), 
		(float)Math.Round(pointYStart + dragSpdY)
	);
	
	if (editorIsWaddleSprite) {
		WaddleSprite spr = (editingSprite as WaddleSprite);
		spr.OriginX = (int)pointX;
		spr.OriginY = (int)pointY;
	}
	else {
		UndertaleSprite spr = (editingSprite as UndertaleSprite);
		spr.OriginXWrapper = (int)pointX;
		spr.OriginYWrapper = (int)pointY;
	}
}

public void SpriteCanvas_MouseHandler(object sender, MouseButtonEventArgs e) 
{
	Point pos = e.GetPosition(SpriteCanvas);	
	
	switch (e.MiddleButton) {
		case (MouseButtonState.Pressed): {
			mouseXStart = (float)pos.X;
			mouseYStart = (float)pos.Y;
			panStartX = editorCamX;
			panStartY = editorCamY;
			panning = true;
			break;
		}
		case (MouseButtonState.Released): {
			panning = false;
			break;
		}
	}
	
	switch (e.LeftButton) {
		case (MouseButtonState.Pressed): {
			offsetting = true;
			(dragXStart, dragYStart) = EditorScreenToWorld((float)pos.X, (float)pos.Y);
			(pointXStart, pointYStart) = (pointX, pointY);
			UpdateEditorOffsets((float)pos.X, (float)pos.Y);
			UpdateEditorView();
			break;
		}
		case (MouseButtonState.Released): {
			offsetting = false;
			break;
		}
	}
}

public void SpriteCanvas_MouseMove(object sender, MouseEventArgs e)
{
	Point pos = e.GetPosition(SpriteCanvas);
	
	if (panning) 
	{
		float diffX = ((float)pos.X) - mouseXStart;
		float diffY = ((float)pos.Y) - mouseYStart;
		editorCamX = panStartX + (diffX / editorZoom);
		editorCamY = panStartY + (diffY / editorZoom);
		UpdateEditorView();
	}
	if (offsetting)
	{
		UpdateEditorOffsets((float)pos.X, (float)pos.Y);
		UpdateEditorView();
	}
}

public void SpriteCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
{
	float zoomDiv = 240.0f;
	Point pos = e.GetPosition(SpriteCanvas);
	editorZoom += (float)e.Delta / zoomDiv;
	
	if (editorZoom < 0.25f)
		editorZoom = 0.25f;
	
	if (offsetting) UpdateEditorOffsets((float)pos.X, (float)pos.Y);
	UpdateEditorView();
}

public void SpriteCanvas_Startup() {
	if (SpriteBitmaps.Count > 0) SpriteBitmaps.Clear();
	
	float editorCamX = 0.0f;
	float editorCamY = 0.0f;
	float editorZoom = 1.0f;
	float panStartX = 0.0f;
	float panStartY = 0.0f;
	float mouseXStart = 0.0f;
	float mouseYStart = 0.0f;
	float editorCurFrame = 0.0f;
	
	WaddleSprite spr = (editingSprite as WaddleSprite);
	ReloadSpriteFrameImages(ref spr);
	foreach (WaddleSpriteFrame frame in spr.Frames) SpriteBitmaps.Add(MagickToBitmapImage(frame.Image));
	
	UnloadSpriteFrameImages(spr);
	UpdateSpriteView();
	UpdateEditorView();
	editingSprite = spr;
	editing = true;
}

Window WindowSpriteEditor = new() {
	Title = "Sprite Editor",
	ResizeMode = ResizeMode.NoResize,
	WindowStartupLocation = WindowStartupLocation.CenterScreen,
	Width = 900,
	Height = 900
};

Canvas EditorCanvas = new() { Width = 900, Height = 900 };
Canvas SpriteCanvas = new() { 
	Width = 760, Height = 640, 
	Margin = new(70, 210, 0, 0), ClipToBounds = true
};

bool panning = false;
bool offsetting = false;
bool editing = false;
bool editorIsWaddleSprite = false;

float editorCamX = 0.0f;
float editorCamY = 0.0f;
float editorZoom = 1.0f;
float panStartX = 0.0f;
float panStartY = 0.0f;
float mouseXStart = 0.0f;
float mouseYStart = 0.0f;

float dragXStart = 0.0f;
float dragYStart = 0.0f;
float pointXStart = 0.0f;
float pointYStart = 0.0f;
float pointX = 0.0f;
float pointY = 0.0f;

float editorCurFrame = 0.0f;

const int checkerBGGridSize = 8;

System.Windows.Shapes.Rectangle checkerBGRect = new() { Width = 760, Height = 640 };
DrawingBrush checkerBGBrush = new();

GeometryDrawing bgRect = new(Brushes.White, null, 
	new RectangleGeometry(new Rect(0, 0, checkerBGGridSize * 2, checkerBGGridSize * 2)));

GeometryGroup checkerGeoGroup = new();
checkerGeoGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, checkerBGGridSize, checkerBGGridSize)));
checkerGeoGroup.Children.Add(new RectangleGeometry(new Rect(checkerBGGridSize, checkerBGGridSize, checkerBGGridSize, checkerBGGridSize)));
GeometryDrawing checkerPattern = new (Brushes.DarkGray, null, checkerGeoGroup);

DrawingGroup checkerDrawingGroup = new();
checkerDrawingGroup.Children.Add(bgRect);
checkerDrawingGroup.Children.Add(checkerPattern);

checkerBGBrush.Drawing = checkerDrawingGroup;
checkerBGBrush.Viewport = new Rect(0, 0, checkerBGGridSize * 2, checkerBGGridSize * 2);
checkerBGBrush.ViewportUnits = BrushMappingMode.Absolute;
checkerBGBrush.TileMode = TileMode.Tile;

checkerBGRect.Fill = checkerBGBrush;
SpriteCanvas.Children.Add(checkerBGRect);

List<System.Windows.Media.Imaging.BitmapImage> SpriteBitmaps = new();
System.Windows.Shapes.Rectangle editorSpriteRect = new() { Width = 760, Height = 640 };
ImageBrush editorSpriteBrush = new();
object editingSprite = null; // Can be WaddleSprite or UndertaleSprite...?

editorSpriteBrush.ViewportUnits = BrushMappingMode.Absolute;
editorSpriteBrush.Viewport = new Rect(0, 0, 200, 200);
editorSpriteRect.Fill = editorSpriteBrush;
RenderOptions.SetBitmapScalingMode(editorSpriteRect, BitmapScalingMode.NearestNeighbor);
SpriteCanvas.Children.Add(editorSpriteRect);

System.Windows.Media.Imaging.BitmapImage OffsetPointerImage = new();
OffsetPointerImage.BeginInit();
OffsetPointerImage.UriSource = new(Path.Combine(WADDLETOOLS_EDITORGFX_DIR, "OffsetPointer.png"));
OffsetPointerImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
OffsetPointerImage.EndInit();

ImageBrush OffsetPointerBrush = new();
System.Windows.Shapes.Rectangle OffsetPointerRect = new() { Width = 760, Height = 640 };
OffsetPointerBrush.ViewportUnits = BrushMappingMode.Absolute;
OffsetPointerBrush.Viewport = new Rect(0, 0, 16, 16);
OffsetPointerBrush.ImageSource = OffsetPointerImage;
OffsetPointerRect.Fill = OffsetPointerBrush;
RenderOptions.SetBitmapScalingMode(OffsetPointerRect, BitmapScalingMode.NearestNeighbor);
SpriteCanvas.Children.Add(OffsetPointerRect);

EditorCanvas.Children.Add(SpriteCanvas);
WindowSpriteEditor.Content = EditorCanvas;

SpriteCanvas.MouseDown += SpriteCanvas_MouseHandler;
SpriteCanvas.MouseUp += SpriteCanvas_MouseHandler;
SpriteCanvas.MouseMove += SpriteCanvas_MouseMove;
SpriteCanvas.MouseWheel += SpriteCanvas_MouseWheel;

TaskCompletionSource<object> spriteEditorWindowTCS;
WindowSpriteEditor.Closing += (object s, System.ComponentModel.CancelEventArgs e) => {
	if (!editing)
		return;
	
	WindowSpriteEditor.Hide();
	spriteEditorWindowTCS.SetResult(null);
	editing = false;
	e.Cancel = true;
};