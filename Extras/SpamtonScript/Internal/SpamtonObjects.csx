#load "./ObjectManager.csx"
#load "./CameraObject.csx"
#load "./FontsManager.csx"

using System.Numerics;
using SDL_Sharp.Mixer;
using SDL_Sharp.Image;
using SDL_Sharp;

public static partial class ObjectManager
{
	public partial interface ICameraClickableComponent: IClickableComponent
	{	
		void IClickableComponent.UpdateMouse(int MouseX, int MouseY)
		{
			FPoint TransformedMouse = GameManager.CameraObject.ScreenToWorldSpace(new(MouseX, MouseY));
			this.Clicked = false;
			this.Pressed = false;
			
			if (this.PointInsideMe(TransformedMouse.X, TransformedMouse.Y))
				this.Hovered = true;
			else
				this.Hovered = false;
		}
	}
	
	public class SpamtonObject: IObject, ICoordinateComponent, IPhysicsComponent, ICameraClickableComponent, IRenderSpriteComponent
	{	
		public Direction Direction { get; set; } = Direction.Right; 
	
		public float X { get; set; } = 0.0f;
		public float Y { get; set; } = 0.0f;
		public float Width { get; set; } = 48.0f;
		public float Height { get; set; } = 80.0f;
		
		public float VelocityX { get; set; } = 0.0f;
		public float VelocityY { get; set; } = 0.0f;
		public float Gravity { get; set; } = 0.0f;
		
		public float CurrentFrame { get; set; } = 0.0f;
		public float AnimationSpeed { get; set; } = 0.25f;
		
		public float OriginX { get; set; } = 16.0f;
		public float OriginY { get; set; } = 8.0f;
		public float ScaleX { get; set;} = -2.5f;
		public float ScaleY { get; set;} = 2.5f;
		public float Angle { get; set; } = 0.0f;
		
		public float MoveSpeedX { get; set; } = 1.0f;
		public float MoveSpeedY { get; set; } = 1.0f;
		
		public bool Clicked { get; set; } = false;
		public bool Pressed { get; set; } = false;
		public bool Hovered { get; set; } = false;
		
		public byte SpriteColorModR { get; set; } = 255;
		public byte SpriteColorModG { get; set; } = 255;
		public byte SpriteColorModB { get; set; } = 255;
		
		public bool Playing { get; set; } = true;
		
		public int Layer { get; set; } = 0;
		
		public int MoveChangeTicks { get; set; } = 30;
		
		public List<Texture> TextureFrames { get; set; } = AssetManager.GFX_SPAMTON_JUMPER;
		
		public static SpamtonObject CreateSpamton(bool RandomSize = true)
		{
			SpamtonObject NewSpamton = new();
			
			if (RandomSize)
			{
				float SizeMod = GameManager.RandomRangeF(0.64f, 1.75f);
				NewSpamton.Width *= SizeMod;
				NewSpamton.Height *= SizeMod;
				NewSpamton.ScaleX *= SizeMod;
				NewSpamton.ScaleY *= SizeMod;
			}
			
			return NewSpamton;
		}
		
		public void Update()
		{
			this.VelocityX = GameManager.ApproachF(this.VelocityX, this.MoveSpeedX, 0.12f);
			this.VelocityY = GameManager.ApproachF(this.VelocityY, this.MoveSpeedY, 0.12f);
			
			if (this.MoveChangeTicks <= 0)
			{
				if (GameManager.RandomRangeI(0, 100) <= 25)
					this.Direction = (Direction)((int)this.Direction * -1);
				
				this.MoveChangeTicks = (int)GameManager.FPS * (GameManager.RandomRangeI(500, 3200) / 1000);
				this.MoveSpeedX = GameManager.RandomRangeF(0.12f, 2.4f) * (float)this.Direction;
				this.MoveSpeedY = GameManager.RandomRangeF(-4.0f, 4.0f);
			}
			else
				this.MoveChangeTicks--;
			
			(this as IPhysicsComponent).Move();
			(this as IRenderSpriteComponent).UpdateSprite();
			
			float XBounds = Math.Clamp(this.X + (this.Width / 2.0f), -250.0f, 1050.0f);
			float YBounds = Math.Clamp(this.Y + (this.Height / 2.0f), 0.0f, 600.0f);
			this.Y = YBounds - (this.Height / 2.0f);
			if ((YBounds == 0.0f && this.VelocityY < 0.0f) || (YBounds == 600.0f && this.VelocityY > 0.0f))
			{
				this.VelocityY *= -1.0f;
				if ((this.MoveSpeedY < 0 && YBounds == 0.0f) || (this.MoveSpeedY > 0 && YBounds == 600.0f))
				{
					this.MoveSpeedY *= -1.0f;
					this.MoveChangeTicks += GameManager.RandomRangeI(-100, 500);
				}
			}
			
			if (XBounds == -250.0f || XBounds == 1050.0f)
				RemoveObject(this);
		}
		
