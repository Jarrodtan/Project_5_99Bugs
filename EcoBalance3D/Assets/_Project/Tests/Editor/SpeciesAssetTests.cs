// [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
using NUnit.Framework;
using UnityEditor;

// Checks the real species assets used in the game, not test copies. Catches data
// that silently breaks when SpeciesDefinition changes (e.g. the old `eats` field
// being ignored after the switch to `relationships`).
public class SpeciesAssetTests
{
    const string SpeciesDir = "Assets/_Project/ScriptableObjects/Species/";
    const string ResourceDir = "Assets/_Project/ScriptableObjects/Resources/";

    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, $"Missing asset: {path}");
        return asset;
    }

    [Test]
    public void BlueSheep_EatsGrass()
    {
        var sheep = Load<SpeciesDefinition>(SpeciesDir + "BlueSheep.asset");
        var grass = Load<ResourceDefinition>(ResourceDir + "Grass.asset");

        Assert.IsTrue(sheep.HasRelationship(RelationshipType.Eats, grass));
    }

    [Test]
    public void SnowLeopard_EatsBlueSheep()
    {
        var leopard = Load<SpeciesDefinition>(SpeciesDir + "SnowLeopard.asset");
        var sheep = Load<SpeciesDefinition>(SpeciesDir + "BlueSheep.asset");

        Assert.IsTrue(leopard.HasRelationship(RelationshipType.Eats, sheep));
    }

    [Test]
    public void AllSpecies_HaveValidRelationships()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:SpeciesDefinition", new[] { SpeciesDir.TrimEnd('/') }))
        {
            var s = AssetDatabase.LoadAssetAtPath<SpeciesDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var r in s.relationships)
                Assert.IsTrue(r != null && r.IsValid, $"{s.name}: relationship {r} is invalid ({r?.ValidationError})");
        }
    }
}
// [AI-GENERATED END]
