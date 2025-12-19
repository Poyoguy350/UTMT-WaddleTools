#load "./AssetManager.csx"
#load "./RendererManager.csx"
#load "./ObjectManager.csx"
#load "./SDLUtils.csx"

using System;
using SDL_Sharp.Mixer;
using SDL_Sharp.Image;
using SDL_Sharp;

public static partial class GameManager
{
	public const uint FPS = 60u;
	public static uint Time = 0u;
	public static int MusicID;
	
	public static bool Running = false;
	
	//public static ObjectManager.HUD HUD = null;
	public static Random RNG = null;
	public static Music BGM;
	
	public static float RandomRangeF(float Min, float Max)
	{
		float Range = (Max - Min);
		float Sample = (float)RNG.NextDouble();
		return (Sample * Range) + Min;
	}
	
	public static int RandomRangeI(int Min, int Max)
	{
		return (int)RNG.Next(Min, Max + 1);
	}
	
	public static float ApproachF(float X, float Goal, float Step)
	{
		int Distance = Math.Sign(Goal - X);
		
		if (Distance > 0)
			return X + Step;
		else if (Distance < 0)
			return X - Step;
		else
			return X; // you're already there matey.
	}
	
	public static void PlayBGMList(int BGMId)
	{
		if (!BGM.IsNull)
			MIX.FreeMusic(BGM);
		BGM = MIX.LoadMUS(AssetManager.BGMList[BGMId]);
		
		if (BGM.IsNull)
			throw new ScriptException($"Unable to load {AssetManager.BGMList[BGMId]}!\n\"{MIX.GetError()}\"");
		
		if (MIX.PlayMusic(BGM, -1) == -1)
			throw new ScriptException($"SDL_mixer ERROR: {MIX.GetError()}");
	}
	
	public static void Initialize()
	{
		RNG = new();
		MusicID = RandomRangeI(0, AssetManager.BGMList.Length - 1);
		SDL.ShowWindow(RendererManager.Window);
		PlayBGMList(MusicID);
	}
	
	public static void Run()
	{
		Running = true;
		
		while (Running)
		{
			Time++;
			SDL.GetMouseState(out int MouseX, out int MouseY);
			ObjectManager.UpdateClickableObjects(MouseX, MouseY);
			
			while (SDL.PollEvent(out Event ev) == 1)
			{
				switch (ev.Type)
				{
					case (EventType.Quit):
					{
						Running = false;
						break;
					}
					case (EventType.MouseButtonDown):
					{
						if (ev.Button.Button == MouseButton.Left && ev.Button.State == ButtonState.Pressed)
							ObjectManager.CheckClickableObjectsInput(MouseX, MouseY);
						break;
					}
					case (EventType.KeyDown):
					{
						if (ev.Keyboard.Keysym.Sym == Keycode.R)
						{
							MusicID = (MusicID + 1) % AssetManager.BGMList.Length;
							PlayBGMList(MusicID);
						}
							
						break;
					}
				}
			}
			
			ObjectManager.UpdateObjects();
			ObjectManager.RefreshObjectsList();
			
			SDL.SetRenderDrawColor(RendererManager.Renderer, 0, 0, 0, 255);
			SDL.RenderClear(RendererManager.Renderer);
			
			ObjectManager.DrawObjects(RendererManager.Renderer);
			
			// RenderBitmapFont(GFX_DEFAULT_FONT, $"Object Count: {ObjectManager.GetObjectListCount()}", 16, 16, Renderer, 2, 2, [ new FontEffectWave((float)SCRIPT_TIME / 5.0f, 4.0f) { MinRange = 13 } ]);
			// RenderBitmapFont(GFX_DEFAULT_FONT, "Because you are cool and awesome sauce!", 16, 48, Renderer, 2, 2, [ new FontEffectGrowShrinkShake() ]);
			// RenderBitmapFont(GFX_DEFAULT_FONT, "SDL2 C# Wrapper \"SDL-Sharp\" by: GabrielFrigo4 on GitHub", 16, 96, Renderer, 1, 1, [ new FontEffectGrowShrinkShake() { ScaleShake = 0.0f } ]);
			SDL.RenderPresent(RendererManager.Renderer);
			SDL.Delay(1000u / FPS);
		}
	}
	
	public static void Quit()
	{
		MIX.FreeMusic(BGM);
		MIX.CloseAudio();
		MIX.Quit();
		
		IMG.Quit();
		SDL.Quit();
	}
}