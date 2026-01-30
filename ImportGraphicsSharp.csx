#load ".\Internal\Scripts\Constants.csx"
#load ".\Internal\Scripts\WaddleSprite.csx"
#load ".\Internal\Scripts\SpriteEditor.csx"
#load ".\Internal\Scripts\TexturePacker.csx"

using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using static UndertaleModTool.MainWindow;
using UndertaleModLib.Util;

#region Classes

public class GameSpecificSpriteTemplate
{
	public string ConditionString;
	public string SpriteOffsetXString;
	public string SpriteOffsetYString;
	public string SpriteSpecialString;
	public string SpriteSpecialVersionString;
	public string SpriteGMS2PlaybackSpeedTypeString;
	public string SpritePlaybackSpeedString;
	
	public static string ReplaceMultiple(string BaseString, Dictionary<string, object> ReplaceDict)
	{
		string ResultString = new(BaseString);
		
		foreach (string Key in ReplaceDict.Keys)
		{
			string KeyBlock = "${" + Key + "}";
			ResultString = ResultString.Replace(KeyBlock, ReplaceDict[Key].ToString());
		}
		
		return ResultString;
	}
	
	public async static Task<AnimSpeedType> ParseAnimSpeedType(string StringBase)
	{
		switch (StringBase)
		{
			case ("FramesPerGameFrame"):
			{
				return AnimSpeedType.FramesPerGameFrame;
				break;
			}
			case ("FramesPerSecond"):
			{
				return AnimSpeedType.FramesPerSecond;
				break;
			}
			default:
			{
				return Enum.Parse<AnimSpeedType>(await CSharpScript.EvaluateAsync<string>(StringBase));
				break;
			}
		}
	}
}

#endregion
#region Variables

List<WaddleSprite> GraphicsSharpQueue = new();

Window GraphicsSharpWindow = (Window)LoadXaml(Path.Combine(WADDLETOOLS_ASSETS_DIR, "ImportGraphicsSharp.xaml")); 
Button QueueGraphicsButton = (Button)GraphicsSharpWindow.FindName("QueueGraphics");
Button DequeueGraphicsButton = (Button)GraphicsSharpWindow.FindName("DequeueGraphics");
Button EditGraphicsButton = (Button)GraphicsSharpWindow.FindName("EditGraphic");
Button StartImportButton = (Button)GraphicsSharpWindow.FindName("StartImport");
ListView VisualQueue = (ListView)GraphicsSharpWindow.FindName("VisualQueue");
TaskCompletionSource<object> GraphicsSharpWindowTask = new();
GameSpecificSpriteTemplate Template = null;

bool ImportCancel = true;

#endregion
#region Functions

public bool WaddleSpriteNameExists(string Name) {
	foreach (WaddleSprite sprite in GraphicsSharpQueue) {
		if (sprite.Name != Name) continue;
		return true;
	}
	
	return false;
}

public void RefreshVisualQueue() {
	VisualQueue.Items.Clear();
	
	foreach (WaddleSprite sprite in GraphicsSharpQueue) 
	{ VisualQueue.Items.Add(sprite.Name); }
}

