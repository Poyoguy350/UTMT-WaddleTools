#load ".\Internal\Scripts\SpriteEditor.csx"

using System.Windows;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

EnsureDataLoaded();
UndertaleModTool.MainWindow mainWindow = Application.Current.MainWindow as UndertaleModTool.MainWindow;
UndertaleSprite editingSprite = null;

if (mainWindow.CurrentTab.CurrentObject is UndertaleSprite)
	editingSprite = (mainWindow.CurrentTab.CurrentObject as UndertaleSprite);
else {
	Window SpriteEditorInputBoxWindow = (Window)LoadXaml(Path.Combine(WADDLETOOLS_ASSETS_DIR, "SpriteEditorInputBox.xaml"));
	TextBox SpriteNameBox = (TextBox)SpriteEditorInputBoxWindow.FindName("SpriteNameBox");
	ButtonDark EditButton = (ButtonDark)SpriteEditorInputBoxWindow.FindName("EditButton");
	TaskCompletionSource<object> SpriteEditorInputBoxWindowTask = new();
	bool editCancel = true;
	
	SpriteEditorInputBoxWindow.Closed += (s, e) => SpriteEditorInputBoxWindowTask.SetResult(null);
	RoutedEventHandler SearchSprite = (s, e) => {
		editingSprite = null;
		foreach (UndertaleSprite sprite in Data.Sprites) {
			if (sprite.Name.Content != SpriteNameBox.Text) continue;
			
			editingSprite = sprite;
			break;
		}
		
		if (editingSprite == null) {
			CustomScriptMessage($"Unable to search for a sprite named \"{SpriteNameBox.Text}\"!", 
				"WaddleTools' SpriteEditor", SpriteEditorInputBoxWindow);
			return;
		}
		
		editCancel = false;
		SpriteEditorInputBoxWindow.Close();
	};
	
	
	EditButton.Click += SearchSprite;
	SpriteNameBox.KeyDown += (s, e) => {
		if (e.Key == Key.Enter)
			SearchSprite(s, e);
	};
	
	SpriteEditorInputBoxWindow.Show();
	await SpriteEditorInputBoxWindowTask.Task;
	
	if (editCancel) {
		CustomScriptMessage("Script Cancelled!", "WadleTools' SpriteEditor");
		return;
	}
}

SpriteEditorContext Context = CreateEditorContextFromSprite(editingSprite);
Context.Window.Show();
await Context.WindowTask.Task;

if (Context.ConfirmButtonPressed)
	Context.Sprite.ApplyToSprite(editingSprite);