		public void Draw(Renderer Renderer)
		{
			(this as IRenderSpriteComponent).DrawSprite(
				Renderer, GameManager.CameraObject, 
				(X + Width / 2.0f), (Y + Height / 2.0f), 
				this.ScaleX * (float)this.Direction
			);
			
			//Rect Hitbox = new((int)X, (int)Y, (int)Width, (int)Height);
			//SDL.SetRenderDrawColor(Renderer, 255, 0, 0, 100);
			//SDL.RenderFillRect(Renderer, ref Hitbox);
		}
		
		public void OnClick(int MouseX, int MouseY)
		{
			float DebrisStrength = GameManager.RandomRangeF(0.05f, 0.75f);
			FPoint TransformedMouse = GameManager.CameraObject.ScreenToWorldSpace(new(MouseX, MouseY));
			SpamtonDebris Debris = SpamtonDebris.CreateDebrisFromSpamton(this, (Direction)Math.Sign(TransformedMouse.X - (this.X + (this.Width / 2.0f))));
			Debris.VelocityX = (Debris.X - TransformedMouse.X) * DebrisStrength;
			Debris.VelocityY = ((Debris.Y - TransformedMouse.Y) * DebrisStrength) - 6.4f;
			
			GameManager.Score += 15;
			GameManager.ComboTicks = 3 * (int)GameManager.FPS;
			GameManager.Combo++;
			
			GameManager.HUD.ComboFlash = 255;
			GameManager.CameraObject.TargetResetTicks = (int)GameManager.FPS;
			GameManager.CameraObject.TargetX = Math.Clamp(this.X, 390.0f, 410.0f);
			GameManager.CameraObject.TargetY = Math.Clamp(this.Y, 290.0f, 310.0f);
			GameManager.CameraObject.TargetZoom = 1.1f;
			
			AddObject(Explosion.QuickExplosion(this.X, this.Y, this.ScaleX, this.ScaleY));
			AddObject(Debris);
			RemoveObject(this);
			
			MIX.PlayChannel(-1, AssetManager.SFX_EXPLOSION, 0);
		}
	}
	
	public class SpamtonDebris: IObject, ICoordinateComponent, IPhysicsComponent, IRenderSpriteComponent
	{
		public float X { get; set; } = 0.0f;
		public float Y { get; set; } = 0.0f;
		public float Width { get; set; } = 0.0f;
		public float Height { get; set; } = 0.0f;
		
		public float VelocityX { get; set; } = 0.0f;
		public float VelocityY { get; set; } = 0.0f;
		public float Gravity { get; set; } = 0.5f;
		
		public float CurrentFrame { get; set; } = 0.0f;
		public float AnimationSpeed { get; set; } = 0.25f;
		
		public float OriginX { get; set; } = 15.0f;
		public float OriginY { get; set; } = 15.0f;
		public float ScaleX { get; set;} = 1.0f;
		public float ScaleY { get; set;} = 1.0f;
		public float Angle { get; set; } = 0.0f;
		
		public byte SpriteColorModR { get; set; } = 50;
		public byte SpriteColorModG { get; set; } = 50;
		public byte SpriteColorModB { get; set; } = 50;
		
		public int Layer { get; set; } = -1;
		
		public bool Playing { get; set; } = true;
		
		public List<Texture> TextureFrames { get; set; } = AssetManager.GFX_SPAMTON_HURT;
		
		public static SpamtonDebris CreateDebrisFromSpamton(SpamtonObject Spamton, Direction? CustomDirection = null)
		{
			if (!CustomDirection.HasValue)
				CustomDirection = Spamton.Direction;
			
			SpamtonDebris Debris = new();
			Debris.X = Spamton.X + (Spamton.Width / 2.0f);
			Debris.Y = Spamton.Y + (Spamton.Height / 2.0f);
			Debris.ScaleX = Spamton.ScaleX * (float)CustomDirection.Value;
			Debris.ScaleY = Spamton.ScaleY;
			
			return Debris;
		}
		
