using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

public unsafe struct BackgroundNode
{
    private AtkUldAsset _asset;
    private AtkUldPart _part;
    private AtkUldPartsList _parts;
    private AtkImageNode _imageNode;
    private AtkResNode* _nodeToFollow;

    public AtkImageNode* ImageNode => (AtkImageNode*)Unsafe.AsPointer(ref _imageNode);
    public bool PreviewEnabled { get; set; }
    public bool FixGapsEnabled { get; set; }
    public Vector4 Color { get; set; }

    public void Init(AtkResNode* nodetoFollow, Configuration configuration)
    {
        PreviewEnabled = configuration.Preview;
        FixGapsEnabled = configuration.FixGaps;
        Color = configuration.Color;

        _nodeToFollow = nodetoFollow;

        _asset.AtkTexture.Ctor();

        _part.UldAsset = (AtkUldAsset*)Unsafe.AsPointer(ref _asset);

        _parts.Parts = (AtkUldPart*)Unsafe.AsPointer(ref _part);
        _parts.PartCount = 1;
        _parts.Id = 0;

        _imageNode.Ctor();
        _imageNode.Type = NodeType.Image;
        _imageNode.Flags = (byte)ImageNodeFlags.AutoFit;
        _imageNode.WrapMode = 0x1;
        _imageNode.PartsList = (AtkUldPartsList*)Unsafe.AsPointer(ref _parts);

        Update();
    }

    public void Destroy()
    {
        _asset.AtkTexture.Destroy(false);
        _imageNode.Destroy(false);
    }

    public void Update()
    {
        NodeFlags nodeFlags = _nodeToFollow->NodeFlags;

        if (PreviewEnabled)
        {
            nodeFlags |= NodeFlags.Visible;
        }

        Rectangle bounds = ComputeBounds();

        _imageNode.NodeFlags = nodeFlags;
        _imageNode.SetXFloat(bounds.Left);
        _imageNode.SetYFloat(bounds.Top);
        _imageNode.SetWidth((ushort)bounds.Width);
        _imageNode.SetHeight((ushort)bounds.Height);
        _imageNode.SetScale(_nodeToFollow->GetScaleX(), _nodeToFollow->GetScaleY());

        _imageNode.Color = new ByteColor { R = 0, G = 0, B = 0, A = (byte)(Color.W * 255) };
        _imageNode.AddRed = (byte)(Color.X * 255);
        _imageNode.AddGreen = (byte)(Color.Y * 255);
        _imageNode.AddBlue = (byte)(Color.Z * 255);
    }

    private Rectangle ComputeBounds()
    {
        Rectangle bounds = new()
        {
            X = _nodeToFollow->GetXShort(),
            Y = _nodeToFollow->GetYShort(),
            Width = _nodeToFollow->GetWidth(),
            Height = _nodeToFollow->GetHeight()
        };

        if (!FixGapsEnabled
            || _nodeToFollow->PrevSiblingNode == null
            || _nodeToFollow->PrevSiblingNode->GetYShort() != _nodeToFollow->GetYShort())
        {
            return bounds;
        }

        // Left justified
        if (_nodeToFollow->PrevSiblingNode->GetXShort() < _nodeToFollow->GetXShort())
        {
            int prevNodeRightEdge = _nodeToFollow->PrevSiblingNode->GetXShort()
                                    + _nodeToFollow->PrevSiblingNode->GetWidth();
            int gap = _nodeToFollow->GetXShort() - prevNodeRightEdge;

            bounds.X -= gap;
            bounds.Width += gap;
        }
        // Right justified
        else
        {
            bounds.Width = _nodeToFollow->PrevSiblingNode->GetXShort() - _nodeToFollow->GetXShort();
        }

        return bounds;
    }
}
