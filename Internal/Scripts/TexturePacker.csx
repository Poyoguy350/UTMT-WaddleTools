#load "./WaddleSprite.csx"

// rewrote this TexturePacker by Samuel Roy
// https://github.com/mfascia/TexturePacker/tree/master

using UndertaleModLib.Util;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using ImageMagick;
using ImageMagick.Drawing;

public enum PackerSplitType
{
    Horizontal,
    Vertical,
}

public enum PackerBestFitHeuristic
{
	Area,
	MaxOneAxis
}

public class PackerNode
{
	public int X = 0;
	public int Y = 0;
	public int Width = 0;
	public int Height = 0;
	
	public PackerSplitType SplitType;
	public WaddleSpriteFrame Source;
}

public class PackerAtlas
{
	public int Width = 0;
	public int Height = 0;
	
	public List<PackerNode> Nodes;
	
	public MagickImage CreateImage()
	{	
		MagickImage AtlasImage = new(MagickColors.Transparent, (uint)Width, (uint)Height);
		
        foreach (PackerNode Node in Nodes)
        {
			if (Node.Source != null)
            {
                using IMagickImage<byte> ResizedSourceImage = TextureWorker.ResizeImage(Node.Source.Image, (int)Node.Width, (int)Node.Height);
                AtlasImage.Composite(ResizedSourceImage, (int)Node.X, (int)Node.Y, CompositeOperator.Copy);
            }
        }
		
        return AtlasImage;
	}
}

public class Packer
{
	public const int Padding = 2;
	public const int AtlasSize = 2048;
	public const int SizeStep = (AtlasSize / 4);
	
	public List<PackerAtlas> OutputAtlasses;
	public PackerBestFitHeuristic FitHeuristic;
	
	public WaddleSpriteFrame FindBestFitForNode(PackerNode Node, List<WaddleSpriteFrame> Frames)
	{
		WaddleSpriteFrame Best = null;
		
		float NodeArea = (float)Node.Width * (float)Node.Height;
		float MaxCriteria = 0.0f;
		
		foreach (WaddleSpriteFrame Frame in Frames)
		{
			int FrameWidth = (int)Frame.TargetWidth;
			int FrameHeight = (int)Frame.TargetHeight;
			
			if (FrameWidth > Node.Width || FrameHeight > Node.Height)
				continue;
			
			switch (FitHeuristic)
			{
				case (PackerBestFitHeuristic.MaxOneAxis):
				{
					float WidthRatio = (float)FrameWidth / (float)Node.Width;
					float HeightRatio = (float)FrameHeight / (float)Node.Height;
					float Ratio = WidthRatio > HeightRatio ? WidthRatio : HeightRatio;
					
					if (Ratio > MaxCriteria)
					{
						MaxCriteria = Ratio;
						Best = Frame;
					}
					
					break;
				}
				case (PackerBestFitHeuristic.Area):
				{
					float FrameArea = (float)FrameWidth * (float)FrameHeight;
					float Coverage = FrameArea / NodeArea;
					
					if (Coverage > MaxCriteria)
					{
						MaxCriteria = Coverage;
						Best = Frame;
					}
					
					break;
				}
			}
		}
		
		return Best;
	}
	
	public void HorizontalSplit(PackerNode SplittingNode, int Width, int Height, List<PackerNode> NodeList)
    {
        PackerNode Node1 = new();
        Node1.X = SplittingNode.X + Width + Padding;
        Node1.Y = SplittingNode.Y;
        Node1.Width = SplittingNode.Width - Width - Padding;
        Node1.Height = Height;
        Node1.SplitType = PackerSplitType.Vertical;

        PackerNode Node2 = new();
        Node2.X = SplittingNode.X;
        Node2.Y = SplittingNode.Y + Height + Padding;
        Node2.Width = SplittingNode.Width;
        Node2.Height = SplittingNode.Height - Height - Padding;
        Node2.SplitType = PackerSplitType.Horizontal;

        if (Node1.Width > 0 && Node1.Height > 0)
            NodeList.Add(Node1);
        if (Node2.Width > 0 && Node2.Height > 0)
            NodeList.Add(Node2);
    }

