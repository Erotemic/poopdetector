// PoopDetector.AI.Vision/VisionModelManager.cs
// --------------------------------------------------------------
using CommunityToolkit.Mvvm.ComponentModel;
using PoopDetector.Services;
using PoopDetector.AI.Vision.YoloX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PoopDetector.AI.Vision;

public partial class VisionModelManager : ObservableObject
{
    sealed record VisionModelDescriptor(
        string FileName,
        Func<string, IVision> Factory,
        string? LegacyUrl = null);

    static readonly VisionModelOptions _options = VisionModelOptionsLoader.Load();
    static readonly IReadOnlyDictionary<ModelTypes, VisionModelDescriptor> _models =
        new Dictionary<ModelTypes, VisionModelDescriptor>
        {
            {
                ModelTypes.YoloxNanoPoop,
                new VisionModelDescriptor(
                    "yolox_nano_poop_cropped_only_best.onnx",
                    path => new YoloX.YoloX(path, 416, 416, YoloXColormap.PoopList),
                    "https://github.com/mkorzunowicz/poop_models/raw/refs/heads/main/yolox_nano_poop_cropped_only_best.onnx")
            },
            {
                ModelTypes.Yolov9ScatSpotter,
                new VisionModelDescriptor(
                    "yolov9_poop.onnx",
                    path => new Yolov9.Yolov9(path, YoloXColormap.PoopList),
                    "https://huggingface.co/erotemic/shitspotter-models/resolve/main/models/yolo-v9/shitspotter-simple-v3-run-v06-epoch%3D0032-step%3D000132-trainlosstrain_loss%3D7.603.onnx")
            },
            {
                ModelTypes.YoloxNano,
                new VisionModelDescriptor(
                    "yolox_nano.onnx",
                    path => new YoloX.YoloX(path, 416, 416, YoloXColormap.ColormapList),
                    "https://huggingface.co/yourbucket/yolox_nano.onnx")
            },
            {
                ModelTypes.ShitspotterCustomV2,
                new VisionModelDescriptor(
                    "shitspotter_custom_v2_epoch126.onnx",
                    path => new YoloX.YoloX(path, 416, 416, YoloXColormap.PoopList),
                    "https://github.com/Erotemic/poop_models/raw/refs/heads/main/shitspotter_custom_v2_epoch126.onnx")
            },
            {
                ModelTypes.ShitspotterCustomV5,
                new VisionModelDescriptor(
                    "shitspotter-custom-v5-epoch_115.onnx",
                    path => new YoloX.YoloX(path, 416, 416, YoloXColormap.PoopList),
                    "https://raw.githubusercontent.com/Erotemic/poop_models/main/shitspotter-custom-v5-epoch_115.onnx")
            }
        };

    static readonly ModelTypes _defaultModelType = ResolveDefaultModelType();
    public static ModelTypes DefaultModel => _defaultModelType;

    // singleton
    public static VisionModelManager Instance { get; } = new();

    VisionModelManager() { }

    public IVision? CurrentModel { get; private set; }
    public MobileSam.MobileSam? MobileSam { get; private set; }

    // --------------  progress binding properties  -------------- //
    [ObservableProperty] double _downloadProgress;  // 0-1
    [ObservableProperty] bool _isDownloading;

    // --------------  public API  ------------------------------- //
    public async Task ChangeModelAsync(ModelTypes type,
                                       CancellationToken cancel = default)
    {
        if (CurrentModel is not null &&
            _cache.TryGetValue(type, out var ready) &&
            ready == CurrentModel)
            return;      // already active

        await EnsureBundledModelsAsync(cancel);

        IsDownloading = true;
        DownloadProgress = 0;

        try
        {
            // Always let the cache decide whether a packaged copy is good enough or
            // if we need to reach out to the network for a fresh download.
            string localPath = await EnsureModelFileAsync(type, cancel);
            CurrentModel = GetDescriptor(type).Factory(localPath);
            _cache[type] = CurrentModel;
        }
        catch (Exception ex)
        {
            // fallback: stay without a model but keep the app alive
            RaiseError($"Initial model download failed:\n{ex.Message}");
            IsDownloading = false;
            return;
        }
        finally
        {
            IsDownloading = false;
        }
    }
    bool _bootstrapped;
    bool _bundledPrepared;

    /// <summary>
    /// Prepare the default vision model so the shell has something ready when
    /// the app launches. This honours bundled assets before attempting any
    /// remote download.
    /// </summary>
    public async Task EnsureDefaultModelAsync()
    {
        MobileSam = new MobileSam.MobileSam();
        if (_bootstrapped || CurrentModel is not null) return;
        await EnsureBundledModelsAsync(CancellationToken.None);
        await ChangeModelAsync(_defaultModelType, CancellationToken.None);
        _bootstrapped = CurrentModel is not null;
    }
    // --------------  internals  -------------------------------- //
    readonly ConcurrentDictionary<ModelTypes, IVision> _cache = new();