		public void Update()
		{
			this.Angle += this.VelocityX;
			(this as IPhysicsComponent).Move();
			
			
			if (this.Y > 1000.0f)
				RemoveObject(this);
		}
		
		public void Draw(Renderer Renderer)
		{
			(this as IRenderSpriteComponent).DrawSprite(Renderer, GameManager.CameraObject);
		}
	}
	
	public class Explosion: IObject, ICoordinateComponent, IRenderSpriteComponent
	{
		public float X { get; set; } = 0.0f;
		public float Y { get; set; } = 0.0f;
		public float Width { get; set; } = 0.0f;
		public float Height { get; set; } = 0.0f;
		
		public int Layer { get; set; } = 1;
		
		public float CurrentFrame { get; set; } = 0.0f;
		public float AnimationSpeed { get; set; } = 0.25f;
		
		public float OriginX { get; set; } = 50.0f;
		public float OriginY { get; set; } = 40.0f;
		public float ScaleX { get; set;} = 1.0f;
		public float ScaleY { get; set;} = 1.0f;
		public float Angle { get; set; } = 0.0f;
		
		public byte SpriteColorModR { get; set; } = 255;
		public byte SpriteColorModG { get; set; } = 255;
		public byte SpriteColorModB { get; set; } = 255;
		
		public bool Playing { get; set; } = true;
		
		public List<Texture> TextureFrames { get; set; } = AssetManager.GFX_EXPLOSION;
		
		public static Explosion QuickExplosion(float CustomX, float CustomY, float CustomScaleX = 1.0f, float CustomScaleY = 0.0f)
		{
			Explosion NewExplosion = new()
			{
				X = CustomX,
				Y = CustomY,
				ScaleX = CustomScaleX,
				ScaleY = CustomScaleY
			};
			
			return NewExplosion;
		}
		
		public void Update()
		{
			float PrevFrame = this.CurrentFrame;
			(this as IRenderSpriteComponent).UpdateSprite();
			
			if (PrevFrame < this.CurrentFrame)
				return;
			
			RemoveObject(this);
		}
		
		public void Draw(Renderer Renderer)
		{
			(this as IRenderSpriteComponent).DrawSprite(Renderer, GameManager.CameraObject);
		}
	}
	
	public class SpamtonBackground: IObject, ICoordinateComponent, IRenderSpriteComponent
	{
		public float X { get; set; } = 0.0f;
		public float Y { get; set; } = 0.0f;
		public float Width { get; set; } = 0.0f;
		public float Height { get; set; } = 0.0f;
		
		public int Layer { get; set; } = -2;
		
		public float CurrentFrame { get; set; } = 0.0f;
		public float AnimationSpeed { get; set; } = 0.0f;
		
		public float OriginX { get; set; } = 0.0f;
		public float OriginY { get; set; } = 0.0f;
		public float ScaleX { get; set;} = 1.0f;
		public float ScaleY { get; set;} = 1.0f;
		public float Angle { get; set; } = 0.0f;
		public float HueShift { get; set; } = 0.0f;
		public const float Brightness = 0.5f;
		
		public byte SpriteColorModR { get; set; } = 255;
		public byte SpriteColorModG { get; set; } = 255;
		public byte SpriteColorModB { get; set; } = 255;
		
		public bool Playing { get; set; } = false;
		
		public List<Texture> TextureFrames { get; set; } = new() {AssetManager.GFX_SPAMTON_BG};

		public void Update() {
			HueShift = (HueShift + 1.2f) % 360.0f;
			(SpriteColorModR, SpriteColorModG, SpriteColorModB) = RendererManager.HSVtoRGB(HueShift, 1.0f, 1.0f);
			SpriteColorModR = (byte)Math.Round((float)SpriteColorModR * Brightness);
			SpriteColorModG = (byte)Math.Round((float)SpriteColorModG * Brightness);
			SpriteColorModB = (byte)Math.Round((float)SpriteColorModB * Brightness);
		}
		
		public void Draw(Renderer Renderer)
		{
			(this as IRenderSpriteComponent).GetSpriteRenderTransform(out Rect DrawRect, out RendererFlip _, out Texture _);
			this.ScaleX = GameManager.CameraObject.Width / DrawRect.Width;
			this.ScaleY = GameManager.CameraObject.Height / DrawRect.Height;
			
			List<FPoint> Directions = new() {
				new(-1.0f, -1.0f), new(0.0f, -1.0f), new(1.0f, -1.0f),
				new(-1.0f, 0.0f), new(0.0f, 0.0f), new(1.0f, 0.0f),
				new(-1.0f, 1.0f), new(0.0f, 1.0f), new(1.0f, 1.0f),
			};
			
			Directions.ForEach(Dir => {
				this.X = GameManager.CameraObject.Width * Dir.X;
				this.Y = GameManager.CameraObject.Height * Dir.Y;
				(this as IRenderSpriteComponent).DrawSprite(Renderer, GameManager.CameraObject);
			});
			
			this.ScaleX = 1.0f;
			this.ScaleY = 1.0f;
		}
	}
	
