using System.Collections.Generic;
using Nox.CCK.Interaction;
using Nox.CCK.Theme;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using Logger = Nox.CCK.Utils.Logger;

// ============================================================
// OUTLINE GLOW : Renderer Feature (URP 17 / Unity 6, RenderGraph)
// ============================================================
// Principe :
//   1. Masque    : les objets de la couche choisie sont dessines en blanc
//   2. Flou H/V  : le masque est floute (demi-resolution)
//   3. Composite : degrade = rampe smoothstep(min, max) sur le flou,
//                  outline = masque dilate - masque
// Le composite se fait AVANT le post-process : le Bloom voit le glow HDR.

public class OutlineGlowFeature : ScriptableRendererFeature {
	[System.Serializable]
	public class Settings {
		[Header("Objets")]
		public LayerMask layerMask = 0; // couche des copies du glow (UI) et de leurs masques de rendu

		[Header("Outline")]
		[ColorUsage(true, true)]
		public Color outlineColor = new Color(2.5f, 0.9f, 0.1f, 1f);

		[Range(0f, 12f)]
		public float outlineWidth = 2f;

		[Header("Glow")]
		[ColorUsage(true, true)]
		public Color glowColor = new Color(2.5f, 0.9f, 0.1f, 1f);

		[Range(1f, 80f)]
		public float glowWidth = 16f;

		[Range(0f, 10f)]
		public float glowIntensity = 1.5f;

		[Range(0.1f, 8f)]
		public float glowFalloff = 1.5f;

		public Vector2 gradientMinMax = new Vector2(0f, 1f);

		[Header("Avance")]
		public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

		public Shader shader; // laisse vide : Hidden/!Neon/OutlineGlowPost
	}

	public Settings settings = new Settings();

	private Material _material;
	private OutlineGlowPass _pass;

	public override void Create() {
		var sh = settings.shader != null
			? settings.shader
			: Shader.Find("Hidden/!Neon/OutlineGlowPost");

		if (sh == null) {
			Logger.LogWarning("OutlineGlow: shader not found.", tag: nameof(OutlineGlowFeature));
			return;
		}

		_material = CoreUtils.CreateEngineMaterial(sh);
		_pass = new OutlineGlowPass(settings, _material) { renderPassEvent = settings.renderPassEvent };
		ThemeManager.OnThemeChanged.AddListener(OnThemeChanged);
	}

	/// <summary>The hover glow follows the theme of the UI: it is repainted when the theme changes.</summary>
	/// <param name="_">Id of the new theme.</param>
	private void OnThemeChanged(string _)
		=> _pass?.RefreshColors();

	private void OnValidate()
		=> _pass?.RefreshColors();

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
		if (_pass == null || _material == null || settings.layerMask.value == 0)
			return;

		if (!GlowSources.Any)
			return;

		var type = renderingData.cameraData.cameraType;
		if (type == CameraType.Preview || type == CameraType.Reflection)
			return;

		if (renderingData.cameraData.camera.targetTexture != null)
			return;

