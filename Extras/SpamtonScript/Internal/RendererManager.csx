#load "./SDLUtils.csx"

using System.Runtime.InteropServices;

using SDL_Sharp;
using SDL_Sharp.Image;
using SDL_Sharp.Mixer;

public static partial class RendererManager
{
	public static Window Window;
	public static Renderer Renderer;
	
	public static void RenderTexture(
		Renderer Render, 
		Texture Txt, 
		Rect? Source = null, 
		Rect? Destination = null, 
		float Angle = 0.0f, 
		Point? Center = null, 
		RendererFlip Flip = RendererFlip.None
	)
	{
		IntPtr SourcePtr = IntPtr.Zero;
		IntPtr DestinationPtr = IntPtr.Zero;
		IntPtr CenterPtr = IntPtr.Zero;
		
		if (Source != null)
		{
			SourcePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Rect)));
			Marshal.StructureToPtr(Source, SourcePtr, false);
		}
		
		if (Destination != null)
		{
			DestinationPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Rect)));
			Marshal.StructureToPtr(Destination, DestinationPtr, false);
		}
		
		if (Center != null)
		{
			CenterPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Point)));
			Marshal.StructureToPtr(Center, CenterPtr, false);
		}
		
		SDL.RenderCopyEx(Render, Txt, SourcePtr, DestinationPtr, (double)Angle, CenterPtr, Flip);
		
		if (Source != null) Marshal.FreeHGlobal(SourcePtr);
		if (Destination != null) Marshal.FreeHGlobal(DestinationPtr);
		if (Center != null) Marshal.FreeHGlobal(CenterPtr);
	}
	
	public static void RenderRect(
		Renderer Renderer, Rect Destination,
		byte R = 255, byte G = 255, byte B = 255, byte A = 255
	)
	{
		SDL.SetRenderDrawColor(Renderer, R, G, B, A);
		SDL.RenderFillRect(Renderer, ref Destination);
	}
	
	public static (byte, byte, byte) HSVtoRGB(float H, float S, float V)
	{
		float R = 0.0f;
		float G = 0.0f;
		float B = 0.0f;
		int I = (int)(MathF.Floor(H / 60.0f)) % 6;
		float f = H / 60.0f - MathF.Floor(H / 60.0f);
		float p = V * (1.0f - S);
		float q = V * (1.0f - f * S);
		float t = V * (1.0f - (1.0f - f) * S);
	
		switch (I) {
			case 0: R = V; G = t; B = p; break;
			case 1: R = q; G = V; B = p; break;
			case 2: R = p; G = V; B = t; break;
			case 3: R = p; G = q; B = V; break;
			case 4: R = t; G = p; B = V; break;
			case 5: R = V; G = p; B = q; break;
		}
	
		return (
			(byte)Math.Clamp(R * 255.0f, 0.0f, 255.0f),
			(byte)Math.Clamp(G * 255.0f, 0.0f, 255.0f),
			(byte)Math.Clamp(B * 255.0f, 0.0f, 255.0f)
		);
	}
	
	// I'm also just gonna shove in core SDL init functionalities here because why not
	public static void Initialize()
	{
		SDL.Init(SdlInitFlags.Video|SdlInitFlags.Audio);
		SDL.SetHint(SDL.AudioResamplingMode, "4");
		IMG.Init(ImgInitFlags.Png);
		
		Window = SDL.CreateWindow("SpamtonScript", SDL.WINDOWPOS_UNDEFINED, SDL.WINDOWPOS_UNDEFINED, 800, 600, WindowFlags.Hidden);
		
		if (Window.IsNull)
			throw new ScriptException($"Unable to create Window!\n{SDL.GetError()}");
		
		Renderer = SDL.CreateRenderer(Window, -1, RendererFlags.Accelerated);
		SDL.SetRenderDrawBlendMode(Renderer, BlendMode.Blend);
		
		if (Renderer.IsNull)
			throw new ScriptException($"Unable to create Renderer!\n{SDL.GetError()}");
		
		MIX.Init(MixInitFlags.Opus|MixInitFlags.Mod);
		MIX.OpenAudio(44100, MIX.DEFAULT_FORMAT, MIX.DEFAULT_CHANNELS, 4096);
		MIX.AllocateChannels(64);
	}
	
	public static void Quit()
	{
		SDL.DestroyRenderer(Renderer);
		SDL.DestroyWindow(Window);
	}
}