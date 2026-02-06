#load "./Constants.csx"
#load "./Utils.csx"

using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows;

using UndertaleModLib.Util;
using ImageMagick;

Window MessageWindowOwner_WaddleSprite = null;

public enum WaddleSpriteType: uint
{
	Sprite,
    Background,
    Font,
    Unknown
}

public enum WaddleBBoxMode: uint
{
	None = 0,
	FullImage = 1,
	Manual = 2
}

public class WaddleSpriteFrame {
	public ushort TargetX = 0;
	public ushort TargetY = 0;
	public ushort TargetWidth = 0;
	public ushort TargetHeight = 0; 
	
	public ushort BoundWidth = 0;
	public ushort BoundHeight = 0;
	
	public MagickImage Image = null;
	
	private WaddleSprite _SpriteSource = null;
	public WaddleSprite SpriteSource
	{
		get => _SpriteSource;
		set {
			if (_SpriteSource != null)
				_SpriteSource.Frames.Remove(this);
			_SpriteSource = value;
		}
	}
}

public class WaddleSprite {
	public string Name = "Sprite";
	public string TextureGroup = null;
	
	public uint Width = 0;
	public uint Height = 0;
	
	public int MarginLeft = 0;
	public int MarginRight = 0;
	public int MarginTop = 0;
	public int MarginBottom = 0;
	
	public int OriginX = 0;
	public int OriginY = 0;
	
	// Members used by the script
	public bool EditedByUser = false; 
	public bool GenerateMasks = false;
	public bool ChangedSpriteDimensions = false;
	public bool GrewBoundingBox = false;
	public WaddleSpriteFrame BiggestFrame = null;
	
	public bool Special = false;
	public uint SpecialVersion = 1;
	
	public float AnimationSpeed = 15.0f;
	public AnimSpeedType GMS2PlaybackSpeedType = 0;
	public WaddleSpriteType SpriteType = WaddleSpriteType.Unknown;
	public WaddleBBoxMode BBoxMode = WaddleBBoxMode.None;
	
	public List<WaddleSpriteFrame> Frames = new();
	public List<uint> FramesSequence = new();
}

// TODO: change this later
public WaddleSpriteType GetSpriteType(string name)
{
	if (name.StartsWith("spr_") || name.StartsWith("sprite_"))
		return WaddleSpriteType.Sprite;
	else if (name.StartsWith("bg_") || name.StartsWith("background_"))
		return WaddleSpriteType.Background;
	
	// ImportGraphics disregard fonts as sprites? let's just set them unknown for now.
	//else if (name.StartsWith("fn_") || name.StartsWith("fnt_") || name.StartsWith("font_"))
	//	return WaddleSpriteType.Font;
	
	return WaddleSpriteType.Unknown;
}

public void UnloadSpriteFrameImages(WaddleSprite waddleSprite) {
	foreach (WaddleSpriteFrame frame in waddleSprite.Frames) 
	{ frame.Image.Dispose(); }
}

public void ReloadSpriteFrameImages(WaddleSprite waddleSprite) {
	string spriteDirectory = WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR + "\\" + waddleSprite.Name;
	
	for (int i = 0; i < waddleSprite.Frames.Count; i++)
	{ waddleSprite.Frames[i].Image = new(spriteDirectory + "\\" + i.ToString() + ".png", MAGICK_READSETTINGS); }
}

// Unused Function, breaks the whole tool that loads ts anyway so whateverr bye bye
//public MagickImage GetWaddleImageWithPadding(WaddleSpriteFrame frame) {
//	MagickImage returningImage = new(MagickColors.Transparent, (uint)frame.BoundWidth, (uint)frame.BoundHeight);
//	returningImage.Composite(frame.Image, frame.TargetX, frame.TargetY, CompositeOperator.Copy);
//	
//	return returningImage;
//}

public void WriteWaddleSpriteFrames(WaddleSprite sprite) {
	string spriteDirectory = WADDLETOOLS_IMPORTGRAPHICSPLUSPLUS_SPRITES_DIR + "\\" + sprite.Name;
	Directory.CreateDirectory(spriteDirectory);
	
	for (int i = 0; i < sprite.Frames.Count; i++)
	{ sprite.Frames[i].Image.Write(spriteDirectory + "\\" + i.ToString() + ".png"); }
}

