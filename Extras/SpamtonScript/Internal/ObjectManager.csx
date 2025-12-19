#load "./RendererManager.csx"
#load "./SDLUtils.csx"

using System.Linq;
using SDL_Sharp;

public static partial class ObjectManager
{	
	private static List<IObject> ObjectsList = new();
	private static List<IObject> ObjectDeletionList = new();
	private static List<IObject> ObjectAdditionList = new();

	public enum Direction: int
	{
		Left = -1,
		Right = 1
	}

	public interface IObject
	{
		public abstract void Update();
	}

	public interface ICoordinateComponent
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Width { get; set; }
		public float Height { get; set; }
		
		public bool PointInsideMe(float PointX, float PointY)
		{
			return (
				PointX >= X && PointX <= X + Width &&
				PointY >= Y && PointY <= Y + Height
			);
		}
	}
	
	public interface IPhysicsComponent: ICoordinateComponent
	{	
		public float VelocityX { get; set; }
		public float VelocityY { get; set; }
		public float Gravity { get; set; }
		
		public void Move(float? MoveX = null, float? MoveY = null, float? CustomGravity = null)
		{
			if (!MoveX.HasValue)
				MoveX = VelocityX;
			if (!MoveY.HasValue)
				MoveY = VelocityY;
			if (!CustomGravity.HasValue)
				CustomGravity = Gravity;
			
			VelocityY += CustomGravity.Value;
			X += MoveX.Value;
			Y += MoveY.Value;
		}
	}
	
	public partial interface IClickableComponent: ICoordinateComponent
	{	
		public bool Clicked { get; set; }
		public bool Pressed { get; set; }
		public bool Hovered { get; set; }
		
		public abstract void OnClick(int MouseX, int MouseY);
		
		public virtual void UpdateMouse(int MouseX, int MouseY)
		{
			this.Clicked = false;
			this.Pressed = false;
			
			if (this.PointInsideMe((float)MouseX, (float)MouseY))
				this.Hovered = true;
			else
				this.Hovered = false;
		}
	}
	
	public interface IRenderComponent
	{
		public int Layer { get; set; }
		
		public abstract void Draw(Renderer Renderer);
	}
	
	public interface IRenderHudComponent: IRenderComponent
	{	
		public abstract void DrawHUD(Renderer Renderer);
	}
	
	public partial interface IRenderSpriteComponent: ICoordinateComponent, IRenderComponent
	{
		public List<Texture> TextureFrames { get; set; }
		
		public bool Playing { get; set; }
		
		public float CurrentFrame { get; set; }
		public float AnimationSpeed { get; set; }
		
		public float OriginX { get; set; }
		public float OriginY { get; set; }
		public float ScaleX { get; set;}
		public float ScaleY { get; set;}
		public float Angle { get; set; }
		
		public byte SpriteColorModR { get; set; }
		public byte SpriteColorModG { get; set; }
		public byte SpriteColorModB { get; set; }
		
		public void GetSpriteRenderTransform(
			out Rect RectResult, out RendererFlip FlipResult, out Texture TextureResult,
			float? CustomX = null, float? CustomY = null, float? CustomScaleX = null, float? CustomScaleY = null
		)
		{
			if (TextureFrames is null || TextureFrames.Count == 0)
				throw new ScriptException("ERROR: Trying to render a SpriteComponent that has no TextureFrames!");
			int index = (int)MathF.Floor(CurrentFrame % (float)TextureFrames.Count);
			
			Texture Frame = TextureFrames[index];
			TextureResult = Frame;
			SDL.QueryTexture(Frame, out uint _, out TextureAccess _, out int SpriteWidth, out int SpriteHeight);
			
			if (!CustomScaleX.HasValue)
				CustomScaleX = ScaleX;
			if (!CustomScaleY.HasValue)
				CustomScaleY = ScaleY;
			if (!CustomX.HasValue)
				CustomX = X;
			if (!CustomY.HasValue)
				CustomY = Y;
			
			int ScaledWidth = (int)MathF.Round((float)SpriteWidth * CustomScaleX.Value);
			int ScaledHeight = (int)MathF.Round((float)SpriteHeight * CustomScaleY.Value);
			int ScaledOriginX = (int)MathF.Round((float)OriginX * CustomScaleX.Value);
			int ScaledOriginY = (int)MathF.Round((float)OriginY * CustomScaleY.Value);
			
			int X1 = (int)MathF.Round(CustomX.Value) - ScaledOriginX;
			int X2 = X1 + ScaledWidth;
			int Y1 = (int)MathF.Round(CustomY.Value) - ScaledOriginY;
			int Y2 = Y1 + ScaledHeight;
			
			FlipResult = RendererFlip.None;
			RectResult = new();
			RectResult.X = Math.Min(X1, X2);
			RectResult.Y = Math.Min(Y1, Y2);
			RectResult.Width = Math.Max(X1, X2) - RectResult.X;
			RectResult.Height = Math.Max(Y1, Y2) - RectResult.Y;
			
			if (ScaledWidth < 0) FlipResult |= RendererFlip.Horizontal;
			if (ScaledHeight < 0) FlipResult |= RendererFlip.Vertical;
		}
		
		public void DrawSprite(
			Renderer Renderer, 
			float? CustomX = null, float? CustomY = null, 
			float? CustomScaleX = null, float? CustomScaleY = null
		)
		{
			this.GetSpriteRenderTransform(
				out Rect DrawRect, out RendererFlip Flips, out Texture Frame, 
				CustomX, CustomScaleY, CustomScaleX, CustomScaleY
			);
			
			SDL.SetTextureColorMod(Frame, SpriteColorModR, SpriteColorModG, SpriteColorModB);
			RendererManager.RenderTexture(Renderer, Frame, null, DrawRect, Angle, null, Flips);
		}
		
		public void SetAnimationFrames(List<Texture> NewFrames, bool AutoPlay = false)
		{
			TextureFrames = NewFrames;
			CurrentFrame = 0.0f;
			Playing = AutoPlay;
		}
		
		public void UpdateSprite()
		{
			if (!Playing) return;
			
			CurrentFrame = (CurrentFrame + AnimationSpeed) % (float)TextureFrames.Count;
		}
	}
	
	public static void UpdateObjects()
	{
		ObjectsList.ForEach(Obj => {
			if (ObjectDeletionList.Contains(Obj))
				return;
			
			Obj.Update();
		});
	}
	
	public static void UpdateClickableObjects(int MouseX, int MouseY)
	{
		List<IClickableComponent> Clickables = ObjectsList.OfType<IClickableComponent>().ToList();
		
		Clickables.ForEach(Obj => {
			if (ObjectDeletionList.Contains((IObject)Obj))
				return;
			
			Obj.UpdateMouse(MouseX, MouseY);
		});
	}
	
	public static void CheckClickableObjectsInput(int MouseX, int MouseY)
	{
		List<IClickableComponent> Clickables = ObjectsList.OfType<IClickableComponent>().ToList();
		
		Clickables.ForEach(Obj => {
			if (ObjectDeletionList.Contains((IObject)Obj) || !Obj.Hovered)
				return;
			
			if (!Obj.Pressed)
			{
				Obj.Clicked = true;
				Obj.OnClick(MouseX, MouseY);
			}
			
			Obj.Pressed = true;
		});
	}
	
	public static void DrawObjects(Renderer Renderer)
	{
		List<IRenderComponent> Renderables = ObjectsList.OfType<IRenderComponent>().OrderBy(Obj => Obj.Layer).ToList();
		List<IRenderHudComponent> RenderablesHUD = ObjectsList.OfType<IRenderHudComponent>().OrderBy(Obj => Obj.Layer).ToList();
		
		Renderables.ForEach(Obj => {
			if (ObjectDeletionList.Contains((IObject)Obj))
				return;
			
			Obj.Draw(Renderer);
		});
		
		RenderablesHUD.ForEach(Obj => {
			if (ObjectDeletionList.Contains((IObject)Obj))
				return;
			
			Obj.DrawHUD(Renderer);
		});
	}
	
	public static void AddObject(IObject Object)
	{
		if (ObjectDeletionList.Contains(Object) || ObjectAdditionList.Contains(Object) || ObjectsList.Contains(Object))
			return;
		ObjectAdditionList.Add(Object);
	}
	
	public static void RemoveObject(IObject Object)
	{
		if (ObjectAdditionList.Contains(Object) || ObjectDeletionList.Contains(Object) || !ObjectsList.Contains(Object))
			return;
		ObjectDeletionList.Add(Object);
	}
	
	public static void RefreshObjectsList()
	{
		ObjectDeletionList.ForEach(Obj => ObjectsList.Remove(Obj));
		ObjectDeletionList.Clear();
		
		ObjectAdditionList.ForEach(Obj => ObjectsList.Add(Obj));
		ObjectAdditionList.Clear();
	}
	
	public static List<IObject> GetObjectList()
	{
		List<IObject> ListCopy = ObjectsList.ToList();
		ObjectDeletionList.ForEach(Obj => ListCopy.Remove(Obj));
		ObjectAdditionList.ForEach(Obj => ListCopy.Add(Obj));
		return ListCopy;
	}
	
	public static int GetObjectListCount()
	{
		return GetObjectList().Count;
	}
}