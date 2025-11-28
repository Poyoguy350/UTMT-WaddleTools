#load ".\Internal\Scripts\Constants.csx"
#load ".\Internal\Scripts\WaddleSprite.csx"
#load ".\Internal\Scripts\SpriteEditor.csx"

using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using UndertaleModLib.Util;

#region Variables

List<WaddleSprite> GraphicsSharpQueue = new();

Window GraphicsSharpWindow = (Window)LoadXaml(Path.Combine(ASSETS_DIR, "ImportGraphicsSharp.xaml")); 
Button QueueGraphicsButton = (Button)GraphicsSharpWindow.FindName("QueueGraphics");
Button DequeueGraphicsButton = (Button)GraphicsSharpWindow.FindName("DequeueGraphics");
ListView VisualQueue = (ListView)GraphicsSharpWindow.FindName("VisualQueue");
TaskCompletionSource<object> GraphicsSharpWindowTask = new();

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
			
			GraphicsSharpQueue.Add(sprite);
		}
		
		AddProgress(1);
	}
	
	await StopProgressBarUpdater();
	RefreshVisualQueue();
	HideProgressBar();
	
	GraphicsSharpWindow.IsEnabled = true;
}

public void RemoveSpriteButton_Click(object sender, RoutedEventArgs ev)
{
	if (VisualQueue.SelectedIndex == -1) {
		CustomScriptMessage("No selected sprite to delete! Operation aborted.");
		return;
	}
	
	string SpriteName = VisualQueue.SelectedItem.ToString();
	string spriteDirectory = Path.Combine(WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR, SpriteName);
	
	GraphicsSharpQueue.RemoveAt(VisualQueue.SelectedIndex);
	Directory.Delete(spriteDirectory, true);
	RefreshVisualQueue();
}

#endregion
#region Setup


#endregion
#region Events

GraphicsSharpWindow.Closed += (s, e) => GraphicsSharpWindowTask.SetResult(null);
QueueGraphicsButton.Click += (s, e) => ImportSpriteButton_Click(s, e);
DequeueGraphicsButton.Click += (s, e) => RemoveSpriteButton_Click(s, e);

#endregion
#region Execution

GraphicsSharpWindow.Show();
await GraphicsSharpWindowTask.Task;
if (ImportCancel) CustomScriptMessage("Import Cancelled!", "ImportGraphicsSharp");

#endregion