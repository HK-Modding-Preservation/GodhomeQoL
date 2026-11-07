using TMPro;
using Vasi;

namespace GodhomeQoL.Modules.QoL;

public sealed class FastText : Module
{
    public override bool DefaultEnabled => true;

    public override ToggleableLevel ToggleableLevel => ToggleableLevel.ChangeScene;

    private protected override void Load() =>
        On.DialogueBox.ShowNextChar += OnNextChar;

    private protected override void Unload() =>
        On.DialogueBox.ShowNextChar -= OnNextChar;

    private static void OnNextChar(On.DialogueBox.orig_ShowNextChar orig, DialogueBox self)
    {
        TextMeshPro? text = ReflectionHelper.GetField<DialogueBox, TextMeshPro>(self, "textMesh");
        if (text == null || text.textInfo == null || text.textInfo.pageCount <= 0)
        {
            orig(self);
            return;
        }

        int pageIndex = Mathf.Clamp(self.currentPage - 1, 0, text.textInfo.pageCount - 1);
        text.maxVisibleCharacters = text.textInfo.pageInfo[pageIndex].lastCharacterIndex + 1;
    }
}