	public class SpamtonSpawner: IObject, ICoordinateComponent
	{
		public float X { get; set; } = 0.0f;
		public float Y { get; set; } = 16.0f;
		public float Width { get; set; } = 0.0f;
		public float Height { get; set; } = 584.0f;
		
		public int SpawnerTicks { get; set; } = 30;
		
		public Direction SpawnDirection { get; set; } = Direction.Right; 
		
		public void Update()
		{
			if (this.SpawnerTicks <= 0)
			{
				var NewSpamton = SpamtonObject.CreateSpamton();
				NewSpamton.X = this.X - ((((SpawnDirection == Direction.Right) ? NewSpamton.Width : 0) + 8) * (float)this.SpawnDirection); // +8 cuz idk :3
				NewSpamton.Y = this.Y + GameManager.RandomRangeF(0.0f, this.Height) - (NewSpamton.Height / 2.0f);
				NewSpamton.VelocityX = (float)this.SpawnDirection * GameManager.RandomRangeF(4.8f, 8.0f);
				NewSpamton.Direction = this.SpawnDirection;
				AddObject(NewSpamton);
				
				int Channel = MIX.PlayChannel(-1, AssetManager.SFX_SPAMTONLAUGH, 0);
				
				if (Channel != -1)
				{
					int CenterDistance = Math.Sign(X - 400.0f);
					
					if (CenterDistance > 0)
						MIX.SetPanning(Channel, 128, 255);
					else
						MIX.SetPanning(Channel, 255, 128);
				}
				
				this.SpawnerTicks = GameManager.RandomRangeI(-10, 120);
			}
			else
				this.SpawnerTicks--;
		}
	}
	
	public class HUD: IObject, IRenderHudComponent
	{
		public int Layer { get; set; } = 0;
		
		public int ClickSpamtonTextWidth { get; set; } = 0;
		public int ComboFlash { get; set; } = 0;
		
		public FontsManager.RenderedBitmapFont ClickSpamtonText { get; set; } = null;
		public FontsManager.RenderedBitmapFont WatermarkText { get; set; } = null;
		
		public HUD()
		{
			this.ClickSpamtonText = FontsManager.RenderText(AssetManager.GFX_MAIN_FONT, "CLICK THE SPAMTONS!!!", 2.8f, 2.8f);
			this.WatermarkText = FontsManager.RenderText(AssetManager.GFX_MAIN_FONT, @"SDL2 to CSharp Wrapper by ""GabrielFrigo4"" named ""SDL-Sharp"" on GitHub", 1.6f, 1.6f);
			this.ClickSpamtonTextWidth = this.ClickSpamtonText.GetWidth();
		}
		
