#load "./AssetManager.csx"
#load "./GameManager.csx"
#load "./RendererManager.csx"
#load "./SDLUtils.csx"

using System.Linq;
using SDL_Sharp;

public static class FontsManager
{
	public class BitmapGlyph
	{
		public ushort Character;
		public short Shift;
		public short Offset;
		public Rect Source;
		public BitmapFont SourceFont;
	}
	
	public class BitmapFont
	{
		public List<BitmapGlyph> CharacterSet;
		public Texture FontTexture;
	}
	
	public class RenderedBitmapChar
	{
		public BitmapGlyph SourceGlyph;	
		public Rect Destination;
	}
	
	public class RenderedBitmapFont
	{
		public List<RenderedBitmapChar> RenderedCharacters;
		public BitmapFont SourceFont;
		
		public int GetWidth()
		{
			int _X = 0;
			
			foreach (RenderedBitmapChar Char in this.RenderedCharacters)
				_X = Math.Max(Char.Destination.X + Char.Destination.Width, _X);
			return _X;
		}
		
		public int GetHeight()
		{
			int _Y = 0;
			
			foreach (RenderedBitmapChar Char in this.RenderedCharacters)
				_Y = Math.Max(Char.Destination.Y + Char.Destination.Height, _Y);
			return _Y;
		}
	}
	
	public static class Effects
	{
		// So far just for simple positioning and transformation purposes other than that yeah
		public abstract class TextEffectBase
		{
			public int MinRange = -1;
			public int MaxRange = -1;
			
			// Implement things
			public abstract void ApplyEffect(ref Rect Destination, in RenderedBitmapChar Character, in uint CharacterIndex);
			public virtual void CleanupEffect(in RenderedBitmapChar Character) {}
		}
		
		public class TextWaveEffect: TextEffectBase
		{
			public float Amp = 3.0f;
			public float WaveSpeed = 1.0f;
			
			public TextWaveEffect(float _Amp, float _WaveSpeed)
			{ this.Amp = _Amp; this.WaveSpeed = _WaveSpeed; }
			
			public override void ApplyEffect(ref Rect Destination, in RenderedBitmapChar Character, in uint CharacterIndex)
			{
				Destination.Y += (int)Math.Round(Math.Sin(((float)GameManager.Time * WaveSpeed) + (float)CharacterIndex) * Amp);
			}
		}
		
		public class GrowShrinkShakeEffect: TextEffectBase
		{
			public float ScaleShake = 1.0f;
			
			public override void ApplyEffect(ref Rect Destination, in RenderedBitmapChar Character, in uint CharacterIndex)
			{
				Destination.X += GameManager.RNG.Next(-1, 1);
				Destination.Y += GameManager.RNG.Next(-1, 1);
				Destination.Width += (int)Math.Round(1.0f + (GameManager.RNG.NextSingle() * ScaleShake));
				Destination.Height += (int)Math.Round(1.0f + (GameManager.RNG.NextSingle() * ScaleShake));
			}
		}
		
		public class TextAlphaEffect: TextEffectBase
		{
			public byte Alpha = 125;
			public byte CleanupAlpha = 255;
			
			public override void ApplyEffect(ref Rect Destination, in RenderedBitmapChar Character, in uint CharacterIndex)
			{
				SDLUtils.SetTextureAlphaMod(Character.SourceGlyph.SourceFont.FontTexture, Alpha);
			}
			
			public override void CleanupEffect(in RenderedBitmapChar Character)
			{
				SDLUtils.SetTextureAlphaMod(Character.SourceGlyph.SourceFont.FontTexture, CleanupAlpha);
			}
		}
		
		
	}
	
