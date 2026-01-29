#load ".\Utils.csx"
#load ".\Constants.csx"
#load ".\WaddleSprite.csx"

// messy organization
// i should stop trying to lock in crazy coding at the middle of the midnight
// i do stupid shit

using UndertaleModLib.Util;
using UndertaleModTool;

using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

#region Classes

public class SpriteEditorFrame 
{
	public ushort TargetX = 0;
	public ushort TargetY = 0;
	public ushort TargetWidth = 0;
	public ushort TargetHeight = 0; 
	
	public ushort BoundWidth = 0;
	public ushort BoundHeight = 0;
	
	public System.Windows.Media.Imaging.BitmapImage Bitmap;
}

// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
// DATA BINDING IS HELL
public class SpriteEditorSprite: INotifyPropertyChanged
{
	
	public event PropertyChangedEventHandler PropertyChanged;
	public DispatcherTimer AnimationTimer;
	
	public Image ImageElement;
	public Border SpriteAreaBorder;
	public Border SpriteTargetBorder;
	
	public string TextureGroup = null;
	
	private List<SpriteEditorFrame> _Frames = null;
	public List<SpriteEditorFrame> Frames
	{
		get => _Frames;
		set 
		{
			if (_Frames != value)
			{
				_Frames = value;
				CurrentSpriteFrame = 0.0f;
				if (value?.Count > 0) _CurrentFrame = value[0];
				else _CurrentFrame = null;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Frames)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFrame)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentBitmap)));
			}
		}
	}
	
	public uint SpriteWidth = 0;
	public uint SpriteHeight = 0;
	
	private uint _SpecialVersion = 1;
	public uint SpecialVersion {
		get => _SpecialVersion;
		set {
			if (_SpecialVersion != value)
			{
				_SpecialVersion = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpecialVersion)));
			}
		}
	}
	
	private float _GameFPS = 30.0f;
	public float GameFPS
	{
		get => _GameFPS;
		set {
			if (_GameFPS != value)
			{
				_GameFPS = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameFPS)));
				UpdateAnimations();
			}
		}
	}
	
	private float _AnimationSpeed = 15.0f;
	public float AnimationSpeed {
		get => _AnimationSpeed;
		set { 
			if (value != _AnimationSpeed)
			{
				_AnimationSpeed = Math.Clamp(value, 0.0f, 9999.0f); // capped cuz editor breaks idk
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AnimationSpeed)));
				UpdateAnimations();
			}
		}
	}
	
	private float _CurrentSpriteFrame = 0.0f;
	public float CurrentSpriteFrame {
		get => _CurrentSpriteFrame;
		set { 
			if (_CurrentSpriteFrame != value)
			{
				_CurrentSpriteFrame = (value % (Frames.Count));
				while (_CurrentSpriteFrame < 0.0f)
					_CurrentSpriteFrame += (float)Frames.Count;
				
				if (Frames.Count > 0)
					CurrentFrame = Frames[(int)Math.Floor(_CurrentSpriteFrame)];
				else
					CurrentFrame = null;
				
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSpriteFrame)));
			}
		}
	}
	
	private float _SpriteOffsetX = 0.0f;
	public float SpriteOffsetX {
		get => _SpriteOffsetX;
		set {
			_SpriteOffsetX = Math.Clamp(MathF.Round(value), 0.0f, SpriteWidth);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpriteOffsetX)));
		}
	}
	
	
	private float _SpriteOffsetY = 0.0f;
	public float SpriteOffsetY {
		get => _SpriteOffsetY;
		set {
			_SpriteOffsetY = Math.Clamp(MathF.Round(value), 0.0f, SpriteHeight);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpriteOffsetY)));
		}
	}
	
	private bool _IsSpecial = false;
	public bool IsSpecial 
	{
		get => _IsSpecial;
		set {
			if (_IsSpecial != value)
			{
				_IsSpecial = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSpecial)));
				UpdateAnimations();
			}
		}
	}
	
	private bool _IsGMS2 = false;
	public bool IsGMS2 
	{
		get => _IsGMS2;
		set {
			if (value != _IsGMS2)
			{
				_IsGMS2 = value;
				UpdateAnimations();
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGMS2)));
			}
		}
	}
	
	private string _Name = null;
	public string Name
	{
		get => _Name;
		set
		{
			if (value != _Name)
			{
				_Name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
			}
		}
	}
	
	private AnimSpeedType _GMS2PlaybackSpeedType = 0;
	public AnimSpeedType GMS2PlaybackSpeedType 
	{
		get => _GMS2PlaybackSpeedType;
		set
		{
			if (value != _GMS2PlaybackSpeedType)
			{
				_GMS2PlaybackSpeedType = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GMS2PlaybackSpeedType)));
				UpdateAnimations();
			}
		}
	}
	
	private SpriteEditorFrame _CurrentFrame = null;
	public SpriteEditorFrame CurrentFrame 
	{
		get => _CurrentFrame;
		set {
			if (_CurrentFrame != value)
			{
				_CurrentFrame = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFrame)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentBitmap)));
			}
		}
	}
	
	public System.Windows.Media.Imaging.BitmapImage CurrentBitmap
	{
		get => CurrentFrame?.Bitmap;
	}
	
	public float SpriteFramesCount {
		get => Frames.Count;
	}
	
	public void AnimationTimer_Tick(object sender, EventArgs e)
	{
		float Speed = 1.0f;
		
		if (GMS2PlaybackSpeedType == AnimSpeedType.FramesPerGameFrame && sender == AnimationTimer)
			Speed = AnimationSpeed;
		
		CurrentSpriteFrame += Speed;
	}
	
	public void UpdateAnimations()
	{
		bool Restart = AnimationTimer.IsEnabled;
		if (Restart)
			AnimationTimer.Stop();
		
		float FPS = (GMS2PlaybackSpeedType == AnimSpeedType.FramesPerSecond) ? AnimationSpeed : GameFPS;
		
		if (!IsSpecial || !IsGMS2) // sprite's fps playback is apparently at 30fps if not gamemaker 2 says utmt idk
		{
			GMS2PlaybackSpeedType = AnimSpeedType.FramesPerSecond;
			AnimationSpeed = 1.0f;
			SpecialVersion = 1;
			FPS = 30.0f;
			
			if (!IsGMS2 && IsSpecial)
				IsSpecial = false;
		}
		
		AnimationTimer.Interval = TimeSpan.FromMilliseconds(1000.0f / FPS);
		
		if (Restart)
			AnimationTimer.Start();
	}
	
	public void UpdateView(SpriteEditorContext Context)
	{
		float w = (float)SpriteWidth * Context.CameraZoom;
		float h = (float)SpriteHeight * Context.CameraZoom;
		(float x, float y) = Context.WorldToScreen(-SpriteOffsetX, -SpriteOffsetY);
		SpriteAreaBorder.Margin = new(x, y, 0, 0);
		(SpriteAreaBorder.Width, SpriteAreaBorder.Height) = (w, h);
		
		if (CurrentFrame != null)
		{
			w = (float)CurrentFrame.TargetWidth * Context.CameraZoom;
			h = (float)CurrentFrame.TargetHeight * Context.CameraZoom;
			(x, y) = Context.WorldToScreen(-SpriteOffsetX + CurrentFrame.TargetX, -SpriteOffsetY + CurrentFrame.TargetY);
			ImageElement.Margin = new(x, y, 0, 0);
			SpriteTargetBorder.Margin = new(x, y, 0, 0);
			(ImageElement.Width, ImageElement.Height) = (w, h);
			(SpriteTargetBorder.Width, SpriteTargetBorder.Height) = (w, h);
		}
	}
	
	public void ApplyToSprite(object spr)
	{
		if (spr is UndertaleSprite)
		{
			UndertaleSprite sprite = (spr as UndertaleSprite);
			sprite.IsSpecialType = IsSpecial;
			sprite.SVersion = SpecialVersion;
			sprite.GMS2PlaybackSpeedType = GMS2PlaybackSpeedType;
			sprite.GMS2PlaybackSpeed = AnimationSpeed;
			sprite.OriginXWrapper = (int)SpriteOffsetX;
			sprite.OriginYWrapper = (int)SpriteOffsetY;
		}
		else if (spr is WaddleSprite)
		{
			WaddleSprite sprite = (spr as WaddleSprite);
			sprite.Special = IsSpecial;
			sprite.SpecialVersion = SpecialVersion;
			sprite.GMS2PlaybackSpeedType = GMS2PlaybackSpeedType;
			sprite.AnimationSpeed = AnimationSpeed;
			sprite.OriginX = (int)SpriteOffsetX;
			sprite.OriginY = (int)SpriteOffsetY;
			sprite.Name = Name;
			sprite.TextureGroup = TextureGroup;
		}
	}
}

