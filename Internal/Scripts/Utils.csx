using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using UndertaleModLib.Util;
using ImageMagick;

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

public BitmapImage MagickToBitmapImage(MagickImage magickImg) {
	BitmapImage bitmapImg = new();
	
	using (MemoryStream stream = new())
	{
		magickImg.Write(stream, MagickFormat.Png);
		stream.Position = 0;
		
		bitmapImg.BeginInit();
		bitmapImg.StreamSource = stream;
		bitmapImg.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
		bitmapImg.EndInit();
	}
	
	return bitmapImg;
}