async public void ImportSpriteButton_Click(object sender, RoutedEventArgs ev) 
{
	Microsoft.Win32.OpenFileDialog fileDialog = new ();
	fileDialog.Filter = "Images|*.png;*.gif";
	fileDialog.Multiselect = true;
	
	if (!fileDialog.ShowDialog(GraphicsSharpWindow).Value)
	{
		CustomScriptMessage("Operation cancelled.");
		return;
	}
	
	GraphicsSharpWindow.IsEnabled = false;
	List<WaddleSprite> UnknownGraphics = new();
	SetProgressBar(null, "Queueing Sprites...", 0, fileDialog.FileNames.Length);
    StartProgressBarUpdater();
	
	foreach (string filePath in fileDialog.FileNames)
	{
		WaddleSprite sprite = CreateWaddleSpriteFromFile(filePath);	
		
		if (sprite != null) {
			if (sprite.Name == "") {
				sprite.Name = "WaddleSprite";
				
				if (WaddleSpriteNameExists(sprite.Name)) {
					uint extra = 2;
					
					while (WaddleSpriteNameExists(sprite.Name)) 
					{ sprite.Name = "WaddleSprite" + extra.ToString(); extra++; }
				}
				
			}
			else if (WaddleSpriteNameExists(sprite.Name)) {
				if (ScriptQuestion("Theres already a sprite named \"" + sprite.Name + "\" queued for import? replace it with this one?")) {
					foreach (WaddleSprite sprite2 in GraphicsSharpQueue) {
						if (sprite2.Name != sprite.Name) continue;
						
						GraphicsSharpQueue.Remove(sprite2);
						break;
					}
				}
				else {
					CustomScriptMessage("Operation Aborted.", "ImportGraphicsSharp", GraphicsSharpWindow);
					AddProgress(1);
					continue;
				}
			}
			
			if (Template != null)
			{
				Dictionary<string, object> TemplateGlobals = new() { 
					{"SpriteWidth", sprite.Width}, 
					{"SpriteHeight", sprite.Height}, 
					{"SpriteName", sprite.Name} 
				};
				
				string OffsetXFixed = GameSpecificSpriteTemplate.ReplaceMultiple(Template.SpriteOffsetXString, TemplateGlobals);
				string OffsetYFixed = GameSpecificSpriteTemplate.ReplaceMultiple(Template.SpriteOffsetYString, TemplateGlobals);
				string SpecialFixed = GameSpecificSpriteTemplate.ReplaceMultiple(Template.SpriteSpecialString, TemplateGlobals);
				string SpecialVersionFixed = GameSpecificSpriteTemplate.ReplaceMultiple(Template.SpriteSpecialVersionString, TemplateGlobals);
				string SpriteGMS2PlaybackSpeedTypeString = GameSpecificSpriteTemplate.ReplaceMultiple(Template.SpriteGMS2PlaybackSpeedTypeString, TemplateGlobals);
				string SpritePlaybackSpeedStringFixed = GameSpecificSpriteTemplate.ReplaceMultiple(Template.SpritePlaybackSpeedString, TemplateGlobals);
				sprite.OriginX = await CSharpScript.EvaluateAsync<int>(OffsetXFixed);
				sprite.OriginY = await CSharpScript.EvaluateAsync<int>(OffsetYFixed);
				
				if (Data.IsGameMaker2())
				{
					sprite.Special = await CSharpScript.EvaluateAsync<bool>(SpecialFixed);
					sprite.SpecialVersion = await CSharpScript.EvaluateAsync<uint>(SpecialVersionFixed);
					sprite.GMS2PlaybackSpeedType = await GameSpecificSpriteTemplate.ParseAnimSpeedType(SpriteGMS2PlaybackSpeedTypeString);
					sprite.AnimationSpeed = (float)(await CSharpScript.EvaluateAsync<double>(SpritePlaybackSpeedStringFixed));
				}
				else
					CustomScriptMessage("UndertaleData isn't a GM2 type! Cannot apply Special Playback Templates.", "ImportGraphicsSharp", GraphicsSharpWindow);
			}
			
			GraphicsSharpQueue.Add(sprite);
			
			if (sprite.SpriteType == WaddleSpriteType.Unknown)
				UnknownGraphics.Add(sprite);
		}
		
		AddProgress(1);
	}
	
	await StopProgressBarUpdater();
	HideProgressBar();
	
	if (UnknownGraphics.Count > 0)
	{
		Window UndefinedSpritesWindow = (Window)LoadXaml(Path.Combine(WADDLETOOLS_ASSETS_DIR, "UndefinedSpritesQueue.xaml"));
		ListBox UnknownQueue = (ListBox)UndefinedSpritesWindow.FindName("UnknownQueue");
		Button ConfirmTypesButton = (Button)UndefinedSpritesWindow.FindName("ConfirmTypesButton");
		TaskCompletionSource<object> UndefinedSpritesTask = new();
		bool UndefinedSpritesConfirmed = false; // i actually have no idea what practical use this has but idk
		
		
		UndefinedSpritesWindow.Closed += (s, e) => UndefinedSpritesTask.SetResult(null);
		ConfirmTypesButton.Click += (s, e) => {
			UndefinedSpritesConfirmed = true;
			UndefinedSpritesWindow.Close();
		};
		
		foreach (WaddleSprite spr in UnknownGraphics)
			UnknownQueue.Items.Add(spr.Name);
		
		int index = 0;
		UndefinedSpritesWindow.Show();
		foreach (WaddleSprite spr in UnknownGraphics)
		{
			ListBoxItem Item = (ListBoxItem)UnknownQueue.ItemContainerGenerator.ContainerFromItem(UnknownQueue.Items[index]);
			ContentPresenter Presenter = FindVisualChild<ContentPresenter>(Item); // using static UndertaleModTool.MainWindow;
			ComboBoxDark TypeComboBox = (ComboBoxDark)Presenter.ContentTemplate.FindName("SpriteTypeCombo", Presenter);	
			TypeComboBox.Items[1] = Data.IsGameMaker2() ? "Tileset" : "Background"; // disgusting but it's the least that i can do
			
			index++;
		}
		
		await UndefinedSpritesTask.Task;
		
		
		index = 0;
		foreach (WaddleSprite spr in UnknownGraphics)
		{
			if (!UndefinedSpritesConfirmed) {
				spr.SpriteType = WaddleSpriteType.Sprite;
			}
			else {
				ListBoxItem Item = (ListBoxItem)UnknownQueue.ItemContainerGenerator.ContainerFromItem(UnknownQueue.Items[index]);
				ContentPresenter Presenter = FindVisualChild<ContentPresenter>(Item); // using static UndertaleModTool.MainWindow;
				ComboBoxDark TypeComboBox = (ComboBoxDark)Presenter.ContentTemplate.FindName("SpriteTypeCombo", Presenter);	
				string WadSprTypeString = TypeComboBox.SelectedItem.ToString();
				
				spr.SpriteType = Enum.Parse<WaddleSpriteType>((WadSprTypeString == "Tileset") ? "Background" : WadSprTypeString);
			}
			
			index++;
			if (spr.SpriteType == WaddleSpriteType.Background && spr.Frames.Count > 1) {
				GraphicsSharpQueue.Remove(spr);
				CustomScriptMessage($"Sprite \"{spr.Name}\" not imported, type was set to \"WaddleSpriteType.Background\" but it has more than a single frame!", "ImportGraphicsSharp", GraphicsSharpWindow);
			}
		}
		
		if (!UndefinedSpritesConfirmed)
			CustomScriptMessage("Configuration cancelled! All unknown sprites are set to \"WaddleSpriteType.Sprite\".", "ImportGraphicsSharp", GraphicsSharpWindow);
	}
	
	RefreshVisualQueue();
	GraphicsSharpWindow.IsEnabled = true;
}