public class SpriteEditorContext: INotifyPropertyChanged {
	public event PropertyChangedEventHandler PropertyChanged;
	public DispatcherTimer AnimationTimer;
	
	// ok thank god no more rectangle and brushes or that shit i was just that stupid
	public Window Window;
	public Window CancelledMessageOwner = null;
	public Canvas Canvas;
	public System.Windows.Shapes.Rectangle OffsetPointerRect;
	public Popup OffsetPresetsPopup;
	public Popup ReferenceSpritePopup;
	public ListBox OffsetPresetList;
	public TextBox ReferenceSpriteNameBox;
	public ComboBoxDark TextureGroupCombo;
	public DrawingBrush BackgroundBrush;
	
	public ButtonDark OffsetPresetsButton;
	public ButtonDark ReferenceSpriteButton;
	public ButtonDark SubmitReferenceNameButton;
	public ButtonDark ClearReferenceButton;
	public ButtonDark PlayAnimButton;
	public ButtonDark StopAnimButton;
	public ButtonDark PrevFrameButton;
	public ButtonDark NextFrameButton;
	public ButtonDark ReferencePlayAnimButton;
	public ButtonDark ReferenceStopAnimButton;
	public ButtonDark ReferencePrevFrameButton;
	public ButtonDark ReferenceNextFrameButton;
	public ButtonDark ConfirmEditButton;
	