public void SetWaddleSpriteFrameImg(ref WaddleSpriteFrame waddleFrame, MagickImage img) {
	waddleFrame.Image = img;
	waddleFrame.BoundWidth = (ushort)waddleFrame.Image.Width;
	waddleFrame.BoundHeight = (ushort)waddleFrame.Image.Height;
	
	waddleFrame.Image.BorderColor = MagickColors.Transparent;
    waddleFrame.Image.BackgroundColor = MagickColors.Transparent;
    waddleFrame.Image.Border(1);
    IMagickGeometry? bbox = waddleFrame.Image.BoundingBox;
	
	if (bbox != null)
    {
        waddleFrame.TargetX = (ushort)(bbox.X - 1);
        waddleFrame.TargetY = (ushort)(bbox.Y - 1);
		waddleFrame.TargetWidth = (ushort)(bbox.Width);
        waddleFrame.TargetHeight = (ushort)(bbox.Height);
        waddleFrame.Image.Trim();
    }
    else
    {
        waddleFrame.TargetX = 0;
        waddleFrame.TargetY = 0;
        waddleFrame.Image.Crop(1, 1);
    }
	
	// UTMTCE Compatibilities man whateverr
	string ResetPageMethod = "ResetPage";
	var MagickType = waddleFrame.Image.GetType();
	if (MagickType.GetMethod(ResetPageMethod) == null)
		ResetPageMethod = "RePage";
	MagickType.GetMethod(ResetPageMethod).Invoke(waddleFrame.Image, null);
}

public void AddWaddleFrameToSprite(ref WaddleSprite wadSpr, ref WaddleSpriteFrame waddleFrame, ref bool definedMargins) {
	wadSpr.Width = Math.Max(wadSpr.Width, waddleFrame.BoundWidth);
	wadSpr.Height = Math.Max(wadSpr.Height, waddleFrame.BoundHeight);
	
	int PrevMarginLeft = wadSpr.MarginLeft;
	int PrevMarginTop = wadSpr.MarginTop;
	int PrevMarginRight = wadSpr.MarginRight;
	int PrevMarginBottom = wadSpr.MarginBottom;
	
	if (!definedMargins) {
		definedMargins = true;	
		wadSpr.MarginLeft = waddleFrame.TargetX;
		wadSpr.MarginTop = waddleFrame.TargetY;
		wadSpr.MarginRight =  waddleFrame.TargetX + (waddleFrame.TargetWidth - 1);
		wadSpr.MarginBottom = waddleFrame.TargetY + (waddleFrame.TargetHeight - 1);
	}
	else {
		wadSpr.MarginLeft = Math.Min(wadSpr.MarginLeft, waddleFrame.TargetX);
		wadSpr.MarginTop = Math.Min(wadSpr.MarginTop, waddleFrame.TargetY);
		wadSpr.MarginRight = Math.Max(wadSpr.MarginRight, waddleFrame.TargetX + (waddleFrame.TargetWidth - 1));
		wadSpr.MarginBottom = Math.Max(wadSpr.MarginBottom, waddleFrame.TargetY + (waddleFrame.TargetHeight - 1));
	}
	
	if (wadSpr.MarginLeft < PrevMarginLeft || wadSpr.MarginTop < PrevMarginTop ||
		wadSpr.MarginRight > PrevMarginRight || wadSpr.MarginBottom > PrevMarginBottom)
		wadSpr.BiggestFrame = waddleFrame;
	
	// frames with similar pixels!! fuck that we compress shit here!
	// (11/23/25) - Apparently nevermind.... I MIGHT readd this in the future hrrrmm...
	//bool foundDupe = false;
	//for (int i = 0; i < wadSpr.Frames.Count; i++) {
	//	WaddleSpriteFrame waddleFrame2 = wadSpr.Frames[i];
	//	if (waddleFrame.Image.Compare(waddleFrame2.Image, ErrorMetric.Absolute) > 0)
	//		continue;
	//	
	//	foundDupe = true;
	//	wadSpr.FramesSequence.Add((uint)i);
	//	break;
	//}
	//
	//if (foundDupe)
	//	return;
	
	waddleFrame.SpriteSource = wadSpr;
	wadSpr.FramesSequence.Add((uint)wadSpr.Frames.Count);
	wadSpr.Frames.Add(waddleFrame);
}