public void RemoveSpriteButton_Click(object sender, RoutedEventArgs ev)
{
	if (VisualQueue.SelectedIndex == -1) {
		CustomScriptMessage("No selected sprite to delete! Operation aborted.", "ImportGraphicsSharp", GraphicsSharpWindow);
		return;
	}
	
	string SpriteName = VisualQueue.SelectedItem.ToString();
	string spriteDirectory = Path.Combine(WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR, SpriteName);
	
	GraphicsSharpQueue.RemoveAt(VisualQueue.SelectedIndex);
	Directory.Delete(spriteDirectory, true);
	RefreshVisualQueue();
}

public async void EditSpriteButton_Click(object sender, RoutedEventArgs ev)
{
	if (VisualQueue.SelectedIndex == -1) {
		CustomScriptMessage("No selected sprite to edit! Operation aborted.", "ImportGraphicsSharp", GraphicsSharpWindow);
		return;
	}
	
	WaddleSprite editingSprite = GraphicsSharpQueue[VisualQueue.SelectedIndex];
	if (editingSprite.SpriteType != WaddleSpriteType.Sprite) {
		CustomScriptMessage("This feature is only available for sprite with types \"WaddleSpriteType.Sprite\"!", "ImportGraphicsSharp", GraphicsSharpWindow); // handicapping my script </3
		return;
	}
	
	SpriteEditorContext Context = CreateEditorContextFromSprite(editingSprite);
	Context.CancelledMessageOwner = GraphicsSharpWindow;
	Context.Window.Show();
	GraphicsSharpWindow.IsEnabled = false;
	await Context.WindowTask.Task;
	GraphicsSharpWindow.IsEnabled = true;
	
	if (Context.ConfirmButtonPressed)
		Context.Sprite.ApplyToSprite(editingSprite);
}

#endregion
#region Setup

EnsureDataLoaded();