	public TaskCompletionSource<object> WindowTask;
	
	private SpriteEditorSprite _Sprite { get; set; } = null;
	public SpriteEditorSprite Sprite { 
		get => _Sprite; 
		set
		{
			if (_Sprite != value)
			{
				_Sprite = value;
				_Sprite.GameFPS = _GameFPS;
				_Sprite.IsGMS2 = _IsGMS2;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Sprite)));
			}
		}
	}
	
	private SpriteEditorSprite _ReferenceSprite = null;
	public SpriteEditorSprite ReferenceSprite { 
		get => _ReferenceSprite; 
		set
		{
			if (_ReferenceSprite != value)
			{
				_ReferenceSprite = value;
				_ReferenceSprite.GameFPS = _GameFPS;
				_ReferenceSprite.IsGMS2 = _IsGMS2;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReferenceSprite)));
			}
		}
	}
	
	private string _Title = "WaddleTools' Sprite Editor";
	public string Title { get => _Title; } 
	
	private bool _ReferenceEnabled = false;
	public bool ReferenceEnabled
	{
		get => _ReferenceEnabled;
		set {
			if (_ReferenceEnabled != value)
			{
				_ReferenceEnabled = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReferenceEnabled)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReferenceVisiblility)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReferenceBorderVisiblility)));
			}
		}
	}
	
	private bool _IsGMS2 = false;
	public bool IsGMS2 
	{
		get => _IsGMS2;
		set {
			if (_IsGMS2 != value)
			{
				_IsGMS2 = value;
				if (Sprite != null) Sprite.IsGMS2 = value;
				if (ReferenceSprite != null) ReferenceSprite.IsGMS2 = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGMS2)));
			}
		}
	}
	
	
	private bool _VisibleBorders = true;
	public bool VisibleBorders 
	{
		get => _VisibleBorders;
		set {
			if (_VisibleBorders != value)
			{
				_VisibleBorders = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VisibleBorders)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReferenceBorderVisiblility)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BordersVisibility)));
			}
		}
	}
	
	public bool _WaddleSpriteMode = false;
	public bool WaddleSpriteMode
	{
		get => _WaddleSpriteMode;
		set {
			if (_WaddleSpriteMode != value)
			{
				_WaddleSpriteMode = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WaddleSpriteMode)));
			}
		}
	}
	
	public bool InputPanning = false;
	public bool InputOffsetting = false;
	public bool ConfirmButtonPressed = false;
	
	private float _GameFPS = 30.0f;
	public float GameFPS
	{
		get => _GameFPS;
		set {
			if (_GameFPS != value)
			{
				_GameFPS = value;
				if (Sprite != null) Sprite.GameFPS = value;
				if (ReferenceSprite != null) ReferenceSprite.GameFPS = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameFPS)));
			}
		}
	}
	
	public float CameraZoom = 1.0f;
	
	private float _CameraX = 0.0f;
	public float CameraX { 
		get => _CameraX; 
		set {
			_CameraX = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraX)));
		}
	}
	
	private float _CameraY = 0.0f;
	public float CameraY { 
		get => _CameraY; 
		set {
			_CameraY = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraY)));
		}
	}
	
	private int _TextureGroupIndex = 0;
	public int TextureGroupIndex {
		get => _TextureGroupIndex;
		set {
			if (_TextureGroupIndex != value) {
				_TextureGroupIndex = value;
				if (value == 0)
					Sprite.TextureGroup = null;
				else
					Sprite.TextureGroup = (string)TextureGroupCombo.Items[value];
				
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextureGroupIndex)));
			}
		}
	}
	
	public Visibility ReferenceVisiblility
	{ get { return (ReferenceEnabled) ? Visibility.Visible : Visibility.Hidden; }}
	
	public Visibility ReferenceBorderVisiblility
	{ get { return (ReferenceEnabled && VisibleBorders) ? Visibility.Visible : Visibility.Hidden; }}
	
	public Visibility BordersVisibility
	{ get { return (VisibleBorders) ? Visibility.Visible : Visibility.Hidden; }}
	
	public float InputPanStartX = 0.0f;
	public float InputPanStartY = 0.0f;
	public float InputMouseStartX = 0.0f;
	public float InputMouseStartY = 0.0f;
	public float InputDragStartX = 0.0f;
	public float InputDragStartY = 0.0f;
	public float InputOffsetXStart = 0.0f;
	public float InputOffsetYStart = 0.0f;
	
	public (float, float) WorldToScreen(float x, float y) {
		float translatedX = x + CameraX;
		float translatedY = y + CameraY;
		
		float zoomedX = translatedX * CameraZoom;
		float zoomedY = translatedY * CameraZoom;
		
		return (zoomedX + (((float)Canvas.ActualWidth) / 2.0f), zoomedY + (((float)Canvas.ActualHeight) / 2.0f));
	}
	
	public (float, float) ScreenToWorld(float x, float y) 
	{
		float adjustedX = x - (((float)Canvas.ActualWidth) / 2.0f);
		float adjustedY = y - (((float)Canvas.ActualHeight) / 2.0f);
		
		float unzoomedX = adjustedX / CameraZoom;
		float unzoomedY = adjustedY / CameraZoom;
		
		return (unzoomedX - CameraX, unzoomedY - CameraY);
	}
	
	public void UpdateView() {
		(float x, float y) = WorldToScreen(0, 0);
		OffsetPointerRect.Margin = new(x - 6, y - 6, 0, 0);
		
		Rect editorViewport = BackgroundBrush.Viewport;
		(editorViewport.X, editorViewport.Y) = (x, y);
		editorViewport.Width = (checkerBGGridSize * 2) * CameraZoom;
		editorViewport.Height = (checkerBGGridSize * 2) * CameraZoom;
		BackgroundBrush.Viewport = editorViewport;
		
		Sprite.UpdateView(this);
		ReferenceSprite.UpdateView(this);
	}
	
	public void UpdateOffsets(float x, float y) 
	{
		(float rX, float rY) = ScreenToWorld(x, y);
		float dragSpdX = InputDragStartX - rX;
		float dragSpdY = InputDragStartY - rY;
		
		(Sprite.SpriteOffsetX, Sprite.SpriteOffsetY) = (InputOffsetXStart + dragSpdX, InputOffsetYStart + dragSpdY);
	}
	
	public void Canvas_MouseHandler(object sender, MouseButtonEventArgs e) 
	{
		Point pos = e.GetPosition(Canvas);	
		
		switch (e.MiddleButton) {
			case (MouseButtonState.Pressed): {
				InputMouseStartX = (float)pos.X;
				InputMouseStartY = (float)pos.Y;
				InputPanStartX = CameraX;
				InputPanStartY = CameraY;
				InputPanning = true;
				break;
			}
			case (MouseButtonState.Released): {
				InputPanning = false;
				break;
			}
		}
		
		switch (e.LeftButton) {
			case (MouseButtonState.Pressed): {
				InputOffsetting = true;
				(InputDragStartX, InputDragStartY) = ScreenToWorld((float)pos.X, (float)pos.Y);
				(InputOffsetXStart, InputOffsetYStart) = (Sprite.SpriteOffsetX, Sprite.SpriteOffsetY);
				UpdateOffsets((float)pos.X, (float)pos.Y);
				break;
			}
			case (MouseButtonState.Released): {
				InputOffsetting = false;
				break;
			}
		}
	}
	
	public void Canvas_MouseMove(object sender, MouseEventArgs e)
	{
		Point pos = e.GetPosition(Canvas);
		
		if (InputPanning) 
		{
			float diffX = ((float)pos.X) - InputMouseStartX;
			float diffY = ((float)pos.Y) - InputMouseStartY;
			CameraX = InputPanStartX + (diffX / CameraZoom);
			CameraY = InputPanStartY + (diffY / CameraZoom);
			UpdateView();
		}
		if (InputOffsetting)
		{
			UpdateOffsets((float)pos.X, (float)pos.Y);
			UpdateView();
		}
	}
	
	public void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		float zoomDiv = 240.0f;
		Point pos = e.GetPosition(Canvas);
		CameraZoom += (float)e.Delta / zoomDiv;
		
		if (CameraZoom < 0.25f)
			CameraZoom = 0.25f;
		
		if (InputOffsetting) UpdateOffsets((float)pos.X, (float)pos.Y);
		UpdateView();
	}
	
	public void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateView();
	}
	
	public void OffsetPresetsList_Open(object sender, EventArgs e) 
	{
		OffsetPresetList.SelectedItem = null;
		OffsetPresetsPopup.IsOpen = true;
	}
	
	public void ReferenceSpritePopup_Open(object sender, EventArgs e) 
	{
		ReferenceSpritePopup.IsOpen = true;
		ReferenceSpriteNameBox.Text = "";
	}
	
	public void OffsetPresetsList_Select(object sender, RoutedEventArgs e) 
	{
		if (!OffsetPresetsPopup.IsOpen || OffsetPresetList.SelectedItem == null)
			return;
		
		OffsetPresetsPopup.IsOpen = false;
		
		switch ((OffsetPresetList.SelectedItem as ListBoxItem).Content) 
		{
			case ("Top Left"): 
			{
				Sprite.SpriteOffsetX = 0.0f;
				Sprite.SpriteOffsetY = 0.0f;
				break;
			}
			case ("Top Center"): 
			{
				Sprite.SpriteOffsetX = (float)(Sprite.SpriteWidth / 2);
				Sprite.SpriteOffsetY = 0.0f;
				break;
			}
			case ("Top Right"):
			{
				Sprite.SpriteOffsetX = (float)Sprite.SpriteWidth;
				Sprite.SpriteOffsetY = 0.0f;
				break;
			}
			case ("Middle Left"): 
			{
				Sprite.SpriteOffsetX = 0.0f;
				Sprite.SpriteOffsetY = (float)(Sprite.SpriteHeight / 2);
				break;
			}
			case ("Middle Center"): 
			{
				Sprite.SpriteOffsetX = (float)(Sprite.SpriteWidth / 2);
				Sprite.SpriteOffsetY = (float)(Sprite.SpriteHeight / 2);
				break;
			}
			case ("Middle Right"):
			{
				Sprite.SpriteOffsetX = (float)Sprite.SpriteWidth;
				Sprite.SpriteOffsetY = (float)(Sprite.SpriteHeight / 2);
				break;
			}
			case ("Bottom Left"): 
			{
				Sprite.SpriteOffsetX = 0.0f;
				Sprite.SpriteOffsetY = (float)Sprite.SpriteHeight;
				break;
			}
			case ("Bottom Center"): 
			{
				Sprite.SpriteOffsetX = (float)(Sprite.SpriteWidth / 2);
				Sprite.SpriteOffsetY = (float)Sprite.SpriteHeight;
				break;
			}
			case ("Bottom Right"):
			{
				Sprite.SpriteOffsetX = (float)Sprite.SpriteWidth;
				Sprite.SpriteOffsetY = (float)Sprite.SpriteHeight;
				break;
			}
		}
	}
	
	public void ClearReferenceButton_Click(object sender, RoutedEventArgs e) 
	{
		if (ReferenceSprite.AnimationTimer.IsEnabled)
			ReferenceSprite.AnimationTimer.Stop();
		
		ReferenceSprite.Frames.Clear();
		ReferenceSprite.CurrentSpriteFrame = 0.0f;
		ReferenceSprite.Name = null;
		ReferenceEnabled = false;
	}
	
	public void ConfirmButton_Click(object sender, RoutedEventArgs e) 
	{
		ConfirmButtonPressed = true;
		Window.Close();
	}
};

