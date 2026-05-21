public struct SpellPhrase
{
    public ModifierWordData Modifier { get; private set; }
    public ElementWordData Element { get; private set; }
    public FormWordData Form { get; private set; }

    public bool IsComplete => Modifier != null && Element != null && Form != null;

    public void SetModifier(ModifierWordData modifier)
    {
        Modifier = modifier;
    }

    public void SetElement(ElementWordData element)
    {
        Element = element;
    }

    public void SetForm(FormWordData form)
    {
        Form = form;
    }

    public void Clear()
    {
        Modifier = null;
        Element = null;
        Form = null;
    }

    public override string ToString()
    {
        var modifierLabel = Modifier != null ? Modifier.DisplayName : "--";
        var elementLabel = Element != null ? Element.DisplayName : "--";
        var formLabel = Form != null ? Form.DisplayName : "--";

        return $"{modifierLabel} - {elementLabel} - {formLabel}";
    }
}