		_pass.ConfigureInput(ScriptableRenderPassInput.Color);
		renderer.EnqueuePass(_pass);
	}

	protected override void Dispose(bool disposing) {
		ThemeManager.OnThemeChanged.RemoveListener(OnThemeChanged);
		CoreUtils.Destroy(_material);
	}

	// ============================================================
	// PASS
	// ============================================================

	private class OutlineGlowPass : ScriptableRenderPass {
		private const int PassMask = 0, PassBlur = 1, PassComposite = 2;

		private static readonly int SourceTexId = Shader.PropertyToID("_SourceTex");
		private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
		private static readonly int BlurTexId = Shader.PropertyToID("_BlurTex");
		private static readonly int BlurDirId = Shader.PropertyToID("_BlurDir");
		private static readonly int PixelSizeId = Shader.PropertyToID("_PixelSize");
		private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
		private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
		private static readonly int GlowParamsId = Shader.PropertyToID("_GlowParams");
		private static readonly int GradMinMaxId = Shader.PropertyToID("_GradMinMax");

		private static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId> {
			new ShaderTagId("UniversalForward"),
			new ShaderTagId("UniversalForwardOnly"),
			new ShaderTagId("SRPDefaultUnlit")
		};

		private readonly Settings _settings;
		private readonly Material _material;

		/// <summary>Colours of the glow once the theme is applied, refreshed when it changes.</summary>
		private Color _outlineColor;
		private Color _glowColor;

		private class MaskData {
			public RendererListHandle list;
		}

		private class FullData {
			public TextureHandle src;
			public TextureHandle mask;
			public TextureHandle blur;
			public Material material;
			public int pass;
			public Vector4 dir;
			public MaterialPropertyBlock block;
		}

		public OutlineGlowPass(Settings settings, Material material) {
			_settings = settings;
			_material = material;
			profilingSampler = new ProfilingSampler("OutlineGlow");
			RefreshColors();
		}

		/// <summary>
		/// Colours of the glow of the theme in use: the renderer asset gives the authored ones, the theme swaps
		/// them so the highlight of a grabbable keeps the hue of the accent of the UI.
		/// </summary>
		public void RefreshColors() {
			var theme = ThemeManager.Current;
			_outlineColor = theme.Map(_settings.outlineColor);
			_glowColor = theme.Map(_settings.glowColor);
		}

		public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData) {
			var resources = frameData.Get<UniversalResourceData>();
			if (resources.isActiveTargetBackBuffer)
				return;

			var cameraData = frameData.Get<UniversalCameraData>();
			var renderingData = frameData.Get<UniversalRenderingData>();
			var lightData = frameData.Get<UniversalLightData>();

			var color = resources.activeColorTexture;

			// ---- Parametres constants (poses sur le material) ----
			var camDesc = cameraData.cameraTargetDescriptor;
			_material.SetVector(PixelSizeId, new Vector4(1f / camDesc.width, 1f / camDesc.height, 0, 0));
			_material.SetColor(OutlineColorId, _outlineColor);
			_material.SetColor(GlowColorId, _glowColor);
			_material.SetVector(GlowParamsId,
				new Vector4(_settings.glowWidth, _settings.outlineWidth, _settings.glowIntensity, _settings.glowFalloff));
			_material.SetVector(GradMinMaxId, new Vector4(_settings.gradientMinMax.x, _settings.gradientMinMax.y, 0, 0));

			// ---- Textures ----
			var maskDesc = camDesc;
			maskDesc.depthBufferBits = 0;
			maskDesc.msaaSamples = 1;
			maskDesc.graphicsFormat = GraphicsFormat.R16_SFloat;

			var blurDesc = maskDesc;
			blurDesc.width = Mathf.Max(1, camDesc.width / 2);
			blurDesc.height = Mathf.Max(1, camDesc.height / 2);

			var copyDesc = camDesc;
			copyDesc.depthBufferBits = 0;
			copyDesc.msaaSamples = 1;

			var mask = UniversalRenderer.CreateRenderGraphTexture(rg, maskDesc, "_OutlineGlowMask", true, FilterMode.Bilinear);
			var blurA = UniversalRenderer.CreateRenderGraphTexture(rg, blurDesc, "_OutlineGlowBlurA", false, FilterMode.Bilinear);
			var blurB = UniversalRenderer.CreateRenderGraphTexture(rg, blurDesc, "_OutlineGlowBlurB", false, FilterMode.Bilinear);
			var copy = UniversalRenderer.CreateRenderGraphTexture(rg, copyDesc, "_OutlineGlowCopy", false);

			// ============ 1. MASQUE ============
			using (var builder = rg.AddRasterRenderPass<MaskData>("OutlineGlow Mask", out var data)) {
				var drawing = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData,
					SortingCriteria.CommonOpaque);
				drawing.overrideMaterial = _material;
				drawing.overrideMaterialPassIndex = PassMask;

				var filtering = new FilteringSettings(new RenderQueueRange(GrabbableHighlight.GlowQueue, GrabbableHighlight.GlowQueue), _settings.layerMask);
				var param = new RendererListParams(renderingData.cullResults, drawing, filtering);

				data.list = rg.CreateRendererList(param);
				builder.UseRendererList(data.list);
				builder.SetRenderAttachment(mask, 0);
				builder.SetRenderFunc((MaskData d, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(d.list));
			}

			// ============ 2. FLOU HORIZONTAL puis VERTICAL ============
			AddFullscreenPass(rg, "OutlineGlow BlurH", mask, default, default, blurA, PassBlur, new Vector4(1, 0, 0, 0), false, _blocks[0]);
			AddFullscreenPass(rg, "OutlineGlow BlurV", blurA, default, default, blurB, PassBlur, new Vector4(0, 1, 0, 0), false, _blocks[1]);

			// ============ 3. COMPOSITE ============
			rg.AddCopyPass(color, copy, "OutlineGlow Copy");
			AddFullscreenPass(rg, "OutlineGlow Composite", copy, mask, blurB, color, PassComposite, Vector4.zero, true, _blocks[2]);
		}

		/// <summary>
		/// One <see cref="MaterialPropertyBlock"/> per fullscreen pass, created once: the pass is reused for
		/// every camera and every frame, and allocating them per record cost three objects per camera.
		/// </summary>
		private readonly MaterialPropertyBlock[] _blocks = {
			new MaterialPropertyBlock(),
			new MaterialPropertyBlock(),
			new MaterialPropertyBlock()
		};

		private void AddFullscreenPass(RenderGraph rg, string name,
			TextureHandle src, TextureHandle mask, TextureHandle blur,
			TextureHandle dst, int passIndex, Vector4 dir, bool noCulling,
			MaterialPropertyBlock block
		) {
			using var builder = rg.AddRasterRenderPass<FullData>(name, out var data);
			data.src = src;
			data.mask = mask;
			data.blur = blur;
			data.material = _material;
			data.pass = passIndex;
			data.dir = dir;
			data.block = block;

			builder.UseTexture(src);
			if (mask.IsValid()) builder.UseTexture(mask);
			if (blur.IsValid()) builder.UseTexture(blur);
			builder.SetRenderAttachment(dst, 0);
			if (noCulling) builder.AllowPassCulling(false);

			builder.SetRenderFunc(static (FullData d, RasterGraphContext ctx) => {
				d.block.Clear();
				d.block.SetVector(BlurDirId, d.dir);
				d.block.SetTexture(SourceTexId, ((RTHandle)d.src).rt);
				if (d.mask.IsValid()) d.block.SetTexture(MaskTexId, ((RTHandle)d.mask).rt);
				if (d.blur.IsValid()) d.block.SetTexture(BlurTexId, ((RTHandle)d.blur).rt);

				ctx.cmd.DrawProcedural(Matrix4x4.identity, d.material, d.pass, MeshTopology.Triangles, 3, 1, d.block);
			});
		}
	}
}
