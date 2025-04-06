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

    public void Init(AtkResNode* nodetoFollow)
    {
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
        _imageNode.Color = new ByteColor { R = 0, G = 0, B = 0, A = 128 };
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
        _imageNode.NodeFlags = _nodeToFollow->NodeFlags;
        _imageNode.SetXFloat(_nodeToFollow->X);
        _imageNode.SetYFloat(_nodeToFollow->Y);
        _imageNode.SetWidth((_nodeToFollow->Width));
        _imageNode.SetHeight(_nodeToFollow->Height);
        _imageNode.SetScale(_nodeToFollow->ScaleX, _nodeToFollow->ScaleY);
    }
}
