using UndertaleModLib.Util;
using ImageMagick;

// gng we can't use constants or it won't run the script ;-; (or static too)
public string WADDLETOOLS_DIR = Path.Combine(ExePath, "WaddleTools");
public string WADDLETOOLS_EDITORASSETS_DIR = Path.Combine(WADDLETOOLS_DIR, "EditorAssets");
public string WADDLETOOLS_EDITORGFX_DIR = Path.Combine(WADDLETOOLS_EDITORASSETS_DIR, "Graphics");
public string WADDLETOOLS_IMPORTGRAPHICS_DIR = Path.Combine(WADDLETOOLS_DIR, "ImportGraphicsSharp");
public string WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR = Path.Combine(WADDLETOOLS_IMPORTGRAPHICS_DIR, "Sprites");
public string SPRITENAME_REGEX = @"(.*)(\d+)";

public MagickReadSettings MAGICK_READSETTINGS = new() { ColorSpace = ColorSpace.sRGB };