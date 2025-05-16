using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Vector4 = System.Numerics.Vector4;

namespace StatusContrast;

public unsafe struct BackgroundNode
{
    private AtkUldAsset _asset;
    private AtkUldPart _part;
    private AtkUldPartsList _parts;
    private AtkImageNode _imageNode;

    public bool PreviewEnabled { get; set; }
    public bool FixGapsEnabled { get; set; }
    public Vector4 Color { get; set; }

    public void Init(AtkResNode* attachTarget, Configuration configuration, NodeIdProvider idProvider)
    {
        PreviewEnabled = configuration.Preview;
        FixGapsEnabled = configuration.FixGaps;
        Color = configuration.Color;

        _asset.AtkTexture.Ctor();
        _part.UldAsset = (AtkUldAsset*)Unsafe.AsPointer(ref _asset);

        _parts.Parts = (AtkUldPart*)Unsafe.AsPointer(ref _part);
        _parts.PartCount = 1;
        _parts.Id = idProvider.GetNext();

        _imageNode.Ctor();
        _imageNode.NodeId = idProvider.GetNext();
        _imageNode.Type = NodeType.Image;
        _imageNode.Flags = (byte)ImageNodeFlags.AutoFit;
        _imageNode.WrapMode = 0x1;
        _imageNode.PartsList = (AtkUldPartsList*)Unsafe.AsPointer(ref _parts);

        NodeLinker.AttachToNode((AtkResNode*)Unsafe.AsPointer(ref _imageNode), attachTarget);
    }

    public void Destroy()
    {
        NodeLinker.DetachNode((AtkResNode*)Unsafe.AsPointer(ref _imageNode));
        _asset.AtkTexture.Destroy(false);
        _imageNode.Destroy(false);
    }

    public void AssociateWithNode(AtkResNode* associatedNode)
    {
        // Set node as dirty to force redraw
        _imageNode.DrawFlags = 0x1;

        if (associatedNode == null)
        {
            // Hide node
            _imageNode.NodeFlags = NodeFlags.Enabled | NodeFlags.EmitsEvents;
            return;
        }

        NodeFlags nodeFlags = associatedNode->NodeFlags;

        if (PreviewEnabled)
        {
            nodeFlags |= NodeFlags.Visible;
        }

        Bounds bounds = ComputeBounds(associatedNode);

        _imageNode.NodeFlags = nodeFlags;
        _imageNode.SetXShort((short)bounds.Pos1.X);
        _imageNode.SetYShort((short)bounds.Pos1.Y);
        _imageNode.SetWidth((ushort)bounds.Width);
        _imageNode.SetHeight((ushort)bounds.Height);

        _imageNode.Color = new ByteColor { R = 0, G = 0, B = 0, A = (byte)(Color.W * 255) };
        _imageNode.AddRed = (byte)(Color.X * 255);
        _imageNode.AddGreen = (byte)(Color.Y * 255);
        _imageNode.AddBlue = (byte)(Color.Z * 255);
    }

    private Bounds ComputeBounds(AtkResNode* associatedNode)
    {
        Bounds bounds = new();
        associatedNode->GetBounds(&bounds);

        if (!FixGapsEnabled || associatedNode->PrevSiblingNode == null)
        {
            return bounds;
        }

        Bounds prevSiblingBounds = new();
        associatedNode->PrevSiblingNode->GetBounds(&prevSiblingBounds);

        if (bounds.Pos1.Y != prevSiblingBounds.Pos1.Y)
        {
            // Different row, no need for gap correction
            return bounds;
        }

        // Left justified
        if (prevSiblingBounds.Pos1.X < bounds.Pos1.X)
        {
            bounds.Pos1.X = prevSiblingBounds.Pos2.X;
        }
        // Right justified
        else
        {
            bounds.Pos2.X = prevSiblingBounds.Pos1.X;
        }

        return bounds;
    }
}
