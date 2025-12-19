#r "./Libraries/SDL-Sharp.dll"

using System.Runtime.InteropServices;
using SDL_Sharp.Image;
using SDL_Sharp;

// this is the part where i implement my own thingamajigs
public static class SDLUtils
{
	// not used in SpamtonScript
	[DllImport("SDL2", EntryPoint = "SDL_SetWindowPosition", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SetWindowPosition(Window window, int x, int y);
	
	[DllImport("SDL2", EntryPoint = "SDL_SetTextureAlphaMod", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool SetTextureAlphaMod(Texture texture, byte alpha);
	
	[DllImport("SDL2", EntryPoint = "SDL_RWFromMem", CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr RWFromMem(IntPtr Memory, int size);
}