		public void Draw(Renderer Renderer) {}
		public void DrawHUD(Renderer Renderer)
		{
			FontsManager.RenderedBitmapFont ScoreCounterText = FontsManager.RenderText(
				AssetManager.GFX_MAIN_FONT, $"SCORE: {GameManager.Score}", 1.6f, 1.6f
			);
			
			FontsManager.RenderedBitmapFont HighComboCounterText = FontsManager.RenderText(
				AssetManager.GFX_MAIN_FONT, $"HIGHEST COMBO: {GameManager.HighestCombo}", 1.6f, 1.6f
			);
			
			FontsManager.RenderedBitmapFont ObjectCounterText = FontsManager.RenderText(
				AssetManager.GFX_MAIN_FONT, $"OBJECT COUNT: {ObjectManager.GetObjectListCount()}", 1.6f, 1.6f
			);
			
			RendererManager.RenderBitmapFont(this.ClickSpamtonText, Renderer, 
				400 - (int)Math.Round((float)this.ClickSpamtonTextWidth / 2.0f), 10, new FontsManager.Effects.TextEffectBase[] { 
				new FontsManager.Effects.TextWaveEffect(6.0f, 0.12f),
				new FontsManager.Effects.TextAlphaEffect()
			});
			
			RendererManager.RenderBitmapFont(ScoreCounterText, Renderer, 10, 10);
			RendererManager.RenderBitmapFont(HighComboCounterText, Renderer, 10, 32);
			RendererManager.RenderBitmapFont(ObjectCounterText, Renderer, 10, 532, new[] { new FontsManager.Effects.TextAlphaEffect() });
			RendererManager.RenderBitmapFont(WatermarkText, Renderer, 10, 564, new[] { new FontsManager.Effects.TextAlphaEffect() });
			
			if (ComboFlash > 0)
			{
				ComboFlash -= 10;
				
				if (ComboFlash <= 0)
					ComboFlash = 0;
			}
			
			if (GameManager.Combo > 0)
			{
				float Progress = (float)GameManager.ComboTicks / (3.0f * (float)GameManager.FPS);
				int Size = (int)Math.Round(Progress * 100.0f);
				
				FontsManager.RenderedBitmapFont ComboCounterText = FontsManager.RenderText(
					AssetManager.GFX_MAIN_FONT, $"COMBO: {GameManager.Combo}", 1.6f, 1.6f
				);
				
				RendererManager.RenderBitmapFont(ComboCounterText, Renderer, 10, 64, 
					new[] { new FontsManager.Effects.GrowShrinkShakeEffect() { 
						ScaleShake = Single.Lerp(0.0f, 1.2f, (float)GameManager.Combo / 10.0f)}});
						
				RendererManager.RenderRect(Renderer, new(10, 96, 100, 16), 0, 0, 0, 255);
				RendererManager.RenderRect(Renderer, new(10, 96, Size, 16), (byte)ComboFlash, (byte)ComboFlash, 255, 255);
			}
		}
		
		public void Update()
		{
			if (GameManager.HUD != this)
			{
				RemoveObject(this);
				return;
			}
			
			if (GameManager.ComboTicks > 0)
			{
				GameManager.ComboTicks--;
				
				if (GameManager.ComboTicks <= 0)
				{
					MIX.PlayChannel(-1, AssetManager.SFX_QUEENHOOT, 0);
					GameManager.HighestCombo = Math.Max(GameManager.Combo, GameManager.HighestCombo);
					GameManager.Score += (5 * GameManager.Combo);
					GameManager.ComboTicks = 0;
					GameManager.Combo = 0;
				}
			}
		}
	}
	
	public class SpamtonCamera: Camera
	{
		public float TargetX { get; set; } = 400.0f;
		public float TargetY { get; set; } = 300.0f;
		public float TargetZoom { get; set; } = 1.0f;
		
		public int TargetResetTicks { get; set; } = 0;
		
		public override void Update()
		{
			this.X = Single.Lerp(this.X, this.TargetX, 0.25f);
			this.Y = Single.Lerp(this.Y, this.TargetY, 0.25f);
			this.ZoomX = Single.Lerp(this.ZoomX, this.TargetZoom, 0.05f);
			this.ZoomY = Single.Lerp(this.ZoomY, this.TargetZoom, 0.05f);
			
			if (this.TargetResetTicks > 0)
			{
				this.X += GameManager.RandomRangeF(-4.0f, 4.0f);
				this.Y += GameManager.RandomRangeF(-4.0f, 4.0f);
				this.TargetResetTicks--;
			}
			else
			{
				this.TargetResetTicks = 0;
				this.TargetX = 400.0f;
				this.TargetY = 300.0f;
				this.TargetZoom  = 1.0f;
			}
		}
	}
}

public static partial class GameManager
{
	public static int Score = 0;
	public static int Combo = 0;
	public static int ComboTicks = 0;
	public static int HighestCombo = 0;
	
	public static ObjectManager.HUD HUD = null;
	public static ObjectManager.SpamtonBackground Background = null;
	public static ObjectManager.SpamtonCamera CameraObject = null;
	public static ObjectManager.SpamtonSpawner SpamtonSpawnerL = null;
	public static ObjectManager.SpamtonSpawner SpamtonSpawnerR = null;
	
	public static void InitializeObjects()
	{
		HUD = new();
		Background = new();
		CameraObject = new() { X = 400.0f, Y = 300.0f, OffsetX = 400, OffsetY = 300 };
		SpamtonSpawnerL = new();
		SpamtonSpawnerR = new() { X = 800, SpawnDirection = ObjectManager.Direction.Left };
		
		ObjectManager.AddObject(HUD);
		ObjectManager.AddObject(Background);
		ObjectManager.AddObject(CameraObject);
		ObjectManager.AddObject(SpamtonSpawnerL);
		ObjectManager.AddObject(SpamtonSpawnerR);
	}
}