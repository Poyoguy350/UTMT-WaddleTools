using UndertaleModLib.Util;
using ImageMagick;

// gng we can't use constants or it won't run the script ;-; (or static too)
public string WADDLETOOLS_DIR = Path.GetDirectoryName(ScriptPath);
public string WADDLETOOLS_INTERNAL_DIR = Path.Combine(WADDLETOOLS_DIR, "Internal");
public string WADDLETOOLS_ASSETS_DIR = Path.Combine(WADDLETOOLS_INTERNAL_DIR, "Assets");
public string WADDLETOOLS_IMPORTGRAPHICS_DIR = Path.Combine(WADDLETOOLS_INTERNAL_DIR, "ImportGraphicsSharp");
public string WADDLETOOLS_IMPORTGRAPHICS_TEMPLATES_DIR = Path.Combine(WADDLETOOLS_IMPORTGRAPHICS_DIR, "GameSpecificData");
public string WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR = Path.Combine(WADDLETOOLS_IMPORTGRAPHICS_DIR, "Sprites");
public string SPRITENAME_REGEX = @"(.*)(\d+)";

public MagickReadSettings MAGICK_READSETTINGS = new() { ColorSpace = ColorSpace.sRGB };