    /// <summary>
    /// Copy any packaged ONNX files that ship with the app into the app data
    /// directory so later calls to <see cref="EnsureModelFileAsync"/> can reuse
    /// them without touching the network.
    /// </summary>
    async Task EnsureBundledModelsAsync(CancellationToken cancel)
    {
        if (_bundledPrepared)
            return;

        _bundledPrepared = true;

        if (_options.BundledModels == null || _options.BundledModels.Count == 0)
            return;

        foreach (string fileName in _options.BundledModels
                     .Where(n => !string.IsNullOrWhiteSpace(n))
                     .Select(n => n.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                await ModelCache.EnsurePackagedCopyAsync(fileName, cancel);
            }
            catch (Exception ex)
            {
                RaiseError($"Could not stage bundled model '{fileName}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Resolve a usable ONNX file for <paramref name="t"/>. A packaged asset
    /// wins if it exists; otherwise we download and cache the remote model.
    /// </summary>
    static async Task<string> EnsureModelFileAsync(ModelTypes t,
                                                   CancellationToken ct)
    {
        var p = new Progress<double>(d =>
            Instance.DownloadProgress = d);     // pushes into binding

        string url = GetRemoteUrl(t);
        string fileName = GetDescriptor(t).FileName;
        return await ModelCache.GetAsync(url, fileName, p, ct);
    }

    static string GetRemoteUrl(ModelTypes type)
    {
        VisionModelDescriptor descriptor = GetDescriptor(type);
        string? resolved = _options.ResolveRemoteUrl(type.ToString(), descriptor.FileName, descriptor.LegacyUrl);

        if (string.IsNullOrWhiteSpace(resolved))
            throw new InvalidOperationException($"No remote URL configured for model '{type}'.");

        return resolved;
    }

    static ModelTypes ResolveDefaultModelType()
    {
        if (!string.IsNullOrWhiteSpace(_options.DefaultModel) &&
            Enum.TryParse<ModelTypes>(_options.DefaultModel, true, out var configured) &&
            _models.ContainsKey(configured))
        {
            return configured;
        }

        return ModelTypes.ShitspotterCustomV5;
    }
    public enum Backend
    {
        YoloX,
        Yolov9
    }

    /// <summary>
    /// Download an ONNX from <paramref name="url"/> (once), create the requested
    /// backend wrapper, and make it the <see cref="CurrentModel"/>.
    /// </summary>
    /// <param name="url">HTTP / HTTPS / IPFS gateway link</param>
    /// <param name="backend">Which post-processor to use</param>
    /// <param name="inputW">Model’s expected width  (default 640)</param>
    /// <param name="inputH">Model’s expected height (default 640)</param>
    /// <param name="labels">Class list / colour map</param>
    public async Task LoadRemoteModelAsync(
        string url,
        Backend backend,
        int inputW,
        int inputH,
        List<(string, System.Drawing.Color)> labels,
        CancellationToken cancel = default)
    {
        IsDownloading = true;
        DownloadProgress = 0;

        try
        {
            string localPath = await ModelCache.GetAsync(
                                   url,
                                   Path.GetFileName(new Uri(url).AbsolutePath),
                                   new Progress<double>(p => DownloadProgress = p),
                                   cancel);

            CurrentModel = backend switch
            {
                Backend.YoloX => new YoloX.YoloX(localPath, inputW, inputH, labels),
                Backend.Yolov9 => new Yolov9.Yolov9(localPath, labels),
                _ => throw new ArgumentOutOfRangeException(nameof(backend))
            };
        }
        catch (Exception ex)
        {
            RaiseError($"Could not download model:\n{ex.Message}");
            throw;                                   // still fail if nobody handled
        }
        finally
        {
            IsDownloading = false;
        }
    }
    static VisionModelDescriptor GetDescriptor(ModelTypes type) =>
        _models.TryGetValue(type, out var descriptor)
            ? descriptor
            : throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown model type.");

    /// <summary>
    /// Raised whenever downloading or reading a model fails.
    /// </summary>
    public event EventHandler<string>? DownloadError;

    void RaiseError(string msg)
    {
        DownloadError?.Invoke(this, msg);
#if DEBUG
        System.Diagnostics.Debug.WriteLine("Model download error: " + msg);
#endif
    }
    // ----------------------------------------------------------------- //
    public enum ModelTypes
    {
        YoloxNanoPoop,
        Yolov9ScatSpotter,
        YoloxNano,
        ShitspotterCustomV2,
        ShitspotterCustomV5,
    }
}