    public void VerticalSplit(PackerNode SplittingNode, int Width, int Height, List<PackerNode> NodeList)
    {
        PackerNode Node1 = new();
        Node1.X = SplittingNode.X + Width + Padding;
        Node1.Y = SplittingNode.Y;
        Node1.Width = SplittingNode.Width - Width - Padding;
        Node1.Height = SplittingNode.Height;
        Node1.SplitType = PackerSplitType.Vertical;

        PackerNode Node2 = new();
        Node2.X = SplittingNode.X;
        Node2.Y = SplittingNode.Y + Height + Padding;
        Node2.Width = Width;
        Node2.Height = SplittingNode.Height - Height - Padding;
        Node2.SplitType = PackerSplitType.Horizontal;

		if (Node1.Width > 0 && Node1.Height > 0)
            NodeList.Add(Node1);
        if (Node2.Width > 0 && Node2.Height > 0)
            NodeList.Add(Node2);
    }
	
	public List<WaddleSpriteFrame> LayoutAtlas(List<WaddleSpriteFrame> Frames, PackerAtlas Atlas)
	{
		List<PackerNode> FreeNodes = new();
		List<PackerNode> UnfitNodes = new();
		List<WaddleSpriteFrame> FramesToPack = Frames.OrderBy(r => r.TargetWidth * r.TargetHeight).ToList();
		List<WaddleSpriteFrame> UnfitFrames = new();
		
		Atlas.Nodes = new();
		
		FreeNodes.Add(new() {
			Width = Atlas.Width,
			Height = Atlas.Height,
			SplitType = PackerSplitType.Horizontal
		});
		
		while (FreeNodes.Count > 0 && FramesToPack.Count > 0)
		{
			PackerNode Node = FreeNodes[0];
			FreeNodes.RemoveAt(0);
			
			WaddleSpriteFrame Frame = FindBestFitForNode(Node, FramesToPack);
			
			if (Frame == null) {
				UnfitNodes.Add(Node);
				continue;
			}
			
			if (Node.SplitType == PackerSplitType.Horizontal)
				HorizontalSplit(Node, Frame.TargetWidth, Frame.TargetHeight, FreeNodes);
            else
				VerticalSplit(Node, Frame.TargetWidth, Frame.TargetHeight, FreeNodes);
			
			Node.Source = Frame;
			Node.Width = Frame.TargetWidth;
			Node.Height = Frame.TargetHeight;
			FramesToPack.Remove(Frame);
			FreeNodes.AddRange(UnfitNodes); // bring em back we might need em!
			Atlas.Nodes.Add(Node);
		}
		
		return FramesToPack;
	}
	
	public void Pack(List<WaddleSpriteFrame> Frames)
	{	
		List<WaddleSpriteFrame> Queue = Frames.ToList();
		OutputAtlasses = new();
		
		while (Queue.Count > 0)
		{
			PackerAtlas Atlas = new()
			{
				Width = (int)AtlasSize,
				Height = (int)AtlasSize
			};
			
			List<WaddleSpriteFrame> LeftoverQueue = LayoutAtlas(Queue, Atlas);
			
			if (LeftoverQueue.Count == 0)
			{
				while (LeftoverQueue.Count == 0)
                {
                    Atlas.Width -= (int)SizeStep;
                    Atlas.Height -= (int)SizeStep;
                    LeftoverQueue = LayoutAtlas(Queue, Atlas);
                }
				
				Atlas.Width += (int)SizeStep;
                Atlas.Height += (int)SizeStep;
				LeftoverQueue = LayoutAtlas(Queue, Atlas);
			}
			
			OutputAtlasses.Add(Atlas);
			Queue = LeftoverQueue;
		}
	}
	
	public void SaveAtlasses(string Path)
	{
		for (int i = 0; i < OutputAtlasses.Count; i++)
		{
			TextureWorker.SaveImageToFile(OutputAtlasses[i].CreateImage(), Path + $"{i}.png");
		}
	}
}