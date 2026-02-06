#load "./AssetManager.csx"
#load "./RendererManager.csx"
#load "./ObjectManager.csx"
#load "./GameManager.csx"
#load "./SDLUtils.csx"

using SDL_Sharp;

public static partial class ObjectManager
{
	public class Camera: IObject, ICoordinateComponent
	{
		// coordinate values will be treated as the viewport values
		public float X { get; set; } = 0.0f;
		public float Y { get; set; } = 0.0f;
		public float Width { get; set; } = 800.0f;
		public float Height { get; set; } = 600.0f;
		public float ZoomX { get; set; } = 1.0f;
		public float ZoomY { get; set; } = 1.0f;
		
		public int OffsetX { get; set; } = 0;
		public int OffsetY { get; set; } = 0;
		public int ScreenWidth { get; set; } = 800;
		public int ScreenHeight { get; set; } = 600;
		
		public int Layer { get; set; } = 0;
		
		public void DrawFromCamera(
			Texture Txt, Renderer Renderer, Rect Destination, Rect? Source = null, 
			float Angle = 0.0f, Point? Center = null, 
			RendererFlip Flip = RendererFlip.None
		)
		{
			float WidthRatio = (float)this.ScreenWidth / (this.Width / this.ZoomX);
			float HeightRatio = (float)this.ScreenHeight / (this.Height / this.ZoomY);
			Point ScreenPoint = this.WorldToScreenSpace(new((float)Destination.X, (float)Destination.Y));
			(Destination.X, Destination.Y) = (ScreenPoint.X, ScreenPoint.Y);
			Destination.Width = (int)Math.Round((float)Destination.Width * WidthRatio);
			Destination.Height = (int)Math.Round((float)Destination.Height * HeightRatio);
			
			if (Center.HasValue)
				Center = this.WorldToScreenSpace(new((float)Center.Value.X, (float)Center.Value.Y));
			
			RendererManager.RenderTexture(Renderer, Txt, Source, Destination, Angle, Center, Flip);
		}
		
		public Point WorldToScreenSpace(FPoint WorldPosition)
		{
			float WidthRatio = (float)this.ScreenWidth / (this.Width / this.ZoomX);
			float HeightRatio = (float)this.ScreenHeight / (this.Height / this.ZoomY);
			FPoint ViewPosition = new(
				WorldPosition.X - (this.X), 
				WorldPosition.Y - (this.Y)
			);
			
			return new(
				(int)Math.Round(ViewPosition.X * WidthRatio) + this.OffsetX,
				(int)Math.Round(ViewPosition.Y * HeightRatio) + this.OffsetY
			);
		}
		
		public FPoint ScreenToWorldSpace(Point ScreenPosition)
		{
			float WidthRatio = (float)this.ScreenWidth / (this.Width / this.ZoomX);
			float HeightRatio = (float)this.ScreenHeight / (this.Height / this.ZoomY);
			FPoint ViewPosition = new(
				((float)ScreenPosition.X - this.OffsetX) / WidthRatio, 
				((float)ScreenPosition.Y - this.OffsetY) / WidthRatio
			);
			
			return new(ViewPosition.X + this.X, ViewPosition.Y + this.Y);
		}
		
		public virtual void Update() {}
	}
	
	public partial interface IRenderSpriteComponent: ICoordinateComponent, IRenderComponent
	{
		public void DrawSprite(
			Renderer Renderer, Camera CamObj, 
			float? CustomX = null, float? CustomY = null, 
			float? CustomScaleX = null, float? CustomScaleY = null
		)
		{
			this.GetSpriteRenderTransform(
				out Rect DrawRect, out RendererFlip Flips, out Texture Frame, 
				CustomX, CustomScaleY, CustomScaleX, CustomScaleY
			);
			
			SDL.SetTextureColorMod(Frame, SpriteColorModR, SpriteColorModG, SpriteColorModB);
			CamObj.DrawFromCamera(Frame, Renderer, DrawRect, null, Angle, null, Flips);
		}
	}
}