public WaddleSprite CreateWaddleSpriteFromFile(string file) {
	string dirName = Path.GetDirectoryName(file);
    string spriteExtension = Path.GetExtension(file);
	string nameNoExtension = Path.GetFileNameWithoutExtension(file);
	string spriteImportedName = nameNoExtension.Replace(' ', '_'); // name sprite will be imported as
	WaddleSpriteType spriteType = GetSpriteType(spriteImportedName);
	List<string> spritesAddQueue = new();
	bool dirNameSprite = false;
	
	// unknown type? try referencing the parent directory.
	if (spriteType == WaddleSpriteType.Unknown) {
		string dirNameAsSprite = Path.GetFileName(dirName).Replace(' ', '_');
		spriteType = GetSpriteType(dirNameAsSprite);
		
		if (spriteType != WaddleSpriteType.Unknown) {
			spriteImportedName = dirNameAsSprite;
			dirNameSprite = true;
		}
	}
	
	if (spriteExtension == ".png") {
		Match numberMatch = null;
		if (spriteType == WaddleSpriteType.Sprite) 
			 numberMatch = Regex.Match(nameNoExtension, SPRITENAME_REGEX);
			
		if (numberMatch != null && numberMatch.Success) {
			string spriteFileName = numberMatch.Groups[1].Value;
            string frameCountStr = numberMatch.Groups[2].Value;
			if (!dirNameSprite) spriteImportedName = spriteFileName.Replace(' ', '_');
			
			uint frames;
            try
            {
                frames = UInt32.Parse(frameCountStr);
            }
            catch
            {
                CustomScriptMessage(file + " has an invalid strip numbering scheme. Operation aborted.", "WaddleSprite Error!", MessageWindowOwner_WaddleSprite);
				return null;
            }

            if (spriteType != WaddleSpriteType.Sprite && spriteType != WaddleSpriteType.Unknown && frames > 1)
            {
                CustomScriptMessage(file + " is not a sprite, but has more than 1 frame. Operation aborted.", "WaddleSprite Error!", MessageWindowOwner_WaddleSprite);
				return null;
            }
			
			
			string initalSpritePath = dirName + "\\" + spriteFileName;
			uint highestFrame = frames;
			uint startFrame = 0;
			
			if (File.Exists(initalSpritePath + (highestFrame + 1).ToString() + ".png")) {
			while (File.Exists(initalSpritePath + (highestFrame + 1).ToString() + ".png")) { highestFrame++; }}	
			if (!File.Exists(initalSpritePath + "0.png") && !File.Exists(initalSpritePath + "1.png")) 
			{ 
				CustomScriptMessage("Can't find \"" + spriteFileName + "0.png\"\nor\"" + spriteFileName + "1.png\"!\nOperation aborted.", "WaddleSprite Error!", MessageWindowOwner_WaddleSprite); 
				return null; 
			}
			
			startFrame = (uint)(!File.Exists(initalSpritePath + "0.png") ? 1 : 0);
			for (uint i = startFrame; i <= highestFrame; i++) 
			{
				string spriteFrame = initalSpritePath + i.ToString() + ".png";
				if (!File.Exists(spriteFrame))
				{
					CustomScriptMessage("Missing frame \"" + spriteFrame + "\"! Operation aborted."); 
					return null; 
				}
				
				spritesAddQueue.Add(spriteFrame); 
			}
			
			// get rid of that "_" at the end eugh
			// still possible to bypass by adding another right beside it but who would fucking do that
			if (spriteImportedName.EndsWith("_"))
			{ spriteImportedName = spriteImportedName.Substring(0, spriteImportedName.Length - 1); } 
		} else { spritesAddQueue.Add(file); }
	}
	
	bool definedMargins = false;
	WaddleSprite wadSpr = new() { Name = spriteImportedName, SpriteType = spriteType };
	
	if (spriteExtension == ".gif") {
		using MagickImageCollection gif = new(file, MAGICK_READSETTINGS);
		gif.Coalesce(); // makes sure some frames aren't a blank frame because of .gif file probably having optimization stuff
		
		int frames = gif.Count;
		if (spriteType != WaddleSpriteType.Sprite && spriteType != WaddleSpriteType.Unknown && frames > 1)
        {
            CustomScriptMessage(file + " is not a sprite, but has more than 1 frame. Operation aborted.", "WaddleSprite Error!", MessageWindowOwner_WaddleSprite);
			return null;
        }
		
		double totalDelaySeconds = 0;
		while (gif.Count >= 1) {
			WaddleSpriteFrame waddleFrame = new();
			MagickImage imageFrame = (MagickImage)gif[0];
			totalDelaySeconds += (imageFrame.AnimationDelay / 100.0);
			
			SetWaddleSpriteFrameImg(ref waddleFrame, imageFrame);
			AddWaddleFrameToSprite(ref wadSpr, ref waddleFrame, ref definedMargins);
			gif.RemoveAt(0); // anti auto-disposal
		}
		
		// automatically detects fps... woa
		// although does it in a fucky wucky way... thats why 
		// it's encouraged to manually set animation speed yourself
		if (frames > 1)
			wadSpr.AnimationSpeed = (float)(frames / totalDelaySeconds);
		else
			wadSpr.AnimationSpeed = 1.0f;

		gif.Dispose();
	}
	else {
		foreach (string frame in spritesAddQueue) 
		{
			WaddleSpriteFrame waddleFrame = new();
			SetWaddleSpriteFrameImg(ref waddleFrame, new MagickImage(frame, MAGICK_READSETTINGS));
			AddWaddleFrameToSprite(ref wadSpr, ref waddleFrame, ref definedMargins);
		}
	}
	
	WriteWaddleSpriteFrames(wadSpr);
	UnloadSpriteFrameImages(wadSpr); // not unloading all of them can be very lethal to the weak 'puters.
	
	return wadSpr;
}