using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

public sealed unsafe class BackgroundNodePool : IDisposable
{
    private const int BackgroundNodeCount = 200;

    private readonly IPluginLog _log;
    private readonly BackgroundNode* _backgrounds;
    private int _currentIndex;

    public BackgroundNodePool(IPluginLog log, AtkResNode* attachTarget, Configuration configuration,
        NodeIdProvider idProvider)
    {
        _log = log;
        _currentIndex = 0;

        ulong allocationSize = (ulong)(sizeof(BackgroundNode) * BackgroundNodeCount);
        _backgrounds = (BackgroundNode*)IMemorySpace.GetUISpace()->Malloc(allocationSize, 8);
        if (_backgrounds == null)
        {
            throw new Exception("Failed to allocate background pool");
        }

        IMemorySpace.Memset(_backgrounds, 0, allocationSize);

        for (int i = 0; i < BackgroundNodeCount; i++)
        {
            _backgrounds[i].Init(attachTarget, configuration, idProvider);
        }
    }

    public void Dispose()
    {
        for (int i = 0; i < BackgroundNodeCount; i++)
        {
            _backgrounds[i].Destroy();
        }

        IMemorySpace.Free(_backgrounds);
    }

    public void AssociateNextNode(AtkResNode* statusNode)
    {
        if (_currentIndex >= BackgroundNodeCount)
        {
            _log.Error("Not enough background nodes in pool!");
            return;
        }

        _backgrounds[_currentIndex].AssociateWithNode(statusNode);
        _currentIndex++;
    }

    public void PrepareForNextFrame()
    {
        _currentIndex = 0;
        for (int i = 0; i < BackgroundNodeCount; i++)
        {
            _backgrounds[i].AssociateWithNode(null);
        }
    }

    public void SetConfiguration(Configuration configuration)
    {
        for (int i = 0; i < BackgroundNodeCount; i++)
        {
            _backgrounds[i].PreviewEnabled = configuration.Preview;
            _backgrounds[i].FixGapsEnabled = configuration.FixGaps;
            _backgrounds[i].Color = configuration.Color;
        }
    }
}
