using System.IO;
using NUnit.Framework;
using PixelFx;
using PixelFx.Editor;
using UnityEditor;
using UnityEngine;

// Window > General > Test Runner > EditMode > Run All
public class PixelFxTests
{
    const string Sample = PixelFxSheetPostprocessor.SheetsRoot + "/explosions/small-explosion.json";

    [Test]
    public void ParsesTheSampleSheet()
    {
        var d = PixelFxSheetData.Parse(File.ReadAllText(Sample));
        Assert.AreEqual("explosions/small-explosion", d.id);
        Assert.AreEqual(d.frames.Count, 163);
        Assert.AreEqual(300, d.frameWidth);
        Assert.AreEqual(240, d.frameHeight);
        Assert.AreEqual(150, d.pivotX);
        Assert.AreEqual(196, d.pivotY);
        Assert.IsFalse(d.loop);
    }

    [Test]
    public void ImportsTextureAsPixelArt()
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(Path.ChangeExtension(Sample, ".png"));
        Assert.AreEqual(FilterMode.Point, ti.filterMode);
        Assert.AreEqual(TextureImporterCompression.Uncompressed, ti.textureCompression);
        Assert.IsFalse(ti.mipmapEnabled);
    }

    [Test]
    public void BuildsSheetAssetSpritesAndPrefab()
    {
        var sheet = PixelFxSheetPostprocessor.BuildAssets(Sample);
        Assert.NotNull(sheet.texture);
        Assert.AreEqual(sheet.data.textureWidth, sheet.texture.width, "texture was downscaled on import");
        var sprites = sheet.Sprites;
        Assert.AreEqual(163, sprites.Length);
        Assert.AreEqual(300, sprites[0].rect.width);
        // pivot sits at the effect's origin: 150 px from the left, 196 px from the top of a 240 px frame
        Assert.AreEqual(150f, sprites[0].pivot.x, 0.01f);
        Assert.AreEqual(240f - 196f, sprites[0].pivot.y, 0.01f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.ChangeExtension(Sample, ".prefab"));
        Assert.NotNull(prefab);
        Assert.AreEqual(sheet, prefab.GetComponent<PixelFxPlayer>().sheet);
    }
}
