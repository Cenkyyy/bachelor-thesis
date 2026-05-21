public struct SpellPhrase
{
    public ModifierWordType? Modifier { get; private set; }
    public ElementWordType? Element { get; private set; }
    public FormWordType? Form { get; private set; }

    public bool IsComplete => Modifier.HasValue && Element.HasValue && Form.HasValue;

    public void SetModifier(ModifierWordType modifier)
    {
        Modifier = modifier;
    }

    public void SetElement(ElementWordType element)
    {
        Element = element;
    }

    public void SetForm(FormWordType form)
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