foreach (string SpecificData in Directory.GetFiles(WADDLETOOLS_IMPORTGRAPHICS_TEMPLATES_DIR))
{
	string SpecificDataText = File.ReadAllText(SpecificData); 
	JsonNode Root = JsonNode.Parse(SpecificDataText);
	GameSpecificSpriteTemplate NewTemplate = new();
	
	NewTemplate.ConditionString = Root["ConditionExpression"].ToString();
	NewTemplate.SpriteOffsetXString = Root["SpriteTemplate"]["OriginX"].ToString();
	NewTemplate.SpriteOffsetYString = Root["SpriteTemplate"]["OriginY"].ToString();
	NewTemplate.SpriteSpecialString = Root["SpriteTemplate"]["Special"].ToString();
	NewTemplate.SpriteSpecialVersionString = Root["SpriteTemplate"]["SpecialVersion"].ToString();
	NewTemplate.SpriteGMS2PlaybackSpeedTypeString = Root["SpriteTemplate"]["GMS2PlaybackSpeedType"].ToString();
	NewTemplate.SpritePlaybackSpeedString = Root["SpriteTemplate"]["AnimationSpeed"].ToString();
	
	string ConditionStringFixed = GameSpecificSpriteTemplate.ReplaceMultiple(NewTemplate.ConditionString, new() {
		{"DisplayName", Data.GeneralInfo.DisplayName}, {"FileName", Data.GeneralInfo.FileName}
	});
	
	bool result = await CSharpScript.EvaluateAsync<bool>(ConditionStringFixed);
	if (!result)
		continue;
	
	Template = NewTemplate;
	break;
}

#endregion
#region Events

GraphicsSharpWindow.Closed += (s, e) => GraphicsSharpWindowTask.SetResult(null);
QueueGraphicsButton.Click += (s, e) => ImportSpriteButton_Click(s, e);
DequeueGraphicsButton.Click += (s, e) => RemoveSpriteButton_Click(s, e);
EditGraphicsButton.Click += (s, e) => EditSpriteButton_Click(s, e);
StartImportButton.Click += (s, e) => {
	ImportCancel = false;
	GraphicsSharpWindow.Close();
};

#endregion
#region Execution

