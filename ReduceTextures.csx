// This script deletes unreferenced embedded textures & texture pages in the game data,
// Initially a also meant to be an extension of NewTexturePacker.csx but that's probably for a future thing,
// Or who knows this might be another script I'll throw on the bin because the functionalities i proposed 
// to implement is too ambitious for my mental capacity skills to pull off ¯\_(ツ)_/¯

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using UndertaleModTool.Windows;
using UndertaleModLib.Decompiler;
using ImageMagick;

EnsureDataLoaded();

Dictionary<Type, string> _cleanup_typesDict;
Dictionary<string, List<object>> _cleanup_results;

_cleanup_typesDict = new() {{ typeof(UndertaleSprite), "SPRITES" }};
_cleanup_results = (await UndertaleResourceReferenceMethodsMap.GetUnreferencedObjects(Data, _cleanup_typesDict));

// CODE FOR CLEARING "UNREFERENCED" SPRITES;
// Decompiler kinda freaks out trying to identify sprite in the code so uhh nevermind
//
// var msg = "";
// if (_cleanup_results != null) {
// 	ConcurrentBag<UndertaleSprite> sprite_bag = new();
// 	GlobalDecompileContext decompileContext = new(Data);
// 	Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;
// 	
// 	await Task.Run(() => Parallel.ForEach(Data.Code, (UndertaleCode code) => {
// 			try
// 			{
// 				if (code is not null && code.ParentEntry is null)
// 				{
// 					var code_text = new Underanalyzer.Decompiler.DecompileContext(decompileContext, code, decompilerSettings).DecompileToString();
// 					
// 					foreach (object data in _cleanup_results["SPRITES"]) {
// 						UndertaleSprite spr = (data as UndertaleSprite);
// 						if (!code_text.Contains(spr.Name.Content, StringComparison.CurrentCulture) || sprite_bag.Contains(spr))
// 							continue;
// 						
// 						sprite_bag.Add(spr);
// 					}
// 				}
// 			}
// 			catch (Exception)
// 			{
// 				// idk twin;
// 			}
// 		})
// 	);
// 	
// 	foreach (UndertaleSprite spr in sprite_bag)
// 	{ _cleanup_results["SPRITES"].Remove(spr); }
// 	
// 	foreach (object data in _cleanup_results["SPRITES"]) 
// 	{ 
// 		msg += (data as UndertaleSprite).Name.Content + "\n";
// 		Data.Sprites.Remove((data as UndertaleSprite));
// 	}
// }
// ScriptMessage(msg);

// UNREFERENCED TEXTURE PAGES CLEANUP
// pages has to come first in order to really know which embedded textures are unused alongside it's pages
_cleanup_typesDict = new() {{ typeof(UndertaleTexturePageItem), "TXTPAGES" }};
_cleanup_results = (await UndertaleResourceReferenceMethodsMap.GetUnreferencedObjects(Data, _cleanup_typesDict));

if (_cleanup_results != null) {
foreach (object data in _cleanup_results["TXTPAGES"]) { Data.TexturePageItems.Remove(data as UndertaleTexturePageItem); }}

// UNREFERENCED EMBEDDED TEXTURES CLEANUP
_cleanup_typesDict = new() {{ typeof(UndertaleEmbeddedTexture), "EMBEDTXTS" }};
_cleanup_results = (await UndertaleResourceReferenceMethodsMap.GetUnreferencedObjects(Data, _cleanup_typesDict));

if (_cleanup_results != null) {
foreach (object data in _cleanup_results["EMBEDTXTS"]) { Data.EmbeddedTextures.Remove(data as UndertaleEmbeddedTexture); }}

ScriptMessage("Script completed!");