	public static RenderedBitmapFont RenderText(BitmapFont BF, string Text, float ScaleX = 1.0f, float ScaleY = 1.0f)
	{
		RenderedBitmapFont RenderedResult = new();
		RenderedResult.RenderedCharacters = new();
		RenderedResult.SourceFont = BF;
		uint CurIdx = 0u;
		int CurX = 0;
		int CurY = 0;
		
		foreach (ushort Character in Text)
		{
			BitmapGlyph FirstGlyph = BF.CharacterSet.First(g => g.Character == Character);
			if (FirstGlyph == null)
				continue;
			
			int GlyphX = CurX - FirstGlyph.Offset;
			int GlyphY = CurY;
			float GlyphScaleX = ScaleX;
			float GlyphScaleY = ScaleY;
			RenderedBitmapChar RenderedChar = new();
			RenderedChar.SourceGlyph = FirstGlyph;
			
			RenderedChar.Destination = new(GlyphX, GlyphY, 
				(int)Math.Round((float)FirstGlyph.Source.Width * GlyphScaleX), 
				(int)Math.Round((float)FirstGlyph.Source.Height * GlyphScaleY));
			
			RenderedResult.RenderedCharacters.Add(RenderedChar);
			CurX += (int)Math.Round((float)FirstGlyph.Shift * ScaleX);
			CurIdx++;
		}
		
		return RenderedResult;
	}
}

public static partial class AssetManager
{
	public static FontsManager.BitmapFont GFX_MAIN_FONT = null;
	
	public static UndertaleFont GetFontByName(string FontName)
	{
		return DataContext.Fonts.First(Font => Font.Name.Content == FontName);
	}
	
	public static FontsManager.BitmapFont LoadBitmapFont(UndertaleFont Font, Renderer Render)
	{
		FontsManager.BitmapFont BF = new();
		BF.CharacterSet = new();
		BF.FontTexture = LoadTexturePage(Font.Texture, Render, false);
		
		foreach (UndertaleFont.Glyph Glyph in Font.Glyphs)
		{
			BF.CharacterSet.Add(new() {
				Character = Glyph.Character,
				Shift = Glyph.Shift,
				Offset = Glyph.Offset,
				Source = new(Glyph.SourceX, Glyph.SourceY, Glyph.SourceWidth, Glyph.SourceHeight),
				SourceFont = BF
			});
		}
		
		return BF;
	}
	
	public static void InitializeFont()
	{
		GFX_MAIN_FONT = LoadBitmapFont(GetFontByName("fnt_main"), RendererManager.Renderer);
	}
}

public static partial class RendererManager
{
	public static void RenderBitmapFont(
		FontsManager.RenderedBitmapFont Rendered, 
		Renderer Render, int X = 0, int Y = 0, FontsManager.Effects.TextEffectBase[] Effects = null
	)
	{
		uint CurIdx = 0u;
		
		foreach (FontsManager.RenderedBitmapChar Character in Rendered.RenderedCharacters)
		{
			Rect CharDest = new(Character.Destination.X + X, Character.Destination.Y + Y, Character.Destination.Width, Character.Destination.Height);
			List<FontsManager.Effects.TextEffectBase> ExecutedEffects = new();
			
			if (Effects != null)
			{
				foreach (FontsManager.Effects.TextEffectBase Effect in Effects)
				{
					if ((Effect.MinRange > -1 && Effect.MaxRange <= -1 && CurIdx < Effect.MinRange) ||
						(Effect.MaxRange > -1 && Effect.MinRange <= -1 && CurIdx > Effect.MaxRange) ||
						(Effect.MaxRange > -1 && Effect.MaxRange > -1 && (CurIdx < Effect.MinRange || CurIdx > Effect.MaxRange))
					) continue; // i dunno man
					
					Effect.ApplyEffect(ref CharDest, Character, CurIdx);
					ExecutedEffects.Add(Effect);
				}
			}
			
			SDL.RenderCopy(Render, Rendered.SourceFont.FontTexture, ref Character.SourceGlyph.Source, ref CharDest);
			CurIdx++;
			
			ExecutedEffects.ForEach(Effect => Effect.CleanupEffect(Character));
		}
	}
}