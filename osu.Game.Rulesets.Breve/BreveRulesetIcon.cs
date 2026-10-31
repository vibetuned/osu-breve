using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace osu.Game.Rulesets.Breve
{
    /// <summary>
    /// The ruleset's icon: two of Cytus's ornate arrows crossed like swords, one pointing up to the right and one up to the left.
    /// Each has a long blade that flares into a pointed bulb, then a neck, a smaller knob, and a rod to the round end it comes out of.
    /// </summary>
    /// <remarks>
    /// It's drawn at a fixed size, which the containers icons are shown in scale to fit.
    /// The arrows' curves can't be built from the framework's shapes, so the icon is drawn into a texture, smoothing its edges by sampling each pixel several times.
    /// They're thicker than in Cytus, so that they still read at the small sizes icons are shown at.
    /// </remarks>
    public partial class BreveRulesetIcon : CompositeDrawable
    {
        private const float icon_size = 100;

        private const int texture_size = 128;

        /// <summary>
        /// How many times each pixel is sampled across and down, to smooth the edges.
        /// </summary>
        private const int samples_per_axis = 4;

        // The arrows' shape, in the icon's units, measured along an arrow from its tip.
        private const float blade_length = 58;
        private const float blade_half_width = 4.2f;

        /// <summary>
        /// How sharply the blade widens towards its base. Above 1, it stays thin for most of its length.
        /// </summary>
        private const float blade_flare = 1.25f;

        private static readonly Bulge bulb = new Bulge(46, 72, 7.5f, 0.62f);
        private static readonly Bulge knob = new Bulge(74, 91, 5.5f, 0.5f);

        private const float rod_half_width = 2.4f;

        /// <summary>
        /// Where the rod meets the round end.
        /// </summary>
        private const float rod_end = 100;

        private const float end_radius = 6.5f;

        private const float arrow_length = rod_end + 2 * end_radius;

        // How the arrows are laid out in the icon.
        private const float arrow_scale = 0.92f;

        /// <summary>
        /// How far from upright the arrows lean, in degrees.
        /// </summary>
        private const float arrow_lean = 40;

        /// <summary>
        /// How far from their tips the arrows cross, up their blades.
        /// </summary>
        private const float crossing_distance = 34;

        private static readonly Lazy<Rgba32[]> pixels = new Lazy<Rgba32[]>(draw);

        private Texture? texture;

        public BreveRulesetIcon()
        {
            Size = new Vector2(icon_size);
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            texture = renderer.CreateTexture(texture_size, texture_size);
            texture.SetData(new TextureUpload(Image.LoadPixelData<Rgba32>(pixels.Value, texture_size, texture_size)));

            InternalChild = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Texture = texture,
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            texture?.Dispose();
        }

        /// <summary>
        /// Draws the icon in white, each pixel as opaque as the share of it the arrows cover.
        /// </summary>
        private static Rgba32[] draw()
        {
            float lean = MathHelper.DegreesToRadians(arrow_lean);

            // The directions from the arrows' tips towards their ends: down to the left for the arrow pointing up to the right, and the other way round.
            var directions = new[]
            {
                new Vector2(-MathF.Sin(lean), MathF.Cos(lean)),
                new Vector2(MathF.Sin(lean), MathF.Cos(lean)),
            };

            // The arrows are centred across the icon by being symmetrical, and down it by placing the crossing between their tips' and ends' heights.
            float tipsY = -crossing_distance * MathF.Cos(lean);
            float endsY = (arrow_length - crossing_distance) * MathF.Cos(lean);
            var crossing = new Vector2(icon_size / 2, icon_size / 2 - (tipsY + endsY) / 2 * arrow_scale);

            var result = new Rgba32[texture_size * texture_size];
            const float pixel_size = icon_size / texture_size;

            for (int y = 0; y < texture_size; y++)
            {
                for (int x = 0; x < texture_size; x++)
                {
                    int covered = 0;

                    for (int sy = 0; sy < samples_per_axis; sy++)
                    {
                        for (int sx = 0; sx < samples_per_axis; sx++)
                        {
                            var sample = new Vector2(x + (sx + 0.5f) / samples_per_axis, y + (sy + 0.5f) / samples_per_axis) * pixel_size;
                            var fromCrossing = (sample - crossing) / arrow_scale;

                            foreach (var direction in directions)
                            {
                                float along = Vector2.Dot(fromCrossing, direction) + crossing_distance;
                                float across = fromCrossing.X * direction.Y - fromCrossing.Y * direction.X;

                                if (arrowCovers(along, across))
                                {
                                    covered++;
                                    break;
                                }
                            }
                        }
                    }

                    result[y * texture_size + x] = new Rgba32(255, 255, 255, (byte)Math.Round(255f * covered / (samples_per_axis * samples_per_axis)));
                }
            }

            return result;
        }

        /// <summary>
        /// Whether a point is on an arrow, given how far along the arrow it is from its tip, and how far across from its middle.
        /// </summary>
        private static bool arrowCovers(float along, float across)
        {
            float endCentre = rod_end + end_radius - 1;

            if (new Vector2(along - endCentre, across).Length <= end_radius)
                return true;

            if (along < 0 || along > rod_end + 1)
                return false;

            float halfWidth = 0;

            if (along <= blade_length)
                halfWidth = blade_half_width * MathF.Pow(along / blade_length, blade_flare);

            // The neck and rod, which run through the knob to the round end, start a little before the end of the blade so that it joins them under the bulb.
            if (along >= blade_length - 2)
                halfWidth = MathF.Max(halfWidth, rod_half_width);

            halfWidth = MathF.Max(halfWidth, MathF.Max(bulb.HalfWidthAt(along), knob.HalfWidthAt(along)));

            return MathF.Abs(across) <= halfWidth;
        }

        /// <summary>
        /// A pointed swelling along an arrow, between two distances from its tip.
        /// </summary>
        /// <param name="Start">How far from the tip it starts.</param>
        /// <param name="End">How far from the tip it ends.</param>
        /// <param name="HalfWidth">How far it reaches out from the middle of the arrow at its widest.</param>
        /// <param name="Widest">Where it's widest, as a proportion of its length.</param>
        private record Bulge(float Start, float End, float HalfWidth, float Widest)
        {
            public float HalfWidthAt(float along)
            {
                if (along <= Start || along >= End)
                    return 0;

                // Skews a sine bump so that its peak falls where the swelling is widest.
                float progress = MathF.Pow((along - Start) / (End - Start), MathF.Log(0.5f) / MathF.Log(Widest));
                return HalfWidth * MathF.Sin(MathF.PI * progress);
            }
        }
    }
}
