#load "./Internal/AssetManager.csx"
#load "./Internal/FontsManager.csx"
#load "./Internal/RendererManager.csx"
#load "./Internal/GameManager.csx"
#load "./Internal/SpamtonObjects.csx"
#load "./Internal/CameraObject.csx"

// Fucking around .dlls
// SDL2 to CSharp Wrapper by GabrielFrigo4 named "SDL-Sharp" on GitHub
// https://github.com/GabrielFrigo4/SDL-Sharp

using System.Linq;

EnsureDataLoaded();

List<string> BGMList = new() {
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "KEYGEN.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "spamton_battle.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "spamton_dance.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "spamton_happy.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "spamton_meeting.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "spamton_neo_after.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "spamton_neo_mix_ex_wip.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "battle.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "noelle.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "noelle_ferriswheel.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "noelle_house_wip.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "noelle_school.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "joker.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "minigame_kart.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "tenna_battle.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "knight.ogg"),
	Path.Combine(Directory.GetParent(Path.GetDirectoryName(FilePath)).FullName, "mus", "nightmare_boss_heavy.ogg")
};

AssetManager.BGMList = BGMList.Where(MusFile => File.Exists(MusFile)).ToArray();

if (AssetManager.BGMList.Length == 0)
{
	ScriptMessage("Unable to load any DELTARUNE BGM *.oggs!\nAre you sure you loaded DELTARUNE Chapter 2?");
	return;
}

AssetManager.DataContext = Data;
AssetManager.DataContextFilePath = FilePath;

RendererManager.Initialize();
AssetManager.Initialize();
AssetManager.InitializeFont();
GameManager.Initialize();
GameManager.InitializeObjects();

// 
// 
//SDL.SetTextureColorMod(GFX_SPAMTON_BG, (byte)(R * Brightness), (byte)(G * Brightness), (byte)(B * Brightness));
//RenderTexture(Renderer, GFX_SPAMTON_BG);

// ObjectManager.AddObject(GameManager.HUD);
// ObjectManager.AddObject(new ObjectManager.SpamtonSpawner());
// ObjectManager.AddObject(new ObjectManager.SpamtonSpawner() { X = 800, SpawnDirection = ObjectManager.Direction.Left });

GameManager.Run();

RendererManager.Quit();
GameManager.Quit();