#endregion
#region Variables

const float checkerBGGridSize = 8.0f;

#endregion
#region Functions

public void EnableReferenceSprite(SpriteEditorContext Context, string SpriteName)
{
	if (Context.ReferenceSprite.AnimationTimer.IsEnabled)
		Context.ReferenceSprite.AnimationTimer.Stop();
	
	UndertaleSprite reference = null;
	foreach (UndertaleSprite sprite in Data.Sprites) {
		if (sprite.Name.Content != SpriteName) continue;
		
		reference = sprite;
		break;
	}
	
	if (reference == null) {
		CustomScriptMessage($"Unable to search for a sprite named \"{SpriteName}\"!", 
			"WaddleTools' SpriteEditor", Context.Window);
		return;
	}
	
	Context.ReferenceSpritePopup.IsOpen = false;
	Context.ReferenceEnabled = true;
	Context.ReferenceSprite.Frames = new();
	
	TextureWorker worker = new();
	foreach (UndertaleSprite.TextureEntry entry in reference.Textures) 
	{
		Context.ReferenceSprite.Frames.Add(new() {
			TargetX = entry.Texture.TargetX,
			TargetY = entry.Texture.TargetY,
			TargetWidth = entry.Texture.TargetWidth,
			TargetHeight = entry.Texture.TargetHeight, 
			
			BoundWidth = entry.Texture.BoundingWidth,
			BoundHeight = entry.Texture.BoundingHeight,
			
			Bitmap = MagickToBitmapImage(new(worker.GetTextureFor(entry.Texture, reference.Name.Content, false)))
		});
	}
	
	// Size coords must come first before offsets !! cuz evil execution order and typa shit
	Context.ReferenceSprite.SpriteWidth = reference.Width;
	Context.ReferenceSprite.SpriteHeight = reference.Height;
	Context.ReferenceSprite.IsSpecial = reference.IsSpecialType;
	Context.ReferenceSprite.SpecialVersion = reference.SVersion;
	Context.ReferenceSprite.GMS2PlaybackSpeedType = reference.GMS2PlaybackSpeedType;
	Context.ReferenceSprite.AnimationSpeed = reference.GMS2PlaybackSpeed;
	Context.ReferenceSprite.SpriteOffsetX = (float)reference.OriginXWrapper;
	Context.ReferenceSprite.SpriteOffsetY = (float)reference.OriginYWrapper;
	Context.ReferenceSprite.Name = reference.Name.Content;
	worker.Dispose();
	
	// To initiate first visual udpate
	// I don't like this but it works omfgfgfg ;-;
	Context.ReferenceSprite.CurrentSpriteFrame = 0.01f; // KILL ME
	Context.ReferenceSprite.CurrentSpriteFrame = 0.0f; // KILL ME
}

