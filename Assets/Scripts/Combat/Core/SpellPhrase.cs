public struct SpellPhrase
{
    public ModifierWord? Modifier { get; private set; }
    public ElementWord? Element { get; private set; }
    public FormWord? Form { get; private set; }

    public bool IsComplete => Modifier.HasValue && Element.HasValue && Form.HasValue;

    public void SetModifier(ModifierWord modifier)
    {
        Modifier = modifier;
    }

    public void SetElement(ElementWord element)
    {
        Element = element;
    }

    public void SetForm(FormWord form)
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
        var modifierLabel = Modifier.HasValue ? Modifier.Value.ToString() : "--";
        var elementLabel = Element.HasValue ? Element.Value.ToString() : "--";
        var formLabel = Form.HasValue ? Form.Value.ToString() : "--";

        return $"{modifierLabel} - {elementLabel} - {formLabel}";
    }
}
