using System.Windows;
using UndertaleModLib.Util;
using System.Windows.Markup;
using System.Threading.Tasks;

public MessageBoxResult CustomScriptMessage(string message, string title = "Message", Window owner = null) 
{ 
	if (owner == null) owner = Application.Current.MainWindow;
	return MessageBoxExtensions.ShowMessage(owner, message, title); 
}


public object LoadXaml(string path) {
	using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
		return XamlReader.Load(fileStream);
	}
	
	return null;
}