public void ReferenceSpriteNameBox_KeyDown(object sender, KeyEventArgs e) 
{
	TextBox SenderBox = (TextBox)sender;
	if (e.Key == Key.Enter)
		EnableReferenceSprite((SpriteEditorContext)SenderBox.DataContext, SenderBox.Text);
}

public SpriteEditorSprite CreateEditorSprite(SpriteEditorContext Context, string ImageElementName, string AreaBorderName, string TargetBorderName)
{
	SpriteEditorSprite Sprite = new();
	Sprite.ImageElement = (Image)Context.Window.FindName(ImageElementName);
	Sprite.SpriteAreaBorder = (Border)Context.Window.FindName(AreaBorderName);
	Sprite.SpriteTargetBorder = (Border)Context.Window.FindName(TargetBorderName);
	Sprite.AnimationTimer = new();
	Sprite.Frames = new();
	
	Sprite.AnimationTimer.Tick += Sprite.AnimationTimer_Tick;
	
	return Sprite;
}

public SpriteEditorContext CreateEditorContext() {
	SpriteEditorContext Context = new();
	Context.Window = (Window)LoadXaml(Path.Combine(WADDLETOOLS_ASSETS_DIR, "SpriteEditor.xaml"));
	Context.Canvas = (Canvas)Context.Window.FindName("EditorCanvas");
	Context.OffsetPointerRect = (System.Windows.Shapes.Rectangle)Context.Window.FindName("EditorOffsetPoint");
	Context.OffsetPresetsButton = (ButtonDark)Context.Window.FindName("OffsetPresetsButton");
	Context.ReferenceSpriteButton = (ButtonDark)Context.Window.FindName("ReferenceSpriteButton");
	Context.ReferenceSpriteNameBox = (TextBox)Context.Window.FindName("ReferenceSpriteNameBox");
	Context.PlayAnimButton = (ButtonDark)Context.Window.FindName("PlayAnimButton");
	Context.StopAnimButton = (ButtonDark)Context.Window.FindName("StopAnimButton");
	Context.PrevFrameButton = (ButtonDark)Context.Window.FindName("PrevFrameButton");
	Context.NextFrameButton = (ButtonDark)Context.Window.FindName("NextFrameButton");
	Context.ReferencePlayAnimButton = (ButtonDark)Context.Window.FindName("ReferencePlayAnimButton");
	Context.ReferenceStopAnimButton = (ButtonDark)Context.Window.FindName("ReferenceStopAnimButton");
	Context.ReferencePrevFrameButton = (ButtonDark)Context.Window.FindName("ReferencePrevFrameButton");
	Context.ReferenceNextFrameButton = (ButtonDark)Context.Window.FindName("ReferenceNextFrameButton");
	Context.SubmitReferenceNameButton = (ButtonDark)Context.Window.FindName("SubmitReferenceNameButton");
	Context.ConfirmEditButton = (ButtonDark)Context.Window.FindName("ConfirmEditButton");
	Context.ClearReferenceButton = (ButtonDark)Context.Window.FindName("ClearReferenceButton");
	Context.TextureGroupCombo = (ComboBoxDark)Context.Window.FindName("TextureGroupCombo");
	Context.OffsetPresetList = (ListBox)Context.Window.FindName("OffsetPresetList");
	Context.OffsetPresetsPopup = (Popup)Context.Window.FindName("OffsetPresetsPopup");
	Context.ReferenceSpritePopup = (Popup)Context.Window.FindName("ReferenceSpritePopup");
	Context.Sprite = CreateEditorSprite(Context, "SpriteVisual", "SpriteAreaBorder", "SpriteTargetBorder");
	Context.ReferenceSprite = CreateEditorSprite(Context, "ReferenceSpriteVisual", "ReferenceSpriteAreaBorder", "ReferenceSpriteTargetBorder");
	Context.BackgroundBrush = (DrawingBrush)Context.Canvas.Background;
	Context.WindowTask = new();
	
	Context.Window.Closed += (s, e) => {
		if (!Context.ConfirmButtonPressed)
			CustomScriptMessage("Sprite Edit Cancelled!", Context.Title, Context.CancelledMessageOwner);
		Context.WindowTask.SetResult(null);
	};
	
	Context.Window.Loaded += (s, e) => Context.ConfirmButtonPressed = false;
	Context.Sprite.PropertyChanged += (s, e) => Context.UpdateView();
	Context.ReferenceSprite.PropertyChanged += (s, e) => Context.UpdateView();
	Context.OffsetPresetList.SelectionChanged += Context.OffsetPresetsList_Select;
	Context.Canvas.MouseDown += Context.Canvas_MouseHandler;
	Context.Canvas.MouseUp += Context.Canvas_MouseHandler;
	Context.Canvas.MouseMove += Context.Canvas_MouseMove;
	Context.Canvas.MouseWheel += Context.Canvas_MouseWheel;
	Context.Canvas.SizeChanged += Context.Canvas_SizeChanged;
	Context.ReferenceSpriteNameBox.KeyDown += ReferenceSpriteNameBox_KeyDown;
	
	Context.PrevFrameButton.Click += (s, e) => Context.Sprite.CurrentSpriteFrame -= 1.0f;
	Context.NextFrameButton.Click += (s, e) => Context.Sprite.CurrentSpriteFrame += 1.0f;
	Context.PlayAnimButton.Click += (s, e) => Context.Sprite.AnimationTimer.Start();
	Context.StopAnimButton.Click += (s, e) => Context.Sprite.AnimationTimer.Stop();
	Context.ReferencePrevFrameButton.Click += (s, e) => Context.ReferenceSprite.CurrentSpriteFrame -= 1.0f;
	Context.ReferenceNextFrameButton.Click += (s, e) => Context.ReferenceSprite.CurrentSpriteFrame += 1.0f;
	Context.ReferencePlayAnimButton.Click += (s, e) => Context.ReferenceSprite.AnimationTimer.Start();
	Context.ReferenceStopAnimButton.Click += (s, e) => Context.ReferenceSprite.AnimationTimer.Stop();
	Context.OffsetPresetsButton.Click += Context.OffsetPresetsList_Open;
	Context.ReferenceSpriteButton.Click += Context.ReferenceSpritePopup_Open;
	Context.ClearReferenceButton.Click += Context.ClearReferenceButton_Click;
	Context.ConfirmEditButton.Click += Context.ConfirmButton_Click;
	Context.SubmitReferenceNameButton.Click += (s, e) => EnableReferenceSprite(Context, Context.ReferenceSpriteNameBox.Text);
	
	Context.Window.DataContext = Context;
	Context.GameFPS = Data.GeneralInfo.GMS2FPS;
	Context.IsGMS2 = Data.IsGameMaker2();
	Context.WaddleSpriteMode = false;
	Context.TextureGroupIndex = 0;
	
	int index = 0;
	Context.TextureGroupCombo.Items.Add("(No Texture Group)");
	foreach (UndertaleTextureGroupInfo TextureGroup in Data.TextureGroupInfo) {
		Context.TextureGroupCombo.Items.Add(TextureGroup.Name.Content);
		
		// Search for Default Texture Page(?)
		if (TextureGroup.Name.Content == "Default" && Context.TextureGroupIndex == 0) // hard-coded bullcrap my beloved
			Context.TextureGroupIndex = (index + 1);
		
		index++;
	}
	
	return Context;
}

