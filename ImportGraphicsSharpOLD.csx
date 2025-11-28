#load "..\..\WaddleTools\Scripts\Constants.csx"
#load "..\..\WaddleTools\Scripts\WaddleSprite.csx"
#load "..\..\WaddleTools\Scripts\SpriteEditor.csx"

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

EnsureDataLoaded();

List<WaddleSprite> ImportSpriteList = new();
bool importCancel = true;

public bool WaddleSpriteNameExists(string Name) {
	foreach (WaddleSprite sprite in ImportSpriteList) {
		if (sprite.Name != Name) continue;
		return true;
	}
	
	return false;
}

public void ImportSpriteButton_Click(object sender, RoutedEventArgs ev) 
{
	var fileDialog = new Microsoft.Win32.OpenFileDialog();
	fileDialog.Filter = "Images|*.png;*.gif";
	fileDialog.Multiselect = true;
	
	if (!fileDialog.ShowDialog(WindowSpriteSelector).Value)
	{
		CustomScriptMessage("Operation cancelled.");
		return;
	}
	
	int prog = 0;
	var updateImportProg = () => WindowSpriteSelector.Title = "Importing Sprites (" + prog + "/" + fileDialog.FileNames.Length + ")";
	
	WindowSpriteSelector.IsEnabled = false;
	updateImportProg();
	
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
					foreach (WaddleSprite sprite2 in ImportSpriteList) {
						if (sprite2.Name != sprite.Name) continue;
						
						ImportSpriteList.Remove(sprite2);
						break;
					}
				}
				else {
					CustomScriptMessage("Operation Aborted.", "ImportGraphicsSharp", WindowSpriteSelector);
					continue;
				}
			}
			
			ImportSpriteList.Add(sprite);
		}
		prog++;
		updateImportProg();
	}
	
	RefreshSpritesList();
	WindowSpriteSelector.Title = prog + " Sprites Imported!";
	WindowSpriteSelector.IsEnabled = true;
}

public void RemoveSpriteButton_Click(object sender, RoutedEventArgs ev)
{
	if (SpritesList.SelectedIndex == -1) {
		CustomScriptMessage("No selected sprite to delete! Operation aborted.");
		return;
	}
	
	string SpriteName = SpritesList.SelectedItem.ToString();
	string spriteDirectory = Path.Combine(WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR, SpriteName);
	
	ImportSpriteList.RemoveAt(SpritesList.SelectedIndex);
	Directory.Delete(spriteDirectory, true);
	RefreshSpritesList();
}

public async void EditSpriteButton_Click(object sender, RoutedEventArgs ev)
{
	if (SpritesList.SelectedItem == null) {
		CustomScriptMessage("No selected sprite to edit! Operation aborted.");
		return;
	}
	
	editingSprite = ImportSpriteList.Find(spr => spr.Name == SpritesList.SelectedItem.ToString());
	
	editorIsWaddleSprite = true;
	WindowSpriteSelector.IsEnabled = false;
	WindowSpriteEditor.Owner = WindowSpriteSelector;
	spriteEditorWindowTCS = new();
	SpriteCanvas_Startup();
	WindowSpriteEditor.Show();
	await spriteEditorWindowTCS.Task;
	WindowSpriteSelector.IsEnabled = true;
	
	WaddleSprite spr = (editingSprite as WaddleSprite);
	CustomScriptMessage($"Offset: {spr.OriginX}, {spr.OriginY}", "woaw", WindowSpriteSelector);
}

public void StartImportButton_Click(object sender, RoutedEventArgs ev) 
{
	importCancel = false;
	WindowSpriteSelector.Close();
}

public void RefreshSpritesList() {
	SpritesList.Items.Clear();
	
	foreach (WaddleSprite sprite in ImportSpriteList) {
		SpritesList.Items.Add(sprite.Name);
	}
}

Window WindowSpriteSelector = new() {
	Title = "Select/Import Sprites!",
	ResizeMode = ResizeMode.NoResize,
	WindowStartupLocation = WindowStartupLocation.CenterScreen,
	Width = 800,
	Height = 800
};

Canvas WindowCanvas = new() {
	Width = 800,
	Height = 800
};

ButtonDark ImportSpriteButton = new() {
	Name = "ImportSpriteButton",
	Content = new TextBlock() {
		Text = "Add\nSprites",
		TextAlignment = System.Windows.TextAlignment.Center,
		Foreground = Brushes.Black
	},
	
	Width = 120,
	Height = 50,
	Margin = new(20, 680, 0, 0)		
};

ButtonDark DeleteQueuedSprite = new() {
	Name = "DeleteQueuedSprite",
	Content = new TextBlock() {
		Text = "Remove\nSprite",
		TextAlignment = System.Windows.TextAlignment.Center,
		Foreground = Brushes.Black
	},
	
	Width = 120,
	Height = 50,
	Margin = new(160, 680, 0, 0)		
};

ButtonDark EditSpriteButton = new() {
	Name = "EditSpriteButton",
	Content = new TextBlock() {
		Text = "Edit Sprite",
		TextAlignment = System.Windows.TextAlignment.Center,
		Foreground = Brushes.Black
	},
	
	Width = 120,
	Height = 50,
	Margin = new(300, 680, 0, 0)
};

ButtonDark StartImportButton = new() {
	Name = "StartImportButton",
	Content = new TextBlock() {
		Text = "Start Import",
		TextAlignment = System.Windows.TextAlignment.Center,
		Foreground = Brushes.Black
	},
	
	Width = 120,
	Height = 50,
	Margin = new(440, 680, 0, 0)
};

ListBox SpritesList = new() {
	Name = "SpritesList",
	BorderThickness = new(2),
	
	Width = 750,
	Height = 640,
	Margin = new(12.5, 20, 0, 0)
};

SpritesList.SetResourceReference(ListBox.BackgroundProperty, SystemColors.WindowBrushKey);
SpritesList.SetResourceReference(ListBox.ForegroundProperty, SystemColors.WindowTextBrushKey);

TaskCompletionSource<object> spriteSelectorWindowTCS = new();

ImportSpriteButton.Click += ImportSpriteButton_Click;
DeleteQueuedSprite.Click += RemoveSpriteButton_Click;
EditSpriteButton.Click += EditSpriteButton_Click;
StartImportButton.Click += StartImportButton_Click;
WindowSpriteSelector.Closed += (s, e) => spriteSelectorWindowTCS.SetResult(null);

WindowCanvas.Children.Add(ImportSpriteButton);
WindowCanvas.Children.Add(DeleteQueuedSprite);
WindowCanvas.Children.Add(EditSpriteButton);
WindowCanvas.Children.Add(StartImportButton);
WindowCanvas.Children.Add(SpritesList);

WindowSpriteSelector.Content = WindowCanvas;

// clear shit from previous run of this script
if (Directory.Exists(WADDLETOOLS_IMPORTGRAPHICS_DIR)) {
	foreach (string filePath in Directory.GetFiles(WADDLETOOLS_IMPORTGRAPHICS_DIR))
	{ File.Delete(filePath); }
	foreach (string subDir in Directory.GetDirectories(WADDLETOOLS_IMPORTGRAPHICS_DIR))
	{ Directory.Delete(subDir, true); }
}

Directory.CreateDirectory(WADDLETOOLS_IMPORTGRAPHICS_DIR);
Directory.CreateDirectory(WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR);

MessageWindowOwner_WaddleSprite = WindowSpriteSelector;
WindowSpriteSelector.Show();
await spriteSelectorWindowTCS.Task;
if (importCancel) ScriptMessage("Script Cancelled!");

