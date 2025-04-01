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

    public AtkImageNode* ImageNode => (AtkImageNode*)Unsafe.AsPointer(ref _imageNode);

    public void Init(AtkResNode* nodetoCopy)
    {
        _asset.AtkTexture.Ctor();

        _part.UldAsset = (AtkUldAsset*)Unsafe.AsPointer(ref _asset);

        _parts.Parts = (AtkUldPart*)Unsafe.AsPointer(ref _part);
        _parts.PartCount = 1;
        _parts.Id = 0;

        _imageNode.Ctor();
        _imageNode.Type = NodeType.Image;
        _imageNode.Flags = (byte)ImageNodeFlags.AutoFit;
        _imageNode.WrapMode = 0x1;
        _imageNode.NodeFlags = NodeFlags.Visible;
        _imageNode.Color = new ByteColor { R = 0, G = 0, B = 0, A = 128 };
        _imageNode.PartsList = (AtkUldPartsList*)Unsafe.AsPointer(ref _parts);

        Update(nodetoCopy);
    }

    public void Update(AtkResNode* nodetoCopy)
    {
        _imageNode.SetXFloat(nodetoCopy->X);
        _imageNode.SetYFloat(nodetoCopy->Y);
        _imageNode.SetWidth(nodetoCopy->Width);
        _imageNode.SetHeight(nodetoCopy->Height);
        _imageNode.SetScale(nodetoCopy->ScaleX, nodetoCopy->ScaleY);
    }

    public void Destroy()
    {
        _asset.AtkTexture.Destroy(false);
        _imageNode.Destroy(false);
    }
}