public SpriteEditorContext CreateEditorContextFromSprite(object EditingSprite) {
	SpriteEditorContext Context = CreateEditorContext();

	if (EditingSprite is WaddleSprite) {
		WaddleSprite spr = (EditingSprite as WaddleSprite);
		ReloadSpriteFrameImages(spr);
		foreach (WaddleSpriteFrame frame in spr.Frames) {
			Context.Sprite.Frames.Add(new() {
				TargetX = frame.TargetX,
				TargetY = frame.TargetY,
				TargetWidth = frame.TargetWidth,
				TargetHeight = frame.TargetHeight, 
				
				BoundWidth = frame.BoundWidth,
				BoundHeight = frame.BoundHeight,
				
				Bitmap = MagickToBitmapImage(frame.Image)
			});
		};
		
		UnloadSpriteFrameImages(spr);
		Context.WaddleSpriteMode = true;
		Context.Sprite.SpriteWidth = spr.Width;
		Context.Sprite.SpriteHeight = spr.Height;
		Context.Sprite.IsSpecial = spr.Special;
		Context.Sprite.SpecialVersion = spr.SpecialVersion;
		Context.Sprite.GMS2PlaybackSpeedType = spr.GMS2PlaybackSpeedType;
		Context.Sprite.AnimationSpeed = spr.AnimationSpeed;
		Context.Sprite.SpriteOffsetX = (float)spr.OriginX;
		Context.Sprite.SpriteOffsetY = (float)spr.OriginY;
		Context.Sprite.Name = spr.Name;
	}
	else if (EditingSprite is UndertaleSprite) {
		UndertaleSprite spr = (EditingSprite as UndertaleSprite);
		TextureWorker worker = new();
		foreach (UndertaleSprite.TextureEntry entry in spr.Textures) 
		{
			Context.Sprite.Frames.Add(new() {
				TargetX = entry.Texture.TargetX,
				TargetY = entry.Texture.TargetY,
				TargetWidth = entry.Texture.TargetWidth,
				TargetHeight = entry.Texture.TargetHeight, 
				
				BoundWidth = entry.Texture.BoundingWidth,
				BoundHeight = entry.Texture.BoundingHeight,
				
				Bitmap = MagickToBitmapImage(new(worker.GetTextureFor(entry.Texture, spr.Name.Content, false)))
			});
		}
		
		// Size coords must come first before offsets !! cuz evil execution order and typa shit
		Context.Sprite.SpriteWidth = spr.Width;
		Context.Sprite.SpriteHeight = spr.Height;
		Context.Sprite.IsSpecial = spr.IsSpecialType;
		Context.Sprite.SpecialVersion = spr.SVersion;
		Context.Sprite.GMS2PlaybackSpeedType = spr.GMS2PlaybackSpeedType;
		Context.Sprite.AnimationSpeed = spr.GMS2PlaybackSpeed;
		Context.Sprite.SpriteOffsetX = (float)spr.OriginXWrapper;
		Context.Sprite.SpriteOffsetY = (float)spr.OriginYWrapper;
		Context.Sprite.Name = spr.Name.Content;
		Context.TextureGroupCombo.Items[0] = "(Disabled)";
		Context.TextureGroupIndex = 0;
		worker.Dispose();
	}
	
	// To initiate first visual udpate
	// I don't like this but it works omfgfgfg ;-;
	Context.Sprite.CurrentSpriteFrame = 0.01f; // KILL ME
	Context.Sprite.CurrentSpriteFrame = 0.0f; // KILL ME
	return Context;
}

#endregion