GraphicsSharpWindow.Show();
await GraphicsSharpWindowTask.Task;
if (ImportCancel) CustomScriptMessage("Import Cancelled!", "ImportGraphicsSharp");
else
{
	int LastTexturePageCount = Data.EmbeddedTextures.Count - 1;
	int LastTexturePageItemCount = Data.TexturePageItems.Count - 1;
	
	foreach (var SpritesGroup in GraphicsSharpQueue.GroupBy(Asset => Asset.TextureGroup)) {
		Packer _Packer = new();
		List<WaddleSpriteFrame> FramesQueue = new();
		
		foreach (WaddleSprite Sprite in SpritesGroup)
		{
			switch (Sprite.SpriteType) {
				case (WaddleSpriteType.Sprite): {
					UndertaleSprite UTSprite = Data.Sprites.ByName(Sprite.Name);
					
					// Create Sprite if it doesn't exist...
					if (UTSprite == null) {
						UTSprite = new();
						UTSprite.Name = Data.Strings.MakeString(Sprite.Name);
						Data.Sprites.Add(UTSprite);
					}
					
					// Hawk tuah! update that thang...
                    UTSprite.Width = (uint)Sprite.Width;
                    UTSprite.Height = (uint)Sprite.Height;
                    UTSprite.MarginLeft = Sprite.MarginLeft;
                    UTSprite.MarginRight = Sprite.MarginRight;
                    UTSprite.MarginTop = Sprite.MarginTop;
                    UTSprite.MarginBottom = Sprite.MarginBottom;
                    UTSprite.GMS2PlaybackSpeedType = Sprite.GMS2PlaybackSpeedType;
                    UTSprite.GMS2PlaybackSpeed = Sprite.AnimationSpeed;
                    UTSprite.IsSpecialType = Sprite.Special;
                    UTSprite.SVersion = Sprite.SpecialVersion;
					UTSprite.OriginX = Sprite.OriginX;
					UTSprite.OriginY = Sprite.OriginY;
					
					for (int i = UTSprite.Textures.Count; i < Sprite.Frames.Count; i++)
						UTSprite.Textures.Add(null);
					
					if (Sprite.TextureGroup != null) {
						foreach (UndertaleTextureGroupInfo TxGroup in Data.TextureGroupInfo) {
							List<int> RemoveIndexes = new();
							
							int IteratedIndex = 0;
							foreach (var SpriteResource in TxGroup.Sprites) {
								if (SpriteResource.Resource == UTSprite)
									RemoveIndexes.Add(IteratedIndex);
								IteratedIndex++;
							}
							
							foreach(int Index in RemoveIndexes)
								TxGroup.Sprites.RemoveAt(Index);
						}
						
						UndertaleTextureGroupInfo TextureGroup = Data.TextureGroupInfo.ByName(Sprite.TextureGroup);
						TextureGroup.Sprites.Add(new() { Resource = UTSprite });
					}
					
					break;
				}
				case (WaddleSpriteType.Background): {
					UndertaleBackground UTBackground = Data.Backgrounds.ByName(Sprite.Name);
					
					if (UTBackground != null)
						continue;
					
					UTBackground = new UndertaleBackground();
                    UTBackground.Name = Data.Strings.MakeString(Sprite.Name);
                    UTBackground.Transparent = false;
                    UTBackground.Preload = false;
                    Data.Backgrounds.Add(UTBackground);
					
					if (Sprite.TextureGroup != null) {
						UndertaleTextureGroupInfo TextureGroup = Data.TextureGroupInfo.ByName(Sprite.TextureGroup);
						TextureGroup.Tilesets.Add(new() { Resource = UTBackground });
					}
					
					break;
				}
			}
			
			ReloadSpriteFrameImages(Sprite);
			FramesQueue.AddRange(Sprite.Frames);
		}
		
		_Packer.Pack(FramesQueue);
		foreach (PackerAtlas Atlas in _Packer.OutputAtlasses) {
			UndertaleEmbeddedTexture NewTexture = new();
			NewTexture.Name = new UndertaleString($"Texture {++LastTexturePageCount}");
			NewTexture.TextureData.Image = GMImage.FromMagickImage(Atlas.CreateImage()).ConvertToPng();
			Data.EmbeddedTextures.Add(NewTexture);
			
			if (SpritesGroup.Key != null) {
				UndertaleTextureGroupInfo TextureGroup = Data.TextureGroupInfo.ByName(SpritesGroup.Key);
				TextureGroup.TexturePages.Add(new() { Resource = NewTexture });
			}
			
			foreach (PackerNode Node in Atlas.Nodes)
			{
				if (Node.Source == null)
					continue;
				
				UndertaleTexturePageItem NewPageItem = new UndertaleTexturePageItem();
                NewPageItem.Name = new UndertaleString($"PageItem {++LastTexturePageItemCount}");
                NewPageItem.SourceX = (ushort)Node.X;
                NewPageItem.SourceY = (ushort)Node.Y;
                NewPageItem.SourceWidth = (ushort)Node.Width;
                NewPageItem.SourceHeight = (ushort)Node.Height;
                NewPageItem.TargetX = (ushort)Node.Source.TargetX;
                NewPageItem.TargetY = (ushort)Node.Source.TargetY;
                NewPageItem.TargetWidth = (ushort)Node.Source.TargetWidth;
                NewPageItem.TargetHeight = (ushort)Node.Source.TargetHeight;
                NewPageItem.BoundingWidth = (ushort)Node.Source.BoundWidth;
                NewPageItem.BoundingHeight = (ushort)Node.Source.BoundHeight;
                NewPageItem.TexturePage = NewTexture;
				Data.TexturePageItems.Add(NewPageItem);
				
				WaddleSprite ImportingSprite = Node.Source.SpriteSource;
				switch (ImportingSprite.SpriteType) {
					case (WaddleSpriteType.Sprite): {
						UndertaleSprite UTSprite = Data.Sprites.ByName(ImportingSprite.Name);
						UndertaleSprite.TextureEntry SpriteEntry = new();
						SpriteEntry.Texture = NewPageItem;
						
						UTSprite.Textures[ImportingSprite.Frames.IndexOf(Node.Source)] = SpriteEntry; // stinky
						break;
					}
					case (WaddleSpriteType.Background): {
						UndertaleBackground UTBackground = Data.Backgrounds.ByName(ImportingSprite.Name);
						UTBackground.Texture = NewPageItem;
						break;
					}
				}
			}
		}
		
		//_Packer.SaveAtlasses(Path.Combine(WADDLETOOLS_DIR, "PackerTest"));
	}
}

#endregion