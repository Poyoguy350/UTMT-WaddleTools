#load "./SDLUtils.csx"

using System.Runtime.InteropServices;
using System.Linq;

using UndertaleModLib.Util;
using ImageMagick;

using SDL_Sharp.Mixer;
using SDL_Sharp.Image;
using SDL_Sharp;

public static partial class AssetManager
{
	public static List<Texture> GFX_SPAMTON_JUMPER = null;
	public static List<Texture> GFX_SPAMTON_HURT = null;
	public static List<Texture> GFX_EXPLOSION = null;
	public static Texture GFX_SPAMTON_BG;
	
	public static PChunk SFX_EXPLOSION = null;
	public static PChunk SFX_SPAMTONLAUGH = null;
	public static PChunk SFX_QUEENHOOT = null;
	
	public static UndertaleData DataContext = null;
	public static string DataContextFilePath = null;
	
	public static string[] BGMList = null;
	
	// I dunno what i'm doing with this but it works idgaf
	public static Texture LoadTexturePage(UndertaleTexturePageItem TexturePage, Renderer Render, bool Padding)
	{
		Texture SDLTexture = new();
		TextureWorker Worker = new();
		IMagickImage<byte> TextureMagick = Worker.GetTextureFor(TexturePage, TexturePage.Name.Content, Padding);
		
		byte[] PixelsArray = TextureMagick.ToByteArray(MagickFormat.Bgra);
		GCHandle PinnedPixels = GCHandle.Alloc(PixelsArray, GCHandleType.Pinned);
		IntPtr PixelsPtr = PinnedPixels.AddrOfPinnedObject();
		
		int Depth = 32;
		int Pitch = (int)TextureMagick.Width * (Depth / 8);
		
		SDL.CreateRGBSurfaceFrom(
			PixelsPtr, 
			(int)TextureMagick.Width, 
			(int)TextureMagick.Height, 
			Depth, 
			Pitch, 
			0x00ff0000, // R mask
			0x0000ff00, // G mask
			0x000000ff, // B mask
			0xff000000,  // A mask
			out PSurface TextureSurf
		);
	
		Texture FinalTexture = SDL.CreateTextureFromSurface(Render, TextureSurf);
		SDL.FreeSurface(TextureSurf);
		PinnedPixels.Free();
		Worker.Dispose();
	
		return FinalTexture;
	}
	
	public static Texture LoadTextureFromSpriteFrame(UndertaleSprite Sprite, int index, Renderer Render, bool Padding = true)
	{
		return LoadTexturePage(Sprite.Textures[index].Texture, Render, Padding);
	}
	
	// wasted my holy two hours for this
	public static List<Texture> LoadTexturesSprite(UndertaleSprite Sprite, Renderer Render, bool Padding = true)
	{
		List<Texture> TextureList = new();
		for (int i = 0; i < Sprite.Textures.Count; i++) 
		{ TextureList.Add(LoadTextureFromSpriteFrame(Sprite, i, Render, Padding)); }
		
		return TextureList;
	}
	
	public static PChunk LoadByteArrayAsChunk(byte[] Sound)
	{
		GCHandle PinnedSound = GCHandle.Alloc(Sound, GCHandleType.Pinned);
		IntPtr SoundPtr = PinnedSound.AddrOfPinnedObject();
		RWops Memory = SDLUtils.RWFromMem(SoundPtr, Sound.Length);
		
		PChunk LoadedChunk;
		MIX.LoadWAV_RW(Memory, 1, out LoadedChunk);
		PinnedSound.Free();
		
		return LoadedChunk;
	}
	
	public static UndertaleEmbeddedAudio GetEmbeddedAudioFromSound(UndertaleSound Sound)
	{
		if (Sound.GroupID != 0 && Sound.AudioID != -1)
		{
			try
			{
				string RelativePath;
				if (Sound.AudioGroup is UndertaleAudioGroup { Path.Content: string customRelativePath })
				{
					RelativePath = customRelativePath;
				}
				else
				{
					RelativePath = $"audiogroup{Sound.GroupID}.dat";
				}
				
				string AudioGroupPath = Path.Combine(Path.GetDirectoryName(DataContextFilePath), RelativePath);
				
				if (File.Exists(AudioGroupPath))
				{
					using FileStream AudioStream = new(AudioGroupPath, FileMode.Open, FileAccess.Read);
					UndertaleData AudioGroupData = UndertaleIO.Read(AudioStream, (Warning, _) =>
					{
						throw new ScriptException(Warning);
					});
	
					return AudioGroupData.EmbeddedAudio[Sound.AudioID];
				}
				else
					throw new ScriptException("Failed to find audio group file.");
			}
			catch (Exception Ex)
			{
				throw new ScriptException($"FAILED TO LOAD UNDERTALESOUND: {Ex.Message}");
			}
		}
		
		return Sound.AudioFile;
	}
	
	public static UndertaleSprite GetSpriteByName(string SpriteName)
	{
		return DataContext.Sprites.First(Sprite => Sprite.Name.Content == SpriteName);
	}
	
	public static UndertaleSound GetSoundByName(string SoundName)
	{
		return DataContext.Sounds.First(Sound => Sound.Name.Content == SoundName);
	}
	
	public static void Initialize()
	{
		GFX_SPAMTON_JUMPER = LoadTexturesSprite(GetSpriteByName("spr_spamton_jumper"), RendererManager.Renderer, true);
		GFX_SPAMTON_HURT = LoadTexturesSprite(GetSpriteByName("spr_spamton_hurt"), RendererManager.Renderer, true);	
		GFX_EXPLOSION = LoadTexturesSprite(GetSpriteByName("spr_realisticexplosion"), RendererManager.Renderer, true);		
		GFX_SPAMTON_BG = LoadTextureFromSpriteFrame(GetSpriteByName("spr_shop_spamton_bg_battle"), 0, RendererManager.Renderer, true);
		
		SFX_SPAMTONLAUGH = LoadByteArrayAsChunk(GetEmbeddedAudioFromSound(GetSoundByName("snd_spamton_laugh")).Data);
		SFX_EXPLOSION = LoadByteArrayAsChunk(GetEmbeddedAudioFromSound(GetSoundByName("snd_badexplosion")).Data);
		SFX_QUEENHOOT = LoadByteArrayAsChunk(GetEmbeddedAudioFromSound(GetSoundByName("snd_queen_hoot_0")).Data);
		
		MIX.VolumeChunk(AssetManager.SFX_SPAMTONLAUGH, 100);
		MIX.VolumeChunk(AssetManager.SFX_EXPLOSION, 45);
	}
}